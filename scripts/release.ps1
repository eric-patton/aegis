[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $repo "artifacts"
$clientPublishDir = Join-Path $artifactRoot "publish-client-$Runtime"
$toolsPublishDir = Join-Path $artifactRoot "publish-tools-$Runtime"
$stageDir = Join-Path $artifactRoot "aegis-1.0.0-$Runtime"
$extractDir = Join-Path $artifactRoot "smoke-aegis-1.0.0-$Runtime"
$zipPath = Join-Path $artifactRoot "aegis-1.0.0-$Runtime.zip"
$checksumPath = "$zipPath.sha256"
$warningBaselinePath = Join-Path $PSScriptRoot "aot-warning-baseline.txt"

if ((git -C $repo status --porcelain).Count -ne 0) {
    throw "Release packaging requires a clean worktree."
}

Get-Process aegis,aegis-tools -ErrorAction SilentlyContinue | Stop-Process -Force

foreach ($path in @($clientPublishDir, $toolsPublishDir, $stageDir, $extractDir)) {
    if (Test-Path -LiteralPath $path) {
        $resolved = [System.IO.Path]::GetFullPath($path)
        $root = [System.IO.Path]::GetFullPath($artifactRoot) + [System.IO.Path]::DirectorySeparatorChar
        if (-not $resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a path outside the artifact directory: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
foreach ($path in @($zipPath, $checksumPath)) {
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Force
    }
}

$clientPublishOutput = @(
    & dotnet publish (Join-Path $repo "src\Aegis.Client\Aegis.Client.csproj") `
        -c $Configuration -r $Runtime --self-contained true `
        -p:PublishAot=true -p:DebugType=None -p:DebugSymbols=false `
        -o $clientPublishDir 2>&1
)
$clientPublishExit = $LASTEXITCODE
$clientPublishOutput | ForEach-Object { Write-Output "$_" }
if ($clientPublishExit -ne 0) { throw "Client dotnet publish failed." }

$actualWarnings = @(
    foreach ($line in $clientPublishOutput) {
        if ("$line" -match "warning (IL\d+): Assembly '([^']+)'") {
            "$($Matches[1])|$($Matches[2])"
        }
    }
) | Sort-Object -Unique
$unclassifiedWarnings = @(
    $clientPublishOutput |
        Where-Object { "$_" -match "\bwarning\b" -and "$_" -notmatch "warning (IL\d+): Assembly '([^']+)'" }
)
if ($unclassifiedWarnings.Count -ne 0) {
    throw "Client AOT publish produced unclassified warnings: $($unclassifiedWarnings -join ' | ')"
}
$expectedWarnings = @(
    Get-Content -LiteralPath $warningBaselinePath |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
) | Sort-Object -Unique
$warningDrift = @(Compare-Object -ReferenceObject $expectedWarnings -DifferenceObject $actualWarnings)
if ($warningDrift.Count -ne 0) {
    throw "Client AOT warning baseline changed: $($warningDrift -join ' | ')"
}

$toolsPublishOutput = @(
    & dotnet publish (Join-Path $repo "src\Aegis.Cli\Aegis.Cli.csproj") `
        -c $Configuration -r $Runtime --self-contained true `
        -p:PublishAot=true -p:DebugType=None -p:DebugSymbols=false `
        -o $toolsPublishDir 2>&1
)
$toolsPublishExit = $LASTEXITCODE
$toolsPublishOutput | ForEach-Object { Write-Output "$_" }
if ($toolsPublishExit -ne 0) { throw "Tools dotnet publish failed." }
if (@($toolsPublishOutput | Where-Object { "$_" -match "\bwarning\b" }).Count -ne 0) {
    throw "Tools AOT publish produced warnings."
}

$required = @(
    (Join-Path $clientPublishDir "aegis.exe"),
    (Join-Path $clientPublishDir "SDL2.dll"),
    (Join-Path $clientPublishDir "openal.dll"),
    (Join-Path $toolsPublishDir "aegis-tools.exe"),
    (Join-Path $repo "README.md"),
    (Join-Path $repo "RELEASE-NOTES-1.0.0.md"),
    (Join-Path $repo "THIRD-PARTY-NOTICES.md")
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing package input: $path"
    }
}

New-Item -ItemType Directory -Path $stageDir | Out-Null
foreach ($path in $required) {
    Copy-Item -LiteralPath $path -Destination $stageDir
}

$commit = (git -C $repo rev-parse HEAD).Trim()
$manifest = @(
    "product=Aegis",
    "productVersion=1.0.0",
    "commit=$commit",
    "saveVersion=100",
    "generatorVersion=1",
    "runtimeIdentifier=$Runtime",
    "client=SadConsole 10.10.1",
    "host=MonoGame DesktopGL 3.8.4.1",
    "manifestNote=every packaged file except this manifest is hashed below",
    ""
)
foreach ($file in Get-ChildItem -LiteralPath $stageDir -File | Sort-Object Name) {
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    $manifest += "$hash  $($file.Name)"
}
$manifestPath = Join-Path $stageDir "SHA256SUMS.txt"
[System.IO.File]::WriteAllLines($manifestPath, $manifest, [System.Text.UTF8Encoding]::new($false))

Compress-Archive -Path (Join-Path $stageDir "*") -DestinationPath $zipPath
New-Item -ItemType Directory -Path $extractDir | Out-Null
Expand-Archive -LiteralPath $zipPath -DestinationPath $extractDir

$tools = Join-Path $extractDir "aegis-tools.exe"
$client = Join-Path $extractDir "aegis.exe"
$helpText = (& $tools --help) -join "`n"
if ($LASTEXITCODE -ne 0) { throw "Clean-extraction tools help smoke failed." }
if (-not $helpText.Contains("journey --release")) {
    throw "Clean-extraction tools help smoke did not expose the release route."
}
$sim = ((& $tools sim --seed 1 --keys "0....." --quiet --generator 1) -join "`n") | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Clean-extraction sim smoke failed." }
if ($sim.keysApplied -ne 6 -or
    $sim.final.cycle -ne 1 -or
    $sim.final.turn -ne 4 -or
    $sim.final.saveVersion -ne 100 -or
    $sim.final.generatorVersion -ne 1) {
    throw "Clean-extraction sim smoke did not reproduce the pinned state."
}
$worldgen = ((& $tools worldgen --seeds 1 --tiers 1 --json --generator 1) -join "`n") | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Clean-extraction worldgen smoke failed." }
if ($worldgen.worlds -ne 1 -or
    $worldgen.digestMismatches -ne 0 -or
    $worldgen.generatorVersion -ne 1) {
    throw "Clean-extraction worldgen smoke did not reproduce generator 1."
}

function Start-PilotClient {
    param(
        [string] $Session,
        [string] $SaveDirectory
    )

    $process = Start-Process -FilePath $client `
        -ArgumentList @(
            "--headless",
            "--pilot",
            "--session", $Session,
            "--save", "package-smoke",
            "--save-dir", $SaveDirectory,
            "--seed", "1"
        ) `
        -PassThru `
        -WindowStyle Hidden
    for ($attempt = 0; $attempt -lt 80; $attempt++) {
        & $tools pilot ping --session $Session 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            return $process
        }
        Start-Sleep -Milliseconds 100
    }

    if (-not $process.HasExited) {
        $process.Kill()
    }
    $process.Dispose()
    throw "Clean-extraction client pilot did not become ready."
}

$smokeSaveDirectory = Join-Path $extractDir "smoke-saves"
$first = Start-PilotClient -Session "package_create" -SaveDirectory $smokeSaveDirectory
try {
    & $tools pilot keys "150400..." --session "package_create" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Clean-extraction client input smoke failed." }
    $created = ((& $tools pilot state --session "package_create") -join "`n") | ConvertFrom-Json
    $frame = ((& $tools pilot frame --session "package_create") -join "`n") | ConvertFrom-Json
    if ($created.inCreation -or [string]::IsNullOrWhiteSpace($created.bearerName)) {
        throw "Clean-extraction client did not complete character creation."
    }
    if ($frame.width -ne 120 -or $frame.height -ne 40 -or $frame.cells.Count -ne 4800) {
        throw "Clean-extraction structured frame smoke failed."
    }
    & $tools pilot quit --session "package_create" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Clean-extraction client shutdown smoke failed." }
    if (-not $first.WaitForExit(5000)) { throw "Clean-extraction client did not exit." }
}
finally {
    if (-not $first.HasExited) { $first.Kill() }
    $first.Dispose()
}

$second = Start-PilotClient -Session "package_reload" -SaveDirectory $smokeSaveDirectory
try {
    $reloaded = ((& $tools pilot state --session "package_reload") -join "`n") | ConvertFrom-Json
    if ($reloaded.seed -ne $created.seed -or
        $reloaded.bearerName -ne $created.bearerName -or
        $reloaded.folk -ne $created.folk -or
        $reloaded.past -ne $created.past -or
        $reloaded.turn -ne $created.turn) {
        throw "Clean-extraction save reload did not reproduce the created state."
    }
    & $tools pilot quit --session "package_reload" | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "Clean-extraction reload shutdown smoke failed." }
    if (-not $second.WaitForExit(5000)) { throw "Clean-extraction reloaded client did not exit." }
}
finally {
    if (-not $second.HasExited) { $second.Kill() }
    $second.Dispose()
}

foreach ($line in Get-Content -LiteralPath (Join-Path $extractDir "SHA256SUMS.txt")) {
    if ($line -notmatch '^([0-9a-f]{64})  (.+)$') { continue }
    $actual = (Get-FileHash -LiteralPath (Join-Path $extractDir $Matches[2]) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Matches[1]) { throw "Hash mismatch for $($Matches[2])." }
}

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
[System.IO.File]::WriteAllText(
    $checksumPath,
    "$zipHash  $(Split-Path -Leaf $zipPath)`n",
    [System.Text.UTF8Encoding]::new($false))
Write-Output "package=$zipPath"
Write-Output "checksum=$checksumPath"
Write-Output "sha256=$zipHash"

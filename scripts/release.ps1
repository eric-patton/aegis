[CmdletBinding()]
param(
    [string] $Configuration = "Release",
    [string] $Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$repo = Split-Path -Parent $PSScriptRoot
$artifactRoot = Join-Path $repo "artifacts"
$publishDir = Join-Path $artifactRoot "publish-$Runtime"
$stageDir = Join-Path $artifactRoot "aegis-1.0.0-$Runtime"
$extractDir = Join-Path $artifactRoot "smoke-aegis-1.0.0-$Runtime"
$zipPath = Join-Path $artifactRoot "aegis-1.0.0-$Runtime.zip"

if ((git -C $repo status --porcelain).Count -ne 0) {
    throw "Release packaging requires a clean worktree."
}

Get-Process aegis -ErrorAction SilentlyContinue | Stop-Process -Force

foreach ($path in @($publishDir, $stageDir, $extractDir)) {
    if (Test-Path -LiteralPath $path) {
        $resolved = [System.IO.Path]::GetFullPath($path)
        $root = [System.IO.Path]::GetFullPath($artifactRoot) + [System.IO.Path]::DirectorySeparatorChar
        if (-not $resolved.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to remove a path outside the artifact directory: $resolved"
        }
        Remove-Item -LiteralPath $resolved -Recurse -Force
    }
}
if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

dotnet publish (Join-Path $repo "src\Aegis.Cli\Aegis.Cli.csproj") `
    -c $Configuration -r $Runtime --self-contained true `
    -p:PublishAot=true -p:DebugType=None -p:DebugSymbols=false `
    -o $publishDir
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed." }

$required = @(
    (Join-Path $publishDir "aegis.exe"),
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
Copy-Item -LiteralPath $required[0] -Destination $stageDir
Copy-Item -LiteralPath $required[1] -Destination $stageDir
Copy-Item -LiteralPath $required[2] -Destination $stageDir
Copy-Item -LiteralPath $required[3] -Destination $stageDir

$commit = (git -C $repo rev-parse HEAD).Trim()
$manifest = @(
    "product=Aegis",
    "productVersion=1.0.0",
    "commit=$commit",
    "saveVersion=99",
    "generatorVersion=1",
    "runtimeIdentifier=$Runtime",
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

$exe = Join-Path $extractDir "aegis.exe"
$helpText = (& $exe --help) -join "`n"
if ($LASTEXITCODE -ne 0) { throw "Clean-extraction help smoke failed." }
if (-not $helpText.Contains("journey --release")) {
    throw "Clean-extraction help smoke did not expose the release route."
}
$sim = ((& $exe sim --seed 1 --keys "0...." --quiet --generator 1) -join "`n") | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Clean-extraction sim smoke failed." }
if ($sim.keysApplied -ne 5 -or $sim.final.cycle -ne 1 -or $sim.final.turn -ne 4
    -or $sim.final.saveVersion -ne 99 -or $sim.final.generatorVersion -ne 1) {
    throw "Clean-extraction sim smoke did not reproduce the pinned state."
}
$worldgen = ((& $exe worldgen --seeds 1 --tiers 1 --json --generator 1) -join "`n") | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "Clean-extraction worldgen smoke failed." }
if ($worldgen.worlds -ne 1 -or $worldgen.digestMismatches -ne 0
    -or $worldgen.generatorVersion -ne 1) {
    throw "Clean-extraction worldgen smoke did not reproduce generator 1."
}

foreach ($line in Get-Content -LiteralPath (Join-Path $extractDir "SHA256SUMS.txt")) {
    if ($line -notmatch '^([0-9a-f]{64})  (.+)$') { continue }
    $actual = (Get-FileHash -LiteralPath (Join-Path $extractDir $Matches[2]) -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $Matches[1]) { throw "Hash mismatch for $($Matches[2])." }
}

$zipHash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
Write-Output "package=$zipPath"
Write-Output "sha256=$zipHash"

using Aegis.Client;
using Aegis.Host;
using SadConsole;
using SadConsole.Configuration;
using SadRogue.Primitives;

ClientOptions options;
try
{
    options = ClientOptions.Parse(args);
}
catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
{
    WriteStartupError(ex.Message);
    return 1;
}

using var runtime = new ClientRuntime(options);
PresentationSettings presentation = PresentationSettings.Load();
if (options.FontScaleOverride is { } scale)
{
    presentation.FontScale = scale;
    presentation.Save();
}

if (options.Headless)
{
    var session = new GameSession(runtime.Game, null);
    using var server = new PilotServer(options.Session, session.Writer);
    server.Start();
    await session.RunAsync();
    await Task.Delay(250);
    server.Stop();
    return 0;
}

AegisScreen? screen = null;
Settings.WindowTitle = "Aegis";
Settings.ResizeMode = Settings.WindowResizeOptions.Fit;
Settings.WindowMinimumSize = new Point(640, 400);
Settings.ClearColor = new Color(
    AegisPalette.Clear.R,
    AegisPalette.Clear.G,
    AegisPalette.Clear.B);

Builder
    .GetBuilder()
    .ConfigureFonts(true)
    .SetDefaultFontSize(presentation.FontScale == 2 ? IFont.Sizes.Two : IFont.Sizes.One)
    .SetWindowSizeInCells(GameSession.ObservationWidth, GameSession.ObservationHeight)
    .SetStartingScreen(_ => screen = new AegisScreen(options, runtime, presentation))
    .IsStartingScreenFocused(true)
    .Run();

screen?.Dispose();
return 0;

static void WriteStartupError(string message)
{
    string directory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Aegis");
    Directory.CreateDirectory(directory);
    File.WriteAllText(
        Path.Combine(directory, "startup-error.txt"),
        message,
        PilotWire.Utf8NoBom);
}

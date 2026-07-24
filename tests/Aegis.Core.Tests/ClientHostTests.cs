using System.Threading.Channels;
using Aegis.Client;
using Aegis.Core;
using Aegis.Host;
using SadConsole.Input;

namespace Aegis.Core.Tests;

public class ClientHostTests
{
    [Fact]
    public void Palette_ResolvesEveryHueToTheApprovedRgb()
    {
        Assert.Equal(new Rgb24(12, 16, 22), AegisPalette.Resolve(Hue.Black));
        Assert.Equal(new Rgb24(232, 237, 243), AegisPalette.Resolve(Hue.White));
        Assert.Equal(new Rgb24(244, 112, 118), AegisPalette.Resolve(Hue.Red));
        Assert.Equal(new Rgb24(96, 211, 231), AegisPalette.Resolve(Hue.Cyan));

        foreach (Hue hue in Enum.GetValues<Hue>())
            Assert.InRange(AegisPalette.Resolve(hue).Packed, 0, 0xFFFFFF);
    }

    [Theory]
    [InlineData(Keys.Up, '\0', 'k')]
    [InlineData(Keys.Down, '\0', 'j')]
    [InlineData(Keys.Left, '\0', 'h')]
    [InlineData(Keys.Right, '\0', 'l')]
    [InlineData(Keys.Escape, '\0', 'q')]
    [InlineData(Keys.K, 'K', 'K')]
    [InlineData(Keys.OemPeriod, '>', '>')]
    public void SadConsoleInput_PreservesCanonicalCharactersAndAliases(
        Keys key,
        char character,
        char expected)
    {
        Assert.Equal(expected, SadConsoleInputMapper.Map(key, character));
    }

    [Fact]
    public void SadConsoleInput_UsesConventionalCreationKeysWithoutControlCharacters()
    {
        Assert.Equal('-', SadConsoleInputMapper.Map(Keys.Back, '\b', ClientSurface.CreationText));
        Assert.Equal('.', SadConsoleInputMapper.Map(Keys.Enter, '\r', ClientSurface.CreationText));
        Assert.Equal('.', SadConsoleInputMapper.Map(Keys.Enter, '\r', ClientSurface.CreationReview));
        Assert.Equal('[', SadConsoleInputMapper.Map(Keys.Escape, '\0', ClientSurface.CreationChoice));
    }

    [Fact]
    public void InteractionContext_DescribesWorldCreationMenusAndConversation()
    {
        var world = new Game(176);
        ClientInteractionContext worldContext = ClientInteractionContext.From(
            world,
            Presenter.Render(world, 120, 40));
        Assert.Equal(ClientSurface.World, worldContext.Surface);
        Assert.Empty(worldContext.Actions);
        Assert.True(worldContext.SupportsCompass);

        var creation = new Game(176, firstWake: true);
        creation.ApplyKey('1');
        creation.ApplyKey('1');
        creation.ApplyKey('1');
        ClientInteractionContext creationContext = ClientInteractionContext.From(
            creation,
            Presenter.Render(creation, 120, 40));
        Assert.Equal(ClientSurface.CreationChoice, creationContext.Surface);
        Assert.Contains(creationContext.Actions, a => a.Key == '1' && !a.Enabled);
        Assert.Contains(creationContext.Actions, a => a.Key == '2' && a.Enabled);

        var talk = new Game(176);
        Npc npc = talk.World.Npcs.First();
        Pos beside = Directions.All8
            .Select(d => npc.Pos.Plus(d.dx, d.dy))
            .First(p => talk.CurrentMap.Walkable(p)
                && !talk.World.Npcs.Any(other => other.Pos == p));
        talk.Debug_SetPlayerPos(beside);
        (int dx, int dy) = (npc.Pos.X - beside.X, npc.Pos.Y - beside.Y);
        char key = (dx, dy) switch
        {
            (0, -1) => 'k',
            (0, 1) => 'j',
            (-1, 0) => 'h',
            (1, 0) => 'l',
            (-1, -1) => 'y',
            (1, -1) => 'u',
            (-1, 1) => 'b',
            (1, 1) => 'n',
            _ => throw new InvalidOperationException("NPC was not adjacent"),
        };
        talk.ApplyKey(key);
        ClientInteractionContext talkContext = ClientInteractionContext.From(
            talk,
            Presenter.Render(talk, 120, 40));
        Assert.Equal(ClientSurface.Conversation, talkContext.Surface);
        Assert.NotEmpty(talkContext.Actions);
        Assert.NotEmpty(talkContext.Transcript);
        Assert.Contains(npc.Name, talkContext.Title);
    }

    [Fact]
    public void Session_FrameObservationIsFixedAndUsesResolvedColors()
    {
        var game = new Game(176);
        var session = new GameSession(game, null);
        PilotResponse response = Dispatch(session, new PilotRequest { Cmd = "frame" });

        Assert.True(response.Ok);
        Assert.NotNull(response.Frame);
        Assert.Equal(120, response.Frame.Width);
        Assert.Equal(40, response.Frame.Height);
        Assert.Equal(120 * 40, response.Frame.Cells.Length);

        Frame expected = Presenter.Render(game, 120, 40);
        Cell first = expected[0, 0];
        FrameCellObservation observed = response.Frame.Cells[0];
        Assert.Equal(first.Ch, observed.Glyph);
        Assert.Equal(AegisPalette.Resolve(first.Fg).Packed, observed.Foreground);
        Assert.Equal(AegisPalette.Resolve(first.Bg).Packed, observed.Background);
    }

    [Fact]
    public void Session_KeyBatchIsAppliedBeforeItsResult()
    {
        var game = new Game(176);
        var session = new GameSession(game, null);
        PilotResponse response = Dispatch(
            session,
            new PilotRequest { Cmd = "keys", Keys = "..." });

        Assert.True(response.Ok);
        Assert.Equal(3, response.State!.Turn);
        Assert.Equal(40, response.Screen!.Length);
    }

    [Fact]
    public void Session_RoutesPresentationControlsWithoutChangingEngineState()
    {
        var game = new Game(176);
        var sink = new PresentationSink();
        var session = new GameSession(game, sink);

        PilotResponse response = Dispatch(
            session,
            new PilotRequest { Cmd = "ui", Action = "guide" });

        Assert.True(response.Ok);
        Assert.Equal("guide", sink.LastAction);
        Assert.Equal(0, game.Turn);
    }

    [Fact]
    public async Task PhysicalAndPilotInput_RetainQueueOrderAndBatchAtomicity()
    {
        var game = new Game(176);
        var session = new GameSession(game, null);
        var completion = new TaskCompletionSource<PilotResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        Assert.True(session.Writer.TryWrite(new HostMessage.Key('.')));
        Assert.True(session.Writer.TryWrite(new HostMessage.Pilot(
            new PilotRequest { Cmd = "keys", Keys = ".." },
            completion)));
        Assert.True(session.Writer.TryWrite(new HostMessage.Key('.')));
        session.Drain();

        PilotResponse response = await completion.Task;
        Assert.Equal(3, response.State!.Turn);
        Assert.Equal(4, game.Turn);
    }

    [Fact]
    public void StructuredFrame_MatchesPresenterAcrossRepresentativeStates()
    {
        var creation = new Game(176, firstWake: true);
        var overworld = new Game(176);

        var menu = new Game(176);
        menu.ApplyKey('?');

        var local = new Game(176);
        local.Debug_SetPlayerPos(local.World.CampSite.OverworldPos);
        local.Apply(Command.Enter);

        var death = new Game(176);
        death.Debug_SetMode(MapMode.Site);
        death.Debug_HurtPlayer(999);
        death.Debug_ForceDeathCheck();

        var laterWorld = new Game(176);
        laterWorld.Debug_ClearCamp();
        laterWorld.Debug_SetPlayerPos(laterWorld.World.GatePos);
        laterWorld.Apply(Command.Enter);
        laterWorld.Apply(Command.Enter);

        foreach (Game game in new[] { creation, overworld, menu, local, death, laterWorld })
            AssertFrameMatches(game);
    }

    [Fact]
    public void SaveFile_AppendsAndReloadsCanonicalKeys()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            $"aegis-save-{Guid.NewGuid():N}");
        string path = SaveFile.SlotPath(directory, "host-test");
        try
        {
            using (var save = SaveFile.Open(path, 176))
            {
                foreach (char key in "150400...")
                    save.Game.ApplyKey(key);
                Assert.False(save.Game.InCreation);
            }

            using var loaded = SaveFile.Open(path, 999);
            Assert.True(loaded.Loaded);
            Assert.False(loaded.Game.InCreation);
            Assert.NotEmpty(loaded.Game.Player.Name);
            Assert.Equal(176UL, loaded.Game.World.Seed);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Session_RejectsOversizedAtomicBatch()
    {
        var session = new GameSession(new Game(176), null);
        PilotResponse response = Dispatch(
            session,
            new PilotRequest
            {
                Cmd = "keys",
                Keys = new string('.', PilotWire.MaxKeyBatchLength + 1),
            });

        Assert.False(response.Ok);
        Assert.Contains("too large", response.Error);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("has/slash")]
    public void PilotSessionName_RejectsUnsafeNames(string session)
    {
        Assert.Throws<ArgumentException>(() => PilotWire.PipeName(session));
    }

    [Fact]
    public void PresentationSettings_AreSeparateAndClampFontScale()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            $"aegis-presentation-{Guid.NewGuid():N}.json");
        try
        {
            var settings = PresentationSettings.Load(path);
            settings.FontScale = 2;
            settings.HelpSeen = true;
            settings.GuideSeen = true;
            settings.Save();

            PresentationSettings loaded = PresentationSettings.Load(path);
            Assert.Equal(2, loaded.FontScale);
            Assert.True(loaded.HelpSeen);
            Assert.True(loaded.GuideSeen);

            File.WriteAllText(path, """{"fontScale":99,"helpSeen":false}""");
            Assert.Equal(2, PresentationSettings.Load(path).FontScale);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task CurrentUserPipe_HandlesPingKeysFrameAndQuit()
    {
        string sessionName = $"test_{Guid.NewGuid():N}";
        var game = new Game(176);
        var session = new GameSession(game, null);
        using var server = new PilotServer(sessionName, session.Writer);
        server.Start();
        Task loop = session.RunAsync();

        PilotResponse ping = await ExchangeEventually(sessionName, new PilotRequest { Cmd = "ping" });
        Assert.True(ping.Ok);

        PilotResponse keys = PilotConnection.Exchange(
            sessionName,
            new PilotRequest { Cmd = "keys", Keys = ".." })!;
        Assert.True(keys.Ok);
        Assert.Equal(2, keys.State!.Turn);

        PilotResponse frame = PilotConnection.Exchange(
            sessionName,
            new PilotRequest { Cmd = "frame" })!;
        Assert.True(frame.Ok);
        Assert.Equal(120 * 40, frame.Frame!.Cells.Length);

        PilotResponse quit = PilotConnection.Exchange(
            sessionName,
            new PilotRequest { Cmd = "quit" })!;
        Assert.True(quit.Ok);
        await loop.WaitAsync(TimeSpan.FromSeconds(5));
    }

    private static PilotResponse Dispatch(GameSession session, PilotRequest request)
    {
        var completion = new TaskCompletionSource<PilotResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.True(session.Writer.TryWrite(new HostMessage.Pilot(request, completion)));
        session.Drain();
        return completion.Task.GetAwaiter().GetResult();
    }

    private static void AssertFrameMatches(Game game)
    {
        Frame expected = Presenter.Render(game, 120, 40);
        FrameObservation observed = FrameObservation.From(expected);
        Assert.Equal(expected.Width, observed.Width);
        Assert.Equal(expected.Height, observed.Height);

        int index = 0;
        for (int y = 0; y < expected.Height; y++)
        {
            for (int x = 0; x < expected.Width; x++)
            {
                Cell cell = expected[x, y];
                FrameCellObservation actual = observed.Cells[index++];
                Assert.Equal(cell.Ch, actual.Glyph);
                Assert.Equal(AegisPalette.Resolve(cell.Fg).Packed, actual.Foreground);
                Assert.Equal(AegisPalette.Resolve(cell.Bg).Packed, actual.Background);
            }
        }
    }

    private static async Task<PilotResponse> ExchangeEventually(
        string session,
        PilotRequest request)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            try
            {
                return PilotConnection.Exchange(session, request, 100)!;
            }
            catch (TimeoutException)
            {
                await Task.Delay(25);
            }
        }

        throw new TimeoutException("Pilot server did not start.");
    }

    private sealed class PresentationSink : IFrameSink
    {
        public (int Width, int Height) CurrentSize => (120, 40);
        public string LastAction { get; private set; } = "";

        public void Draw(Frame frame, ClientInteractionContext interaction)
        {
        }

        public PilotResponse? HandlePresentation(PilotRequest request)
        {
            LastAction = request.Action ?? "";
            return new PilotResponse { Ok = true };
        }
    }
}

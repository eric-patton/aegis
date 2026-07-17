namespace Aegis.Core;

public enum MapMode { Overworld, Site }

/// <summary>What the stead sells (D-036): goods and services coin can become.</summary>
public enum TradeGood { Ration, Mending }

/// <summary>
/// The deterministic game engine. No console, no I/O, no wall clock: state advances
/// only through <see cref="Apply(Command)"/>, and all randomness flows from the seed
/// tree. Frontends (TUI, pilot, sim) are pure drivers around this class.
/// </summary>
public sealed class Game
{
    /// <summary>The save identity: every world in this game derives from it.</summary>
    public ulong MasterSeed { get; }

    /// <summary>Which world of the chain this is, 1-based. Also the Hostility Tier (D-011).</summary>
    public int Cycle { get; private set; } = 1;

    public World World { get; private set; }
    public Player Player { get; } = new();
    public MessageLog Log { get; } = new();
    public List<Monster> Monsters { get; } = [];
    public Remnant? Remnant { get; private set; }
    public MapMode Mode { get; private set; } = MapMode.Overworld;
    public int Turn { get; private set; }
    public bool Running { get; private set; } = true;

    /// <summary>The site the player is inside, null on the overworld.</summary>
    public Site? CurrentSite { get; private set; }

    /// <summary>The camp deed is what opens the waygate (D-029); other sites are optional depth.</summary>
    public bool CampCleared => World.CampSite.Cleared;
    public bool InShrineMenu { get; private set; }
    public bool InTalkMenu { get; private set; }
    public bool InUnbindMenu { get; private set; }
    public Npc? TalkNpc { get; private set; }

    /// <summary>Unbindings per world (D-016: a handful, refreshed at each crossing).</summary>
    public const int UnbindingsPerWorld = 3;

    /// <summary>How many unbindings this world's Unbinder will still perform.</summary>
    public int UnbindingsLeft { get; private set; } = UnbindingsPerWorld;

    /// <summary>
    /// What loosening one raise returns: exactly what re-buying it will cost at the
    /// shrine afterward, so a respec round trip never gains or loses essence.
    /// </summary>
    public int UnbindRefund => 10 + 5 * (Player.Attributes.TotalRaises - 1);

    /// <summary>The current conversation's topics, computed live from the fact graph at open.</summary>
    public IReadOnlyList<(string Label, string Answer)> Topics => _topics;
    private readonly List<(string Label, string Answer)> _topics = [];

    /// <summary>What the current conversation partner sells (D-036), listed after the topics.</summary>
    public IReadOnlyList<(TradeGood Good, string Label)> Offers => _offers;
    private readonly List<(TradeGood Good, string Label)> _offers = [];

    /// <summary>Most rations a person can carry: the sink recurs instead of stockpiling.</summary>
    public const int RationCap = 5;

    /// <summary>
    /// Fact-derived pricing (D-025 v0): while a blight story stands uncompleted,
    /// the larders are thin and bread costs half again as much.
    /// </summary>
    public int RationPrice =>
        World.Facts.Exists("story", CreepingBlightTemplate.Id)
        && !World.Facts.Exists("story_complete", CreepingBlightTemplate.Id) ? 6 : 4;

    /// <summary>
    /// What the herbwife asks to dress the wound: priced by how much convalescence
    /// it buys off, so waiting it out is always the poor bearer's option.
    /// </summary>
    public int MendPrice => (Player.WoundedTurns + 3) / 4;

    /// <summary>
    /// Fired for every key that reached the engine while running (including menu keys
    /// and refused moves, which still write log entries). The save journal records
    /// exactly these; replaying them reproduces this game bit for bit.
    /// </summary>
    public event Action<char>? KeyApplied;

    private Rng _combatRng;
    private readonly StoryletEngine _storylets;

    /// <summary>Total storylets fired this character, all worlds (observability).</summary>
    public int StoryletsFired => _storylets.TotalFired;

    public Game(ulong seed)
    {
        MasterSeed = seed;
        // Cycle 1 uses the master seed directly, so pre-crossing saves stay replayable.
        World = WorldGen.Generate(seed);
        _combatRng = new Rng(SeedTree.Derive(World.Seed, "combat"));
        _storylets = new StoryletEngine(World.Seed, FullCatalog());
        Player.Pos = World.ShrinePos;
        SpawnMonsters();

        Log.Add(0, $"You wake at the shrine of {World.SettlementName}, in the world called {World.Name}.");
        Log.Add(0, "A voice, close as your own pulse: \"Walk. I hold this place. I will catch you.\"", LogTone.Aegis);
        Log.Add(0, $"Rumor: goblins from a cave to the {Compass(World.ShrinePos, World.CampPos)} raid {World.SettlementName}'s stores by night.");
        _storylets.TryFire(this, StoryletTrigger.Arrival);
    }

    /// <summary>Global authored content plus this world's compiled story (D-032).</summary>
    private List<Storylet> FullCatalog() => [.. StoryletCatalog.All, .. World.StoryStorylets];

    private void SpawnMonsters()
    {
        // Stats are generation inputs baked into the spawns (D-011), never live multipliers.
        foreach (var site in World.Sites)
            foreach (var spawn in site.Spawns)
                Monsters.Add(new Monster { Kind = spawn.Kind, Pos = spawn.Pos, Hp = spawn.Hp, SiteId = site.Id });
    }

    internal static string Compass(Pos from, Pos to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        string ns = dy <= -Math.Abs(dx) / 2 ? "north" : dy >= Math.Abs(dx) / 2 ? "south" : "";
        string ew = dx <= -Math.Abs(dy) / 2 ? "west" : dx >= Math.Abs(dy) / 2 ? "east" : "";
        string dir = ns + ew;
        return dir.Length == 0 ? "near" : dir;
    }

    public GameMap CurrentMap => Mode == MapMode.Overworld ? World.Overworld : CurrentSite!.Map;

    private string CurrentMapId => CurrentMap.Id;

    public IEnumerable<Monster> LiveMonstersHere =>
        Mode == MapMode.Site ? Monsters.Where(m => m.Alive && m.SiteId == CurrentSite!.Id) : [];

    /// <summary>
    /// Applies one key press: the single entry point every frontend and the save
    /// journal share. Menu keys are routed before command mapping.
    /// </summary>
    public void ApplyKey(char key)
    {
        if (!Running) return;

        if (InUnbindMenu)
        {
            HandleUnbindMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InTalkMenu)
        {
            HandleTalkMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InShrineMenu)
        {
            HandleShrineMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        var cmd = CommandMap.FromKey(key);
        if (cmd == Command.None) return;

        Apply(cmd);
        if (cmd != Command.Quit) KeyApplied?.Invoke(key);
    }

    /// <summary>Applies a command directly (tests). Frontends use <see cref="ApplyKey"/>.</summary>
    public void Apply(Command cmd)
    {
        if (!Running || cmd == Command.None) return;

        if (cmd == Command.Quit)
        {
            Running = false;
            return;
        }

        bool tookTime = cmd switch
        {
            Command.Wait => DoWait(),
            Command.Enter => DoEnter(),
            Command.Exit => DoExit(),
            Command.Grab => DoGrab(),
            Command.Rest => DoRest(),
            Command.Eat => DoEat(),
            _ => CommandMap.Delta(cmd) is { } d && DoMove(d.dx, d.dy),
        };

        if (tookTime) AdvanceTurn();
    }

    private bool DoMove(int dx, int dy)
    {
        var target = Player.Pos.Plus(dx, dy);
        var map = CurrentMap;

        if (Mode == MapMode.Site)
        {
            var blocker = Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == target);
            if (blocker is not null) return AttackMonster(blocker);
        }
        else
        {
            var npc = World.Npcs.FirstOrDefault(n => n.Pos == target);
            if (npc is not null) return StartTalk(npc);
        }

        if (!map.Walkable(target))
        {
            Log.Add(Turn, Mode == MapMode.Site ? "Rough stone blocks the way." : "You cannot cross there.");
            return false;
        }

        Player.Pos = target;
        Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 1);
        DescribeTileIfNotable(target);

        _storylets.TryFire(this, StoryletTrigger.EnterTile, map[target]);
        if (Mode == MapMode.Overworld && Directions.All8.Any(d =>
                map.InBounds(target.Plus(d.dx, d.dy)) && map[target.Plus(d.dx, d.dy)] == Terrain.House))
            _storylets.TryFire(this, StoryletTrigger.NearHouse);
        return true;
    }

    private void DescribeTileIfNotable(Pos p)
    {
        var t = CurrentMap[p];
        if (Mode == MapMode.Overworld)
        {
            if (t == Terrain.CampEntrance)
                Log.Add(Turn, "A cave mouth, littered with gnawed bones. Press > to descend.", LogTone.Danger);
            else if (t == Terrain.Shrine)
                Log.Add(Turn, "The shrine hums faintly. The Aegis anchors here. Press r to rest.", LogTone.Aegis);
            else if (t == Terrain.Waygate)
                Log.Add(Turn, CampCleared
                    ? "An arch of black iron links. It hums, and the air beyond it is not this world's. Press > to cross."
                    : "An arch of black iron links, older than the stones around it. It is shut.", LogTone.Aegis);
            else if (t == Terrain.BarrowEntrance)
                Log.Add(Turn, World.BarrowSite!.Cleared
                    ? "The long mound. Its stones are only stones now."
                    : "A long mound of turf over lintel stones. The passage under it exhales cold. Press > to stoop in.", LogTone.Danger);
            else if (t == Terrain.HollowEntrance)
                Log.Add(Turn, World.HollowSite!.Cleared
                    ? "The stone ring. Its fire is out, and the stones hold nothing now but weather."
                    : "A ring of standing stones. Inside it a small fire burns, though no one gathers wood. Press > to step in.", LogTone.Danger);
        }
        else
        {
            if (Remnant is not null && Remnant.MapId == CurrentMapId && Remnant.Pos == p)
                Log.Add(Turn, "Your remnant lies here: what you dropped when you fell. Press g to reclaim it.", LogTone.Reward);
            else if (!CurrentSite!.ChestLooted && p == CurrentSite.ChestPos)
                Log.Add(Turn, CurrentSite.Kind switch
                {
                    SiteKind.Barrow => "Grave goods lie here on a stone shelf, dressed in dust. Press g to take them.",
                    SiteKind.Hollow => "A bundle of kept things lies here, wrapped against rain with great care. Press g to take it.",
                    _ => "A battered strongbox sits here. Press g to open it.",
                }, LogTone.Reward);
            else if (t == Terrain.ExitLadder)
                Log.Add(Turn, "Daylight above. Press < to climb out.");
        }
    }

    private bool DoWait()
    {
        Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 2);
        return true;
    }

    private bool DoEnter()
    {
        if (Mode == MapMode.Overworld && World.Sites.FirstOrDefault(s => s.OverworldPos == Player.Pos) is { } site)
        {
            Mode = MapMode.Site;
            CurrentSite = site;
            Player.Pos = site.EntryPos;
            Log.Add(Turn, site.Kind switch
            {
                SiteKind.Barrow => "You stoop under the lintel stone. The air inside is still, and cold, and does not want you.",
                SiteKind.Hollow => "You step between the stones. The air changes, the way a room changes when someone in it has been waiting.",
                _ => "You descend into the goblin cave. The dark smells of smoke and old meat.",
            }, LogTone.Danger);
            if (site.Kind == SiteKind.Hollow && !site.Cleared)
            {
                Log.Add(Turn, "At the fire, a figure rises: neither old nor young, dressed out of no living fashion. It looks at your collarbone before it looks at your face.", LogTone.Danger);
                Log.Add(Turn, "\"All is counted, little shield.\" Courteous, and wrong, like a bell with a hairline crack.", LogTone.Danger);
            }
            return true;
        }
        if (Mode == MapMode.Overworld && Player.Pos == World.GatePos)
        {
            if (!CampCleared)
            {
                Log.Add(Turn, "The arch does not stir.", LogTone.Info);
                Log.Add(Turn, $"\"{AegisVoice.GateShutLine}\"", LogTone.Aegis);
                return false;
            }
            CrossToNextWorld();
            return true;
        }
        Log.Add(Turn, "There is nothing to enter here.");
        return false;
    }

    /// <summary>
    /// The NG+ crossing (D-011): character carries, coin converts to Legend, an
    /// unreclaimed remnant is forfeited, and the next world generates one tier
    /// deeper from a seed derived off the master. The completed deed is pressed
    /// into the new world's facts (D-013's mythology pipe).
    /// </summary>
    private void CrossToNextWorld()
    {
        string prevWorld = World.Name;
        string prevSettlement = World.SettlementName;

        if (Remnant is not null)
        {
            Log.Add(Turn, $"\"{AegisVoice.ForfeitLine}\"", LogTone.Aegis);
            Remnant = null;
        }

        int converted = Player.Coin;
        Player.Legend += converted;
        Player.Coin = 0;

        Cycle++;
        World = WorldGen.Generate(SeedTree.Derive(MasterSeed, "cycle", Cycle), tier: Cycle);
        _combatRng = new Rng(SeedTree.Derive(World.Seed, "combat"));
        _storylets.OnCrossing(World.Seed, FullCatalog());
        Monsters.Clear();
        SpawnMonsters();
        InShrineMenu = false;
        InTalkMenu = false;
        InUnbindMenu = false;
        TalkNpc = null;
        CurrentSite = null;
        UnbindingsLeft = UnbindingsPerWorld;

        Mode = MapMode.Overworld;
        Player.Pos = World.ShrinePos;
        Player.WoundedTurns = 0;
        Player.Hp = Player.MaxHp;
        Player.Stamina = Player.MaxStamina;

        World.Facts.Add("echo", "deed", prevSettlement,
            $"In a world called {prevWorld}, the bearer emptied a goblin cave, and {prevSettlement} slept safe.");

        Log.Add(Turn, $"You step through the arch, and {prevWorld} folds shut behind you like a closed book.", LogTone.Danger);
        // The crossing is the arc's guaranteed real estate (arc sec 5). Rungs are
        // gated on earlier rungs' flags, never on cycle counts, so slow players and
        // beeliners climb the same ladder in the same order.
        if (Cycle == 2)
        {
            Log.Add(Turn, $"\"{AegisVoice.FirstCrossingLine1}\"", LogTone.Aegis);
            Log.Add(Turn, $"\"{AegisVoice.FirstCrossingLine2}\"", LogTone.Aegis);
        }
        else if (Player.SeveredTruthHeard && !Player.CrossingGuiltHeard)
        {
            Player.CrossingGuiltHeard = true;
            foreach (string line in AegisVoice.CrossingGuiltLines)
                Log.Add(Turn, $"\"{line}\"", LogTone.Aegis);
        }
        else if (Player.VisionSeen && !Player.LedgerHeard)
        {
            Player.LedgerHeard = true;
            foreach (string line in AegisVoice.CrossingLedgerLines)
                Log.Add(Turn, $"\"{line}\"", LogTone.Aegis);
        }
        else if (Player.UnbinderRevealTier >= 2 && !Player.CommissionHeard)
        {
            Player.CommissionHeard = true;
            foreach (string line in AegisVoice.CrossingCommissionLines)
                Log.Add(Turn, $"\"{line}\"", LogTone.Aegis);
        }
        else
        {
            Log.Add(Turn, $"\"{AegisVoice.LaterCrossingLine}\"", LogTone.Aegis);
        }
        if (converted > 0)
        {
            Log.Add(Turn, $"\"{AegisVoice.CoinConvertedLine}\"", LogTone.Aegis);
            Log.Add(Turn, $"Your {converted} coin is weighed at the threshold and taken. Legend grows by {converted}.", LogTone.Reward);
        }

        Log.Add(Turn, $"You wake at the shrine of {World.SettlementName}, in the world called {World.Name}.");
        Log.Add(Turn, "The air is older here, and hungrier.", LogTone.Danger);
        Log.Add(Turn, $"In {World.SettlementName} they already sing of a stranger who emptied a goblin cave, in a world called {prevWorld}.");
        Log.Add(Turn, $"Rumor: goblins from a cave to the {Compass(World.ShrinePos, World.CampPos)} raid {World.SettlementName}'s stores by night.");
        if (World.BarrowSite is { } barrow)
            Log.Add(Turn, $"They speak lower of the long mound to the {Compass(World.ShrinePos, barrow.OverworldPos)}, where the dead do not lie easy.");
        if (World.HollowSite is { } hollow)
            Log.Add(Turn, $"And of the stone ring to the {Compass(World.ShrinePos, hollow.OverworldPos)} they say only this: leave the fire there to its keeper.");
        if (World.SeveredNpc is { } calm)
            Log.Add(Turn, $"A hermit called {calm.Name} keeps a fire to the {Compass(World.ShrinePos, calm.Pos)}. The stead trades them nothing, owes them nothing, and minds them not at all: they have simply always been there.");
        _storylets.TryFire(this, StoryletTrigger.Arrival);
    }

    private bool DoExit()
    {
        if (Mode == MapMode.Site && CurrentSite!.Map[Player.Pos] == Terrain.ExitLadder)
        {
            Mode = MapMode.Overworld;
            Player.Pos = CurrentSite.OverworldPos;
            CurrentSite = null;
            Log.Add(Turn, "You climb back into daylight.");
            return true;
        }
        Log.Add(Turn, "There is no way out here.");
        return false;
    }

    private bool DoGrab()
    {
        if (Remnant is not null && Remnant.MapId == CurrentMapId && Remnant.Pos == Player.Pos)
        {
            Player.Coin += Remnant.Coin;
            Player.Essence += Remnant.Essence;
            Log.Add(Turn, $"You reclaim your remnant: {Remnant.Coin} coin, {Remnant.Essence} essence.", LogTone.Reward);
            Log.Add(Turn, $"\"{AegisVoice.ReclaimLine}\"", LogTone.Aegis);
            Remnant = null;
            return true;
        }

        if (Mode == MapMode.Site && !CurrentSite!.ChestLooted && Player.Pos == CurrentSite.ChestPos)
        {
            int coin = CurrentSite.Kind switch
            {
                SiteKind.Barrow => _combatRng.Range(15, 27),
                SiteKind.Hollow => _combatRng.Range(4, 10),
                _ => _combatRng.Range(10, 21),
            };
            Player.Coin += coin;
            CurrentSite.ChestLooted = true;
            Log.Add(Turn, CurrentSite.Kind switch
            {
                SiteKind.Barrow => $"Grave-gold: {coin} coin struck for rulers whose names did not keep.",
                SiteKind.Hollow => $"What they kept: a child's wooden horse, a ring sized for a thinner hand, and {coin} coin of a mint no one living has seen.",
                _ => $"The strongbox yields {coin} coin.",
            }, LogTone.Reward);
            return true;
        }

        Log.Add(Turn, "There is nothing here to take.");
        return false;
    }

    /// <summary>
    /// Opens a conversation (D-023's ask-about surface, D-031). Topics are computed
    /// from the fact graph at open, so what people can discuss tracks the world's
    /// actual state. First meetings are written back to the graph.
    /// </summary>
    private bool StartTalk(Npc npc)
    {
        TalkNpc = npc;
        InTalkMenu = true;
        _topics.Clear();
        _topics.AddRange(npc.Kind switch
        {
            NpcKind.Unbinder => BuildUnbinderTopics(npc),
            NpcKind.Severed => BuildSeveredTopics(),
            _ => BuildTopics(npc),
        });
        _offers.Clear();
        if (npc.Kind == NpcKind.Villager) _offers.AddRange(BuildOffers(npc));

        if (npc.Kind == NpcKind.Severed)
        {
            Log.Add(Turn, $"{npc.Name} does not look up until you are close, and then is not surprised. Their eyes go to your collarbone first, the way the ring-keepers' do, and then, unlike theirs, come up to your face.");
            if (!World.Facts.Exists("met", npc.Id))
            {
                World.Facts.Add("met", npc.Id, World.SettlementName,
                    $"{npc.Name}, the hermit at the fire in the wilds, has spoken with the bearer.");
                Log.Add(Turn, "\"I keep a fire, not a door. Sit if you like; the kettle is just boiled.\"");
            }
        }
        else if (npc.Kind == NpcKind.Unbinder)
        {
            Log.Add(Turn, $"{npc.Name} the {npc.Role} looks up from their work, unsurprised.");
            if (!World.Facts.Exists("met", npc.Id))
            {
                World.Facts.Add("met", npc.Id, World.SettlementName,
                    $"{npc.Name}, the wandering {npc.Role}, has spoken with the bearer.");
                Log.Add(Turn, "\"Sit, if you like. The fire is small, but it is honest.\"");
            }
            // First-meeting cycle is recorded before the trigger fires, so recognition
            // content can gate on "met one in an EARLIER world" (D-034).
            if (Player.FirstUnbinderCycle == 0) Player.FirstUnbinderCycle = Cycle;
        }
        else
        {
            Log.Add(Turn, $"{npc.Name}, {npc.Role} of {World.SettlementName}, turns to you.");
            if (!World.Facts.Exists("met", npc.Id))
            {
                World.Facts.Add("met", npc.Id, World.SettlementName,
                    $"{npc.Name}, {npc.Role} of {World.SettlementName}, has spoken with the bearer.");
                Log.Add(Turn, $"\"A stranger, then. Word travels slower than trouble here.\"");
            }
        }
        _storylets.TryFire(this, StoryletTrigger.Talk);
        return true;
    }

    private List<(string Label, string Answer)> BuildTopics(Npc npc)
    {
        var topics = new List<(string, string)>();

        if (World.Facts.Find("settlement", World.SettlementName) is { } stead)
            topics.Add(("The stead", $"{stead.Detail} \"We hold on. That is the whole craft of it.\""));

        if (CampCleared)
            topics.Add(("The quiet nights", $"\"The raids are ended, and everyone knows whose doing that was. {World.SettlementName} sleeps whole again.\""));
        else if (World.Facts.OfType("grievance").FirstOrDefault() is { } grievance)
            topics.Add(("The goblin raids", $"\"{grievance.Detail} We have fed them to keep the peace. It has not bought much peace.\""));

        if (World.Facts.Find("rest_point", "shrine") is { } shrine)
            topics.Add(("The shrine", $"{shrine.Detail} \"Old past knowing. We keep it swept all the same.\""));

        if (World.Facts.Find("site", "waygate") is { } gate)
            topics.Add(("The black arch", gate.Detail + (CampCleared
                ? " \"They say it hums now. No one goes near to check.\""
                : " \"Shut as long as any here remember. Best left so.\"")));

        if (World.BarrowSite is { } barrowSite && World.Facts.Find("site", "barrow") is { } barrow)
            topics.Add(("The long mound", barrow.Detail + (barrowSite.Cleared
                ? " \"Quiet up there now, first time in living memory. Whoever settled them, the stead owes a debt it cannot name.\""
                : " \"None go up. Of late there are lights along the mound at night, and the dogs will not face that way.\"")));

        if (World.HollowSite is { } hollowSite && World.Facts.Find("site", "hollow") is { } hollowFact)
            topics.Add(("The stone ring", hollowFact.Detail + (hollowSite.Cleared
                ? " \"The fire up there is out. First time in anyone's memory. We are not sure we are glad.\""
                : " \"Leave it be, stranger. Whoever keeps that fire has kept it longer than the stead has stood.\"")));

        if (World.Facts.Find("wanderer", "npc_unbinder") is { } wanderer)
            topics.Add(("The wanderer", $"\"{wanderer.Detail} Not the first such to pass through, if the old folk are believed.\""));

        if (World.Facts.OfType("echo").FirstOrDefault() is { } echo)
            topics.Add(("Old songs", $"\"There is a new one, though none can say who taught it. {echo.Detail}\""));

        return topics;
    }

    /// <summary>
    /// The Unbinder's own topics (D-034). The "stead" answer is their worldview
    /// surfaced plainly: it stays coherent and unyielding in every world, every cycle
    /// (the arc's never-disproved rule). The last topic unlocks via the recognition
    /// storylet's fact write.
    /// </summary>
    private List<(string Label, string Answer)> BuildUnbinderTopics(Npc npc)
    {
        var topics = new List<(string, string)>
        {
            ("Their trade", $"\"{UnbinderGuises.WorkLine(npc.Role)} And when the work under the work asks for it, I loosen what is bound too tight. People, mostly.\""),
            ("The stead", $"\"Good people in {World.SettlementName}. They will end, and their stead will end, and that is not a sad thing. Endings are what let anything matter at all.\""),
        };

        if (World.Facts.Exists("noticed", "unbinder"))
            topics.Add(("The one before", "\"The one before. Hm. Roads repeat their travelers, or travelers their roads. When you can tell those two apart, come and ask me again.\""));

        // Reveal tier 1 (D-037): unlocked by the confrontation, restated on demand,
        // and never advanced by asking twice (trust and escalation, not a clock).
        if (Player.UnbinderRevealTier >= 1)
            topics.Add(("The long road", "\"How long have I walked? Longer than this world has had a name. You knew that before you asked. What you want to know is whether it can be borne. It can. Ask your keeper what it counts, and then ask it again.\""));

        // Reveal tier 2 (D-038): the refusal, restated on demand, never softened.
        if (Player.UnbinderRevealTier >= 2)
            topics.Add(("The refusal", "\"Would I do it again? Every dawn of every world. You want to know if the knife is clean. It is the cleanest thing I own. At the threshold it will be yours to take or wave away, and either answer will be yours. That is the entire point of me.\""));

        return topics;
    }

    /// <summary>
    /// The severed hermit's topics (D-038): the agency model's worldview, coherent
    /// and unyielding in every world (the arc's never-disproved rule wears a second
    /// face here). The last topic unlocks once their side has been heard.
    /// </summary>
    private List<(string Label, string Answer)> BuildSeveredTopics()
    {
        var topics = new List<(string, string)>
        {
            ("The fire", "\"It is a small fire because I want a small fire. The stead lets me be, and I let the stead be, and between us we have the tidiest treaty in this world.\""),
            ("Their peace", "\"You are looking for the crack in it. There is none. I am spending myself the way a person spends a purse they finally own: slowly, on mornings like this one. I recommend it to everyone who can bear the price, and to no one who cannot.\""),
        };

        if (Player.SeveredPeaceHeard)
            topics.Add(("The cutting", "\"A courteous stranger held the knife. You have met them; one of them is always about. They asked me three times if I was certain. I was rude, by the third. I have regretted the rudeness and nothing else.\""));

        return topics;
    }

    /// <summary>
    /// The stead's trade surface (D-036): each seller offers what their role would
    /// actually have. Purchases are talk-menu entries, not a separate mode, and the
    /// menu stays open so buying twice is two key presses.
    /// </summary>
    private List<(TradeGood, string)> BuildOffers(Npc npc)
    {
        var offers = new List<(TradeGood, string)>();
        if (npc.Id == "npc_steadholder")
            offers.Add((TradeGood.Ration, $"Buy a ration ({RationPrice} coin)"));
        if (npc.Id == "npc_herbwife" && Player.WoundedTurns > 0)
            offers.Add((TradeGood.Mending, $"Have the wound dressed ({MendPrice} coin)"));
        return offers;
    }

    private void TryBuyRation()
    {
        int price = RationPrice;
        if (Player.Rations >= RationCap)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"You carry all a walking body can. Eat some of it first.\"");
            return;
        }
        if (Player.Coin < price)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"That is {price} coin, and you hold {Player.Coin}. The larder is not a charity, stranger.\"");
            return;
        }

        Player.Coin -= price;
        Player.Rations++;
        Log.Add(Turn, $"Bread, hard cheese, a fist of dried plums, wrapped in waxed cloth. ({Player.Rations} carried)", LogTone.Reward);
        if (price > 4)
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Dear, I know. Prices are what the fields let them be, of late.\"");
    }

    private void TryBuyMending()
    {
        if (Player.WoundedTurns == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"You are whole. Come back when you are not; most do.\"");
            return;
        }
        int price = MendPrice;
        if (Player.Coin < price)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Herbs cost, boiled linen costs. {price} coin, or let time do it for nothing.\"");
            return;
        }

        Player.Coin -= price;
        Player.WoundedTurns = 0;
        _offers.RemoveAll(o => o.Good == TradeGood.Mending);
        Log.Add(Turn, $"{TalkNpc!.Name} unwinds the old dressing, packs the wound with something that smells of thyme, and binds it properly.", LogTone.Reward);
        Log.Add(Turn, "The wound's weight lifts. You are whole again.", LogTone.Info);
        Log.Add(Turn, "\"Mended by another's hands. That is allowed. That is what steads are for.\"", LogTone.Aegis);
    }

    private const string UnbinderFarewell = "Nothing needs to be counted. Walk well.";

    private void HandleTalkMenuKey(char key)
    {
        if (key >= '1' && key <= '0' + _topics.Count)
        {
            var (label, answer) = _topics[key - '1'];
            Log.Add(Turn, $"You ask about {label.ToLowerInvariant()}.");
            Log.Add(Turn, $"{TalkNpc!.Name}: {answer}");
            return;
        }

        if (TalkNpc!.Kind == NpcKind.Villager
            && key > '0' + _topics.Count && key <= '0' + _topics.Count + _offers.Count)
        {
            var (good, _) = _offers[key - '1' - _topics.Count];
            if (good == TradeGood.Ration) TryBuyRation(); else TryBuyMending();
            return;
        }

        if (TalkNpc!.Kind == NpcKind.Unbinder && key == '1' + _topics.Count)
        {
            InTalkMenu = false;
            if (UnbindingsLeft == 0)
            {
                Log.Add(Turn, $"{TalkNpc.Name} shakes their head. \"Not again in this world. What I loosen must settle before I loosen more.\"");
                Log.Add(Turn, $"\"{UnbinderFarewell}\" They return to their work.");
                TalkNpc = null;
                return;
            }
            InUnbindMenu = true;
            Log.Add(Turn, $"{TalkNpc.Name} unrolls a cloth of worn tools that are not for a {TalkNpc.Role}'s work.");
            Log.Add(Turn, "\"Show me where the shape grips too tight. What was spent comes back; nothing is lost.\"");
            return;
        }

        InTalkMenu = false;
        if (TalkNpc.Kind == NpcKind.Unbinder)
            Log.Add(Turn, $"\"{UnbinderFarewell}\" {TalkNpc.Name} returns to their work.");
        else if (TalkNpc.Kind == NpcKind.Severed)
            Log.Add(Turn, $"\"All is counted, little shield. I mean the first part kindly and the second precisely.\" {TalkNpc.Name} turns back to the kettle.");
        else
            Log.Add(Turn, $"You part ways with {TalkNpc.Name}.");
        TalkNpc = null;
    }

    private void HandleUnbindMenuKey(char key)
    {
        if (key >= '1' && key <= '7')
        {
            TryUnbind((Attr)(key - '1'));
            return;
        }

        InUnbindMenu = false;
        Log.Add(Turn, $"\"{UnbinderFarewell}\" {TalkNpc!.Name} rolls the tools away.");
        TalkNpc = null;
    }

    /// <summary>
    /// Attribute respec (D-016, D-034): loosen one raise, refund exactly what
    /// re-buying it will cost. Lossless by construction; the scarcity is the
    /// per-world cap, not a price.
    /// </summary>
    private void TryUnbind(Attr attr)
    {
        if (UnbindingsLeft == 0)
        {
            Log.Add(Turn, "\"Not again in this world. What I loosen must settle before I loosen more.\"");
            return;
        }
        if (Player.Attributes[attr] <= AttributeSet.Baseline)
        {
            Log.Add(Turn, $"\"There is nothing bound in your {AttributeSet.NameOf(attr)} but what you were born with. That, I do not touch.\"");
            return;
        }

        int refund = UnbindRefund;
        Player.Attributes[attr] = Player.Attributes[attr] - 1;
        Player.Essence += refund;
        UnbindingsLeft--;
        Player.Unbindings++;
        if (attr == Attr.Vigor)
        {
            Player.Hp = Math.Min(Player.Hp, Player.EffectiveMaxHp);
            Player.Stamina = Math.Min(Player.Stamina, Player.MaxStamina);
        }
        Log.Add(Turn, $"Their fingers work at something above your collarbone that you cannot see. {AttributeSet.NameOf(attr)} eases to {Player.Attributes[attr]}; {refund} essence returns to you.", LogTone.Reward);
        if (Player.Unbindings == 1)
            Log.Add(Turn, "\"Strange, to be loosened. I held that shape with care. ...I will hold the new one the same.\"", LogTone.Aegis);
    }

    /// <summary>
    /// Eating a ration (D-036): the only healing that works away from the shrine.
    /// Takes a turn, so a mid-fight bite is a real gamble; monsters still act.
    /// </summary>
    private bool DoEat()
    {
        if (Player.Rations == 0)
        {
            Log.Add(Turn, "You carry nothing to eat.");
            return false;
        }
        if (Player.Hp >= Player.EffectiveMaxHp && Player.Stamina >= Player.MaxStamina)
        {
            Log.Add(Turn, "You are neither hurt nor winded; the ration keeps.");
            return false;
        }

        Player.Rations--;
        Player.Hp = Math.Min(Player.EffectiveMaxHp, Player.Hp + 6);
        Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 3);
        Log.Add(Turn, $"You eat, quickly, watching the shadows. Warmth comes back to your hands. ({Player.Rations} left)", LogTone.Info);
        return true;
    }

    /// <summary>Global rising cost per raise (D-014's Essence economy, Souls-style curve).</summary>
    public int NextRaiseCost => 10 + 5 * Player.Attributes.TotalRaises;

    private bool DoRest()
    {
        if (!(Mode == MapMode.Overworld && CurrentMap[Player.Pos] == Terrain.Shrine))
        {
            Log.Add(Turn, "You may only rest where the Aegis anchors.");
            return false;
        }

        Player.Hp = Player.EffectiveMaxHp;
        Player.Stamina = Player.MaxStamina;
        InShrineMenu = true;
        Log.Add(Turn, "You rest at the shrine. Warmth returns to you.", LogTone.Info);
        Log.Add(Turn, "\"Be still. Let me count what you have earned.\"", LogTone.Aegis);
        _storylets.TryFire(this, StoryletTrigger.Rest);
        return true;
    }

    private void HandleShrineMenuKey(char key)
    {
        if (key >= '1' && key <= '7')
        {
            TryRaise((Attr)(key - '1'));
            return;
        }

        InShrineMenu = false;
        Log.Add(Turn, "You rise from the shrine.");
    }

    private void TryRaise(Attr attr)
    {
        int cost = NextRaiseCost;
        if (Player.Essence < cost)
        {
            Log.Add(Turn, $"Raising {AttributeSet.NameOf(attr)} asks {cost} essence; you hold {Player.Essence}.");
            return;
        }

        Player.Essence -= cost;
        Player.Attributes[attr] = Player.Attributes[attr] + 1;
        if (attr == Attr.Vigor)
        {
            // Growing tougher heals the difference immediately.
            Player.Hp = Math.Min(Player.Hp + 2, Player.EffectiveMaxHp);
            Player.Stamina = Math.Min(Player.Stamina + 1, Player.MaxStamina);
        }
        Log.Add(Turn, $"\"{cost} essence, spent well.\" {AttributeSet.NameOf(attr)} is now {Player.Attributes[attr]}.", LogTone.Aegis);
    }

    private bool AttackMonster(Monster target)
    {
        const int staminaCost = 3;
        int damage;
        if (Player.Stamina >= staminaCost)
        {
            Player.Stamina -= staminaCost;
            damage = _combatRng.Range(2, 5) + Player.MeleeBonus;
        }
        else
        {
            // Winded: the swing still lands, but weakly. Stamina is the combat economy (D-004).
            damage = 1;
            Log.Add(Turn, "You are winded; the blow is feeble.", LogTone.Combat);
        }

        target.Hp -= damage;
        if (target.Alive)
        {
            Log.Add(Turn, $"You strike the {target.Name} for {damage}.", LogTone.Combat);
        }
        else
        {
            // The dead hold little a living hand would spend, but they are dense
            // with essence; a severed one is nothing else at all.
            int coin = target.Kind switch
            {
                MonsterKind.Wight => _combatRng.Range(0, 3),
                MonsterKind.Severed => 0,
                _ => _combatRng.Range(2, 7),
            };
            int essence = target.Kind switch
            {
                MonsterKind.Wight => 8,
                MonsterKind.Severed => 15,
                _ => 5,
            };
            Player.Coin += coin;
            Player.Essence += essence;
            Log.Add(Turn, target.Kind switch
            {
                MonsterKind.Wight => $"The wight comes apart into grave-dust and quiet. You take {coin} coin and {essence} essence.",
                MonsterKind.Severed => $"The severed one comes apart slowly, almost gratefully. What it held pours into the Aegis: {essence} essence, and no coin at all.",
                _ => $"The {target.Name} falls. You take {coin} coin and {essence} essence.",
            }, LogTone.Reward);
            CheckSiteCleared(CurrentSite!);
        }
        return true;
    }

    private void CheckSiteCleared(Site site)
    {
        if (site.Cleared || Monsters.Any(m => m.Alive && m.SiteId == site.Id)) return;
        site.Cleared = true;

        if (site.Kind == SiteKind.GoblinCamp)
        {
            World.Facts.Add("deed", "camp_cleared", World.SettlementName,
                $"The goblin cave was emptied. {World.SettlementName}'s stores are safe.");
            Log.Add(Turn, "The camp falls silent. The raids on " + World.SettlementName + " are ended.", LogTone.Reward);
            Log.Add(Turn, "\"A deed with weight. It is counted.\"", LogTone.Aegis);
            Log.Add(Turn, $"\"And far to the {Compass(World.CampPos, World.GatePos)} of this cave, something old has unlocked. I feel it the way you feel a door open in a dark house.\"", LogTone.Aegis);
        }
        else if (site.Kind == SiteKind.Barrow)
        {
            World.Facts.Add("deed", "barrow_stilled", World.SettlementName,
                $"The barrow's dead were put to rest. The lights on the mound above {World.SettlementName} have gone out.");
            Log.Add(Turn, "The passage is still. Whatever the dead here were set to hold, no one is holding it now.", LogTone.Reward);
            Log.Add(Turn, "\"They were given a task and no release. I remember the shape of that arrangement. It is counted, bearer, twice over.\"", LogTone.Aegis);
        }
        else
        {
            World.Facts.Add("deed", "severed_laid", World.SettlementName,
                "The keeper of the stone ring was laid down at last. The fire in the ring is out.");
            Log.Add(Turn, "The ring is empty. What leaves it is not quite silence: more like a debt being read out.", LogTone.Reward);
            Log.Add(Turn, "\"...I know this one. I do not want to know this one. Give me a moment, bearer.\"", LogTone.Aegis);
        }
        _storylets.TryFire(this, StoryletTrigger.DeedWritten);
    }

    private void AdvanceTurn()
    {
        Turn++;

        if (Mode == MapMode.Site)
            foreach (var monster in Monsters.Where(m => m.Alive && m.SiteId == CurrentSite!.Id))
                ActMonster(monster);

        if (Player.WoundedTurns > 0)
        {
            Player.WoundedTurns--;
            if (Player.WoundedTurns == 0)
            {
                Log.Add(Turn, "The wound's weight lifts. You are whole again.", LogTone.Info);
                Player.Hp = Math.Min(Player.Hp, Player.EffectiveMaxHp);
            }
        }

        if (Mode == MapMode.Overworld && Player.Hp > 0)
            _storylets.TryFire(this, StoryletTrigger.AmbientTurn);

        if (Player.Hp <= 0) HandleDeath();
    }

    private void ActMonster(Monster monster)
    {
        // Resolve a telegraphed intent first: it lands on the cell, not the player.
        if (monster.Intent is { } intent)
        {
            intent.TurnsUntilResolve--;
            if (intent.TurnsUntilResolve <= 0)
            {
                monster.Intent = null;
                if (Player.Pos == intent.TargetCell)
                {
                    int damage = intent.Kind switch
                    {
                        IntentKind.BarrowBlade => _combatRng.Range(5, 9),
                        IntentKind.SunderingCut => _combatRng.Range(7, 11),
                        _ => _combatRng.Range(4, 7),
                    };
                    Player.Hp -= damage;
                    Log.Add(Turn, intent.Kind switch
                    {
                        IntentKind.BarrowBlade => $"The wight's barrow blade opens you for {damage}!",
                        IntentKind.SunderingCut => $"The severed one's cut goes through guard, cloth, and certainty for {damage}!",
                        _ => $"The {monster.Name}'s crushing blow lands for {damage}!",
                    }, LogTone.Danger);
                }
                else
                {
                    Log.Add(Turn, intent.Kind switch
                    {
                        IntentKind.BarrowBlade => "The wight's bronze blade shears cold, empty air.",
                        IntentKind.SunderingCut => "The severed one's cut parts the air where you stood, without hurry and without regret.",
                        _ => $"The {monster.Name}'s crushing blow splinters empty stone.",
                    }, LogTone.Combat);
                }
            }
            return;
        }

        if (monster.Kind == MonsterKind.Wight) { ActWight(monster); return; }
        if (monster.Kind == MonsterKind.Severed) { ActSevered(monster); return; }

        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist == 1)
        {
            if (_combatRng.Chance(0.45))
            {
                monster.Intent = new Intent { Kind = IntentKind.CrushingBlow, TargetCell = Player.Pos };
                Log.Add(Turn, $"The {monster.Name} heaves its club high, eyes fixed on where you stand!", LogTone.Danger);
            }
            else
            {
                if (_combatRng.Chance(Player.DodgeChance))
                {
                    Log.Add(Turn, $"The {monster.Name}'s bite finds only air.", LogTone.Combat);
                }
                else
                {
                    int damage = _combatRng.Range(1, 3);
                    Player.Hp -= damage;
                    Log.Add(Turn, $"The {monster.Name} bites you for {damage}.", LogTone.Combat);
                }
            }
            return;
        }

        if (dist <= 8) StepToward(monster);
    }

    /// <summary>
    /// The barrow family (D-033): grave-slow (a step only every other turn, so they can
    /// be kited), a cold grasp that stiffens stamina, and a heavier telegraphed blade.
    /// </summary>
    private void ActWight(Monster monster)
    {
        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist == 1)
        {
            if (_combatRng.Chance(0.35))
            {
                monster.Intent = new Intent { Kind = IntentKind.BarrowBlade, TargetCell = Player.Pos };
                Log.Add(Turn, "The wight draws back a blade of black bronze, patient as stone!", LogTone.Danger);
            }
            else if (_combatRng.Chance(Player.DodgeChance))
            {
                Log.Add(Turn, "The wight's grasp closes on air.", LogTone.Combat);
            }
            else
            {
                int damage = _combatRng.Range(2, 5);
                Player.Hp -= damage;
                Player.Stamina = Math.Max(0, Player.Stamina - 2);
                Log.Add(Turn, $"The wight's grasp burns cold for {damage}. Your limbs stiffen.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 8 && Turn % 2 == 0) StepBfsToward(monster);
    }

    /// <summary>
    /// The severed one (D-037): a former bearer, unraveling. Walks at full speed
    /// with a walker's sureness, telegraphs a heavy cut, and its bare touch thins
    /// essence: what it takes from you is what it is starving for.
    /// </summary>
    private void ActSevered(Monster monster)
    {
        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist == 1)
        {
            if (_combatRng.Chance(0.35))
            {
                monster.Intent = new Intent { Kind = IntentKind.SunderingCut, TargetCell = Player.Pos };
                Log.Add(Turn, "The severed one draws back its arm, unhurried, certain of you!", LogTone.Danger);
            }
            else if (_combatRng.Chance(Player.DodgeChance))
            {
                Log.Add(Turn, "The severed one's reach closes on the place you were.", LogTone.Combat);
            }
            else
            {
                int damage = _combatRng.Range(2, 6);
                Player.Hp -= damage;
                if (Player.Essence > 0) Player.Essence--;
                Log.Add(Turn, $"The severed one's touch takes {damage}, and something thinner than blood with it.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 8) StepBfsToward(monster);
    }

    /// <summary>
    /// Proper pathing (BFS, cardinal steps) for the dead and the severed: they have
    /// walked their halls for an age and do not fumble at their own doorways.
    /// Goblins keep their greedy stumble.
    /// </summary>
    private void StepBfsToward(Monster monster)
    {
        var map = CurrentSite!.Map;
        var from = monster.Pos;
        var cameFrom = new Dictionary<Pos, Pos> { [from] = from };
        var queue = new Queue<Pos>();
        queue.Enqueue(from);
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if (p == Player.Pos) break;
            foreach (var (dx, dy) in Directions.Cardinal)
            {
                var next = p.Plus(dx, dy);
                bool open = next == Player.Pos
                    || (map.Walkable(next)
                        && !Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == next));
                if (open && !cameFrom.ContainsKey(next))
                {
                    cameFrom[next] = p;
                    queue.Enqueue(next);
                }
            }
        }
        if (!cameFrom.ContainsKey(Player.Pos)) return;

        var step = Player.Pos;
        while (cameFrom[step] != from) step = cameFrom[step];
        if (step != Player.Pos) monster.Pos = step;
    }

    private void StepToward(Monster monster)
    {
        var map = CurrentSite!.Map;
        var best = monster.Pos;
        int bestDist = monster.Pos.Manhattan(Player.Pos);
        foreach (var (dx, dy) in Directions.All8)
        {
            var next = monster.Pos.Plus(dx, dy);
            if (!map.Walkable(next)) continue;
            if (next == Player.Pos) continue;
            if (Monsters.Any(m => m.Alive && m != monster && m.Pos == next)) continue;
            int d = next.Manhattan(Player.Pos);
            if (d < bestDist) { bestDist = d; best = next; }
        }
        monster.Pos = best;
    }

    private void HandleDeath()
    {
        Player.Deaths++;
        InShrineMenu = false;
        InTalkMenu = false;
        InUnbindMenu = false;
        TalkNpc = null;

        bool forfeited = Remnant is not null;
        if (forfeited)
            Log.Add(Turn, $"\"{AegisVoice.ForfeitLine}\"", LogTone.Aegis);

        int droppedCoin = Player.Coin;
        int droppedEssence = Player.Essence;
        Remnant = droppedCoin + droppedEssence > 0
            ? new Remnant { MapId = CurrentMapId, Pos = Player.Pos, Coin = droppedCoin, Essence = droppedEssence }
            : null;
        Player.Coin = 0;
        Player.Essence = 0;

        foreach (var monster in Monsters.Where(m => m.Alive))
            monster.Intent = null;

        Log.Add(Turn, "You fall.", LogTone.Danger);
        Log.Add(Turn, $"\"{AegisVoice.DeathLine(Player.Deaths)}\"", LogTone.Aegis);

        Mode = MapMode.Overworld;
        CurrentSite = null;
        Player.Pos = World.ShrinePos;
        Player.WoundedTurns = 80;
        Player.Hp = Player.EffectiveMaxHp;
        Player.Stamina = Player.MaxStamina;

        Log.Add(Turn, $"You wake at the shrine, wounded. The Aegis is spent; it will recover in time.", LogTone.Info);
        if (Remnant is not null)
            Log.Add(Turn, $"What you carried lies where you fell. One chance to reclaim it.", LogTone.Danger);
    }

    // Test hooks: deterministic surgery for unit tests, never used by frontends.
    internal void Debug_SetPlayerPos(Pos p) => Player.Pos = p;
    internal void Debug_SetMode(MapMode mode)
    {
        Mode = mode;
        CurrentSite = mode == MapMode.Site ? World.CampSite : null;
    }
    internal void Debug_HurtPlayer(int damage) => Player.Hp -= damage;
    internal void Debug_ForceDeathCheck() { if (Player.Hp <= 0) HandleDeath(); }
    internal void Debug_ClearCamp() => Debug_ClearSite(SiteKind.GoblinCamp);
    internal void Debug_ClearSite(SiteKind kind)
    {
        var site = World.Sites.First(s => s.Kind == kind);
        foreach (var monster in Monsters.Where(m => m.SiteId == site.Id)) monster.Hp = 0;
        CheckSiteCleared(site);
    }

    public Snapshot TakeSnapshot() => new(
        Seed: MasterSeed,
        Cycle: Cycle,
        Tier: World.Tier,
        Turn: Turn,
        Running: Running,
        Mode: Mode.ToString(),
        WorldName: World.Name,
        SettlementName: World.SettlementName,
        X: Player.Pos.X,
        Y: Player.Pos.Y,
        ShrineX: World.ShrinePos.X,
        ShrineY: World.ShrinePos.Y,
        CampX: World.CampPos.X,
        CampY: World.CampPos.Y,
        GateX: World.GatePos.X,
        GateY: World.GatePos.Y,
        BarrowX: World.BarrowSite?.OverworldPos.X ?? -1,
        BarrowY: World.BarrowSite?.OverworldPos.Y ?? -1,
        BarrowCleared: World.BarrowSite?.Cleared ?? false,
        HollowX: World.HollowSite?.OverworldPos.X ?? -1,
        HollowY: World.HollowSite?.OverworldPos.Y ?? -1,
        HollowCleared: World.HollowSite?.Cleared ?? false,
        SeveredNpcX: World.SeveredNpc?.Pos.X ?? -1,
        SeveredNpcY: World.SeveredNpc?.Pos.Y ?? -1,
        ArcProgress: string.Join(",", new[]
        {
            Player.SeveredTruthHeard ? "truth" : null,
            Player.CrossingGuiltHeard ? "guilt" : null,
            Player.VisionSeen ? "vision" : null,
            Player.LedgerHeard ? "ledger" : null,
            Player.SeveredPeaceHeard ? "peace" : null,
            Player.SeveredCostSeen ? "cost" : null,
            Player.UnbinderRevealTier >= 1 ? $"tier{Player.UnbinderRevealTier}" : null,
            Player.CommissionHeard ? "commission" : null,
        }.Where(s => s is not null)),
        CurrentSite: CurrentSite?.Id ?? "",
        UnbinderX: World.Unbinder.Pos.X,
        UnbinderY: World.Unbinder.Pos.Y,
        UnbindingsLeft: UnbindingsLeft,
        StoryTemplate: World.Facts.OfType("story").FirstOrDefault()?.Subject ?? "",
        Hp: Player.Hp,
        MaxHp: Player.EffectiveMaxHp,
        Stamina: Player.Stamina,
        MaxStamina: Player.MaxStamina,
        Coin: Player.Coin,
        Essence: Player.Essence,
        Legend: Player.Legend,
        Rations: Player.Rations,
        RationPrice: RationPrice,
        MendPrice: Player.WoundedTurns > 0 ? MendPrice : 0,
        Might: Player.Attributes[Attr.Might],
        Grace: Player.Attributes[Attr.Grace],
        Vigor: Player.Attributes[Attr.Vigor],
        Wits: Player.Attributes[Attr.Wits],
        Mind: Player.Attributes[Attr.Mind],
        Will: Player.Attributes[Attr.Will],
        Presence: Player.Attributes[Attr.Presence],
        NextRaiseCost: NextRaiseCost,
        InShrineMenu: InShrineMenu,
        InTalkMenu: InTalkMenu,
        InUnbindMenu: InUnbindMenu,
        TalkNpc: TalkNpc?.Name ?? "",
        WoundedTurns: Player.WoundedTurns,
        Deaths: Player.Deaths,
        MonstersAlive: Monsters.Count(m => m.Alive),
        StoryletsFired: StoryletsFired,
        CampCleared: CampCleared,
        RemnantExists: Remnant is not null,
        RemnantMap: Remnant?.MapId ?? "",
        RemnantX: Remnant?.Pos.X ?? 0,
        RemnantY: Remnant?.Pos.Y ?? 0,
        RemnantCoin: Remnant?.Coin ?? 0,
        RemnantEssence: Remnant?.Essence ?? 0,
        RecentMessages: Log.Recent(8).Select(e => $"[T{e.Turn}] {e.Text}").ToArray());
}

/// <summary>Flat, serialization-friendly view of game state for pilot/sim consumers.</summary>
public sealed record Snapshot(
    ulong Seed,
    int Cycle,
    int Tier,
    int Turn,
    bool Running,
    string Mode,
    string WorldName,
    string SettlementName,
    int X,
    int Y,
    int ShrineX,
    int ShrineY,
    int CampX,
    int CampY,
    int GateX,
    int GateY,
    int BarrowX,
    int BarrowY,
    bool BarrowCleared,
    int HollowX,
    int HollowY,
    bool HollowCleared,
    int SeveredNpcX,
    int SeveredNpcY,
    string ArcProgress,
    string CurrentSite,
    int UnbinderX,
    int UnbinderY,
    int UnbindingsLeft,
    string StoryTemplate,
    int Hp,
    int MaxHp,
    int Stamina,
    int MaxStamina,
    int Coin,
    int Essence,
    int Legend,
    int Rations,
    int RationPrice,
    int MendPrice,
    int Might,
    int Grace,
    int Vigor,
    int Wits,
    int Mind,
    int Will,
    int Presence,
    int NextRaiseCost,
    bool InShrineMenu,
    bool InTalkMenu,
    bool InUnbindMenu,
    string TalkNpc,
    int WoundedTurns,
    int Deaths,
    int MonstersAlive,
    int StoryletsFired,
    bool CampCleared,
    bool RemnantExists,
    string RemnantMap,
    int RemnantX,
    int RemnantY,
    int RemnantCoin,
    int RemnantEssence,
    string[] RecentMessages);

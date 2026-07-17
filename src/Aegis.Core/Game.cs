namespace Aegis.Core;

public enum MapMode { Overworld, Site }

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
    public bool ChestLooted { get; private set; }
    public bool CampCleared { get; private set; }
    public bool InShrineMenu { get; private set; }
    public bool InTalkMenu { get; private set; }
    public Npc? TalkNpc { get; private set; }

    /// <summary>The current conversation's topics, computed live from the fact graph at open.</summary>
    public IReadOnlyList<(string Label, string Answer)> Topics => _topics;
    private readonly List<(string Label, string Answer)> _topics = [];

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
        // Tier hardens each goblin as a generation-time stat, not a live multiplier.
        int hp = 8 + 2 * (World.Tier - 1);
        foreach (var spawn in World.GoblinSpawns)
            Monsters.Add(new Monster { Kind = MonsterKind.Goblin, Pos = spawn, Hp = hp });
    }

    internal static string Compass(Pos from, Pos to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        string ns = dy <= -Math.Abs(dx) / 2 ? "north" : dy >= Math.Abs(dx) / 2 ? "south" : "";
        string ew = dx <= -Math.Abs(dy) / 2 ? "west" : dx >= Math.Abs(dy) / 2 ? "east" : "";
        string dir = ns + ew;
        return dir.Length == 0 ? "near" : dir;
    }

    public GameMap CurrentMap => Mode == MapMode.Overworld ? World.Overworld : World.Camp;

    private string CurrentMapId => CurrentMap.Id;

    public IEnumerable<Monster> LiveMonstersHere =>
        Mode == MapMode.Site ? Monsters.Where(m => m.Alive) : [];

    /// <summary>
    /// Applies one key press: the single entry point every frontend and the save
    /// journal share. Menu keys are routed before command mapping.
    /// </summary>
    public void ApplyKey(char key)
    {
        if (!Running) return;

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
            var blocker = Monsters.FirstOrDefault(m => m.Alive && m.Pos == target);
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
        }
        else
        {
            if (Remnant is not null && Remnant.MapId == CurrentMapId && Remnant.Pos == p)
                Log.Add(Turn, "Your remnant lies here: what you dropped when you fell. Press g to reclaim it.", LogTone.Reward);
            else if (!ChestLooted && p == World.ChestPos)
                Log.Add(Turn, "A battered strongbox sits here. Press g to open it.", LogTone.Reward);
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
        if (Mode == MapMode.Overworld && Player.Pos == World.CampPos)
        {
            Mode = MapMode.Site;
            Player.Pos = World.CampEntryPos;
            Log.Add(Turn, "You descend into the goblin cave. The dark smells of smoke and old meat.", LogTone.Danger);
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
        ChestLooted = false;
        CampCleared = false;
        InShrineMenu = false;
        InTalkMenu = false;
        TalkNpc = null;

        Mode = MapMode.Overworld;
        Player.Pos = World.ShrinePos;
        Player.WoundedTurns = 0;
        Player.Hp = Player.MaxHp;
        Player.Stamina = Player.MaxStamina;

        World.Facts.Add("echo", "deed", prevSettlement,
            $"In a world called {prevWorld}, the bearer emptied a goblin cave, and {prevSettlement} slept safe.");

        Log.Add(Turn, $"You step through the arch, and {prevWorld} folds shut behind you like a closed book.", LogTone.Danger);
        if (Cycle == 2)
        {
            Log.Add(Turn, $"\"{AegisVoice.FirstCrossingLine1}\"", LogTone.Aegis);
            Log.Add(Turn, $"\"{AegisVoice.FirstCrossingLine2}\"", LogTone.Aegis);
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
        _storylets.TryFire(this, StoryletTrigger.Arrival);
    }

    private bool DoExit()
    {
        if (Mode == MapMode.Site && World.Camp[Player.Pos] == Terrain.ExitLadder)
        {
            Mode = MapMode.Overworld;
            Player.Pos = World.CampPos;
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

        if (Mode == MapMode.Site && !ChestLooted && Player.Pos == World.ChestPos)
        {
            int coin = _combatRng.Range(10, 21);
            Player.Coin += coin;
            ChestLooted = true;
            Log.Add(Turn, $"The strongbox yields {coin} coin.", LogTone.Reward);
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
        _topics.AddRange(BuildTopics(npc));

        Log.Add(Turn, $"{npc.Name}, {npc.Role} of {World.SettlementName}, turns to you.");
        if (!World.Facts.Exists("met", npc.Id))
        {
            World.Facts.Add("met", npc.Id, World.SettlementName,
                $"{npc.Name}, {npc.Role} of {World.SettlementName}, has spoken with the bearer.");
            Log.Add(Turn, $"\"A stranger, then. Word travels slower than trouble here.\"");
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

        if (World.Facts.OfType("echo").FirstOrDefault() is { } echo)
            topics.Add(("Old songs", $"\"There is a new one, though none can say who taught it. {echo.Detail}\""));

        return topics;
    }

    private void HandleTalkMenuKey(char key)
    {
        if (key >= '1' && key <= '0' + _topics.Count)
        {
            var (label, answer) = _topics[key - '1'];
            Log.Add(Turn, $"You ask about {label.ToLowerInvariant()}.");
            Log.Add(Turn, $"{TalkNpc!.Name}: {answer}");
            return;
        }

        InTalkMenu = false;
        Log.Add(Turn, $"You part ways with {TalkNpc!.Name}.");
        TalkNpc = null;
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
            int coin = _combatRng.Range(2, 7);
            const int essence = 5;
            Player.Coin += coin;
            Player.Essence += essence;
            Log.Add(Turn, $"The {target.Name} falls. You take {coin} coin and {essence} essence.", LogTone.Reward);
            CheckCampCleared();
        }
        return true;
    }

    private void CheckCampCleared()
    {
        if (CampCleared || Monsters.Any(m => m.Alive)) return;
        CampCleared = true;
        World.Facts.Add("deed", "camp_cleared", World.SettlementName,
            $"The goblin cave was emptied. {World.SettlementName}'s stores are safe.");
        Log.Add(Turn, "The camp falls silent. The raids on " + World.SettlementName + " are ended.", LogTone.Reward);
        Log.Add(Turn, "\"A deed with weight. It is counted.\"", LogTone.Aegis);
        Log.Add(Turn, $"\"And far to the {Compass(World.CampPos, World.GatePos)} of this cave, something old has unlocked. I feel it the way you feel a door open in a dark house.\"", LogTone.Aegis);
        _storylets.TryFire(this, StoryletTrigger.DeedWritten);
    }

    private void AdvanceTurn()
    {
        Turn++;

        if (Mode == MapMode.Site)
            foreach (var monster in Monsters.Where(m => m.Alive))
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
                    int damage = _combatRng.Range(4, 7);
                    Player.Hp -= damage;
                    Log.Add(Turn, $"The {monster.Name}'s crushing blow lands for {damage}!", LogTone.Danger);
                }
                else
                {
                    Log.Add(Turn, $"The {monster.Name}'s crushing blow splinters empty stone.", LogTone.Combat);
                }
            }
            return;
        }

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

    private void StepToward(Monster monster)
    {
        var best = monster.Pos;
        int bestDist = monster.Pos.Manhattan(Player.Pos);
        foreach (var (dx, dy) in Directions.All8)
        {
            var next = monster.Pos.Plus(dx, dy);
            if (!World.Camp.Walkable(next)) continue;
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
    internal void Debug_SetMode(MapMode mode) => Mode = mode;
    internal void Debug_HurtPlayer(int damage) => Player.Hp -= damage;
    internal void Debug_ForceDeathCheck() { if (Player.Hp <= 0) HandleDeath(); }
    internal void Debug_ClearCamp()
    {
        foreach (var monster in Monsters) monster.Hp = 0;
        CheckCampCleared();
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
        Hp: Player.Hp,
        MaxHp: Player.EffectiveMaxHp,
        Stamina: Player.Stamina,
        MaxStamina: Player.MaxStamina,
        Coin: Player.Coin,
        Essence: Player.Essence,
        Legend: Player.Legend,
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
    int Hp,
    int MaxHp,
    int Stamina,
    int MaxStamina,
    int Coin,
    int Essence,
    int Legend,
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

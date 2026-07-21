namespace Aegis.Core;

public enum MapMode { Overworld, Site }

/// <summary>
/// What the stead sells (D-036): goods and services coin can become. <see cref="Trade"/>
/// is not itself a good but a shopfront: it opens a vendor's own menu (D-071), so a
/// seller can carry more than the shared talk-menu's nine digits will hold. <see cref="Hide"/>
/// runs the other way, coin the bearer's own hand earned from what the wilds gave (D-070).
/// </summary>
public enum TradeGood { Ration, Mending, Gear, Repair, Lesson, Pledge, Trade, Hide, Cook, Herb, Draught, Surgery, Brace, Laying, Beast, Stable, Bones, Round, Fence }

/// <summary>
/// The cart's counter (D-124): the road's prices. The ration a coin or two
/// over the stead's board, because carrying bread to the middle of nowhere
/// is work; the trinket at a fence's honest rate, no questions asked; the
/// hide a coin over the wood's-edge bench, the arbitrage's first stone
/// (D-025): the cart resells where hides are wanted.
/// </summary>
public static class Peddling
{
    public const int RationPrice = 6;
    public const int TrinketPrice = 7;
    public const int HideBonus = 1;
}

/// <summary>
/// The guard worn open (D-125): the second bar, and what wears it. Pressure is
/// flat and legible like the footing's 2s (D-094): a paid blow rocks a point,
/// the wall two, the heave's weight three, a parried blow most of all. The
/// parry costs less wind than a swing (it spends the turn not killing), and
/// the riposte through a broken guard lands 4 deeper: the one flat number big
/// enough to be worth standing into a shown blow for.
/// </summary>
public static class GuardBreak
{
    public const int BlowPressure = 1;
    public const int SlamPressure = 2;
    public const int HeavePressure = 3;
    public const int ParryPressure = 4;
    public const int ParryCost = 2;
    public const int RiposteBonus = 4;

    // The field's pressure on the bearer's own guard (D-126): only a committed,
    // telegraphed blow that lands rocks it; a nip or an untelegraphed trade is
    // the field's winded tap. The charge is sheer mass, the heave's mirror.
    public const int BearerLight = 1;
    public const int BearerHeavy = 2;
    public const int BearerCharge = 3;

    // The bearer's break (D-126): the stagger's count (lands as two full turns
    // without arms; the setting turn's own tick takes the first count), and how
    // much deeper every blow finds a beaten-open guard.
    public const int BearerStagger = 3;
    public const int OpenGuardDeeper = 2;

    // The guard war's coda (D-129): the carl's board-check is thrown mass
    // along the guard's line, the charge's tier with no blood in it at all;
    // and the drilled thegn answers the met parry, rolling off the bind with
    // half its force kept while the crossed iron shoves back a point.
    public const int CheckPressure = 3;
    public const int DrilledParryPressure = 2;
    public const int BindPressure = 1;
}

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

    /// <summary>
    /// The one who walks with the bearer (D-097): at most one, world-bound,
    /// mortal. Null is the usual state of the road; the storylet door (the
    /// huntsman's debt) casts one from the stead's own people.
    /// </summary>
    public Guest? Guest { get; private set; }

    /// <summary>
    /// The called shade (D-099): the calling's word given a while of shape,
    /// walking on the guest engine in its own slot beside the mortal one. Not
    /// mortal: it unravels (fall, dismissal, the bearer's death, the waygate)
    /// and nothing grieves; the price is the focus held, never the mourning.
    /// </summary>
    public Guest? Shade { get; private set; }

    /// <summary>Whoever walks with the bearer right now (D-097, D-099): the mortal guest first, then the shade.</summary>
    private IEnumerable<Guest> Fellows
    {
        get
        {
            if (Guest is { Alive: true } guest) yield return guest;
            if (Shade is { Alive: true } shade) yield return shade;
        }
    }

    private bool FellowAt(Pos p) => Fellows.Any(f => f.Pos == p);

    /// <summary>
    /// The beast of the road (D-100): mortal, world-bound, and never below
    /// ground: it walks the open land at the bearer's side, waits at a site's
    /// mouth through every delve, and carries what is loaded on it.
    /// </summary>
    public Mount? Mount { get; private set; }

    /// <summary>
    /// The stead's stable (D-100 stage 2): the beasts gathered and not at the
    /// bearer's side, kept per world. A stabled beast is safe from the raiders'
    /// night: deliberate parking is the saddlebags' honest counterplay.
    /// </summary>
    public List<Mount> Stable { get; } = [];

    /// <summary>The beast's cell counts only where the beast is (D-100): its coordinates live on the overworld alone.</summary>
    private bool MountAt(Pos p) => Mode == MapMode.Overworld && Mount is { } m && m.Pos == p;

    /// <summary>
    /// What the pool will actually answer with (D-099): the calling is held,
    /// not spent, so while the shade walks part of the focus stays bound to
    /// the word and no other working can draw on it.
    /// </summary>
    public int SpendableFocus => Player.Focus - (Shade is not null ? CallingHold : 0);

    /// <summary>The cast NPC held aside while they walk (D-097): re-added at the arc's end, never after a death.</summary>
    private Npc? _guestNpc;

    /// <summary>
    /// The casting (D-097 stage 2): the NPC across the talk is set aside from
    /// their life and falls in beside the bearer. The talk closes; the road
    /// starts here.
    /// </summary>
    internal void CastTalkNpcAsGuest(GuestRole role)
    {
        if (TalkNpc is not { } npc || Guest is not null) return;
        _guestNpc = npc;
        World.Npcs.Remove(npc);
        Guest = new Guest
        {
            Id = $"guest_{npc.Id}",
            Name = npc.Name,
            Role = role,
            NpcId = npc.Id,
            Pos = npc.Pos,
            MaxHp = 16,
            Hp = 16,
        };
        InTalkMenu = false;
        TalkNpc = null;
    }
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

    /// <summary>A vendor's own trade menu (D-071), reached from one digit of the talk
    /// menu, with its own nine digits for what does not fit the shared topics.</summary>
    public bool InTradeMenu { get; private set; }

    /// <summary>The knucklebones board (D-108): open at the skald's hearth, the stakes down.</summary>
    public bool InBonesMenu { get; private set; }

    /// <summary>The bearer's cast as it lies (D-108): three bones face-up on the board.</summary>
    public IReadOnlyList<int> BonesCast => _bonesCast;
    private readonly List<int> _bonesCast = [];

    /// <summary>Whether the one throw back has been taken (D-108): a second cast must lie as it lands.</summary>
    public bool BonesRethrown { get; private set; }

    /// <summary>The world's ledger on the hearth game (D-108): net coin won or lost. Per-world, replay-rebuilt.</summary>
    public int BonesNet { get; private set; }

    /// <summary>The stood round (D-123): once per world, because one round an evening is generosity and two is campaigning.</summary>
    public bool RoundStood { get; private set; }

    /// <summary>The keeping's choice menu (D-039), open only at the Hearth itself.</summary>
    public bool InThresholdMenu { get; private set; }

    /// <summary>The laying-down choice (D-045), open only face to face with a severed one.</summary>
    public bool InLayingMenu { get; private set; }

    private Monster? _layingTarget;

    /// <summary>Set by choosing the old way: the moment closes for this world's keeper.</summary>
    private bool _layingDeclined;

    /// <summary>
    /// The guard set (D-125): the one foe this turn's parry stands against.
    /// Lives exactly one turn, declaration to resolution, and is journal-derived
    /// like everything on the bearer: never serialized.
    /// </summary>
    private Monster? _parryTarget;

    /// <summary>The pack menu (D-041): 'i' anywhere; digits wield or wear, anything else closes.</summary>
    public bool InGearMenu { get; private set; }
    public bool InSheetMenu { get; private set; }

    /// <summary>
    /// A shaft set to the string (D-050): 'f' arms it, the next direction key
    /// looses along that line, anything else lowers the bow. Turn-free like a
    /// menu; the world holds still while the eye chooses.
    /// </summary>
    public bool InAim { get; private set; }

    /// <summary>The point set (D-056): 't' with the spear in hand; the next direction key sends the thrust.</summary>
    public bool InThrust { get; private set; }

    /// <summary>
    /// The heave wound up (D-058): 'w' with iron in hand sets the feet; the next
    /// direction key winds the blow at that line. Turn-free like the aim and the
    /// point: the choice of where to commit costs nothing until it is made.
    /// </summary>
    public bool InHeave { get; private set; }

    /// <summary>
    /// The workings carried (D-091): 'z' opens them, digits speak one, anything
    /// else keeps silence. Turn-free like every menu: the choosing costs nothing
    /// until a word is actually said.
    /// </summary>
    public bool InCastMenu { get; private set; }

    /// <summary>
    /// A line-hungry word chosen (D-091): the spark and the levin want a line
    /// the way the shaft and the point do; the next direction key gives it.
    /// </summary>
    public bool InCastLine { get; private set; }

    private SpellId? _pendingLineSpell;

    /// <summary>The bearer's blood when the levin was raised (D-091): a wound taken while it is held threatens the word.</summary>
    private int _hpAtLevinCommit;

    /// <summary>
    /// The terms of the crossing (D-047), open only at an open waygate: digits
    /// swear or unswear oaths on the next world, '>' crosses under what stands
    /// sworn, anything else steps back and the selection is let go.
    /// </summary>
    public bool InCrossingMenu { get; private set; }

    private readonly HashSet<OathId> _chosenOaths = [];

    /// <summary>The oaths currently marked in the open terms menu (Presenter reads this).</summary>
    public IReadOnlyCollection<OathId> ChosenOaths => _chosenOaths;

    /// <summary>The standing terms' summed weight: the visible Threat score (D-011).</summary>
    public int Burden => World.Burden;

    /// <summary>The songs' weighing of the bearer (D-048): derived from Legend, never stored.</summary>
    public int Standing => LegendStanding.StandingFor(Player.Legend);

    /// <summary>
    /// The keyed faction ledger (D-078, generalizing D-076's single scalar): each
    /// faction's weighing of the bearer, earned only by deeds that faction can
    /// perceive and reset at every crossing (the folk and the dens are this
    /// world's alone). Like everything on the bearer it is rebuilt by replay,
    /// never serialized.
    /// </summary>
    private readonly Dictionary<FactionId, int> _factionRegard = [];

    /// <summary>
    /// The other side of every faction's book (D-086): what each holds against the
    /// bearer, D-023's Infamy axis, kept apart from the regard so the two never
    /// cancel: a faction can esteem a deed and resent another at once, and both
    /// show. The raiders' wrath lives here (it was always their count against),
    /// and the stead's shame joins it as the first home-faction entry.
    /// </summary>
    private readonly Dictionary<FactionId, int> _factionInfamy = [];

    /// <summary>A faction's count on the bearer: zero until a perceivable deed moves it.</summary>
    public int RegardOf(FactionId faction) => _factionRegard.GetValueOrDefault(faction);

    /// <summary>A faction's count against the bearer (D-086): zero until a perceivable transgression moves it.</summary>
    public int InfamyOf(FactionId faction) => _factionInfamy.GetValueOrDefault(faction);

    /// <summary>
    /// The home stead's regard for the bearer (D-076): local Fame, the first rung
    /// of the faction pillar (D-023). The deliberate opposite number to
    /// <see cref="Standing"/>, which carries between worlds.
    /// </summary>
    public int Regard => RegardOf(FactionId.Stead);

    /// <summary>The raiders' wrath at the bearer (D-078): the enemy ledger, one notch per raider slain.</summary>
    public int Wrath => InfamyOf(FactionId.Raiders);

    /// <summary>The stead's suspicion of the bearer (D-086): local Infamy, one rung per door pilfered.</summary>
    public int Shame => InfamyOf(FactionId.Stead);

    /// <summary>The mound's grudge (D-106): the dead's count against the bearer, grave-goods taken while they walked.</summary>
    public int Grudge => InfamyOf(FactionId.Mound);

    /// <summary>How many of its slain this world's mound has raised again (D-106); capped, replay-rebuilt.</summary>
    private int _risenCount;

    /// <summary>
    /// Raids the stead has suffered this world (D-079): the raiders acting on
    /// their coarse tick while their camp stands. Each raid thins the stores
    /// (bread a coin dearer, see RationPrice) until the crossing. Per-world,
    /// replay-rebuilt, never serialized.
    /// </summary>
    public int Raids { get; private set; }

    /// <summary>
    /// The stead's stores (D-089): the grain its season stands on, full at each
    /// world's start. Raids drain it, bread's price rides it, and once the camp
    /// falls it recovers a measure per tick until the lofts stand full again.
    /// </summary>
    public int Stores { get; private set; } = SteadStores.Max;

    /// <summary>The dens' boldness (D-089): derived, causal, replay-free. Plunder emboldens; dead raiders cow.</summary>
    public int Boldness => RaiderBoldness.Of(Raids, Wrath);

    /// <summary>
    /// Whether the stead's levy stands (D-105): the lofts down to the last
    /// measure, the larder closed, the steadholder taking coin against carted
    /// grain instead. Per-world, replay-rebuilt, never serialized.
    /// </summary>
    public bool LevyStands { get; private set; }

    /// <summary>
    /// Whether the stead's watch stands (D-105): posted after a greedy raid,
    /// turning the raiding nights away at a measure a tick in upkeep.
    /// Per-world, replay-rebuilt, never serialized.
    /// </summary>
    public bool WatchStands { get; private set; }

    /// <summary>The turn this world began: the raid tick counts from here, not from cycle 1.</summary>
    private int _worldStartTurn;

    /// <summary>Whether this world's steadholder has named the friend's price aloud (D-080); once per stead.</summary>
    private bool _friendsPriceNamed;

    /// <summary>Whether this world's peddler has named the cart's indifference to the barred larder (D-124); once per world.</summary>
    private bool _cartsBreadNamed;

    public Npc? TalkNpc { get; private set; }

    /// <summary>
    /// A dialogue-tree scene stands open (D-117): the world holds still while the
    /// moment is answered. Digits choose; while choices stand, every other key is
    /// the scene waiting. A node with no choices closes on any key.
    /// </summary>
    public bool InScene { get; private set; }

    private Scene? _scene;
    private List<Fact> _sceneCaptures = [];

    /// <summary>The open scene's current node, null outside a scene.</summary>
    public SceneNode? SceneNode { get; private set; }

    /// <summary>The open scene's box title (Presenter reads this).</summary>
    public string SceneTitle => _scene?.Title ?? "";

    /// <summary>The current node's prose, expanded, as the panel shows it.</summary>
    public IReadOnlyList<(string Text, LogTone Tone)> SceneProse => _sceneProse;
    private readonly List<(string Text, LogTone Tone)> _sceneProse = [];

    /// <summary>
    /// The current node's choices as shown: the label, and for a checked choice
    /// the visible odds tag ("Presence, 40 in 100"), "" otherwise. The odds are
    /// read off the bearer's sheet at node entry, before anything is committed.
    /// </summary>
    public IReadOnlyList<(string Label, string Tag)> SceneChoices => _sceneChoices;
    private readonly List<(string Label, string Tag)> _sceneChoices = [];

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
    public IReadOnlyList<(TradeGood Good, string Arg, string Label)> Offers => _offers;
    private readonly List<(TradeGood Good, string Arg, string Label)> _offers = [];

    /// <summary>The open vendor's trade-menu entries (D-071): its own nine digits,
    /// stable in order so a buyer's fingers never land on a shifted line.</summary>
    public IReadOnlyList<(TradeGood Good, string Arg, string Label)> TradeOffers => _tradeOffers;
    private readonly List<(TradeGood Good, string Arg, string Label)> _tradeOffers = [];

    /// <summary>Most rations a person can carry: the sink recurs instead of stockpiling.</summary>
    public const int RationCap = 5;

    /// <summary>
    /// Fact-derived pricing (D-025 v0): while a blight story stands uncompleted,
    /// the larders are thin and bread costs half again as much, and the thinned
    /// lofts (D-079, D-089) add their mark-up, riding the stores themselves now
    /// rather than a frozen raid count, so a stead whose camp has fallen prices
    /// its bread back down as its season recovers. The hearth-price (D-048)
    /// takes a coin off for the storied, the friend's price (D-080) another for
    /// the one who ended the raids, before the hungry road (D-047) doubles
    /// whatever the world was asking.
    /// </summary>
    public int RationPrice =>
        ((World.Facts.Exists("story", CreepingBlightTemplate.Id)
        && !World.Facts.Exists("story_complete", CreepingBlightTemplate.Id) ? 6 : 4)
        + SteadStores.PriceBump(Stores)
        - (Standing >= 2 && !World.Oaths.Contains(OathId.HushedName) ? 1 : 0)
        // The friend's price (D-080): deed-earned like the welcome (D-077), so
        // the hushed name never touches it where it silences the hearth-price.
        - (FriendsPrice ? 1 : 0)
        // The haunted look (D-098): bread is sold dearer to the one the stead
        // cannot quite meet the eyes of.
        + (Player.HasScar(ScarId.HauntedLook) ? 1 : 0))
        * (World.Oaths.Contains(OathId.HungryRoad) ? 2 : 1);

    /// <summary>
    /// Whether the stead holds the bearer a friend (D-080): the rung that opens the
    /// friend's price. Suspicion closes it (D-086): the folk do not extend a
    /// friend's terms to one held unwelcome, however the deeds once weighed.
    /// </summary>
    private bool FriendsPrice => SteadRegard.RungFor(Regard) >= SteadRegard.FriendRung
        && SteadShame.RungFor(Shame) < SteadShame.UnwelcomeRung;

    /// <summary>Whether the steadholder bars the larder to the bearer (D-086): bread is not sold to a named thief.</summary>
    public bool LarderBarred => SteadShame.RungFor(Shame) >= SteadShame.BarredRung;

    /// <summary>
    /// Whether the stead teaches the bearer freely (D-087): the own rung's boon.
    /// What the stead knows is not sold to the stead's own, only shown. Suspicion
    /// closes it like the friend's terms (D-086): know-how is trust made plain,
    /// and the folk do not put their craft into a hand they are watching.
    /// </summary>
    private bool SteadsTeaching => SteadRegard.RungFor(Regard) >= SteadRegard.OwnRung
        && SteadShame.RungFor(Shame) < SteadShame.UnwelcomeRung;

    /// <summary>How far one wear event moves the ledger: the spent edge (D-047) doubles it.</summary>
    private int WearStep => World.Oaths.Contains(OathId.SpentEdge) ? 2 : 1;

    /// <summary>
    /// One tick of the wear clock (D-092): wrightkin iron frays every other
    /// time, counted like Looses so the ledger and replay agree. Stacks with
    /// the parity knacks the way the knacks stack with each other: rarely.
    /// </summary>
    private int NextWear() => Player.Folk == FolkId.Wrightkin && Player.WearTick++ % 2 == 1 ? 0 : WearStep;


    /// <summary>
    /// What the herbwife asks to dress the wound: priced by how much convalescence
    /// it buys off, so waiting it out is always the poor bearer's option.
    /// </summary>
    public int MendPrice => (Player.WoundedTurns + 3) / 4;

    /// <summary>
    /// What the smith asks to see to everything the bearer owns (D-025's
    /// auto-scaling sink): each item prices its own mending off its own value,
    /// so wealth in iron taxes itself.
    /// </summary>
    public int RepairPrice => Player.AllGear.Sum(g => g.RepairPrice);

    /// <summary>
    /// What a cured hide fetches at the wood's edge (D-071): a flat few coin apiece for
    /// now, the hunt's coin payoff to sit beside its meat and its skill (D-070). A good
    /// hunt buys a lesson or a modest piece of iron; it does not make the market. Left a
    /// property so a world that prizes fur, or an oath, can lean on it later.
    /// </summary>
    public int HidePrice => 3;

    /// <summary>
    /// What a sprig of foraged herb fetches at the bench (D-074): a little more than a
    /// hide apiece, since the wood's simples are wanted for the mending-work and pay
    /// well for it (D-006). Flat for now, like the hide, a fact-flexible property.
    /// </summary>
    public int HerbPrice => 4;

    /// <summary>What the herbwife pays a sprig at her stillroom (D-081): the apothecary's price, a coin over the wood's-edge middleman's.</summary>
    public int StillroomHerbPrice => 5;

    /// <summary>The price the current buyer pays for herbs: the stillroom's if the herbwife is across the bench.</summary>
    private int HerbPriceHere => TalkNpc?.Id == "npc_herbwife" ? StillroomHerbPrice : HerbPrice;

    /// <summary>The price the current buyer pays for hides (D-124): the cart's coin over the bench, since the cart resells where hides are wanted.</summary>
    private int HidePriceHere => TalkNpc?.Kind == NpcKind.Peddler ? HidePrice + Peddling.HideBonus : HidePrice;

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

    public Game(ulong seed) : this(seed, firstWake: false) { }

    /// <summary>
    /// The real game always wakes with the asking (D-092): the TUI, the save
    /// replay, and the sim/journey harnesses all pass firstWake true, so every
    /// journal begins with the creation answers. The plain constructor keeps
    /// the instant, unmade wake for the test suite's fixed key scripts.
    /// </summary>
    public Game(ulong seed, bool firstWake)
    {
        MasterSeed = seed;
        // Cycle 1 uses the master seed directly, so pre-crossing saves stay replayable.
        World = WorldGen.Generate(seed);
        _combatRng = new Rng(SeedTree.Derive(World.Seed, "combat"));
        _storylets = new StoryletEngine(World.Seed, FullCatalog());
        Player.Pos = World.ShrinePos;
        SpawnMonsters();

        Log.Add(0, $"You wake at the shrine of {World.SettlementName}, in the world called {World.Name}.");
        if (firstWake)
        {
            // The asking (D-092): before the first step, the Aegis takes the
            // bearer's measure. The greeting, the rumor, and the arrival draw
            // all wait for the answers, so the opening reads as one scene.
            Log.Add(0, "A voice, close as your own pulse: \"Be still a breath. I have caught you, and I do not yet know you.\"", LogTone.Aegis);
            InCreation = true;
        }
        else
        {
            Log.Add(0, "A voice, close as your own pulse: \"Walk. I hold this place. I will catch you.\"", LogTone.Aegis);
            Log.Add(0, CampRumor);
            _storylets.TryFire(this, StoryletTrigger.Arrival);
        }
    }

    /// <summary>Global authored content plus this world's compiled story (D-032).</summary>
    private List<Storylet> FullCatalog() => [.. StoryletCatalog.All, .. World.StoryStorylets];

    /// <summary>The living chief of the camp's roster (D-110), while one still leads.</summary>
    public Monster? CampChief => Monsters.FirstOrDefault(m => m.Alive && m.Chief);

    /// <summary>
    /// The stead's warning of the camp (D-110): the rumor has always carried
    /// where the goblins den; now it carries who leads them, so the roster is
    /// perceivable before a blow is traded.
    /// </summary>
    private string CampRumor => $"Rumor: goblins from a cave to the {Compass(World.ShrinePos, World.CampPos)} raid {World.SettlementName}'s stores by night."
        + (CampChief is { Epithet: { } chief } ? $" The one that leads them down is called {chief}." : "");

    /// <summary>
    /// The camp knows the bearer (D-110): the first descent puts a voice to
    /// the name the rumor carried, and every named raider owed a grudge speaks
    /// it to the bearer's face, once per memory: the slaying's boast first,
    /// then the scar, then the office that came with a grudge in it.
    /// </summary>
    private void GreetTheRoster()
    {
        if (!_rosterMet && CampChief is { Epithet: { } chief })
        {
            Log.Add(Turn, $"One voice deeper in cuts over the others, giving short orders and getting obedience: {chief}, if the stead has the name right.", LogTone.Danger);
            _rosterMet = true;
        }
        foreach (var named in Monsters.Where(m => m.Alive && m.SiteId == CurrentSite!.Id
            && m.Epithet is not null && m.Grudge && !m.GrudgeSpoken))
        {
            Log.Add(Turn, named.SlewBearer
                ? $"{named.Epithet} sees you first, and grins wide enough to count teeth: it knows where you have been, because its own hand sent you there."
                : named.Scarred
                    ? $"Across the smoke, {named.Epithet} marks you and touches its scar once, the way another creature might touch an amulet."
                    : $"{named.Epithet} wears the chief's place now, and wears it looking at you: the office came with a grudge in it.", LogTone.Danger);
            named.GrudgeSpoken = true;
        }
    }

    /// <summary>
    /// The scar remembered (D-110): a named raider bloodied under the bearer's
    /// visit and left alive keeps the wound's author. Swept when the bearer
    /// leaves the camp, by the ladder or by dying in it: the memory needs the
    /// bearer gone and the raider breathing.
    /// </summary>
    private void MarkTheScarred()
    {
        foreach (var named in Monsters.Where(m => m.Alive && m.SiteId == "goblin-camp"
            && m.Epithet is not null && !m.Scarred && m.MaxHp > 0 && m.Hp < m.MaxHp))
        {
            named.Scarred = true;
            named.GrudgeSpoken = false;
            World.Facts.Add("nemesis", "scarred", named.Epithet!,
                $"{named.Epithet} of the camp above {World.SettlementName} carries a wound with the bearer's shape on the edge of it, and was left alive to remember who put it there.");
            Log.Add(Turn, $"Somewhere behind you, {named.Epithet} is still breathing around a wound with your shape on it. The dens remember that longer than they remember grain.", LogTone.Info);
        }
    }

    private void SpawnMonsters()
    {
        // Stats are generation inputs baked into the spawns (D-011), never live multipliers.
        foreach (var site in World.Sites)
            foreach (var spawn in site.Spawns)
                Monsters.Add(new Monster
                {
                    Kind = spawn.Kind,
                    Pos = spawn.Pos,
                    Hp = spawn.Hp,
                    MaxHp = spawn.Hp,
                    Epithet = spawn.Epithet,
                    Chief = spawn.Chief,
                    SiteId = site.Id,
                    Dormant = spawn.Kind is MonsterKind.Graven or MonsterKind.Warder,
                });
    }

    internal static string Compass(Pos from, Pos to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        string ns = dy <= -Math.Abs(dx) / 2 ? "north" : dy >= Math.Abs(dx) / 2 ? "south" : "";
        string ew = dx <= -Math.Abs(dy) / 2 ? "west" : dx >= Math.Abs(dy) / 2 ? "east" : "";
        string dir = ns + ew;
        return dir.Length == 0 ? "near" : dir;
    }

    // The asking (D-092): the creation scene at the first wake. One question at
    // a time in the standing dialog grammar, every answer a journaled key, so a
    // save replays the whole becoming. Turn-free: the world holds its breath.

    /// <summary>Whether the first-wake asking still stands open (D-092). Real play begins only after it closes.</summary>
    public bool InCreation { get; private set; }

    /// <summary>Which question is on the table (D-092).</summary>
    public CreationStage CreationStage { get; private set; }

    /// <summary>The name so far, while the last question stands (D-092).</summary>
    public string NameEntry { get; private set; } = "";

    /// <summary>Shapings still owed at the shaping question (D-092): two, or three for steadfolk.</summary>
    public int ShapingsLeft { get; private set; }

    /// <summary>The attribute chosen to rise, while the paying half stands open (D-092).</summary>
    public Attr? ShapeRaise { get; private set; }

    /// <summary>Whether the open Thing question is the burden's bought second (D-093).</summary>
    public bool PickingSecondThing { get; private set; }

    private void HandleCreationKey(char key)
    {
        switch (CreationStage)
        {
            case CreationStage.Folk:
                if (key == '0') { RollBearer(); return; }
                if (key >= '1' && key <= '0' + CreationCatalog.Folk.Count)
                {
                    ApplyFolk((FolkId)(key - '1'));
                    CreationStage = CreationStage.Past;
                }
                return;

            case CreationStage.Past:
                if (key >= '1' && key <= '0' + CreationCatalog.Pasts.Count)
                {
                    ApplyPast((PastId)(key - '1'));
                    ShapingsLeft = Player.Folk == FolkId.Steadfolk ? 3 : 2;
                    CreationStage = CreationStage.ShapeRaise;
                }
                return;

            case CreationStage.ShapeRaise:
                if (key == '0') { CreationStage = CreationStage.Thing; return; }
                if (key >= '1' && key <= '7')
                {
                    var attr = (Attr)(key - '1');
                    if (Player.Attributes[attr] >= CreationCatalog.ShapeCeiling)
                    {
                        Log.Add(Turn, $"{AttributeSet.NameOf(attr)} already stands as high as a starting body holds.");
                        return;
                    }
                    ShapeRaise = attr;
                    CreationStage = CreationStage.ShapePay;
                }
                return;

            case CreationStage.ShapePay:
                if (key >= '1' && key <= '7')
                {
                    var pay = (Attr)(key - '1');
                    if (pay == ShapeRaise) return;
                    if (Player.Attributes[pay] <= CreationCatalog.ShapeFloor)
                    {
                        Log.Add(Turn, $"{AttributeSet.NameOf(pay)} has no more to give.");
                        return;
                    }
                    Player.Attributes[ShapeRaise!.Value] += 1;
                    Player.Attributes[pay] -= 1;
                    Log.Add(Turn, $"\"{AttributeSet.NameOf(ShapeRaise.Value)} over {AttributeSet.NameOf(pay)}. The years agree.\"", LogTone.Aegis);
                    ShapeRaise = null;
                    ShapingsLeft--;
                    CreationStage = ShapingsLeft > 0 ? CreationStage.ShapeRaise : CreationStage.Thing;
                }
                return;

            case CreationStage.Thing:
                if (key >= '1' && key <= '0' + CreationCatalog.Things.Count)
                {
                    if (!ApplyThing((ThingId)(key - '1'))) return;
                    CreationStage = PickingSecondThing ? CreationStage.Vow : CreationStage.Burden;
                    PickingSecondThing = false;
                }
                return;

            case CreationStage.Burden:
                if (key == '0') { CreationStage = CreationStage.Vow; return; }
                if (key >= '1' && key <= '0' + CreationCatalog.Burdens.Count)
                {
                    ApplyBurden((BurdenId)(key - '1'));
                    PickingSecondThing = true;
                    CreationStage = CreationStage.Thing;
                }
                return;

            case CreationStage.Vow:
                if (key == '0') { CreationStage = CreationStage.Face; return; }
                if (key >= '1' && key <= '0' + CreationCatalog.Vows.Count)
                {
                    ApplyVow((VowId)(key - '1'));
                    CreationStage = CreationStage.Face;
                }
                return;

            case CreationStage.Face:
                if (key == '.')
                {
                    Player.RememberedFace = NameEntry.Trim();
                    NameEntry = "";
                    CreationStage = CreationStage.Name;
                    return;
                }
                if (key == '-' && NameEntry.Length > 0) { NameEntry = NameEntry[..^1]; return; }
                if ((char.IsAsciiLetter(key) || key == ' ') && NameEntry.Length < 14)
                    NameEntry += NameEntry.Length == 0 ? char.ToUpperInvariant(key) : key;
                return;

            case CreationStage.Name:
                // The seal is '.', the erase '-': both plain printable keys, so
                // the journal's line format never meets a control character.
                if (key == '.') { FinishCreation(NameEntry.Trim()); return; }
                if (key == '-' && NameEntry.Length > 0) { NameEntry = NameEntry[..^1]; return; }
                if ((char.IsAsciiLetter(key) || key == ' ') && NameEntry.Length < 14)
                    NameEntry += NameEntry.Length == 0 ? char.ToUpperInvariant(key) : key;
                return;
        }
    }

    private void ApplyFolk(FolkId id)
    {
        var def = CreationCatalog.FolkOf(id);
        Player.Folk = id;
        if (def.TiltUp is { } up) Player.Attributes[up] += 1;
        if (def.TiltDown is { } down) Player.Attributes[down] -= 1;
        if (id == FolkId.Steadfolk) Player.Coin += 10;
        Log.Add(Turn, $"\"{def.Name}, then: {def.Blurb}.\"", LogTone.Aegis);
    }

    private void ApplyPast(PastId id)
    {
        var def = CreationCatalog.PastOf(id);
        Player.Past = id;
        BankSkill(def.Skill);
        switch (id)
        {
            case PastId.Soldier:
                var jack = GearCatalog.Create("quilted_jack");
                jack.Wear = jack.MaxWear / 2;
                Player.Armor = jack;
                Player.GearLineHeard = true;
                break;
            case PastId.Poacher:
                Player.Bow = GearCatalog.Create("hunting_bow");
                Player.GearLineHeard = true;
                break;
            case PastId.HedgeHealer:
                Player.Herb += 3;
                break;
            case PastId.Wayfarer:
                Player.Rations += 2;
                break;
            case PastId.Oathbreaker:
                // Twice-skilled, once-stained: the second craft is paid for in
                // a name the stead already half-knows (D-086's ledger).
                BankSkill(SkillId.Hunting);
                _factionInfamy[FactionId.Stead] = Shame + 1;
                break;
        }
        World.Facts.Add("past", id.ToString().ToLowerInvariant(), World.SettlementName, def.Blurb);
        Log.Add(Turn, $"\"Once {def.Name}: {def.Blurb}.\"", LogTone.Aegis);
    }

    /// <summary>Level one, banked as counted uses (D-092): growth continues on the same honest ledger.</summary>
    private void BankSkill(SkillId skill)
    {
        int owed = SkillSet.UsesForLevel(1);
        for (int i = 0; i < owed; i++) Player.Skills.AddUse(skill);
    }

    /// <summary>False when the thing is already carried: nothing is taken twice (D-093).</summary>
    private bool ApplyThing(ThingId id)
    {
        if (Player.Things.Contains(id))
        {
            Log.Add(Turn, "\"You carry that already. Choose another.\"", LogTone.Aegis);
            return false;
        }
        Player.Things.Add(id);
        switch (id)
        {
            case ThingId.Word:
                if (!Player.HasSpell(SpellId.Spark)) Player.Spells.Add(SpellId.Spark);
                Player.Focus = Player.MaxFocus;
                Player.SpellLineHeard = true;
                Log.Add(Turn, "The spark has been yours since before the catching: a word older than any stead, carried quiet. (z speaks what you carry)", LogTone.Info);
                Log.Add(Turn, "\"So that is what you kept warm through the dark. Say it carefully.\"", LogTone.Aegis);
                break;
            case ThingId.FineArms:
                Player.Weapon = GearCatalog.Create("grave_iron");
                Player.GearLineHeard = true;
                Log.Add(Turn, "The grave-iron blade rides your hip like it grew there.", LogTone.Info);
                break;
            case ThingId.CraftKit:
                if (!Player.HasLesson(LessonId.Stillcraft)) Player.Lessons.Add(LessonId.Stillcraft);
                Player.LessonLineHeard = true;
                Player.Herb += 6;
                Log.Add(Turn, $"The brewing satchel settles on your shoulder: simples, stoppered vials, and the craft in your hands. ({Player.Herb} sprigs carried)", LogTone.Info);
                break;
            case ThingId.Purse:
                Player.Coin += 25;
                Log.Add(Turn, $"The purse is heavy, and asks no questions. ({Player.Coin} coin)", LogTone.Info);
                break;
            case ThingId.Keepsake:
                Player.Keepsake = true;
                World.Facts.Add("keepsake", "unassuming-thing", World.SettlementName, "small, worn smooth, and it will not say what it is");
                Log.Add(Turn, "A small thing, worn smooth by older hands than yours. It is not worth a coin, and you would not sell it for a hundred.", LogTone.Info);
                break;
        }
        return true;
    }

    private void ApplyBurden(BurdenId id)
    {
        var def = CreationCatalog.BurdenOf(id);
        Player.Burden = id;
        // The ledger burdens mark this first world now; every later world's
        // ledgers wake marked at the crossing (D-093).
        if (id == BurdenId.HuntedPast) _factionInfamy[FactionId.Raiders] = Math.Max(Wrath, 1);
        if (id == BurdenId.MarkedFace) _factionInfamy[FactionId.Stead] = Shame + 1;
        Log.Add(Turn, $"\"{Cap(def.Name)}: {def.Blurb}. So be it; {def.Price}. What else, then, came through the dark?\"", LogTone.Aegis);
    }

    private void ApplyVow(VowId id)
    {
        var def = CreationCatalog.VowOf(id);
        Player.Vow = id;
        Log.Add(Turn, $"\"{Cap(def.Name)}: {def.Blurb}. I will hold it with you. Vows are counted too.\"", LogTone.Aegis);
    }

    /// <summary>The rolled bearer (D-092): one key, the whole becoming, from the world's own stream.</summary>
    private void RollBearer()
    {
        var rng = new Rng(SeedTree.Derive(World.Seed, "bearer"));
        Log.Add(Turn, "\"As you like. The shrine has seen bearers enough to guess a shape.\"", LogTone.Aegis);
        ApplyFolk((FolkId)rng.Next(CreationCatalog.Folk.Count));
        ApplyPast((PastId)rng.Next(CreationCatalog.Pasts.Count));
        int shapings = rng.Next((Player.Folk == FolkId.Steadfolk ? 3 : 2) + 1);
        for (int i = 0; i < shapings; i++)
        {
            var up = (Attr)rng.Next(AttributeSet.Count);
            var pay = (Attr)rng.Next(AttributeSet.Count);
            if (up == pay
                || Player.Attributes[up] >= CreationCatalog.ShapeCeiling
                || Player.Attributes[pay] <= CreationCatalog.ShapeFloor) continue;
            Player.Attributes[up] += 1;
            Player.Attributes[pay] -= 1;
        }
        ApplyThing((ThingId)rng.Next(CreationCatalog.Things.Count));
        // Fate may take a burden too (D-093), and the burden buys its second thing.
        int burden = rng.Next(CreationCatalog.Burdens.Count + 1);
        if (burden > 0)
        {
            ApplyBurden((BurdenId)(burden - 1));
            ThingId second;
            do { second = (ThingId)rng.Next(CreationCatalog.Things.Count); }
            while (Player.Things.Contains(second));
            ApplyThing(second);
        }
        int vow = rng.Next(CreationCatalog.Vows.Count + 1);
        if (vow > 0) ApplyVow((VowId)(vow - 1));
        if (rng.Next(3) == 0 || Player.Vow == VowId.Finding)
            Player.RememberedFace = NameGen.Person(ref rng);
        FinishCreation(NameGen.Person(ref rng));
    }

    private void FinishCreation(string name)
    {
        if (name.Length == 0)
        {
            var nameRng = new Rng(SeedTree.Derive(World.Seed, "bearer-name"));
            name = NameGen.Person(ref nameRng);
        }
        // A vow of finding needs a face to look for (D-093): an unnamed one is
        // drawn from its own stream, so the vow never dangles.
        if (Player.Vow == VowId.Finding && Player.RememberedFace.Length == 0)
        {
            var faceRng = new Rng(SeedTree.Derive(World.Seed, "bearer-face"));
            Player.RememberedFace = NameGen.Person(ref faceRng);
        }
        Player.Name = name;
        InCreation = false;
        NameEntry = "";
        var folk = CreationCatalog.FolkOf(Player.Folk!.Value);
        var past = CreationCatalog.PastOf(Player.Past!.Value);
        Log.Add(Turn, $"So it goes into the count: {Player.Name}, {folk.Name} by blood, once {past.Name}.", LogTone.Info);
        Log.Add(Turn, "\"Enough. I know you now, as far as knowing goes. Walk. I hold this place. I will catch you.\"", LogTone.Aegis);
        Log.Add(Turn, CampRumor);
        // The scribe's-ward extra (D-092): the old writings say where old writing waits.
        if (Player.Past == PastId.ScribesWard
            && World.Sites.FirstOrDefault(s => s.StonePos is not null && s.Kind != SiteKind.GoblinCamp) is { } lettered)
            Log.Add(Turn, $"You know from the old writings: something graven waits {Compass(World.ShrinePos, lettered.OverworldPos)} of here, cut deep in standing stone.");
        _storylets.TryFire(this, StoryletTrigger.Arrival);
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

        if (InCreation)
        {
            HandleCreationKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InScene)
        {
            HandleSceneKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InGearMenu)
        {
            HandleGearMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InSheetMenu)
        {
            HandleSheetMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InCrossingMenu)
        {
            HandleCrossingMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InThresholdMenu)
        {
            HandleThresholdMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InLayingMenu)
        {
            HandleLayingMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InUnbindMenu)
        {
            HandleUnbindMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InBonesMenu)
        {
            HandleBonesMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InTradeMenu)
        {
            HandleTradeMenuKey(key);
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

        if (InAim)
        {
            HandleAimKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InThrust)
        {
            HandleThrustKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InHeave)
        {
            HandleHeaveKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InCastMenu)
        {
            HandleCastMenuKey(key);
            KeyApplied?.Invoke(key);
            return;
        }

        if (InCastLine)
        {
            HandleCastLineKey(key);
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

        // A wound-up heave commits you (D-058): the next thing you do looses it,
        // hit or miss, on the cell you chose. Turn-free peeks (the pack, the
        // sheet) still open; anything that would cost the field a turn spends it
        // on loosing the blow instead. There is no taking it back: that is the
        // whole of the commitment the field already read.
        if (Player.HeaveTarget is { } heaveCell && cmd is not (Command.Gear or Command.Sheet))
        {
            ResolveHeave(heaveCell);
            AdvanceTurn();
            return;
        }

        // The levin held (D-091) commits the same way the heave does: the next
        // act that would cost a turn spends it saying the word instead.
        if (Player.LevinTarget is { } levinCell && cmd is not (Command.Gear or Command.Sheet))
        {
            ResolveLevin(levinCell);
            AdvanceTurn();
            return;
        }

        bool tookTime = cmd switch
        {
            Command.Wait => DoWait(),
            Command.Enter => DoEnter(),
            Command.Exit => DoExit(),
            Command.Grab => DoGrab(),
            Command.Lift => DoLift(),
            Command.Burgle => DoBurgle(),
            Command.Rest => DoRest(),
            Command.Eat => DoEat(),
            Command.Drink => DoDrink(),
            Command.Gear => DoGearMenu(),
            Command.Sheet => DoSheet(),
            Command.Loose => DoLoose(),
            Command.Thrust => DoThrust(),
            Command.Heave => DoHeave(),
            Command.Cast => DoCast(),
            Command.Stance => DoStance(),
            Command.Parry => DoParry(),
            Command.Order => DoOrder(),
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
            if (blocker is not null)
            {
                if (blocker.Kind == MonsterKind.Severed && Player.Resolution != Resolution.None && !_layingDeclined)
                    return OpenLayingMenu(blocker);
                return AttackMonster(blocker);
            }
        }
        else
        {
            var npc = World.Npcs.FirstOrDefault(n => n.Pos == target);
            if (npc is not null) return StartTalk(npc);
        }

        // Stepping into one who walks with you (D-097, D-099) trades places:
        // a doorway is never a standoff between friends.
        if (Fellows.FirstOrDefault(f => f.Pos == target) is { } fellow)
        {
            (fellow.Pos, Player.Pos) = (Player.Pos, target);
            Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 1);
            Log.Add(Turn, $"You and {fellow.Name} trade places in a step.");
            return true;
        }

        // The beast at a gate (D-100) is an obstacle only to strangers.
        if (MountAt(target) && Mount is { } led)
        {
            (led.Pos, Player.Pos) = (Player.Pos, target);
            Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 1);
            Log.Add(Turn, $"You push past {led.Name} with a hand on its neck, trading places in a step.");
            return true;
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

        // The high ground's watcher (D-100 stage 2): the wild pony named once,
        // the first time the road brings the bearer close enough to see it see them.
        if (Mode == MapMode.Overworld && World.WildPonyPos is { } watcher
            && watcher.Chebyshev(target) <= 2 && !World.Facts.Exists("met", "fell_pony"))
        {
            World.Facts.Add("met", "fell_pony", World.SettlementName,
                "A shaggy fell pony keeps the high ground near the stead, fed by no one and afraid of nothing.");
            Log.Add(Turn, "A shaggy fell pony keeps this high ground, watching you with more patience than fear. It wears no halter and belongs to no one, and it does not leave. (bread, offered close, might change its mind)", LogTone.Info);
        }

        // The ridden road (D-100): with the beast at your side, open grass
        // passes two strides to a key: the same clocks (toll, wounds, the
        // raiders' tick) count half the turns for the distance. The far cell
        // must be plain ground with no one standing on it.
        if (Mode == MapMode.Overworld && Mount is { } steed && steed.Pos.Chebyshev(Player.Pos) <= 2)
        {
            var far = target.Plus(dx, dy);
            if (map.InBounds(far)
                && MountCatalog.Strides(steed.Kind, map[target]) && MountCatalog.Strides(steed.Kind, map[far])
                && far != steed.Pos && !FellowAt(far)
                && !World.Npcs.Any(n => n.Pos == far))
            {
                target = far;
                Player.Pos = far;
                Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 1);
                DescribeTileIfNotable(far);
                _storylets.TryFire(this, StoryletTrigger.EnterTile, map[far]);
            }
        }

        // The gleaning (D-052): the spots exist in every world; the lesson is
        // what makes them visible and takeable. An untaught step gathers nothing.
        if (Mode == MapMode.Overworld && Player.HasLesson(LessonId.Gleaning)
            && World.Gleanings.Contains(target))
        {
            if (Player.Rations < RationCap)
            {
                World.Gleanings.Remove(target);
                Player.Rations++;
                Log.Add(Turn, $"Sweet roots under the bracken, right where the lesson said to look. You take them for the road. ({Player.Rations} carried)", LogTone.Reward);
            }
            else
            {
                Log.Add(Turn, "Good gleaning here, but you carry all a walking body can. You mark the spot and leave it standing.", LogTone.Info);
            }
        }

        // The forage (D-074): herbs anyone can stoop and pick, no lesson needed. The
        // Survival skill fattens what a spot gives, and grows for the taking of it, the
        // way Hunting grows off the hide. They bank like a trade-good, uncapped, to be
        // sold at the wood's edge, so a full larder never turns the picking away.
        if (Mode == MapMode.Overworld && World.Herbs.Contains(target))
        {
            World.Herbs.Remove(target);
            int taken = 1 + Player.Skills.Bonus(SkillId.Survival) + (Player.Folk == FolkId.Heathborn ? 1 : 0);
            Player.Herb += taken;
            GainSkill(SkillId.Survival);
            Log.Add(Turn, $"Wortcunning growth under the eaves: you pick {taken} good sprig{(taken == 1 ? "" : "s")} for the wood's-edge bench. ({Player.Herb} in the satchel)", LogTone.Reward);
        }

        // The keeping (D-039): stepping to the Hearth itself puts the choice on the
        // table. It reopens freely until answered; it never reopens after.
        if (Mode == MapMode.Site && CurrentSite is { Kind: SiteKind.Threshold }
            && map[target] == Terrain.Hearth
            && Player.CommissionHeard && Player.Resolution == Resolution.None)
        {
            InThresholdMenu = true;
            Log.Add(Turn, "You stand at the ring of plain stone. The warmth reaches you like a hand taking yours.");
            Log.Add(Turn, "\"Here it is, bearer: what I was forged to bring you to, and yours to take up or to refuse. Take your time. Nothing in this room hurries.\"", LogTone.Aegis);
        }
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
                    ? "An arch of black iron links. It hums, and the air beyond it is not this world's. Press > to read the terms and cross."
                    : "An arch of black iron links, older than the stones around it. It is shut.", LogTone.Aegis);
            else if (t == Terrain.BarrowEntrance)
                Log.Add(Turn, World.BarrowSite!.Cleared
                    ? "The long mound. Its stones are only stones now."
                    : "A long mound of turf over lintel stones. The passage under it exhales cold. Press > to stoop in.", LogTone.Danger);
            else if (t == Terrain.HollowEntrance)
                Log.Add(Turn, World.HollowSite!.Cleared
                    ? "The stone ring. Its fire is out, and the stones hold nothing now but weather."
                    : "A ring of standing stones. Inside it a small fire burns, though no one gathers wood. Press > to step in.", LogTone.Danger);
            else if (t == Terrain.QuarryEntrance)
                Log.Add(Turn, World.QuarrySite!.Cleared
                    ? "The old quarry. Broken stone below, and stillness that is only stillness now."
                    : "An old quarry, open to the sky. Below, half-cut figures stand among the spoil heaps, and none of them is leaning. Press > to climb down.", LogTone.Danger);
            else if (t == Terrain.HallEntrance)
                Log.Add(Turn, World.HallSite!.Cleared
                    ? "The fallen hall. Grey stone open to the sky, and nothing comes out to pace you."
                    : "A roofless hall of grey stone, its gate long fallen. In the shadow of the columns, low shapes stand watching the doorway, and none of them breathes. Press > to go in.", LogTone.Danger);
            else if (t == Terrain.RingfortEntrance)
                Log.Add(Turn, World.RingfortSite!.Cleared
                    ? "The ringfort stands empty. Wind in the gate-mouth, and the lanes between the walls going back to grass."
                    : "A ring-walled fort, grey and whole, its gate open on a grass lane. On the walls stand figures with boards at a spacing no wind ever kept, and something heavy moves between the rings. Press > to go in.", LogTone.Danger);
            else if (t == Terrain.LeaguerEntrance)
                Log.Add(Turn, World.LeaguerSite!.Cleared
                    ? "The leaguer stands empty around its mere. Wind riffles the black water, and the causeway is only a road now."
                    : "Earth-banks ring a broad black mere, dug by an army and never filled in. On the banks stand figures with boards up and slings hanging ready, and every one of them faces the bare holm at the water's middle. Press > to walk the works.", LogTone.Danger);
            else if (t == Terrain.WildsEntrance)
                Log.Add(Turn, World.WildsSite!.Cleared
                    ? "The game-trail, hunted out for now: cropped grass and old slots, and nothing moving in the glade."
                    : "A break in the trees where the deer come down to graze. Slots pressed in the mud, a run worn through the treeline, and the light going gold. Press > to hunt.", LogTone.Info);
            else if (t == Terrain.SonghallEntrance)
                Log.Add(Turn, "The stead's songhall: turf roof, smoke at the roof-hole, and low singing sometimes when the wind sits right. Press > to step in.", LogTone.Info);
            else if (t == Terrain.HarrowEntrance)
                Log.Add(Turn, "An old turf hall inside a ring of leaning stones. Smoke stands at the roof-hole in any weather, and the stones lean inward, like listeners. Press > to step in.", LogTone.Info);
            else if (t == Terrain.ThresholdEntrance)
                Log.Add(Turn, !Player.CommissionHeard
                    ? "A stair descends into the hill, cut clean and swept clean, though nothing lives near to sweep it. The dark below is not night-dark."
                    : Player.Resolution == Resolution.None
                        ? "The stair. Warm air rises from it, thick with hearth-smell. Press > to go down."
                        : "The stair down to the keeping. The warmth below rises to meet you like a door standing open. Press > to go down.", LogTone.Aegis);
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
                    SiteKind.Quarry => "The carvers' toolcache sits under a shelf of slate, sealed tight against an age of dust. Press g to open it.",
                    SiteKind.Hall => "A warded coffer stands against the chamber wall, its clasp unrusted after an age. Press g to open it.",
                    SiteKind.Ringfort => "An arms-chest sits at the heart of the ward, its lid sound under an age of dust. Press g to open it.",
                    SiteKind.Leaguer => "A cist of stacked stone sits on the holm's crown, its capstone set square against the weather. Press g to lift it.",
                    _ => "A battered strongbox sits here. Press g to open it.",
                }, LogTone.Reward);
            else if (CurrentSite is { StonePos: { } sp, StoneRead: false } && p == sp)
                Log.Add(Turn, "A standing stone, man-high, one word cut deep in its face. The cuts have kept their edges an age. Press g to read it.", LogTone.Aegis);
            else if (CurrentSite is { CofferPos: { } cp, CofferOpened: false } && p == cp)
                Log.Add(Turn, CurrentSite.CofferTried
                    ? "The locked coffer. It took your hand's measure once, and old iron does not give a second sitting."
                    : CurrentSite.Kind switch
                    {
                        SiteKind.GoblinCamp => "A strongbox off some road-wagon, dragged here and abandoned whole: no goblin has the patience for iron that argues. The lock still argues. Press g to try it.",
                        SiteKind.Quarry => "The tally-master's lockbox sits where the counting was done, iron-banded, its hasp set with a lock worth the wages inside. Press g to try it.",
                        SiteKind.Hall => "Under the bench, a strong little chest, iron-banded and locked by someone who trusted the lock more than the neighbors. Press g to try it.",
                        SiteKind.Ringfort => "The quartermaster's lockbox, bolted iron over oak, its keyhole worn bright by a key that left with its owner. Press g to try it.",
                        _ => "A captain's iron box, dented by the siege and opened by none of it. Press g to try the lock.",
                    }, LogTone.Info);
            else if (t == Terrain.ExitLadder)
                Log.Add(Turn, "Daylight above. Press < to climb out.");
            else if (t == Terrain.Hearth && CurrentSite!.Kind == SiteKind.Threshold && Player.Resolution != Resolution.None)
                Log.Add(Turn, Player.Resolution == Resolution.Kept
                    ? "The Hearth. It leans toward you the way a fire leans toward its keeper. The count is warm to the touch."
                    : "The Hearth burns alone, by your leave. It does not reproach you. Fires never do.", LogTone.Aegis);
            else if (CurrentSite!.Kind == SiteKind.Songhall)
                DescribeSonghallFixture(t, p);
            else if (CurrentSite!.Kind == SiteKind.Harrow)
                DescribeHarrowFixture(t);
        }
    }

    /// <summary>
    /// The harrow's reading surfaces (D-114). The room states the founding
    /// without a speaker: the mother-stone, and beside it the socket the
    /// daughter-stone was lifted from. The elder's talk says whose fault
    /// that is; the stones only say that it happened.
    /// </summary>
    private void DescribeHarrowFixture(Terrain t)
    {
        if (t == Terrain.Plinth)
            Log.Add(Turn, "The mother-stone stands here, twin in grain and tool-mark to the stead's shrine-stone, and half again its height. Beside it a socket is cut in the floor: empty, swept, and plainly kept that way.", LogTone.Info);
        else if (t == Terrain.Hearth)
            Log.Add(Turn, "The harrow's fire, small and exact. The wood beside it is split to one length, and the ash is raked the way a ledger is ruled.", LogTone.Info);
    }

    /// <summary>
    /// The songhall's reading surfaces (D-054). The room never changes; what the
    /// bearer's patronage has put in it does, read at runtime off the character,
    /// so worldgen never learns whose songs these are. In a hushed world (D-051)
    /// the traces go dark with everything else the songs carry: the deed stands,
    /// but nothing here was told to raise it.
    /// </summary>
    private void DescribeSonghallFixture(Terrain t, Pos p)
    {
        bool hushed = World.Oaths.Contains(OathId.HushedName);
        if (t == Terrain.Plinth)
        {
            if (!hushed && Player.PatronDeeds.Contains(PatronDeedId.RaisedStone))
                Log.Add(Turn, "On the plinth by the door stands a raised stone, grey and new-cut, and the name cut into it is yours. The songs walked ahead, and the stead answered in stone.", LogTone.Reward);
            else if (Player.PledgedDeeds.Contains(PatronDeedId.RaisedStone))
                Log.Add(Turn, "The plinth's socket is swept and chalk-marked: your pledge is made, and the stone will stand wherever the songs go next.", LogTone.Info);
            else
                Log.Add(Turn, "A stone plinth stands bare by the door, its socket cut and empty. It has waited a long time for someone worth a stone.", LogTone.Info);
        }
        else if (t == Terrain.Hearth)
        {
            if (!hushed && Player.PatronDeeds.Contains(PatronDeedId.EndowedHearth))
                Log.Add(Turn, "The long hearth burns high and fed, the woodstore full to the beams: your endowment, carried ahead by the songs. Any walker off the road eats here now, at a fire with your name in it.", LogTone.Reward);
            else if (Player.PledgedDeeds.Contains(PatronDeedId.EndowedHearth))
                Log.Add(Turn, "The long hearth burns as it always has, but the skald's chest holds your endowment now, waiting for the songs to carry it.", LogTone.Info);
            else
                Log.Add(Turn, "The long hearth burns low, tended by whoever sits nearest. The woodstore beside it is down to bark and ends.", LogTone.Info);
        }
        else if (p == WorldGen.SonghallVersePos)
        {
            Log.Add(Turn, "The east wall is scored with verses, cut small: the year's songs, the drovers' songs, five summers to a plank.", LogTone.Info);
            if (World.Facts.OfType("echo").FirstOrDefault() is { } echo)
                Log.Add(Turn, $"Among them, new-cut and already sung wrong at the well: {echo.Detail}", LogTone.Info);
            else
                Log.Add(Turn, "Nothing in them names a walker. The last plank hangs half bare: room, if any road ever sends a verse worth the cutting.", LogTone.Info);
            if (!hushed && Player.PatronDeeds.Contains(PatronDeedId.TrueVerse))
                Log.Add(Turn, $"And set apart, cut deeper, in a hand that took no liberties: your own verse, the account as you gave it. {Player.WorldsWalked.Count} worlds walked, and every stead left sleeping sound. The singers do not change a word of it.", LogTone.Reward);
            else if (Player.PledgedDeeds.Contains(PatronDeedId.TrueVerse))
                Log.Add(Turn, "At the wall's foot the carver's chalk is up for a verse not yet cut: yours, as you gave it, waiting on the crossing.", LogTone.Info);
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
            // The last stair (D-039): the door at its foot opens to the commission,
            // not to the map. Until then it is the arc's shut waygate: a promise.
            if (site.Kind == SiteKind.Threshold && !Player.CommissionHeard)
            {
                Log.Add(Turn, "Twelve steps down, a door of shrine-stone stands shut. No lock, no handle, and no argument to be had with it.");
                Log.Add(Turn, Player.LedgerHeard
                    ? "\"Not yet. There is a word that opens it, and I have not finished remembering the word. When I have it, you will hear it first.\""
                    : "\"I know this door. I do not remember knowing it. Leave it shut a while, bearer; some of what I have forgotten has edges.\"", LogTone.Aegis);
                return false;
            }

            // The mortal beasts smell what keeps an uncanny mouth (D-100 stage
            // 2): they shake the saddlebags off at the bearer's feet (the risk
            // handed back, never carried off free) and bolt for the stead's
            // stable. Only the fell pony stands this ground.
            if (Mount is { } spooked && MountCatalog.Spooks(spooked.Kind) && MountCatalog.UncannyMouth(site.Kind))
            {
                if (spooked.Bags > 0)
                {
                    Player.Coin += spooked.Bags;
                    Log.Add(Turn, $"{Cap(spooked.Name)} will not stand this ground: it shakes the saddlebags off at your feet ({spooked.Bags} coin, yours to carry after all) and is gone for home.", LogTone.Danger);
                    spooked.Bags = 0;
                }
                else
                    Log.Add(Turn, $"{Cap(spooked.Name)} will not stand this ground: ears flat, it turns and is gone for home.", LogTone.Info);
                Stable.Add(spooked);
                Mount = null;
            }

            Mode = MapMode.Site;
            CurrentSite = site;
            Player.Pos = site.EntryPos;
            // Whoever walks with you comes through the same door (D-097, D-099), at the shoulder.
            PlaceFellowsBeside(site.EntryPos);
            if (site.Kind == SiteKind.Threshold)
                Log.Add(Turn, Player.Resolution == Resolution.None
                    ? "You go down. The door of shrine-stone stands open, and the warmth beyond it is a kitchen's, not a forge's. Somewhere ahead, a fire is burning that has never once gone out."
                    : "You go down again. The door stands open. It will always stand open to you now.", LogTone.Aegis);
            else if (site.Kind == SiteKind.Songhall)
                Log.Add(Turn, "You step in under the turf roof. Woodsmoke, wax, and under both the smell of cut oak: the hall keeps its songs the way a granary keeps seed.", LogTone.Info);
            else if (site.Kind == SiteKind.Harrow)
                Log.Add(Turn, "You step in under the harrow's roof. Tallow-smoke, raked ash, and old stone: the room is bare the way a thing is bare on purpose.", LogTone.Info);
            else
                Log.Add(Turn, site.Kind switch
                {
                    SiteKind.Barrow => "You stoop under the lintel stone. The air inside is still, and cold, and does not want you.",
                    SiteKind.Hollow => "You step between the stones. The air changes, the way a room changes when someone in it has been waiting.",
                    SiteKind.Quarry => "You climb down into the old quarry. Half-cut figures stand about the pit in no order, and the silence has a mineral patience to it.",
                    SiteKind.Hall => "You pass under the fallen gate. Grass in the floor-cracks, sky where the roof was, and from the far end of the hall, the click of claws on stone.",
                    SiteKind.Leaguer => "You come up onto the works. Black water on your right hand the whole way round, a bare holm at its middle, and on the banks ahead, boards standing at their mounds like teeth in an old jaw.",
                    SiteKind.Wilds => "You come up onto the game-trail. Cropped grass, deer-slots pressed in the mud, and the whole glade holding still the way a wood holds still when it has already heard you.",
                    _ => "You descend into the goblin cave. The dark smells of smoke and old meat.",
                }, LogTone.Danger);
            if (site.Kind == SiteKind.Hollow && !site.Cleared)
            {
                Log.Add(Turn, "At the fire, a figure rises: neither old nor young, dressed out of no living fashion. It looks at your collarbone before it looks at your face.", LogTone.Danger);
                Log.Add(Turn, "\"All is counted, little shield.\" Courteous, and wrong, like a bell with a hairline crack.", LogTone.Danger);
            }
            // The camp knows the bearer (D-110): the chief put to a voice, and
            // every grudge owed spoken to the face it is held against.
            if (site.Kind == SiteKind.GoblinCamp) GreetTheRoster();
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
            // Reachable with the menu already open only through Apply (tests):
            // ApplyKey routes menu keys to the handler first. A second Enter
            // then means what the handler's '>' means: cross as sworn.
            if (InCrossingMenu)
            {
                InCrossingMenu = false;
                CrossToNextWorld([.. OathCatalog.All.Select(o => o.Id).Where(_chosenOaths.Contains)]);
                return true;
            }
            OpenCrossingMenu();
            return false;
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
    private void CrossToNextWorld(IReadOnlyList<OathId> oaths)
    {
        string prevWorld = World.Name;
        string prevSettlement = World.SettlementName;
        // The far side of a sworn crossing (D-047): the burden carried through
        // this world is honored in Legend, never in power.
        int prevBurden = World.Burden;
        int standingBefore = Standing;
        Player.WorldsWalked.Add(prevWorld);

        if (oaths.Count > 0)
            Log.Add(Turn, $"You set your hand on the arch and take up the terms: {string.Join(", ", oaths.Select(o => OathCatalog.Def(o).Name))}.", LogTone.Danger);

        if (Remnant is not null)
        {
            Log.Add(Turn, $"\"{AegisVoice.ForfeitLine}\"", LogTone.Aegis);
            Remnant = null;
        }

        int converted = Player.Coin;
        Player.Legend += converted;
        Player.Coin = 0;
        int honored = 10 * prevBurden;
        Player.Legend += honored;
        // The patron's weighing (D-054): pledged coin crosses as Legend at half
        // again its count, because patronized coin sings louder than counted
        // coin, and the deed itself joins the character for good.
        int patronized = Player.PledgedDeeds.Sum(d => PatronCatalog.Def(d).Worth);
        Player.Legend += patronized;
        Player.PatronDeeds.AddRange(Player.PledgedDeeds);
        Player.PledgedDeeds.Clear();

        // Repeat-weighting (D-040): the finished world's story travels into the next
        // draw as a generation input. It is itself a pure function of the seed
        // lineage, so worldgen stays deterministic per master seed.
        string? prevStory = World.Facts.OfType("story").FirstOrDefault()?.Subject;

        // The unsaid crosses on its own legs (D-120): every truth the bearer
        // kept at its saying-moment left a wrong story standing, and the whole
        // count so far is captured here to be pressed into the next world.
        var carriedSilences = World.Facts.OfType("silence").ToList();
        var keptTruths = World.Facts.OfType("withheld").ToList();

        Cycle++;
        // The walked list already carries this world's name (added above), so the
        // next world's weave avoids every verse of the long song (D-049).
        World = WorldGen.Generate(SeedTree.Derive(MasterSeed, "cycle", Cycle), tier: Cycle, prevStory: prevStory, oaths: oaths, takenNames: Player.WorldsWalked);
        _combatRng = new Rng(SeedTree.Derive(World.Seed, "combat"));
        _storylets.OnCrossing(World.Seed, FullCatalog());
        Monsters.Clear();
        SpawnMonsters();
        // World-bound (D-097): a living guest never crosses; their world keeps
        // them. The farewell and the portfolio fact are stage 2's work.
        Guest = null;
        // The called thing does not cross either (D-099): only the word does,
        // knowledge like every working, to be said again in the new dark.
        Shade = null;
        // And the beasts are world-bound like every mortal thing (D-100): its
        // land keeps them, saddlebags, stable, and all. The waygate takes
        // only the bearer.
        Mount = null;
        Stable.Clear();
        InShrineMenu = false;
        InTalkMenu = false;
        InUnbindMenu = false;
        InTradeMenu = false;
        InThresholdMenu = false;
        InLayingMenu = false;
        InGearMenu = false;
        InSheetMenu = false;
        InCrossingMenu = false;
        InCastMenu = false;
        InCastLine = false;
        _pendingLineSpell = null;
        _chosenOaths.Clear();
        TalkNpc = null;
        CloseScene();
        CurrentSite = null;
        // The words cross whole; the said state does not (D-091): no ward holds
        // through an arch, no levin survives it, and the pool arrives at brim.
        Player.HeaveTarget = null;
        Player.LevinTarget = null;
        Player.WardTurns = 0;
        Player.ChilledTurns = 0;
        // The guard crosses whole (D-126): worn is not wounded, and the far
        // side of the arch has thrown no blows yet.
        Player.PostureDmg = 0;
        Player.StaggerTurns = 0;
        Player.Focus = Player.MaxFocus;
        // The menders' honor (D-048): a world's Unbinder will loosen one more
        // raise for a bearer the songs carry high. The hushed name (D-051)
        // silences it with every other favor standing buys.
        bool hushed = World.Oaths.Contains(OathId.HushedName);
        UnbindingsLeft = UnbindingsPerWorld + (Standing >= 4 && !hushed ? 1 : 0);
        _layingTarget = null;
        _layingDeclined = false;
        // The ledgers are this world's alone (D-076, D-078, D-086): the far gate
        // leaves regard, wrath, and shame behind with the folk and the dens that
        // kept them, and the next world starts the bearer at a stranger to both.
        _factionRegard.Clear();
        _factionInfamy.Clear();
        // The burdens cross with the bearer (D-093): a hunted past wakes the new
        // dens' wrath, a marked face the new stead's suspicion, before a deed is done.
        if (Player.Burden == BurdenId.HuntedPast) _factionInfamy[FactionId.Raiders] = 1;
        if (Player.Burden == BurdenId.MarkedFace) _factionInfamy[FactionId.Stead] = 1;
        // A fresh world's stores stand whole (D-079, D-089), and its raiders'
        // tick counts from this arrival, not from the far side of the arch.
        Raids = 0;
        Stores = SteadStores.Max;
        LevyStands = false;
        WatchStands = false;
        _risenCount = 0;
        BonesNet = 0;
        RoundStood = false; // the next hearth has met no one's generosity
        _worldStartTurn = Turn;
        _friendsPriceNamed = false;
        _cartsBreadNamed = false; // a fresh world's cart has said nothing yet
        // A fresh world's roster (D-110): these dens have not met the bearer.
        _rosterMet = false;
        _deathHand = null;

        Mode = MapMode.Overworld;
        Player.Pos = World.ShrinePos;
        Player.WoundedTurns = 0;
        // The crossing wipes the count clean (D-098): a fresh world, a rested
        // soul. The scars are the body's and cross with it, until each one's
        // own road back is walked.
        Player.Toll = 0;
        Player.Hp = Player.MaxHp;
        Player.Stamina = Player.MaxStamina;

        // The hushed name (D-051): the deed's song does not travel into a hushed
        // world at all, so nothing there can hum it, sing it, or know the walker.
        if (!hushed)
        {
            World.Facts.Add("echo", "deed", prevSettlement,
                prevBurden > 0
                    ? $"In a world called {prevWorld}, the bearer emptied a goblin cave under oath, and {prevSettlement} slept safe. The songs say they chose the harder walking."
                    : $"In a world called {prevWorld}, the bearer emptied a goblin cave, and {prevSettlement} slept safe.");
            // The traces (D-054): what patronage built travels with the songs,
            // pressed into each new world's facts at the crossing, never drawn
            // by worldgen. A hushed world's stead was never told what to raise.
            foreach (var deed in Player.PatronDeeds)
                World.Facts.Add("patronage", PatronCatalog.IdOf(deed), World.SettlementName, deed switch
                {
                    PatronDeedId.RaisedStone => $"A stone stands at {World.SettlementName}'s songhall door, raised against the walker's coming and cut with the walker's name.",
                    PatronDeedId.EndowedHearth => $"The songhall hearth at {World.SettlementName} burns fed from a walker's endowment: any stranger off the road eats at it.",
                    _ => $"On {World.SettlementName}'s verse-wall one verse is cut deeper than the rest, in the walker's own words, and the singers do not change it.",
                });
        }

        // The unsaid crosses on its own legs (D-120): a hushed name stills the
        // songs about the bearer, but the story a kept truth left standing was
        // never the bearer's song, so it travels through every arch, hushed or
        // not: you cannot hush what was never said. Old silences carry whole;
        // each freshly kept truth crosses as the story that stood in its place,
        // told for true, so the count of unsaid things only ever grows.
        foreach (var s in carriedSilences)
            World.Facts.Add("silence", s.Subject, s.Object, s.Detail);
        foreach (var kept in keptTruths)
            World.Facts.Add("silence", kept.Subject, kept.Object, kept.Subject switch
            {
                "mound_truth" => $"Out of a world called {prevWorld}, a story: {prevSettlement}, the stead that outlasted a barrow's grudge, and the hill that finally lay still. Every teller has it the same way.",
                "seat_truth" => $"Out of a world called {prevWorld}, a story: the seat at {prevSettlement} avenged, the dens' camp broken above the cairn, and a chief gone under with the debt paid. Every teller has it the same way.",
                "pit_truth" => $"Out of a world called {prevWorld}, a story: the old pit above {prevSettlement} that ate its greedy crew stone by stone, and the stead wise enough to leave the seam in the hill. Every teller has it the same way.",
                _ => $"Out of a world called {prevWorld}, a story: the shrine-stone at {prevSettlement} that came down as a gift in a hard year, and the hill folk who grudged the giving. Every teller has it the same way.",
            });

        // The long song (D-045): from the third world on, the walked worlds are one
        // song, compounding a verse per crossing, and every stead sings it wrong.
        if (Player.WorldsWalked.Count >= 2)
            World.Facts.Add("song", "the_descent", World.Name,
                $"First {string.Join(", then ", Player.WorldsWalked)}; and then, the singers swear, a world of glass where the walker wept, which no walker ever walked.");

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
        else if (Player.Resolution != Resolution.None)
        {
            // Steady state (arc sec 9): the crossing keeps its guaranteed real
            // estate, spoken from here on in the final register.
            Log.Add(Turn, $"\"{(Player.Resolution == Resolution.Kept ? AegisVoice.KeptCrossingLine : AegisVoice.RefusedCrossingLine)}\"", LogTone.Aegis);
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
        if (honored > 0)
            Log.Add(Turn, $"The terms you carried through {prevWorld} are weighed with it. Legend grows by {honored} more.", LogTone.Reward);
        if (patronized > 0)
            Log.Add(Turn, $"And what you pledged in {prevWorld} is weighed last, at half again its coin: a patron's deed sings louder than a purse. Legend grows by {patronized} more.", LogTone.Reward);

        // The songs' weighing (D-048): standing is derived, so a rise can only
        // happen here, where Legend is minted, and the threshold announces it.
        if (Standing > standingBefore)
        {
            Log.Add(Turn, $"The weighing tips. In the songs of the worlds you are {LegendStanding.TitleOf(Standing)}.", LogTone.Reward);
            if (!Player.StandingLineHeard)
            {
                Player.StandingLineHeard = true;
                Log.Add(Turn, "\"A third ledger, then. This one no body keeps: the worlds keep it, and set it to tune. I cannot read it, bearer. I can only hear it sung.\"", LogTone.Aegis);
            }
        }

        Log.Add(Turn, $"You wake at the shrine of {World.SettlementName}, in the world called {World.Name}.");
        Log.Add(Turn, "The air is older here, and hungrier.", LogTone.Danger);
        if (World.Oaths.Count > 0)
            Log.Add(Turn, $"The terms you took up hold here: {string.Join(", ", World.Oaths.Select(o => OathCatalog.Def(o).Name))}.", LogTone.Danger);
        if (hushed)
            Log.Add(Turn, $"No song of you has come ahead. In {World.SettlementName} you are only a stranger off the road.");
        else
            Log.Add(Turn, $"In {World.SettlementName} they already sing of a stranger who emptied a goblin cave, in a world called {prevWorld}.");
        // The welcome (D-048): the songs walked ahead, and the stead answers them
        // in bread. Hospitality scales with standing and never past the cap. The
        // endowed hearth (D-054) adds one loaf from the hall's own store: the
        // single mechanical thing patronage buys, and it is bread, not power.
        int hearthLoaf = Player.PatronDeeds.Contains(PatronDeedId.EndowedHearth) ? 1 : 0;
        int welcome = hushed ? 0 : Math.Min(Math.Min(Standing, 3) + hearthLoaf, RationCap - Player.Rations);
        if (welcome > 0)
        {
            Player.Rations += welcome;
            Log.Add(Turn, $"By the shrine stone, bread has been set out against your coming, wrapped in waxed cloth. ({Player.Rations} carried)", LogTone.Reward);
        }
        Log.Add(Turn, CampRumor);
        if (World.BarrowSite is { } barrow)
            Log.Add(Turn, $"They speak lower of the long mound to the {Compass(World.ShrinePos, barrow.OverworldPos)}, where the dead do not lie easy.");
        if (World.HollowSite is { } hollow)
            Log.Add(Turn, $"And of the stone ring to the {Compass(World.ShrinePos, hollow.OverworldPos)} they say only this: leave the fire there to its keeper.");
        if (World.QuarrySite is { } quarry)
            Log.Add(Turn, $"Of the old quarry to the {Compass(World.ShrinePos, quarry.OverworldPos)} they say the carvers left mid-stroke, and that the figures in the pit are never quite where the last teller said they stood.");
        if (World.HallSite is { } hall)
            Log.Add(Turn, $"Of the fallen hall to the {Compass(World.ShrinePos, hall.OverworldPos)} the counsel is old and short: bar the byre at dusk, count the flock at dawn, and never go counting what runs between.");
        if (World.RingfortSite is { } fort)
            Log.Add(Turn, $"Of the ringfort to the {Compass(World.ShrinePos, fort.OverworldPos)} the counsel is oldest of all: the watch on its walls was never stood down, and what they pastured between the rings has not gone tame.");
        if (World.LeaguerSite is { } mere)
            Log.Add(Turn, $"Of the black mere to the {Compass(World.ShrinePos, mere.OverworldPos)} the stead keeps no counsel at all, only a habit: when a whirring carries off the water on a still day, they bide indoors until it stops.");
        if (World.SeveredNpc is { } calm)
            Log.Add(Turn, $"A hermit called {calm.Name} keeps a fire to the {Compass(World.ShrinePos, calm.Pos)}. The stead trades them nothing, owes them nothing, and minds them not at all: they have simply always been there.");
        _storylets.TryFire(this, StoryletTrigger.Arrival);
    }

    private bool DoExit()
    {
        if (Mode == MapMode.Site && CurrentSite!.Map[Player.Pos] == Terrain.ExitLadder)
        {
            bool leftTheCamp = CurrentSite.Kind == SiteKind.GoblinCamp;
            Mode = MapMode.Overworld;
            Player.Pos = CurrentSite.OverworldPos;
            CurrentSite = null;
            Log.Add(Turn, "You climb back into daylight.");
            // The scar remembered (D-110): what was bloodied and left breathing
            // behind you keeps the wound's author.
            if (leftTheCamp) MarkTheScarred();
            // Whoever walks with you climbs out behind you (D-097, D-099),
            // held ground or no: no one is left standing alone in the dark.
            PlaceFellowsBeside(Player.Pos);
            // The tether held (D-100): the small homecoming.
            if (Mount is { } steed)
                Log.Add(Turn, $"{Cap(steed.Name)} waits where you left it, cropping the grass, unimpressed by the underworld.");
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

        if (Mode == MapMode.Site && CurrentSite is { StonePos: { } stonePos, StoneRead: false } && Player.Pos == stonePos)
            return ReadGravenStone(CurrentSite);

        if (Mode == MapMode.Site && !CurrentSite!.ChestLooted && Player.Pos == CurrentSite.ChestPos)
        {
            int coin = CurrentSite.Kind switch
            {
                SiteKind.Barrow => _combatRng.Range(15, 27),
                SiteKind.Hollow => _combatRng.Range(4, 10),
                SiteKind.Quarry => _combatRng.Range(12, 24),
                SiteKind.Hall => _combatRng.Range(13, 25),
                SiteKind.Ringfort => _combatRng.Range(15, 28),
                SiteKind.Leaguer => _combatRng.Range(16, 30),
                _ => _combatRng.Range(10, 21),
            };
            Player.Coin += coin;
            CurrentSite.ChestLooted = true;
            Log.Add(Turn, CurrentSite.Kind switch
            {
                SiteKind.Barrow => $"Grave-gold: {coin} coin struck for rulers whose names did not keep.",
                SiteKind.Hollow => $"What they kept: a child's wooden horse, a ring sized for a thinner hand, and {coin} coin of a mint no one living has seen.",
                SiteKind.Quarry => $"Chisels still sharp under their oilcloth, and the crew's unpaid wages beside them: {coin} coin no one came back for.",
                SiteKind.Hall => $"Under an oiled cloth folded by patient hands: {coin} coin of a mint older than the quarry's wages.",
                SiteKind.Ringfort => $"The watch's pay-chest, tallied and locked against a paymaster who never rode in: {coin} coin, every wage accounted.",
                SiteKind.Leaguer => $"Under the capstone, packed in wool: {coin} coin of the holm-holder's hoard, laid by against a spending day that never came.",
                _ => $"The strongbox yields {coin} coin.",
            }, LogTone.Reward);

            // Site loot beyond coin (D-041, the D-033 deferral): the deep chests
            // each hold one signature piece. A bearer who already owns its like
            // leaves the twin where it lies: the catalog exists once, never once per world.
            string? gearId = CurrentSite.Kind switch
            {
                SiteKind.Barrow => "grave_iron",
                SiteKind.Quarry => "carvers_maul",
                SiteKind.Hall => "wrights_mail",
                SiteKind.Ringfort => "warbow",
                SiteKind.Leaguer => "scaled_byrnie",
                _ => null,
            };
            if (gearId is not null)
            {
                if (!Player.OwnsGear(gearId))
                {
                    var item = GearCatalog.Create(gearId);
                    Log.Add(Turn, CurrentSite.Kind switch
                    {
                        SiteKind.Barrow => $"Beneath the gold, wrapped in oiled wool, a blade of grave-iron: unrusted, and colder than the room. The {item.Name} is yours.",
                        SiteKind.Hall => $"And beneath the coin, folded shirt-wise as if put away for morning: rings of grey iron finer than any smith of this age draws. The {item.Name} is yours.",
                        SiteKind.Ringfort => $"And racked above the coin, strung and waxed as if the watch expected relief by the next moon: a bow of dark yew a head taller than the smith's work. The {item.Name} is yours.",
                        SiteKind.Leaguer => $"And beneath the hoard, folded scale on scale: grey steel made for sitting sieges under falling stones. The {item.Name} is yours.",
                        _ => $"And under the chisels, the master carver's own: a maul with a head like a closing verdict. The {item.Name} is yours.",
                    }, LogTone.Reward);
                    AcquireGear(item);
                }
                else
                {
                    Log.Add(Turn, CurrentSite.Kind switch
                    {
                        SiteKind.Barrow => "Beneath the gold lies a blade the twin of your own. You leave it with its dead.",
                        SiteKind.Hall => "Folded beneath the coin lies mail the twin of your own. You leave it put away.",
                        SiteKind.Ringfort => "Racked above the coin hangs a warbow the twin of your own. You leave it strung against a relief that is never coming.",
                        SiteKind.Leaguer => "Beneath the hoard lies a scaled byrnie the twin of your own. You leave it to keep the holm.",
                        _ => "The master carver's maul lies here too, twin to the one you carry. You leave it to the pit.",
                    }, LogTone.Info);
                }
            }
            // The grave-goods taken while the dead still walk (D-106): the
            // third faction's one transgression. A stilled barrow keeps no
            // ledger: dead laid to rest have no one left under the turf to
            // count, so only the unquiet mound marks the taking.
            if (CurrentSite.Kind == SiteKind.Barrow && !CurrentSite.Cleared)
            {
                _factionInfamy[FactionId.Mound] = Grudge + 1;
                World.Facts.Add("grudge", "grave_goods", World.SettlementName,
                    "The barrow's grave-gold was carried out while its dead still walked; the mound keeps the count.");
                Log.Add(Turn, "The passage does not stir, but something in it turns its attention the way a door turns on a hinge. The dead here were set to keep this, and they have marked whose pack it left in.", LogTone.Danger);
                Log.Add(Turn, "\"That was theirs to hold, bearer, and they are still holding everything else. The dead keep short ledgers: one entry, never smudged. Still it, or carry the mark.\"", LogTone.Aegis);
            }
            return true;
        }

        // The locked coffer (D-122): the crime family's guilt-free outlet, the
        // one lock in the world with no wronged party breathing behind it.
        if (Mode == MapMode.Site && CurrentSite is { CofferPos: { } cofferPos, CofferOpened: false } && Player.Pos == cofferPos)
            return TryCofferLock(CurrentSite);

        // The first transgression (D-086): a door with no one behind it, and a
        // hand that has learned to open things. Repayment outranks theft at a
        // shared corner: making right comes before more wrong, and a thief who
        // wants the next door must find an angle their conscience is not standing on.
        if (Mode == MapMode.Overworld)
        {
            foreach (var (dx, dy) in Directions.All8)
            {
                var q = Player.Pos.Plus(dx, dy);
                if (CurrentMap.InBounds(q) && CurrentMap[q] == Terrain.House
                    && World.PilferedHouses.Contains(q) && !World.RepaidHouses.Contains(q))
                    return RepayHouse(q);
            }
            foreach (var (dx, dy) in Directions.All8)
            {
                var q = Player.Pos.Plus(dx, dy);
                if (CurrentMap.InBounds(q) && CurrentMap[q] == Terrain.House
                    && !World.PilferedHouses.Contains(q))
                    return PilferHouse(q);
            }
        }

        Log.Add(Turn, "There is nothing here to take.");
        return false;
    }

    /// <summary>
    /// Reading a graven stone (D-091): the descent's own prize, a word taken
    /// into the reader for good. Each kind of old fabric leans toward its own
    /// word and gives the first of its leaning the bearer lacks, so the grant
    /// is journal-derived and worldgen stays blind to the character. The first
    /// word ever taken unveils the Focus the bearer never knew they held.
    /// </summary>
    private bool ReadGravenStone(Site site)
    {
        site.StoneRead = true;
        Log.Add(Turn, "You set your palm flat on the graven word, and the cuts warm under it.", LogTone.Reward);

        SpellId? next = null;
        foreach (var id in SpellCatalog.StonePreference(site.Kind))
            if (!Player.HasSpell(id)) { next = id; break; }
        if (next is null)
        {
            Log.Add(Turn, "The word is one you already carry. The stone has nothing left to give but company.", LogTone.Info);
            return true;
        }

        var def = SpellCatalog.Def(next.Value);
        bool first = Player.Spells.Count == 0;
        Player.Spells.Add(next.Value);
        Player.Focus = Player.MaxFocus;
        Log.Add(Turn, $"The word goes into you like cold water: {def.Name} is yours, for good. {def.FoundLine} (z speaks what you carry)", LogTone.Reward);
        World.Facts.Add("working", SpellCatalog.IdOf(next.Value), World.SettlementName,
            $"In the deep places near {World.SettlementName} a graven word was taken up: {def.Name}.");
        if (first)
        {
            Log.Add(Turn, "And behind your breastbone, something gathers that was never there before: a focus, waiting to be spent.", LogTone.Reward);
            if (!Player.SpellLineHeard)
            {
                Player.SpellLineHeard = true;
                Log.Add(Turn, "\"...That word is older than the stead, bearer. Older than me, it may be. I cannot hold what it does; only you can. Say it carefully.\"", LogTone.Aegis);
            }
        }
        return true;
    }

    /// <summary>
    /// Trying the locked coffer (D-122): the crime family's third verb, and the
    /// first with no ledger. The dice ride Sleight alone, harder than a pocket
    /// because old iron argues; a lock that gives pays coin and feeds the hand,
    /// a lock that holds teaches nothing and keeps its lid the rest of the
    /// world, because one sitting is all a lock ever grants. No shame, no
    /// facts, no witness: whoever owned this iron stopped needing it an age ago.
    /// </summary>
    private bool TryCofferLock(Site site)
    {
        if (site.CofferTried)
        {
            Log.Add(Turn, "The lock took your hand's measure once, and old iron does not give a second sitting. This lid keeps until the world does not.");
            return false;
        }

        site.CofferTried = true;
        if (_combatRng.Chance(Locks.ChanceFor(Player.Skills.Level(SkillId.Sleight))))
        {
            site.CofferOpened = true;
            int coin = _combatRng.Range(Locks.TakeMin, Locks.TakeMaxExclusive);
            Player.Coin += coin;
            Log.Add(Turn, "You work the lock by feel, the way a pocket is worked: patience, and a light hand, and one small click that sounds like agreement.", LogTone.Reward);
            Log.Add(Turn, site.Kind switch
            {
                SiteKind.GoblinCamp => $"The wagon-box gives up what the goblins never got at: {coin} coin under a false bottom even the road did not know about. ({Player.Coin} carried)",
                SiteKind.Quarry => $"The tally-master's box, honest to the last: {coin} coin in wage-rolls, each one tied with the same knot. ({Player.Coin} carried)",
                SiteKind.Hall => $"What the neighbors were not trusted with: {coin} coin, and a lock of hair pressed in wax you leave where it lay. ({Player.Coin} carried)",
                SiteKind.Ringfort => $"The quartermaster's own arithmetic: {coin} coin held back from the pay-chest, against a retirement that never rode in. ({Player.Coin} carried)",
                _ => $"The captain's box, kept through the whole siege: {coin} coin and a folded commission too proud to spend either. ({Player.Coin} carried)",
            }, LogTone.Reward);
            GainSkill(SkillId.Sleight);
        }
        else
        {
            Log.Add(Turn, "The lock takes your measure and gives nothing back: the wards inside sit shut against everything your fingers know. Old iron argues, and this one has won.", LogTone.Info);
        }
        return true;
    }

    /// <summary>
    /// Pilfering a house (D-086): the first thing the bearer can do that the stead
    /// counts against them, the transgression the Infamy axis was waiting on. One
    /// take per door per world, a ration's worth, and in a stead of three houses
    /// nothing taken stays secret: shame rises one rung per door, narrated the
    /// moment it lands (D-023's rule), with the way back down named the first time.
    /// </summary>
    private bool PilferHouse(Pos house)
    {
        if (Player.Rations >= RationCap)
        {
            Log.Add(Turn, "The latch would lift. But you carry all a walking body can, and even thieving has its arithmetic.");
            return false;
        }

        World.PilferedHouses.Add(house);
        Player.Rations++;
        Log.Add(Turn, $"The latch lifts under your thumb. A loaf and a heel of cheese, wrapped rough, and out again before the fire notices. ({Player.Rations} carried)", LogTone.Reward);
        // The small thing off the mantel (D-124): the take the stead would know
        // on sight, which is why no one in it will ever buy the thing back.
        // Stolen goods finally exist; the peddler's cart is their buyer.
        Player.Trinket++;
        Log.Add(Turn, $"A small thing follows from the mantel, cold in the palm: the kind a stead knows on sight, and a road-cart never asks about. ({Player.Trinket} with a past)", LogTone.Reward);

        var witness = World.Npcs.FirstOrDefault(n =>
            Math.Max(Math.Abs(n.Pos.X - Player.Pos.X), Math.Abs(n.Pos.Y - Player.Pos.Y)) <= 4);
        Log.Add(Turn, witness is not null
            ? $"{witness.Name} marks you from the lane, and does not look away."
            : "No eye is on you but the door's. It will not matter: steads count their loaves.", LogTone.Danger);

        bool firstShame = Shame == 0;
        RaiseShame(1);
        if (firstShame)
            Log.Add(Turn, "(What is taken can be made right at the door it was taken from: the same hand, and coin twice the loaf's worth on the sill.)", LogTone.Info);
        return true;
    }

    /// <summary>
    /// Picking a pocket (D-107): the crime family's second verb, and the first
    /// with dice in it. 'p' beside one of the stead's folk brushes their purse:
    /// come away clean and the coin and the craft are yours with no one the
    /// wiser (the stead's first secret fact from a deed); be caught and the
    /// wrist is held, the well hears by morning, and the shame is the same
    /// unified ladder pilfering climbs (D-086), because the stead does not
    /// keep separate books on flavors of thief. One try per pocket per world,
    /// and the wronged hand is repaid with the same key that tried it.
    /// </summary>
    private bool DoLift()
    {
        if (Mode != MapMode.Overworld)
        {
            Log.Add(Turn, "No pockets down here worth the name; what the deep places hold is not carried on a hip.");
            return false;
        }

        Npc? mark = null;
        foreach (var (dx, dy) in Directions.All8)
        {
            var q = Player.Pos.Plus(dx, dy);
            if (World.Npcs.FirstOrDefault(n => n.Pos == q && n.Kind == NpcKind.Villager) is { } npc)
            {
                mark = npc;
                break;
            }
        }
        if (mark is null)
        {
            Log.Add(Turn, "No one stands near enough to brush against.");
            return false;
        }

        // Making right outranks more wrong (D-086's corner rule, carried over):
        // a caught hand's restitution takes the key until it is paid.
        if (World.CaughtLifts.Contains(mark.Id) && !World.RepaidLifts.Contains(mark.Id))
            return RepayLift(mark);

        if (World.LiftedNpcs.Contains(mark.Id))
        {
            Log.Add(Turn, $"{mark.Name}'s purse has told you all it is going to. Twice at one pocket is how thieves get named.");
            return false;
        }

        World.LiftedNpcs.Add(mark.Id);
        if (_combatRng.Chance(Lifting.ChanceFor(Player.Skills.Level(SkillId.Sleight))))
        {
            int take = _combatRng.Range(Lifting.TakeMin, Lifting.TakeMaxExclusive);
            Player.Coin += take;
            Log.Add(Turn, $"A brush at the shoulder, a word about the weather, and {mark.Name}'s purse is {take} coin the lighter. No eye follows you away. ({Player.Coin} coin carried)", LogTone.Reward);
            if (!World.Facts.Exists("secret", "lifted_purse"))
                World.Facts.Add("secret", "lifted_purse", World.SettlementName,
                    $"Coin has gone missing from a pocket in {World.SettlementName}, and no one knows whose hand took it.");
            GainSkill(SkillId.Sleight);
        }
        else
        {
            World.CaughtLifts.Add(mark.Id);
            Log.Add(Turn, $"Your fingers find the purse, and {mark.Name}'s hand finds your wrist. \"So that is what you are.\" It will be round the well by morning.", LogTone.Danger);
            if (!World.Facts.Exists("shame", "confronted"))
                World.Facts.Add("shame", "confronted", World.SettlementName,
                    $"{mark.Name} caught the bearer's hand in their purse and said so to their face.");
            if (World.CaughtLifts.Count == 1)
                Log.Add(Turn, "(A wrong done to a hand is made right in that hand: coin twice the take, offered plainly, with the same key that tried it.)", LogTone.Info);
            RaiseShame(1);
        }
        return true;
    }

    /// <summary>
    /// Restitution for a caught lift (D-107): the unified ladder's exit, walked
    /// in the hand the wrong was done to, with the key that did it (D-086's
    /// symmetry kept). The same coin a sill asks, because the stead prices
    /// trust, not takings.
    /// </summary>
    private bool RepayLift(Npc mark)
    {
        if (Player.Coin < SteadShame.RepayCoin)
        {
            Log.Add(Turn, $"Making it right with {mark.Name} costs {SteadShame.RepayCoin} coin, and you hold {Player.Coin}. The wrong keeps until you can pay for it.");
            return false;
        }

        Player.Coin -= SteadShame.RepayCoin;
        World.RepaidLifts.Add(mark.Id);
        Log.Add(Turn, $"You put {SteadShame.RepayCoin} coin into {mark.Name}'s hand and name what it is for. They count it, twice, and nod once.", LogTone.Reward);
        LowerShame(1);
        return true;
    }

    /// <summary>
    /// Burglary proper (D-127): crime's last named verb, the whole distance
    /// in. 's' beside one of the stead's doors slips the latch and crosses
    /// the sill: come out unwoken and the kist's coin and an heirloom for the
    /// road-cart are yours, with the stead none the wiser (a secret fact, the
    /// third from a deed); be seen against the lane coming out and the shame
    /// jumps two rungs at once, because a loaf off a sill is hunger and a
    /// body in the dark of your house is something else. One try per door per
    /// world, and the ledger is the house's own: a sill already pilfered says
    /// nothing about the kist behind it.
    /// </summary>
    private bool DoBurgle()
    {
        if (Mode != MapMode.Overworld)
        {
            Log.Add(Turn, "No door down here has a hearth behind it. What the deep places keep, they keep openly.");
            return false;
        }

        // Making right outranks more wrong (D-086's corner rule, carried
        // over): a door whose dark saw you takes the key until it is paid.
        foreach (var (dx, dy) in Directions.All8)
        {
            var q = Player.Pos.Plus(dx, dy);
            if (CurrentMap.InBounds(q) && CurrentMap[q] == Terrain.House
                && World.CaughtBurglaries.Contains(q) && !World.RepaidBurglaries.Contains(q))
                return RepayBurglary(q);
        }

        bool anyDoor = false;
        Pos? house = null;
        foreach (var (dx, dy) in Directions.All8)
        {
            var q = Player.Pos.Plus(dx, dy);
            if (CurrentMap.InBounds(q) && CurrentMap[q] == Terrain.House)
            {
                anyDoor = true;
                if (!World.BurgledHouses.Contains(q)) { house = q; break; }
            }
        }
        if (house is null)
        {
            Log.Add(Turn, anyDoor
                ? "That dark has heard your step once, and a house listens harder after. This door keeps its nights."
                : "No door stands near enough to slip.");
            return false;
        }

        World.BurgledHouses.Add(house.Value);
        if (_combatRng.Chance(Burglary.ChanceFor(Player.Skills.Level(SkillId.Sleight))))
        {
            int take = _combatRng.Range(Burglary.TakeMin, Burglary.TakeMaxExclusive);
            Player.Coin += take;
            Player.Trinket++;
            Log.Add(Turn, "The latch gives to a light hand, and the dark inside is a room like any room: a banked fire, slow breathing from the loft, and a kist against the wall.", LogTone.Reward);
            Log.Add(Turn, $"The kist gives up {take} coin, and an heirloom comes off the shelf beside it: the kind a stead knows on sight, and a road-cart never asks about. ({Player.Coin} coin, {Player.Trinket} with a past)", LogTone.Reward);
            Log.Add(Turn, "Out, and the lane is empty. The house will know it was entered; it will never know by whom.", LogTone.Info);
            if (!World.Facts.Exists("secret", "burgled_house"))
                World.Facts.Add("secret", "burgled_house", World.SettlementName,
                    $"A house in {World.SettlementName} was entered in the dark and lightened, and no one knows whose foot crossed the sill.");
            GainSkill(SkillId.Sleight);
        }
        else
        {
            World.CaughtBurglaries.Add(house.Value);
            Log.Add(Turn, "The latch gives, but a floorboard answers it, and a voice answers that. You are out the door with empty hands, and behind you someone stands in it, watching you against the lane.", LogTone.Danger);
            if (!World.Facts.Exists("shame", "housebroken"))
                World.Facts.Add("shame", "housebroken", World.SettlementName,
                    $"The bearer was seen coming out of a doorway in {World.SettlementName} that was not theirs, and there is one word for what that makes them.");
            if (World.CaughtBurglaries.Count == 1)
                Log.Add(Turn, $"(A crossed sill is made right at that sill: {SteadShame.BreakInRepayCoin} coin, offered plainly, with the same key that crossed it.)", LogTone.Info);
            RaiseShame(2);
        }
        return true;
    }

    /// <summary>
    /// Restitution for a break-in (D-127): the unified ladder's exit, walked
    /// at the door that was crossed, with the key that crossed it (D-086's
    /// symmetry kept). Twice a sill's coin, because the stead prices trust by
    /// how far in the hand went, and it brings both rungs back down.
    /// </summary>
    private bool RepayBurglary(Pos house)
    {
        if (Player.Coin < SteadShame.BreakInRepayCoin)
        {
            Log.Add(Turn, $"Making a crossed sill right costs {SteadShame.BreakInRepayCoin} coin, and you hold {Player.Coin}. The wrong keeps until you can pay for it.");
            return false;
        }

        Player.Coin -= SteadShame.BreakInRepayCoin;
        World.RepaidBurglaries.Add(house);
        Log.Add(Turn, $"You stand at the door you crossed and put {SteadShame.BreakInRepayCoin} coin into the hands that live behind it, naming what it is for. The silence after is long, but the door does not close on you.", LogTone.Reward);
        LowerShame(2);
        return true;
    }

    /// <summary>
    /// Restitution (D-086): the designed exit off the shame ladder (D-023's
    /// no-eternal-stalemates rule), taken at the door the wrong was done, with the
    /// same key that did it. Coin twice the bread's worth on the sill, and the
    /// stead's count comes down a door; a repaid house is closed both ways.
    /// </summary>
    private bool RepayHouse(Pos house)
    {
        if (Player.Coin < SteadShame.RepayCoin)
        {
            Log.Add(Turn, $"Making this door right costs {SteadShame.RepayCoin} coin, and you hold {Player.Coin}. The wrong keeps until you can pay for it.");
            return false;
        }

        Player.Coin -= SteadShame.RepayCoin;
        World.RepaidHouses.Add(house);
        Log.Add(Turn, $"You leave {SteadShame.RepayCoin} coin on the sill, weighted under a stone: the loaf, and the trust, both paid for.", LogTone.Reward);
        LowerShame(1);
        return true;
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
            NpcKind.Smith => BuildSmithTopics(),
            NpcKind.Skald => BuildSkaldTopics(),
            NpcKind.Keeper => BuildKeeperTopics(),
            NpcKind.Harrower => BuildHarrowTopics(npc),
            NpcKind.Peddler => BuildPeddlerTopics(),
            _ => BuildTopics(npc),
        });
        _offers.Clear();
        if (npc.Kind is NpcKind.Villager or NpcKind.Smith or NpcKind.Skald or NpcKind.Peddler) _offers.AddRange(BuildOffers(npc));

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
        else if (npc.Kind == NpcKind.Peddler)
        {
            // The road's trader (D-124) is of no stead: the greeting formula
            // that names one would put the cart in the wrong house.
            Log.Add(Turn, $"{npc.Name} the peddler looks up from the cart, and weighs your pack before your face.");
            if (!World.Facts.Exists("met", npc.Id))
            {
                World.Facts.Add("met", npc.Id, World.SettlementName,
                    $"{npc.Name}, the peddler camped on the road outside {World.SettlementName}, has spoken with the bearer.");
                Log.Add(Turn, "\"Buying or selling? Both is best. Stand easy; the mule minds no one.\"");
            }
        }
        else if (npc.Kind == NpcKind.Harrower)
        {
            // The harrow's folk (D-114) are of the harrow, not the stead: the
            // stead's greeting formula would put them in the wrong house.
            Log.Add(Turn, $"{npc.Name}, {npc.Role} of the harrow, marks you before you speak, the way doorkeepers do.");
            if (!World.Facts.Exists("met", npc.Id))
            {
                World.Facts.Add("met", npc.Id, World.SettlementName,
                    $"{npc.Name}, {npc.Role} of the harrow, has spoken with the bearer.");
                Log.Add(Turn, "\"The harrow gives fire and floor to any who come up the hill civil. Doctrine is not asked of guests. It is offered, though.\"");
            }
        }
        else
        {
            Log.Add(Turn, $"{npc.Name}, {npc.Role} of {World.SettlementName}, turns to you.");
            if (!World.Facts.Exists("met", npc.Id))
            {
                World.Facts.Add("met", npc.Id, World.SettlementName,
                    $"{npc.Name}, {npc.Role} of {World.SettlementName}, has spoken with the bearer.");
                // The stead's regard reaches ahead of the bearer (D-076): once the
                // folk hold you a friend, even the ones you have not met greet you as one.
                int rung = SteadRegard.RungFor(Regard);
                Log.Add(Turn, rung >= 2
                    ? "\"No stranger to this stead, whatever your name. Word of you came in ahead of your feet.\""
                    : rung >= 1
                        ? "\"I know your face, or the stead's talk of it. Well met.\""
                        : "\"A stranger, then. Word travels slower than trouble here.\"");
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
        {
            // The crowded dark felt from inside (D-051): the stead lives in the
            // oath-bound world too, and its one visible den is the camp.
            string crowded = World.Oaths.Contains(OathId.CrowdedDark)
                ? " And there are more of them this year than the oldest of us can account for."
                : "";
            // The stead's talk keeps D-079's ledger: raids suffered since the
            // bearer walked in sharpen the answer, and the price is named.
            string raided = Raids switch
            {
                0 => "",
                1 => " And once since you walked in they have come again by night. The grain gone prices what is left.",
                _ => $" And {Raids} times since you walked in they have come by night. Every raid prices the bread the dearer.",
            };
            // The roster read from the doors (D-111): the stead speaks what it
            // can perceive of the dens' order, a risen voice over the fires or
            // none at all. The scar stays off this list; no stead can see it.
            string order = CampChief is { Rose: true, Epithet: { } risen }
                ? $" And the fires have a new voice over them since the old one fell: {risen}, the talk says now. A change of voice up there has never yet meant a change of appetite."
                : CampChief is null && World.Facts.Exists("nemesis", "chief")
                    ? " And no voice leads them now, if the night-fires are read right. Leaderless is not gone; it is only quieter about its plans."
                    : "";
            topics.Add(("The goblin raids", $"\"{grievance.Detail} We have fed them to keep the peace. It has not bought much peace.{raided}{crowded}{order}\""));
        }

        if (World.Facts.Find("rest_point", "shrine") is { } shrine)
            topics.Add(("The shrine", $"{shrine.Detail} \"Old past knowing. We keep it swept all the same.\""));

        if (World.Facts.Find("site", "waygate") is { } gate)
            topics.Add(("The black arch", gate.Detail + (CampCleared
                ? " \"They say it hums now. No one goes near to check.\""
                : " \"Shut as long as any here remember. Best left so.\"")));

        if (World.BarrowSite is { } barrowSite && World.Facts.Find("site", "barrow") is { } barrow)
        {
            // The mound topic reads the grudge aloud (D-106 follow-on, D-113):
            // while the mark stands, the stead speaks what it can perceive of
            // it, the lights grown taller and walking, and its own lore about
            // why a mound stirs. It never names the bearer; the dogs know more
            // than the stead does, and say as much.
            string mound = barrowSite.Cleared
                ? " \"Quiet up there now, first time in living memory. Whoever settled them, the stead owes a debt it cannot name.\""
                : Grudge > 0
                    ? " \"None go up, and of late none need telling. The lights along the crest stand taller than they did, and they walk now, end to end, like something pacing a fence line. The old folk say a mound only stirs when something was carried off it, and that it keeps the count better than any steward. The dogs will not face that way at all any more. They start growling before you reach the gate.\""
                    : " \"None go up. Of late there are lights along the mound at night, and the dogs will not face that way.\"";
            topics.Add(("The long mound", barrow.Detail + mound));
        }

        if (World.HollowSite is { } hollowSite && World.Facts.Find("site", "hollow") is { } hollowFact)
            topics.Add(("The stone ring", hollowFact.Detail + (hollowSite.Cleared
                ? " \"The fire up there is out. First time in anyone's memory. We are not sure we are glad.\""
                : " \"Leave it be, stranger. Whoever keeps that fire has kept it longer than the stead has stood.\"")));

        if (World.Facts.Find("wanderer", "npc_unbinder") is { } wanderer)
            topics.Add(("The wanderer", $"\"{wanderer.Detail} Not the first such to pass through, if the old folk are believed.\""));

        if (World.Facts.OfType("echo").FirstOrDefault() is { } echo)
        {
            // Standing heard from the stead's side (D-051): the higher the songs
            // carry the bearer, the less the singer pretends not to notice. In a
            // hushed world no echo fact exists, so the topic silences itself.
            string knowing = Standing >= 3
                ? " They sing it looking at the door you came in by. No one here thinks the walker is a stranger."
                : Standing >= 1
                    ? " And whoever taught it, the walker in it is about your height. Make of that what you like."
                    : "";
            topics.Add(("Old songs", $"\"There is a new one, though none can say who taught it. {echo.Detail}{knowing}\""));
        }

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
            topics.Add(("The refusal", "\"Would I do it again? Every dawn of every world. My name from before? Laid down with everything else; a name is a binding too, and I keep none. You want to know if the knife is clean. It is the cleanest thing I own. At the threshold it will be yours to take or wave away, and either answer will be yours. That is the entire point of me.\""));

        // The one permitted long thread (D-039, arc sec 9): the unfinished argument,
        // advanced a line at a time, the worldview unyielding in either branch.
        if (Player.Resolution == Resolution.Kept)
            topics.Add(("The argument", "\"You chose the fire, and you are still yourself, which spoils half my case and interests the other half. Endings still make the meaning, bearer. Keep your fire; I will keep asking what it is for. Same time next world.\""));
        else if (Player.Resolution == Resolution.Refused)
            topics.Add(("The argument", "\"You laid it down and kept your ward anyway: a middle way I did not walk and will not praise. But you chose it, and choosing is the whole of my creed, so I am obliged to nod. Same time next world.\""));

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
    /// The smith's own topics (D-041): a separate, small menu, so the villagers'
    /// nine digits stay whole and the forge always has room for its wares.
    /// </summary>
    private List<(string Label, string Answer)> BuildSmithTopics()
    {
        var topics = new List<(string, string)>
        {
            ("The forge", "\"Plough-iron, hinge-iron, nail-iron: that is the trade. But iron that has been down in the dark comes up asking to be something else, and I have learned not to argue with it.\""),
            ("Wearing iron", "\"An edge dulls and a jack splits: that is use, not fault. Bring them to me before they fail you, not after. The wheel and the awl put right what the work puts wrong.\""),
        };
        return topics;
    }

    /// <summary>
    /// The skald's own topics (D-054): a separate, small menu like the smith's.
    /// "Your name" is the meta-layer's reading surface: the one place the third
    /// ledger is read back to the bearer in numbers, because reading the count
    /// aloud is precisely a skald's work.
    /// </summary>
    private List<(string Label, string Answer)> BuildSkaldTopics()
    {
        var topics = new List<(string, string)>
        {
            ("The hall", "\"Raised before my grandmother's day, for keeping what will not keep in a granary. Every year the stead cuts its songs into the east wall, and I keep them in my chest besides. Walkers' verses too, when the road sends any worth the cutting.\""),
        };

        if (Player.Legend <= 0)
            topics.Add(("Your name", "\"No song carries you yet, stranger. That is not a lack; it is room. The wall has kept planks bare longer than you have been walking.\""));
        else if (Standing >= LegendStanding.MaxStanding)
            topics.Add(("Your name", $"\"The songs weigh what you have carried at {Player.Legend}, and there is no weighing past where you stand. You are {LegendStanding.TitleOf(Standing)}. The wall will be learning your verses long after both of us.\""));
        else
            topics.Add(("Your name", Standing == 0
                ? $"\"The songs weigh what you have carried at {Player.Legend}. Not yet a name; the first weighing tips at {LegendStanding.Threshold(1)}. Walk, and come read the wall again.\""
                : $"\"The songs weigh what you have carried at {Player.Legend}. In them you are {LegendStanding.TitleOf(Standing)}; the next weighing tips at {LegendStanding.Threshold(Standing + 1)}. The wall keeps room.\""));

        return topics;
    }

    /// <summary>
    /// The shrinekeeper's own topics (D-114): the first faith speaking as an
    /// institution. The stead's read of the shrine-power is the keeper's
    /// creed, and the harrow's claim is answered here, not in the villagers'
    /// nine digits, which a full world already fills. The last topic carries
    /// the rumor line: the custody question walking down the hill.
    /// </summary>
    private List<(string Label, string Answer)> BuildKeeperTopics()
    {
        return
        [
            ("The keeping", "\"Swept at dawn, tended at dusk, and never asked for more than that. What anchors here shelters us, and shelter is a gift. You do not bill a gift; you keep its house well. That is the whole of my office, and I want no deeper one.\""),
            ("The harrow", "\"The old hall up the valley. Their stone and ours were cut from one ring, that much is true, and they have kept their fire honestly. Where we part is what the power is. They say it holds an account, and everything given it is owed. We say it gives, and everything done for it is thanks. Same stone, two readings, and only one of them lets you sleep.\""),
            ("The harrow's claim", "\"They hold our stone went down the hill on loan, and that its keeping is theirs by right. Word at the well is their elder means to come down and say so, at the shrine itself, before the year turns. Let them come. The stone has stood our weather longer than any living memory of theirs, and it has not asked to go home.\""),
        ];
    }

    /// <summary>
    /// The harrow's topics (D-114): the second faith in its own voice. The
    /// elder carries the doctrine (the same power the shrine anchors, read as
    /// debt, not gift) and the founding's custody claim; the doorward keeps
    /// the door's shorter answers. The war, the aggressor, and the schism
    /// accounts are cast at template time, never here.
    /// </summary>
    private List<(string Label, string Answer)> BuildHarrowTopics(Npc npc)
    {
        if (npc.Role == "elder")
            return
            [
                ("The harrow", "\"Holy ground before the valley had a stead to name it. The ring stood, the fire was lit, and the rite has been said over the mother-stone every dusk since. We did not choose this ground. It was chosen, and we are what answering that looks like.\""),
                ("The daughter-stone", "\"The stead's shrine-stone came off our ring, cut and carried down with our own hands, in the founders' day. Lent, elder to daughter, as fire is lent from hearth to hearth. The stead has kept it swept; I grant that gladly. But sweeping is housekeeping, and what anchors in that stone is not a houseguest.\""),
                ("What is owed", "\"The stead says gift, and sleeps well. We say the power holds an account, as a river holds water: not out of spite, out of nature. What shelters you is spending something, and what is spent is owed. The rite pays a little of it every dusk. Their sweeping pays none. That is not wickedness in them; it is arithmetic they have declined to do.\""),
            ];
        return
        [
            ("The door", "\"Fire and floor for any who come up civil, doctrine offered and never forced. I keep the door, the wood, and the raking of the ash. The elder keeps the rest.\""),
            ("The stead below", "\"Good folk, and half my kin among them. They read the stone one way at their shrine, and we read it another up here, and most years that is a difference for winter evenings. Most years.\""),
        ];
    }

    /// <summary>
    /// The cart's talk (D-124): a trader's topics, short and uncurious, because
    /// the peddler's whole trade stands on not asking where things come from.
    /// </summary>
    private static List<(string Label, string Answer)> BuildPeddlerTopics() =>
        [
            ("The road", "\"Longer every year, and the tolls no kinder. I walk it because a stead pays best for what it cannot make and a cart is the only thing that brings it. Ask me what I carry, not where I have been.\""),
            ("The cart", "\"Everything in it is for sale and nothing in it has a story, and that second part is a service I provide free with the first. You would be surprised what folk pay to have a thing become merely a thing.\""),
        ];

    /// <summary>
    /// The stead's trade surface (D-036): each seller offers what their role would
    /// actually have. Purchases are talk-menu entries, not a separate mode, and the
    /// menu stays open so buying twice is two key presses. The smith (D-041) sells
    /// the plain three, each printing its requirement, and mends what use has worn.
    /// </summary>
    private List<(TradeGood, string, string)> BuildOffers(Npc npc)
    {
        var offers = new List<(TradeGood, string, string)>();
        // While the levy stands (D-105) the larder's digit becomes the levy's
        // answer: label text only, the same slot, so no digit shifts (D-041).
        if (npc.Id == "npc_steadholder")
            offers.Add((TradeGood.Ration, "", LarderBarred
                ? "Buy a ration (the larder is barred to you)"
                : LevyStands
                    ? $"Answer the stead's levy ({SteadLevy.AnswerCoin} coin against a carted measure)"
                    : $"Buy a ration ({RationPrice} coin{(FriendsPrice ? ", a friend's price" : "")})"));
        // The herbwife keeps a stillroom (D-081): the second bench, proving the
        // wood's-edge pattern (D-071) generalizes. She is the simples' true
        // buyer, and pays the apothecary's price where the woodward pays a
        // middleman's at the wood's edge: a coin more a sprig for the walk in.
        // The wound-dressing moved in off her talk menu with it: in a full world
        // her topics alone fill eight of the nine shared digits, and the
        // stillroom is where that work would happen anyway.
        if (npc.Id == "npc_herbwife")
            offers.Add((TradeGood.Trade, "", "Trade at the stillroom"));
        // The woodward keeps a bench at the wood's edge (D-071): one talk digit that
        // opens a trade menu of its own, so the teaching (D-052) and the hunt's
        // hide-trade (D-070) share a counter with room to grow, and the villagers'
        // shared nine digits are never crowded by it.
        if (npc.Id == "npc_woodward")
            offers.Add((TradeGood.Trade, "", "Trade at the wood's edge"));
        if (npc.Kind == NpcKind.Smith)
        {
            // Sold pieces stay listed as owned rather than vanishing: menu digits
            // must never shift under a buyer's fingers (learned live, D-041).
            foreach (string id in GearCatalog.SmithStock)
            {
                var item = GearCatalog.Create(id);
                string what = item.Slot switch
                {
                    GearSlot.Weapon => $"arm +{item.Bonus}",
                    GearSlot.Ranged => $"looses +{item.Bonus}",
                    _ => $"wards {item.Bonus}",
                };
                string asks = item.Req > AttributeSet.Baseline ? $", {AttributeSet.NameOf(item.ReqAttr)} {item.Req}" : "";
                offers.Add((TradeGood.Gear, id, Player.OwnsGear(id)
                    ? $"{item.Name} (yours already)"
                    : $"{item.Name} ({item.Value}c, {what}{asks})"));
            }
            // The smith's teaching entry (D-052): always listed, like the stock,
            // so the repair entry's digit never shifts under a buyer's fingers.
            offers.Add((TradeGood.Lesson, LessonCatalog.IdOf(LessonId.TendedIron), LessonLabel(LessonId.TendedIron)));
            // The crushed hand's road back (D-098 stage 2): in the always-listed
            // block, before the conditional repair, so its digit never shifts.
            offers.Add((TradeGood.Brace, "", BraceLabel()));
            if (RepairPrice > 0)
                offers.Add((TradeGood.Repair, "", $"Have your gear seen to ({RepairPrice} coin)"));
        }
        // The patron's ladder (D-054): every deed always listed, in price order,
        // states and all, so the digits never shift under a patron's fingers.
        if (npc.Kind == NpcKind.Skald)
        {
            foreach (var def in PatronCatalog.All)
                offers.Add((TradeGood.Pledge, PatronCatalog.IdOf(def.Id), PledgeLabel(def)));
            // The haunted look's road back (D-098 stage 2): what followed the
            // bearer home is sung to rest where the songs live, and the walk
            // out to the hall is the pilgrimage.
            offers.Add((TradeGood.Laying, "", LayingLabel()));
            // The hearth game (D-108): town life's first activity, kept where
            // men drink and game already. Always listed, after the deeds, so
            // no digit shifts (D-041).
            offers.Add((TradeGood.Bones, "", $"A cast of knucklebones ({Knucklebones.Stake} coin the throw)"));
            // The standing round (D-123): carousing's small verb, at the same
            // hearth. Always listed with a state-read label, so no digit
            // shifts (D-041).
            offers.Add((TradeGood.Round, "", RoundStood
                ? "Stand the room a round (the room drank your health tonight)"
                : $"Stand the room a round ({Carousing.Price} coin)"));
        }
        // The cart's counter (D-124): the road's three digits. Bread at the
        // road's price (sold to anyone; the cart keeps no stead's books), the
        // hides a coin over the wood's-edge bench (D-025's arbitrage, first
        // stone), and the fence's digit, the one no counter in the stead has.
        if (npc.Kind == NpcKind.Peddler)
        {
            offers.Add((TradeGood.Ration, "", $"Buy a ration ({Peddling.RationPrice} coin, the road's price)"));
            offers.Add((TradeGood.Hide, "", HideSaleLabel()));
            offers.Add((TradeGood.Fence, "", FenceLabel()));
        }
        return offers;
    }

    /// <summary>The fence's entry (D-124): what the pack holds with a past, at the cart's uncurious rate.</summary>
    private string FenceLabel() => Player.Trinket > 0
        ? $"Sell what has a past ({Player.Trinket} at {Peddling.TrinketPrice}c, {Player.Trinket * Peddling.TrinketPrice} coin)"
        : "Sell what has a past (nothing in your pack has one)";

    /// <summary>A deed's offer label (D-054): the asking, the waiting, or the standing.</summary>
    private string PledgeLabel(PatronDeedDef def)
    {
        string name = $"{char.ToUpperInvariant(def.Name[0])}{def.Name[1..]}";
        if (Player.PatronDeeds.Contains(def.Id)) return $"{name} (it stands)";
        if (Player.PledgedDeeds.Contains(def.Id)) return $"{name} (pledged)";
        return $"Pledge {def.Name} ({def.Price} coin)";
    }

    /// <summary>
    /// A patron's pledge (D-054, paying D-025's patronage crossing): the coin is
    /// counted out now, into a chest it never comes back out of, which is what
    /// makes it a sink and not a purchase. The weighing waits for the crossing,
    /// where Legend is minted and nowhere else (D-048's one-home rule).
    /// </summary>
    private void TryPledgeDeed(string idStr)
    {
        var id = PatronCatalog.FromId(idStr);
        var def = PatronCatalog.Def(id);
        if (Player.PatronDeeds.Contains(id))
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"It stands already, in every hall your songs have reached. Go and see it; seeing is free.\"");
            return;
        }
        if (Player.PledgedDeeds.Contains(id))
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Pledged, and the chest holds it. The songs will carry it over the next crossing you make. Patience is the cheapest thing I sell.\"");
            return;
        }
        if (Player.Coin < def.Price)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"{char.ToUpperInvariant(def.Name[0])}{def.Name[1..]} asks {def.Price} coin, and you hold {Player.Coin}. The hall keeps; it does not lend.\"");
            return;
        }

        Player.Coin -= def.Price;
        Player.PledgedDeeds.Add(id);
        World.Facts.Add("pledge", PatronCatalog.IdOf(id), World.SettlementName,
            $"A walker has pledged {def.Name} at {World.SettlementName}'s songhall.");
        Log.Add(Turn, $"You count {def.Price} coin into the skald's chest against {def.Name}. ({Player.Coin} left)", LogTone.Reward);
        Log.Add(Turn, id switch
        {
            PatronDeedId.RaisedStone => $"{TalkNpc!.Name}: \"A stone, then. Stone is the plainest promise there is: it stands where it is put. The songs will see it put.\"",
            PatronDeedId.EndowedHearth => $"{TalkNpc!.Name}: \"A fed fire, then. Bread and warmth at your name for whoever the road uses hardest. That is the oldest verse in the hall, and you have just bought a line of it.\"",
            _ => $"{TalkNpc!.Name}: \"Your own account, cut as you give it. I warn you fairly: the singers will garble everything around it, and the one true verse will make the garble show. That is what it is for.\"",
        });
        if (!Player.PatronLineHeard)
        {
            Player.PatronLineHeard = true;
            Log.Add(Turn, "\"Coin into song. Of everything you have spent, bearer, that is the first spending I will hear again in another world. I am told I do not forget; now something else will do the keeping.\"", LogTone.Aegis);
        }
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc!));
    }

    /// <summary>A teaching entry's label (D-052): the asking before, the keeping after, and the boon named when the stead teaches its own (D-087).</summary>
    private string LessonLabel(LessonId id)
    {
        var def = LessonCatalog.Def(id);
        if (Player.HasLesson(id))
            return $"{char.ToUpperInvariant(def.Name[0])}{def.Name[1..]} (yours already)";
        return def.Price > 0 && SteadsTeaching
            ? $"Be shown {def.Name} (freely, to the stead's own)"
            : $"Be shown {def.Name} ({def.Price} coin)";
    }

    /// <summary>
    /// A mentor's teaching (D-052): coin buys a showing, once, and refusals never
    /// take coin. A lesson once shown is shown for good, so the entry stays
    /// listed and says so instead of vanishing (the D-041 menu rule).
    /// </summary>
    private void TryLearnLesson(string idStr)
    {
        var id = LessonCatalog.FromId(idStr);
        var def = LessonCatalog.Def(id);
        if (Player.HasLesson(id))
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"You have it. Shown once is shown; the rest is your own hands' business.\"");
            return;
        }
        // The stead's teaching (D-087): to the stead's own, the price is waved off
        // before the showing starts. The refusal of the coin is itself narrated,
        // so the boon is felt at the moment it pays (D-023's rule).
        int price = SteadsTeaching ? 0 : def.Price;
        if (Player.Coin < price)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Knowing has a price like anything else: {def.Price} coin, and you hold {Player.Coin}.\"");
            return;
        }
        if (price == 0 && def.Price > 0)
            Log.Add(Turn, $"{TalkNpc!.Name} pushes your coin back across with two fingers. \"Not from you. What this stead knows, its own are shown.\"", LogTone.Reward);

        Player.Coin -= price;
        Player.Lessons.Add(id);
        switch (id)
        {
            case LessonId.Gleaning:
                Log.Add(Turn, $"{TalkNpc!.Name} walks you along the hedge-line, pointing with a thumb: which bracken hides sweet roots, which bark means grubs beneath it, what the deer found first. \"The wood sets a table. Most walk past it.\"");
                Log.Add(Turn, "(The gleaning is yours: what the wood sets out will show along your walks. Step onto it to gather.)", LogTone.Reward);
                break;
            case LessonId.TendedIron:
                Log.Add(Turn, $"{TalkNpc!.Name} shows you the evening habit of iron: wax into the straps, a stone drawn once along the edge, the day's grit out of every rivet before it beds in. \"The wheel does the great mendings. This keeps them rare.\"");
                Log.Add(Turn, "(The tended iron is yours: resting will hold your gear back from the worst of its wear.)", LogTone.Reward);
                break;
            case LessonId.Stillcraft:
                Log.Add(Turn, $"{TalkNpc!.Name} walks you through the steeping with her hands over yours: which sprigs to bruise and which to leave whole, how slow is slow enough, when the green goes right. \"Any fire and a patient hour. The simples do the rest.\"");
                Log.Add(Turn, "(The stillcraft is yours: resting with sprigs enough in the satchel will steep a draught of your own, any shrine, any world.)", LogTone.Reward);
                break;
        }
        HearLessonLineOnce();
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc!));
    }

    /// <summary>The Aegis marks the fourth ledger exactly once (D-052).</summary>
    private void HearLessonLineOnce()
    {
        if (Player.LessonLineHeard) return;
        Player.LessonLineHeard = true;
        Log.Add(Turn, "\"Counts, choices, songs, and now what another's hands put into yours. None of it my doing. I begin to suspect I am the smallest part of my own work.\"", LogTone.Aegis);
    }

    /// <summary>
    /// The woodward's bench (D-071): the talk digit's trade menu, its own nine slots.
    /// The teaching that used to sit in the talk menu (D-052) moves here to make room
    /// for the hide-trade (D-070), and both are always listed so their digits hold.
    /// </summary>
    private List<(TradeGood, string, string)> BuildTradeOffers(Npc npc)
    {
        var offers = new List<(TradeGood, string, string)>();
        if (npc.Id == "npc_woodward")
        {
            offers.Add((TradeGood.Hide, "", HideSaleLabel()));
            offers.Add((TradeGood.Cook, "", CookLabel()));
            offers.Add((TradeGood.Herb, "", HerbSaleLabel()));
            offers.Add((TradeGood.Lesson, LessonCatalog.IdOf(LessonId.Gleaning), LessonLabel(LessonId.Gleaning)));
            // The stead's beast (D-100): the woodward keeps the byre's business
            // at the wood's edge on the stead's behalf. Appended and always
            // listed with a state-read label, so the older digits hold (D-041).
            offers.Add((TradeGood.Beast, "", MuleLabel()));
            // The stable (D-100 stage 2): one digit that cycles the gathered
            // beasts, put-up / lead-out / swap read from the state, no submenu.
            offers.Add((TradeGood.Stable, "", StableLabel()));
        }
        // The stillroom (D-081): the simples at their true price, and the
        // wound-dressing at the table where that work was always done.
        if (npc.Id == "npc_herbwife")
        {
            offers.Add((TradeGood.Herb, "", HerbSaleLabel()));
            offers.Add((TradeGood.Mending, "", MendLabel()));
            // The craft itself (D-090): appended, so the older digits hold (D-041).
            offers.Add((TradeGood.Draught, "", DraughtLabel()));
            offers.Add((TradeGood.Lesson, LessonCatalog.IdOf(LessonId.Stillcraft), LessonLabel(LessonId.Stillcraft)));
            // The taken eye's road back (D-098 stage 2): appended and always
            // listed, so the older digits hold (D-041's law).
            offers.Add((TradeGood.Surgery, "", SurgeryLabel()));
        }
        return offers;
    }

    /// <summary>The draught entry (D-090): the simples steeped, priced in sprigs, never in coin.</summary>
    private string DraughtLabel() =>
        Player.Draughts >= DraughtCap ? $"Have a hale-draught drawn (your satchel holds {DraughtCap})"
        : Player.Herb >= DraughtHerbs ? $"Have a hale-draught drawn ({DraughtHerbs} sprigs)"
        : $"Have a hale-draught drawn ({DraughtHerbs} sprigs; you carry {Player.Herb})";

    /// <summary>
    /// The stillroom steeps a draught (D-090): the herb lane's first sink. The
    /// craft is hers and costs nothing; the simples are the price, three sprigs
    /// to the vial, so the satchel finally has a use besides the scales.
    /// </summary>
    private void TryDrawDraught()
    {
        if (Player.Draughts >= DraughtCap)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Two vials is what a satchel keeps whole on the road. Drink one down and come back to me.\"");
            return;
        }
        if (Player.Herb < DraughtHerbs)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Bring me {DraughtHerbs} sprigs and the steeping is yours for the asking. You carry {Player.Herb}.\"");
            return;
        }

        Player.Herb -= DraughtHerbs;
        Player.Draughts++;
        Log.Add(Turn, $"{TalkNpc!.Name} strips your sprigs into the pot, steeps them slow, and pours the green of it off into a stoppered vial. \"Drink it where the road hurts. It knows its work.\" ({Player.Draughts} vial{(Player.Draughts == 1 ? "" : "s")} carried)", LogTone.Reward);
    }

    // The cure roads (D-098 stage 2, paying D-009's "surgeon, pilgrimage,
    // salve"): each scar's own way back to parity, on the bench it belongs to,
    // every entry always listed with a state-read label so no digit ever
    // shifts (D-041's law). The prices are dear on purpose: a mark is lived
    // with before it is bought off.

    /// <summary>The taken eye's entry at the stillroom (D-098 stage 2).</summary>
    private string SurgeryLabel() => Player.HasScar(ScarId.TakenEye)
        ? $"Have the eye seen to ({DeathsToll.EyeCureCoin} coin)"
        : "Have an eye seen to (yours are your own)";

    private void TryEyeSurgery()
    {
        if (!Player.HasScar(ScarId.TakenEye))
        {
            Log.Add(Turn, $"{TalkNpc!.Name} tilts your chin to the light, looks long, and lets it go. \"Both of those are your own. Come to me if the road ever says otherwise.\"");
            return;
        }
        if (Player.Coin < DeathsToll.EyeCureCoin)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"That work is my longest, and the simples for it dear: {DeathsToll.EyeCureCoin} coin, and you hold {Player.Coin}. I will not do it halfway.\"");
            return;
        }
        Player.Coin -= DeathsToll.EyeCureCoin;
        Player.Scars.Remove(ScarId.TakenEye);
        Log.Add(Turn, "The work takes the whole of an afternoon: her thinnest blade, three of the ranked simples steeped to a paste, and a stillness you did not know your body kept. When the cloth comes off, the world has its depth back.", LogTone.Reward);
        Log.Add(Turn, $"\"{AegisVoice.ScarMendedLine}\"", LogTone.Aegis);
    }

    /// <summary>The crushed hand's entry at the smith (D-098 stage 2, D-009's prosthetic hook).</summary>
    private string BraceLabel() => Player.HasScar(ScarId.CrushedHand)
        ? $"Have a brace forged for the hand ({DeathsToll.BraceCoin} coin)"
        : "Have a brace forged (your hands are whole)";

    private void TryForgeBrace()
    {
        if (!Player.HasScar(ScarId.CrushedHand))
        {
            Log.Add(Turn, $"{TalkNpc!.Name} turns your hands over once, like stock they will not be buying. \"Whole. Bring me a hand the road has ruined and we will talk iron.\"");
            return;
        }
        if (Player.Coin < DeathsToll.BraceCoin)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Jointed work, that. Thin iron, and a lot of measuring: {DeathsToll.BraceCoin} coin, and you hold {Player.Coin}.\"");
            return;
        }
        Player.Coin -= DeathsToll.BraceCoin;
        Player.Scars.Remove(ScarId.CrushedHand);
        Log.Add(Turn, "The smith measures the wrong-set knuckles twice and builds to the crookedness instead of against it: thin straps of worked iron, jointed where the hand forgot how, laced snug over the old break. You make a fist. It answers like a fist.", LogTone.Reward);
        Log.Add(Turn, "\"Wear it to bed. It grips steadier than the bone ever did; do not tell the bone.\"", LogTone.Info);
        Log.Add(Turn, $"\"{AegisVoice.ScarMendedLine}\"", LogTone.Aegis);
    }

    /// <summary>The haunted look's entry at the songhall (D-098 stage 2): the pilgrimage's end.</summary>
    private string LayingLabel() => Player.HasScar(ScarId.HauntedLook)
        ? $"Have what followed you sung to rest ({DeathsToll.LayingEssence} essence)"
        : "Have a haunting sung to rest (nothing follows you)";

    private void TryLayHaunting()
    {
        if (!Player.HasScar(ScarId.HauntedLook))
        {
            Log.Add(Turn, $"{TalkNpc!.Name} studies you a moment, unhurried. \"Nothing walks behind you that the songs would want. Keep it so.\"");
            return;
        }
        if (Player.Essence < DeathsToll.LayingEssence)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"A laying is paid in what deeds weigh, not in coin: {DeathsToll.LayingEssence} essence, and you carry {Player.Essence}. Go and do, then come back.\"");
            return;
        }
        Player.Essence -= DeathsToll.LayingEssence;
        Player.Scars.Remove(ScarId.HauntedLook);
        Log.Add(Turn, "The skald hears the whole of it out, and then sings: not a song about you, a song about it, named and measured and given its own verse to live in, where the songs keep what is done. The weight behind your eyes goes with the last line.", LogTone.Reward);
        Log.Add(Turn, $"\"{AegisVoice.ScarMendedLine}\"", LogTone.Aegis);
    }

    /// <summary>The stead's beast entry (D-100): always listed, the label reading the state (D-041).</summary>
    private string MuleLabel() =>
        Mount is not null ? "The mule (yours; it waits without)"
        : SteadRegard.RungFor(Regard) < SteadRegard.FriendRung
            ? $"Buy the stead's mule ({MountCatalog.MuleCoin} coin; a beast is sold to friends)"
            : $"Buy the stead's mule ({MountCatalog.MuleCoin} coin)";

    /// <summary>
    /// The stead's beast bought (D-100 stage 1): the mule falls in on the open
    /// land. A stead sells its own beast only to a friend of the stead: the
    /// regard ladder pays again, and no coin moves a stranger's answer.
    /// </summary>
    private void TryBuyMule()
    {
        if (Mount is not null)
        {
            Log.Add(Turn, "\"You have the stead's beast already. Feed it something green now and then; it has opinions.\"");
            return;
        }
        if (SteadRegard.RungFor(Regard) < SteadRegard.FriendRung)
        {
            Log.Add(Turn, "\"That mule was foaled on this land and it will not leave with a stranger. Be more than that to the stead, and we will talk.\"");
            return;
        }
        if (Player.Coin < MountCatalog.MuleCoin)
        {
            Log.Add(Turn, $"\"{MountCatalog.MuleCoin} coin for the mule, tack and temper included. Come back when your purse agrees.\"");
            return;
        }
        Player.Coin -= MountCatalog.MuleCoin;
        Mount = new Mount { Kind = MountKind.Mule, Pos = Player.Pos };
        PlaceMountBeside(Player.Pos);
        Log.Add(Turn, "The coin is counted and the halter changes hands. The mule looks you over the way the stead once did, and decides you will do.", LogTone.Reward);
        Log.Add(Turn, "It walks the open land at your side (open grass passes two strides to a key), waits above while you go below, and its saddlebags keep what you load there ('o' beside it). What the raiders' night finds tethered, it may take.", LogTone.Info);
        World.Facts.Add("beast", "mule", World.SettlementName,
            $"The stead sold its mule to the bearer: {World.SettlementName}'s own beast walks a friend's road now.");
    }

    /// <summary>The stable entry (D-100 stage 2): put-up, lead-out, or swap, read from the state (D-041).</summary>
    private string StableLabel() =>
        Stable.Count == 0
            ? Mount is { } lone ? $"The stable: put {lone.Name} up" : "The stable (nothing of yours stands in it)"
            : Mount is { } beside ? $"The stable: swap {beside.Name} for {Stable[0].Name} ({Stable.Count} stabled)"
            : $"The stable: lead {Stable[0].Name} out ({Stable.Count} stabled)";

    /// <summary>
    /// The stable's one digit (D-100 stage 2): with beasts on both sides it
    /// swaps, with one it puts up or leads out, and pressed again it cycles
    /// the roster in a fixed round. A stabled beast keeps its bags and is
    /// safe from the raiders' night: this is the parking the saddlebags' risk
    /// was priced against.
    /// </summary>
    private void TryStableSwap()
    {
        if (Mount is null && Stable.Count == 0)
        {
            Log.Add(Turn, "The stable stands empty of anything that answers to you.");
            return;
        }
        var incoming = Stable.Count > 0 ? Stable[0] : null;
        if (incoming is not null) Stable.RemoveAt(0);
        if (Mount is { } put)
        {
            Stable.Add(put);
            Log.Add(Turn, incoming is null
                ? $"{Cap(put.Name)} goes up in the stead's stable, bags and all. The raiders' night does not reach in here."
                : $"{Cap(put.Name)} goes up, and {incoming.Name} is led out into the light.", LogTone.Info);
        }
        else if (incoming is not null)
            Log.Add(Turn, $"{Cap(incoming.Name)} is led out into the light, and falls in at your side.", LogTone.Info);
        Mount = incoming;
        if (Mount is not null) PlaceMountBeside(Player.Pos);
    }

    /// <summary>
    /// The raiders' courser (D-100 stage 2): the storylet's grant. The camp's
    /// breaking left the stolen beast loose, and it answers to the deed's own
    /// hand: to the bearer's side if the road is clear, else to the stable.
    /// </summary>
    internal void GrantTheCourser()
    {
        var courser = new Mount { Kind = MountKind.Courser, Pos = Player.Pos };
        if (Mount is null)
        {
            Mount = courser;
            PlaceMountBeside(Player.Pos);
        }
        else
        {
            Stable.Add(courser);
            Log.Add(Turn, "With a beast at your side already, the courser is led up to the stead's stable to wait its turn.", LogTone.Info);
        }
        World.Facts.Add("beast", "courser", World.SettlementName,
            $"The raiders' stolen courser was given over to the one who broke the camp above {World.SettlementName}.");
    }

    /// <summary>The wound-dressing entry (D-081): priced when there is a wound to dress.</summary>
    private string MendLabel() => Player.WoundedTurns > 0
        ? $"Have the wound dressed ({MendPrice} coin)"
        : "Have a wound dressed (you are whole)";

    /// <summary>The hide-sale entry (D-071): what the bench will weigh, and for how much.</summary>
    private string HideSaleLabel() => Player.Hide > 0
        ? $"Sell your hides ({Player.Hide} at {HidePriceHere}c, {Player.Hide * HidePriceHere} coin)"
        : "Sell hides (none cured yet)";

    /// <summary>The herb-sale entry (D-074): what the satchel holds, at this buyer's price (D-081).</summary>
    private string HerbSaleLabel() => Player.Herb > 0
        ? $"Sell your herbs ({Player.Herb} at {HerbPriceHere}c, {Player.Herb * HerbPriceHere} coin)"
        : "Sell herbs (satchel empty)";

    private void OpenTradeMenu()
    {
        InTalkMenu = false;
        InTradeMenu = true;
        _tradeOffers.Clear();
        _tradeOffers.AddRange(BuildTradeOffers(TalkNpc!));
        Log.Add(Turn, TalkNpc!.Id == "npc_herbwife"
            ? $"{TalkNpc.Name} leads you into the stillroom, low-beamed and sharp with green smells, the simples hanging in ranked bunches to dry."
            : $"{TalkNpc.Name} leads you to the bench at the wood's edge, where the hides hang to cure and the wood's own lessons are kept.");
    }

    private void HandleTradeMenuKey(char key)
    {
        if (key >= '1' && key <= '0' + _tradeOffers.Count)
        {
            var (good, arg, _) = _tradeOffers[key - '1'];
            switch (good)
            {
                case TradeGood.Hide: TrySellHides(); break;
                case TradeGood.Cook: TryCook(); break;
                case TradeGood.Herb: TrySellHerbs(); break;
                case TradeGood.Lesson: TryLearnLesson(arg); break;
                case TradeGood.Mending: TryBuyMending(); break; // the stillroom's table (D-081)
                case TradeGood.Draught: TryDrawDraught(); break; // the steeping (D-090)
                case TradeGood.Surgery: TryEyeSurgery(); break; // the eye's road back (D-098)
                case TradeGood.Beast: TryBuyMule(); break;      // the stead's beast (D-100)
                case TradeGood.Stable: TryStableSwap(); break;  // the gathered beasts (D-100 stage 2)
            }
            // The labels move with the state (hides sold, lesson taken); rebuild so the
            // bench reads true, and the digits keep their order under the buyer's hand.
            _tradeOffers.Clear();
            _tradeOffers.AddRange(BuildTradeOffers(TalkNpc!));
            return;
        }

        InTradeMenu = false;
        Log.Add(Turn, TalkNpc!.Id == "npc_herbwife"
            ? $"You leave the stillroom. {TalkNpc.Name} turns back to her hanging simples."
            : $"You leave the bench. {TalkNpc.Name} turns back to the day's hides.");
        TalkNpc = null;
    }

    /// <summary>
    /// The hunt's sell path (D-071, paying off D-070): cured hides become coin the
    /// bearer's own hand earned, told apart from looted purse and from the dark's
    /// essence. Sold in a lot, since the bench weighs the whole bundle at once.
    /// </summary>
    private void TrySellHides()
    {
        if (Player.Hide == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Bring me hides and I will weigh them fair. You carry none the wood would thank you for.\"");
            return;
        }
        int hides = Player.Hide;
        int paid = hides * HidePriceHere;
        Player.Hide = 0;
        Player.Coin += paid;
        Log.Add(Turn, $"You lay {hides} hide{(hides == 1 ? "" : "s")} across the bench. {TalkNpc!.Name} runs a thumb over each, counts, and pays {paid} coin. ({Player.Coin} now)", LogTone.Reward);
        if (!Player.HideLineHeard)
        {
            Player.HideLineHeard = true;
            Log.Add(Turn, "\"Coin off the wilds, and none of it mine to give or take. A fifth ledger, then, and the first you filled with your own two hands and no leave from anyone. Keep at it.\"", LogTone.Aegis);
        }
    }

    /// <summary>
    /// The forage's sell path (D-074): foraged herbs become coin at the same bench the
    /// hides do, sold in a lot. The woodward takes them on to the mending-folk who want
    /// them; to the bearer it is one more of the wilds' ledgers, kept apart from the rest.
    /// </summary>
    private void TrySellHerbs()
    {
        if (Player.Herb == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Bring me the wood's simples and I will pay for them. Your satchel is empty of them today.\"");
            return;
        }
        int herbs = Player.Herb;
        int paid = herbs * HerbPriceHere; // the stillroom pays the apothecary's price (D-081)
        Player.Herb = 0;
        Player.Coin += paid;
        Log.Add(Turn, TalkNpc!.Id == "npc_herbwife"
            ? $"You empty {herbs} sprig{(herbs == 1 ? "" : "s")} onto the stillroom's table. {TalkNpc.Name} names each one without looking twice and pays {paid} coin, full worth. ({Player.Coin} now)"
            : $"You empty {herbs} sprig{(herbs == 1 ? "" : "s")} onto the bench. {TalkNpc.Name} sorts them with a herb-wife's quickness and pays {paid} coin. ({Player.Coin} now)", LogTone.Reward);
    }

    /// <summary>
    /// What a batch of raw meat cooks down to now (D-073): a ration a cut, and the
    /// Cooking skill squeezes extra meals from the same carcass, all bounded by what
    /// a walking body can carry, so a full larder cooks nothing and wastes no meat.
    /// </summary>
    private (int Meat, int Rations) CookPlan()
    {
        int room = Math.Max(0, RationCap - Player.Rations);
        int fromMeat = Math.Min(Player.RawMeat, room);
        int made = Math.Min(fromMeat + Player.Skills.Bonus(SkillId.Cooking), room);
        return (fromMeat, made);
    }

    /// <summary>The cook entry's label (D-073): what the fire will make of the meat in hand.</summary>
    private string CookLabel()
    {
        if (Player.RawMeat == 0) return "Cook raw meat (none to cook)";
        var (_, made) = CookPlan();
        return made == 0
            ? $"Cook raw meat ({Player.RawMeat} raw, larder full)"
            : $"Cook your raw meat ({Player.RawMeat} raw into {made} ration{(made == 1 ? "" : "s")})";
    }

    /// <summary>
    /// The first craft (D-073): raw meat off the hunt becomes carried rations at the
    /// woodward's fire, the Cooking skill fattening the yield, the way Hunting fattens
    /// the hide. Feeds two of the vision's payoffs, a skill and the larder (D-006).
    /// </summary>
    private void TryCook()
    {
        if (Player.RawMeat == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Bring me something off the wilds and we will put it to the fire. You carry no raw meat.\"");
            return;
        }
        var (meat, made) = CookPlan();
        if (made == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Your larder is full; cook it when you have room to carry it. Raw, it keeps a while yet.\"");
            return;
        }
        Player.RawMeat -= meat;
        Player.Rations += made;
        GainSkill(SkillId.Cooking);
        Log.Add(Turn, $"You spit {meat} cut{(meat == 1 ? "" : "s")} over the woodward's fire and come away with {made} ration{(made == 1 ? "" : "s")}. ({Player.Rations} carried, {Player.RawMeat} raw left)", LogTone.Reward);
    }

    private void TryBuyRation()
    {
        // The cart sells bread too (D-124), at the road's price, and to anyone:
        // the larder's bars and the levy are the stead's books, not the cart's.
        if (TalkNpc!.Kind == NpcKind.Peddler)
        {
            TryBuyRoadRation();
            return;
        }
        // The barred larder (D-086): a named thief is not sold bread. The refusal
        // is the rung's own currency, distinct from the price the raids move.
        if (LarderBarred)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Not to you. The stead knows where its loaves went, and my larder is not for the hand that took them.\"");
            return;
        }
        // The levy's answer (D-105): while it stands the larder sells no bread
        // and takes the levy instead, coin against a carted measure. Answering
        // is a deed the stead perceives; enough answers lift the levy.
        if (LevyStands)
        {
            if (Player.Coin < SteadLevy.AnswerCoin)
            {
                Log.Add(Turn, $"{TalkNpc!.Name}: \"The levy stands, and no bread leaves this board while it does. {SteadLevy.AnswerCoin} coin answers it a measure; less buys nothing here tonight.\"");
                return;
            }
            Player.Coin -= SteadLevy.AnswerCoin;
            Stores = Math.Min(SteadStores.Max, Stores + 1);
            Log.Add(Turn, $"You count {SteadLevy.AnswerCoin} coin onto the larder board, and by the next cart it is grain: a measure carried up to {World.SettlementName}'s lofts under your name.", LogTone.Reward);
            RaiseRegard(1, $"The tally at the well gains a mark, and the mark has your name by it. {World.SettlementName} knows who answered its levy.");
            if (Stores >= SteadLevy.LiftedAt) LiftLevy();
            return;
        }
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
        // The friend's price named once per stead (D-080), the first time it is taken.
        if (FriendsPrice && !_friendsPriceNamed)
        {
            _friendsPriceNamed = true;
            Log.Add(Turn, $"{TalkNpc!.Name}: \"A coin off for you, and no argument. The stead does not forget whose hand ended the raids.\"");
        }
    }

    /// <summary>
    /// Bread off the cart (D-124): the road's price, flat, and sold to anyone
    /// with the coin. To a bearer the larder has barred (D-086) this is the
    /// outcast's grocer, and the cart says so once, plainly: it prices bread,
    /// not conduct.
    /// </summary>
    private void TryBuyRoadRation()
    {
        if (Player.Rations >= RationCap)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Your pack says no, whatever your mouth says. Eat some of it first.\"");
            return;
        }
        if (Player.Coin < Peddling.RationPrice)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"{Peddling.RationPrice} coin on the road, and you hold {Player.Coin}. The cart does not run a slate.\"");
            return;
        }

        Player.Coin -= Peddling.RationPrice;
        Player.Rations++;
        Log.Add(Turn, $"Twice-baked bread and a knot of sausage off the cart's board, dear as road food always is. ({Player.Rations} carried)", LogTone.Reward);
        if (LarderBarred && !_cartsBreadNamed)
        {
            _cartsBreadNamed = true;
            Log.Add(Turn, $"{TalkNpc!.Name}: \"The stead bars its larder to whom it likes. A cart keeps no such books; it prices bread, not conduct.\"");
        }
    }

    /// <summary>
    /// The fence (D-124): the crime family's missing buyer. The cart takes what
    /// has a past at a flat uncurious rate, all of it in one motion, and asks
    /// nothing, which is the whole service. The sale writes the stead's second
    /// secret fact from a deed (beside D-107's lifted purse): its heirlooms
    /// have gone away down the road, and no one knows whose hand sent them.
    /// </summary>
    private void TryFenceTrinkets()
    {
        if (Player.Trinket == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Show me a thing that cannot give an account of itself, and I will price the silence in. Your pack holds nothing of the kind today.\"");
            return;
        }

        int sold = Player.Trinket;
        int paid = sold * Peddling.TrinketPrice;
        Player.Trinket = 0;
        Player.Coin += paid;
        Log.Add(Turn, $"You set {sold} small thing{(sold == 1 ? "" : "s")} on the cart's board. {TalkNpc!.Name} turns each once in the light, asks nothing, and counts out {paid} coin. ({Player.Coin} now)", LogTone.Reward);
        Log.Add(Turn, "\"And now they are merely things. Whatever they were is between you and a mantel somewhere, and I was never part of it.\"");
        if (!World.Facts.Exists("secret", "fenced_goods"))
            World.Facts.Add("secret", "fenced_goods", World.SettlementName,
                $"Small things that once stood on mantels in {World.SettlementName} have gone away down the road in a peddler's cart, and no one knows whose hand sent them.");
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc!)); // the fence's label counts the pack
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

        // The clean dressing (D-052): the first bought mending teaches it. Hands
        // teach hands, and the mend the bearer paid for was the price of watching.
        if (!Player.HasLesson(LessonId.CleanDressing))
        {
            Player.Lessons.Add(LessonId.CleanDressing);
            Log.Add(Turn, $"{TalkNpc.Name} catches you watching and slows her hands so you can follow: how the cloth folds, where the pressure sits, which comes first.");
            Log.Add(Turn, "\"There. Hands teach hands, and you will not always fall where I can reach you.\"");
            Log.Add(Turn, "(The clean dressing is yours: eating while wounded now tends the wound as well.)", LogTone.Reward);
            HearLessonLineOnce();
        }
    }

    private void TryBuyGear(string id)
    {
        if (Player.OwnsGear(id))
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"You have its like already, and one is enough to keep. Wear the one you own; I will keep it honest.\"");
            return;
        }
        var item = GearCatalog.Create(id);
        if (Player.Coin < item.Value)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"That is {item.Value} coin of work, and you hold {Player.Coin}. Iron keeps; come back when your purse does.\"");
            return;
        }

        Player.Coin -= item.Value;
        Log.Add(Turn, $"{TalkNpc!.Name} takes your coin and puts the {item.Name} in your hands like it is being introduced to you.", LogTone.Reward);
        if (!item.MeetsReq(Player.Attributes))
            Log.Add(Turn, $"{TalkNpc.Name}: \"It asks more {AttributeSet.NameOf(item.ReqAttr).ToLowerInvariant()} than you carry yet. Wear it anyway, if you like. Iron is a patient teacher.\"");
        AcquireGear(item);
        // The stock shrinks by what was just bought; the mending entry may appear.
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc));
    }

    private void TryRepairGear()
    {
        int price = RepairPrice;
        if (price == 0)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Nothing here wants my wheel. Use it harder.\"");
            return;
        }
        // Smiths know their own (D-092): a smith's-hand is owed one mending
        // free, spent once ever, any world's forge.
        if (Player.Past == PastId.SmithsHand && !Player.SmithsFavorSpent)
        {
            Player.SmithsFavorSpent = true;
            foreach (var item in Player.AllGear) item.Wear = 0;
            Log.Add(Turn, $"{TalkNpc!.Name} looks at your hands, then at the work, and waves the coin away: \"Bellows-scars. This one's for the trade.\" What you carry is put right for nothing.", LogTone.Reward);
            _offers.Clear();
            _offers.AddRange(BuildOffers(TalkNpc));
            return;
        }
        if (Player.Coin < price)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"The mending is {price} coin, and you hold {Player.Coin}. Wear is honest debt; it will wait.\"");
            return;
        }

        Player.Coin -= price;
        foreach (var item in Player.AllGear) item.Wear = 0;
        Log.Add(Turn, $"{TalkNpc!.Name} works without hurry: the wheel for the edges, the awl and waxed thread for the rest. What you carry is put right for {price} coin.", LogTone.Reward);
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc));
    }

    private const string UnbinderFarewell = "Nothing needs to be counted. Walk well.";

    /// <summary>
    /// Sitting down to the bones (D-108): the stake goes on the board before
    /// the first cast, matched from the skald's own purse, and from there the
    /// coin is spoken for: there is no stepping away from a live board, only
    /// standing on what lies there. Turn-free like every menu; the game is an
    /// evening's texture, not a clock.
    /// </summary>
    private void TryPlayBones()
    {
        if (Player.Coin < Knucklebones.Stake)
        {
            Log.Add(Turn, $"The stake is {Knucklebones.Stake} coin, and you hold {Player.Coin}. {TalkNpc!.Name} smiles without unkindness: \"The bones do not roll on promises.\"");
            return;
        }

        Player.Coin -= Knucklebones.Stake;
        InTalkMenu = false;
        InBonesMenu = true;
        BonesRethrown = false;
        Log.Add(Turn, $"{TalkNpc!.Name} sweeps a board-end clear and sets {Knucklebones.Stake} coin against yours. \"The old game, then. Three bones, one throw back if you dare it, high board takes the pot.\"");
        CastBones();
    }

    private void CastBones()
    {
        _bonesCast.Clear();
        for (int i = 0; i < 3; i++) _bonesCast.Add(_combatRng.Range(1, 7));
        Log.Add(Turn, $"Your cast: {string.Join(", ", _bonesCast)}. The board reads {_bonesCast.Sum()}.");
    }

    /// <summary>
    /// The board's one choice (D-108): '2' sweeps the bones up for the one
    /// throw back; anything else stands on what lies there and lets the skald
    /// answer. Standing as the default keeps a live board escapable without
    /// ever refunding a committed stake.
    /// </summary>
    private void HandleBonesMenuKey(char key)
    {
        if (key == '2' && !BonesRethrown)
        {
            BonesRethrown = true;
            Log.Add(Turn, "You sweep the bones up and throw again. However they land now, they lie.");
            CastBones();
            return;
        }
        ResolveBones();
    }

    /// <summary>
    /// The skald's answer and the pot (D-108): the house plays its odds
    /// plainly (stands at its line, sweeps up anything under), so the game
    /// can be read and played against. The world's net rides a per-world
    /// ledger, and a stead this small talks when it runs steep either way.
    /// </summary>
    private void ResolveBones()
    {
        int mine = _bonesCast.Sum();
        var theirs = new List<int> { _combatRng.Range(1, 7), _combatRng.Range(1, 7), _combatRng.Range(1, 7) };
        Log.Add(Turn, $"{TalkNpc!.Name} casts: {string.Join(", ", theirs)}. The board reads {theirs.Sum()}.");
        if (theirs.Sum() < Knucklebones.SkaldStandsAt)
        {
            theirs = [_combatRng.Range(1, 7), _combatRng.Range(1, 7), _combatRng.Range(1, 7)];
            Log.Add(Turn, $"\"Not those.\" The skald sweeps them up and throws again: {string.Join(", ", theirs)}, for {theirs.Sum()}.");
        }

        if (mine > theirs.Sum())
        {
            Player.Coin += 2 * Knucklebones.Stake;
            BonesNet += Knucklebones.Stake;
            Log.Add(Turn, $"High board. The pot slides to your side of the table: {2 * Knucklebones.Stake} coin off the board. ({Player.Coin} carried)", LogTone.Reward);
            if (BonesNet >= Knucklebones.TalkedAboutAt && !World.Facts.Exists("game", "lucky_hand"))
            {
                World.Facts.Add("game", "lucky_hand", World.SettlementName,
                    $"At the hearth in {World.SettlementName} the bearer's knucklebones have run winner throw after throw, and the luck is being talked about.");
                Log.Add(Turn, "Luck like that gets counted in a stead this small.", LogTone.Info);
            }
        }
        else if (mine < theirs.Sum())
        {
            BonesNet -= Knucklebones.Stake;
            Log.Add(Turn, $"Low board. Your coin crosses the table without looking back. \"The bones owe no one,\" the skald says, not unkindly.", LogTone.Danger);
            if (BonesNet <= -Knucklebones.TalkedAboutAt && !World.Facts.Exists("game", "light_purse"))
                World.Facts.Add("game", "light_purse", World.SettlementName,
                    $"At the hearth in {World.SettlementName} the bearer's coin has crossed the board to the skald's purse, throw after throw.");
        }
        else
        {
            Player.Coin += Knucklebones.Stake;
            Log.Add(Turn, "Even boards. The stakes go home the way they came, and the game owes nothing either way.");
        }

        InBonesMenu = false;
        InTalkMenu = true; // the hearth stays warm: another throw is one digit away
    }

    /// <summary>
    /// Standing the room a round (D-123): carousing's small verb, an evening's
    /// warmth bought once per world. Deliberately no rung and no ledger: D-108
    /// set carousing aside precisely because a coin-for-regard dial cheapens
    /// the honest roads, so what the round buys is a fact and a warmer lane,
    /// the stead remembering who poured. Turn-free like every menu.
    /// </summary>
    private void TryStandRound()
    {
        if (RoundStood)
        {
            Log.Add(Turn, $"{TalkNpc!.Name} shakes their head, smiling. \"The room has drunk your health once tonight. One round an evening is generosity; two is campaigning.\"");
            return;
        }
        if (Player.Coin < Carousing.Price)
        {
            Log.Add(Turn, $"A round for the room is {Carousing.Price} coin, and you hold {Player.Coin}. {TalkNpc!.Name}: \"The hall pours on coin, walker. Goodwill is what the coin buys, not what it takes instead.\"");
            return;
        }

        Player.Coin -= Carousing.Price;
        RoundStood = true;
        Log.Add(Turn, $"You put {Carousing.Price} coin on the board and stand the room a round. ({Player.Coin} carried)", LogTone.Reward);
        Log.Add(Turn, "The horns go down the benches hand to hand, and the room warms by one clean measure: the talk loosens, somebody starts the old song about the miller's fence, and for a while the dark outside is only weather.", LogTone.Reward);
        Log.Add(Turn, BonesNet <= -Knucklebones.TalkedAboutAt
            ? $"{TalkNpc!.Name} pours yours last and grins. \"Stood on a purse my board has been emptying all evening. That is either character or stubbornness, and the room will drink to both.\""
            : $"{TalkNpc!.Name} pours yours last. \"A stood round is remembered here longer than the ale lasts. That is the whole trick of it.\"");
        World.Facts.Add("game", "round_stood", World.SettlementName,
            $"At the songhall in {World.SettlementName} the bearer stood the room a round, and the evening went warmer for it.");
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc!)); // the label reads the stood round now
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

        if (TalkNpc!.Kind is NpcKind.Villager or NpcKind.Smith or NpcKind.Skald or NpcKind.Peddler
            && key > '0' + _topics.Count && key <= '0' + _topics.Count + _offers.Count)
        {
            var (good, arg, _) = _offers[key - '1' - _topics.Count];
            switch (good)
            {
                case TradeGood.Ration: TryBuyRation(); break;
                case TradeGood.Mending: TryBuyMending(); break;
                case TradeGood.Gear: TryBuyGear(arg); break;
                case TradeGood.Repair: TryRepairGear(); break;
                case TradeGood.Lesson: TryLearnLesson(arg); break;
                case TradeGood.Pledge: TryPledgeDeed(arg); break;
                case TradeGood.Trade: OpenTradeMenu(); break;
                case TradeGood.Brace: TryForgeBrace(); break;   // the hand's road back (D-098)
                case TradeGood.Laying: TryLayHaunting(); break; // the look's road back (D-098)
                case TradeGood.Bones: TryPlayBones(); break;    // the hearth game (D-108)
                case TradeGood.Round: TryStandRound(); break;   // the standing round (D-123)
                // The cart's counter (D-124): hides at the road's coin, and the
                // fence's digit. The hide sale refreshes the menu itself here,
                // since the label counts the pack (the D-041 read-true rule).
                case TradeGood.Hide:
                    TrySellHides();
                    _offers.Clear();
                    _offers.AddRange(BuildOffers(TalkNpc!));
                    break;
                case TradeGood.Fence: TryFenceTrinkets(); break;
            }
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

    /// <summary>
    /// A storylet opens its scene (D-117). Called from the engine at fire time;
    /// the captures come along so the scene's prose can quote the same matched
    /// facts its storylet's lines could. A scene opened out of a conversation
    /// takes the conversation's place: the moment outranks the menu.
    /// </summary>
    internal void OpenScene(Scene scene, List<Fact> captures)
    {
        InTalkMenu = false;
        TalkNpc = null;
        InScene = true;
        _scene = scene;
        _sceneCaptures = captures;
        EnterSceneNode(scene.Nodes[0]);
    }

    /// <summary>
    /// Lands a node: its prose goes to the log (the log stays the one full
    /// transcript), its entry effect runs, and the panel state is rebuilt. The
    /// visible odds on each checked choice are read here, at entry, so the player
    /// commits to exactly the number they were shown.
    /// </summary>
    private void EnterSceneNode(SceneNode node)
    {
        SceneNode = node;
        _sceneProse.Clear();
        foreach (var (text, tone) in node.Lines)
        {
            string expanded = StoryletEngine.Expand(text, this, _sceneCaptures);
            _sceneProse.Add((expanded, tone));
            Log.Add(Turn, expanded, tone);
        }
        node.OnEnter?.Invoke(this);

        _sceneChoices.Clear();
        foreach (var choice in node.Choices)
            _sceneChoices.Add((choice.Label, choice.Check is { } c
                ? $"{c.Name}, {(int)Math.Round(c.ChanceFor(this) * 100)} in 100"
                : ""));
    }

    /// <summary>
    /// Digits answer the open node; a checked choice rolls its shown odds on the
    /// gameplay stream and branches on the result. While choices stand, any other
    /// key is ignored: a scene is a moment, not a menu, and it waits to be
    /// answered. A node with no choices closes on any key.
    /// </summary>
    private void HandleSceneKey(char key)
    {
        var node = SceneNode!;
        if (node.Choices.Length == 0) { CloseScene(); return; }
        if (key < '1' || key > '0' + node.Choices.Length) return;

        var choice = node.Choices[key - '1'];
        Log.Add(Turn, $"> {choice.Label}", LogTone.Info);

        string next = choice.Next;
        if (choice.Check is { } check)
        {
            bool carried = _combatRng.Chance(check.ChanceFor(this));
            Log.Add(Turn, $"({check.Name}: {(carried ? "it carries." : "it fails.")})", LogTone.Info);
            if (carried && check.Skill is { } skill) GainSkill(skill);
            if (!carried) next = choice.FailNext;
        }

        if (next.Length == 0) { CloseScene(); return; }
        EnterSceneNode(_scene!.NodeById(next));
    }

    private void CloseScene()
    {
        InScene = false;
        _scene = null;
        SceneNode = null;
        _sceneCaptures = [];
        _sceneProse.Clear();
        _sceneChoices.Clear();
    }

    /// <summary>
    /// The terms of the crossing (D-047): covenants in the game's register. The
    /// arch will carry any burden freely taken up; what a burden buys is Legend
    /// and a louder echo, never raw power. Post-resolution the same menu is the
    /// bearer setting their own terms (arc sec 8): register, never mechanics.
    /// </summary>
    private void OpenCrossingMenu()
    {
        InCrossingMenu = true;
        _chosenOaths.Clear();
        Log.Add(Turn, "Terms are cut into the arch's iron, in a script that reads itself to you.", LogTone.Info);
        Log.Add(Turn, Player.Resolution switch
        {
            Resolution.Kept => "\"The terms of the crossing, keeper: yours to set now. Name the kindling as hard as you please; the count honors what is carried.\"",
            Resolution.Refused => "\"The old terms, bearer, and no commission behind them now. Take up any you will bear, for no reason but your own. I will keep the count of it all the same.\"",
            _ => "\"The old terms. Whoever cut them meant this: a crossing may be made harder, freely, and what is carried is counted. Take up any you will bear, or none.\"",
        }, LogTone.Aegis);
    }

    private void HandleCrossingMenuKey(char key)
    {
        int index = key - '1';
        if (index >= 0 && index < OathCatalog.All.Count)
        {
            var oath = OathCatalog.All[index];
            if (!_chosenOaths.Add(oath.Id)) _chosenOaths.Remove(oath.Id);
            return;
        }
        if (key == '>')
        {
            InCrossingMenu = false;
            CrossToNextWorld([.. OathCatalog.All.Select(o => o.Id).Where(_chosenOaths.Contains)]);
            AdvanceTurn();
            return;
        }
        InCrossingMenu = false;
        Log.Add(Turn, "You step back from the arch. The terms keep.", LogTone.Info);
    }

    private void HandleThresholdMenuKey(char key)
    {
        if (key == '1') { ResolveThreshold(kept: true); return; }
        if (key == '2') { ResolveThreshold(kept: false); return; }

        InThresholdMenu = false;
        Log.Add(Turn, "You step back from the fire.");
        Log.Add(Turn, "\"No clock runs in this room. It has waited an age; it will wait for you.\"", LogTone.Aegis);
    }

    /// <summary>
    /// The threshold choice (D-039, arc sec 8). Both answers resolve the mystery
    /// into a changed relationship; the guardrail is absolute: they differ in
    /// fiction, register, and flavor, never in a single mechanical number.
    /// </summary>
    private void ResolveThreshold(bool kept)
    {
        Player.Resolution = kept ? Resolution.Kept : Resolution.Refused;
        Player.ResolutionCycle = Cycle;
        InThresholdMenu = false;

        if (kept)
        {
            Log.Add(Turn, "You wave the knife away, and set your hands above the fire, on the keeping-stone, where an age of other hands has worn two smooth places exactly the size of yours.", LogTone.Info);
            Log.Add(Turn, "Nothing vast happens. The fire leans toward you the way a hearth leans toward whoever tends it, and the count settles into your keeping like a ledger changing hands: not taken from the Aegis, shared out of it.", LogTone.Info);
            Log.Add(Turn, "\"So. A keeper, and on your own terms: the crossing is the keeping, and we walk it as we always have. All is counted, bearer. It is your count now. Spend it as you see fit.\"", LogTone.Aegis);
            Log.Add(Turn, "The Unbinder pockets the knife without ceremony. \"Kept, then. By choice, which is the only way it was ever going to hold. I remain unconvinced, and I remain on the roads. We will argue again; I look forward to it.\"", LogTone.Info);
            Log.Add(Turn, "They bow, slightly, to the fire and not to you, and take the stair up at a walker's pace.", LogTone.Info);
        }
        else
        {
            Log.Add(Turn, "You wave the knife away, and you do not set your hands on the stone. You say it plainly to both of them: the commission ends here. Not with a cutting: with a laying down.", LogTone.Info);
            Log.Add(Turn, "The fire does not flare, and does not dim. It burns alone, as it has an age, and the aloneness settles into plain fact: unkept, unowed, and yours to walk away from. The deep worlds stay wild. You choose that with both eyes open.", LogTone.Info);
            Log.Add(Turn, "\"Then it is laid down, and the tithe ends with it: what we earn stays ours. I was forged for one errand, bearer, and I find, saying this, that I mind less than I was built to. We walk. That is the whole of the commission now.\"", LogTone.Aegis);
            Log.Add(Turn, "The Unbinder pockets the knife without ceremony. \"Half my road. The half I could not walk, for my ward would not walk it with me.\" Something in the courtesy has gone quiet and real. \"Keep each other, then. That is not advice I thought I would ever give.\"", LogTone.Info);
            Log.Add(Turn, "They bow, slightly, to you and not to the fire, and take the stair up at a walker's pace.", LogTone.Info);
        }

        // The mystery's resolution is never the final note (technique commitment 7):
        // the same pointer home, either way, toward the witnessed epilogue above.
        Log.Add(Turn, "\"Go up after them. There is a stead over your head that thinks you are only a stranger who was kind to it. That matters more than this room. Go and be that.\"", LogTone.Aegis);
    }

    /// <summary>
    /// The post-resolution meeting (D-045, arc sec 9): face to face with a severed
    /// one, the bearer holds the count now, and the choice the Unbinder always had
    /// is on the table. Turn-free like every menu; the moment holds its breath.
    /// Both resolutions get the same verb at the same price (arc sec 8 guardrail).
    /// </summary>
    private bool OpenLayingMenu(Monster keeper)
    {
        InLayingMenu = true;
        _layingTarget = keeper;
        Log.Add(Turn, "At arm's length it stops, mid-reach, the way a routine stops when something in it finally does not fit. It is looking at your collarbone. It is, perhaps, the first true thing it has seen in an age.", LogTone.Danger);
        Log.Add(Turn, Player.Resolution == Resolution.Kept
            ? "\"It sees the keeping on you, bearer. You hold what it is owed. You could close its count here, gently, if you choose. Or the old way. Yours to weigh now.\""
            : "\"It sees what you laid down, bearer, and cannot understand a laying-down it was never offered. You could offer it one, here, gently. Or the old way. Yours to weigh now.\"", LogTone.Aegis);
        return false;
    }

    private void HandleLayingMenuKey(char key)
    {
        InLayingMenu = false;
        var keeper = _layingTarget!;
        _layingTarget = null;

        if (key == '1')
        {
            _layingDeclined = true;
            Log.Add(Turn, "The old way, then. The moment closes like water over a dropped stone.", LogTone.Info);
            if (AttackMonster(keeper)) AdvanceTurn();
        }
        else if (key == '2')
        {
            LayDownSevered(keeper);
            AdvanceTurn();
        }
        else if (key == '3' && CanRestoreSevered)
        {
            RestoreSevered(keeper);
            AdvanceTurn();
        }
        else
        {
            Log.Add(Turn, "You step back from the moment. It holds its half of the silence, and does not follow it.", LogTone.Info);
        }
    }

    /// <summary>
    /// The gentle answer (D-045): the Unbinder's own act, in the bearer's hands.
    /// No essence changes hands: the count closes instead of transferring, and
    /// where it goes is register, never a number.
    /// </summary>
    private void LayDownSevered(Monster keeper)
    {
        keeper.Hp = 0;
        keeper.Intent = null;
        Player.SeveredUnbound++;

        Log.Add(Turn, "You take its hands. They are cold, and under yours they stop shaking. You speak its count out plainly: what it kept, what it carried, what it is owed. It listens the way dry ground takes rain.", LogTone.Info);
        Log.Add(Turn, "It comes apart the way a knot comes undone: nothing breaks. At the end there is a face, and the face is only tired, and then it is only gone.", LogTone.Info);
        Log.Add(Turn, Player.Resolution == Resolution.Kept
            ? "\"Its count is closed and carried, bearer. I have it; the fire will have it. Nothing about it was wasted, and now nothing about it is lost.\""
            : "\"Its count is closed and struck out, bearer: paid in full, owed to no fire and no ledger. That is what we could give it. I think it is what it wanted.\"", LogTone.Aegis);

        if (CurrentSite is { Cleared: false } site && !Monsters.Any(m => m.Alive && m.SiteId == site.Id))
        {
            site.Cleared = true;
            World.Facts.Add("deed", "severed_laid", World.SettlementName,
                "The keeper of the stone ring was laid down gently, and the fire in the ring went out with no one left to need it.");
            _storylets.TryFire(this, StoryletTrigger.DeedWritten);
        }
    }

    /// <summary>
    /// The rarest grace (D-060): offered only to a bearer who has resolved the
    /// threshold and already shown a keeper the gentle laying-down. Restoring is
    /// the harder thing, and it is spent once ever: a bearer walks the mercy road
    /// before they are trusted with the deeper mending.
    /// </summary>
    public bool CanRestoreSevered =>
        Player.Resolution != Resolution.None
        && Player.SeveredUnbound >= 1
        && Player.SeveredRestored == 0;

    /// <summary>
    /// The third answer (D-060): not the sword, not the gentle stop, but the thing
    /// the ward was forged for and never once turned on a severed one. The count is
    /// not closed; it is caught the way the ward catches its own bearer, and the
    /// mended one passes whole into the songs to walk the deep roads ahead. No
    /// essence changes hands: the grace, like the mercy, is not bought, and both
    /// resolutions reach it at the same price (arc sec 8 guardrail).
    /// </summary>
    private void RestoreSevered(Monster keeper)
    {
        keeper.Hp = 0;
        keeper.Intent = null;
        Player.SeveredRestored++;
        Player.SeveredRestoredCycle = Cycle;

        Log.Add(Turn, "You take its hands, and this time you do not close the count. You read it back whole, every world and every deed still owed and owing, and then you do the thing the shield was forged for and never once turned this way: you hold the far end of it, and you do not let go.", LogTone.Info);
        Log.Add(Turn, "It does not come apart. For the first time in an age something holds it, and the worn shape stops repeating. What was ground smooth stands up, and looks at you, and is gone the way a name is gone when it passes into a song: not lost. Sung.", LogTone.Reward);
        Log.Add(Turn, Player.Resolution == Resolution.Kept
            ? "\"Caught, keeper. I have it the way I have you: not to spend, to carry. It goes into the kindling with all that we finish, and the worlds below us will wake already knowing its face. That is not a mercy I was forged for. It is a better one.\""
            : "\"Caught, walker, on no fire's account but the songs' own. We will carry it the way we carry each other: by choosing to, each day, out loud. Not kept. Not laid down. Woven in.\"", LogTone.Aegis);

        if (CurrentSite is { Cleared: false } site && !Monsters.Any(m => m.Alive && m.SiteId == site.Id))
        {
            site.Cleared = true;
            World.Facts.Add("deed", "severed_restored", World.SettlementName,
                "The keeper of the stone ring was not laid down but mended: caught whole, and set into the songs to walk the deep roads ahead of the bearer who would not let it fall.");
            _storylets.TryFire(this, StoryletTrigger.DeedWritten);
        }
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
        // The clean dressing (D-052): a taught bearer's meal is also wound-craft,
        // so a wounded body at full strength still has a reason to eat.
        bool canDress = Player.WoundedTurns > 0 && Player.HasLesson(LessonId.CleanDressing);
        if (Player.Hp >= Player.EffectiveMaxHp && Player.Stamina >= Player.MaxStamina && !canDress)
        {
            Log.Add(Turn, "You are neither hurt nor winded; the ration keeps.");
            return false;
        }

        Player.Rations--;
        Player.Hp = Math.Min(Player.EffectiveMaxHp, Player.Hp + 6);
        Player.Stamina = Math.Min(Player.MaxStamina, Player.Stamina + 3);
        Log.Add(Turn, $"You eat, quickly, watching the shadows. Warmth comes back to your hands. ({Player.Rations} left)", LogTone.Info);
        if (canDress)
        {
            // Sixteen turns per ration: the herbwife's own rate, at the plain
            // price of bread. Convenience is the good; the arithmetic is hers.
            Player.WoundedTurns = Math.Max(0, Player.WoundedTurns - 16);
            Log.Add(Turn, Player.WoundedTurns == 0
                ? "While you chew you see to the wound as she showed you: clean cloth, firm hands. The last of its weight lifts; you are whole again."
                : $"While you chew you see to the wound as she showed you: clean cloth, firm hands. It eases. ({Player.WoundedTurns} turns of weight remain)", LogTone.Info);
        }
        return true;
    }

    /// <summary>Most vials the satchel keeps whole on the road (D-090).</summary>
    public const int DraughtCap = 2;

    /// <summary>Sprigs one hale-draught steeps from (D-090): the herb lane's first sink.</summary>
    public const int DraughtHerbs = 3;

    /// <summary>What a draught gives back: two meals' worth of blood, and a deep cut at the wound's weight.</summary>
    public const int DraughtHeal = 12;
    public const int DraughtWoundCut = 24;

    /// <summary>
    /// Drinks a hale-draught (D-090): the stillroom's craft spent where it was
    /// always meant to be spent, on the road, far from any table. Stronger than
    /// a meal at blood and wound alike, and it asks no appetite: medicine, not
    /// food. Costs the turn the stopper and the swallow cost.
    /// </summary>
    private bool DoDrink()
    {
        if (Player.Draughts == 0)
        {
            Log.Add(Turn, "You carry no draught. The stillroom steeps them, three sprigs to the vial.");
            return false;
        }
        if (Player.Hp >= Player.EffectiveMaxHp && Player.WoundedTurns == 0)
        {
            Log.Add(Turn, "You are neither hurt nor carrying a wound's weight; the vial keeps.");
            return false;
        }

        Player.Draughts--;
        Player.Hp = Math.Min(Player.EffectiveMaxHp, Player.Hp + DraughtHeal);
        Log.Add(Turn, $"You thumb the stopper and drink the draught down, bitter and green. Strength runs back along the bone. ({Player.Draughts} vial{(Player.Draughts == 1 ? "" : "s")} left)", LogTone.Info);
        if (Player.WoundedTurns > 0)
        {
            Player.WoundedTurns = Math.Max(0, Player.WoundedTurns - DraughtWoundCut);
            Log.Add(Turn, Player.WoundedTurns == 0
                ? "The simples find the wound and sit down in it. Its weight lifts whole; you are yourself again."
                : $"The simples find the wound and ease it deep. ({Player.WoundedTurns} turns of weight remain)", LogTone.Info);
        }
        return true;
    }

    /// <summary>
    /// Opens the pack (D-041). Costs no turn, like every menu: the fiction is a
    /// glance down at your own hands, not a rummage.
    /// </summary>
    private bool DoGearMenu()
    {
        if (!Player.AllGear.Any())
        {
            Log.Add(Turn, "You carry no gear. Your hands, and what the Aegis makes of them.");
            return false;
        }
        InGearMenu = true;
        return false;
    }

    private void HandleGearMenuKey(char key)
    {
        var items = Player.AllGear.ToList();
        if (key >= '1' && key <= '0' + items.Count)
        {
            var item = items[key - '1'];
            if (item == Player.Weapon || item == Player.Armor || item == Player.Bow)
            {
                Log.Add(Turn, $"The {item.Name} is already serving.");
                return;
            }
            EquipGear(item);
            return;
        }

        InGearMenu = false;
        Log.Add(Turn, "You close the pack.");
    }

    /// <summary>
    /// Opens the sheet ('c'): both ledgers of what you are, the seven the Aegis
    /// shapes and the four the body keeps (D-042). Costs no turn, like every
    /// menu: the fiction is a moment's honest self-regard.
    /// </summary>
    private bool DoSheet()
    {
        InSheetMenu = true;
        return false;
    }

    /// <summary>
    /// Threshold questions standing open: reached, and no answer taken (D-046).
    /// Derived entirely from uses and chosen knacks, so nothing here needs
    /// saving, resetting, or death handling.
    /// </summary>
    public IEnumerable<KnackChoice> OpenKnackChoices =>
        PerkCatalog.Choices.Where(c => Player.Skills.Level(c.Skill) >= c.Level
            && !c.Options.Any(o => Player.HasPerk(o.Id)));

    /// <summary>The question the sheet is currently putting, if any: one at a time.</summary>
    public KnackChoice? PendingKnack => OpenKnackChoices.FirstOrDefault();

    /// <summary>
    /// The sheet mostly just closes, but while a knack question is open its
    /// digits answer it (D-046). Choosing keeps the sheet open, so a second
    /// waiting question is put right away.
    /// </summary>
    private void HandleSheetMenuKey(char key)
    {
        var choice = PendingKnack;
        if (choice is not null && key >= '1' && key <= '0' + choice.Options.Length)
        {
            ChooseKnack(choice, choice.Options[key - '1']);
            return;
        }
        InSheetMenu = false;
    }

    private void ChooseKnack(KnackChoice choice, PerkDef perk)
    {
        Player.Perks.Add(perk.Id);
        Log.Add(Turn, $"{perk.ChosenLine} ({perk.Name} is yours, for good.)", LogTone.Reward);
        if (!Player.KnackLineHeard)
        {
            Player.KnackLineHeard = true;
            Log.Add(Turn, "\"The body's ledger keeps choices now, not only counts. Still none of my doing, bearer. It is becoming quite a book.\"", LogTone.Aegis);
        }
        // The first deep answer gets its own remark (D-055); the queue puts the
        // level-2 questions first, so this is never also the first knack.
        if (choice.Level >= 4 && !Player.DeepKnackLineHeard)
        {
            Player.DeepKnackLineHeard = true;
            Log.Add(Turn, "\"A second answer from the same hand. The craft is not widening, bearer; it is narrowing. I mean that as praise.\"", LogTone.Aegis);
        }
    }

    /// <summary>Wields or wears a pack item; whatever held the slot goes to the pack.</summary>
    private void EquipGear(GearItem item)
    {
        Player.Pack.Remove(item);
        var displaced = item.Slot switch
        {
            GearSlot.Weapon => Player.Weapon,
            GearSlot.Ranged => Player.Bow,
            _ => Player.Armor,
        };
        if (displaced is not null) Player.Pack.Add(displaced);
        switch (item.Slot)
        {
            case GearSlot.Weapon: Player.Weapon = item; break;
            case GearSlot.Ranged: Player.Bow = item; break;
            default: Player.Armor = item; break;
        }

        Log.Add(Turn, item.Slot switch
        {
            GearSlot.Weapon => $"You heft the {item.Name}. It settles into your grip like an argument won.",
            GearSlot.Ranged => $"You string the {item.Name} and hang it ready at your shoulder.",
            _ => $"You lace on the {item.Name}.",
        }, LogTone.Info);
        // The iron's verb (D-056), said once at the taking-up, so the hand
        // knows what it holds beyond the numbers.
        switch (item.Move)
        {
            case MoveVerb.Arc:
                Log.Add(Turn, "Iron this broad does not stop at one body: a full swing carries through everything at your side.", LogTone.Info);
                break;
            case MoveVerb.Answer:
                Log.Add(Turn, "An edge this quick answers for itself: any read blow you stand through is returned over the iron.", LogTone.Info);
                break;
            case MoveVerb.Reach:
                Log.Add(Turn, "Its length is a verb of its own: 't' levels the point at anything two strides out.", LogTone.Info);
                break;
        }
        if (!item.MeetsReq(Player.Attributes))
            Log.Add(Turn, $"It asks {AttributeSet.NameOf(item.ReqAttr)} {item.Req} of an arm that carries {Player.Attributes[item.ReqAttr]}. You can use it, badly, and it will tell you what to become.", LogTone.Info);
        if (!Player.GearLineHeard)
        {
            Player.GearLineHeard = true;
            Log.Add(Turn, "\"Iron of your own. Good. What I cannot turn aside, turn aside yourself.\"", LogTone.Aegis);
        }
    }

    /// <summary>
    /// Takes ownership of found or bought gear: an empty slot is filled on the
    /// spot; a full one sends it to the pack for the 'i' menu to sort out.
    /// </summary>
    private void AcquireGear(GearItem item)
    {
        bool slotEmpty = item.Slot switch
        {
            GearSlot.Weapon => Player.Weapon is null,
            GearSlot.Ranged => Player.Bow is null,
            _ => Player.Armor is null,
        };
        if (slotEmpty)
        {
            Player.Pack.Add(item);
            EquipGear(item);
        }
        else
        {
            Player.Pack.Add(item);
            Log.Add(Turn, $"The {item.Name} goes into your pack. (i to manage your gear)", LogTone.Info);
        }
    }

    /// <summary>Global rising cost per raise (D-014's Essence economy, Souls-style curve).</summary>
    public int NextRaiseCost => 10 + 5 * Player.Attributes.TotalRaises;

    /// <summary>
    /// The footing (D-094): 'x' cycles measured, pressing, guarded. A free
    /// resetting of the feet on quiet ground; under live steel it is an act the
    /// field can answer, and costs the turn (D-004's commitment, both ways).
    /// </summary>
    private bool DoStance()
    {
        Player.Stance = Player.Stance switch
        {
            Stance.Measured => Stance.Pressing,
            Stance.Pressing => Stance.Guarded,
            _ => Stance.Measured,
        };
        Log.Add(Turn, Player.Stance switch
        {
            Stance.Pressing => "You set your feet to press: blows given harder, guard held thinner.",
            Stance.Guarded => "You set your feet to guard: blows turned further, your own given softer.",
            _ => "You settle back to the measured footing: nothing given, nothing owed.",
        }, LogTone.Info);
        return LiveMonstersHere.Any();
    }

    /// <summary>What iron can meet (D-125): a swung blow. Not a charging mass, a falling stone, a cry, or the cold.</summary>
    private static bool Parryable(IntentKind kind) => kind is IntentKind.CrushingBlow or IntentKind.BarrowBlade
        or IntentKind.SunderingCut or IntentKind.GravenFist or IntentKind.ThroatLunge or IntentKind.SeaxStab
        or IntentKind.MeasuredCut or IntentKind.BoardCheck;

    /// <summary>
    /// The blow a parry could meet (D-125): an adjacent foe whose wind-up's
    /// shown mark sits on the bearer's own ground and falls this coming turn.
    /// The iron goes where the eye says, so a mark that lies (the thegn's
    /// feint below a keen read shows a neighbor cell) is a blow that cannot be
    /// met: the feint's teeth stay exactly as long as the read stays short.
    /// </summary>
    private Monster? ParryMark() => Mode != MapMode.Site ? null
        : Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id
            && m.Pos.Chebyshev(Player.Pos) == 1
            && m.Intent is { } i && i.TurnsUntilResolve <= 1 && i.TargetCell == Player.Pos
            && i.FeintCell is null && Parryable(i.Kind));

    /// <summary>A blow is shown at the bearer's feet that iron could meet (the sidebar's hint).</summary>
    public bool ParryOpen => ParryMark() is not null;

    /// <summary>
    /// The parry (D-125): 'a' against a blow shown at your own ground. The
    /// turn is committed to the guard, not the kill: the blow is turned aside
    /// whole, and the force of its own swing rocks the striker's guard harder
    /// than any blow of yours could. Costs less wind than a swing; a guard
    /// nobody menaces, or arms too spent to set one, refuse without the turn.
    /// </summary>
    private bool DoParry()
    {
        // The beaten-open guard (D-126): there is no guard left to set.
        if (Player.StaggerTurns > 0)
        {
            Log.Add(Turn, "Your guard is beaten open and your arms will not answer; your feet still will.", LogTone.Combat);
            return false;
        }
        if (ParryMark() is not { } foe)
        {
            Log.Add(Turn, "No blow is shown at your ground; there is nothing for a guard to meet.");
            return false;
        }
        if (Player.Stamina < GuardBreak.ParryCost)
        {
            Log.Add(Turn, "Your arms are too spent to set the guard; this one must be answered by feet.", LogTone.Combat);
            return false;
        }
        Player.Stamina -= GuardBreak.ParryCost;
        _parryTarget = foe;
        Log.Add(Turn, $"You set your guard on the shown line and hold your ground against the {foe.Name}.", LogTone.Combat);
        return true;
    }

    /// <summary>
    /// Pressure on the second bar (D-125). At the brim the guard breaks: the
    /// wind-up dies, the body staggers open two turns (shafts find it, feet
    /// and blows stop), and the riposte's door stands open until one melee
    /// blow takes it or the stagger runs out.
    /// </summary>
    private void RockGuard(Monster monster, int pressure)
    {
        if (!monster.Alive || monster.Dormant) return;
        monster.PostureDmg += pressure;
        if (monster.PostureDmg < monster.MaxPosture) return;
        monster.PostureDmg = 0;
        monster.GuardBroken = true;
        monster.ExposedTurns = 2;
        if (monster.Intent is not null) monster.Intent = null;
        Log.Add(Turn, $"The {monster.Name}'s guard is beaten open: it staggers, arms wide, nothing between you and it.", LogTone.Reward);
    }

    /// <summary>
    /// The riposte (D-125): one melee blow through a broken guard lands 4
    /// deeper and spends the open door. Only a paid blow takes it; a winded
    /// tap through an open guard is still a winded tap.
    /// </summary>
    private int RiposteBonus(Monster target)
    {
        if (!target.GuardBroken || target.ExposedTurns == 0) return 0;
        target.GuardBroken = false;
        Log.Add(Turn, $"Your blow goes through the door the broken guard left open.", LogTone.Combat);
        return GuardBreak.RiposteBonus;
    }

    /// <summary>
    /// What a landed telegraphed blow puts on the bearer's own guard (D-126).
    /// Stones are thrown weight against the body, not force through the
    /// guard's line, so they rock nothing; the cry and the cold are not blows.
    /// </summary>
    private static int BearerPressure(IntentKind kind) => kind switch
    {
        IntentKind.SunderingCut or IntentKind.GravenFist or IntentKind.ThroatLunge => GuardBreak.BearerHeavy,
        IntentKind.CrushingBlow or IntentKind.BarrowBlade or IntentKind.SeaxStab
            or IntentKind.MeasuredCut => GuardBreak.BearerLight,
        _ => 0,
    };

    /// <summary>
    /// The field reads the bearer (D-126, the other half of D-004's contract):
    /// committed force works a pressing bearer's thinner guard a point harder
    /// and a set guard a point less (a guarded stance can shrug a light blow
    /// whole), and a body holding its own wind-up is a body whose guard is
    /// already down, so the blow that finds it rocks a point deeper. At the
    /// brim the bearer's guard breaks: the held heave dies in the hands, the
    /// arms refuse for two turns, and every blow finds the open guard deeper.
    /// The feet keep working: retreat is the staggered bearer's whole answer.
    /// </summary>
    private void RockBearer(int pressure)
    {
        if (pressure <= 0 || Player.StaggerTurns > 0 || Player.Hp <= 0) return;
        pressure += Player.Stance switch { Stance.Pressing => 1, Stance.Guarded => -1, _ => 0 };
        if (Player.HeaveTarget is not null || Player.LevinTarget is not null) pressure += 1;
        if (pressure <= 0) return;
        Player.PostureDmg += pressure;
        if (Player.PostureDmg < Player.MaxPosture) return;
        Player.PostureDmg = 0;
        Player.StaggerTurns = GuardBreak.BearerStagger;
        if (Player.HeaveTarget is not null)
        {
            Player.HeaveTarget = null;
            Log.Add(Turn, "The wound-up blow dies in your own hands.", LogTone.Danger);
        }
        Log.Add(Turn, "Your guard is beaten open: your arms will not answer, and every blow will find you deeper. Your feet still will.", LogTone.Danger);
    }

    /// <summary>
    /// The one word to the one who walks with you (D-097): a single key whose
    /// meaning reads from the moment. Beside a hurt guest with the satchel to
    /// spare, it tends them; otherwise it sets or lifts their ground. Like the
    /// footing (D-094), the word itself is free off the fight and costs the
    /// turn under live steel; the tending is handwork and always costs it.
    /// </summary>
    private bool DoOrder()
    {
        // The word goes to the mortal first (D-099): a hurt friend outranks a
        // held working. With no guest walking, the shade takes the ground-word,
        // and with no one walking at all, the beast's saddlebags answer (D-100).
        // The wild fell pony (D-100 stage 2): bread offered on the high ground,
        // before every other meaning of the key: a taming in progress is not
        // interrupted by anyone's saddlebags.
        if (Mode == MapMode.Overworld && World.WildPonyPos is { } wildPos && wildPos.Chebyshev(Player.Pos) == 1)
            return DoFeedWildPony(wildPos);

        bool muleBeside = Mode == MapMode.Overworld && Mount is { } steed && steed.Pos.Chebyshev(Player.Pos) == 1;
        var fellow = Guest is { Alive: true } ? Guest : Shade;
        if (fellow is not { Alive: true } guest)
        {
            if (muleBeside) return DoSaddlebags();
            Log.Add(Turn, "No one walks with you.");
            return false;
        }

        if (guest.Role != GuestRole.Shade
            && guest.Pos.Chebyshev(Player.Pos) == 1 && guest.Hp < guest.MaxHp
            && (Player.Draughts > 0 || Player.Herb > 0 || Player.Rations > 0))
        {
            // The best of the satchel goes first: the stillroom's vial, the
            // wood's simples, then plain bread. Care costs you something real.
            int mended;
            string spent;
            if (Player.Draughts > 0) { Player.Draughts--; mended = 8; spent = "You tip a hale-draught between their teeth"; }
            else if (Player.Herb > 0) { Player.Herb--; mended = 4; spent = "You bind the worst of it with a good sprig from the satchel"; }
            else { Player.Rations--; mended = 2; spent = "You put bread in their hands and make them sit until it is eaten"; }
            guest.Hp = Math.Min(guest.MaxHp, guest.Hp + mended);
            guest.Beats++; // care spent banks a beat (D-097 stage 2)
            Log.Add(Turn, $"{spent}: {guest.Name} is mended {mended}. ({guest.Hp}/{guest.MaxHp})", LogTone.Reward);
            return true;
        }

        // The bank outranks the ground-word (D-100), never the tending: coin
        // is loaded standing beside the beast, whoever else walks.
        if (muleBeside) return DoSaddlebags();

        guest.Holding = !guest.Holding;
        Log.Add(Turn, (guest.Role, guest.Holding) switch
        {
            (GuestRole.Shade, true) => "\"Hold here.\" The shade stills where it stands, patient as the stone its word was cut in.",
            (GuestRole.Shade, false) => "\"With me.\" The shade drifts back to your shoulder.",
            (_, true) => $"\"Hold here.\" {guest.Name} plants their feet where they stand.",
            _ => $"\"With me.\" {guest.Name} falls in at your shoulder.",
        }, LogTone.Info);
        return LiveMonstersHere.Any();
    }

    /// <summary>
    /// Winning the wild pony (D-100 stage 2): bread, patience, and standing
    /// still, three times over. No coin and no bench: the third road in is
    /// the only one the stead has no hand in. Tamed with a beast already at
    /// the bearer's side, it walks itself down to the stead's stable.
    /// </summary>
    private bool DoFeedWildPony(Pos wildPos)
    {
        if (Player.Rations == 0)
        {
            Log.Add(Turn, "The fell pony watches your empty hands with an expert's patience, and does not move. It knows exactly what you have not brought.");
            return false;
        }
        Player.Rations--;
        World.WildPonyFed++;
        if (World.WildPonyFed < MountCatalog.PonyFeedings)
        {
            Log.Add(Turn, World.WildPonyFed == 1
                ? "You hold the bread out and look at the hills instead of at it. A long while later it is taken from your palm, with lips soft as a whisper. (1 of a patience of 3)"
                : "It comes two steps to meet the bread this time, and stands a moment after, breathing you in. (2 of a patience of 3)", LogTone.Reward);
            return true;
        }

        World.WildPonyPos = null;
        var pony = new Mount { Kind = MountKind.FellPony, Pos = wildPos };
        Log.Add(Turn, "The third bread is eaten against your chest, and when you turn away the pony turns with you: shaggy, sure-footed, and done pretending it was not lonely. The fell pony is yours.", LogTone.Reward);
        Log.Add(Turn, "It fears nothing it has already outlived: alone of the beasts, it will stand tethered at the uncanny mouths.", LogTone.Info);
        if (Mount is null)
        {
            Mount = pony;
            PlaceMountBeside(Player.Pos);
        }
        else
        {
            Stable.Add(pony);
            Log.Add(Turn, "With a beast at your side already, it walks itself down to the stead's stable, unhurried, as if it always meant to.", LogTone.Info);
        }
        World.Facts.Add("beast", "fell_pony", World.SettlementName,
            "A wild fell pony off the high ground gave itself over to the bearer, for bread and patience.");
        return true;
    }

    /// <summary>
    /// The saddlebags (D-100): one key beside the beast, a turn of handwork.
    /// A full purse goes in whole; an empty one takes the bags back out.
    /// What rides the beast does not fall with the bearer; what the raiders'
    /// night finds tethered while the bearer is below, it may take whole.
    /// </summary>
    private bool DoSaddlebags()
    {
        var steed = Mount!;
        int room = MountCatalog.BagsCap(steed.Kind) - steed.Bags;
        if (Player.Coin > 0 && room > 0)
        {
            int loaded = Math.Min(Player.Coin, room);
            steed.Bags += loaded;
            Player.Coin -= loaded;
            Log.Add(Turn, $"You count your coin into the saddlebags: {steed.Bags} rides with {steed.Name} now. What the beast carries does not fall with you; what the beast risks, it risks whole.", LogTone.Info);
            if (Player.Coin > 0)
                Log.Add(Turn, $"The courser's bags are a racer's tack, not a banker's: they hold no more, and {Player.Coin} stays in your own purse.", LogTone.Info);
            return true;
        }
        if (steed.Bags > 0)
        {
            Player.Coin += steed.Bags;
            steed.Bags = 0;
            Log.Add(Turn, $"You take the coin back out of the saddlebags: {Player.Coin} in your own purse, and your own risk again.", LogTone.Info);
            return true;
        }
        Log.Add(Turn, "The saddlebags hang empty, and so does your purse.");
        return false;
    }

    private bool DoRest()
    {
        if (!(Mode == MapMode.Overworld && CurrentMap[Player.Pos] == Terrain.Shrine))
        {
            Log.Add(Turn, "You may only rest where the Aegis anchors.");
            return false;
        }

        Player.Hp = Player.EffectiveMaxHp;
        Player.Stamina = Player.MaxStamina;
        Player.Focus = Player.MaxFocus;

        // The tended iron (D-052): a taught bearer's rest holds their gear back
        // from the worst of its wear. Only back to half: the deep wear is the
        // wheel's business, so the smith's sink keeps its bottom half whole.
        if (Player.HasLesson(LessonId.TendedIron))
        {
            bool tended = false;
            foreach (var item in Player.AllGear)
                if (item.Wear > item.MaxWear / 2)
                {
                    item.Wear = item.MaxWear / 2;
                    tended = true;
                }
            if (tended)
                Log.Add(Turn, "Before resting you see to your iron as the smith showed you: wax, stone, patience. The worst of the wear comes off; the deep wear waits for the wheel.", LogTone.Info);
        }

        // The stillcraft (D-090): a taught bearer's rest steeps a draught of
        // their own, any shrine, any world: the lesson's keep is independence.
        if (Player.HasLesson(LessonId.Stillcraft)
            && Player.Draughts < DraughtCap && Player.Herb >= DraughtHerbs)
        {
            Player.Herb -= DraughtHerbs;
            Player.Draughts++;
            Log.Add(Turn, $"While the shrine hums you steep the simples as she showed you: bruised, slow, patient. A draught of your own goes stoppered into the pack. ({Player.Draughts} vial{(Player.Draughts == 1 ? "" : "s")} carried)", LogTone.Reward);
        }

        // Fireside words (D-097 stage 2): a rest with the guest beside mends
        // them whole, banks a beat, and gives up a little of who they are, one
        // line at a time, in a fixed round so replay agrees.
        if (Guest is { Alive: true } friend)
        {
            friend.Hp = friend.MaxHp;
            friend.Beats++;
            string[] fireside =
            [
                "I have not slept a night away from my own bench in years. The wood will keep. It kept before me.",
                "My kin fed those dens a winter's worth of hides once, for peace. The peace lasted until the meat did.",
                "You sleep like something hunted, you know. I will watch a while. I am good at watching.",
                "When this is done I will go back to the bench and weigh hides, and no one will believe a word of it. That is fine. I will know.",
            ];
            Log.Add(Turn, $"{friend.Name}, at the fire: \"{fireside[(friend.Beats - 1) % fireside.Length]}\"", LogTone.Info);
        }

        // The held word rests too (D-099): soul-stuff re-knits where the
        // calling is kept, and says nothing at the fire. It has nothing to say.
        if (Shade is { } restingShade && restingShade.Hp < restingShade.MaxHp)
        {
            restingShade.Hp = restingShade.MaxHp;
            Log.Add(Turn, "At the fire's edge the shade fills back in, the way a shadow does when the light steadies.", LogTone.Info);
        }

        InShrineMenu = true;
        Log.Add(Turn, "You rest at the shrine. Warmth returns to you.", LogTone.Info);
        Log.Add(Turn, Player.Resolution == Resolution.None
            ? "\"Be still. Let me count what you have earned.\""
            : "\"Sit. Count with me; the count answers to you now.\"", LogTone.Aegis);
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
        // The beaten-open guard (D-126): the arms refuse, turn-free. The feet
        // still work, and the feet are the answer.
        if (Player.StaggerTurns > 0)
        {
            Log.Add(Turn, "Your guard is beaten open and your arms will not answer; your feet still will.", LogTone.Combat);
            return false;
        }
        // Under-requirement gear is usable, badly (D-015): the swing costs extra
        // wind on top of the halved edge the item itself reports.
        var weapon = Player.Weapon;
        var family = weapon?.Family ?? SkillId.Brawling;
        int staminaCost = 3
            - (family == SkillId.Blades && Player.HasPerk(PerkId.SpareMotion) ? 1 : 0)
            - (weapon is null && Player.HasPerk(PerkId.ShortPath) ? 1 : 0)
            + (weapon is not null && !weapon.MeetsReq(Player.Attributes) ? 1 : 0)
            // The crushed hand (D-098): knuckles that never set right pay a
            // breath more wind on every swing, whatever it holds.
            + (Player.HasScar(ScarId.CrushedHand) ? 1 : 0);
        int damage;
        SkillId? trained = null;
        if (Player.Stamina >= staminaCost)
        {
            Player.Stamina -= staminaCost;
            // The read moment (D-055): a body in its wind-up is a body already
            // spoken for. The answered cut and the caught arm collect on it.
            damage = _combatRng.Range(2, 5) + Player.MeleeBonus + (weapon?.EffectiveBonus(Player.Attributes) ?? 0)
                + Player.Skills.Bonus(family)
                + (family == SkillId.Blades && Player.HasPerk(PerkId.DrawnCut) ? 1 : 0)
                + (family == SkillId.Blades && Player.HasPerk(PerkId.AnsweredCut) && target.Intent is not null ? 2 : 0)
                + (family == SkillId.Hafted && Player.HasPerk(PerkId.TrueArc) ? 1 : 0)
                + (weapon is null && Player.HasPerk(PerkId.KnuckleAndBone) ? 2 : 0)
                + (weapon is null && Player.HasPerk(PerkId.CaughtArm) && target.Intent is not null ? 3 : 0);
            // The footing (D-094): pressing gives the blow 2 more, guarded 2 less, never below 1.
            damage = Math.Max(1, damage + Player.StanceBlow - (Player.ChilledTurns > 0 ? 2 : 0));
            // The riposte (D-125): a paid blow through a broken guard lands deeper.
            damage += RiposteBonus(target);
            // Only a full swing teaches (D-014's cost gating: this one was paid
            // for in wind and wear). Feeble flailing is free, and free is unfed.
            trained = family;
            // The kind grip (D-046) and the stropped edge (D-055): every second
            // counted swing spares the edge. Uses parity, so replay and the
            // wear ledger agree.
            bool edgeSpared = (family == SkillId.Hafted && Player.HasPerk(PerkId.KindGrip)
                    || family == SkillId.Blades && Player.HasPerk(PerkId.StroppedEdge))
                && Player.Skills.Uses(family) % 2 == 1;
            if (weapon is not null && !weapon.Worn && !edgeSpared)
            {
                weapon.Wear = Math.Min(weapon.MaxWear, weapon.Wear + NextWear());
                if (weapon.Worn)
                    Log.Add(Turn, $"The {weapon.Name}'s edge is gone: it lands like a bar of dull iron now. The smith's wheel would right it.", LogTone.Combat);
            }
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
            if (target.Dormant)
            {
                if (target.Kind == MonsterKind.Warder) RouseLeaguer(target);
                else
                {
                    target.Dormant = false;
                    Log.Add(Turn, "Grit sifts from the figure. The head grinds around to face you.", LogTone.Danger);
                }
            }
            // The guard rocked (D-125): only a paid blow carries the pressure.
            if (trained is not null) RockGuard(target, GuardBreak.BlowPressure);
            // The checked swing (D-055): a landed hafted blow breaks the wind-up
            // outright. Only a paid swing has the weight; feeble flailing checks
            // nothing.
            if (trained == SkillId.Hafted && Player.HasPerk(PerkId.CheckedSwing) && target.Intent is not null)
            {
                target.Intent = null;
                Log.Add(Turn, $"The weight staggers the {target.Name}; the blow it was raising dies unthrown.", LogTone.Combat);
            }
            // The answered step (D-095): the blade's whole craft is the counter.
            // A paid cut into a body whose wind-up marks your own ground carries
            // the feet with it: a half-step aside, off the marked cell.
            if (trained == SkillId.Blades && target.Intent is { } aimed && aimed.TargetCell == Player.Pos
                && SlipCell(target) is { } slip)
            {
                Player.Pos = slip;
                Log.Add(Turn, "Your cut answers the wind-up, and your feet answer with it: a half-step off the marked ground.", LogTone.Combat);
            }
            // The shove (D-095): bare knuckles are the grappler's craft. A paid
            // blow carries the body one full stride back when the ground gives;
            // against a wall it simply does not.
            if (trained == SkillId.Brawling && weapon is null)
            {
                int sx = Math.Sign(target.Pos.X - Player.Pos.X), sy = Math.Sign(target.Pos.Y - Player.Pos.Y);
                var back = target.Pos.Plus(sx, sy);
                if (CurrentMap.Walkable(back)
                    && !FellowAt(back)
                    && !Monsters.Any(m => m.Alive && m.SiteId == target.SiteId && m.Pos == back))
                {
                    target.Pos = back;
                    Log.Add(Turn, $"The blow carries the {target.Name} a full stride back.", LogTone.Combat);
                }
                else
                {
                    Log.Add(Turn, $"The {target.Name} is driven against what will not give, and held there a breath.", LogTone.Combat);
                    // The wall-slam (D-125, tracked since D-095): ground that
                    // will not give hands the shove's weight back through the
                    // body, and the guard takes it.
                    RockGuard(target, GuardBreak.SlamPressure);
                }
            }
        }
        else
        {
            HarvestRemains(target);
            // The follow-through (D-046): a hafted swing that finishes its foe
            // hands part of its wind back. Quiet; the wind bar says it.
            if (trained == SkillId.Hafted && Player.HasPerk(PerkId.FollowThrough))
                Player.Stamina = Math.Min(Player.Stamina + 2, Player.MaxStamina);
        }

        // The arc (D-056): a paid swing of broad iron carries through into
        // everything else at the bearer's side, at half its weight. One swing,
        // one wear, one counted use: the carry is the same blow still moving.
        if (trained is not null && weapon is { Move: MoveVerb.Arc })
        {
            foreach (var other in Monsters.Where(m => m.Alive && m != target
                && m.SiteId == CurrentSite!.Id && m.Pos.Chebyshev(Player.Pos) == 1).ToList())
            {
                int carry = Math.Max(1, damage / 2);
                other.Hp -= carry;
                if (other.Alive)
                {
                    Log.Add(Turn, $"The swing carries through into the {other.Name} for {carry}.", LogTone.Combat);
                    if (other.Dormant)
                    {
                        if (other.Kind == MonsterKind.Warder) RouseLeaguer(other);
                        else
                        {
                            other.Dormant = false;
                            Log.Add(Turn, "Grit sifts from the figure. The head grinds around to face you.", LogTone.Danger);
                        }
                    }
                }
                else
                {
                    HarvestRemains(other);
                }
            }
        }

        if (trained is { } skill) GainSkill(skill);
        return true;
    }

    /// <summary>
    /// The answered step's ground (D-095): a cell beside the bearer to slip to,
    /// preferring one that keeps the blade at the foe's reach, off the marked
    /// cell (the bearer's own), walkable and unclaimed. Deterministic order.
    /// </summary>
    private Pos? SlipCell(Monster target)
    {
        Pos? fallback = null;
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = Player.Pos.Plus(dx, dy);
            if (!CurrentMap.Walkable(p)) continue;
            if (FellowAt(p)) continue;
            if (Monsters.Any(m => m.Alive && m.SiteId == target.SiteId && m.Pos == p)) continue;
            if (p.Chebyshev(target.Pos) == 1) return p;
            fallback ??= p;
        }
        return fallback;
    }

    /// <summary>
    /// What a felled foe leaves behind, however it fell (melee or the loosed
    /// line, D-050). The dead hold little a living hand would spend, but they
    /// are dense with essence; a severed one is nothing else at all.
    /// </summary>
    private void HarvestRemains(Monster target)
    {
        int coin = target.Kind switch
        {
            MonsterKind.Wight => _combatRng.Range(0, 3),
            MonsterKind.Severed => 0,
            MonsterKind.Graven => _combatRng.Range(1, 5),
            MonsterKind.Hound => _combatRng.Range(1, 4),
            MonsterKind.Carl => _combatRng.Range(2, 6),
            MonsterKind.Boar => 0,
            MonsterKind.Warder => _combatRng.Range(2, 6),
            MonsterKind.Thegn => _combatRng.Range(3, 7),
            MonsterKind.Hart => 0,
            _ => _combatRng.Range(2, 7),
        };
        int essence = target.Kind switch
        {
            MonsterKind.Wight => 8,
            MonsterKind.Severed => 15,
            MonsterKind.Graven => 10,
            MonsterKind.Hound => 6,
            MonsterKind.Carl => 8,
            MonsterKind.Boar => 6,
            MonsterKind.Warder => 9,
            MonsterKind.Thegn => 12,
            MonsterKind.Hart => 0,
            _ => 5,
        };
        // The lean dark (D-051): the dark yields half its essence, rounded
        // against the bearer. Coin is unbothered: it was never the dark's to give.
        if (World.Oaths.Contains(OathId.LeanDark)) essence /= 2;
        Player.Coin += coin;
        Player.Essence += essence;
        // The hunt's own yield (D-070): a hart carries no essence and no purse, only
        // a hide for the woodward and meat for the pot, and the woodcraft of having
        // taken it. The Hunting skill fattens the take, a hide or two more the higher
        // it climbs. The skill-use is granted here, the one place every kill-path meets.
        bool game = target.Kind == MonsterKind.Hart;
        int hides = 0;
        if (game)
        {
            // The heathborn take one more from every kill worth skinning (D-092).
            hides = 1 + Player.Skills.Bonus(SkillId.Hunting) + (Player.Folk == FolkId.Heathborn ? 1 : 0);
            Player.Hide += hides;
            GainSkill(SkillId.Hunting);
            // The hunt yields raw cuts, not a road-ration (D-073): they cook into
            // rations at a fire, the hunting lane feeding the cooking lane. Uncapped
            // like the hide, since the cooking (bounded by the ration cap) is the sink.
            Player.RawMeat++;
        }
        // The war-boar still pays its field-ration (D-053), the meat taken in the
        // thick of a fight: only the dedicated hunt, the hart, yields raw cuts to
        // carry to a fire. The knife takes a ration if a walking body can hold one.
        bool meat = target.Kind == MonsterKind.Boar && Player.Rations < RationCap;
        if (meat) Player.Rations++;
        Log.Add(Turn, target.Kind switch
        {
            MonsterKind.Wight => $"The wight comes apart into grave-dust and quiet. You take {coin} coin and {essence} essence.",
            MonsterKind.Severed => $"The severed one comes apart slowly, almost gratefully. What it held pours into the Aegis: {essence} essence, and no coin at all.",
            MonsterKind.Graven => $"The graven man breaks along its chisel-lines and stands again as what it always was: quarry-stone. You take {coin} coin and {essence} essence.",
            MonsterKind.Hound => $"The iron hound drops mid-stride and lies still: a made thing, and whatever ran it has run out. You take {coin} coin and {essence} essence.",
            MonsterKind.Carl => $"The shield-carl folds down behind its board, a watch relieved at last. You take {coin} coin and {essence} essence.",
            MonsterKind.Boar => meat
                ? $"The war-boar goes down heavy enough to feel through your boots. No purse on a beast: you take {essence} essence, and the knife takes meat for the road. ({Player.Rations} carried)"
                : $"The war-boar goes down heavy enough to feel through your boots. No purse on a beast: you take {essence} essence, and leave more meat than a walking body can carry.",
            MonsterKind.Warder => $"The sling-warder sits down against the bank like a man at the end of a long watch, and does not get up. You take {coin} coin and {essence} essence.",
            MonsterKind.Thegn => $"The sword-thegn lowers its point and folds down without a sound, the way it did everything: unhurried, and at last relieved of a watch no one remembered setting. You take {coin} coin and {essence} essence.",
            MonsterKind.Hart => $"The hart drops at the end of its run. You take the hide{(hides > 1 ? $", {hides} good pieces," : "")} and the raw meat, to cook at a fire. ({Player.Hide} hides, {Player.RawMeat} raw meat)",
            _ => $"The {target.Name} falls. You take {coin} coin and {essence} essence.",
        }, LogTone.Reward);
        // The dens count their dead (D-078): every raider felled deepens the
        // raiders' wrath, the enemy half of the ledger the stead's regard opened.
        if (target.Kind == MonsterKind.Goblin) RaiseWrath(1);
        // The named fall by name (D-110), and a chief slain over a standing
        // lieutenant hands the camp on: the succession is the roster's answer,
        // and the office comes with the grudge already in it.
        if (target.Epithet is { } fallen)
        {
            if (!target.Chief)
                Log.Add(Turn, $"That was {fallen}. The dens will say the name differently now.", LogTone.Info);
            else if (Monsters.FirstOrDefault(m => m.Alive && m.SiteId == target.SiteId && m.Epithet is not null) is { } heir)
            {
                heir.Chief = true;
                heir.Rose = true;
                heir.GrudgeSpoken = false;
                World.Facts.Add("nemesis", "risen", heir.Epithet!,
                    $"With {fallen} dead, {heir.Epithet} rose to chief of the camp above {World.SettlementName}, and the first thing the new chief owned was a grudge.");
                Log.Add(Turn, $"That was {fallen}, who led this camp. A howl goes up somewhere deeper in: {heir.Epithet} has just risen to a place with a grudge already in it.", LogTone.Danger);
            }
            else
                Log.Add(Turn, $"That was {fallen}, who led this camp, and no voice takes up the order after it. Whatever held this place together is bleeding out on the same ground.", LogTone.Reward);
        }
        // The bond banks its beats (D-097 stage 2): blood shared within reach
        // of each other, and every raider felled toward the huntsman's debt.
        if (Guest is { Alive: true } fellow)
        {
            if (fellow.Pos.Chebyshev(Player.Pos) <= 3) fellow.Beats++;
            if (target.Kind == MonsterKind.Goblin) fellow.Beats++;
        }
        CheckSiteCleared(CurrentSite!);
    }

    /// <summary>Live tenants standing within arm's reach: the shield-wall's head-count (D-055).</summary>
    private int FoesBeside() => CurrentSite is { } site
        ? Monsters.Count(m => m.Alive && m.SiteId == site.Id && m.Pos.Chebyshev(Player.Pos) == 1)
        : 0;

    /// <summary>How far a shaft flies: one cell short of a graven man's throw, so the pit is never outranged from safety.</summary>
    public const int BowRange = 8;

    /// <summary>What a loose asks in wind: a swing's price, lightened by the knack, taxed by an unmet requirement (D-015).</summary>
    private int LooseCost => 3
        - (Player.HasPerk(PerkId.LightDraw) ? 1 : 0)
        + (Player.Bow is { } bow && !bow.MeetsReq(Player.Attributes) ? 1 : 0);

    /// <summary>
    /// The bearer's ranged verb (D-050), first key of two: 'f' sets the shaft
    /// and costs nothing; the next direction key looses along that line. The
    /// draw refuses without the wind to pay it: at range, unlike cornered in
    /// melee, keeping the shaft costs only tempo.
    /// </summary>
    private bool DoLoose()
    {
        if (Player.Bow is null)
        {
            Log.Add(Turn, "You have no bow to loose.");
            return false;
        }
        if (Mode != MapMode.Site)
        {
            Log.Add(Turn, "Nothing under this sky calls for a shaft.");
            return false;
        }
        if (Player.Stamina < LooseCost)
        {
            Log.Add(Turn, "You have not the wind to draw; the shaft stays on the string.", LogTone.Combat);
            return false;
        }

        InAim = true;
        Log.Add(Turn, "You set a shaft to the string. Choose a line; any other key lowers the bow.");
        return false;
    }

    private void HandleAimKey(char key)
    {
        InAim = false;
        if (CommandMap.Delta(CommandMap.FromKey(key)) is { } d)
        {
            LooseShaft(d.dx, d.dy);
            AdvanceTurn();
        }
        else
        {
            Log.Add(Turn, "You lower the bow, and keep the shaft.");
        }
    }

    /// <summary>How far the spear reaches (D-056): two strides, the length of the ash.</summary>
    public const int SpearReach = 2;

    /// <summary>
    /// What a thrust asks in wind (D-056): more than a swing, because the good
    /// bought is the two strides the world keeps. No knack lightens it; the
    /// unmet asking taxes it like any iron (D-015).
    /// </summary>
    private int ThrustCost => 4
        + (Player.Weapon is { } w && !w.MeetsReq(Player.Attributes) ? 1 : 0);

    /// <summary>
    /// The reach (D-056), first key of two: 't' sets the point and costs
    /// nothing; the next direction key sends the thrust down that line. Like
    /// the draw, a winded arm refuses outright: at reach, keeping the point
    /// up costs only tempo.
    /// </summary>
    private bool DoThrust()
    {
        // The beaten-open guard (D-126): the arms refuse, turn-free.
        if (Player.StaggerTurns > 0)
        {
            Log.Add(Turn, "Your guard is beaten open and your arms will not answer; your feet still will.", LogTone.Combat);
            return false;
        }
        if (Player.Weapon is not { Move: MoveVerb.Reach })
        {
            Log.Add(Turn, "You hold nothing with that kind of reach.");
            return false;
        }
        if (Mode != MapMode.Site)
        {
            Log.Add(Turn, "Nothing under this sky stands off at the spear's length.");
            return false;
        }
        if (Player.Stamina < ThrustCost)
        {
            Log.Add(Turn, "You have not the wind to keep the point up; the spear stays couched.", LogTone.Combat);
            return false;
        }

        InThrust = true;
        Log.Add(Turn, "You level the spear. Choose a line; any other key lowers the point.");
        return false;
    }

    private void HandleThrustKey(char key)
    {
        InThrust = false;
        if (CommandMap.Delta(CommandMap.FromKey(key)) is { } d)
        {
            ThrustSpear(d.dx, d.dy);
            AdvanceTurn();
        }
        else
        {
            Log.Add(Turn, "You lower the point.");
        }
    }

    /// <summary>
    /// The thrust (D-056): a full swing at the spear's length. It stops at the
    /// first body or the first stone within two strides, pays wind and edge
    /// like the swing it is, and only a thrust that finds a body teaches. The
    /// board stops it like any far thing (D-053): a spear at reach is exactly
    /// what the linden was raised against.
    /// </summary>
    private void ThrustSpear(int dx, int dy)
    {
        var spear = Player.Weapon!;
        Player.Stamina -= ThrustCost;

        // The kind grip (D-046) spares the haft's every second counted use,
        // thrust or swing: the clock is the skill's, not the verb's.
        bool edgeSpared = Player.HasPerk(PerkId.KindGrip) && Player.Skills.Uses(SkillId.Hafted) % 2 == 1;
        if (!spear.Worn && !edgeSpared)
        {
            spear.Wear = Math.Min(spear.MaxWear, spear.Wear + NextWear());
            if (spear.Worn)
                Log.Add(Turn, $"The {spear.Name}'s point is rolled and dull: it pushes where it used to bite. The smith's wheel would right it.", LogTone.Combat);
        }

        var map = CurrentMap;
        var pos = Player.Pos;
        for (int step = 0; step < SpearReach; step++)
        {
            pos = pos.Plus(dx, dy);
            if (!map.Walkable(pos))
            {
                Log.Add(Turn, "The point checks against stone.", LogTone.Combat);
                return;
            }

            var target = Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == pos);
            if (target is null) continue;

            // A thrust from a resolved bearer's hand is still the old way
            // (D-045): the spear's two strides change nothing the shaft's
            // eight did not already say.
            if (target.Kind == MonsterKind.Severed && Player.Resolution != Resolution.None && !_layingDeclined)
            {
                _layingDeclined = true;
                Log.Add(Turn, "\"From this distance, then. The old way has a reach, bearer. So be it.\"", LogTone.Aegis);
            }

            // The board (D-053): a walking carl keeps its linden between you
            // and any far point, and a warder on the works keeps its own
            // (D-057). The wind and the edge are spent; nothing is bought or
            // taught. A point taken on a dormant warder's board is a sighting.
            if (target.Kind is MonsterKind.Carl or MonsterKind.Warder
                && target.Intent is null && target.ExposedTurns == 0 && !target.BoardBroken)
            {
                Log.Add(Turn, "The point drives into the linden board and is turned along the grain.", LogTone.Combat);
                if (target.Dormant) RouseLeaguer(target);
                return;
            }

            int damage = _combatRng.Range(2, 5) + Player.MeleeBonus + spear.EffectiveBonus(Player.Attributes)
                + Player.Skills.Bonus(SkillId.Hafted)
                + (Player.HasPerk(PerkId.TrueArc) ? 1 : 0);
            damage = Math.Max(1, damage + Player.StanceBlow - (Player.ChilledTurns > 0 ? 2 : 0)); // footing (D-094), grave-cold (D-096)
            damage += RiposteBonus(target); // the open door (D-125) is within the ash's reach
            target.Hp -= damage;

            if (target.Alive)
            {
                Log.Add(Turn, $"Your thrust takes the {target.Name} at the spear's length for {damage}.", LogTone.Combat);
                if (target.Dormant)
                {
                    if (target.Kind == MonsterKind.Warder) RouseLeaguer(target);
                    else
                    {
                        target.Dormant = false;
                        Log.Add(Turn, "Grit sifts from the figure. The head grinds around to face you.", LogTone.Danger);
                    }
                }
                // The guard rocked (D-125): a paid point is pressure like a paid edge.
                RockGuard(target, GuardBreak.BlowPressure);
                // The checked swing (D-055) has the same weight at the ash's
                // length: a paid landed blow breaks the wind-up outright.
                if (Player.HasPerk(PerkId.CheckedSwing) && target.Intent is not null)
                {
                    target.Intent = null;
                    Log.Add(Turn, $"The weight staggers the {target.Name}; the blow it was raising dies unthrown.", LogTone.Combat);
                }
            }
            else
            {
                HarvestRemains(target);
                // The follow-through (D-046) knows no distance: a hafted
                // killing blow hands part of its wind back.
                if (Player.HasPerk(PerkId.FollowThrough))
                    Player.Stamina = Math.Min(Player.Stamina + 2, Player.MaxStamina);
            }

            // Only a thrust that found a body teaches (D-014's cost gating,
            // the shaft's rule at the spear's length).
            GainSkill(SkillId.Hafted);
            return;
        }

        Log.Add(Turn, "The point finds only the air two strides out.", LogTone.Combat);
    }

    /// <summary>
    /// What a heave asks in wind (D-058): the dearest melee price, because the
    /// blow is the biggest single thing a hand can throw and the wind-up buys a
    /// turn's exposure with it. The unmet requirement taxes it like any iron
    /// (D-015); no knack lightens it.
    /// </summary>
    private int HeaveCost => 5
        + (Player.Weapon is { } w && !w.MeetsReq(Player.Attributes) ? 1 : 0);

    /// <summary>
    /// The heave (D-058), first key of two: 'w' with iron in hand sets the feet
    /// and costs nothing; the next direction key winds the blow at that line.
    /// Commitment runs both ways (D-004): what the field's telegraphs are to the
    /// bearer, this is to the field. Bare fists keep their verbs in the knacks
    /// (D-056), and only a body under this sky is worth so heavy a blow.
    /// </summary>
    private bool DoHeave()
    {
        // The beaten-open guard (D-126): the arms refuse, turn-free.
        if (Player.StaggerTurns > 0)
        {
            Log.Add(Turn, "Your guard is beaten open and your arms will not answer; your feet still will.", LogTone.Combat);
            return false;
        }
        if (Player.Weapon is null)
        {
            Log.Add(Turn, "A heave wants iron in the hand; bare fists have their own quickness, not this weight.");
            return false;
        }
        if (Mode != MapMode.Site)
        {
            Log.Add(Turn, "Nothing under this open sky is worth so heavy a blow.");
            return false;
        }
        if (Player.Stamina < HeaveCost)
        {
            Log.Add(Turn, "You have not the wind to wind it; the blow stays in your shoulder.", LogTone.Combat);
            return false;
        }

        InHeave = true;
        Log.Add(Turn, "You set your feet to wind a heavy blow. Choose a line; any other key eases off.");
        return false;
    }

    private void HandleHeaveKey(char key)
    {
        InHeave = false;
        if (CommandMap.Delta(CommandMap.FromKey(key)) is { } d)
        {
            CommitHeave(d.dx, d.dy);
            AdvanceTurn();
        }
        else
        {
            Log.Add(Turn, "You ease off, and keep your feet under you.");
        }
    }

    /// <summary>
    /// The wind-up (D-058): the wind is spent now and the cell is locked now,
    /// and the blow stands one turn, visible, for the field to answer before it
    /// falls. There is no taking it back; the next act looses it. This is the
    /// whole of the exposure, the mirror of a monster's declared intent.
    /// </summary>
    private void CommitHeave(int dx, int dy)
    {
        Player.Stamina -= HeaveCost;
        Player.HeaveTarget = Player.Pos.Plus(dx, dy);
        Log.Add(Turn, $"You wind the {Player.Weapon!.Name} up and back, all your weight gathering behind it. Everything here can see it come.", LogTone.Combat);
    }

    /// <summary>
    /// The heave loosed (D-058): the biggest single blow a hand throws, on the
    /// cell chosen a turn ago. A body that left the cell is a body the blow
    /// never touches, because a telegraph is dodged by feet, and the field's
    /// feet answer the bearer's as the bearer's answer the field. It pays a full
    /// swing's wear, hit or miss, and teaches only where it finds a body.
    /// </summary>
    private void ResolveHeave(Pos cell)
    {
        Player.HeaveTarget = null;
        var weapon = Player.Weapon!;
        var family = weapon.Family;

        // A full swing's wear and parity, hit or miss (the thrust's rule, D-056).
        bool edgeSpared = (family == SkillId.Hafted && Player.HasPerk(PerkId.KindGrip)
                || family == SkillId.Blades && Player.HasPerk(PerkId.StroppedEdge))
            && Player.Skills.Uses(family) % 2 == 1;
        if (!weapon.Worn && !edgeSpared)
        {
            weapon.Wear = Math.Min(weapon.MaxWear, weapon.Wear + NextWear());
            if (weapon.Worn)
                Log.Add(Turn, $"The {weapon.Name}'s edge is gone: it lands like a bar of dull iron now. The smith's wheel would right it.", LogTone.Combat);
        }

        var target = Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == cell);
        if (target is null)
        {
            Log.Add(Turn, "The heave comes down on ground gone empty and cracks it: a blow that big, spent on nothing.", LogTone.Combat);
            return;
        }

        int damage = _combatRng.Range(6, 12) + 2 * Player.MeleeBonus + 2 * weapon.EffectiveBonus(Player.Attributes)
            + Player.Skills.Bonus(family)
            + (family == SkillId.Blades && Player.HasPerk(PerkId.DrawnCut) ? 1 : 0)
            + (family == SkillId.Hafted && Player.HasPerk(PerkId.TrueArc) ? 1 : 0);
        damage = Math.Max(1, damage + Player.StanceBlow - (Player.ChilledTurns > 0 ? 2 : 0)); // footing (D-094), grave-cold (D-096)
        damage += RiposteBonus(target); // the open door (D-125), hit with everything
        target.Hp -= damage;

        if (target.Alive)
        {
            Log.Add(Turn, $"The heave lands full on the {target.Name} for {damage}.", LogTone.Combat);
            if (target.Dormant)
            {
                if (target.Kind == MonsterKind.Warder) RouseLeaguer(target);
                else
                {
                    target.Dormant = false;
                    Log.Add(Turn, "Grit sifts from the figure. The head grinds around to face you.", LogTone.Danger);
                }
            }
            // The guard rocked hardest by weight (D-125): the heave is the
            // biggest single thing a hand can throw, and the bar knows it.
            RockGuard(target, GuardBreak.HeavePressure);
            // The checked swing (D-055): a landed paid hafted blow breaks the
            // wind-up outright, at the heave's weight as at the swing's.
            if (family == SkillId.Hafted && Player.HasPerk(PerkId.CheckedSwing) && target.Intent is not null)
            {
                target.Intent = null;
                Log.Add(Turn, $"The weight staggers the {target.Name}; the blow it was raising dies unthrown.", LogTone.Combat);
            }
            // The sunder (D-095): a hafted heave is the board's one martial
            // answer. The linden splits for good, and the weight of the blow
            // staggers any wind-up it lands on, knack or no knack.
            if (family == SkillId.Hafted)
            {
                if (target.Kind is MonsterKind.Carl or MonsterKind.Warder && !target.BoardBroken)
                {
                    target.BoardBroken = true;
                    Log.Add(Turn, "The heave splits the linden board down its grain: what hangs on that arm will turn nothing again.", LogTone.Combat);
                }
                if (target.Intent is not null)
                {
                    target.Intent = null;
                    Log.Add(Turn, $"The sheer weight of it staggers the {target.Name} clean out of its wind-up.", LogTone.Combat);
                }
            }
        }
        else
        {
            HarvestRemains(target);
            // The follow-through (D-046): a hafted killing blow hands wind back,
            // heave or swing.
            if (family == SkillId.Hafted && Player.HasPerk(PerkId.FollowThrough))
                Player.Stamina = Math.Min(Player.Stamina + 2, Player.MaxStamina);
        }

        // Only a heave that found a body teaches (D-014's cost gating).
        GainSkill(family);
    }

    /// <summary>How far a said word carries (D-091): half a bowshot. The words are old, not long.</summary>
    public const int SpellRange = 4;

    /// <summary>How many turns the ward-word holds the air thick (D-091).</summary>
    public const int WardHeldTurns = 6;

    /// <summary>Turns between the pool gathering a point back on the road (D-091).</summary>
    public const int FocusRegenTurns = 8;

    /// <summary>How much of the pool the calling keeps bound while the shade walks (D-099): held, never spent.</summary>
    public const int CallingHold = 2;

    /// <summary>
    /// The workings ('z', D-091): opens what the bearer carries. Costs no turn;
    /// an empty head costs nothing either, only the telling of where words wait.
    /// </summary>
    private bool DoCast()
    {
        if (Player.Spells.Count == 0)
        {
            Log.Add(Turn, "You carry no workings. What words there are wait graven in the deep places.");
            return false;
        }
        InCastMenu = true;
        return false;
    }

    private void HandleCastMenuKey(char key)
    {
        InCastMenu = false;
        var known = Player.Spells;
        if (key < '1' || key > (char)('0' + known.Count))
        {
            Log.Add(Turn, "You let the words be.");
            return;
        }

        var def = SpellCatalog.Def(known[key - '1']);
        // Releasing is not saying (D-099): a walking shade is let go anywhere,
        // open sky or old dark, and the letting-go asks nothing of the pool.
        if (def.Id == SpellId.Calling && Shade is not null)
        {
            DismissShade();
            AdvanceTurn();
            return;
        }
        if (Mode != MapMode.Site)
        {
            Log.Add(Turn, "The words want the old dark they were cut in; under this open sky nothing answers them.");
            return;
        }
        if (SpendableFocus < def.Focus)
        {
            Log.Add(Turn, Shade is not null
                ? $"{Cap(def.Name)} asks {def.Focus} focus, and of the {Player.Focus} you hold, {CallingHold} stay bound to the calling. Let the shade go, or wait on the turns."
                : $"{Cap(def.Name)} asks {def.Focus} focus of the {Player.Focus} you hold. It gathers back with the turns, and whole at a rest.");
            return;
        }

        switch (def.Id)
        {
            case SpellId.Spark:
                _pendingLineSpell = SpellId.Spark;
                InCastLine = true;
                Log.Add(Turn, "The spark sits ready behind your teeth. Choose a line; any other key swallows it.");
                return;
            case SpellId.Levin:
                _pendingLineSpell = SpellId.Levin;
                InCastLine = true;
                Log.Add(Turn, "The levin gathers, wanting a line. Choose one; any other key lets it go.");
                return;
            case SpellId.Ward:
                CastWard();
                AdvanceTurn();
                return;
            case SpellId.Calling:
                CastCalling();
                AdvanceTurn();
                return;
            default:
                CastVeilsight();
                AdvanceTurn();
                return;
        }
    }

    private static string Cap(string s) => char.ToUpperInvariant(s[0]) + s[1..];

    private void HandleCastLineKey(char key)
    {
        InCastLine = false;
        var spell = _pendingLineSpell;
        _pendingLineSpell = null;
        if (CommandMap.Delta(CommandMap.FromKey(key)) is { } d)
        {
            if (spell == SpellId.Spark) CastSpark(d.dx, d.dy);
            else CommitLevin(d.dx, d.dy);
            AdvanceTurn();
        }
        else
        {
            Log.Add(Turn, "You swallow the word unsaid, and keep its focus.");
        }
    }

    /// <summary>
    /// The spark (D-091): the small word, said and gone in the same breath. It
    /// flies its short line like a shaft, but it is fire, not wood: a linden
    /// board is no answer to it, which is the caster's own lane past the
    /// shield-carls. Only a spark that found a body teaches (D-014's gate).
    /// </summary>
    private void CastSpark(int dx, int dy)
    {
        Player.Focus -= SpellCatalog.Def(SpellId.Spark).Focus;
        var map = CurrentMap;
        var pos = Player.Pos;
        for (int step = 0; step < SpellRange; step++)
        {
            pos = pos.Plus(dx, dy);
            if (!map.Walkable(pos))
            {
                Log.Add(Turn, "The spark cracks against stone and dies to an ember-smell.", LogTone.Combat);
                return;
            }

            var target = Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == pos);
            if (target is null) continue;

            int damage = _combatRng.Range(2, 5) + Player.SpellBonus + Player.Skills.Bonus(SkillId.Spellcraft);
            target.Hp -= damage;
            if (target.Alive)
            {
                Log.Add(Turn, $"The spark takes the {target.Name} in a snap of white fire for {damage}.", LogTone.Combat);
                WakeStruck(target, "The head grinds around, hunting the hand that burned it.");
            }
            else
            {
                HarvestRemains(target);
            }
            GainSkill(SkillId.Spellcraft);
            return;
        }

        Log.Add(Turn, "The spark flies its short line and gutters out on the dark.", LogTone.Combat);
    }

    /// <summary>
    /// The levin raised (D-091): the caster's own wind-up, the heave's mirror
    /// said instead of swung. The Focus is spent now and the ground is marked
    /// now: the first body on the line within the word's reach, or the line's
    /// far end. It stands one turn, visible, and the next act says it, hit or
    /// miss. A wound taken while it is held can knock it crooked: Will and
    /// Spellcraft are the grip (see ResolveLevin).
    /// </summary>
    private void CommitLevin(int dx, int dy)
    {
        Player.Focus -= SpellCatalog.Def(SpellId.Levin).Focus;
        var map = CurrentMap;
        var cell = Player.Pos;
        for (int step = 0; step < SpellRange; step++)
        {
            var next = cell.Plus(dx, dy);
            if (!map.Walkable(next)) break;
            cell = next;
            if (Monsters.Any(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == cell)) break;
        }
        if (cell == Player.Pos)
        {
            Log.Add(Turn, "Stone crowds the line before the word can mark it. The levin goes unsaid, its focus kept.", LogTone.Combat);
            Player.Focus += SpellCatalog.Def(SpellId.Levin).Focus;
            return;
        }

        Player.LevinTarget = cell;
        _hpAtLevinCommit = Player.Hp;
        Log.Add(Turn, "You raise the levin-word and hold it one breath from spoken. The air over the marked ground goes taut; everything here can feel where it will fall.", LogTone.Combat);
    }

    /// <summary>
    /// The levin said (D-091): the biggest working a mouth holds, on the ground
    /// marked a turn ago. A body that left the mark is a body the word never
    /// touches: a telegraph is dodged by feet, both ways, the heave's own rule.
    /// A wound taken while it was held threatens the saying: Will and Spellcraft
    /// keep the grip, or the word scatters, its focus spent on nothing.
    /// </summary>
    private void ResolveLevin(Pos cell)
    {
        Player.LevinTarget = null;

        if (Player.Hp < _hpAtLevinCommit)
        {
            double hold = Math.Clamp(
                0.5 + 0.1 * (Player.Attributes[Attr.Will] - AttributeSet.Baseline)
                    + 0.05 * Player.Skills.Level(SkillId.Spellcraft), 0.5, 0.95);
            if (!_combatRng.Chance(hold))
            {
                Log.Add(Turn, "The blow you took knocked the word crooked in your mouth: the levin scatters as heat and a smell of storms, spent on nothing.", LogTone.Combat);
                return;
            }
            Log.Add(Turn, "Hurt, and the word held anyway: your will keeps its grip on the gathered weight.", LogTone.Combat);
        }

        var target = Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == cell);
        if (target is null)
        {
            Log.Add(Turn, "The levin comes down and cracks the empty ground white. A word that big, spent on stone.", LogTone.Combat);
            return;
        }

        int damage = _combatRng.Range(7, 12) + 2 * Player.SpellBonus + Player.Skills.Bonus(SkillId.Spellcraft);
        target.Hp -= damage;
        if (target.Alive)
        {
            Log.Add(Turn, $"The levin comes down full on the {target.Name} for {damage}.", LogTone.Combat);
            WakeStruck(target, "Grit sifts from the figure. The head grinds around to face you.");
        }
        else
        {
            HarvestRemains(target);
        }
        GainSkill(SkillId.Spellcraft);
    }

    /// <summary>A struck sleeper wakes (D-040, D-057): the working's blow follows the iron's rule.</summary>
    private void WakeStruck(Monster target, string line)
    {
        if (!target.Dormant) return;
        if (target.Kind == MonsterKind.Warder) RouseLeaguer(target);
        else
        {
            target.Dormant = false;
            Log.Add(Turn, line, LogTone.Danger);
        }
    }

    /// <summary>
    /// The ward (D-091): the patient word. While it holds, every blow that
    /// lands is turned further (see Absorb), and a blow it actually turned is
    /// the only ward that teaches (D-014's gate, the Warding skill's own rule).
    /// </summary>
    private void CastWard()
    {
        Player.Focus -= SpellCatalog.Def(SpellId.Ward).Focus;
        Player.WardTurns = WardHeldTurns;
        Log.Add(Turn, "You say the ward-word, low. The air about you thickens faintly, like a held breath that is not yours.", LogTone.Info);
    }

    /// <summary>
    /// The veilsight (D-091): the quiet word. The floor gives up what moves on
    /// it: every kind named and read (the bestiary sharpened on the spot, D-059,
    /// restamped at this tier, D-061), and the pretenders shown for what they
    /// are, drawn true from here on. It teaches only when it truly sharpened a
    /// read: a floor already known cold has nothing left to school.
    /// </summary>
    private void CastVeilsight()
    {
        Player.Focus -= SpellCatalog.Def(SpellId.Veilsight).Focus;
        var here = Monsters.Where(m => m.Alive && m.SiteId == CurrentSite!.Id).ToList();
        Log.Add(Turn, "You say the veil-word, and for a heartbeat the dark forgets how to hold its shapes.", LogTone.Info);
        if (here.Count == 0)
        {
            Log.Add(Turn, "Nothing moves on this floor that your eye had not already found.", LogTone.Info);
            return;
        }

        bool sharpened = false;
        var kinds = here.GroupBy(m => m.Kind).OrderBy(g => (int)g.Key).ToList();
        Log.Add(Turn, "The floor gives up its living: " + string.Join(", ",
            kinds.Select(g => g.Count() == 1 ? $"one {g.First().Name}" : $"{g.Count()} of the {g.First().Name}'s kind")) + ".", LogTone.Reward);
        int feigning = here.Count(m => m.Dormant);
        if (feigning > 0 && !CurrentSite!.Unveiled)
            Log.Add(Turn, feigning == 1
                ? "And one of the still figures is not what it stands as. You will know it now."
                : $"And {feigning} of the still figures are not what they stand as. You will know them now.", LogTone.Danger);
        CurrentSite!.Unveiled = true;

        foreach (var group in kinds)
        {
            if (Player.Reads.GetValueOrDefault(group.Key) < Player.ReadKeen) sharpened = true;
            Player.WitnessTell(group.Key, Cycle);
        }
        if (sharpened)
        {
            Log.Add(Turn, "Their shapes settle into your reading: you will know their blows before they are thrown.", LogTone.Reward);
            GainSkill(SkillId.Spellcraft);
        }
    }

    /// <summary>
    /// The calling (D-099): the held word. Nothing is spent; while the shade
    /// walks, part of the pool stays bound to keeping it half-said (see
    /// SpendableFocus). The shade takes the guest engine whole: it follows,
    /// fights to its own measure, takes real blows, and trades places, and
    /// none of it is mortal: what ends it is an unraveling, never a death.
    /// </summary>
    private void CastCalling()
    {
        Shade = new Guest
        {
            Id = "shade",
            Name = "the shade",
            Role = GuestRole.Shade,
            Pos = Player.Pos,
            MaxHp = 10,
            Hp = 10,
        };
        PlaceFellowBeside(Shade, Player.Pos);
        Log.Add(Turn, "You say the calling, and stop it half-said. The dark beside you gathers, borrows a stance from you, and keeps it: a shade, standing, waiting on your road.", LogTone.Reward);
        Log.Add(Turn, $"While the word is held, {CallingHold} of your focus stay bound to it. Saying it again lets the shade go.", LogTone.Info);
        if (!Player.CallingLineHeard)
        {
            Player.CallingLineHeard = true;
            Log.Add(Turn, $"\"{AegisVoice.CallingLine}\"", LogTone.Aegis);
        }
        GainSkill(SkillId.Spellcraft);
    }

    /// <summary>The word released on purpose (D-099): no wound, no sound, the held focus loosened.</summary>
    private void DismissShade()
    {
        Shade = null;
        Log.Add(Turn, "You let the calling finish itself. The shade goes out of the world the way a breath goes, and the focus it held loosens in your chest.", LogTone.Info);
    }

    /// <summary>
    /// The shade broken by the field (D-099): the deliberate opposite of a
    /// guest's fall. No grave fact, no shame, no empty bench: soul-stuff given
    /// shape by a word owes the world nothing when the shape fails.
    /// </summary>
    private void ShadeUnravels()
    {
        Shade = null;
        Log.Add(Turn, "The shade frays to a smoke the dark takes back, and is gone. The focus it held loosens; nothing here mourns.", LogTone.Combat);
    }

    /// <summary>
    /// The loosed line (D-050): the shaft flies flat along one of the eight
    /// lines, stops at the first body or the first stone, and reaches eight
    /// cells at the furthest. Cover breaks it exactly as it breaks the graven
    /// men's throws: the pillars answer to both sides now.
    /// </summary>
    private void LooseShaft(int dx, int dy)
    {
        var bow = Player.Bow!;
        Player.Stamina -= LooseCost;

        // The string frays a little with every draw, hit or miss: wear is the
        // bow's whole ammunition, and the smith restrings it like any edge.
        // The waxed string (D-055) spares every second draw; the clock counts
        // draws, not marks, because that is what frays.
        bool stringSpared = Player.HasPerk(PerkId.WaxedString) && Player.Looses % 2 == 1;
        Player.Looses++;
        if (!bow.Worn && !stringSpared)
        {
            bow.Wear = Math.Min(bow.MaxWear, bow.Wear + NextWear());
            if (bow.Worn)
                Log.Add(Turn, $"The {bow.Name}'s string is frayed past trusting: it throws soft until the smith sees it.", LogTone.Combat);
        }

        var map = CurrentMap;
        var pos = Player.Pos;
        for (int step = 0; step < BowRange; step++)
        {
            pos = pos.Plus(dx, dy);
            if (!map.Walkable(pos))
            {
                Log.Add(Turn, "The shaft splinters against stone.", LogTone.Combat);
                return;
            }

            var target = Monsters.FirstOrDefault(m => m.Alive && m.SiteId == CurrentSite!.Id && m.Pos == pos);
            if (target is null) continue;

            // A shaft from a resolved bearer's hand is still the old way (D-045):
            // there is no gentle unmaking at the length of a pit.
            if (target.Kind == MonsterKind.Severed && Player.Resolution != Resolution.None && !_layingDeclined)
            {
                _layingDeclined = true;
                Log.Add(Turn, "\"From this distance, then. The old way has a reach, bearer. So be it.\"", LogTone.Aegis);
            }

            // The board (D-053): a walking carl keeps its linden between you
            // and it, and a warder on the works keeps its own (D-057). The
            // wind and the string are spent; nothing is bought or taught. The
            // board leaves its line only while its bearer's own blow or cast
            // is about its work, and in the blown turns after. A shaft taken
            // on a dormant warder's board is a sighting.
            if (target.Kind is MonsterKind.Carl or MonsterKind.Warder
                && target.Intent is null && target.ExposedTurns == 0 && !target.BoardBroken)
            {
                Log.Add(Turn, "The shaft thuds into the linden board and stands there, quivering.", LogTone.Combat);
                if (target.Dormant) RouseLeaguer(target);
                return;
            }

            // The picked moment (D-055): a body mid-motion, winding up or
            // standing open, takes the shaft 2 deeper. At range the moment is
            // yours to pick; the knack is the picking.
            int damage = _combatRng.Range(1, 4) + bow.EffectiveBonus(Player.Attributes)
                + Player.AimBonus + Player.Skills.Bonus(SkillId.Ranged)
                + (Player.HasPerk(PerkId.HuntersEye) ? 1 : 0)
                + (Player.HasPerk(PerkId.PickedMoment) && (target.Intent is not null || target.ExposedTurns > 0) ? 2 : 0);
            target.Hp -= damage;

            if (target.Alive)
            {
                Log.Add(Turn, $"Your shaft takes the {target.Name} for {damage}.", LogTone.Combat);
                if (target.Dormant)
                {
                    if (target.Kind == MonsterKind.Warder) RouseLeaguer(target);
                    else
                    {
                        target.Dormant = false;
                        Log.Add(Turn, "Grit sifts from the figure. The head grinds around, hunting the line the shaft flew.", LogTone.Danger);
                    }
                }
            }
            else
            {
                HarvestRemains(target);
            }

            // Only a shaft that found a body teaches (D-014's cost gating held
            // to its spirit: distance and stone would school a scarecrow).
            GainSkill(SkillId.Ranged);
            return;
        }

        Log.Add(Turn, "The shaft flies the length of its line and finds only distance.", LogTone.Combat);
    }

    /// <summary>
    /// Counts one real use and speaks up at each level line (D-042). Levels are
    /// derived from uses, so this is the only place growth can happen.
    /// </summary>
    private void GainSkill(SkillId id)
    {
        int before = Player.Skills.Level(id);
        Player.Skills.AddUse(id);
        int after = Player.Skills.Level(id);
        if (after == before) return;

        Log.Add(Turn, id switch
        {
            SkillId.Blades => $"The edge finds its line without being asked. (Blades rises to {after})",
            SkillId.Hafted => $"The haft has stopped arguing with your grip. (Hafted rises to {after})",
            SkillId.Brawling => $"Your fists have learned where the bones are not. (Brawling rises to {after})",
            SkillId.Ranged => $"The shaft goes where the eye went, not where the hand hoped. (Ranged rises to {after})",
            SkillId.Hunting => $"You read the ground now, the bent grass and the changed wind. (Hunting rises to {after})",
            SkillId.Cooking => $"The fire and the meat have stopped surprising you; more comes off the same carcass. (Cooking rises to {after})",
            SkillId.Survival => $"The wood keeps fewer secrets from you; your hands find the good growth without your eyes. (Survival rises to {after})",
            SkillId.Warding => $"You take the blow where the iron is thickest. (Warding rises to {after})",
            SkillId.Spellcraft => $"The words come steadier now, and cost you less of yourself to hold. (Spellcraft rises to {after})",
            SkillId.Sleight => $"Your fingers have learned the weight of a purse without asking the eyes. (Sleight rises to {after})",
            _ => $"Your craft deepens. ({SkillSet.NameOf(id)} rises to {after})",
        }, LogTone.Reward);

        if (!Player.SkillLineHeard)
        {
            Player.SkillLineHeard = true;
            Log.Add(Turn, "\"That was none of my doing. The body keeps a ledger of its own, bearer; I am only permitted to read it.\"", LogTone.Aegis);
        }

        // A threshold crossed opens its question (D-046). Announced here, put by
        // the sheet, and standing open forever until answered. The level-4 wave
        // (D-055) announces itself as what it is: the same craft, asked again.
        foreach (var choice in PerkCatalog.Choices)
            if (choice.Skill == id && before < choice.Level && after >= choice.Level
                && !choice.Options.Any(o => Player.HasPerk(o.Id)))
                Log.Add(Turn, choice.Level >= 4
                    ? $"Your {SkillSet.NameOf(id)} has deepened into a second question. The sheet ('c') will put it to you."
                    : $"Your {SkillSet.NameOf(id)} has settled into a question of style. The sheet ('c') will put it to you.", LogTone.Reward);
    }

    /// <summary>
    /// Armor's whole job (D-041): every hit taken is thinned by the worn piece,
    /// never below 1: iron helps, and nothing makes being hit free. Turning a
    /// blow is what wears the armor down.
    /// </summary>
    private int Absorb(int raw, bool telegraphed = false)
    {
        // The old blood (D-051): every tenant of the world strikes 1 deeper.
        // On the raw blow, before iron, so armor still earns its keep.
        if (World.Oaths.Contains(OathId.OldBlood)) raw += 1;
        // The beaten-open guard (D-126): while the stagger runs, every blow
        // finds the bearer deeper. On the raw blow, before iron, like the
        // footing: an open guard is the body's failure, not the armor's.
        if (Player.StaggerTurns > 0) raw += GuardBreak.OpenGuardDeeper;
        // The footing (D-094): guarded turns every landing blow 2 further, and
        // pressing bleeds 2 through. On the raw blow, iron or no iron: footing
        // is the body's own craft, and it guards the unarmored too.
        if (Player.StanceGuard != 0) raw = Math.Max(1, raw - Player.StanceGuard);
        var armor = Player.Armor;
        if (armor is null) return raw;
        // Warding is armor-craft, not toughness: it only helps while iron is
        // worn, and only a blow the iron actually turned teaches it (D-042).
        // The braced shoulder (D-046) reads the wind-up: a telegraphed blow
        // that still lands is turned 2 further. The shield-wall (D-055) reads
        // the crowd: each foe beside you past the first is turned 1 more, up
        // to 2; the fitted iron turns 1 more with no reading at all.
        int reduced = Math.Max(1, raw - armor.EffectiveBonus(Player.Attributes) - Player.Skills.Bonus(SkillId.Warding)
            - (telegraphed && Player.HasPerk(PerkId.BracedShoulder) ? 2 : 0)
            - (Player.HasPerk(PerkId.ShieldWall) ? Math.Min(2, Math.Max(0, FoesBeside() - 1)) : 0)
            - (Player.HasPerk(PerkId.FittedIron) ? 1 : 0));
        // The ward-word (D-091): while the air holds thick, every landing blow
        // is turned further, and only a blow the word actually turned teaches
        // the craft (the Warding skill's own gate, kept to the letter).
        if (Player.WardTurns > 0)
        {
            int warded = Math.Max(1, reduced - (2 + Math.Min(2, Player.SpellBonus) + Player.Skills.Bonus(SkillId.Spellcraft)));
            if (warded < reduced)
            {
                Log.Add(Turn, "The thickened air drinks a part of the blow.", LogTone.Combat);
                GainSkill(SkillId.Spellcraft);
            }
            reduced = warded;
        }
        if (reduced < raw)
        {
            // The mended strap (D-046): every second turned blow spares the
            // straps. Uses parity, same clock as the kind grip.
            bool strapSpared = Player.HasPerk(PerkId.MendedStrap)
                && Player.Skills.Uses(SkillId.Warding) % 2 == 1;
            if (!armor.Worn && !strapSpared)
            {
                armor.Wear = Math.Min(armor.MaxWear, armor.Wear + NextWear());
                if (armor.Worn)
                    Log.Add(Turn, $"The {armor.Name} hangs in cut batting now; it turns nothing more until it is mended.", LogTone.Combat);
            }
            GainSkill(SkillId.Warding);
        }
        return reduced;
    }

    /// <summary>
    /// The stead marks a deed it can see (D-076). Every rise is perceived at the
    /// moment it lands (D-023's rule: a change the player cannot feel does not fire),
    /// so the line names it, a crossed rung names the new standing, and the first
    /// regard ever earned draws the one line the Aegis keeps for it. Remote deep-site
    /// deeds pass no regard: the stead cannot perceive a quarry hushed leagues off.
    /// </summary>
    private void RaiseRegard(int amount, string line)
    {
        if (amount <= 0) return;
        // The haunted look (D-098): the stead's warmth comes harder to a face
        // something looks out of. Every gain of regard loses one, never below
        // one: the deeds still count, they just count colder.
        if (Player.HasScar(ScarId.HauntedLook)) amount = Math.Max(1, amount - 1);
        int rungBefore = SteadRegard.RungFor(Regard);
        _factionRegard[FactionId.Stead] = Regard + amount;
        int rungAfter = SteadRegard.RungFor(Regard);
        Log.Add(Turn, line, LogTone.Reward);
        if (rungAfter > rungBefore)
        {
            Log.Add(Turn, $"In {World.SettlementName} you are {SteadRegard.TitleOf(Regard)} now.", LogTone.Reward);
            // Every rung crossed is written to the graph (D-085): reputation
            // becomes a fact other content can require, the seam the rumor and
            // every richer boon gate through. World facts die with the world,
            // so the locality holds by construction.
            for (int rung = rungBefore + 1; rung <= rungAfter; rung++)
                World.Facts.Add("regard", rung switch { 1 => "known", 2 => "friend", _ => "own" },
                    World.SettlementName, $"In {World.SettlementName} the bearer is {SteadRegard.TitleOf(SteadRegard.Threshold(rung))}.");
        }
        // The friend's welcome (D-077): the first time a stead comes to hold the
        // bearer a friend, its folk press what they can spare on the one who has
        // stood for them. Regard only rises in a world, so this crosses exactly
        // once per stead. It is deed-earned, not name-carried, so the hushed name
        // (D-051) never silences it: the songs may go unsung and the gratitude still stands.
        if (rungBefore < SteadRegard.FriendRung && rungAfter >= SteadRegard.FriendRung)
            GiveFriendsWelcome();
        if (rungBefore < SteadRegard.OwnRung && rungAfter >= SteadRegard.OwnRung)
            NameSteadsTeaching();
        if (!Player.RegardLineHeard)
        {
            Player.RegardLineHeard = true;
            Log.Add(Turn, "\"A nearer weighing than mine, bearer. The songs carry your name between worlds; this is only these folk, this valley, this while. It does not cross the arch with you. It is the warmer of the two all the same.\"", LogTone.Aegis);
        }
    }

    /// <summary>
    /// The stead's own thanks (D-077), regard's first boon: deliberately the
    /// opposite number to Legend's arrival-welcome (D-048), which the songs set out
    /// before the bearer has lifted a hand. This is earned here, by these folk, for
    /// what was done under their own roof. A modest purse, coin the stead pooled
    /// between them: it always lands (D-023's rule, never a change the bearer cannot
    /// feel) and, unlike the regard that bought it, it is the bearer's to carry off,
    /// crossing the arch as coin does. Kept to coin, not bread, so it stays clear of
    /// the arrival-welcome's own larder rather than piling onto it.
    /// </summary>
    private void GiveFriendsWelcome()
    {
        // Suspicion holds the purse shut (D-086): a stead does not pool coin for a
        // hand it has watched in its own larders, whatever that hand has slain.
        // The withholding is narrated, per D-023's rule: a boon lost silently is a
        // change the player cannot perceive.
        if (SteadShame.RungFor(Shame) >= SteadShame.UnwelcomeRung)
        {
            Log.Add(Turn, $"There would be a purse for the one who did this, in another year. The folk of {World.SettlementName} look at you, and count their doors, and keep their coin.", LogTone.Info);
            return;
        }
        const int giftCoin = 5;
        Player.Coin += giftCoin;
        Log.Add(Turn, $"The folk of {World.SettlementName} gather what coin they can spare for the one who has stood for them: a purse of {giftCoin}, pressed on you with both hands.", LogTone.Reward);
    }

    /// <summary>
    /// The stead's teaching named (D-087), the own rung's boon: where the friend
    /// rung paid in this world's coin (the purse, the price), the top rung pays in
    /// the one coin that crosses the arch: know-how. Every showing the stead sells
    /// is shown freely to its own. The crossing narrates the opening (or, under
    /// suspicion, the withholding: never a change the bearer cannot feel, D-023),
    /// and the benches say it again in their own labels.
    /// </summary>
    private void NameSteadsTeaching()
    {
        if (SteadShame.RungFor(Shame) >= SteadShame.UnwelcomeRung)
        {
            Log.Add(Turn, $"What {World.SettlementName} knows, it shows its own freely; so the saying goes. It is not said to you. The folk count their doors, and keep their craft behind them.", LogTone.Info);
            return;
        }
        Log.Add(Turn, LessonCatalog.All.Any(l => l.Price > 0 && !Player.HasLesson(l.Id))
            ? $"What {World.SettlementName} knows is yours for the asking now. Nothing its folk can show is sold to the stead's own; it is only shown."
            : $"What {World.SettlementName} knows is yours for the asking now, though its folk find, taking stock, that there is little left they could show you.", LogTone.Reward);
    }

    /// <summary>
    /// The raiders mark a raider felled (D-078): the enemy ledger, the wrath of
    /// the dens, rising one notch per slaying. The kill itself is the perceived
    /// change (D-023's rule: the bearer watched it land); the ledger speaks only
    /// when a rung crosses, so hate deepens in three audible steps rather than a
    /// drumbeat of book-keeping. At the dread rung the wrath grows teeth the
    /// bearer can feel in every blow that follows: see RaiderWrath.Steadied.
    /// </summary>
    private void RaiseWrath(int amount)
    {
        if (amount <= 0) return;
        int rungBefore = RaiderWrath.RungFor(Wrath);
        _factionInfamy[FactionId.Raiders] = Wrath + amount;
        int rungAfter = RaiderWrath.RungFor(Wrath);
        if (rungAfter > rungBefore)
        {
            Log.Add(Turn, rungAfter switch
            {
                1 => "The raiders have a name for you now. It is short, and it is not kind.",
                2 => "Dread has entered the raiders' work: their blows come feared now, and land the weaker for it.",
                _ => "You are past hate with the raiders now. To the dens you are weather: a thing to be survived, not fought.",
            }, LogTone.Danger);
        }
        if (!Player.WrathLineHeard)
        {
            Player.WrathLineHeard = true;
            Log.Add(Turn, "\"The stead is not the only ledger kept on you, bearer. The dens keep one too, and hate is also a kind of regard.\"", LogTone.Aegis);
        }
    }

    /// <summary>
    /// The stead marks a wrong done under its roofs (D-086): the home faction's
    /// Infamy, the shame of the doors. Every rise crosses a rung by construction
    /// (one door, one rung), so every rise is named aloud; each rung crossed is
    /// written to the graph as permanent history (D-085's seam from the dark
    /// side), because a stead remembers having watched a bearer even after the
    /// sill is paid. The way back down is named at the deed itself (each verb
    /// knows its own restitution road, D-107), and the first shame ever earned
    /// draws the one line the Aegis keeps for it.
    /// </summary>
    private void RaiseShame(int amount)
    {
        if (amount <= 0) return;
        int rungBefore = SteadShame.RungFor(Shame);
        _factionInfamy[FactionId.Stead] = Shame + amount;
        int rungAfter = SteadShame.RungFor(Shame);
        if (rungAfter > rungBefore)
        {
            Log.Add(Turn, $"In {World.SettlementName} you are {SteadShame.TitleOf(Shame)} now.", LogTone.Danger);
            for (int rung = rungBefore + 1; rung <= rungAfter; rung++)
            {
                string subject = rung switch { 1 => "watched", 2 => "unwelcome", _ => "thief" };
                if (!World.Facts.Exists("shame", subject))
                    World.Facts.Add("shame", subject, World.SettlementName,
                        $"In {World.SettlementName} the bearer has been {SteadShame.TitleOf(SteadShame.Threshold(rung))}.");
            }
        }
        if (!Player.ShameLineHeard)
        {
            Player.ShameLineHeard = true;
            Log.Add(Turn, "\"I hold you whatever you carry, bearer. But what is taken is carried too, and it does not lighten on the road.\"", LogTone.Aegis);
        }
    }

    /// <summary>
    /// The stead lets a count come down (D-086): restitution walking the shame
    /// ladder back, one door at a time, each step named so the easing is felt the
    /// moment it lands. At zero the book is even, and everything suspicion closed
    /// (the friend's price, the larder, the hearthtale's telling) stands open again.
    /// </summary>
    private void LowerShame(int amount)
    {
        if (amount <= 0 || Shame <= 0) return;
        int rungBefore = SteadShame.RungFor(Shame);
        _factionInfamy[FactionId.Stead] = Math.Max(0, Shame - amount);
        int rungAfter = SteadShame.RungFor(Shame);
        if (rungAfter < rungBefore)
            Log.Add(Turn, Shame == 0
                ? $"Word gets round the well by morning. The stead's book on you is even again, and the doors of {World.SettlementName} stop watching you pass."
                : $"Word gets round the well by morning. In {World.SettlementName} you are {SteadShame.TitleOf(Shame)} now, and no worse.", LogTone.Reward);
    }

    /// <summary>
    /// A raid lands on the stead (D-079, D-089): the raiders acting on their
    /// coarse tick, sized by their boldness: a bold den carries off double.
    /// Every firing is perceived as it lands (D-023's mandatory narration
    /// hook): the raid is named, the cost is named, and a fact is written so
    /// the world remembers it happened. Lofts bared to nothing are the raids'
    /// own dark exit, named the moment it closes.
    /// </summary>
    private void RaidTheStead()
    {
        bool bold = Boldness >= RaiderBoldness.BoldAt;
        int take = Math.Min(Stores, bold ? SteadStores.BoldRaidTake : SteadStores.RaidTake);
        Raids++;
        Stores -= take;
        World.Facts.Add("event", "raid", World.SettlementName,
            $"Raiders came down on {World.SettlementName} by night and left with grain.");
        Log.Add(Turn, bold
            ? $"By night the raiders come down on {World.SettlementName} again, and they come greedy now: two lofts opened, grain gone by the sackful, no one dead but no one unshaken."
            : $"By night the raiders come down on {World.SettlementName} again: grain gone, a byre-door split, no one dead but no one unshaken.", LogTone.Danger);
        if (Stores == 0)
        {
            if (!World.Facts.Exists("event", "lofts_bare"))
                World.Facts.Add("event", "lofts_bare", World.SettlementName,
                    $"{World.SettlementName}'s lofts were raided down to the boards; there is nothing left worth a night's ride.");
            Log.Add(Turn, "Bread will be dearer for it, and the lofts are down to the boards now: there is nothing left in this stead worth a night's ride.", LogTone.Danger);
        }
        else
            Log.Add(Turn, "Bread will be dearer for it while the camp stands.", LogTone.Info);

        // A landing raid takes a beast left tethered while the bearer is below
        // (D-100): the saddlebags' honest counterweight. The counterplay is the
        // game's own spine: break the camp first, or carry the coin down and
        // let the shrine's ledger hold the risk instead.
        if (Mount is { } steed && Mode == MapMode.Site)
        {
            Log.Add(Turn, steed.Bags > 0
                ? $"And the tether above stands cut: {steed.Name} is gone with the raiders, saddlebags and all: {steed.Bags} coin, off into the hills."
                : $"And the tether above stands cut: {steed.Name} is gone with the raiders.", LogTone.Danger);
            World.Facts.Add("event", "beast_taken", World.SettlementName,
                $"Raiders led the bearer's beast off {World.SettlementName}'s land by night.");
            Mount = null;
        }

        // The stead moves on its own tick at last (D-105): a sackful night
        // posts the watch, and the last measure calls the levy.
        if (bold && !WatchStands && Stores > 0) PostWatch();
        if (!LevyStands && Stores <= SteadLevy.CalledAt) CallLevy();
    }

    /// <summary>
    /// The watch is posted (D-105): the stead's answer to a raid come greedy,
    /// the home faction acting on the tick instead of only suffering it. From
    /// this tick the raiding nights are met at the fold walls, and the lofts
    /// feed the watchers: protection bought with the very grain it guards.
    /// </summary>
    private void PostWatch()
    {
        WatchStands = true;
        if (!World.Facts.Exists("event", "watch_posted"))
            World.Facts.Add("event", "watch_posted", World.SettlementName,
                $"{World.SettlementName} posted a watch on its lofts after the raiders came greedy.");
        Log.Add(Turn, $"By morning {World.SettlementName} has had enough of sackful nights: a watch is posted on the lofts, scythes and boar-spears under the eaves. Watchers must eat, and it is the lofts that will feed them.", LogTone.Info);
    }

    /// <summary>
    /// The watch holds the raiding night (D-105): the raiders are turned away
    /// with nothing, so no plunder lands and the dens' greed stops compounding,
    /// but the watchers eat a measure of the grain they guard. Left standing
    /// long enough, the watch can bare the lofts itself: the stead's own move
    /// walking into the raids' dark exit by another road.
    /// </summary>
    private void WatchHoldsTheNight()
    {
        Stores -= SteadWatch.Upkeep;
        Log.Add(Turn, $"By night torches stand along {World.SettlementName}'s fold walls, and the raiders find the lofts watched: shouts, one thrown spear, and they melt back into the hills with nothing.", LogTone.Reward);
        Log.Add(Turn, "The watch must eat, though, and it is the lofts that feed it. Bread stays dear while the spears stand under the eaves.", LogTone.Info);
        if (Stores == 0)
        {
            if (!World.Facts.Exists("event", "lofts_bare"))
                World.Facts.Add("event", "lofts_bare", World.SettlementName,
                    $"{World.SettlementName}'s lofts went to the boards feeding its own watch; there is nothing left worth a night's ride.");
            WatchStands = false;
            Log.Add(Turn, $"And with that measure the lofts are down to the boards: {World.SettlementName}'s watch has eaten the stead bare guarding it, and the spears go back under the eaves for want of bread. There is nothing left here worth a night's ride.", LogTone.Danger);
        }
        if (!LevyStands && Stores <= SteadLevy.CalledAt) CallLevy();
    }

    /// <summary>
    /// The levy is called (D-105): the lofts down to the last measure, the
    /// stead's other move on the tick. What grain is left is spoken for, the
    /// larder sells no bread while the levy stands, and the steadholder takes
    /// the levy's answer instead: the bearer's coin against a carted measure,
    /// the stores axis' first bearer-side input beside the camp-clear.
    /// </summary>
    private void CallLevy()
    {
        LevyStands = true;
        if (!World.Facts.Exists("event", "levy_called"))
            World.Facts.Add("event", "levy_called", World.SettlementName,
                $"{World.SettlementName} called a levy: the lofts down to the last measure, and what is left spoken for.");
        Log.Add(Turn, $"Word goes door to door in {World.SettlementName}: the lofts are down to the last measure, and the stead calls a levy. What grain is left is spoken for, and the larder sells no bread while it stands.", LogTone.Danger);
        Log.Add(Turn, "\"A stead counting its last measure aloud is not asking for pity, bearer. It is asking for grain, and it will remember the hand that brings any.\"", LogTone.Aegis);
    }

    /// <summary>
    /// The mound raises its slain (D-106): the third faction acting on the
    /// coarse tick. A wight cut down in an unstilled barrow stands its
    /// passage again while the grudge burns, whole at its grave's own
    /// strength, and the stead speaks of the lights by dusk (the mandatory
    /// perceivability hook: the fact is written, the word comes up the lane).
    /// </summary>
    private void RaiseTheFallen(Site mound, Monster fallen)
    {
        _risenCount++;
        fallen.Hp = mound.Spawns.First(s => s.Kind == MonsterKind.Wight).Hp;
        fallen.Intent = null;
        if (!World.Facts.Exists("event", "mound_restless"))
            World.Facts.Add("event", "mound_restless", World.SettlementName,
                "The lights over the long mound have burned taller since the grave-gold walked out; what is cut down there does not stay down.");
        Log.Add(Turn, "Word comes up the lane at dusk: the lights over the long mound stand taller than they should, and a shape that was cut down in its passages walks them again. The dead remember what left in your pack.", LogTone.Danger);
    }

    /// <summary>The levy lifts (D-105): the lofts climbed clear again, by answers or by the season's recovery.</summary>
    private void LiftLevy()
    {
        LevyStands = false;
        if (!World.Facts.Exists("event", "levy_met"))
            World.Facts.Add("event", "levy_met", World.SettlementName,
                $"{World.SettlementName}'s levy was met; the larder opened its board again.");
        Log.Add(Turn, $"The tally at the well is rubbed out: {World.SettlementName}'s levy is met, and the larder opens its board again.", LogTone.Reward);
    }

    /// <summary>
    /// The tick passes and no raid comes (D-089): the dens cowed below the
    /// raiding line, wrath's first faction-scale consequence. Named once per
    /// world, the moment the quiet is first owed to the bearer's hand.
    /// </summary>
    private void NoteCowedDens()
    {
        if (World.Facts.Exists("event", "dens_cowed")) return;
        World.Facts.Add("event", "dens_cowed", World.SettlementName,
            $"A raiding night passed {World.SettlementName} by: the dens count their dead now before they count the stead's lofts.");
        Log.Add(Turn, $"The raiding night comes and goes, and no torch shows on the hills. Word is the dens count their dead now before they count {World.SettlementName}'s lofts.", LogTone.Reward);
        Log.Add(Turn, "\"Fear is a ledger too, bearer, and you have been writing in it. While it holds, the stead sleeps on your credit.\"", LogTone.Aegis);
    }

    /// <summary>
    /// The stead recovers on the tick (D-089): with the camp fallen, the stores
    /// climb a measure per tick until the lofts stand full, each easing of the
    /// bread narrated as it lands and the made-good season written to the graph.
    /// Ending the raids now earns the stead its season back, where D-079 froze
    /// the taken grain until the crossing.
    /// </summary>
    private void RecoverStores()
    {
        int bumpBefore = SteadStores.PriceBump(Stores);
        Stores++;
        if (SteadStores.PriceBump(Stores) < bumpBefore)
            Log.Add(Turn, $"Carts creak in from the far fields: {World.SettlementName}'s lofts fill a little, and bread comes a coin back down.", LogTone.Reward);
        // The season's own recovery lifts a standing levy (D-105) the moment
        // the lofts climb clear of the last measure.
        if (LevyStands && Stores >= SteadLevy.LiftedAt) LiftLevy();
        if (Stores == SteadStores.Max)
        {
            World.Facts.Add("event", "lofts_full", World.SettlementName,
                $"{World.SettlementName}'s lofts stand full again; the raided season is made good.");
            Log.Add(Turn, $"The last of it is made good: {World.SettlementName}'s lofts stand full again, as if the raided nights had never been.", LogTone.Reward);
        }
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
            // The watch stands down with the war it was posted for (D-105).
            if (WatchStands)
            {
                WatchStands = false;
                Log.Add(Turn, $"Word of it will stand {World.SettlementName}'s watch down by nightfall: there is nothing left in the hills to watch for, and the lofts can stop feeding spears.", LogTone.Info);
            }
            Log.Add(Turn, "\"A deed with weight. It is counted.\"", LogTone.Aegis);
            Log.Add(Turn, $"\"And far to the {Compass(World.CampPos, World.GatePos)} of this cave, something old has unlocked. I feel it the way you feel a door open in a dark house.\"", LogTone.Aegis);
            RaiseRegard(3, $"Word of the ended raids will reach {World.SettlementName} before you do. The stead knows whose hand it was.");

            // The huntsman's debt paid (D-097 stage 2): the arc ends where it
            // aimed. The guest goes home to the bench, the going home is the
            // portfolio, and a stead believes its own.
            if (Guest is { Alive: true } guest && guest.NpcId is { } npcId && _guestNpc is { } home)
            {
                Log.Add(Turn, $"{guest.Name} stands a long moment over the cold fire-pits, counting something private. \"That is the debt paid. Mine, and the wood's. I will walk the rest of my roads alone now, and gladly.\"", LogTone.Info);
                Log.Add(Turn, "\"They stepped out of a whole life to see this done, bearer. Few do. It is counted for them too.\"", LogTone.Aegis);
                World.Facts.Add("portfolio", npcId, World.SettlementName,
                    $"{guest.Name} walked with the bearer until the camp broke, and keeps the wood's edge with a straighter back for it.");
                World.Npcs.Add(home);
                RaiseRegard(1, $"{guest.Name} carries the tale home themselves. A stead believes its own.");
                Guest = null;
                _guestNpc = null;
            }
        }
        else if (site.Kind == SiteKind.Barrow)
        {
            World.Facts.Add("deed", "barrow_stilled", World.SettlementName,
                $"The barrow's dead were put to rest. The lights on the mound above {World.SettlementName} have gone out.");
            Log.Add(Turn, "The passage is still. Whatever the dead here were set to hold, no one is holding it now.", LogTone.Reward);
            Log.Add(Turn, "\"They were given a task and no release. I remember the shape of that arrangement. It is counted, bearer, twice over.\"", LogTone.Aegis);
            // The stilling settles all accounts (D-106): dead laid to rest
            // keep no ledgers, so the grudge is the one Infamy in the game
            // whose designed exit is the deed itself.
            if (Grudge > 0)
            {
                _factionInfamy[FactionId.Mound] = 0;
                Log.Add(Turn, "And the grudge goes out of the ground with them: what was carried off is carried off, and no one is left under the turf to count it.", LogTone.Reward);
            }
            RaiseRegard(2, $"The lights on the mound above {World.SettlementName} are out tonight. The stead will sleep the easier, and know why.");
        }
        else if (site.Kind == SiteKind.Quarry)
        {
            World.Facts.Add("deed", "quarry_hushed", World.SettlementName,
                "The graven men of the old quarry stand still at last. The pit is only a pit now.");
            Log.Add(Turn, "The pit is still. Broken stone everywhere, and not one figure left standing that should not.", LogTone.Reward);
            Log.Add(Turn, "\"Set to watch a working no one finished, and never told to stop. There is a lot of that, this deep. It is counted.\"", LogTone.Aegis);
        }
        else if (site.Kind == SiteKind.Hall)
        {
            World.Facts.Add("deed", "pack_broken", World.SettlementName,
                "The iron hounds of the fallen hall run no more. The dusk belongs to the flocks again.");
            Log.Add(Turn, "The hall is quiet. Wind over the wall-tops, and nothing pacing you behind the columns.", LogTone.Reward);
            Log.Add(Turn, "\"They were not wicked, bearer. They were hungry, and everyone whose work it was to feed them is gone. It is counted.\"", LogTone.Aegis);
        }
        else if (site.Kind == SiteKind.Ringfort)
        {
            World.Facts.Add("deed", "watch_relieved", World.SettlementName,
                "The old watch of the ringfort stands relieved. The lanes between the walls are only lanes now.");
            Log.Add(Turn, "The fort is still. Boards lie where they were held, and nothing walks the wall-tops but wind.", LogTone.Reward);
            Log.Add(Turn, "\"A wall watched for a war that ended before the stead's first stone. No relief ever rode in, bearer, so you are it. It is counted.\"", LogTone.Aegis);
        }
        else if (site.Kind == SiteKind.Leaguer)
        {
            World.Facts.Add("deed", "siege_lifted", World.SettlementName,
                "The leaguer around the black mere is lifted. Nothing watches the holm now but herons.");
            Log.Add(Turn, "The works are still. The mere settles glass-flat, and for the first time in an age nothing on its banks is counting.", LogTone.Reward);
            Log.Add(Turn, "\"Sit until the holm yields, they were told. It never yielded, bearer; it only emptied, and no one thought to tell them that either. It is counted.\"", LogTone.Aegis);
        }
        else if (site.Kind == SiteKind.Wilds)
        {
            // No deed and no essence (D-070): the hunt is a smaller ledger than the
            // deep sites keep, meat and hide, not a wrong set right. Cleared here only
            // means the glade is hunted out for now; the far gate fills it again.
            Log.Add(Turn, "The glade goes still. Nothing left in it but tracks, crushed bracken, and the smell of the hunt.", LogTone.Reward);
            Log.Add(Turn, "\"A smaller counting, this one. Not a deed. But the body keeps that ledger too, and it is heavier than it looks.\"", LogTone.Aegis);
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

        // The factions act on the coarse tick (D-079, D-089, D-105). While the
        // camp stands and the lofts hold grain, a bold den raids and a cowed one
        // keeps to its dens; not while the bearer is inside the camp itself (a
        // den under attack defends its own). A posted watch meets the raiding
        // night instead, at a measure of upkeep, and stands down the tick the
        // dens' greed breaks. Once the camp falls the stead recovers, a measure
        // per tick, until the lofts stand full. Bared lofts are the raids' own
        // dark exit: nothing left worth a night's ride.
        if (Turn > _worldStartTurn && (Turn - _worldStartTurn) % SteadRaids.TickTurns == 0)
        {
            if (!CampCleared && Stores > 0 && CurrentSite?.Kind != SiteKind.GoblinCamp)
            {
                if (WatchStands && Boldness < RaiderBoldness.BoldAt)
                {
                    WatchStands = false;
                    Log.Add(Turn, $"The greed has gone out of the hills, and {World.SettlementName} stands its watch down: the fields want the hands back more than the fold walls do.", LogTone.Info);
                }
                if (WatchStands) WatchHoldsTheNight();
                else if (Boldness >= RaiderBoldness.RaidingAt) RaidTheStead();
                else NoteCowedDens();
            }
            else if (CampCleared && Stores < SteadStores.Max)
                RecoverStores();

            // The mound seethes (D-106): grave-goods in a living pack while
            // the barrow stands unstilled do not go unanswered. On its tick
            // the mound raises one of its own slain again, up to the cap, and
            // never under the bearer's eye (a mound walked keeps its counsel,
            // the camp-under-attack rule's mirror). The stilling settles it.
            if (Grudge > 0 && _risenCount < MoundGrudge.RisenCap
                && World.BarrowSite is { Cleared: false } mound && CurrentSite != mound
                && Monsters.FirstOrDefault(m => !m.Alive && m.SiteId == mound.Id && m.Kind == MonsterKind.Wight) is { } fallen)
                RaiseTheFallen(mound, fallen);
        }

        if (Mode == MapMode.Site)
            foreach (var monster in Monsters.Where(m => m.Alive && m.SiteId == CurrentSite!.Id))
            {
                // The death remembers its shape (D-098): the wind-up is caught
                // before the act consumes it, and only the hand that actually
                // dropped the bearer is written, so the scar can match the death.
                var windup = monster.Intent?.Kind;
                int hpBefore = Player.Hp;
                ActMonster(monster);
                if (hpBefore > 0 && Player.Hp <= 0)
                {
                    _deathShape = (monster.Kind, windup);
                    _deathHand = monster;
                }
            }

        // The guard set (D-125) lives declaration to resolution, exactly one
        // field-turn, met or not: iron cannot wait in a line forever.
        _parryTarget = null;

        // Those who walk with you (D-097, D-099) take their own step after
        // the field has moved, so what they answer is what actually stands.
        if (Player.Hp > 0)
        {
            ActFellow(Guest);
            ActFellow(Shade);
            ActMount();
        }

        // The ward-word runs out with the turns (D-091), and the pool gathers
        // itself back a point at a time on the road, once any word is carried.
        if (Player.WardTurns > 0 && --Player.WardTurns == 0)
            Log.Add(Turn, "The ward-word's held breath goes out of the air.", LogTone.Info);
        if (Player.Spells.Count > 0 && Player.Focus < Player.MaxFocus && Turn % FocusRegenTurns == 0)
            Player.Focus++;

        if (Player.WoundedTurns > 0)
        {
            Player.WoundedTurns--;
            if (Player.WoundedTurns == 0)
            {
                Log.Add(Turn, "The wound's weight lifts. You are whole again.", LogTone.Info);
                Player.Hp = Math.Min(Player.Hp, Player.EffectiveMaxHp);
            }
        }

        // The Death's Toll (D-098) drains a point a turn: time is the only
        // mercy in the count, and the crossing back under the line is spoken,
        // so the bearer always knows which side of it they stand on.
        if (Player.Toll > 0 && --Player.Toll == DeathsToll.Line - 1)
            Log.Add(Turn, "The toll's count settles below the line. A fall now would leave no mark.", LogTone.Info);

        // The grave-cold (D-096) works its way out a turn at a time.
        if (Player.ChilledTurns > 0 && --Player.ChilledTurns == 0)
            Log.Add(Turn, "The grave-cold works out of your arms at last.", LogTone.Info);

        // The bearer's stagger (D-126) walks off the same way.
        if (Player.StaggerTurns > 0 && --Player.StaggerTurns == 0)
            Log.Add(Turn, "Your feet find their line again, and the guard comes back up whole.", LogTone.Info);

        // Quiet ground settles the guard (D-126): the second bar is fight
        // pressure, not a wound. Out from under the blows it regathers whole,
        // the mirror of a foe's bar dying with the foe.
        if (Player.PostureDmg > 0 && !LiveMonstersHere.Any(m => !m.Dormant))
        {
            Player.PostureDmg = 0;
            Log.Add(Turn, "Out from under the blows, your guard settles whole.", LogTone.Info);
        }

        if (Mode == MapMode.Overworld && Player.Hp > 0)
            _storylets.TryFire(this, StoryletTrigger.AmbientTurn);

        if (Player.Hp <= 0) HandleDeath();
    }

    private void ActMonster(Monster monster)
    {
        // The guard regathered (D-125): a stagger walked off un-riposted is a
        // door closed. The body next acts whole.
        if (monster.GuardBroken && monster.ExposedTurns == 0) monster.GuardBroken = false;

        // Resolve a telegraphed intent first: it lands on the cell, not the player.
        if (monster.Intent is { } intent)
        {
            intent.TurnsUntilResolve--;
            if (intent.TurnsUntilResolve <= 0)
            {
                monster.Intent = null;
                // The bestiary (D-059): a wind-up watched to its end teaches the
                // tell, hit or miss, and the knowledge banks with the bearer
                // across the crossing. This is the one clause of D-004 still
                // owed since the first combat decision: the read is earned.
                Player.WitnessTell(monster.Kind, Cycle);
                // The feint (D-096): the blow falls where it was always going,
                // which is not always where the mark said.
                var struckCell = intent.FeintCell ?? intent.TargetCell;
                bool landed = Player.Pos == struckCell;
                // The guard set (D-125) meets the blow it was set against. A
                // feinting striker can never be the set target: the shown mark
                // the declaration demanded was never on the bearer's cell.
                bool parried = landed && monster == _parryTarget;

                // The dice are rolled only when a body is actually struck, so a
                // world without a guest draws exactly what it always drew.
                int RollFor(IntentKind kind) => kind switch
                {
                    IntentKind.BarrowBlade => _combatRng.Range(5, 9),
                    IntentKind.SunderingCut => _combatRng.Range(7, 11),
                    IntentKind.HurledStone => _combatRng.Range(4, 8),
                    IntentKind.GravenFist => _combatRng.Range(6, 10),
                    IntentKind.ThroatLunge => _combatRng.Range(6, 10),
                    IntentKind.SeaxStab => _combatRng.Range(6, 10),
                    IntentKind.MeasuredCut => _combatRng.Range(5, 9),
                    _ => _combatRng.Range(4, 7),
                };
                if (intent.Kind == IntentKind.BoarCharge)
                {
                    // The charge (D-053) resolves along its lane, not on one cell.
                    ResolveCharge(monster, intent);
                }
                else if (intent.Kind == IntentKind.LoftedStone)
                {
                    // The lofted cast (D-057) falls on its marked ground and bursts.
                    ResolveLoft(monster, intent);
                }
                else if (intent.Kind == IntentKind.RallyCry)
                {
                    // The cry (D-096): not a blow at all. The camp answers it.
                    ResolveCry(monster);
                }
                else if (intent.Kind == IntentKind.GraveChill)
                {
                    if (landed)
                    {
                        Player.ChilledTurns = 4;
                        Log.Add(Turn, "The grave-cold closes over the ground you kept, and gets into your arms: your blows will come slower while it holds.", LogTone.Danger);
                    }
                    else
                        Log.Add(Turn, "The grave-cold closes on ground you had the sense to leave: frost stars the stone where you stood.", LogTone.Combat);
                }
                else if (parried)
                {
                    if (monster.Kind == MonsterKind.Thegn)
                    {
                        // The drilled hand (D-129): the one kind that answers
                        // the met parry. It was taught the bind beside the
                        // blow: it rolls off the set guard keeping half its
                        // force, and the crossed iron shoves back a point
                        // into the bearer's arms.
                        Log.Add(Turn, "The blow comes as shown and your guard meets it, but the sword-thegn was drilled for the bind: it rolls off your guard with half its force kept, and the crossed iron shoves back into your arms.", LogTone.Reward);
                        RockGuard(monster, GuardBreak.DrilledParryPressure);
                        RockBearer(GuardBreak.BindPressure);
                    }
                    else
                    {
                        // The parry (D-125): the blow is turned whole, no dice, and
                        // the striker's own committed force is what rocks its guard.
                        // The turn was spent on the guard, not the kill: that is
                        // the price, and the broken guard's riposte is the pay.
                        Log.Add(Turn, $"The blow comes exactly as shown, and your guard is waiting: you turn it aside whole, and the force of its own swing rocks the {monster.Name}.", LogTone.Reward);
                        RockGuard(monster, GuardBreak.ParryPressure);
                    }
                    GainSkill(Player.Weapon?.Family ?? SkillId.Brawling);
                }
                else if (intent.Kind == IntentKind.BoardCheck)
                {
                    // The board-check (D-129): no dice and no blood at all;
                    // the whole verb is thrown mass along the guard's line.
                    // Dodged by feet like any shown blow, met by the parry.
                    if (landed)
                    {
                        Log.Add(Turn, "The whole board takes you edge-on: no iron in it, but the weight shoves your guard wide of its line.", LogTone.Danger);
                        RockBearer(GuardBreak.CheckPressure);
                    }
                    else
                        Log.Add(Turn, "The board's full weight goes through the place you left, and the carl stamps to keep its feet.", LogTone.Combat);
                }
                else if (landed)
                {
                    int roll = RollFor(intent.Kind);
                    // The dread stays the raider's hand (D-078): applied to the
                    // raw roll, after the dice, so the draw count never changes.
                    // A named grudge arms it back a point (D-110), same rail.
                    if (monster.Kind == MonsterKind.Goblin) roll = RaiderRoster.Armed(monster.Grudge, RaiderWrath.Steadied(Wrath, roll));
                    // The grudge arms the dead's (D-106): the dark mirror.
                    if (monster.Kind == MonsterKind.Wight) roll = MoundGrudge.Riled(Grudge, roll);
                    // The reader (D-126): the thegn alone knows the door a
                    // beaten-open guard leaves, and gives the point its whole
                    // arm through it: the riposte's mirror, drilled an age.
                    if (monster.Kind == MonsterKind.Thegn && Player.StaggerTurns > 0)
                    {
                        roll += 2;
                        Log.Add(Turn, "The sword-thegn sees the beaten-open guard for exactly what it is.", LogTone.Danger);
                    }
                    int damage = Absorb(roll, telegraphed: true);
                    Player.Hp -= damage;
                    Log.Add(Turn, intent.Kind switch
                    {
                        IntentKind.BarrowBlade => $"The wight's barrow blade opens you for {damage}!",
                        IntentKind.SunderingCut => $"The severed one's cut goes through guard, cloth, and certainty for {damage}!",
                        IntentKind.HurledStone => $"The hurled stone takes you square for {damage}!",
                        IntentKind.GravenFist => $"The graven fist comes down like a falling lintel for {damage}!",
                        IntentKind.ThroatLunge => $"The iron hound hits you full-length, jaws first, for {damage}!",
                        IntentKind.SeaxStab => $"The seax comes over the board's rim and finds you for {damage}!",
                        IntentKind.MeasuredCut => intent.FeintCell is not null
                            ? $"The mark was the lie. The thegn's point was always coming here, and it opens you for {damage}!"
                            : $"The sword-thegn's measured cut falls true for {damage}!",
                        _ => $"The {monster.Name}'s crushing blow lands for {damage}!",
                    }, LogTone.Danger);
                    // The landed committed blow rocks the bearer's own guard
                    // (D-126): force through the guard's line, not the blood.
                    RockBearer(BearerPressure(intent.Kind));
                    // The drag (D-096): the hound's lunge that lands does not
                    // let go: jaws lock and haul the bearer a stride toward
                    // whatever of the pack still stands.
                    if (intent.Kind == IntentKind.ThroatLunge && Player.Hp > 0
                        && Monsters.Where(m => m.Alive && m != monster && m.Kind == MonsterKind.Hound && m.SiteId == monster.SiteId)
                            .OrderBy(m => m.Pos.Chebyshev(Player.Pos)).FirstOrDefault() is { } pack)
                    {
                        var pull = Player.Pos.Plus(Math.Sign(pack.Pos.X - Player.Pos.X), Math.Sign(pack.Pos.Y - Player.Pos.Y));
                        if (pull != Player.Pos && CurrentMap.Walkable(pull)
                            && !FellowAt(pull)
                            && !Monsters.Any(m => m.Alive && m.SiteId == monster.SiteId && m.Pos == pull))
                        {
                            Player.Pos = pull;
                            Log.Add(Turn, "The jaws do not let go: you are hauled a full stride toward the rest of the pack before you tear free.", LogTone.Danger);
                        }
                    }
                }
                else if (Fellows.FirstOrDefault(f => f.Pos == struckCell) is { } struck)
                {
                    // The second body (D-097, D-099): a fellow standing on the
                    // marked ground takes the blow meant for it, whole. No
                    // stance, no iron, no Aegis: what guards the bearer never
                    // guarded them.
                    int roll = RollFor(intent.Kind);
                    if (monster.Kind == MonsterKind.Goblin) roll = RaiderRoster.Armed(monster.Grudge, RaiderWrath.Steadied(Wrath, roll));
                    if (monster.Kind == MonsterKind.Wight) roll = MoundGrudge.Riled(Grudge, roll);
                    struck.Hp -= roll;
                    Log.Add(Turn, struck.Alive
                        ? struck.Role == GuestRole.Shade
                            ? $"The blow finds the shade on the marked ground: it is torn for {roll}!"
                            : $"The blow finds {struck.Name} on the marked ground: they are opened for {roll}!"
                        : $"The blow comes down on {struck.Name}, full weight.", LogTone.Danger);
                    if (!struck.Alive) FellowFalls(struck);
                }
                else
                {
                    Log.Add(Turn, intent.Kind switch
                    {
                        IntentKind.BarrowBlade => "The wight's bronze blade shears cold, empty air.",
                        IntentKind.SunderingCut => "The severed one's cut parts the air where you stood, without hurry and without regret.",
                        IntentKind.HurledStone => "The stone bursts on the floor where you stood, loud as the quarry's last working day.",
                        IntentKind.GravenFist => "The graven fist cracks the floor where you stood.",
                        IntentKind.ThroatLunge => "The hound's lunge carries it through the space you left; it lands badly and comes up snarling.",
                        IntentKind.SeaxStab => "The seax jabs over the rim into air gone empty.",
                        IntentKind.MeasuredCut => intent.FeintCell is not null
                            ? "The thegn's point comes back to where you truly stood, and finds you gone anyway. It inclines its head a fraction."
                            : "The sword-thegn's measured cut parts air, and it does not seem surprised.",
                        _ => $"The {monster.Name}'s crushing blow splinters empty stone.",
                    }, LogTone.Combat);
                }

                // The blow spent, the board leaves its line (D-053): hit or
                // miss, the carl stands open, and shafts find it.
                if (intent.Kind == IntentKind.SeaxStab)
                {
                    monster.ExposedTurns = 2;
                    Log.Add(Turn, "The blow spent, the shield-carl's board hangs wide of its line.", LogTone.Combat);
                }

                // The answer (D-056): a read blow stood through and taken, with
                // answering iron in hand and the striker within its length, is
                // answered instantly and for free. The price was already paid
                // in blood; the blow dodged is the blow never answered. A cut
                // at the hollow's keeper is the old way, so while the laying
                // moment stands open the hand holds (D-045). A lofted stone
                // (D-057) falls from the sky's top, not from a hand in reach:
                // there is nothing to answer over.
                if (landed && !parried && intent.Kind is not IntentKind.BoarCharge and not IntentKind.LoftedStone && Player.Hp > 0
                    && Player.Weapon is { Move: MoveVerb.Answer } blade
                    && monster.Pos.Chebyshev(Player.Pos) == 1)
                {
                    if (monster.Kind == MonsterKind.Severed && Player.Resolution != Resolution.None && !_layingDeclined)
                    {
                        Log.Add(Turn, "Your hand starts the answer, and you hold it.", LogTone.Combat);
                    }
                    else
                    {
                        int answer = 1 + blade.EffectiveBonus(Player.Attributes);
                        monster.Hp -= answer;
                        if (monster.Alive)
                            Log.Add(Turn, $"You take the blow standing and answer over the iron: the {monster.Name} is cut for {answer}.", LogTone.Combat);
                        else
                            HarvestRemains(monster);
                    }
                }
            }
            return;
        }

        if (monster.Kind == MonsterKind.Wight) { ActWight(monster); return; }
        if (monster.Kind == MonsterKind.Severed) { ActSevered(monster); return; }
        if (monster.Kind == MonsterKind.Graven) { ActGraven(monster); return; }
        if (monster.Kind == MonsterKind.Hound) { ActHound(monster); return; }
        if (monster.Kind == MonsterKind.Carl) { ActCarl(monster); return; }
        if (monster.Kind == MonsterKind.Boar) { ActBoar(monster); return; }
        if (monster.Kind == MonsterKind.Hart) { ActHart(monster); return; }
        if (monster.Kind == MonsterKind.Warder) { ActWarder(monster); return; }
        if (monster.Kind == MonsterKind.Thegn) { ActThegn(monster); return; }

        // Standing open (D-125): the kinds on the generic path stagger like
        // the schooled ones do. A broken guard holds feet and blows both.
        if (monster.ExposedTurns > 0) { monster.ExposedTurns--; return; }

        // The second body (D-097, D-099): a raider that one of the fellows
        // stands nearer than the bearer turns on that fellow. The old kinds
        // keep their old eyes for now; blood is blood to a den, and a den
        // does not ask whether what it cuts at bleeds.
        if (Fellows.Where(f => monster.Pos.Chebyshev(f.Pos) < monster.Pos.Chebyshev(Player.Pos))
            .OrderBy(f => monster.Pos.Chebyshev(f.Pos)).FirstOrDefault() is { } quarry)
        {
            ActAgainstGuest(monster, quarry);
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
                    // Only the raiders walk this generic path, so the dread
                    // (D-078) weighs on the bite as it does on the club, and a
                    // named grudge (D-110) arms it back.
                    int damage = Absorb(RaiderRoster.Armed(monster.Grudge, RaiderWrath.Steadied(Wrath, _combatRng.Range(1, 3))));
                    Player.Hp -= damage;
                    Log.Add(Turn, $"The {monster.Name} bites you for {damage}.", LogTone.Combat);
                }
            }
            return;
        }

        // The cry (D-096): a goblin with campmates left may stop mid-lope and
        // fill its lungs, marked a turn like any wind-up. Killing it first, or
        // being gone when the camp answers, is the counterplay.
        if (monster.Kind == MonsterKind.Goblin && dist >= 2 && dist <= 6
            && Monsters.Any(m => m.Alive && m != monster && m.Kind == MonsterKind.Goblin && m.SiteId == monster.SiteId)
            && _combatRng.Chance(0.12))
        {
            monster.Intent = new Intent { Kind = IntentKind.RallyCry, TargetCell = monster.Pos };
            Log.Add(Turn, "A goblin stops dead, plants its feet, and fills its lungs!", LogTone.Danger);
            return;
        }

        if (dist <= 8) StepToward(monster);
    }

    /// <summary>
    /// A raider on the guest's blood (D-097): no telegraph, a plain cut at
    /// arm's reach, a stride otherwise. Guests take blows the way the field
    /// deals them, and the field is not gentle.
    /// </summary>
    private void ActAgainstGuest(Monster monster, Guest guest)
    {
        int dist = monster.Pos.Chebyshev(guest.Pos);
        if (dist == 1)
        {
            int damage = RaiderRoster.Armed(monster.Grudge, RaiderWrath.Steadied(Wrath, _combatRng.Range(2, 5)));
            guest.Hp -= damage;
            Log.Add(Turn, guest.Alive
                ? $"The {monster.Name} turns its iron on {guest.Name}: a cut for {damage}."
                : guest.Role == GuestRole.Shade
                    ? $"The {monster.Name}'s iron goes through what it struck and meets nothing that holds it."
                    : $"The {monster.Name}'s iron goes into {guest.Name}, and stays a heartbeat too long.", LogTone.Danger);
            if (!guest.Alive) FellowFalls(guest);
            return;
        }
        if (dist <= 8) StepTowardPos(monster, guest.Pos);
    }

    /// <summary>The cry answered (D-096): every campmate still standing comes a stride at once.</summary>
    private void ResolveCry(Monster crier)
    {
        Log.Add(Turn, "The screech goes through the camp like a thrown knife: every ear in it now knows exactly where you stand.", LogTone.Danger);
        foreach (var packmate in Monsters.Where(m => m.Alive && m != crier
            && m.Kind == MonsterKind.Goblin && m.SiteId == crier.SiteId && m.Intent is null).ToList())
            StepToward(packmate);
    }

    /// <summary>
    /// A fellow's turn (D-097, D-099): held ground is held; otherwise a foe in
    /// reach is fought, and then the road back to the bearer's shoulder. They
    /// fight to their own measure, never the bearer's: a huntsman's blow is
    /// worth having, a crofter's is a gesture with a knife in it, and the
    /// shade's modest hand falls double on the uncanny kinds, soul-stuff
    /// answering soul-stuff. None of them raise a hand to a severed one (the
    /// laying is the bearer's own choice to make, D-038 and D-045, and the
    /// shade least of all: it is kin), a hart (not a foe), or anything still
    /// dormant (waking the stone is the bearer's own mistake to make).
    /// </summary>
    private void ActFellow(Guest? fellow)
    {
        if (fellow is not { Alive: true } guest) return;

        if (Mode == MapMode.Site)
        {
            var foe = Monsters.Where(m => m.Alive && !m.Dormant && m.SiteId == CurrentSite!.Id
                    && m.Kind is not MonsterKind.Severed and not MonsterKind.Hart
                    && m.Pos.Chebyshev(guest.Pos) == 1)
                .OrderBy(m => m.Hp).ThenBy(m => m.Pos.X).ThenBy(m => m.Pos.Y).FirstOrDefault();
            if (foe is not null)
            {
                var (lo, hi) = guest.Blow;
                int blow = _combatRng.Range(lo, hi);
                bool uncanny = guest.Role == GuestRole.Shade
                    && foe.Kind is MonsterKind.Wight or MonsterKind.Graven;
                if (uncanny) blow *= 2;
                foe.Hp -= blow;
                if (foe.Alive)
                    Log.Add(Turn, guest.Role switch
                    {
                        GuestRole.Shade when uncanny => $"The shade puts its hands into the {foe.Name}, and what it takes hold of is not the body: {blow}, and the {foe.Name} knows itself gripped.",
                        GuestRole.Shade => $"The shade passes a hand through the {foe.Name}, and something under the hide tears, for {blow}.",
                        GuestRole.Huntsman => $"{guest.Name} cuts at the {foe.Name}, workmanlike, for {blow}.",
                        _ => $"{guest.Name} jabs at the {foe.Name} for {blow}, holding the knife the way a knife is not held.",
                    }, LogTone.Combat);
                else
                {
                    Log.Add(Turn, guest.Role == GuestRole.Shade
                        ? $"The {foe.Name} goes down under the shade's hands without a mark on it."
                        : $"{guest.Name} drops the {foe.Name} where it stands.", LogTone.Combat);
                    // The spoils and the counts run through the one place every
                    // kill-path meets: the dens still tally their dead, and a
                    // site a fellow's hand finishes still clears.
                    HarvestRemains(foe);
                }
                return;
            }
        }

        if (guest.Holding || guest.Pos.Chebyshev(Player.Pos) <= 1) return;
        GuestStepToward(guest);
    }

    /// <summary>One greedy stride back toward the bearer, around bodies and stone.</summary>
    private void GuestStepToward(Guest guest)
    {
        var map = CurrentMap;
        var best = guest.Pos;
        int bestDist = guest.Pos.Manhattan(Player.Pos);
        foreach (var (dx, dy) in Directions.All8)
        {
            var next = guest.Pos.Plus(dx, dy);
            if (!map.Walkable(next)) continue;
            if (next == Player.Pos) continue;
            if (Fellows.Any(f => f != guest && f.Pos == next)) continue;
            if (MountAt(next)) continue;
            if (LiveMonstersHere.Any(m => m.Pos == next)) continue;
            int d = next.Manhattan(Player.Pos);
            if (d < bestDist) { bestDist = d; best = next; }
        }
        guest.Pos = best;
    }

    /// <summary>
    /// Every fellow set down beside an anchor (D-097, D-099): the mortal guest
    /// takes the first open cell, the shade the next, in the fixed compass
    /// order, so doorways, exits, and wakings at the shrine put them
    /// deterministically at the bearer's shoulder.
    /// </summary>
    private void PlaceFellowsBeside(Pos anchor)
    {
        if (Guest is { Alive: true } guest) PlaceFellowBeside(guest, anchor);
        if (Shade is { Alive: true } shade) PlaceFellowBeside(shade, anchor);
    }

    /// <summary>
    /// The beast's own step (D-100): overworld only, one greedy stride back to
    /// the bearer's side. It waits, uncommanded, whenever the bearer is below.
    /// </summary>
    private void ActMount()
    {
        if (Mode != MapMode.Overworld || Mount is not { } steed) return;
        if (steed.Pos.Chebyshev(Player.Pos) <= 1) return;
        var map = CurrentMap;
        var best = steed.Pos;
        int bestDist = steed.Pos.Manhattan(Player.Pos);
        foreach (var (dx, dy) in Directions.All8)
        {
            var next = steed.Pos.Plus(dx, dy);
            if (!map.Walkable(next)) continue;
            if (next == Player.Pos) continue;
            if (FellowAt(next)) continue;
            if (World.Npcs.Any(n => n.Pos == next)) continue;
            int d = next.Manhattan(Player.Pos);
            if (d < bestDist) { bestDist = d; best = next; }
        }
        steed.Pos = best;
    }

    /// <summary>The beast set down beside an anchor (D-100): the fellows' compass rule, overworld coordinates.</summary>
    private void PlaceMountBeside(Pos anchor)
    {
        var steed = Mount!;
        var map = World.Overworld;
        foreach (var (dx, dy) in Directions.All8)
        {
            var cell = anchor.Plus(dx, dy);
            if (!map.Walkable(cell) || cell == Player.Pos) continue;
            if (Fellows.Any(f => f.Pos == cell)) continue;
            if (World.Npcs.Any(n => n.Pos == cell)) continue;
            steed.Pos = cell;
            return;
        }
        steed.Pos = anchor;
    }

    private void PlaceFellowBeside(Guest fellow, Pos anchor)
    {
        fellow.Holding = false;
        var map = CurrentMap;
        foreach (var (dx, dy) in Directions.All8)
        {
            var cell = anchor.Plus(dx, dy);
            if (!map.Walkable(cell) || cell == Player.Pos) continue;
            if (Fellows.Any(f => f != fellow && f.Pos == cell)) continue;
            if (MountAt(cell)) continue;
            if (LiveMonstersHere.Any(m => m.Pos == cell)) continue;
            fellow.Pos = cell;
            return;
        }
        fellow.Pos = anchor;
    }

    /// <summary>What ends depends on what walked (D-099): a mortal fall has weight, an unraveling has none.</summary>
    private void FellowFalls(Guest fellow)
    {
        if (fellow.Role == GuestRole.Shade) ShadeUnravels();
        else GuestFalls(fellow);
    }

    /// <summary>
    /// A guest falls (D-097): the full weight. The world writes the grave
    /// fact; a bond with beats enough writes the beloved fact the memorial
    /// thread cashes; and the stead grieves and remembers: a life spent in
    /// the bearer's keeping costs standing. Their NPC never comes home.
    /// </summary>
    private void GuestFalls(Guest guest)
    {
        Log.Add(Turn, $"{guest.Name} goes down, and does not move again.", LogTone.Danger);
        Log.Add(Turn, "\"I could not reach them, bearer. I was never made to hold but one.\"", LogTone.Aegis);
        if (guest.NpcId is { } npcId)
        {
            World.Facts.Add("guest-fell", npcId, World.SettlementName,
                $"{guest.Name} fell on the bearer's road, and {World.SettlementName} keeps the name.");
            if (guest.Beats >= 3)
                World.Facts.Add("guest-beloved", npcId, World.SettlementName,
                    $"{guest.Name} shared the road's bread and blood with the bearer, and the small kindnesses were counted.");
            RaiseShame(1);
            Log.Add(Turn, $"{World.SettlementName} will hear how {guest.Name} ended, and in whose company. Doors will be slower for a while.", LogTone.Danger);
            _guestNpc = null;
        }
    }

    /// <summary>
    /// The barrow family (D-033): grave-slow (a step only every other turn, so they can
    /// be kited), a cold grasp that stiffens stamina, and a heavier telegraphed blade.
    /// </summary>
    private void ActWight(Monster monster)
    {
        int dist = monster.Pos.Chebyshev(Player.Pos);

        // The chill (D-096): at its slow closing distance the wight may instead
        // breathe the barrow out over the bearer's ground. Marked a turn;
        // stepping off it is the whole answer.
        if (dist >= 2 && dist <= 4 && _combatRng.Chance(0.25))
        {
            monster.Intent = new Intent { Kind = IntentKind.GraveChill, TargetCell = Player.Pos };
            Log.Add(Turn, "The wight stops, and the air goes wrong: a grave-cold gathers over the ground you stand on!", LogTone.Danger);
            return;
        }

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
                int damage = Absorb(_combatRng.Range(2, 5));
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
                int damage = Absorb(_combatRng.Range(2, 6));
                Player.Hp -= damage;
                if (Player.Essence > 0) Player.Essence--;
                Log.Add(Turn, $"The severed one's touch takes {damage}, and something thinner than blood with it.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 8) StepBfsToward(monster);
    }

    /// <summary>
    /// The quarry family (D-040): statues until you are close enough to see, and
    /// seen. Awake, they are artillery: they hold their ground and hurl quarry-stone
    /// at telegraphed cells wherever line of sight allows, lumber a step every third
    /// turn when it does not, and trade a heavy telegraphed fist up close. The
    /// pillars of their own pit are the counterplay: cover breaks the throw.
    /// </summary>
    private void ActGraven(Monster monster)
    {
        int dist = monster.Pos.Chebyshev(Player.Pos);
        var map = CurrentSite!.Map;

        if (monster.Dormant)
        {
            if (dist <= 5 && map.LineOfSight(monster.Pos, Player.Pos))
            {
                monster.Dormant = false;
                Log.Add(Turn, "Grit sifts from a figure you took for quarry-stone. It turns its head, and the head grinds.", LogTone.Danger);
            }
            return;
        }

        if (dist == 1)
        {
            if (_combatRng.Chance(0.4))
            {
                monster.Intent = new Intent { Kind = IntentKind.GravenFist, TargetCell = Player.Pos };
                Log.Add(Turn, "The graven man raises a fist like a keystone coming loose!", LogTone.Danger);
            }
            else if (_combatRng.Chance(Player.DodgeChance))
            {
                Log.Add(Turn, "The graven man's grip closes on air, slow as subsidence.", LogTone.Combat);
            }
            else
            {
                int damage = Absorb(_combatRng.Range(2, 4));
                Player.Hp -= damage;
                Log.Add(Turn, $"The graven man's grip scores you for {damage}. Stone dust in the wound.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 9 && map.LineOfSight(monster.Pos, Player.Pos))
        {
            if (_combatRng.Chance(0.5))
            {
                monster.Intent = new Intent { Kind = IntentKind.HurledStone, TargetCell = Player.Pos };
                Log.Add(Turn, "The graven man hefts a broken block, eye-hollows fixed on where you stand!", LogTone.Danger);
            }
            return;
        }

        if (dist <= 12 && Turn % 3 == 0) StepBfsToward(monster);
    }

    /// <summary>
    /// The hall family (D-044): the game's first pack. Iron hounds run at a
    /// walker's full speed and are weak alone: a worrying bite that grows a point
    /// for every packmate also at your side, and the throat-lunge, their one heavy
    /// telegraph, attempted only while a packmate holds your attention. The
    /// counterplay is ground, not feet: in the porch or a door-slot only one hound
    /// can reach you, and a hound that cannot flank never lunges.
    /// </summary>
    private void ActHound(Monster monster)
    {
        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist == 1)
        {
            int packmates = Monsters.Count(m => m.Alive && m != monster && m.Kind == MonsterKind.Hound
                && m.SiteId == monster.SiteId && m.Pos.Chebyshev(Player.Pos) == 1);
            if (packmates >= 1 && _combatRng.Chance(0.4))
            {
                monster.Intent = new Intent { Kind = IntentKind.ThroatLunge, TargetCell = Player.Pos };
                Log.Add(Turn, "While its packmate holds your eye, the iron hound gathers itself low!", LogTone.Danger);
            }
            else if (_combatRng.Chance(Player.DodgeChance))
            {
                Log.Add(Turn, "The iron hound's teeth clash shut on air.", LogTone.Combat);
            }
            else
            {
                int damage = Absorb(_combatRng.Range(2, 4) + packmates);
                Player.Hp -= damage;
                Log.Add(Turn, packmates > 0
                    ? $"Teeth from more than one side: the pack tears at you for {damage}."
                    : $"The iron hound's bite worries you for {damage}.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 10) StepBfsToward(monster);
    }

    /// <summary>
    /// The fort's watch (D-053): the game's answer to the loosed line. The
    /// linden board is raised against the far thing: a shaft loosed at a
    /// walking carl stops in the wood and teaches nothing. The board leaves
    /// its line only while the seax is about its blow, and in the blown turns
    /// after: those are the archer's windows. Up close the axe comes under the
    /// board, and the carl fights like any patient dead thing.
    /// </summary>
    private void ActCarl(Monster monster)
    {
        if (monster.ExposedTurns > 0) { monster.ExposedTurns--; return; }
        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist == 1)
        {
            // The board-check (D-129): the guard war's own verb, the whole
            // board put behind a shoulder and aimed at the guard's line, not
            // the blood. A sundered board (D-095) has no check left in it.
            if (!monster.BoardBroken && _combatRng.Chance(0.25))
            {
                monster.Intent = new Intent { Kind = IntentKind.BoardCheck, TargetCell = Player.Pos };
                Log.Add(Turn, "The shield-carl squares the whole board behind its shoulder!", LogTone.Danger);
            }
            else if (_combatRng.Chance(0.4))
            {
                monster.Intent = new Intent { Kind = IntentKind.SeaxStab, TargetCell = Player.Pos };
                Log.Add(Turn, "The shield-carl locks its board and draws the seax back behind it!", LogTone.Danger);
            }
            else if (_combatRng.Chance(Player.DodgeChance))
            {
                Log.Add(Turn, "The board's iron rim sweeps past; you are not where it looked.", LogTone.Combat);
            }
            else
            {
                int damage = Absorb(_combatRng.Range(2, 4));
                Player.Hp -= damage;
                Log.Add(Turn, $"The board's iron rim clips you for {damage}.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 10 && Turn % 2 == 0) StepBfsToward(monster);
    }

    /// <summary>
    /// The fort's beasts (D-053): the charger. A war-boar with a clear straight
    /// lane and room for a run-up wheels onto it and comes: the same clean
    /// lines a bowman wants are the lanes it runs, so range is never safety
    /// here. It cannot charge from beside you: closing is the counterplay,
    /// where the tusks can only worry. A missed charge leaves it blown.
    /// </summary>
    private void ActBoar(Monster monster)
    {
        if (monster.ExposedTurns > 0) { monster.ExposedTurns--; return; }
        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist >= 3 && dist <= 8 && ChargeLaneClear(monster) && _combatRng.Chance(0.6))
        {
            monster.Intent = new Intent { Kind = IntentKind.BoarCharge, TargetCell = Player.Pos };
            Log.Add(Turn, "The war-boar wheels onto your line: hoof-scrape, dropped head, one long breath!", LogTone.Danger);
            return;
        }

        if (dist == 1)
        {
            if (_combatRng.Chance(Player.DodgeChance))
            {
                Log.Add(Turn, "The tusks hook air; the boar shoulders past.", LogTone.Combat);
            }
            else
            {
                int damage = Absorb(_combatRng.Range(2, 5));
                Player.Hp -= damage;
                Log.Add(Turn, $"Close in, the boar has no run: the tusks can only rake you for {damage}.", LogTone.Combat);
            }
            return;
        }

        if (dist <= 12) StepBfsToward(monster);
    }

    /// <summary>How near the bearer must come before a grazing hart bolts (D-070): a stalk closes to here, then the hunt is on.</summary>
    private const int HartFleeRange = 5;

    /// <summary>
    /// The wilds' game (D-070): a hart grazes until the bearer is within a few cells,
    /// then flees, keeping its distance at the bearer's own speed, so it is never run
    /// down on foot. It is taken by the bow while it grazes or mid-flight, or herded
    /// into a corner where no step gains it distance and a bump ends it. A hart that
    /// reaches a run in the treeline (any walkable border cell) is gone into the deep
    /// wood and leaves nothing, because HarvestRemains is never called for it. Nothing
    /// the hart does damages the bearer: the whole of the challenge is the catch.
    /// </summary>
    private void ActHart(Monster monster)
    {
        if (monster.Pos.Chebyshev(Player.Pos) > HartFleeRange) return; // grazing: the stalk is the bearer's to close.

        StepAwayFrom(monster);

        var map = CurrentSite!.Map;
        var p = monster.Pos;
        if (p.X == 0 || p.X == map.Width - 1 || p.Y == 0 || p.Y == map.Height - 1)
        {
            monster.Hp = 0; // through the treeline and gone: no HarvestRemains, so no yield.
            Log.Add(Turn, "The hart finds a run in the treeline and takes it, and the wood closes behind it. Lost.", LogTone.Info);
            CheckSiteCleared(CurrentSite!);
        }
    }

    /// <summary>The flee (D-070): a greedy step keeping the most distance from the bearer, the mirror of <see cref="StepToward"/>. No open step that gains distance means cornered: the hart holds, and a bump ends it.</summary>
    private void StepAwayFrom(Monster monster)
    {
        var map = CurrentSite!.Map;
        var best = monster.Pos;
        int bestDist = monster.Pos.Manhattan(Player.Pos);
        foreach (var (dx, dy) in Directions.All8)
        {
            var next = monster.Pos.Plus(dx, dy);
            if (!map.Walkable(next)) continue;
            if (next == Player.Pos) continue;
            if (Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == next)) continue;
            int d = next.Manhattan(Player.Pos);
            if (d > bestDist) { bestDist = d; best = next; }
        }
        monster.Pos = best;
    }

    /// <summary>A straight lane (one of the eight lines) from boar to bearer, every cell open: charge country.</summary>
    private bool ChargeLaneClear(Monster monster)
    {
        int dx = Player.Pos.X - monster.Pos.X, dy = Player.Pos.Y - monster.Pos.Y;
        if (dx != 0 && dy != 0 && Math.Abs(dx) != Math.Abs(dy)) return false;
        var map = CurrentSite!.Map;
        var p = monster.Pos;
        int sx = Math.Sign(dx), sy = Math.Sign(dy);
        while (true)
        {
            p = p.Plus(sx, sy);
            if (p == Player.Pos) return true;
            if (!map.Walkable(p) || Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == p))
                return false;
        }
    }

    /// <summary>
    /// The charge resolved (D-053): the boar runs its declared lane the length
    /// of it. The lane is dodged sideways, never backward: it runs through
    /// where you were and on through where you are, if where you are is still
    /// on the line. A missed charge leaves it standing blown: the window the
    /// fort teaches.
    /// </summary>
    private void ResolveCharge(Monster monster, Intent intent)
    {
        var map = CurrentSite!.Map;
        int sx = Math.Sign(intent.TargetCell.X - monster.Pos.X);
        int sy = Math.Sign(intent.TargetCell.Y - monster.Pos.Y);
        for (int step = 0; step < BowRange; step++)
        {
            var next = monster.Pos.Plus(sx, sy);
            if (next == Player.Pos)
            {
                int damage = Absorb(_combatRng.Range(7, 11), telegraphed: true);
                Player.Hp -= damage;
                Log.Add(Turn, $"The war-boar takes you full on the tusks for {damage}: the lane was its whole argument!", LogTone.Danger);
                // Sheer mass (D-126): the charge rocks the guard the way the
                // heave rocks a foe's, by weight alone.
                RockBearer(GuardBreak.BearerCharge);
                return;
            }
            if (!map.Walkable(next) || Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == next))
            {
                monster.ExposedTurns = 2;
                Log.Add(Turn, "The charge slams to its end against stone; the boar stands blown, flanks heaving.", LogTone.Combat);
                return;
            }
            monster.Pos = next;
        }
        monster.ExposedTurns = 2;
        Log.Add(Turn, "The boar ploughs through where you stood and slews to a stop, blown.", LogTone.Combat);
    }

    /// <summary>How far a warder's loft carries: past the bows, because the mere was dug to be sat behind.</summary>
    public const int LoftRange = 10;

    /// <summary>
    /// The leaguer's watch (D-057): board and sling, the shielded thrower the
    /// fort deferred. The board is the carl's rule at the sling's range: shafts
    /// and thrust points stop in the linden while the warder stands its ground
    /// or gives it, and only the whirl and the blown turns after the cast open
    /// it. The loft needs no line of sight: the stone comes over banks and
    /// mounds both, so cover is no roof here; feet are. It never advances:
    /// crowded, it gives ground to reopen its range, and only cornered against
    /// its own works does the board's rim come down. Chasing one pins it
    /// silent; its fellows loft at the chaser: the leaguer's whole argument.
    /// </summary>
    private void ActWarder(Monster monster)
    {
        if (monster.Dormant)
        {
            if (monster.Pos.Chebyshev(Player.Pos) <= 8
                && CurrentSite!.Map.LineOfSight(monster.Pos, Player.Pos))
                RouseLeaguer(monster);
            return;
        }

        if (monster.ExposedTurns > 0) { monster.ExposedTurns--; return; }
        int dist = monster.Pos.Chebyshev(Player.Pos);

        if (dist <= 2)
        {
            if (BestRetreat(monster) is { } back)
            {
                if (dist == 1)
                    Log.Add(Turn, "The sling-warder backs off behind its board, giving ground it clearly knows by heart.", LogTone.Combat);
                monster.Pos = back;
                return;
            }
            if (dist == 1)
            {
                // Cornered, the rim is all it has: this kind was never the
                // fort's melee half, and it knows it.
                if (_combatRng.Chance(Player.DodgeChance))
                {
                    Log.Add(Turn, "Backed to the water's edge, the warder swings its rim wide of where you were.", LogTone.Combat);
                }
                else
                {
                    int damage = Absorb(_combatRng.Range(2, 4));
                    Player.Hp -= damage;
                    Log.Add(Turn, $"Cornered, the sling-warder cracks its board's rim across you for {damage}.", LogTone.Combat);
                }
            }
            return;
        }

        if (dist <= LoftRange && _combatRng.Chance(0.5))
        {
            monster.Intent = new Intent
            {
                Kind = IntentKind.LoftedStone,
                TargetCell = Player.Pos,
                TurnsUntilResolve = 2,
            };
            Log.Add(Turn, "Across the works a sling rises into the whirl: a low whirring, climbing!", LogTone.Danger);
        }
        // Out of the sling's reach it does nothing at all: the leaguer holds
        // the line it was set, and has held it through longer waits than you.
    }

    /// <summary>The cardinal step that opens the most ground, dry-footed; null when the works allow none.</summary>
    private Pos? BestRetreat(Monster monster)
    {
        Pos? best = null;
        int bestDist = monster.Pos.Chebyshev(Player.Pos);
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var p = monster.Pos.Plus(dx, dy);
            if (!CurrentSite!.Map.Walkable(p) || p == Player.Pos
                || Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == p))
                continue;
            int d = p.Chebyshev(Player.Pos);
            if (d > bestDist) { bestDist = d; best = p; }
        }
        return best;
    }

    /// <summary>
    /// The horn (D-057): the leaguer wakes as one, or not at all. Five bands
    /// of the deep taught the dark to come at the bearer a tenant at a time;
    /// the works were dug by soldiers, and soldiers post a signal.
    /// </summary>
    private void RouseLeaguer(Monster sighted)
    {
        if (!sighted.Dormant) return;
        foreach (var m in Monsters.Where(m => m.Alive && m.SiteId == sighted.SiteId && m.Kind == MonsterKind.Warder))
            m.Dormant = false;
        Log.Add(Turn, "A horn sounds low across the water, cracked with age, and every board on the banks comes up as one.", LogTone.Danger);
    }

    /// <summary>
    /// The stone comes down (D-057). Standing on the mark is the full price,
    /// standing beside it is a graze, and two strides of honest walking is the
    /// whole dodge: the loft is dodged by feet that keep moving, never by
    /// cover. The cast made, the board hangs wide: the window the leaguer
    /// teaches, twinned with the whirl that came before it.
    /// </summary>
    private void ResolveLoft(Monster monster, Intent intent)
    {
        int dist = Player.Pos.Chebyshev(intent.TargetCell);
        if (dist == 0)
        {
            int damage = Absorb(_combatRng.Range(7, 11), telegraphed: true);
            Player.Hp -= damage;
            Log.Add(Turn, $"The sling-stone comes down out of the sky's top and takes you square for {damage}!", LogTone.Danger);
        }
        else if (dist == 1)
        {
            int damage = Absorb(Math.Max(1, _combatRng.Range(7, 11) / 2), telegraphed: true);
            Player.Hp -= damage;
            Log.Add(Turn, $"The stone bursts a stride off: shards and mere-mud rake you for {damage}.", LogTone.Danger);
        }
        else
        {
            Log.Add(Turn, "The stone comes down where you stood and bursts, loud over the water.", LogTone.Combat);
        }
        monster.ExposedTurns = 2;
        Log.Add(Turn, "The cast made, the sling-warder's board hangs wide while the arm gathers back.", LogTone.Combat);
    }

    /// <summary>
    /// The sword-thegn (D-058): the even hand made flesh (D-055 named the
    /// principle; this is the foe drilled into it). It was taught to never
    /// strike first and only to answer, and the teaching has outlived the war,
    /// the cause, and the hand that gave it. So it comes on at a walk and trades
    /// measured blows, patient, and waits. Its teeth is the counter: a heave
    /// wound up within its reach is the opening it has waited an age for, and it
    /// steps inside the winding blow and breaks it unthrown, the checked swing
    /// (D-055) turned at last on the bearer. A blow it must still close on it
    /// cannot answer the same way; there is nothing yet to step inside. Its
    /// counter spent, it stands a breath open, and a reader's own commitment can
    /// be read: bait the answer and the guard is there to be cracked.
    /// </summary>
    private void ActThegn(Monster monster)
    {
        if (monster.ExposedTurns > 0) { monster.ExposedTurns--; return; }

        int dist = monster.Pos.Chebyshev(Player.Pos);

        // The counter: a heave wound up in its face dies half-drawn, and the
        // point comes back on the bearer. Telegraph-free: this is a read, and a
        // read finds you. The spent counter opens it for a breath.
        if (dist == 1 && Player.HeaveTarget is not null)
        {
            Player.HeaveTarget = null;
            int damage = Absorb(_combatRng.Range(5, 9));
            Player.Hp -= damage;
            // The counter kills on the bearer's own turn, outside the monster
            // loop, so the death's shape (D-098) is written here.
            if (Player.Hp <= 0) { _deathShape = (monster.Kind, null); _deathHand = monster; }
            monster.ExposedTurns = 1;
            Log.Add(Turn, $"The sword-thegn was waiting for exactly this. It steps inside the winding blow: your heave dies half-drawn as the point comes back and finds you for {damage}.", LogTone.Danger);
            Log.Add(Turn, "Its counter spent, the sword-thegn stands a breath out of its guard.", LogTone.Combat);
            return;
        }

        if (dist == 1)
        {
            // The feint (D-096): the thegn's one marked blow, and the mark lies
            // to any bearer whose read of the kind is short of keen. The shown
            // cell is a neighbor of the truth; the point was always coming to
            // the ground the bearer keeps. A keen read is shown the truth.
            if (_combatRng.Chance(0.3))
            {
                if (Player.ReadOf(MonsterKind.Thegn, Cycle) < ReadTier.Keen)
                {
                    var lies = new List<Pos>();
                    foreach (var (dx, dy) in Directions.All8)
                    {
                        var p = Player.Pos.Plus(dx, dy);
                        if (p != monster.Pos && CurrentMap.Walkable(p)) lies.Add(p);
                    }
                    var shown = lies.Count > 0 ? lies[_combatRng.Next(lies.Count)] : Player.Pos;
                    monster.Intent = new Intent { Kind = IntentKind.MeasuredCut, TargetCell = shown, FeintCell = Player.Pos };
                }
                else
                {
                    monster.Intent = new Intent { Kind = IntentKind.MeasuredCut, TargetCell = Player.Pos };
                }
                Log.Add(Turn, "The sword-thegn's point rises and settles on its mark, unhurried and certain.", LogTone.Danger);
                return;
            }
            if (_combatRng.Chance(0.5))
            {
                if (_combatRng.Chance(Player.DodgeChance))
                {
                    Log.Add(Turn, "The sword-thegn's measured cut you turn aside.", LogTone.Combat);
                }
                else
                {
                    int damage = Absorb(_combatRng.Range(2, 5));
                    Player.Hp -= damage;
                    Log.Add(Turn, $"The sword-thegn's cut comes in unhurried and certain and opens you for {damage}.", LogTone.Combat);
                }
            }
            else
            {
                Log.Add(Turn, "The sword-thegn holds its guard and watches your hands, waiting for you to over-reach.", LogTone.Combat);
            }
            return;
        }

        // A swordsman's feet, not a shield's: it closes at a walk, every turn,
        // wanting the bind the counter needs.
        if (dist <= 10) StepBfsToward(monster);
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
                        && !FellowAt(next)
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

    private void StepToward(Monster monster) => StepTowardPos(monster, Player.Pos);

    private void StepTowardPos(Monster monster, Pos goal)
    {
        var map = CurrentSite!.Map;
        var best = monster.Pos;
        int bestDist = monster.Pos.Manhattan(goal);
        foreach (var (dx, dy) in Directions.All8)
        {
            var next = monster.Pos.Plus(dx, dy);
            if (!map.Walkable(next)) continue;
            if (next == Player.Pos) continue;
            if (FellowAt(next)) continue;
            if (Monsters.Any(m => m.Alive && m != monster && m.Pos == next)) continue;
            int d = next.Manhattan(goal);
            if (d < bestDist) { bestDist = d; best = next; }
        }
        monster.Pos = best;
    }

    /// <summary>The hand and wind-up that dropped the bearer this turn (D-098), so the scar can match the death. Replay-derived, never serialized.</summary>
    private (MonsterKind Kind, IntentKind? Windup)? _deathShape;

    /// <summary>The very hand that dropped the bearer (D-110), so the boast can find its owner. Replay-derived, never serialized.</summary>
    private Monster? _deathHand;

    /// <summary>Whether this world's camp has put a voice to its chief's name (D-110): the first descent's announcement.</summary>
    private bool _rosterMet;

    /// <summary>
    /// A death above the line converts (D-098): the scar the death's shape asks
    /// for where it can, then the fixed order among the marks not yet carried,
    /// so the landing is replay-clean without a die. A bearer already carrying
    /// all three has nothing left the count can keep.
    /// </summary>
    private void LandScar()
    {
        var matched = DeathsToll.Match(_deathShape?.Kind, _deathShape?.Windup);
        ScarId[] order = matched is { } m
            ? [m, ScarId.TakenEye, ScarId.CrushedHand, ScarId.HauntedLook]
            : [ScarId.TakenEye, ScarId.CrushedHand, ScarId.HauntedLook];
        foreach (var scar in order)
        {
            if (Player.HasScar(scar)) continue;
            Player.Scars.Add(scar);
            Log.Add(Turn, $"Something does not come all the way back with you: {DeathsToll.NameOf(scar)}.", LogTone.Danger);
            Log.Add(Turn, DeathsToll.CostOf(scar), LogTone.Danger);
            Log.Add(Turn, $"\"{AegisVoice.ScarLine}\"", LogTone.Aegis);
            return;
        }
    }

    private void HandleDeath()
    {
        Player.Deaths++;
        InShrineMenu = false;
        InTalkMenu = false;
        InUnbindMenu = false;
        InTradeMenu = false;
        InThresholdMenu = false;
        InLayingMenu = false;
        InCrossingMenu = false;
        _chosenOaths.Clear();
        InGearMenu = false;
        InSheetMenu = false;
        InCastMenu = false;
        InCastLine = false;
        _pendingLineSpell = null;
        TalkNpc = null;
        _layingTarget = null;
        // Death drops what was held mid-swing and mid-word (D-058, D-091): the
        // fall is its own interruption, and the shrine is no place to loose either.
        Player.HeaveTarget = null;
        Player.LevinTarget = null;
        Player.WardTurns = 0;
        Player.ChilledTurns = 0;
        // The fall clears the guard with everything else (D-126): the shrine
        // hands the bearer back standing, feet under them.
        Player.PostureDmg = 0;
        Player.StaggerTurns = 0;
        // The calling slips with everything else held (D-099): whatever kept
        // the shade in the world was in the bearer's keeping, and the keeping
        // failed. It is gone before the bearer is.
        if (Shade is not null)
        {
            Shade = null;
            Log.Add(Turn, "The half-said word goes out of your mouth with everything else: the shade is gone before you are.", LogTone.Info);
        }

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
        // The boast kept (D-110): a named hand that authors the fall owns the
        // telling of it, and will still own it the next time you meet.
        if (_deathHand is { Alive: true, Kind: MonsterKind.Goblin, Epithet: { } slayer } hand)
        {
            if (!hand.SlewBearer)
            {
                hand.SlewBearer = true;
                hand.GrudgeSpoken = false;
                World.Facts.Add("nemesis", "slew_bearer", slayer,
                    $"{slayer} of the camp above {World.SettlementName} struck the blow that put the bearer down, and holds the boast.");
            }
            Log.Add(Turn, $"The last face over you is {slayer}'s, and the face is pleased. That telling belongs to the dens tonight.", LogTone.Danger);
        }
        // Dying in the camp leaves the bloodied breathing (D-110): the scars
        // are remembered on this exit like any other.
        if (CurrentSite is { Kind: SiteKind.GoblinCamp }) MarkTheScarred();
        // Death lines carry register, never plot (arc sec 4): worried once the
        // ledger is known, candid between equals once the threshold is answered.
        int register = Player.Resolution != Resolution.None ? 3 : Player.LedgerHeard ? 2 : 1;
        Log.Add(Turn, $"\"{AegisVoice.DeathLine(Player.Deaths, register)}\"", LogTone.Aegis);

        Mode = MapMode.Overworld;
        CurrentSite = null;
        Player.Pos = World.ShrinePos;
        // Whoever still walked with you kept the road home (D-097): the guest
        // is at the shrine when you wake, and says nothing about the carrying.
        // The shade is not: it unraveled with the fall (above), and the word
        // waits to be said again.
        PlaceFellowsBeside(World.ShrinePos);
        // The slow mending (D-047): the death consequence scales in magnitude,
        // never in shape (D-011): the same wound, held twice as long.
        Player.WoundedTurns = World.Oaths.Contains(OathId.SlowMending) ? 160 : 80;
        Player.Hp = Player.EffectiveMaxHp;
        Player.Stamina = Player.MaxStamina;

        Player.Focus = Player.MaxFocus;
        Log.Add(Turn, $"You wake at the shrine, wounded. The Aegis is spent; it will recover in time.", LogTone.Info);
        if (Remnant is not null)
            Log.Add(Turn, $"What you carried lies where you fell. One chance to reclaim it.", LogTone.Danger);

        // The Death's Toll (D-098, paying D-009): the judgment reads the count
        // as it stood when the bearer fell, and this death's fill lands after,
        // so the first death of a cluster warns and the next one collects. No
        // roll anywhere: the fairness was on the rail before the fall.
        bool scarring = Player.Toll >= DeathsToll.Line;
        bool heavy = _deathShape?.Kind is MonsterKind.Thegn or MonsterKind.Hart;
        Player.Toll += DeathsToll.FillFor(heavy, Player.Attributes[Attr.Will]);
        if (scarring) LandScar();
        _deathShape = null;
        _deathHand = null;
        Log.Add(Turn, $"The toll stands at {Player.Toll}. Fall again before it drains under {DeathsToll.Line} and the count will keep something.", LogTone.Danger);
    }

    // Test hooks: deterministic surgery for unit tests, never used by frontends.
    internal void Debug_SetPlayerPos(Pos p) => Player.Pos = p;
    internal void Debug_SetGuest(Guest? guest) => Guest = guest;
    internal void Debug_SetMode(MapMode mode)
    {
        Mode = mode;
        CurrentSite = mode == MapMode.Site ? World.CampSite : null;
    }
    internal void Debug_HurtPlayer(int damage) => Player.Hp -= damage;
    internal void Debug_GrantGear(string id) => AcquireGear(GearCatalog.Create(id));
    internal void Debug_LearnSpell(SpellId id)
    {
        if (!Player.HasSpell(id)) Player.Spells.Add(id);
        Player.Focus = Player.MaxFocus;
    }
    internal void Debug_ForceDeathCheck() { if (Player.Hp <= 0) HandleDeath(); }
    internal void Debug_BurnCombatRoll() => _combatRng.Chance(0.5);
    internal void Debug_Raid() => RaidTheStead();
    internal void Debug_SetMount(Mount? mount) => Mount = mount;
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
        ThresholdX: World.ThresholdSite?.OverworldPos.X ?? -1,
        ThresholdY: World.ThresholdSite?.OverworldPos.Y ?? -1,
        QuarryX: World.QuarrySite?.OverworldPos.X ?? -1,
        QuarryY: World.QuarrySite?.OverworldPos.Y ?? -1,
        QuarryCleared: World.QuarrySite?.Cleared ?? false,
        HallX: World.HallSite?.OverworldPos.X ?? -1,
        HallY: World.HallSite?.OverworldPos.Y ?? -1,
        HallCleared: World.HallSite?.Cleared ?? false,
        RingfortX: World.RingfortSite?.OverworldPos.X ?? -1,
        RingfortY: World.RingfortSite?.OverworldPos.Y ?? -1,
        RingfortCleared: World.RingfortSite?.Cleared ?? false,
        WildsX: World.WildsSite?.OverworldPos.X ?? -1,
        WildsY: World.WildsSite?.OverworldPos.Y ?? -1,
        WildsCleared: World.WildsSite?.Cleared ?? false,
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
            Player.Resolution switch
            {
                Resolution.Kept => "kept",
                Resolution.Refused => "refused",
                _ => null,
            },
        }.Where(s => s is not null)),
        CurrentSite: CurrentSite?.Id ?? "",
        UnbinderX: World.Unbinder.Pos.X,
        UnbinderY: World.Unbinder.Pos.Y,
        UnbindingsLeft: UnbindingsLeft,
        StoryTemplate: World.Facts.OfType("story").FirstOrDefault()?.Subject ?? "",
        Oaths: string.Join(",", World.Oaths.Select(OathCatalog.IdOf)),
        Burden: Burden,
        Hp: Player.Hp,
        MaxHp: Player.EffectiveMaxHp,
        Stamina: Player.Stamina,
        MaxStamina: Player.MaxStamina,
        Coin: Player.Coin,
        Essence: Player.Essence,
        Legend: Player.Legend,
        Standing: Standing,
        Title: LegendStanding.TitleOf(Standing),
        Regard: Regard,
        RegardTitle: SteadRegard.TitleOf(Regard),
        Wrath: Wrath,
        WrathTitle: RaiderWrath.TitleOf(Wrath),
        Raids: Raids,
        Stores: Stores,
        Boldness: Boldness,
        LevyStands: LevyStands,
        WatchStands: WatchStands,
        Shame: Shame,
        ShameTitle: SteadShame.TitleOf(Shame),
        Grudge: Grudge,
        GrudgeTitle: MoundGrudge.TitleOf(Grudge),
        Rations: Player.Rations,
        Hide: Player.Hide,
        RawMeat: Player.RawMeat,
        Herb: Player.Herb,
        Draughts: Player.Draughts,
        Focus: Player.Focus,
        MaxFocus: Player.MaxFocus,
        Spells: string.Join(",", Player.Spells.Select(SpellCatalog.IdOf)),
        WardTurns: Player.WardTurns,
        Folk: Player.Folk?.ToString().ToLowerInvariant() ?? "",
        Past: Player.Past?.ToString().ToLowerInvariant() ?? "",
        BearerName: Player.Name,
        Keepsake: Player.Keepsake,
        BearerBurden: Player.Burden?.ToString().ToLowerInvariant() ?? "",
        BearerVow: Player.Vow?.ToString().ToLowerInvariant() ?? "",
        Stance: Player.Stance.ToString().ToLowerInvariant(),
        RationPrice: RationPrice,
        MendPrice: Player.WoundedTurns > 0 ? MendPrice : 0,
        WeaponId: Player.Weapon?.Id ?? "",
        WeaponWear: Player.Weapon?.Wear ?? 0,
        ArmorId: Player.Armor?.Id ?? "",
        ArmorWear: Player.Armor?.Wear ?? 0,
        BowId: Player.Bow?.Id ?? "",
        BowWear: Player.Bow?.Wear ?? 0,
        PackGear: string.Join(",", Player.Pack.Select(g => g.Id)),
        RepairPrice: RepairPrice,
        SmithX: World.Smith.Pos.X,
        SmithY: World.Smith.Pos.Y,
        SonghallX: World.SonghallSite.OverworldPos.X,
        SonghallY: World.SonghallSite.OverworldPos.Y,
        SkaldX: World.Skald.Pos.X,
        SkaldY: World.Skald.Pos.Y,
        PledgedDeeds: string.Join(",", Player.PledgedDeeds.Select(PatronCatalog.IdOf)),
        PatronDeeds: string.Join(",", Player.PatronDeeds.Select(PatronCatalog.IdOf)),
        Skills: string.Join(",", Enum.GetValues<SkillId>()
            .Select(s => $"{SkillSet.NameOf(s).ToLowerInvariant()}:{Player.Skills.Level(s)}:{Player.Skills.Uses(s)}")),
        Perks: string.Join(",", Player.Perks.Select(PerkCatalog.IdOf)),
        PendingKnack: PendingKnack is { } knack ? $"{SkillSet.NameOf(knack.Skill).ToLowerInvariant()} {knack.Level}" : "",
        Lessons: string.Join(",", Player.Lessons.Select(LessonCatalog.IdOf)),
        Reads: string.Join(",", Player.Reads.OrderBy(kv => (int)kv.Key)
            .Select(kv => $"{kv.Key.ToString().ToLowerInvariant()}:{kv.Value}")),
        ReadTiers: string.Join(",", Player.Reads.OrderBy(kv => (int)kv.Key)
            .Select(kv => $"{kv.Key.ToString().ToLowerInvariant()}:{Player.ReadOf(kv.Key, Cycle)}")),
        Gleanings: World.Gleanings.Count,
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
        InTradeMenu: InTradeMenu,
        InBonesMenu: InBonesMenu,
        BonesNet: BonesNet,
        InThresholdMenu: InThresholdMenu,
        InLayingMenu: InLayingMenu,
        InGearMenu: InGearMenu,
        InSheetMenu: InSheetMenu,
        InCrossingMenu: InCrossingMenu,
        InAim: InAim,
        InThrust: InThrust,
        InHeave: InHeave,
        InCastMenu: InCastMenu,
        InCastLine: InCastLine,
        InCreation: InCreation,
        HeaveLoaded: Player.HeaveTarget is not null,
        LevinLoaded: Player.LevinTarget is not null,
        TalkNpc: TalkNpc?.Name ?? "",
        WoundedTurns: Player.WoundedTurns,
        GuardWorn: Player.PostureDmg,
        StaggerTurns: Player.StaggerTurns,
        Deaths: Player.Deaths,
        Toll: Player.Toll,
        Scars: string.Join(",", Player.Scars.Select(s => s.ToString().ToLowerInvariant())),
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
    int ThresholdX,
    int ThresholdY,
    int QuarryX,
    int QuarryY,
    bool QuarryCleared,
    int HallX,
    int HallY,
    bool HallCleared,
    int RingfortX,
    int RingfortY,
    bool RingfortCleared,
    int WildsX,
    int WildsY,
    bool WildsCleared,
    string ArcProgress,
    string CurrentSite,
    int UnbinderX,
    int UnbinderY,
    int UnbindingsLeft,
    string StoryTemplate,
    string Oaths,
    int Burden,
    int Hp,
    int MaxHp,
    int Stamina,
    int MaxStamina,
    int Coin,
    int Essence,
    int Legend,
    int Standing,
    string Title,
    int Regard,
    string RegardTitle,
    int Wrath,
    string WrathTitle,
    int Raids,
    int Stores,
    int Boldness,
    bool LevyStands,
    bool WatchStands,
    int Shame,
    string ShameTitle,
    int Grudge,
    string GrudgeTitle,
    int Rations,
    int Hide,
    int RawMeat,
    int Herb,
    int Draughts,
    int Focus,
    int MaxFocus,
    string Spells,
    int WardTurns,
    string Folk,
    string Past,
    string BearerName,
    bool Keepsake,
    string BearerBurden,
    string BearerVow,
    string Stance,
    int RationPrice,
    int MendPrice,
    string WeaponId,
    int WeaponWear,
    string ArmorId,
    int ArmorWear,
    string BowId,
    int BowWear,
    string PackGear,
    int RepairPrice,
    int SmithX,
    int SmithY,
    int SonghallX,
    int SonghallY,
    int SkaldX,
    int SkaldY,
    string PledgedDeeds,
    string PatronDeeds,
    string Skills,
    string Perks,
    string PendingKnack,
    string Lessons,
    string Reads,
    string ReadTiers,
    int Gleanings,
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
    bool InTradeMenu,
    bool InBonesMenu,
    int BonesNet,
    bool InThresholdMenu,
    bool InLayingMenu,
    bool InGearMenu,
    bool InSheetMenu,
    bool InCrossingMenu,
    bool InAim,
    bool InThrust,
    bool InHeave,
    bool InCastMenu,
    bool InCastLine,
    bool InCreation,
    bool HeaveLoaded,
    bool LevinLoaded,
    string TalkNpc,
    int WoundedTurns,
    int GuardWorn,
    int StaggerTurns,
    int Deaths,
    int Toll,
    string Scars,
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

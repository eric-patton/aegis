using System.Text;
using Aegis.Core;

namespace Aegis.Cli;

/// <summary>
/// The ladder-climbing driver (D-062): `aegis journey --seed N --cycles K` builds a real
/// world and hands it to <see cref="JourneyPilot"/>, which plays it through the key
/// surface, world after world, up to K crossings. In each world it clears every site it
/// can reach and win (the camp that gates the arch, and the barrow, hollow, quarry, hall,
/// ringfort, and leaguer besides), then crosses under its own sworn terms (D-069). It
/// reports what it cleared, the arc it walked, the burden it took up and the Legend that
/// bought, and above all what the bearer's bestiary read on either side of each arch: the
/// bank carried whole, the read softened by the harder ground (D-061). Because the pilot is a pure
/// function of state, `--seed N` reruns identically, so the crossing evidence is
/// reproducible, not a one-off hand-driven session.
///
/// It drives the same <see cref="Game"/> the shipped binary runs, through
/// <see cref="Game.ApplyKey"/> alone: no debug hook, no shortcut. Every crossing it prints
/// is a crossing the engine actually made.
/// </summary>
public static class JourneyRunner
{
    private readonly record struct Read(MonsterKind Kind, int Bank, ReadTier Tier);

    private readonly record struct SiteOutcome(string Name, bool Cleared, bool Skipped);

    private sealed record Crossing(
        int FromCycle, string FromWorld, string FromTwist, int ToCycle, string ToWorld, string ToTwist,
        int Turn, int DeathsInWorld, string Arms, IReadOnlyList<OathId> Sworn, int Burden,
        IReadOnlyList<SiteOutcome> Sites, IReadOnlyList<Read> Before, IReadOnlyList<Read> After);

    public static int Run(string[] args)
    {
        ulong seed = 42;
        int cycles = 3;
        int maxKeys = 400000;
        int perWorldBudget = 60000;
        int siteKeyBudget = 3000;   // in-site keys spent on one site before writing it off.
        int siteDeathBudget = 8;    // deaths at one site before writing it off.
        bool emitKeys = false;
        bool json = false;
        bool wits = false; // the perception-build demo (D-084): the eye raised first.

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--seed": seed = ulong.Parse(args[++i]); break;
                case "--cycles": cycles = int.Parse(args[++i]); break;
                case "--max-keys": maxKeys = int.Parse(args[++i]); break;
                case "--per-world": perWorldBudget = int.Parse(args[++i]); break;
                case "--site-keys": siteKeyBudget = int.Parse(args[++i]); break;
                case "--site-deaths": siteDeathBudget = int.Parse(args[++i]); break;
                case "--emit-keys": emitKeys = true; break;
                case "--json": json = true; break;
                case "--wits": wits = true; break;
                default:
                    Console.Error.WriteLine($"aegis journey: unexpected argument '{args[i]}'");
                    return 1;
            }
        }

        var game = new Game(seed, firstWake: true);
        int targetCycle = game.Cycle + cycles;
        var keys = new StringBuilder();
        var crossings = new List<Crossing>();

        // Per-world bookkeeping: which sites we have given up on, and how much each has
        // cost so far. All of it resets at a crossing, because the next world's sites are
        // freshly generated (and may reuse an id).
        var skip = new HashSet<string>();
        var siteKeys = new Dictionary<string, int>();
        var siteDeaths = new Dictionary<string, int>();

        int totalKeys = 0;
        int keysThisWorld = 0;
        int prevDeaths = 0;
        int deathsThisWorld = 0;
        // What the reclaim won back (D-065): a remnant taken is one the crossing did not
        // forfeit. Counted by watching the drop vanish with no death and no crossing to
        // explain it, which leaves only a reclaim (the 'g' that DoGrab honors).
        int remnantsReclaimed = 0;
        int coinReclaimed = 0;
        int essenceReclaimed = 0;
        // What the looting brought up (D-066): every cleared site's chest opened for its
        // coin, and in the deep sites a piece of iron the smith never stocks. Counted by
        // watching the chest's own flag flip as the 'g' lands on its cell.
        int chestsLooted = 0;
        int chestCoin = 0;
        int gearTaken = 0;
        // What the sheet answered (D-067): every threshold question taken as it opened,
        // the knack chosen for good. Counted by watching the perk list grow on the key
        // that lands the answer (choosing a knack is the only thing that grows it, by one).
        int knacksTaken = 0;
        // How far the arc was walked (D-068): the reveal ladder climbed by real feet, the
        // keeping answered at the Hearth, then a keeper laid down and the next mended,
        // D-060's rarest grace driven live. Each is caught by watching its flag turn, on
        // the one key that turns it, so the cycle it turns on is the cycle it happened.
        var resolvedAs = Resolution.None;
        int resolvedCycle = 0;
        int laidCycle = 0;
        int mendedCycle = 0;
        // What the sworn terms earned (D-069): the bot sets its own harder walking at each
        // arch, and the burden it carries through a world is honored in Legend on the crossing
        // out of it (10 per weight). Summed by reading the leaving world's burden as each
        // crossing fires, which is exactly the count the engine turns into Legend right then.
        int legendFromBurden = 0;
        // What the hunt brought in (D-070): hides taken off the wilds, watched the same
        // deterministic way as the loot and the knacks, by the counter's rise on the key
        // that lands a hart. Meat folds into rations, so the hides are the clean signal.
        int hidesTaken = 0;
        // What the trade brought in (D-072): the same counter falling to zero at the bench,
        // and the coin it fetched, so the hunt's whole loop (catch, cure, sell) shows in the
        // report the way arming and looting already do.
        int hidesSold = 0;
        int coinFromHides = 0;
        // What the fire made of the hunt (D-073): raw meat only ever falls at the bench, as
        // it cooks down to rations, so the drop and the rations that answered it are the
        // clean signal, watched the same deterministic way.
        int meatCooked = 0;
        int rationsCooked = 0;
        // What the wood gave (D-074/D-075): herbs foraged and the coin they fetched, the
        // counter rising on a step in the wood and falling to zero at the bench.
        int herbsForaged = 0;
        int herbsSold = 0;
        int coinFromHerbs = 0;
        // The steeping and the swallow (D-090): vials drawn at the stillroom and
        // drunk where the road hurt, watched by the satchel's own count.
        int draughtsDrawn = 0;
        int draughtsDrunk = 0;
        // The words (D-091, D-099): graven words taken, wards said, shades called,
        // watched by the learn-list, the ward clock rising, and the slot filling.
        int wordsLearned = 0;
        int wardsSaid = 0;
        int shadesCalled = 0;
        // The beasts (D-100): mules bought, coursers claimed off the deed, and the
        // coin the saddlebags ever carried, watched by ownership and the bags' rise.
        int mulesBought = 0;
        int coursersTaken = 0;
        int coinBanked = 0;
        // The east road (D-138): mouths taken and nights camped, travel-as-play's own tallies.
        int roadsTaken = 0;
        int nightsCamped = 0;
        // The high fells (D-146): climbs onto the frontier, the third country's tally.
        int fellsTaken = 0;
        // The market town (D-140): gates walked; the lots sold are Commerce's own uses.
        int marketsWalked = 0;
        // The school and the bond (D-141): forge sittings, and bonds sworn world by world.
        int forgeSittings = 0;
        int bondsSworn = 0;
        // The caravan leg (D-144): sacks loaded at the cart, sacks sold at the counter.
        int saltBought = 0;
        int saltSold = 0;
        // The fells' own trade (D-153): raw iron worked, smelted, then sold
        // as blooms through the guild, watched by the two carried counters.
        int tarnIronMined = 0;
        int tarnIronSmelted = 0;
        int ironBloomsTempered = 0;
        int ironItemsTempered = 0;
        int ironBloomsSold = 0;
        int coinFromIron = 0;
        // The black tarn (D-156): catch, cooking, and the town sale, watched
        // through the carried fish counter and the exact key that spends it.
        int fishCaught = 0;
        int fishCooked = 0;
        int fishRations = 0;
        int fishSold = 0;
        int coinFromFish = 0;
        // The stead's regard at its height (D-076): a per-world Fame, reset at every
        // crossing, so the run's peak is the warmest any one stead came to hold the
        // bearer, watched by the counter's high-water mark on every key.
        int maxRegard = 0;
        int maxWrath = 0;
        int raidsSuffered = 0;
        int prevRaids = 0;
        var weatherTicks = Enum.GetValues<ClimateBand>()
            .SelectMany(b => Enum.GetValues<WeatherFamily>().Select(f => $"{b.ToString().ToLowerInvariant()}:{f.ToString().ToLowerInvariant()}"))
            .ToDictionary(k => k, _ => 0, StringComparer.Ordinal);
        var exposedCamps = Enum.GetValues<WeatherFamily>()
            .ToDictionary(f => f.ToString().ToLowerInvariant(), _ => 0, StringComparer.Ordinal);
        foreach (var band in Enum.GetValues<ClimateBand>())
            weatherTicks[$"{band.ToString().ToLowerInvariant()}:{game.WeatherAt(band).ToString().ToLowerInvariant()}"]++;
        int forecastDeferrals = 0;
        int coldCampRefusals = 0;
        int hayingDays = 0;
        int lateFrosts = 0;
        int granaryPreventions = 0;
        int bargainsOffered = 0;
        int bargainsBought = 0;
        int bargainsRefused = 0;
        int bargainsExpired = 0;
        bool forecastDeferralActive = false;
        string stop;

        while (true)
        {
            if (!game.Running) { stop = "the bearer fell for good (the run ended)"; break; }
            if (game.Cycle >= targetCycle) { stop = $"reached the target of {cycles} crossing(s)"; break; }
            if (totalKeys >= maxKeys) { stop = $"hit the {maxKeys}-key safety cap"; break; }
            if (keysThisWorld >= perWorldBudget)
            {
                stop = $"stuck in cycle {game.Cycle} (tier {game.World.Tier}) after {keysThisWorld} keys, {Where(game)}";
                break;
            }

            int cycleBefore = game.Cycle;
            string worldBefore = game.World.Name;
            string twistBefore = WorldTwistCatalog.IdOf(game.World.Twist);
            var beforeReads = Bestiary(game, cycleBefore);
            var sitesBefore = SiteStates(game, skip);

            // The site the bot is fighting in right now, if any (the camp is never given
            // up: a crossing needs it, and it is always winnable). Budget it here, before
            // asking the pilot, so a fresh skip is honored on the very same tick.
            string? activeSite = game.Mode == MapMode.Site && game.CurrentSite is { } cs
                && !cs.Cleared && !skip.Contains(cs.Id) && cs.Kind != SiteKind.GoblinCamp
                    ? cs.Id : null;
            if (activeSite is not null)
            {
                int k = siteKeys.GetValueOrDefault(activeSite) + 1;
                siteKeys[activeSite] = k;
                if (k > siteKeyBudget) skip.Add(activeSite);
            }

            char? key = JourneyPilot.NextKey(game, skip, wits);
            // The door counts as the site (D-146's lesson): a death on the very
            // key that steps in (a pack waiting at the mouth) must land on the
            // site's own death budget, or the give-up machinery never sees a
            // site that kills at its threshold and the run loops on the door.
            if (activeSite is null && key == '>' && game.Mode == MapMode.Overworld
                && game.World.Sites.FirstOrDefault(s => s.Area == game.Area
                    && s.OverworldPos == game.Player.Pos) is { } entering
                && !entering.Cleared && !skip.Contains(entering.Id)
                && entering.Kind != SiteKind.GoblinCamp)
                activeSite = entering.Id;
            if (key is null)
            {
                // Cannot win or even reach a foe here: write the site off and move on.
                // (skip.Add returns false if it was already written off, meaning we cannot
                // even reach the ladder to leave: that, and any dead end in the camp or on
                // the overworld, is genuinely terminal.)
                if (game.Mode == MapMode.Site && game.CurrentSite is { } deadSite
                    && deadSite.Kind != SiteKind.GoblinCamp && skip.Add(deadSite.Id))
                    continue;
                stop = $"no move available in cycle {game.Cycle} (tier {game.World.Tier}), {Where(game)}";
                break;
            }

            var remnantBefore = game.Remnant;
            var chestSite = game.CurrentSite;
            bool chestOpenBefore = chestSite?.ChestLooted ?? true;
            int coinBefore = game.Player.Coin;
            int gearBefore = game.Player.AllGear.Count();
            int perksBefore = game.Player.Perks.Count;
            var resolutionBefore = game.Player.Resolution;
            int unboundBefore = game.Player.SeveredUnbound;
            int restoredBefore = game.Player.SeveredRestored;
            // The burden of the world being left (D-069): if this key crosses the arch, the
            // engine honors 10 per weight of it in Legend right now. Read it before the key,
            // because crossing replaces the world and clears the burden with it.
            int burdenLeftBehind = game.World.Burden;
            int hideBefore = game.Player.Hide;
            int rawBefore = game.Player.RawMeat;
            int rationsBefore = game.Player.Rations;
            int herbBefore = game.Player.Herb;
            int draughtsBefore = game.Player.Draughts;
            int spellsBefore = game.Player.Spells.Count;
            int wardBefore = game.Player.WardTurns;
            bool shadeBefore = game.Shade is not null;
            bool muleBefore = OwnsBeast(game, MountKind.Mule);
            bool courserBefore = OwnsBeast(game, MountKind.Courser);
            int bagsBefore = TotalBags(game);
            var areaBefore = game.Area;
            int turnBefore = game.Turn;
            bool inTownBefore = game.CurrentSite?.Kind == SiteKind.Town;
            int smithUsesBefore = game.Player.Skills.Uses(SkillId.Smithing);
            bool swornBefore = game.GuildSworn;
            int saltBefore = game.Player.Salt;
            int tarnIronBefore = game.Player.TarnIron;
            int ironBloomBefore = game.Player.IronBloom;
            int fishBefore = game.Player.TarnTrout;
            int tellerTicksBefore = game.Teller.Readings.Count;
            var campWeatherBefore = game.CurrentWeather;
            bool campShelteredBefore = game.WaystoneShelter;
            bool hayingBefore = game.World.Facts.Exists("event", "haying_days");
            bool frostBefore = game.World.Facts.Exists("event", "late_frost");
            bool frostStoodBefore = game.World.Facts.Exists("event", "late_frost_stood");
            bool bargainBefore = game.World.Facts.Exists("event", "season_bargain");
            bool bargainBoughtBefore = game.World.Facts.Exists("event", "season_bargain_bought");
            bool bargainRefusedBefore = game.World.Facts.Exists("event", "season_bargain_refused");
            bool bargainExpiredBefore = game.World.Facts.Exists("event", "season_bargain_expired");
            bool forecastWait = key == '.' && game.Mode == MapMode.Overworld
                && ((game.Area == Area.Valley && game.Player.Pos == game.World.RoadMouthPos
                        && game.WeatherAt(ClimateBand.Road) == WeatherFamily.Cold
                        && game.ForecastAt(ClimateBand.Road) != WeatherFamily.Cold)
                    || (game.Area == Area.Road && game.Player.Pos == game.World.FellMouthPos
                        && game.WeatherAt(ClimateBand.Fells) == WeatherFamily.Cold
                        && game.ForecastAt(ClimateBand.Fells) != WeatherFamily.Cold));
            int temperedBefore = game.Player.AllGear.Count(item => item.TarnTempered);
            int fishRoomBefore = Math.Max(0, Game.RationCap - rationsBefore);
            int fishMadeBefore = Math.Min(Math.Min(fishBefore, fishRoomBefore)
                + game.Player.Skills.Bonus(SkillId.Cooking), fishRoomBefore);
            bool fishCookAction = fishBefore > 0 && fishRoomBefore > 0
                && (key == 'm' || (game.InTradeMenu && key >= '1'
                    && key <= '0' + game.TradeOffers.Count
                    && game.TradeOffers[key.Value - '1'].Good == TradeGood.CookFish));
            bool fishSaleAction = fishBefore > 0 && game.InTalkMenu
                && game.TalkNpc?.Id == "npc_provisioner" && key > '0' + game.Topics.Count
                && key <= '0' + game.Topics.Count + game.Offers.Count
                && game.Offers[key.Value - '1' - game.Topics.Count].Good == TradeGood.TarnTrout;
            game.ApplyKey(key.Value);
            keys.Append(key.Value);
            totalKeys++;
            keysThisWorld++;

            // A remnant that vanished this key without a fresh death or a crossing to
            // account for it was reclaimed, not forfeited: bank what it held (D-065).
            if (remnantBefore is not null && game.Remnant is null
                && game.Player.Deaths == prevDeaths && game.Cycle == cycleBefore)
            {
                remnantsReclaimed++;
                coinReclaimed += remnantBefore.Coin;
                essenceReclaimed += remnantBefore.Essence;
            }

            // A chest that opened this key (a 'g' on its cell adds coin and, in the deep
            // sites, a piece of iron): tally the coin it gave and whether iron came with
            // it (D-066). A loot never changes CurrentSite, so the held reference reads
            // its own flipped flag.
            if (chestSite is not null && !chestOpenBefore && chestSite.ChestLooted)
            {
                chestsLooted++;
                chestCoin += game.Player.Coin - coinBefore;
                if (game.Player.AllGear.Count() > gearBefore) gearTaken++;
            }

            // A perk that appeared this key is a threshold question answered (D-067):
            // the sheet's digit is the only thing that adds one, and it adds exactly one.
            if (game.Player.Perks.Count > perksBefore) knacksTaken++;

            // The arc's flags, watched the same way (D-068): the keeping answered, a keeper
            // laid down, a keeper mended. None turns twice, so the first turn dates it.
            if (resolutionBefore == Resolution.None && game.Player.Resolution != Resolution.None)
            {
                resolvedAs = game.Player.Resolution;
                resolvedCycle = game.Cycle;
            }
            if (game.Player.SeveredUnbound > unboundBefore && laidCycle == 0) laidCycle = game.Cycle;
            if (game.Player.SeveredRestored > restoredBefore) mendedCycle = game.Cycle;
            if (game.Player.Hide > hideBefore) hidesTaken += game.Player.Hide - hideBefore;
            // Hides only ever fall at the bench (a sale zeroes the lot); death and the
            // crossing carry them whole, so a drop is a sale and nothing else (D-072).
            else if (game.Player.Hide < hideBefore)
            {
                int sold = hideBefore - game.Player.Hide;
                hidesSold += sold;
                coinFromHides += sold * game.HidePrice;
            }
            // Raw meat falls only at the fire (D-073): the drop is cuts cooked, and the
            // rations that rose on the same key are the meals they became.
            if (game.Player.RawMeat < rawBefore)
            {
                meatCooked += rawBefore - game.Player.RawMeat;
                rationsCooked += Math.Max(0, game.Player.Rations - rationsBefore);
            }
            // Herbs rise on a step in the wood (foraged) and fall to zero at a bench
            // (sold): the two clean signals of the forage loop (D-074/D-075). The
            // coin is read off the key itself, since the stillroom pays the
            // apothecary's price where the wood's edge paid the middleman's (D-081/D-082).
            if (game.Player.Herb > herbBefore) herbsForaged += game.Player.Herb - herbBefore;
            else if (game.Player.Herb < herbBefore && game.Player.Draughts == draughtsBefore)
            {
                herbsSold += herbBefore - game.Player.Herb;
                coinFromHerbs += game.Player.Coin - coinBefore;
            }
            // The steeping takes its sprigs without a sale (D-090): a herb drop
            // with a vial rising on the same key is the pot, not the scales. The
            // swallow only ever falls out on the road.
            if (game.Player.Draughts > draughtsBefore) draughtsDrawn += game.Player.Draughts - draughtsBefore;
            else if (game.Player.Draughts < draughtsBefore) draughtsDrunk += draughtsBefore - game.Player.Draughts;
            // The words (D-091, D-099): the learn-list only ever grows (a stone read),
            // the ward clock only rises on the saying, the shade slot fills on the call.
            wordsLearned += Math.Max(0, game.Player.Spells.Count - spellsBefore);
            if (game.Player.WardTurns > wardBefore) wardsSaid++;
            if (game.Shade is not null && !shadeBefore) shadesCalled++;
            // The beasts (D-100): ownership appearing is a buy or the deed's gift,
            // and the bags only ever rise on a loading (the spook and the fetch
            // hand coin back, they never add any).
            if (!muleBefore && OwnsBeast(game, MountKind.Mule)) mulesBought++;
            if (!courserBefore && OwnsBeast(game, MountKind.Courser)) coursersTaken++;
            coinBanked += Math.Max(0, TotalBags(game) - bagsBefore);
            // The east road (D-138): the mouth flips the flag, and a camp key
            // that took time is a night passed at the fire, cold or fed.
            if (areaBefore == Area.Valley && game.Area == Area.Road) roadsTaken++;
            if (areaBefore == Area.Road && game.Area == Area.Fells) fellsTaken++;
            if (key == 'm' && game.Turn > turnBefore) nightsCamped++;
            if (key == 'm' && game.Turn > turnBefore && !campShelteredBefore)
                exposedCamps[campWeatherBefore.ToString().ToLowerInvariant()]++;
            if (key == 'm' && game.Turn == turnBefore && campWeatherBefore == WeatherFamily.Cold)
                coldCampRefusals++;
            if (forecastWait && !forecastDeferralActive) forecastDeferrals++;
            forecastDeferralActive = forecastWait;
            if (game.Teller.Readings.Count > tellerTicksBefore || game.Cycle != cycleBefore)
                foreach (var band in Enum.GetValues<ClimateBand>())
                    weatherTicks[$"{band.ToString().ToLowerInvariant()}:{game.WeatherAt(band).ToString().ToLowerInvariant()}"]++;
            if (!hayingBefore && game.World.Facts.Exists("event", "haying_days")) hayingDays++;
            if (!frostBefore && game.World.Facts.Exists("event", "late_frost")) lateFrosts++;
            if (!frostStoodBefore && game.World.Facts.Exists("event", "late_frost_stood")) granaryPreventions++;
            if (!bargainBefore && game.World.Facts.Exists("event", "season_bargain")) bargainsOffered++;
            if (!bargainBoughtBefore && game.World.Facts.Exists("event", "season_bargain_bought")) bargainsBought++;
            if (!bargainRefusedBefore && game.World.Facts.Exists("event", "season_bargain_refused")) bargainsRefused++;
            if (!bargainExpiredBefore && game.World.Facts.Exists("event", "season_bargain_expired")) bargainsExpired++;
            // The gate walked (D-140): each entry into the town is a market day.
            if (!inTownBefore && game.CurrentSite?.Kind == SiteKind.Town) marketsWalked++;
            // The school and the bond (D-141): a Smithing use grown inside the
            // walls is a forge sitting (the home bench stands in the valley),
            // and the bond flipping on is one swearing, counted world by world.
            int temperedNow = game.Player.AllGear.Count(item => item.TarnTempered);
            if (inTownBefore && game.Player.Skills.Uses(SkillId.Smithing) > smithUsesBefore
                && game.Player.TarnIron >= tarnIronBefore && temperedNow == temperedBefore) forgeSittings++;
            if (!swornBefore && game.GuildSworn) bondsSworn++;
            // The caravan leg's two ends (D-144): salt only ever rises at the
            // cart and only ever falls at the town counter.
            if (game.Player.Salt > saltBefore) saltBought += game.Player.Salt - saltBefore;
            if (game.Player.Salt < saltBefore && game.Cycle == cycleBefore) saltSold += saltBefore - game.Player.Salt;
            // Tarn-iron rises only at a seam and falls only into forge blooms.
            // A bloom now falls either into one tempered piece or onto the
            // guild scale (D-154), distinguished by the carried gear's mark.
            if (game.Player.TarnIron > tarnIronBefore) tarnIronMined += game.Player.TarnIron - tarnIronBefore;
            if (game.Player.TarnIron < tarnIronBefore) tarnIronSmelted += tarnIronBefore - game.Player.TarnIron;
            if (game.Player.IronBloom < ironBloomBefore)
            {
                int spent = ironBloomBefore - game.Player.IronBloom;
                if (temperedNow > temperedBefore)
                {
                    ironBloomsTempered += spent;
                    ironItemsTempered += temperedNow - temperedBefore;
                }
                else
                {
                    ironBloomsSold += spent;
                    coinFromIron += Math.Max(0, game.Player.Coin - coinBefore);
                }
            }
            if (game.Player.TarnTrout > fishBefore)
                fishCaught += game.Player.TarnTrout - fishBefore;
            else if (game.Player.TarnTrout < fishBefore && fishCookAction)
            {
                fishCooked += fishBefore - game.Player.TarnTrout;
                fishRations += fishMadeBefore;
            }
            else if (game.Player.TarnTrout < fishBefore && fishSaleAction)
            {
                fishSold += fishBefore - game.Player.TarnTrout;
                coinFromFish += Math.Max(0, game.Player.Coin - coinBefore);
            }
            // The stead's regard, at its high-water mark (D-076): it resets at each
            // crossing, so the peak is the warmest one stead ever came to hold the bearer.
            maxRegard = Math.Max(maxRegard, game.Regard);
            // The raiders' wrath, same high-water treatment (D-078).
            maxWrath = Math.Max(maxWrath, game.Wrath);
            // Raids land on a tick and reset each crossing (D-079): count rises.
            if (game.Raids > prevRaids) raidsSuffered += game.Raids - prevRaids;
            prevRaids = game.Raids;

            if (game.Player.Deaths > prevDeaths)
            {
                int d = game.Player.Deaths - prevDeaths;
                deathsThisWorld += d;
                prevDeaths = game.Player.Deaths;
                if (activeSite is not null)
                {
                    int sd = siteDeaths.GetValueOrDefault(activeSite) + d;
                    siteDeaths[activeSite] = sd;
                    if (sd > siteDeathBudget) skip.Add(activeSite);
                }
            }

            if (game.Cycle > cycleBefore)
            {
                // The far side carries the terms just sworn: game.World is now the new world,
                // so its Oaths are what the bot took up and its Burden is their summed weight.
                legendFromBurden += 10 * burdenLeftBehind;
                crossings.Add(new Crossing(
                    cycleBefore, worldBefore, twistBefore, game.Cycle, game.World.Name,
                    WorldTwistCatalog.IdOf(game.World.Twist),
                    game.Turn, deathsThisWorld, Arms(game), game.World.Oaths.ToList(), game.World.Burden,
                    sitesBefore, beforeReads, Bestiary(game, game.Cycle)));
                keysThisWorld = 0;
                deathsThisWorld = 0;
                skip.Clear();
                siteKeys.Clear();
                siteDeaths.Clear();
            }
        }

        if (json)
        {
            // The machine-readable report (D-083): the same facts the prose tells,
            // as one JSON object, so a sweep or CI consumes crossings as data.
            var report = new JourneyReport(
                Seed: seed,
                TargetCrossings: cycles,
                WitsDemo: wits,
                CycleReached: game.Cycle,
                Tier: game.World.Tier,
                CurrentTwist: WorldTwistCatalog.IdOf(game.World.Twist),
                CrossingsMade: crossings.Count,
                Stop: stop,
                KeysPressed: totalKeys,
                Turns: game.Turn,
                Deaths: game.Player.Deaths,
                Scars: string.Join(",", game.Player.Scars.Select(s => DeathsToll.NameOf(s))),
                RemnantsReclaimed: remnantsReclaimed,
                CoinReclaimed: coinReclaimed,
                EssenceReclaimed: essenceReclaimed,
                ChestsLooted: chestsLooted,
                ChestCoin: chestCoin,
                GearTaken: gearTaken,
                KnacksTaken: knacksTaken,
                Knacks: KnackList(game),
                ArcReach: ArcReach(game),
                ResolvedAs: resolvedAs.ToString().ToLowerInvariant(),
                ResolvedCycle: resolvedCycle,
                LaidCycle: laidCycle,
                MendedCycle: mendedCycle,
                Legend: game.Player.Legend,
                LegendFromBurden: legendFromBurden,
                HidesTaken: hidesTaken,
                HidesSold: hidesSold,
                CoinFromHides: coinFromHides,
                MeatCooked: meatCooked,
                RationsCooked: rationsCooked,
                HerbsForaged: herbsForaged,
                HerbsSold: herbsSold,
                CoinFromHerbs: coinFromHerbs,
                DraughtsDrawn: draughtsDrawn,
                DraughtsDrunk: draughtsDrunk,
                WordsLearned: wordsLearned,
                WardsSaid: wardsSaid,
                ShadesCalled: shadesCalled,
                MulesBought: mulesBought,
                CoursersTaken: coursersTaken,
                CoinBanked: coinBanked,
                RoadsTaken: roadsTaken,
                NightsCamped: nightsCamped,
                FellsTaken: fellsTaken,
                MarketsWalked: marketsWalked,
                LotsSoldInTown: game.Player.Skills.Uses(SkillId.Commerce),
                ForgeSittings: forgeSittings,
                BondsSworn: bondsSworn,
                SaltBought: saltBought,
                SaltSold: saltSold,
                TarnIronMined: tarnIronMined,
                TarnIronSmelted: tarnIronSmelted,
                IronBloomsTempered: ironBloomsTempered,
                IronItemsTempered: ironItemsTempered,
                IronBloomsSold: ironBloomsSold,
                CoinFromIron: coinFromIron,
                FishCaught: fishCaught,
                FishCooked: fishCooked,
                FishRations: fishRations,
                FishSold: fishSold,
                CoinFromFish: coinFromFish,
                MaxRegard: maxRegard,
                RegardTitle: SteadRegard.TitleOf(maxRegard),
                MaxWrath: maxWrath,
                WrathTitle: RaiderWrath.TitleOf(maxWrath),
                RaidsSuffered: raidsSuffered,
                WeatherTicks: weatherTicks,
                ExposedCamps: exposedCamps,
                ForecastDeferrals: forecastDeferrals,
                ColdCampRefusals: coldCampRefusals,
                HayingDays: hayingDays,
                LateFrosts: lateFrosts,
                GranaryPreventions: granaryPreventions,
                BargainsOffered: bargainsOffered,
                BargainsBought: bargainsBought,
                BargainsRefused: bargainsRefused,
                BargainsExpired: bargainsExpired,
                PacingNights: game.Teller.Readings.Count,
                PacingSpaceCalls: game.Teller.SpaceCalls,
                PacingPressCalls: game.Teller.PressCalls,
                PacingDealtUnderSpace: game.Teller.DealtUnderSpace,
                PacingQuietUnderPress: game.Teller.QuietUnderPress,
                Keys: emitKeys ? keys.ToString() : null,
                Crossings: crossings.Select(c => new JourneyCrossingDto(
                    FromCycle: c.FromCycle,
                    FromWorld: c.FromWorld,
                    FromTwist: c.FromTwist,
                    ToCycle: c.ToCycle,
                    ToWorld: c.ToWorld,
                    ToTwist: c.ToTwist,
                    Turn: c.Turn,
                    DeathsInWorld: c.DeathsInWorld,
                    Arms: c.Arms,
                    Sworn: c.Sworn.Select(o => OathCatalog.Def(o).Name).ToList(),
                    Burden: c.Burden,
                    Sites: c.Sites.Select(s => new JourneySiteDto(s.Name, s.Cleared, s.Skipped)).ToList(),
                    BestiaryBefore: c.Before.Select(r => new JourneyReadDto(
                        r.Kind.ToString().ToLowerInvariant(), r.Bank, r.Tier.ToString())).ToList(),
                    BestiaryAfter: c.After.Select(r => new JourneyReadDto(
                        r.Kind.ToString().ToLowerInvariant(), r.Bank, r.Tier.ToString())).ToList())).ToList());
            Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report, PilotJsonPretty.Default.JourneyReport));
            return 0;
        }

        Report(seed, cycles, wits, crossings, stop, game, totalKeys, keys, emitKeys,
            remnantsReclaimed, coinReclaimed, essenceReclaimed,
            chestsLooted, chestCoin, gearTaken, knacksTaken,
            resolvedAs, resolvedCycle, laidCycle, mendedCycle, legendFromBurden,
            hidesTaken, hidesSold, coinFromHides, meatCooked, rationsCooked,
            herbsForaged, herbsSold, coinFromHerbs, draughtsDrawn, draughtsDrunk,
            wordsLearned, wardsSaid, shadesCalled,
            mulesBought, coursersTaken, coinBanked,
            roadsTaken, nightsCamped, fellsTaken,
            marketsWalked, game.Player.Skills.Uses(SkillId.Commerce), game.Player.Skills.Level(SkillId.Commerce),
            forgeSittings, bondsSworn, game.Player.Skills.Level(SkillId.Smithing),
            saltBought, saltSold, tarnIronMined, tarnIronSmelted,
            ironBloomsTempered, ironItemsTempered, ironBloomsSold, coinFromIron,
            fishCaught, fishCooked, fishRations, fishSold, coinFromFish,
            maxRegard, maxWrath, raidsSuffered,
            weatherTicks, exposedCamps, forecastDeferrals, coldCampRefusals,
            hayingDays, lateFrosts, granaryPreventions,
            bargainsOffered, bargainsBought, bargainsRefused, bargainsExpired,
            game.Teller);
        return 0;
    }

    /// <summary>A beast of the kind answers to the bearer, at the side or in the stable (D-100).</summary>
    private static bool OwnsBeast(Game game, MountKind kind) =>
        game.Mount?.Kind == kind || game.Stable.Any(b => b.Kind == kind);

    /// <summary>Every coin riding in saddlebags anywhere in the roster (D-100).</summary>
    private static int TotalBags(Game game) =>
        (game.Mount?.Bags ?? 0) + game.Stable.Sum(b => b.Bags);

    /// <summary>The bearer's read of every known kind at a given tier: the bank, and what it reads to here.</summary>
    private static List<Read> Bestiary(Game game, int tier) =>
        game.Player.Reads.Keys
            .OrderBy(k => (int)k)
            .Select(k => new Read(k, game.Player.Reads[k], game.Player.ReadOf(k, tier)))
            .ToList();

    /// <summary>Every tenanted site in the current world, and how the bot left it.</summary>
    private static List<SiteOutcome> SiteStates(Game game, IReadOnlySet<string> skip) =>
        game.World.Sites
            .Where(s => s.Spawns.Count > 0)
            .OrderBy(s => (int)s.Kind)
            .Select(s => new SiteOutcome(ShortSite(s.Kind), s.Cleared, skip.Contains(s.Id)))
            .ToList();

    private static string ShortSite(SiteKind kind) =>
        kind == SiteKind.GoblinCamp ? "camp" : kind.ToString().ToLowerInvariant();

    /// <summary>The bearer's iron and the attributes that drive it: what the coin from the dark bought (D-064).</summary>
    private static string Arms(Game game)
    {
        var p = game.Player;
        string weapon = p.Weapon?.Name ?? "bare fists";
        string armor = p.Armor?.Name ?? "no mail";
        string bow = p.Bow?.Name ?? "no bow";
        var a = p.Attributes;
        return $"{weapon}, {armor}, {bow}  (Vig {a[Attr.Vigor]}, Might {a[Attr.Might]}, Grace {a[Attr.Grace]})";
    }

    /// <summary>The knacks the bearer chose, in the ledger's order (the level-2 wave, then the level-4).</summary>
    private static string KnackList(Game game) =>
        string.Join(", ", PerkCatalog.Choices
            .SelectMany(c => c.Options)
            .Where(o => game.Player.HasPerk(o.Id))
            .Select(o => o.Name));

    /// <summary>The furthest rung of the arc's ladder the bearer has reached, as a terse label (D-068).</summary>
    private static string ArcReach(Game game)
    {
        var p = game.Player;
        if (p.SeveredRestored > 0) return "the mending";
        if (p.SeveredUnbound > 0 && p.Resolution != Resolution.None) return "the laying-down";
        if (p.Resolution != Resolution.None) return "the keeping";
        if (p.CommissionHeard) return "the commission";
        if (p.UnbinderRevealTier >= 2) return "reveal tier 2";
        if (p.UnbinderRevealTier >= 1) return "reveal tier 1";
        if (p.LedgerHeard) return "the ledger";
        if (p.VisionSeen) return "the vision";
        if (p.CrossingGuiltHeard) return "the guilt";
        if (p.SeveredTruthHeard) return "the truth";
        return "nothing yet";
    }

    private static string Where(Game game) =>
        game.Mode == MapMode.Site
            ? $"underground with {game.LiveMonstersHere.Count()} foe(s) standing"
            : $"on the overworld at ({game.Player.Pos.X},{game.Player.Pos.Y})";

    private static void Report(
        ulong seed, int cycles, bool wits, List<Crossing> crossings, string stop,
        Game game, int totalKeys, StringBuilder keys, bool emitKeys,
        int remnantsReclaimed, int coinReclaimed, int essenceReclaimed,
        int chestsLooted, int chestCoin, int gearTaken, int knacksTaken,
        Resolution resolvedAs, int resolvedCycle, int laidCycle, int mendedCycle,
        int legendFromBurden, int hidesTaken, int hidesSold, int coinFromHides,
        int meatCooked, int rationsCooked, int herbsForaged, int herbsSold, int coinFromHerbs,
        int draughtsDrawn, int draughtsDrunk,
        int wordsLearned, int wardsSaid, int shadesCalled,
        int mulesBought, int coursersTaken, int coinBanked,
        int roadsTaken, int nightsCamped, int fellsTaken,
        int marketsWalked, int lotsSold, int commerceLevel,
        int forgeSittings, int bondsSworn, int smithingLevel,
        int saltBought, int saltSold,
        int tarnIronMined, int tarnIronSmelted, int ironBloomsTempered, int ironItemsTempered,
        int ironBloomsSold, int coinFromIron,
        int fishCaught, int fishCooked, int fishRations, int fishSold, int coinFromFish,
        int maxRegard, int maxWrath, int raidsSuffered,
        Dictionary<string, int> weatherTicks, Dictionary<string, int> exposedCamps,
        int forecastDeferrals, int coldCampRefusals,
        int hayingDays, int lateFrosts, int granaryPreventions,
        int bargainsOffered, int bargainsBought, int bargainsRefused, int bargainsExpired,
        Storyteller teller)
    {
        var w = Console.Out;
        w.WriteLine($"AEGIS JOURNEY   seed {seed}   target {cycles} crossing(s)"
                    + (wits ? "   [--wits: the keen-eyed walk (D-084)]" : ""));
        w.WriteLine(new string('=', 62));

        if (crossings.Count == 0)
            w.WriteLine("  no crossing was made.");

        foreach (var c in crossings)
        {
            w.WriteLine();
            w.WriteLine($"crossing {crossings.IndexOf(c) + 1}: cycle {c.FromCycle} \"{c.FromWorld}\" (tier {c.FromCycle}) "
                        + $"-> cycle {c.ToCycle} \"{c.ToWorld}\" (tier {c.ToCycle})   [turn {c.Turn}]");
            if (c.FromTwist != "none" || c.ToTwist != "none")
                w.WriteLine($"  world laws: {c.FromTwist} -> {c.ToTwist}");

            string cleared = string.Join(", ", c.Sites.Where(s => s.Cleared).Select(s => s.Name));
            string standing = string.Join(", ", c.Sites.Where(s => !s.Cleared).Select(s => s.Name));
            w.WriteLine($"  sites cleared: {(cleared.Length == 0 ? "none" : cleared)}"
                        + (standing.Length == 0 ? "" : $"; left standing: {standing}"));
            w.WriteLine($"  {c.DeathsInWorld} death(s) in that world.");
            w.WriteLine($"  arms: {c.Arms}");
            if (c.Sworn.Count > 0)
                w.WriteLine($"  terms taken up into \"{c.ToWorld}\": "
                            + $"{string.Join(", ", c.Sworn.Select(o => OathCatalog.Def(o).Name))} "
                            + $"(burden {c.Burden}, honored in Legend at its far arch) (D-069).");

            if (c.Before.Count == 0)
            {
                w.WriteLine("  bestiary: empty (nothing was read in that world).");
                continue;
            }
            w.WriteLine("  bestiary across the arch (bank carries whole; read may soften):");
            var afterByKind = c.After.ToDictionary(r => r.Kind);
            foreach (var b in c.Before)
            {
                var a = afterByKind.TryGetValue(b.Kind, out var aa) ? aa : b;
                string note = a.Tier < b.Tier ? "softened"
                            : a.Tier > b.Tier ? "sharpened"
                            : "held";
                string bankNote = a.Bank == b.Bank ? $"bank {b.Bank} (unchanged)" : $"bank {b.Bank}->{a.Bank}";
                w.WriteLine($"    {b.Kind.ToString().ToLowerInvariant(),-8} {bankNote,-20} {b.Tier,-4} -> {a.Tier,-4}  ({note})");
            }
        }

        w.WriteLine();
        w.WriteLine(new string('-', 62));
        w.WriteLine($"OUTCOME: reached cycle {game.Cycle} (tier {game.World.Tier}), {crossings.Count} crossing(s) made.");
        w.WriteLine($"         {stop}.");
        w.WriteLine($"         {totalKeys} keys pressed, {game.Turn} turns, {game.Player.Deaths} death(s) total.");
        if (game.Player.Scars.Count > 0)
            w.WriteLine($"         the toll kept: {string.Join(", ", game.Player.Scars.Select(DeathsToll.NameOf))} (D-098).");
        w.WriteLine($"         reclaimed {remnantsReclaimed} remnant(s) from where it fell: "
                    + $"{coinReclaimed} coin, {essenceReclaimed} essence kept back from the dark (D-065).");
        w.WriteLine($"         looted {chestsLooted} chest(s) from the sites it cleared: "
                    + $"{chestCoin} coin, {gearTaken} piece(s) of deep iron taken up and worn (D-066).");
        w.WriteLine($"         answered {knacksTaken} threshold question(s) as they opened (D-067)"
                    + (knacksTaken == 0 ? "." : $": {KnackList(game)}."));
        w.WriteLine($"         the arc: climbed the reveal ladder to {ArcReach(game)} (D-068).");
        if (resolvedCycle > 0)
            w.WriteLine($"           answered the keeping ({resolvedAs.ToString().ToLowerInvariant()}) at the Hearth in cycle {resolvedCycle}.");
        if (laidCycle > 0)
            w.WriteLine($"           laid a keeper down gently in cycle {laidCycle} (the mercy road walked first).");
        if (mendedCycle > 0)
            w.WriteLine($"           mended a keeper in cycle {mendedCycle}: D-060's restore path driven live, end to end.");
        else if (resolvedCycle > 0)
            w.WriteLine($"           (no mending this run: {cycles} crossing(s) reached no second post-resolution hollow.)");
        w.WriteLine($"         the hunt: took {hidesTaken} hide(s) off the wilds (D-070)"
                    + (hidesTaken == 0 ? " (no game bagged this run)." : ", plus raw meat for the fire."));
        if (hidesSold > 0)
            w.WriteLine($"         the trade: sold {hidesSold} hide(s) at the wood's edge for {coinFromHides} coin (D-072).");
        if (meatCooked > 0)
            w.WriteLine($"         the fire: cooked {meatCooked} cut(s) of raw meat into {rationsCooked} ration(s) (D-073).");
        if (herbsForaged > 0)
            w.WriteLine($"         the forage: picked {herbsForaged} sprig(s) of herb; sold {herbsSold} for {coinFromHerbs} coin at the stillroom's price (D-074/D-075, D-082).");
        if (draughtsDrawn > 0)
            w.WriteLine($"         the steeping: drew {draughtsDrawn} hale-draught(s) from the satchel's own sprigs; drank {draughtsDrunk} where the road hurt (D-090).");
        if (wordsLearned > 0)
            w.WriteLine($"         the words: took {wordsLearned} graven word(s) off the stones; said the ward {wardsSaid} time(s); the shade answered the calling {shadesCalled} time(s) (D-091, D-099).");
        if (mulesBought + coursersTaken > 0)
            w.WriteLine($"         the roads: bought the stead's mule {mulesBought} time(s); the raiders' courser answered the deed {coursersTaken} time(s); the saddlebags carried {coinBanked} coin all told (D-100).");
        if (roadsTaken > 0)
            w.WriteLine($"         the east road: took the mouth {roadsTaken} time(s) and camped {nightsCamped} night(s) on the way (D-138): travel as play, the D-006 box opened.");
        if (fellsTaken > 0)
            w.WriteLine($"         the frontier: climbed the drovers' track {fellsTaken} time(s) onto the high fells and hunted the pack's ground, the hides carried down to the town's counter (D-146).");
        if (marketsWalked > 0)
            w.WriteLine($"         the market: walked {marketsWalked} town gate(s) and sold {lotsSold} lot(s) at town prices; Commerce stands at level {commerceLevel} (D-140).");
        if (forgeSittings + bondsSworn > 0)
            w.WriteLine($"         the town school: {forgeSittings} sitting(s) at the forge under the smith's eye (Smithing stands at level {smithingLevel}); the carriers' bond sworn {bondsSworn} time(s) across the worlds (D-141).");
        if (saltBought + saltSold > 0)
            w.WriteLine($"         the caravan leg: loaded {saltBought} sack(s) of salt at the cart and sold {saltSold} at the town counter, the margin earned by the walk (D-144).");
        if (tarnIronMined + tarnIronSmelted + ironBloomsSold > 0)
            w.WriteLine($"         the fell iron: worked {tarnIronMined} raw piece(s), smelted {tarnIronSmelted}, tempered {ironItemsTempered} piece(s) with {ironBloomsTempered} bloom(s), and sold {ironBloomsSold} bloom(s) for {coinFromIron} coin through the carriers (D-153, D-154).");
        if (fishCaught + fishCooked + fishSold > 0)
            w.WriteLine($"         the black tarn: caught {fishCaught} tarn trout, cooked {fishCooked} into {fishRations} ration(s), and sold {fishSold} for {coinFromFish} coin at the town counter (D-156).");
        w.WriteLine($"         the weather: regional tick counts {string.Join(", ", weatherTicks.Select(kv => $"{kv.Key} {kv.Value}"))} (D-158).");
        w.WriteLine($"           exposed camps by family: {string.Join(", ", exposedCamps.Select(kv => $"{kv.Key} {kv.Value}"))}; forecast deferrals {forecastDeferrals}; Cold refusals {coldCampRefusals}.");
        w.WriteLine($"           seasonal events: Haying days {hayingDays}, Late frost {lateFrosts}, granary prevention {granaryPreventions}; bargains {bargainsOffered} offered, {bargainsBought} bought, {bargainsRefused} refused, {bargainsExpired} expired.");
        w.WriteLine($"         the stead: came to hold the bearer as {(maxRegard > 0 ? SteadRegard.TitleOf(maxRegard) : "a stranger")} at its warmest "
                    + $"(peak regard {maxRegard}, reset at every crossing) (D-076).");
        w.WriteLine($"         the dens: came to hold the bearer as {(maxWrath > 0 ? RaiderWrath.TitleOf(maxWrath) : "no one at all")} at their most fearful "
                    + $"(peak wrath {maxWrath}, reset at every crossing) (D-078).");
        w.WriteLine($"         the raids: the steads suffered {raidsSuffered} raid(s) while camps stood, each thinning the lofts bread is priced by (D-079, D-089).");
        w.WriteLine($"         the teller's book (read-only): watched {teller.Readings.Count} tick night(s), "
                    + $"called for air {teller.SpaceCalls} time(s) and for the screw {teller.PressCalls}; "
                    + $"the season dealt through {teller.DealtUnderSpace} call(s) for air, and {teller.QuietUnderPress} pressed night(s) stayed quiet (D-145).");
        int sworn = crossings.Count(c => c.Sworn.Count > 0);
        if (sworn > 0)
        {
            var terms = crossings.First(c => c.Sworn.Count > 0);
            string names = string.Join(", ", terms.Sworn.Select(o => OathCatalog.Def(o).Name));
            w.WriteLine($"         swore terms at {sworn} of {crossings.Count} crossing(s) (D-069): "
                        + $"{names} (burden {terms.Burden}), the harder walking freely taken.");
            w.WriteLine($"           the burden honored: {legendFromBurden} of the bearer's {game.Player.Legend} "
                        + $"Legend was earned carrying it (10 per weight, paid at each sworn world's far arch).");
        }
        else
            w.WriteLine("         swore no terms this run (D-069): no crossing was made under oath.");
        w.WriteLine("         a seeded journey replays identically: the pilot reads only game state.");
        if (emitKeys)
        {
            w.WriteLine();
            w.WriteLine($"keys ({keys.Length}): {keys}");
        }
    }
}

// ---- the machine-readable report (D-083): the prose report's facts as data ----

internal sealed record JourneyReadDto(string Kind, int Bank, string Read);

internal sealed record JourneySiteDto(string Name, bool Cleared, bool Skipped);

internal sealed record JourneyCrossingDto(
    int FromCycle, string FromWorld, string FromTwist, int ToCycle, string ToWorld, string ToTwist, int Turn,
    int DeathsInWorld, string Arms, List<string> Sworn, int Burden,
    List<JourneySiteDto> Sites, List<JourneyReadDto> BestiaryBefore, List<JourneyReadDto> BestiaryAfter);

internal sealed record JourneyReport(
    ulong Seed, int TargetCrossings, bool WitsDemo, int CycleReached, int Tier, string CurrentTwist, int CrossingsMade, string Stop,
    int KeysPressed, int Turns, int Deaths, string Scars,
    int RemnantsReclaimed, int CoinReclaimed, int EssenceReclaimed,
    int ChestsLooted, int ChestCoin, int GearTaken,
    int KnacksTaken, string Knacks,
    string ArcReach, string ResolvedAs, int ResolvedCycle, int LaidCycle, int MendedCycle,
    int Legend, int LegendFromBurden,
    int HidesTaken, int HidesSold, int CoinFromHides,
    int MeatCooked, int RationsCooked,
    int HerbsForaged, int HerbsSold, int CoinFromHerbs,
    int DraughtsDrawn, int DraughtsDrunk,
    int WordsLearned, int WardsSaid, int ShadesCalled,
    int MulesBought, int CoursersTaken, int CoinBanked,
    int RoadsTaken, int NightsCamped, int FellsTaken,
    int MarketsWalked, int LotsSoldInTown,
    int ForgeSittings, int BondsSworn,
    int SaltBought, int SaltSold,
    int TarnIronMined, int TarnIronSmelted, int IronBloomsTempered, int IronItemsTempered,
    int IronBloomsSold, int CoinFromIron,
    int FishCaught, int FishCooked, int FishRations, int FishSold, int CoinFromFish,
    int MaxRegard, string RegardTitle, int MaxWrath, string WrathTitle, int RaidsSuffered,
    Dictionary<string, int> WeatherTicks, Dictionary<string, int> ExposedCamps,
    int ForecastDeferrals, int ColdCampRefusals,
    int HayingDays, int LateFrosts, int GranaryPreventions,
    int BargainsOffered, int BargainsBought, int BargainsRefused, int BargainsExpired,
    int PacingNights, int PacingSpaceCalls, int PacingPressCalls,
    int PacingDealtUnderSpace, int PacingQuietUnderPress,
    string? Keys,
    List<JourneyCrossingDto> Crossings);

namespace Aegis.Core;

public static class FenLife
{
    public const int PanTurns = 6;
    public const int HamletRationPrice = 4;
    public const int HamletBedPrice = 4;
    public const int RestockSacks = 2;
}

public sealed partial class Game
{
    public int FenVisits { get; private set; }
    public int FenCrossings { get; private set; }
    public int FenPanAttempts { get; private set; }
    public int FenPanRefusals { get; private set; }
    public int FenPansWorked { get; private set; }
    public int FenAdderKills { get; private set; }
    public int FenArcConclusions { get; private set; }
    public int FenRestocks { get; private set; }
    public string FenArcOutcome =>
        World.Facts.Find("fen-outcome", "measure_kept") is not null ? "measure_kept"
        : World.Facts.Find("fen-outcome", "road_opened") is not null ? "road_opened"
        : "";
    public bool FenRestockScheduled => _schedule.Any(f => f.Key == "fen_restock");
    public bool FenRestocked => World.Facts.Exists("delivery", "fen_restock");

    private bool TakeTheCauseway()
    {
        bool toFens = Area == Area.Road;
        var steed = Mount;
        bool steedComes = steed is not null && steed.Area == Area
            && steed.Pos.Chebyshev(Player.Pos) <= 2;
        Area = toFens ? Area.Fens : Area.Road;
        Player.Pos = toFens ? World.FenHomePos : World.FenMouthPos;
        if (steedComes)
            PlaceMountBeside(Player.Pos);
        else if (steed is not null && steed.Area != Area)
            Log.Add(Turn, $"{Cap(steed.Name)} is not at your side for the causeway mouth; it keeps its own ground until you return.", LogTone.Info);
        PlaceFellowsBeside(Player.Pos);
        FenCrossings++;
        if (toFens)
        {
            FenVisits++;
            Log.Add(Turn, $"You take the raised way into the {World.FenRegion.Name}. Firm bank and reed ground break the water, and every path worth trusting stands above it.", LogTone.Info);
            Log.Add(Turn, WeatherLine(ClimateBand.Fens), LogTone.Info);
            Log.Add(Turn, ForecastLine(ClimateBand.Fens), LogTone.Info);
        }
        else
            Log.Add(Turn, $"You leave the {World.FenRegion.Name} by the same raised mouth and take the {World.RoadRegion.Name}'s road again.", LogTone.Info);
        return true;
    }

    private void MarkFenHamletVisited()
    {
        if (World.Facts.Exists("fen-arc", "hamlet_seen")) return;
        World.Facts.Add("fen-arc", "hamlet_seen", World.FenHamletName,
            $"The bearer entered {World.FenHamletName}, heard the compact's pressure, and learned which four places hold its present account.");
        Log.Add(Turn, $"{World.FenHamletName} stands close around one dry lane. The compact's marks hang over the common roof, and every speaker points back toward the same four places.", LogTone.Info);
    }

    private List<(string Label, string Answer)> BuildFenTopics(Npc npc)
    {
        var topics = new List<(string, string)>
        {
            ("The Salt Fen", $"\"The {World.FenRegion.Name} is firm bank, reed ground, and raised way. Water and bog are not roads, however near they look.\""),
            ("The compact", $"\"The salters' compact keeps measures, labor, and freight in one account. {World.FenHamletName} is its roof, not its border.\""),
        };
        if (World.Facts.Exists("fen-arc", "ready"))
            topics.Add(("The open account", "\"You have brought every part of the account into one room. The keeper can close it in either honest measure.\""));
        else
            topics.Add(("The open account", "\"The pans, the reed-bank, the old watch, and the drowned house each hold one part. Nothing closes until all four are read.\""));
        return topics;
    }

    private List<(TradeGood, string, string)> BuildFenOffers(Npc npc)
    {
        var offers = new List<(TradeGood, string, string)>();
        if (npc.Id == "npc_compact_keeper")
        {
            offers.Add((TradeGood.FenRation, "", $"Buy fen bread ({FenLife.HamletRationPrice} coin)"));
            offers.Add((TradeGood.FenBed, "", $"Sleep under the compact's roof ({FenLife.HamletBedPrice} coin)"));
            if (FenArcReady && FenArcOutcome.Length == 0)
            {
                offers.Add((TradeGood.CompactMeasure, "", "Close the account by the kept measure"));
                offers.Add((TradeGood.CompactRoad, "", "Close the account by the opened road"));
            }
        }
        return offers;
    }

    private bool FenArcReady =>
        World.Facts.Exists("fen-arc", "hamlet_seen")
        && World.Facts.Exists("fen-work", "pan_worked")
        && World.Facts.Exists("fen-deed", "wilds_read")
        && World.Facts.Exists("fen-deed", "watch_stilled")
        && World.Facts.Exists("fen-deed", "vault_opened");

    private void RefreshFenArc()
    {
        if (!FenArcReady || World.Facts.Exists("fen-arc", "ready")) return;
        World.Facts.Add("fen-arc", "ready", World.FenHamletName,
            "The hamlet, pans, reed-bank, old watch, and drowned house have all entered the compact's present account; either bounded conclusion is now available.");
        Log.Add(Turn, $"The four parts of the compact's account now answer one another. Its keeper at {World.FenHamletName} can close the matter.", LogTone.Reward);
    }

    private bool WorkSaltPan(Site saltwork)
    {
        FenPanAttempts++;
        WeatherFamily weather = WeatherAt(ClimateBand.Fens);
        if (weather is WeatherFamily.Wet or WeatherFamily.Cold)
        {
            FenPanRefusals++;
            Log.Add(Turn, weather == WeatherFamily.Wet
                ? "Rain lies in the pan. The brine will not take the rake, and no time or labor is spent."
                : "Frost has locked the pan wrong. Working it now would spoil the measure, and no time or labor is spent.", LogTone.Info);
            return false;
        }

        Pos pan = Player.Pos;
        for (int i = 1; i < FenLife.PanTurns; i++) AdvanceTurn();
        saltwork.SaltPans.Remove(pan);
        saltwork.Map[pan] = Terrain.ExhaustedPan;
        Player.Salt++;
        FenPansWorked++;
        GainSkill(SkillId.Survival);
        World.Facts.Add("fen-work", $"pan_{pan.X}_{pan.Y}", World.FenHamletName,
            $"One salt pan at {pan.X},{pan.Y} was worked dry in six completed turns under {weather.ToString().ToLowerInvariant()} weather.");
        if (!World.Facts.Exists("fen-work", "pan_worked"))
            World.Facts.Add("fen-work", "pan_worked", World.FenHamletName,
                "At least one of the compact's three finite pans was worked and its sack carried.");
        if (saltwork.SaltPans.Count == 0)
            World.Facts.Add("fen-work", "all_pans_worked", World.FenHamletName,
                "All three of the compact's pans have given their one sack for this world.");
        Log.Add(Turn, $"Six patient turns bring the pan to grey crystal. One sack joins the pack. ({Player.Salt} carried, {saltwork.SaltPans.Count} pans unworked)", LogTone.Reward);
        RefreshFenArc();
        return true;
    }

    private void CompleteFenSite(Site site)
    {
        switch (site.Kind)
        {
            case SiteKind.FenWilds:
                World.Facts.Add("fen-deed", "wilds_read", World.FenHamletName,
                    "The adder bank was hunted through, and its signs and yield entered the compact's account.");
                Log.Add(Turn, "The reed-bank settles. What hunted here has paid in hide, meat, and a safer path.", LogTone.Reward);
                break;
            case SiteKind.FenWatch:
                World.Facts.Add("fen-deed", "watch_stilled", World.FenHamletName,
                    "The old bank-watch was stilled and its kept measure recovered for the compact's reckoning.");
                Log.Add(Turn, "The old bank-watch is still, and the causeway through it belongs to feet again.", LogTone.Reward);
                break;
            case SiteKind.FenVault:
                World.Facts.Add("fen-deed", "vault_opened", World.FenHamletName,
                    "The drowned counting-house was opened and its old account brought back under a living roof.");
                Log.Add(Turn, "The drowned house keeps no further answer. Its account can be carried back into daylight.", LogTone.Reward);
                break;
        }
        RefreshFenArc();
    }

    private void ResolveFenArc(string outcome)
    {
        if (!FenArcReady || FenArcOutcome.Length > 0) return;
        string id = outcome == "measure" ? "measure_kept" : "road_opened";
        World.Facts.Add("fen-outcome", id, World.FenHamletName,
            $"The compact's bounded account concluded as {id}; one sack was paid and one capped peddler restock was scheduled for the next coarse tick.");
        Player.Salt++;
        FenArcConclusions++;
        ScheduleFenRestock();
        Log.Add(Turn, $"The compact closes the account and sets one sack into your hands. A carrier is sent west with the next cart tally. ({Player.Salt} salt carried)", LogTone.Reward);
        _offers.Clear();
        _offers.AddRange(BuildOffers(TalkNpc!));
    }

    private void ScheduleFenRestock()
    {
        if (FenRestockScheduled || FenRestocked) return;
        _schedule.Add(new ScheduledFact
        {
            Key = "fen_restock",
            DueTick = (Turn - _worldStartTurn) / SteadRaids.TickTurns + 1,
            Fire = g => g.DeliverFenRestock(),
        });
    }

    private void DeliverFenRestock()
    {
        int cap = Peddling.SaltStock(World.Tier);
        int before = World.PeddlerSalt;
        World.PeddlerSalt = Math.Min(cap, World.PeddlerSalt + FenLife.RestockSacks);
        int delivered = World.PeddlerSalt - before;
        FenRestocks++;
        World.Facts.Add("delivery", "fen_restock", World.FenHamletName,
            $"The compact's one scheduled delivery restored {delivered} sack{(delivered == 1 ? "" : "s")} to the peddler's cart, capped at its original stock of {cap}.");
        Log.Add(Turn, $"A compact carrier reaches the westbound cart. {delivered} sack{(delivered == 1 ? "" : "s")} of salt find room on its boards, and the delivery is entered once.", LogTone.Reward);
    }

    private void TryFenRation()
    {
        if (Player.Rations >= RationCap)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"Your pack is full enough. Bread kept here is bread for the next wet walker.\"");
            return;
        }
        if (Player.Coin < FenLife.HamletRationPrice)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"{FenLife.HamletRationPrice} coin the loaf, and you hold {Player.Coin}.\"");
            return;
        }
        Player.Coin -= FenLife.HamletRationPrice;
        Player.Rations++;
        Log.Add(Turn, $"Dense fen bread wrapped against spray. ({Player.Rations} carried, {Player.Coin} coin remains)", LogTone.Reward);
    }

    private void TryFenBed()
    {
        if (Player.Coin < FenLife.HamletBedPrice)
        {
            Log.Add(Turn, $"{TalkNpc!.Name}: \"{FenLife.HamletBedPrice} coin for dry floor, blanket, and breakfast. You hold {Player.Coin}.\"");
            return;
        }
        Player.Coin -= FenLife.HamletBedPrice;
        Player.Hp = Player.EffectiveMaxHp;
        Player.Stamina = Player.MaxStamina;
        Player.Focus = Player.MaxFocus;
        TendIronAtRest();
        SteepAtRest("Under the compact's roof");
        foreach (var friend in Fellows) friend.Hp = friend.MaxHp;
        Log.Add(Turn, "Reed-thatch, a raised floor, and shutters tight against the water: you wake whole under the compact's roof.", LogTone.Reward);
    }

    private void ActFenAdder(Monster monster)
    {
        int dist = monster.Pos.Chebyshev(Player.Pos);
        if (dist == 1)
        {
            if (_combatRng.Chance(Player.DodgeChance))
                Log.Add(Turn, "The fen adder's bite closes on air and reed-shadow.", LogTone.Combat);
            else
            {
                int damage = Absorb(_combatRng.Range(2, 5));
                Player.Hp -= damage;
                Log.Add(Turn, $"The fen adder bites and tears away for {damage}.", LogTone.Combat);
            }
            return;
        }

        int dx = Player.Pos.X - monster.Pos.X;
        int dy = Player.Pos.Y - monster.Pos.Y;
        bool straightTwo = (Math.Abs(dx) == 2 && dy == 0) || (Math.Abs(dy) == 2 && dx == 0);
        if (straightTwo && _combatRng.Chance(0.6))
        {
            var middle = monster.Pos.Plus(Math.Sign(dx), Math.Sign(dy));
            monster.Intent = new Intent
            {
                Kind = IntentKind.CoilStrike,
                TargetCell = Player.Pos,
                Footprint = [middle, Player.Pos],
            };
            Log.Add(Turn, "The fen adder locks into a straight coil, two lengths of bank marked ahead of its head!", LogTone.Danger);
            return;
        }

        if (dist <= 10) StepAdderToward(monster);
    }

    private void StepAdderToward(Monster monster)
    {
        var map = CurrentMap;
        var best = monster.Pos;
        int bestDist = monster.Pos.Manhattan(Player.Pos);
        foreach (var (dx, dy) in Directions.Cardinal)
        {
            var one = monster.Pos.Plus(dx, dy);
            if (map.Walkable(one) && one != Player.Pos && !FellowAt(one)
                && !Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == one))
            {
                int d = one.Manhattan(Player.Pos);
                if (d < bestDist) { best = one; bestDist = d; }
            }

            var two = one.Plus(dx, dy);
            if (!map.InBounds(one) || map[one] != Terrain.Water || !map.Walkable(two)
                || two == Player.Pos || FellowAt(two)
                || Monsters.Any(m => m.Alive && m != monster && m.SiteId == monster.SiteId && m.Pos == two))
                continue;
            int leapDistance = two.Manhattan(Player.Pos);
            if (leapDistance < bestDist) { best = two; bestDist = leapDistance; }
        }
        monster.Pos = best;
    }
}

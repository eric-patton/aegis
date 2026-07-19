namespace Aegis.Core;

/// <summary>
/// Renders game state into a <see cref="Frame"/>. Pure function of state; owns the
/// layout (header, map viewport, sidebar, message log). The map viewport flexes to
/// absorb any size beyond the 80x24 baseline; the sidebar keeps a fixed width on the
/// right and the log a fixed line count at the bottom. Below the baseline, layout is
/// computed at 80x24 and the frame simply crops.
/// </summary>
public static class Presenter
{
    public const int DefaultWidth = 80;
    public const int DefaultHeight = 24;

    private const int SidebarWidth = 23;
    private const int LogLines = 5;

    /// <summary>Computed per-render region geometry.</summary>
    private readonly record struct Layout(int MapX, int MapY, int MapW, int MapH, int SideX, int LogY)
    {
        public static Layout For(int width, int height)
        {
            int w = Math.Max(width, DefaultWidth);
            int h = Math.Max(height, DefaultHeight);
            int logY = h - LogLines;
            return new Layout(
                MapX: 0,
                MapY: 1,
                MapW: w - SidebarWidth - 2,
                MapH: logY - 2,
                SideX: w - SidebarWidth,
                LogY: logY);
        }
    }

    public static Frame Render(Game game) => Render(game, DefaultWidth, DefaultHeight);

    public static Frame Render(Game game, int width, int height)
    {
        var frame = new Frame(Math.Max(width, 1), Math.Max(height, 1));
        var layout = Layout.For(width, height);
        DrawHeader(frame, game);
        DrawMap(frame, game, layout);
        DrawSidebar(frame, game, layout);
        DrawLog(frame, game, layout);
        if (game.InShrineMenu) DrawShrineMenu(frame, game, layout);
        if (game.InTalkMenu) DrawTalkMenu(frame, game, layout);
        if (game.InUnbindMenu) DrawUnbindMenu(frame, game, layout);
        if (game.InTradeMenu) DrawTradeMenu(frame, game, layout);
        if (game.InThresholdMenu) DrawThresholdMenu(frame, layout);
        if (game.InLayingMenu) DrawLayingMenu(frame, game, layout);
        if (game.InGearMenu) DrawGearMenu(frame, game, layout);
        if (game.InCastMenu) DrawCastMenu(frame, game, layout);
        if (game.InSheetMenu) DrawSheet(frame, game, layout);
        if (game.InCrossingMenu) DrawCrossingMenu(frame, game, layout);
        if (game.InCreation) DrawCreation(frame, game, layout);
        return frame;
    }

    /// <summary>
    /// The asking (D-092): the first-wake creation scene, one question at a
    /// time in the standing dialog grammar. Options are a bright name row over
    /// a dim blurb row; the folk rows carry their gift on the name line.
    /// </summary>
    private static void DrawCreation(Frame frame, Game game, Layout layout)
    {
        var p = game.Player;
        const int boxW = 70;
        string title;
        var rows = new List<(string Text, Hue Hue)>();
        string hint;

        switch (game.CreationStage)
        {
            case CreationStage.Folk:
                title = "\"What folk bore you?\"";
                for (int i = 0; i < CreationCatalog.Folk.Count; i++)
                {
                    var def = CreationCatalog.Folk[i];
                    rows.Add(($"{i + 1}) {def.Name,-13} {def.Trait}", Hue.White));
                    rows.Add(($"   {def.Blurb}", Hue.DarkGray));
                }
                rows.Add(("0) Let the shrine decide: the whole of you, rolled at once", Hue.Cyan));
                hint = "1-5 answer; 0 leaves it to fate";
                break;

            case CreationStage.Past:
                title = "\"And what were your hands, before?\"";
                for (int i = 0; i < CreationCatalog.Pasts.Count; i++)
                {
                    var def = CreationCatalog.Pasts[i];
                    rows.Add(($"{i + 1}) {def.Name,-16} ({SkillSet.NameOf(def.Skill)})", Hue.White));
                    rows.Add(($"   {def.Blurb}", Hue.DarkGray));
                }
                hint = "1-7 answer";
                break;

            case CreationStage.ShapeRaise:
            case CreationStage.ShapePay:
                title = game.CreationStage == CreationStage.ShapeRaise
                    ? $"\"The years lean on a body. Where did yours rise?\" ({game.ShapingsLeft} left)"
                    : $"\"And what paid for the {AttributeSet.NameOf(game.ShapeRaise!.Value)}?\"";
                for (int i = 0; i < AttributeSet.Count; i++)
                {
                    var attr = (Attr)i;
                    rows.Add(($"{i + 1}) {AttributeSet.NameOf(attr),-9} {p.Attributes[attr]}",
                        attr == game.ShapeRaise ? Hue.Cyan : Hue.White));
                }
                hint = game.CreationStage == CreationStage.ShapeRaise
                    ? "1-7 rise; 0 stands as you are"
                    : "1-7 pays the rise";
                break;

            case CreationStage.Thing:
                title = game.PickingSecondThing
                    ? "\"And the burden's price buys a second. What else came through?\""
                    : "\"One thing came through the dark with you. What is it?\"";
                for (int i = 0; i < CreationCatalog.Things.Count; i++)
                {
                    var def = CreationCatalog.Things[i];
                    bool held = p.Things.Contains(def.Id);
                    rows.Add(($"{i + 1}) {def.Name}{(held ? "  (carried)" : "")}", held ? Hue.DarkGray : Hue.White));
                    rows.Add(($"   {def.Blurb}", Hue.DarkGray));
                }
                hint = "1-5 answer";
                break;

            case CreationStage.Burden:
                title = "\"Will you carry more, for more? A weight buys a second thing.\"";
                for (int i = 0; i < CreationCatalog.Burdens.Count; i++)
                {
                    var def = CreationCatalog.Burdens[i];
                    rows.Add(($"{i + 1}) {def.Name,-15} {def.Price}", Hue.White));
                    rows.Add(($"   {def.Blurb}", Hue.DarkGray));
                }
                rows.Add(("0) Carry nothing more", Hue.Cyan));
                hint = "1-3 take a burden; 0 declines";
                break;

            case CreationStage.Vow:
                title = "\"And what do you walk FOR, bearer? Vows are counted too.\"";
                for (int i = 0; i < CreationCatalog.Vows.Count; i++)
                {
                    var def = CreationCatalog.Vows[i];
                    rows.Add(($"{i + 1}) {def.Name}", Hue.White));
                    rows.Add(($"   {def.Blurb}", Hue.DarkGray));
                }
                rows.Add(("0) No vow but the road", Hue.Cyan));
                hint = "1-3 swear; 0 walks unsworn";
                break;

            case CreationStage.Face:
                title = "\"Is there a face you carry, from before the catching?\"";
                rows.Add(($"> {game.NameEntry}_", Hue.White));
                hint = "letters name them; - takes one back; . seals it; empty carries no one";
                break;

            default:
                title = "\"Last: what are you called?\"";
                rows.Add(($"> {game.NameEntry}_", Hue.White));
                hint = "letters shape it; - takes one back; . seals it; empty takes the folk's own";
                break;
        }

        int boxH = 6 + rows.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);
        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, "The asking", Hue.Cyan);
        frame.Write(x0 + 2, y0 + 2, title, Hue.Gray);
        for (int i = 0; i < rows.Count; i++)
            frame.Write(x0 + 2, y0 + 4 + i, rows[i].Text, rows[i].Hue);
        frame.Write(x0 + 2, y0 + boxH - 2, hint, Hue.DarkGray);
    }

    /// <summary>
    /// The terms of the crossing (D-047): the oath list with what stands sworn,
    /// the burden it sums to, and the two ways out (cross, or step back).
    /// </summary>
    private static void DrawCrossingMenu(Frame frame, Game game, Layout layout)
    {
        var oaths = OathCatalog.All;
        const int boxW = 52;
        int boxH = 7 + oaths.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, "The terms of the crossing", Hue.Cyan);

        for (int i = 0; i < oaths.Count; i++)
        {
            bool sworn = game.ChosenOaths.Contains(oaths[i].Id);
            frame.Write(x0 + 2, y0 + 3 + i,
                $"{i + 1}) {(sworn ? 'x' : '-')} {oaths[i].Name}: {oaths[i].Blurb}",
                sworn ? Hue.White : Hue.Gray);
        }

        int burden = oaths.Where(o => game.ChosenOaths.Contains(o.Id)).Sum(o => o.Weight);
        frame.Write(x0 + 2, y0 + 4 + oaths.Count, burden > 0
            ? $"The burden you take up: {burden}. Legend honors it."
            : "No terms taken up. The crossing is plain.", Hue.Yellow);
        frame.Write(x0 + 2, y0 + boxH - 2,
            $"1-{oaths.Count} swear or unswear; > crosses; else steps back", Hue.DarkGray);
    }

    private static void DrawTalkMenu(Frame frame, Game game, Layout layout)
    {
        var npc = game.TalkNpc;
        if (npc is null) return;

        bool unbinder = npc.Kind == NpcKind.Unbinder;
        int entries = game.Topics.Count + (unbinder ? 1 : game.Offers.Count);
        const int boxW = 46;
        int boxH = 5 + entries;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, npc.Kind switch
        {
            NpcKind.Unbinder => $"{npc.Name}, a wandering {npc.Role}",
            NpcKind.Severed => $"{npc.Name}, {npc.Role} of no stead at all",
            _ => $"{npc.Name}, {npc.Role} of {game.World.SettlementName}",
        }, Hue.White);

        for (int i = 0; i < game.Topics.Count; i++)
            frame.Write(x0 + 2, y0 + 3 + i, $"{i + 1}) Ask about {game.Topics[i].Label}", Hue.Gray);
        if (unbinder)
            frame.Write(x0 + 2, y0 + 3 + game.Topics.Count,
                $"{entries}) The unbinding ({game.UnbindingsLeft} left this world)", Hue.Cyan);
        for (int i = 0; i < game.Offers.Count; i++)
            frame.Write(x0 + 2, y0 + 3 + game.Topics.Count + i,
                $"{game.Topics.Count + i + 1}) {game.Offers[i].Label}", Hue.Yellow);

        frame.Write(x0 + 2, y0 + boxH - 2, $"1-{entries} choose; any other key to part ways", Hue.DarkGray);
    }

    private static void DrawUnbindMenu(Frame frame, Game game, Layout layout)
    {
        var npc = game.TalkNpc;
        if (npc is null) return;

        const int boxW = 46;
        int boxH = 6 + AttributeSet.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, $"The unbinding | {npc.Name} the {npc.Role}", Hue.White);
        frame.Write(x0 + 2, y0 + 2, game.Player.Attributes.TotalRaises > 0
            ? $"Loosening returns {game.UnbindRefund} essence   {game.UnbindingsLeft} left"
            : "Nothing to loosen: you are as you began", Hue.Cyan);

        for (int i = 0; i < AttributeSet.Count; i++)
        {
            var attr = (Attr)i;
            bool bound = game.Player.Attributes[attr] > AttributeSet.Baseline;
            frame.Write(x0 + 2, y0 + 4 + i,
                $"{i + 1}) {AttributeSet.NameOf(attr),-9} {game.Player.Attributes[attr],2}",
                bound ? Hue.White : Hue.DarkGray);
        }

        frame.Write(x0 + 2, y0 + boxH - 2, "1-7 loosen; any other key to part ways", Hue.DarkGray);
    }

    /// <summary>
    /// A vendor's bench (D-071): the trade menu behind one talk digit, its own nine
    /// slots for what the shared topics have no room to hold.
    /// </summary>
    private static void DrawTradeMenu(Frame frame, Game game, Layout layout)
    {
        var npc = game.TalkNpc;
        if (npc is null) return;

        const int boxW = 46;
        int boxH = 5 + game.TradeOffers.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, $"The wood's edge | {npc.Name} the {npc.Role}", Hue.White);
        frame.Write(x0 + 2, y0 + 2, $"You hold {game.Player.Coin} coin", Hue.Cyan);

        for (int i = 0; i < game.TradeOffers.Count; i++)
            frame.Write(x0 + 2, y0 + 4 + i,
                $"{i + 1}) {game.TradeOffers[i].Label}", Hue.Yellow);

        frame.Write(x0 + 2, y0 + boxH - 2,
            $"1-{game.TradeOffers.Count} choose; any other key to step back", Hue.DarkGray);
    }

    /// <summary>
    /// The keeping (D-039): two answers, one room. The menu names the choice
    /// plainly and hurries no one; the guardrail lives in the handler, not here.
    /// </summary>
    private static void DrawThresholdMenu(Frame frame, Layout layout)
    {
        const int boxW = 46;
        const int boxH = 9;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, "The Keeping", Hue.White);
        frame.Write(x0 + 2, y0 + 2, "The Hearth burns alone.", Hue.Cyan);
        frame.Write(x0 + 2, y0 + 4, "1) Take up the keeping", Hue.White);
        frame.Write(x0 + 2, y0 + 5, "2) Lay the commission down and walk on", Hue.White);
        frame.Write(x0 + 2, y0 + boxH - 2, "1-2 choose; any other key to step back", Hue.DarkGray);
    }

    /// <summary>
    /// The laying-down (D-045): the post-resolution choice, at arm's length. A
    /// storied, merciful bearer is also offered the rarest answer (D-060): the
    /// mending. The extra option is render-only, so no old journal's log shifts.
    /// </summary>
    private static void DrawLayingMenu(Frame frame, Game game, Layout layout)
    {
        bool mend = game.CanRestoreSevered;
        const int boxW = 46;
        int boxH = mend ? 10 : 9;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, "The Severed One", Hue.White);
        frame.Write(x0 + 2, y0 + 2, mend ? "It waits. The count is yours to weigh, or to carry." : "It waits. The count is yours to weigh.", Hue.Cyan);
        frame.Write(x0 + 2, y0 + 4, "1) The old way", Hue.White);
        frame.Write(x0 + 2, y0 + 5, "2) Lay it down gently", Hue.White);
        if (mend)
            frame.Write(x0 + 2, y0 + 6, "3) Mend it, and set it in the songs", Hue.White);
        frame.Write(x0 + 2, y0 + boxH - 2, mend ? "1-3 choose; any other key to step back" : "1-2 choose; any other key to step back", Hue.DarkGray);
    }

    /// <summary>
    /// The workings (D-091): what words are carried, what each asks of the
    /// pool, and which are wound up rather than said outright. Reached on 'z',
    /// turn-free like every menu.
    /// </summary>
    private static void DrawCastMenu(Frame frame, Game game, Layout layout)
    {
        var spells = game.Player.Spells;
        const int boxW = 46;
        int boxH = 5 + spells.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, "The workings you carry", Hue.White);
        frame.Write(x0 + 2, y0 + 2, $"Focus {game.Player.Focus}/{game.Player.MaxFocus}", Hue.Cyan);

        for (int i = 0; i < spells.Count; i++)
        {
            var def = SpellCatalog.Def(spells[i]);
            frame.Write(x0 + 2, y0 + 4 + i,
                $"{i + 1}) {def.Name,-14} {def.Focus} focus{(def.WindUp ? "  (wind-up)" : "")}",
                game.Player.Focus >= def.Focus ? Hue.White : Hue.DarkGray);
        }

        frame.Write(x0 + 2, y0 + boxH - 2,
            $"1-{spells.Count} speak; any other key keeps silence", Hue.DarkGray);
    }

    /// <summary>
    /// The pack (D-041): what is owned, what is worn, what each piece asks and
    /// gives. Requirements print here before any coin or essence is invested.
    /// </summary>
    private static void DrawGearMenu(Frame frame, Game game, Layout layout)
    {
        var items = game.Player.AllGear.ToList();
        const int boxW = 46;
        int boxH = 5 + items.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, "Your gear", Hue.White);

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            bool held = item == game.Player.Weapon || item == game.Player.Armor || item == game.Player.Bow;
            bool under = !item.MeetsReq(game.Player.Attributes);
            string what = item.Slot switch
            {
                GearSlot.Weapon => $"arm +{item.EffectiveBonus(game.Player.Attributes)}",
                GearSlot.Ranged => $"looses +{item.EffectiveBonus(game.Player.Attributes)}",
                _ => $"wards {item.EffectiveBonus(game.Player.Attributes)}",
            };
            string state = item.Worn ? " WORN" : $" {item.Wear}/{item.MaxWear}";
            string asks = under ? $" {AttributeSet.NameOf(item.ReqAttr)} {item.Req}!" : "";
            frame.Write(x0 + 2, y0 + 3 + i,
                $"{i + 1}) {item.Name,-16}{(held ? '*' : ' ')} {what}{state}{asks}",
                item.Worn || under ? Hue.Red : held ? Hue.White : Hue.Gray);
        }

        frame.Write(x0 + 2, y0 + boxH - 2,
            items.Count > 0 ? $"1-{items.Count} wield or wear (* held); other closes" : "any key closes",
            Hue.DarkGray);
    }

    /// <summary>
    /// The sheet (D-042): both ledgers on one page. Attributes are what the
    /// Aegis has been paid to shape; skills are what the body counted itself.
    /// </summary>
    private static void DrawSheet(Frame frame, Game game, Layout layout)
    {
        var p = game.Player;
        var choice = game.PendingKnack;
        const int boxW = 54;
        // Every skill gets its own row (a D-091 mending: the Taught and Legend
        // rows used to overwrite the newest skills), then Taught, then Legend.
        int boxH = choice is null ? 12 + SkillSet.Count : 14 + SkillSet.Count + choice.Options.Length;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        string who = p.Name.Length > 0 && p.Folk is { } folk && p.Past is { } past
            ? $"{p.Name}, {CreationCatalog.FolkOf(folk).Name}, once {CreationCatalog.PastOf(past).Name}"
            : "The bearer";
        if (who.Length > boxW - 4) who = who[..(boxW - 4)];
        frame.Write(x0 + 2, y0 + 1, who, Hue.White);

        for (int i = 0; i < AttributeSet.Count; i++)
        {
            var attr = (Attr)i;
            int col = i < 4 ? x0 + 2 : x0 + 24;
            int row = y0 + 3 + (i < 4 ? i : i - 4);
            bool raised = p.Attributes[attr] > AttributeSet.Baseline;
            frame.Write(col, row, $"{AttributeSet.NameOf(attr),-9}{p.Attributes[attr],2}",
                raised ? Hue.White : Hue.Gray);
        }

        for (int i = 0; i < SkillSet.Count; i++)
        {
            var skill = (SkillId)i;
            int level = p.Skills.Level(skill);
            // Both waves' answers on one row (D-055), articles dropped so two
            // knacks and the uses fraction share the box's width.
            var knacks = PerkCatalog.Choices.Where(c => c.Skill == skill)
                .SelectMany(c => c.Options).Where(o => p.HasPerk(o.Id))
                .Select(o => o.Name.StartsWith("the ") ? o.Name[4..] : o.Name).ToList();
            string row = $"{SkillSet.NameOf(skill),-9}{level,2}  {p.Skills.Uses(skill)}/{SkillSet.UsesForLevel(level + 1)}";
            if (knacks.Count > 0) row += $"  {string.Join(", ", knacks)}";
            frame.Write(x0 + 2, y0 + 8 + i, row, level > 0 ? Hue.White : Hue.Gray);
        }

        // The lessons row (D-052): the fourth ledger, what other hands put in.
        frame.Write(x0 + 2, y0 + 8 + SkillSet.Count,
            $"Taught   {(p.Lessons.Count > 0 ? string.Join(", ", p.Lessons.Select(l => LessonCatalog.Def(l).Short)) : "-")}",
            p.Lessons.Count > 0 ? Hue.White : Hue.Gray);

        frame.Write(x0 + 2, y0 + 9 + SkillSet.Count,
            game.Standing > 0
                ? $"Legend {p.Legend,4}   {LegendStanding.TitleOf(game.Standing)}"
                : $"Legend {p.Legend,4}",
            game.Standing > 0 ? Hue.Magenta : Hue.Gray);

        if (choice is not null)
        {
            frame.Write(x0 + 2, y0 + 10 + SkillSet.Count,
                choice.Level >= 4
                    ? $"{SkillSet.NameOf(choice.Skill)} has deepened into a second question:"
                    : $"{SkillSet.NameOf(choice.Skill)} has settled into a question:", Hue.Cyan);
            for (int i = 0; i < choice.Options.Length; i++)
                frame.Write(x0 + 2, y0 + 11 + SkillSet.Count + i,
                    $"{i + 1}) {choice.Options[i].Name}: {choice.Options[i].Blurb}", Hue.White);
            frame.Write(x0 + 2, y0 + boxH - 2,
                $"1-{choice.Options.Length} choose, for good; any other key closes", Hue.DarkGray);
        }
        else
        {
            frame.Write(x0 + 2, y0 + boxH - 2, "any key closes", Hue.DarkGray);
        }
    }

    private static void DrawBox(Frame frame, int x0, int y0, int boxW, int boxH)
    {
        for (int y = 0; y < boxH; y++)
            for (int x = 0; x < boxW; x++)
            {
                bool border = y == 0 || y == boxH - 1 || x == 0 || x == boxW - 1;
                frame.Put(x0 + x, y0 + y, border ? (y is 0 || y == boxH - 1 ? '-' : '|') : ' ',
                    border ? Hue.Cyan : Hue.Gray, Hue.Black);
            }
        frame.Put(x0, y0, '+', Hue.Cyan);
        frame.Put(x0 + boxW - 1, y0, '+', Hue.Cyan);
        frame.Put(x0, y0 + boxH - 1, '+', Hue.Cyan);
        frame.Put(x0 + boxW - 1, y0 + boxH - 1, '+', Hue.Cyan);
    }

    private static void DrawShrineMenu(Frame frame, Game game, Layout layout)
    {
        const int boxW = 42;
        int boxH = 6 + AttributeSet.Count;
        int x0 = Math.Max(0, layout.MapX + (layout.MapW - boxW) / 2);
        int y0 = Math.Max(0, layout.MapY + (layout.MapH - boxH) / 2);

        DrawBox(frame, x0, y0, boxW, boxH);
        frame.Write(x0 + 2, y0 + 1, $"The Shrine of {game.World.SettlementName}", Hue.White);
        frame.Write(x0 + 2, y0 + 2, $"Essence {game.Player.Essence}   next raise costs {game.NextRaiseCost}", Hue.Cyan);

        for (int i = 0; i < AttributeSet.Count; i++)
        {
            var attr = (Attr)i;
            bool affordable = game.Player.Essence >= game.NextRaiseCost;
            frame.Write(x0 + 2, y0 + 4 + i,
                $"{i + 1}) {AttributeSet.NameOf(attr),-9} {game.Player.Attributes[attr],2}",
                affordable ? Hue.White : Hue.DarkGray);
        }

        frame.Write(x0 + 2, y0 + boxH - 2, "1-7 raise; any other key to rise", Hue.DarkGray);
    }

    private static void DrawHeader(Frame frame, Game game)
    {
        string header = $" AEGIS | {game.World.Name} | {game.World.SettlementName} | Cycle {game.Cycle} | T{game.Turn}";
        frame.Write(0, 0, header.PadRight(frame.Width), Hue.Black, Hue.DarkCyan);
    }

    private static void DrawMap(Frame frame, Game game, Layout layout)
    {
        var map = game.CurrentMap;
        var (mapX, mapY, mapW, mapH) = (layout.MapX, layout.MapY, layout.MapW, layout.MapH);

        int camX = Math.Clamp(game.Player.Pos.X - mapW / 2, 0, Math.Max(0, map.Width - mapW));
        int camY = Math.Clamp(game.Player.Pos.Y - mapH / 2, 0, Math.Max(0, map.Height - mapH));

        for (int sy = 0; sy < mapH; sy++)
        {
            for (int sx = 0; sx < mapW; sx++)
            {
                var p = new Pos(camX + sx, camY + sy);
                if (!map.InBounds(p)) continue;
                var (ch, fg, bg) = Glyph(map[p]);
                frame.Put(mapX + sx, mapY + sy, ch, fg, bg);
            }
        }

        void PutWorld(Pos p, char ch, Hue fg, Hue bg = Hue.Black)
        {
            int sx = p.X - camX, sy = p.Y - camY;
            if (sx >= 0 && sx < mapW && sy >= 0 && sy < mapH)
                frame.Put(mapX + sx, mapY + sy, ch, fg, bg);
        }

        // Telegraphed intent cells: the readable danger the combat design runs on.
        // A boar's charge (D-053) is a lane, not a cell: the whole run is marked,
        // to the length it can carry, because sideways is the only honest dodge.
        foreach (var monster in game.LiveMonstersHere)
            if (monster.Intent is { } intent)
            {
                // Familiarity-sharpened telegraphs (D-059): a stranger's wind-up
                // shows only where it is aimed. The shape of it (a boar's whole
                // lane, a warder's nine-cell burst) is drawn for a bearer who has
                // read the kind before. The aimed cell is always shown, so the
                // dodge is always there; the shape is what the read reveals.
                if (game.Player.ReadOf(monster.Kind, game.Cycle) == ReadTier.Blur)
                {
                    PutWorld(intent.TargetCell, '!', Hue.White, Hue.DarkRed);
                }
                else if (intent.Kind == IntentKind.BoarCharge)
                {
                    int dx = Math.Sign(intent.TargetCell.X - monster.Pos.X);
                    int dy = Math.Sign(intent.TargetCell.Y - monster.Pos.Y);
                    var lane = monster.Pos;
                    for (int i = 0; i < Game.BowRange; i++)
                    {
                        lane = lane.Plus(dx, dy);
                        if (!map.Walkable(lane)) break;
                        PutWorld(lane, '!', Hue.White, Hue.DarkRed);
                    }
                }
                else if (intent.Kind == IntentKind.LoftedStone)
                {
                    // The lofted cast (D-057) bursts: the mark is nine cells,
                    // the full price at its middle and the graze around it,
                    // because a dodge the drawing undersells is not a dodge.
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            var cell = intent.TargetCell.Plus(dx, dy);
                            if (map.Walkable(cell))
                                PutWorld(cell, '!', Hue.White, dx == 0 && dy == 0 ? Hue.DarkRed : Hue.Red);
                        }
                }
                else
                {
                    PutWorld(intent.TargetCell, '!', Hue.White, Hue.DarkRed);
                }
            }

        // The bearer's own committed blow (D-058): commitment runs both ways, so
        // the heave marks its cell the way a monster's intent marks its own, in
        // the bearer's own amber rather than the field's red.
        if (game.Player.HeaveTarget is { } heaveCell)
            PutWorld(heaveCell, '!', Hue.White, Hue.DarkYellow);

        // The levin held (D-091): the caster's commitment marks its ground in
        // the bearer's own amber, exactly as the heave marks its cell.
        if (game.Player.LevinTarget is { } levinCell)
            PutWorld(levinCell, '!', Hue.White, Hue.DarkYellow);

        // The graven stone (D-091): the descent's prize, standing where the
        // site's fabric runs deepest, until its word is read.
        if (game.CurrentSite is { StonePos: { } stonePos, StoneRead: false })
            PutWorld(stonePos, 'I', Hue.Cyan);

        if (game.Remnant is { } remnant && remnant.MapId == map.Id)
            PutWorld(remnant.Pos, '%', Hue.Magenta);

        if (game.CurrentSite is { ChestLooted: false } site)
            PutWorld(site.ChestPos, '$', Hue.Yellow);

        // The gleanings (D-052): drawn only for a bearer taught to see them.
        if (game.Mode == MapMode.Overworld && game.Player.HasLesson(LessonId.Gleaning))
            foreach (var spot in game.World.Gleanings)
                PutWorld(spot, '"', Hue.Green);

        // The herbs (D-074): plain to any eye, so drawn for everyone, unlike the gleanings.
        if (game.Mode == MapMode.Overworld)
            foreach (var spot in game.World.Herbs)
                PutWorld(spot, '*', Hue.Green);

        if (game.Mode == MapMode.Overworld)
            foreach (var npc in game.World.Npcs)
                PutWorld(npc.Pos, 'p', npc.Kind == NpcKind.Severed ? Hue.Magenta : Hue.Green);

        foreach (var monster in game.LiveMonstersHere)
        {
            char ch = monster.Kind switch
            {
                MonsterKind.Wight => 'w',
                MonsterKind.Severed => 's',
                MonsterKind.Graven => 'm',
                MonsterKind.Hound => 'd',
                MonsterKind.Carl => 'c',
                MonsterKind.Boar => 'b',
                MonsterKind.Warder => 'v',
                MonsterKind.Thegn => 't',
                _ => 'g',
            };
            var calm = monster.Kind switch
            {
                MonsterKind.Wight => Hue.Cyan,
                MonsterKind.Severed => Hue.Magenta,
                // A sleeping graven man is drawn like the stone it is pretending
                // to be, unless the veil-word has shown it for what it is (D-091).
                MonsterKind.Graven => monster.Dormant && !(game.CurrentSite?.Unveiled ?? false) ? Hue.DarkGray : Hue.DarkYellow,
                MonsterKind.Hound => Hue.DarkCyan,
                MonsterKind.Carl => Hue.Yellow,
                MonsterKind.Boar => Hue.DarkRed,
                // A watching warder is drawn low, part of the works it stands,
                // unless the veil-word has picked it off the works (D-091).
                MonsterKind.Warder => monster.Dormant && !(game.CurrentSite?.Unveiled ?? false) ? Hue.DarkGray : Hue.DarkGreen,
                // The sword-thegn stands steel-grey and still, giving nothing away.
                MonsterKind.Thegn => Hue.Gray,
                _ => Hue.Red,
            };
            PutWorld(monster.Pos, ch, monster.Intent is null ? calm : Hue.White,
                monster.Intent is null ? Hue.Black : Hue.DarkRed);
        }

        PutWorld(game.Player.Pos, '@', Hue.White);
    }

    private static (char, Hue, Hue) Glyph(Terrain t) => t switch
    {
        Terrain.Grass => ('.', Hue.DarkGreen, Hue.Black),
        Terrain.Forest => ('&', Hue.Green, Hue.Black),
        Terrain.Hills => ('^', Hue.DarkYellow, Hue.Black),
        Terrain.Water => ('~', Hue.Blue, Hue.Black),
        Terrain.House => ('#', Hue.Yellow, Hue.Black),
        Terrain.Shrine => ('+', Hue.Cyan, Hue.Black),
        Terrain.CampEntrance => ('>', Hue.Red, Hue.Black),
        Terrain.Wall => ('#', Hue.DarkGray, Hue.Black),
        Terrain.Floor => ('.', Hue.Gray, Hue.Black),
        Terrain.ExitLadder => ('<', Hue.White, Hue.Black),
        Terrain.Waygate => ('O', Hue.Magenta, Hue.Black),
        Terrain.BarrowEntrance => ('n', Hue.DarkYellow, Hue.Black),
        Terrain.HollowEntrance => ('o', Hue.White, Hue.Black),
        Terrain.ThresholdEntrance => ('v', Hue.Magenta, Hue.Black),
        Terrain.Hearth => ('*', Hue.Yellow, Hue.Black),
        Terrain.QuarryEntrance => ('x', Hue.DarkYellow, Hue.Black),
        Terrain.HallEntrance => ('H', Hue.DarkCyan, Hue.Black),
        Terrain.SonghallEntrance => ('S', Hue.Cyan, Hue.Black),
        Terrain.Plinth => ('T', Hue.White, Hue.Black),
        Terrain.RingfortEntrance => ('0', Hue.Yellow, Hue.Black),
        Terrain.LeaguerEntrance => ('U', Hue.Blue, Hue.Black),
        Terrain.WildsEntrance => ('"', Hue.Green, Hue.Black),
        _ => ('?', Hue.Magenta, Hue.Black),
    };

    private static void DrawSidebar(Frame frame, Game game, Layout layout)
    {
        var p = game.Player;
        int y = 1;
        void Line(string text, Hue fg = Hue.Gray)
        {
            if (y < layout.LogY - 1) frame.Write(layout.SideX, y, text, fg);
            y++;
        }

        Line(p.Name.Length > 0 ? p.Name : "The Bearer", Hue.White);
        Line(new string('-', 22), Hue.DarkGray);
        Line($"HP  {Bar(p.Hp, p.EffectiveMaxHp, 10)} {p.Hp}/{p.EffectiveMaxHp}", p.Hp * 3 <= p.EffectiveMaxHp ? Hue.Red : Hue.Gray);
        Line($"ST  {Bar(p.Stamina, p.MaxStamina, 10)} {p.Stamina}/{p.MaxStamina}", Hue.Gray);
        // The pool (D-091): unveiled with the first word, and never before.
        if (p.Spells.Count > 0)
            Line($"FO  {Bar(p.Focus, p.MaxFocus, 10)} {p.Focus}/{p.MaxFocus}", Hue.Cyan);
        Line($"Coin    {p.Coin}", Hue.Yellow);
        Line($"Essence {p.Essence}", Hue.Cyan);
        if (p.Rations > 0) Line($"Rations {p.Rations}", Hue.Green);
        // The vials (D-090): the stillroom's craft rides the rail beside the bread.
        if (p.Draughts > 0) Line($"Vials   {p.Draughts}", Hue.Green);
        if (p.Legend > 0) Line($"Legend  {p.Legend}", Hue.Magenta);
        if (game.Standing > 0) Line($" {LegendStanding.TitleOf(game.Standing)}", Hue.DarkGray);
        // The stead's regard (D-076): per-world and transient, so it lives on the
        // live rail, not the permanent sheet, right under the songs' own standing.
        if (game.Regard > 0) Line($" {SteadRegard.TitleOf(game.Regard)}", Hue.Green);
        // The raiders' wrath (D-078): the enemy ledger, in the enemy's color.
        if (game.Wrath > 0) Line($" {RaiderWrath.TitleOf(game.Wrath)}", Hue.Red);
        // The stead's suspicion (D-086): the home ledger's dark side, beside the
        // regard it never cancels: a friend and watched can both be true, and show.
        if (game.Shame > 0) Line($" {SteadShame.TitleOf(game.Shame)}", Hue.Red);
        if (p.Weapon is { } wpn) Line($"Wpn {wpn.Name}{(wpn.Worn ? "!" : "")}", wpn.Worn ? Hue.Red : Hue.Gray);
        if (p.Bow is { } bow) Line($"Bow {bow.Name}{(bow.Worn ? "!" : "")}", bow.Worn ? Hue.Red : Hue.Gray);
        if (p.Armor is { } arm) Line($"Arm {arm.Name}{(arm.Worn ? "!" : "")}", arm.Worn ? Hue.Red : Hue.Gray);
        if (p.WoundedTurns > 0) Line($"WOUNDED ({p.WoundedTurns})", Hue.Red);
        y++;

        if (game.Mode == MapMode.Site)
        {
            Line(game.CurrentSite!.Kind switch
            {
                SiteKind.Barrow => "The barrow",
                SiteKind.Hollow => "The stone ring",
                SiteKind.Threshold => "The last stair",
                SiteKind.Quarry => "The old quarry",
                SiteKind.Hall => "The fallen hall",
                SiteKind.Ringfort => "The ringfort",
                SiteKind.Songhall => "The songhall",
                SiteKind.Leaguer => "The fen-leaguer",
                _ => "Goblin cave",
            }, Hue.White);
            int alive = game.LiveMonstersHere.Count();
            Line($"Foes here: {alive}", alive > 0 ? Hue.Red : Hue.DarkGreen);
            if (game.InAim) Line("Shaft set: choose a line", Hue.Cyan);
            if (game.InThrust) Line("Spear leveled: choose a line", Hue.Cyan);
            if (game.InHeave) Line("Feet set: choose a line", Hue.DarkYellow);
            if (game.InCastLine) Line("The word waits: choose a line", Hue.Cyan);
            if (game.Player.HeaveTarget is not null) Line("! heave wound up: act to loose", Hue.DarkYellow);
            if (game.Player.LevinTarget is not null) Line("! levin held: act to say it", Hue.DarkYellow);
            if (game.Player.WardTurns > 0) Line($"Warded ({game.Player.WardTurns})", Hue.Cyan);
            if (game.CurrentSite is { StonePos: { } stonePos, StoneRead: false } && stonePos == p.Pos)
                Line("Graven stone: g reads", Hue.Cyan);
            // The read, in words (D-059). A stranger's wind-up reads as danger
            // without a name; a read tell is named; a keen read knows its weight.
            static string Weight(IntentKind k) => k switch
            {
                IntentKind.HurledStone or IntentKind.CrushingBlow => " (light)",
                _ => " (heavy)",
            };
            foreach (var monster in game.LiveMonstersHere.Where(m => m.Intent is not null))
            {
                var tier = game.Player.ReadOf(monster.Kind, game.Cycle);
                if (tier == ReadTier.Blur)
                {
                    Line($"! {monster.Name} coils, unread", Hue.DarkRed);
                    continue;
                }
                string named = monster.Intent!.Kind switch
                {
                    IntentKind.BarrowBlade => "! barrow blade poised",
                    IntentKind.SunderingCut => "! sundering cut poised",
                    IntentKind.HurledStone => "! hurled stone incoming",
                    IntentKind.GravenFist => "! graven fist poised",
                    IntentKind.ThroatLunge => "! throat-lunge gathering",
                    IntentKind.LoftedStone => "! sling-stone falling",
                    _ => "! crushing blow poised",
                };
                if (tier == ReadTier.Keen) named += Weight(monster.Intent!.Kind);
                Line(named, Hue.Red);
            }
        }
        else
        {
            Line(game.World.SettlementName, Hue.White);
            var here = game.CurrentMap[p.Pos];
            if (here == Terrain.Shrine) Line("At the shrine: r rests", Hue.Cyan);
            if (here == Terrain.CampEntrance) Line("Cave mouth: > enters", Hue.Red);
            if (here == Terrain.BarrowEntrance) Line("Barrow mouth: > enters", Hue.DarkYellow);
            if (here == Terrain.HollowEntrance) Line("Stone ring: > enters", Hue.White);
            if (here == Terrain.QuarryEntrance) Line("Quarry rim: > descends", Hue.DarkYellow);
            if (here == Terrain.HallEntrance) Line("Fallen gate: > enters", Hue.DarkCyan);
            if (here == Terrain.RingfortEntrance) Line("Fort gate: > enters", Hue.Yellow);
            if (here == Terrain.LeaguerEntrance) Line("Leaguer banks: > enters", Hue.Blue);
            if (here == Terrain.WildsEntrance) Line("Game-trail: > enters", Hue.Green);
            if (here == Terrain.SonghallEntrance) Line("Songhall door: > enters", Hue.Cyan);
            if (here == Terrain.ThresholdEntrance)
                Line(game.Player.CommissionHeard ? "Deep stair: > descends" : "Deep stair: shut", Hue.Magenta);
            if (here == Terrain.Waygate)
                Line(game.CampCleared ? "Waygate hums: > crosses" : "Waygate: shut", Hue.Magenta);
            foreach (var npc in game.World.Npcs)
                if (npc.Pos.Chebyshev(p.Pos) == 1)
                    Line($"{npc.Name}: bump to talk", Hue.Green);
        }

        if (game.Remnant is { } remnant)
        {
            y++;
            Line("Remnant dropped:", Hue.Magenta);
            Line($" {remnant.Coin}c {remnant.Essence}e ({remnant.MapId})", Hue.Magenta);
        }

        y++;
        Line("hjkl/yubn move  . wait", Hue.DarkGray);
        Line("g grab  >/< enter/exit", Hue.DarkGray);
        Line("f loose  e eat  d drink", Hue.DarkGray);
        Line("z cast  i gear  c you", Hue.DarkGray);
        Line("q quit", Hue.DarkGray);
    }

    private static string Bar(int value, int max, int slots)
    {
        int filled = max <= 0 ? 0 : Math.Clamp(value * slots / max, 0, slots);
        return new string('=', filled) + new string(' ', slots - filled);
    }

    private static void DrawLog(Frame frame, Game game, Layout layout)
    {
        int logY = layout.LogY;
        frame.Write(0, logY - 1, new string('-', frame.Width), Hue.DarkGray);
        int y = logY;
        foreach (var entry in game.Log.Recent(LogLines))
        {
            var fg = entry.Tone switch
            {
                LogTone.Aegis => Hue.Cyan,
                LogTone.Danger => Hue.Red,
                LogTone.Reward => Hue.Yellow,
                LogTone.Combat => Hue.Gray,
                _ => Hue.Gray,
            };
            string text = entry.Text.Length > frame.Width ? entry.Text[..frame.Width] : entry.Text;
            frame.Write(0, y++, text, fg);
        }
    }
}

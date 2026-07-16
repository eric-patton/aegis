using Aegis.Core;

namespace Aegis.Core.Tests;

public class CombatTests
{
    /// <summary>Places the player in the camp next to a goblin, other goblins removed.</summary>
    private static (Game Game, Monster Goblin) ArrangeDuel(ulong seed = 11)
    {
        var game = new Game(seed);
        game.Debug_SetMode(MapMode.Site);

        var goblin = game.Monsters[0];
        foreach (var other in game.Monsters.Skip(1)) other.Hp = 0;

        // Find a walkable cell adjacent to the goblin for the player.
        foreach (var (dx, dy) in Directions.All8)
        {
            var p = goblin.Pos.Plus(dx, dy);
            if (game.World.Camp.Walkable(p))
            {
                game.Debug_SetPlayerPos(p);
                return (game, goblin);
            }
        }
        throw new InvalidOperationException("no adjacent open cell (seed choice)");
    }

    [Fact]
    public void BumpAttack_DamagesGoblin_AndCostsStamina()
    {
        var (game, goblin) = ArrangeDuel();
        int hpBefore = goblin.Hp;
        int staminaBefore = game.Player.Stamina;

        AttackToward(game, goblin);

        Assert.True(goblin.Hp < hpBefore);
        Assert.True(game.Player.Stamina < staminaBefore);
    }

    [Fact]
    public void KillingGoblin_PaysCoinAndEssence()
    {
        var (game, goblin) = ArrangeDuel();
        goblin.Hp = 1;

        AttackToward(game, goblin);

        Assert.False(goblin.Alive);
        Assert.True(game.Player.Coin > 0);
        Assert.Equal(5, game.Player.Essence);
    }

    [Fact]
    public void ClearingCamp_WritesDeedFact()
    {
        var (game, goblin) = ArrangeDuel(); // others already dead; this goblin is the last
        goblin.Hp = 1;

        AttackToward(game, goblin);

        Assert.True(game.CampCleared);
        Assert.True(game.World.Facts.Exists("deed", "camp_cleared"));
    }

    [Fact]
    public void TelegraphedBlow_MissesIfPlayerMoves_HitsIfPlayerStays()
    {
        // Deterministically probe both outcomes by scanning seeds for a windup.
        bool sawMiss = false, sawHit = false;

        for (ulong seed = 1; seed <= 60 && (!sawMiss || !sawHit); seed++)
        {
            foreach (bool stay in new[] { false, true })
            {
                var (game, goblin) = ArrangeDuel(seed);
                int hpBefore = game.Player.Hp;

                // Wait until the goblin telegraphs (or give up on this seed).
                for (int i = 0; i < 6 && goblin.Intent is null && game.Player.Hp > 0; i++)
                    game.Apply(Command.Wait);
                if (goblin.Intent is null) continue;

                var targeted = goblin.Intent.TargetCell;
                Assert.Equal(game.Player.Pos, targeted);

                if (stay)
                {
                    int hpAtWindup = game.Player.Hp;
                    game.Apply(Command.Wait);
                    if (game.Player.Hp < hpAtWindup) sawHit = true;
                }
                else
                {
                    // Step to any walkable cell that is not the targeted one.
                    foreach (var (dx, dy) in Directions.All8)
                    {
                        var p = game.Player.Pos.Plus(dx, dy);
                        if (p != targeted && game.World.Camp.Walkable(p) && p != goblin.Pos)
                        {
                            int hpAtWindup = game.Player.Hp;
                            game.Apply(DirToCommand(dx, dy));
                            if (game.Player.Pos != targeted &&
                                !game.Log.Entries.Any(e => e.Text.Contains("crushing blow lands") && e.Turn == game.Turn))
                                sawMiss = true;
                            break;
                        }
                    }
                }
            }
        }

        Assert.True(sawHit, "never observed a landing crushing blow across seeds");
        Assert.True(sawMiss, "never observed a dodged crushing blow across seeds");
    }

    private static void AttackToward(Game game, Monster goblin)
    {
        int dx = Math.Sign(goblin.Pos.X - game.Player.Pos.X);
        int dy = Math.Sign(goblin.Pos.Y - game.Player.Pos.Y);
        game.Apply(DirToCommand(dx, dy));
    }

    private static Command DirToCommand(int dx, int dy) => (dx, dy) switch
    {
        (0, -1) => Command.MoveN,
        (0, 1) => Command.MoveS,
        (-1, 0) => Command.MoveW,
        (1, 0) => Command.MoveE,
        (-1, -1) => Command.MoveNW,
        (1, -1) => Command.MoveNE,
        (-1, 1) => Command.MoveSW,
        (1, 1) => Command.MoveSE,
        _ => Command.None,
    };
}

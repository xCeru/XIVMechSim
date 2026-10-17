using System;
using System.Linq;
using System.Numerics;
using AnoMech.Core.Game.Ai;
using AnoMech.Core.Game.Party;
using AnoMech.Core.SimObjects;
using static AnoMech.Scenarios.Fru.FruConstants;
using static AnoMech.Scenarios.Fru.P4DarklitDragonsong.FruP4DarklitDragonsongState;

namespace AnoMech.Scenarios.Fru.P4DarklitDragonsong;

public sealed class FruP4DarklitDragonsongAiLpdu : IScenarioAi<FruP4DarklitDragonsongState>
{
    public string Name => "LPDU";
    public string Group => "EU";

    private const float S = 1f / 2.358f;

    // EU West-to-East priority per FRU-Sim EU_JP_WE_PRIO: t1, t2, h1, h2, m1, m2, r1, r2
    private static readonly PartyRole[] EuWePrio = [PartyRole.MainTank, PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.RegenHealer, PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

    private FruP4DarklitDragonsongState state = null!;
    private SimWorld world = null!;
    private PartyRole[] swapped = [];
    private PartyRole somberTank;
    private bool baitEast;

    public void Run(FruP4DarklitDragonsongState stateParam, SimWorld worldParam)
    {
        state = stateParam;
        world = worldParam;
        swapped = SwapFreeWaterToItsGroupEu();
        somberTank = ChooseSomberTank();
        var ai = new AiManager(world);
        ai.Move(2f, () => ByRole(Middle), arrivalTime: 6.6f);
        ai.Move(6.65f, () => ByRole(OpeningSpreadEu), arrivalTime: 8.8f);
        ai.Move(14.5f, () => ByRole(LineupEu), arrivalTime: 24.2f);
        ai.Move(24.2f, () => ByRole(role => BowtieEu(role, state.LineupOrder.ToArray())), arrivalTime: 28.2f);
        ai.Move(28.2f, () => ByRole(role => BowtieEu(role, swapped)), arrivalTime: TowersAt);
        ai.Move(33.1f, () => ByRole(SpiritTakerSpreadEu), arrivalTime: 35.6f);
        ai.Move(35.65f, () => ByRole(WaterStackEu), arrivalTime: 40f);
        world.Events.Add(40.5f, BaitSomberDanceAwayFromOracle);
        ai.Move(40.6f, () => ByRole(role => role == somberTank ? FromSim(0, baitEast ? 43 : -43) : null), arrivalTime: 43.8f);
        ai.Move(42.4f, () => ByRole(role => role == somberTank ? null : DancePartyEu(role)), arrivalTime: 43.8f);
        ai.Move(45f, () => ByRole(role => role == somberTank ? JustPastOracleFromCenter() : null), jitter: 0f, arrivalTime: 47f);
        ai.Move(50.15f, () => ByRole(AkhMornEu), arrivalTime: 57.7f);
    }

    // EU water swap: applies EU_JP_WE_PRIO (tanks first, then healers, then DPS) when deciding
    // which side the free water swaps to, instead of NAUR's healer-first order.
    private PartyRole[] SwapFreeWaterToItsGroupEu()
    {
        var after = state.LineupOrder.ToArray();
        if ((state.TetherWaterIndex < 2) == (state.FreeWaterIndex < 2))
        {
            var freeWaterRole = after[4 + state.FreeWaterIndex];
            var swapIdx = 4 + 3 - state.FreeWaterIndex;
            (after[4 + state.FreeWaterIndex], after[swapIdx]) = (after[swapIdx], after[4 + state.FreeWaterIndex]);
        }
        return after;
    }

    private PartyRole ChooseSomberTank()
    {
        var player = world.Party.PlayerRole;
        if (world.Party.Get(PartyRole.MainTank) is { } mainTank && world.Party.IsBotDriven(mainTank))
            return PartyRole.MainTank;
        return PartyRole.OffTank;
    }

    private Vector3 OraclePosition()
        => world.Children.OfType<SimEnemy>().FirstOrDefault(e => e.BNpcBaseId == BNpcBaseId.OracleP4)?.Position ?? Vector3.Zero;

    private Vector3 JustPastOracleFromCenter()
    {
        var oracle = OraclePosition();
        return oracle.LengthSquared() > 0.01f ? oracle + Vector3.Normalize(oracle) * 2f : oracle;
    }

    private void BaitSomberDanceAwayFromOracle()
    {
        var x = OraclePosition().X;
        baitEast = x < -4f * S || MathF.Abs(x) < 4f * S && state.EastWing;
    }

    private static Vector3 FromSim(float north, float east) => new(east * S, 0, -north * S);

    private static Vector3 Quadrant(int index, float north, float east)
        => FromSim(index % 4 < 2 ? north : -north, index % 4 is 1 or 2 ? east : -east);

    // EU W/E index: position within EU_JP_WE_PRIO ordering (tanks 0-1, healers 2-3, melee 4-5, ranged 6-7).
    private int EuIndex(PartyRole role) => Array.IndexOf(EuWePrio, role);

    private bool EuNorth(PartyRole role) => EuIndex(role) < 4;

    private static Vector3? Middle(PartyRole role) => FromSim((int)role % 2 * 0.3f, (int)role * 0.08f);

    // EU opening spread: tanks/healers (West side) and DPS (East side) use EU_JP_WE_PRIO ordering.
    private Vector3? OpeningSpreadEu(PartyRole role) => role switch
    {
        PartyRole.MainTank => FromSim(16.4f, 10.1f),
        PartyRole.OffTank => FromSim(10.1f, 16.4f),
        PartyRole.RegenHealer => FromSim(10.1f, -16.4f),
        PartyRole.ShieldHealer => FromSim(16.4f, -10.1f),
        PartyRole.MeleeDpsA => FromSim(-16.4f, 10.1f),
        PartyRole.MeleeDpsB => FromSim(-10.1f, 16.4f),
        PartyRole.PhysRangedDps => FromSim(-10.1f, -16.4f),
        _ => FromSim(-16.4f, -10.1f),
    };

    private Vector3? LineupEu(PartyRole role) => role switch
    {
        PartyRole.MainTank => FromSim(4.5f, 5),
        PartyRole.OffTank => FromSim(0.2f, 11.3f),
        PartyRole.RegenHealer => FromSim(0.2f, -11.3f),
        PartyRole.ShieldHealer => FromSim(4.5f, -5),
        PartyRole.MeleeDpsA => FromSim(-25.5f, 5),
        PartyRole.MeleeDpsB => FromSim(-21.2f, 11.3f),
        PartyRole.PhysRangedDps => FromSim(-21.2f, -11.3f),
        _ => FromSim(-25.5f, -5),
    };

    // EU bowtie: positions assigned by EU W/E index rather than NAUR lineup order.
    private Vector3? BowtieEu(PartyRole role, PartyRole[] order)
    {
        var index = Array.IndexOf(order, role);
        return index < 4 ? Quadrant(index, 19, 5.2f) : Quadrant(index, 6.7f, 14.6f);
    }

    private Vector3? SpiritTakerSpreadEu(PartyRole role)
    {
        var index = Array.IndexOf(swapped, role);
        if (index < 4) return Quadrant(index, 19, 17.2f);
        var west = index is 4 or 7;
        return FromSim(0, west ? ((int)role < 4 ? -38 : -19) : ((int)role < 4 ? 19 : 0));
    }

    private Vector3? WaterStackEu(PartyRole role)
        => FromSim(EuNorth(role) ? 17.3f : -17.3f, state.EastWing ? -15.5f : 15.5f) + Middle(role)!.Value * 0.3f;

    private Vector3? DancePartyEu(PartyRole role) => FromSim(EuNorth(role) ? 15 : -15, 0) + Middle(role)!.Value * 0.3f;

    private static Vector3? AkhMornEu(PartyRole role) => role == PartyRole.MainTank ? new Vector3(-9.3f, 0, 0) : Vector3.Zero;

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}

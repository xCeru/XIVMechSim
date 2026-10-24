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
    private static readonly PartyRole[] EuWePrio =
        [PartyRole.MainTank, PartyRole.OffTank, PartyRole.ShieldHealer, PartyRole.RegenHealer,
         PartyRole.MeleeDpsA, PartyRole.MeleeDpsB, PartyRole.PhysRangedDps, PartyRole.CasterDps];

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
        ai.Move(2f, () => ByRole(MiddleEu), arrivalTime: 6.6f);
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

    // EU water swap: applies EU_JP_WE_PRIO when deciding which side the free water swaps to,
    // so the forced/swapped player ends up on the correct side per EU priority.
    private PartyRole[] SwapFreeWaterToItsGroupEu()
    {
        var after = state.LineupOrder.ToArray();
        if ((state.TetherWaterIndex < 2) == (state.FreeWaterIndex < 2))
        {
            var freeIdx = 4 + state.FreeWaterIndex;
            var freeRole = after[freeIdx];
            var partnerIdx = 4 + 3 - state.FreeWaterIndex;
            // Only swap if they're on the same N/S side (both north or both south).
            var freeNorth = freeIdx < 6; // indices 4,5 are north; 6,7 are south
            var partnerNorth = partnerIdx < 6;
            if (freeNorth == partnerNorth)
            {
                // Swap based on EU W/E priority: lower priority index moves to the other side.
                (after[freeIdx], after[partnerIdx]) = (after[partnerIdx], after[freeIdx]);
            }
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

    private int EuIndex(PartyRole role) => Array.IndexOf(EuWePrio, role);

    private static bool IsSupport(PartyRole role) =>
        role is PartyRole.MainTank or PartyRole.OffTank or PartyRole.ShieldHealer or PartyRole.RegenHealer;

    // EU opening spread (POST_AA_PARTY_EU): tanks west, healers north, DPS south per EU_JP_WE_PRIO.
    private Vector3? OpeningSpreadEu(PartyRole role) => role switch
    {
        PartyRole.MainTank => FromSim(3.0f, -19.0f),
        PartyRole.OffTank => FromSim(-3.0f, -19.0f),
        PartyRole.ShieldHealer => FromSim(19.0f, -3.0f),
        PartyRole.RegenHealer => FromSim(19.0f, 3.0f),
        PartyRole.MeleeDpsA => FromSim(-19.0f, -3.0f),
        PartyRole.MeleeDpsB => FromSim(-19.0f, 3.0f),
        PartyRole.PhysRangedDps => FromSim(-16.4f, 10.1f),
        _ => FromSim(-3.0f, 19.0f),
    };

    // EU LR pre-pos (LR_PARTY_EU after S_OFFSET).
    private Vector3? LineupEu(PartyRole role) => role switch
    {
        PartyRole.MainTank => FromSim(-13.5f, -19.0f),
        PartyRole.OffTank => FromSim(-7.5f, -19.0f),
        PartyRole.ShieldHealer => FromSim(8.5f, -3.0f),
        PartyRole.RegenHealer => FromSim(8.5f, 3.0f),
        PartyRole.MeleeDpsA => FromSim(-29.7f, -2.2f),
        PartyRole.MeleeDpsB => FromSim(-29.2f, 5.4f),
        PartyRole.PhysRangedDps => FromSim(-24.2f, 13.7f),
        _ => FromSim(-15.9f, 18.7f),
    };

    private static Vector3? MiddleEu(PartyRole role) => FromSim((int)role % 2 * 0.3f, (int)role * 0.08f);

    // EU bowtie: non-tethered players (indices 4-7) split supports west (NW/SW = left)
    // and DPS east (NE/SE = right), ordered by EU W/E priority within each side.
    // Tethered players (indices 0-3) keep NA bowtie positions.
    private Vector3? BowtieEu(PartyRole role, PartyRole[] order)
    {
        var index = Array.IndexOf(order, role);
        if (index < 4) return Quadrant(index, 19, 5.2f);

        // Non-tethered: split by support vs DPS, order by EU W/E priority within each side.
        var nonTethers = order.Skip(4).Take(4).ToArray();
        var supports = nonTethers.Where(IsSupport).OrderBy(EuIndex).ToArray();
        var dps = nonTethers.Where(r => !IsSupport(r)).OrderBy(EuIndex).ToArray();

        // NW = first support, SW = second support (west / left)
        // NE = first DPS, SE = second DPS (east / right)
        return role switch
        {
            _ when role == supports[0] => Quadrant(0, 6.7f, 14.6f), // NW
            _ when role == supports[1] => Quadrant(3, 6.7f, 14.6f), // SW
            _ when role == dps[0] => Quadrant(1, 6.7f, 14.6f),     // NE
            _ => Quadrant(2, 6.7f, 14.6f),                          // SE
        };
    }

    // EU Spirit spread (SPIRIT_DD_EU): tethers at SP_TETHER quadrants; non-tethers at static bait positions.
    // Supports go west (SP_W_SUP far, SP_W_DPS mid), DPS go east (SP_E_DPS center, SP_E_SUP mid) per FRU-Sim EU.
    private Vector3? SpiritTakerSpreadEu(PartyRole role)
    {
        var index = Array.IndexOf(swapped, role);
        if (index < 4) return Quadrant(index, 19, 17.2f);

        var nonTethers = swapped.Skip(4).Take(4).ToArray();
        var supports = nonTethers.Where(IsSupport).OrderBy(EuIndex).ToArray();
        var dps = nonTethers.Where(r => !IsSupport(r)).OrderBy(EuIndex).ToArray();

        return role switch
        {
            _ when role == supports[0] => FromSim(0, -38f),   // SP_W_SUP - far west support
            _ when role == supports[1] => FromSim(0, -19f),   // SP_W_DPS - mid west support
            _ when role == dps[0] => FromSim(0, 0f),          // SP_E_DPS - center/east DPS
            _ => FromSim(0, 19f),                             // SP_E_SUP - mid east DPS
        };
    }

    // Water stacks: everyone moves to the side opposite the wings (east wing → west side,
    // west wing → east side). North/south based on swapped index, matching NA behavior.
    private Vector3? WaterStackEu(PartyRole role)
    {
        var index = Array.IndexOf(swapped, role);
        var north = index % 4 < 2 ? 17.3f : -17.3f;
        var east = state.EastWing ? -15.5f : 15.5f;
        return FromSim(north, east) + MiddleEu(role)!.Value * 0.3f;
    }

    // Somber Dance stacks: everyone goes north or south based on swapped index, no wing flip.
    private Vector3? DancePartyEu(PartyRole role)
    {
        var index = Array.IndexOf(swapped, role);
        var north = index % 4 < 2 ? 15f : -15f;
        return FromSim(north, 0f) + MiddleEu(role)!.Value * 0.3f;
    }

    private static Vector3? AkhMornEu(PartyRole role) => role == PartyRole.MainTank ? new Vector3(-9.3f, 0, 0) : Vector3.Zero;

    private static IAiMove ByRole(Func<PartyRole, Vector3?> at)
        => AiMove.Create(Enum.GetValues<PartyRole>().Select(role => at(role) is { } p ? new Vector2(p.X, p.Z) : (Vector2?)null).ToArray()).NaturalOrder();
}

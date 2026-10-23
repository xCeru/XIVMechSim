using AnoMech.Core.Game.Party;

namespace AnoMech.Scenarios.Fru.P3Apocalypse;

// User-forced rolls for FruP3ApocalypseState; null leaves a field randomized.
public sealed class FruP3ApocalypseStateOverrides
{
    // Eighths of a turn from north, 0-3.
    public int? Rotation { get; set; }
    public bool? Clockwise { get; set; }
    public PartyRole? SpiritTakerTarget { get; set; }
    // Freepoc = static positions after water hit; Permaswap = keep swapped positions.
    public bool? Swap { get; set; }
    // Force the selected role to swap water groups by sharing a duration with the lowest
    // priority player in their group (same logic as FRU-Sim's force swap).
    public PartyRole? ForceSwapTarget { get; set; }
}

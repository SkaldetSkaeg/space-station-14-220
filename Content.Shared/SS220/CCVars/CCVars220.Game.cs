using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public partial class CCVars220
{
    /// <summary>
    ///     Whether the played map memory is enabled
    /// </summary>
    public static readonly CVarDef<bool> GamePlayedMapMemory =
        CVarDef.Create("game.played_map_memory", true, CVar.SERVERONLY);

    /// <summary>
    ///     The depth of the queue of played maps when full-cycle memory is disabled.
    /// </summary>
    public static readonly CVarDef<int> GamePlayedMapMemoryDepth =
        CVarDef.Create("game.played_map_memory_depth", 1, CVar.SERVERONLY);

    /// <summary>
    ///     Remember all played maps until every currently eligible map has been played.
    ///     Requires played map memory and ignores its configured depth while enabled.
    ///     Excludes the last played map after a cycle reset when another map is eligible.
    /// </summary>
    [CVarControl(AdminFlags.Round)]
    public static readonly CVarDef<bool> GamePlayedMapMemoryCycle =
        CVarDef.Create("game.played_map_memory_cycle", false, CVar.SERVERONLY);
}

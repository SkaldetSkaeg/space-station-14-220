// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Administration;
using Content.Shared.CCVar.CVarAccess;
using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

public sealed partial class CCVars220
{
    /// <summary>
    ///     Maximum number of randomly selected eligible maps offered in a map vote.
    ///     Zero disables the limit.
    /// </summary>
    [CVarControl(AdminFlags.Round, min: 0, max: int.MaxValue)]
    public static readonly CVarDef<int> VoteMaxMapsPerVote =
        CVarDef.Create("vote.max_maps_per_vote", 3, CVar.SERVERONLY);
}

using Content.Shared.Administration;
using Content.Shared.GameTicking;

namespace Content.Client.Administration.UI.Events;

/// <summary>
/// Localized names for rule picker groups.
/// </summary>
internal static class AdminGameRuleCategoryText
{
    public static string Get(AdminGameRuleCategory category)
    {
        return Loc.GetString("admin-events-group", ("category", category.ToString()));
    }

    public static string Get(StationEventCategory category)
    {
        return Loc.GetString("admin-events-subgroup", ("category", category.ToString()));
    }
}

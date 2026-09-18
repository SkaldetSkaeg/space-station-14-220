using Content.Shared.Administration;

namespace Content.Client.Administration.UI.Events;

/// <summary>
/// Localized names for rule picker groups.
/// </summary>
internal static class AdminGameRuleCategoryText
{
    public static string Get(AdminGameRuleCategory category) =>
        Loc.GetString("admin-events-group", ("category", category.ToString()));

    public static string Get(AdminStationEventCategory category) =>
        Loc.GetString("admin-events-subgroup", ("category", category.ToString()));
}

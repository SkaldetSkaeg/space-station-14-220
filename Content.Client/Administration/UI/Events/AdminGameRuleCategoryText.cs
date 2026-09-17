using Content.Shared.Administration;

namespace Content.Client.Administration.UI.Events;

/// <summary>
/// Localized names for rule picker groups.
/// </summary>
internal static class AdminGameRuleCategoryText
{
    public static string Get(AdminGameRuleCategory category) => Loc.GetString(category switch
    {
        AdminGameRuleCategory.Events => "admin-events-group-events",
        AdminGameRuleCategory.Schedulers => "admin-events-group-schedulers",
        AdminGameRuleCategory.Roles => "admin-events-group-roles",
        AdminGameRuleCategory.RoundComposition => "admin-events-group-round-composition",
        AdminGameRuleCategory.StationVariations => "admin-events-group-station-variations",
        AdminGameRuleCategory.RoundControl => "admin-events-group-round-control",
        AdminGameRuleCategory.SpecialModes => "admin-events-group-special-modes",
        _ => "admin-events-group-other",
    });

    public static string Get(AdminStationEventCategory category) => Loc.GetString(category switch
    {
        AdminStationEventCategory.Antagonists => "admin-events-subgroup-antagonists",
        AdminStationEventCategory.DerelictCyborgs => "admin-events-subgroup-cyborgs",
        AdminStationEventCategory.Creatures => "admin-events-subgroup-creatures",
        AdminStationEventCategory.CargoGifts => "admin-events-subgroup-cargo-gifts",
        AdminStationEventCategory.Meteors => "admin-events-subgroup-meteors",
        AdminStationEventCategory.Shuttles => "admin-events-subgroup-shuttles",
        _ => "admin-events-subgroup-effects",
    });
}

using Content.Shared.Administration;

namespace Content.Client.Administration.UI.Events;

/// <summary>
/// Consistent localized prototype descriptions and configured start delays across the event viewer.
/// </summary>
internal static class AdminEventTooltip
{
    public static string Get(AdminGameRulePrototypeInfo? prototype, string fallback)
    {
        if (prototype == null)
            return fallback;

        var description = Loc.TryGetString($"ent-{prototype.Id}.desc", out var localized)
            ? localized
            : prototype.Description;
        if (string.IsNullOrWhiteSpace(description))
            description = Loc.GetString("admin-events-info-no-description");

        var name = prototype.Id;
        if (prototype.Name.Length > 0 && prototype.Name != prototype.Id)
            name += $"\n{prototype.Name}";

        return $"{name}\n{description}\n{GetDelay(prototype)}";
    }

    private static string GetDelay(AdminGameRulePrototypeInfo prototype)
    {
        var minimum = prototype.MinimumStartDelaySeconds;
        var maximum = prototype.MaximumStartDelaySeconds;
        if (minimum == null || maximum == null || maximum <= 0)
            return Loc.GetString("admin-events-delay-none");

        if (minimum == maximum)
            return Loc.GetString("admin-events-delay-fixed", ("seconds", minimum.Value));

        return Loc.GetString("admin-events-delay-range", ("min", minimum.Value), ("max", maximum.Value));
    }
}

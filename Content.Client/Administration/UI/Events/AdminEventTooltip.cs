using Content.Shared.Administration;
using Robust.Shared.Localization;

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

        // An explicit array avoids the span overload, which is rejected by the client sandbox.
        return string.Join("\n", new[]
        {
            GetName(prototype.Id, prototype.Name),
            GetDescription(prototype.Id, prototype.Description),
            GetDelay(prototype),
        });
    }

    /// <summary>
    /// Shows the prototype ID and a distinct non-empty display name on separate lines.
    /// </summary>
    public static string GetName(string id, string name)
    {
        if (name.Length == 0 || name == id)
            return id;

        return string.Join("\n", new[] { id, name });
    }

    /// <summary>
    /// Prefers the client's localized description and supplies a placeholder for empty descriptions.
    /// </summary>
    public static string GetDescription(string id, string description)
    {
        var localization = IoCManager.Resolve<ILocalizationManager>();
        if (localization.TryGetString($"ent-{id}.desc", out var localized))
            description = localized;

        if (string.IsNullOrWhiteSpace(description))
            return Loc.GetString("admin-events-info-no-description");

        return description;
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

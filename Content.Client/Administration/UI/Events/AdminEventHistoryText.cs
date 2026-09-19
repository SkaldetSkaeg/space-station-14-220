using Content.Shared.Administration;
using Content.Shared.GameTicking;

namespace Content.Client.Administration.UI.Events;

/// <summary>
/// Localized history labels keep unknown origins and reasons distinct from inferred explanations.
/// </summary>
internal static class AdminEventHistoryText
{
    public static string Status(GameRuleHistoryStatus status)
    {
        return Loc.GetString("admin-events-history-status", ("status", status.ToString()));
    }

    public static Color StatusColor(GameRuleHistoryStatus status)
    {
        return status switch
        {
            GameRuleHistoryStatus.Active => Color.LightGreen,
            GameRuleHistoryStatus.Delayed => Color.Gold,
            GameRuleHistoryStatus.Stopped or GameRuleHistoryStatus.Cancelled => Color.LightCoral,
            _ => Color.LightGray,
        };
    }

    public static string Time(TimeSpan? time)
    {
        if (time == null)
            return Loc.GetString("admin-events-history-empty-value");

        return $"{(int)time.Value.TotalHours:00}:{time.Value.Minutes:00}:{time.Value.Seconds:00}";
    }

    public static string Source(GameRuleSource source)
    {
        return Loc.GetString("admin-events-history-source-value",
            ("kind", source.Kind.ToString()),
            ("name", source.Name ?? Loc.GetString("admin-events-history-unknown")),
            ("entity", source.Scheduler?.ToString() ?? "?"),
            ("table", source.Table ?? Loc.GetString("admin-events-inline-table")));
    }

    public static string EndReason(AdminEventHistoryEntry entry)
    {
        if (entry.EndedAt == null)
            return Loc.GetString("admin-events-history-empty-value");

        var reason = Loc.GetString("admin-events-history-end-reason", ("reason", entry.EndReason.ToString()));
        if (entry.EndedBy == null)
            return reason;

        return Loc.GetString("admin-events-history-reason-by", ("reason", reason), ("name", entry.EndedBy));
    }
}

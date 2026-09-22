using Content.Shared.Administration;
using Content.Shared.GameTicking;

namespace Content.Client.Administration.UI.GameRules;

/// <summary>
/// Localized history labels keep unknown origins and reasons distinct from inferred explanations.
/// </summary>
internal static class AdminGameRuleHistoryText
{
    public static string Status(GameRuleHistoryStatus status)
    {
        return Loc.GetString("admin-gamerules-history-status", ("status", status.ToString()));
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
            return Loc.GetString("admin-gamerules-history-empty-value");

        return $"{(int)time.Value.TotalHours:00}:{time.Value.Minutes:00}:{time.Value.Seconds:00}";
    }

    public static string Source(GameRuleSource source)
    {
        return Loc.GetString("admin-gamerules-history-source-value",
            ("kind", source.Kind.ToString()),
            ("name", source.Name ?? Loc.GetString("admin-gamerules-history-unknown")),
            ("entity", source.Scheduler?.ToString() ?? "?"),
            ("table", source.Table ?? Loc.GetString("admin-gamerules-inline-table")));
    }

    public static string EndReason(AdminGameRuleHistoryEntry entry)
    {
        if (entry.EndedAt == null)
            return Loc.GetString("admin-gamerules-history-empty-value");

        var reason = Loc.GetString("admin-gamerules-history-end-reason", ("reason", entry.EndReason.ToString()));
        if (entry.EndedBy == null)
            return reason;

        return Loc.GetString("admin-gamerules-history-reason-by", ("reason", reason), ("name", entry.EndedBy));
    }
}

using Content.Shared.GameTicking;

namespace Content.Server.GameTicking;

/// <summary>
/// Server-side lifecycle of one GameRule, retained until the round restarts.
/// Times are elapsed round time; null means the transition has not occurred.
/// The network identity is captured before deletion for consumers of completed history.
/// </summary>
public sealed record GameRuleHistoryEntry(
    int Sequence,
    NetEntity Entity,
    string Prototype,
    string Name,
    string Description,
    TimeSpan? AddedAt,
    TimeSpan? StartedAt,
    TimeSpan? EndedAt,
    GameRuleHistoryStatus Status,
    GameRuleSource Source,
    GameRuleEndReason EndReason,
    string? EndedBy);

using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking;

/// <summary>
/// Identifies the caller that explicitly supplied a rule's origin. Unknown is never inferred from timing.
/// </summary>
[Serializable, NetSerializable]
public enum GameRuleSourceKind : byte
{
    Unknown,
    Scheduler,
    Administrator,
    ServerConsole,
}

/// <summary>
/// Captured origin of a rule instance. Name is the administrator name or scheduler prototype ID.
/// A scheduler's null Table means an inline selector.
/// </summary>
[Serializable, NetSerializable]
public sealed record GameRuleSource(GameRuleSourceKind Kind, string? Name = null, NetEntity? Scheduler = null, string? Table = null);

/// <summary>
/// Explicit reason supplied by the code ending a rule. Unannotated callers retain Unknown.
/// </summary>
[Serializable, NetSerializable]
public enum GameRuleEndReason : byte
{
    Unknown,
    DurationElapsed,
    Administrator,
    ServerConsole,
    RulesCleared,
    EntityDeleted,
}

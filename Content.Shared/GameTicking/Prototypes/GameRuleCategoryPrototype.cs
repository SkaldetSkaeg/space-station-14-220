using Robust.Shared.Prototypes;

namespace Content.Shared.GameTicking.Prototypes;

/// <summary>
/// Data-defined grouping for GameRules. Categories do not control rule behavior or scheduling.
/// </summary>
[Prototype]
public sealed partial class GameRuleCategoryPrototype : IPrototype
{
    public static readonly ProtoId<GameRuleCategoryPrototype> Default = "Other";

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localized category name.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Categories with higher priority appear first.
    /// </summary>
    [DataField]
    public int Priority;
}

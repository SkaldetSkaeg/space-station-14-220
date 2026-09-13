using Content.Shared.Damage;

namespace Content.Server.Spreader;

/// <summary>
/// Handles entities that spread out when they reach the relevant growth level.
/// </summary>
[RegisterComponent]
public sealed partial class KudzuComponent : Component
{
    /// <summary>
    /// Current growth stage, starting at 1. Growth increases this value and damage can reduce it.
    /// Spreading becomes possible at <see cref="MaxGrowthLevel"/>.
    /// </summary>
    [DataField]
    public int GrowthLevel = 1;

    /// <summary>
    /// Final growth stage and the minimum stage required to spread. Must be at least 1.
    /// Entities using KudzuVisuals need a kudzu_{stage}{variant} sprite state for each stage and variant.
    /// </summary>
    [DataField]
    public int MaxGrowthLevel = 3;

    /// <summary>
    /// Probability, from 0 to 1, of spreading during an eligible neighbor spread event.
    /// Rolled once for the event, not separately for each neighboring tile.
    /// </summary>
    [DataField]
    public float SpreadChance = 1f;

    /// <summary>
    /// Accumulated damage per growth stage lost when damage or healing is applied. Must be greater than zero.
    /// The stage reduction uses total remaining damage divided by this value, rounded down,
    /// rather than just the damage dealt by the latest hit. Growth cannot fall below stage 1.
    /// Direct damage setters do not trigger this reduction.
    /// </summary>
    [DataField]
    public float GrowthHealth = 10.0f;

    /// <summary>
    /// Accumulated damage threshold at which a growth attempt has a 95% chance of being blocked.
    /// The check uses damage measured before recovery and only runs when total damage exceeds 1.
    /// </summary>
    [DataField]
    public float GrowthBlock = 20.0f;

    /// <summary>
    /// Damage change applied after the growth chance roll succeeds, when total damage exceeds 1.
    /// Use negative amounts for healing; null disables recovery. Applied before the growth block roll,
    /// even if that roll subsequently prevents the stage from increasing.
    /// </summary>
    [DataField]
    public DamageSpecifier? DamageRecovery = null;

    /// <summary>
    /// Probability, from 0 to 1, of attempting recovery and growth on each half-second growth tick.
    /// A successful roll does not guarantee growth: damage can still block it.
    /// </summary>
    [DataField]
    public float GrowthTickChance = 1f;

    /// <summary>
    /// Number of visual variants provided for each growth stage. A variant is chosen at startup
    /// and reused as the stage changes; this does not control the number of growth stages.
    /// </summary>
    /// <remarks>
    /// The current selection uses this value as an exclusive upper bound, so the last variant
    /// is not selected when this value is greater than 1.
    /// </remarks>
    [DataField]
    public int SpriteVariants = 3;
}

namespace Content.Shared.Spreader;

/// <summary>
/// Sprite states for one visual variant of kudzu.
/// </summary>
[DataDefinition]
public sealed partial class KudzuVisualVariant
{
    /// <summary>
    /// Growth thresholds and their states. Must include a threshold of 1, with no duplicate thresholds.
    /// The state with the greatest threshold not exceeding current growth is displayed.
    /// </summary>
    [DataField(required: true)]
    public List<KudzuVisualStage> Stages = new();
}

namespace Content.Shared.Spreader;

/// <summary>
/// An explicitly named sprite state and the growth threshold at which it becomes visible.
/// </summary>
[DataDefinition]
public sealed partial class KudzuVisualStage
{
    /// <summary>
    /// First growth stage at which this state can be displayed. Must be at least 1.
    /// </summary>
    [DataField(required: true)]
    public int MinGrowth;

    /// <summary>
    /// State in the sprite's RSI. Its name has no required format.
    /// </summary>
    [DataField(required: true)]
    public string State = string.Empty;
}

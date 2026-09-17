namespace Content.Shared.Spreader;

/// <summary>
/// Visual variants of kudzu and the sprite states used at each growth threshold.
/// Shared so the server can select a variant from the same list used by the client.
/// </summary>
[RegisterComponent]
public sealed partial class KudzuVisualsComponent : Component
{
    /// <summary>
    /// Available visual variants. One is selected at startup and retained as growth changes.
    /// Must contain at least one variant.
    /// </summary>
    [DataField(required: true)]
    public List<KudzuVisualVariant> Variants = new();
}

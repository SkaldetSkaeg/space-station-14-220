using Robust.Shared.Serialization;

namespace Content.Shared.Spreader;

[Serializable, NetSerializable]
public enum KudzuVisuals : byte
{
    GrowthLevel,
    Variant
}

/// <summary>
/// Sprite layers controlled by the kudzu visualizer.
/// </summary>
[Serializable, NetSerializable]
public enum KudzuVisualLayers : byte
{
    Base,
}

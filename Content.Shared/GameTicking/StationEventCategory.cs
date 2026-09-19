using Robust.Shared.Serialization;

namespace Content.Shared.GameTicking;

/// <summary>
/// Subgroups of station events, independent of their current availability.
/// </summary>
[Serializable, NetSerializable]
public enum StationEventCategory : byte
{
    Effects,
    Antagonists,
    DerelictCyborgs,
    Creatures,
    CargoGifts,
    Meteors,
    Shuttles,
}

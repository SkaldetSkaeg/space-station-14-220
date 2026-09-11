using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Ghost;

[Serializable, NetSerializable]
public enum GhostHudUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public enum GhostHudType : byte
{
    Medical,
    Security,
}

[Serializable, NetSerializable]
public sealed class GhostHudBoundUserInterfaceState(bool medicalEnabled, bool securityEnabled) : BoundUserInterfaceState
{
    public bool MedicalEnabled { get; } = medicalEnabled;
    public bool SecurityEnabled { get; } = securityEnabled;
}

[Serializable, NetSerializable]
public sealed class GhostHudToggledMessage(GhostHudType hud, bool enabled) : BoundUserInterfaceMessage
{
    public GhostHudType Hud { get; } = hud;
    public bool Enabled { get; } = enabled;
}

using Content.Client.SS220.Ghost;
using Content.Shared.Overlays;

namespace Content.Client.Overlays;

// The GetStatusIconsEvent subscription is handled in JobStatusSystem
public sealed class ShowJobIconsSystem : GhostHudEquipmentSystem<ShowJobIconsComponent>; // SS220 ghost-hud-settings

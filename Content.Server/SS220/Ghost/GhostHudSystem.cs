using Content.Shared.Ghost;
using Content.Shared.Overlays;
using Content.Shared.SS220.Ghost;

namespace Content.Server.SS220.Ghost;

public sealed class GhostHudSystem : EntitySystem
{
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GhostComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<GhostComponent, GhostHudToggledMessage>(OnHudToggled);
    }

    private void OnUiOpened(Entity<GhostComponent> ent, ref BoundUIOpenedEvent args)
    {
        if (!args.UiKey.Equals(GhostHudUiKey.Key))
            return;

        UpdateUi(ent.Owner);
    }

    private void OnHudToggled(Entity<GhostComponent> ent, ref GhostHudToggledMessage args)
    {
        if (args.Actor != ent.Owner)
            return;

        switch (args.Hud)
        {
            case GhostHudType.Medical:
                SetMedicalHud(ent.Owner, args.Enabled);
                break;
            case GhostHudType.Security:
                SetSecurityHud(ent.Owner, args.Enabled);
                break;
            default:
                return;
        }

        UpdateUi(ent.Owner);
    }

    private void SetMedicalHud(EntityUid uid, bool enabled)
    {
        if (!enabled)
        {
            RemComp<ShowHealthBarsComponent>(uid);
            RemComp<ShowHealthIconsComponent>(uid);
            return;
        }

        var healthBars = EnsureComp<ShowHealthBarsComponent>(uid);
        healthBars.DamageContainers = ["Biological", "Ipc"];
        Dirty(uid, healthBars);

        var healthIcons = EnsureComp<ShowHealthIconsComponent>(uid);
        healthIcons.DamageContainers = ["Biological", "Ipc"];
        Dirty(uid, healthIcons);
    }

    private void SetSecurityHud(EntityUid uid, bool enabled)
    {
        if (!enabled)
        {
            RemComp<GhostHudOnOtherComponent>(uid);
            RemComp<ShowJobIconsComponent>(uid);
            RemComp<ShowMindShieldIconsComponent>(uid);
            RemComp<ShowCriminalRecordIconsComponent>(uid);
            return;
        }

        EnsureComp<GhostHudOnOtherComponent>(uid);
        EnsureComp<ShowJobIconsComponent>(uid);
        EnsureComp<ShowMindShieldIconsComponent>(uid);
        EnsureComp<ShowCriminalRecordIconsComponent>(uid);
    }

    private void UpdateUi(EntityUid uid)
    {
        var medicalEnabled = HasComp<ShowHealthBarsComponent>(uid) && HasComp<ShowHealthIconsComponent>(uid);
        var securityEnabled = HasComp<GhostHudOnOtherComponent>(uid);
        _ui.SetUiState(uid, GhostHudUiKey.Key, new GhostHudBoundUserInterfaceState(medicalEnabled, securityEnabled));
    }
}

using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking;
using Content.Shared.Antag.Components;
using Content.Shared.GameTicking.Rules.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminEventsSystem
{
    private AdminGameRuleCategory GetCategory(EntityPrototype prototype)
    {
        if (HasComp<StationEventComponent>(prototype))
            return AdminGameRuleCategory.Events;

        if (HasComp<BasicStationEventSchedulerComponent>(prototype)
            || HasComp<RampingStationEventSchedulerComponent>(prototype))
            return AdminGameRuleCategory.Schedulers;

        if (HasComp<SecretRuleComponent>(prototype)
            || HasComp<DynamicRuleComponent>(prototype)
            || HasComp<SubGamemodesComponent>(prototype))
            return AdminGameRuleCategory.RoundComposition;

        if (HasComp<StationVariationPassRuleComponent>(prototype)
            || HasComp<RoundstartStationVariationRuleComponent>(prototype))
            return AdminGameRuleCategory.StationVariations;

        if (HasComp<AntagSelectionComponent>(prototype) || HasComp<SurvivorRuleComponent>(prototype))
            return AdminGameRuleCategory.Roles;

        if (HasComp<SandboxRuleComponent>(prototype) || HasComp<DeathMatchRuleComponent>(prototype))
            return AdminGameRuleCategory.SpecialModes;

        if (HasComp<RespawnDeadRuleComponent>(prototype)
            || HasComp<InactivityRuleComponent>(prototype)
            || HasComp<MaxTimeRestartRuleComponent>(prototype))
            return AdminGameRuleCategory.RoundControl;

        return AdminGameRuleCategory.Other;
    }

    private StationEventCategory GetEventCategory(EntityPrototype prototype)
    {
        if (!prototype.TryComp<StationEventComponent>(out var stationEvent, EntityManager.ComponentFactory))
            return StationEventCategory.Effects;

        if (stationEvent.Category != null)
            return stationEvent.Category.Value;

        if (HasComp<CargoGiftsRuleComponent>(prototype))
            return StationEventCategory.CargoGifts;

        if (HasComp<MeteorSwarmComponent>(prototype) || HasComp<ImmovableRodRuleComponent>(prototype))
            return StationEventCategory.Meteors;

        if (HasComp<VentCrittersRuleComponent>(prototype) || HasComp<VentHordeRuleComponent>(prototype))
            return StationEventCategory.Creatures;

        if (HasComp<AntagSelectionComponent>(prototype))
            return StationEventCategory.Antagonists;

        if (HasComp<LoadMapRuleComponent>(prototype))
            return StationEventCategory.Shuttles;

        return StationEventCategory.Effects;
    }
}

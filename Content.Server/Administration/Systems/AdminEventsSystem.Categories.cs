using System.Linq;
using Content.Server.Antag.Components;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.Administration;
using Content.Shared.GameTicking.Rules;
using Content.Shared.GameTicking.Rules.Components;
using Content.Shared.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminEventsSystem
{
    private static readonly EntProtoId DerelictCyborgBase = "BaseDerelictCyborgSpawn";
    // These ghost-role events do not use AntagSelection.
    private static readonly EntProtoId RevenantEvent = "RevenantSpawn";
    private static readonly EntProtoId SkeletonEvent = "ClosetSkeleton";

    private static AdminGameRuleCategory GetCategory(EntityPrototype prototype)
    {
        if (prototype.HasComponent<StationEventComponent>())
            return AdminGameRuleCategory.Events;

        if (prototype.HasComponent<BasicStationEventSchedulerComponent>() || prototype.HasComponent<RampingStationEventSchedulerComponent>())
            return AdminGameRuleCategory.Schedulers;

        if (prototype.HasComponent<SecretRuleComponent>()
            || prototype.HasComponent<DynamicRuleComponent>()
            || prototype.HasComponent<SubGamemodesComponent>())
            return AdminGameRuleCategory.RoundComposition;

        if (prototype.HasComponent<StationVariationPassRuleComponent>() || prototype.HasComponent<RoundstartStationVariationRuleComponent>())
            return AdminGameRuleCategory.StationVariations;

        if (prototype.HasComponent<AntagSelectionComponent>() || prototype.HasComponent<SurvivorRuleComponent>())
            return AdminGameRuleCategory.Roles;

        if (prototype.HasComponent<SandboxRuleComponent>() || prototype.HasComponent<DeathMatchRuleComponent>())
            return AdminGameRuleCategory.SpecialModes;

        if (prototype.HasComponent<RespawnDeadRuleComponent>()
            || prototype.HasComponent<InactivityRuleComponent>()
            || prototype.HasComponent<MaxTimeRestartRuleComponent>())
            return AdminGameRuleCategory.RoundControl;

        return AdminGameRuleCategory.Other;
    }

    private AdminStationEventCategory GetEventCategory(EntityPrototype prototype)
    {
        if (!prototype.HasComponent<StationEventComponent>())
            return AdminStationEventCategory.Effects;

        if (prototype.HasComponent<CargoGiftsRuleComponent>())
            return AdminStationEventCategory.CargoGifts;

        if (prototype.HasComponent<MeteorSwarmComponent>() || prototype.HasComponent<ImmovableRodRuleComponent>())
            return AdminStationEventCategory.Meteors;

        if (prototype.HasComponent<VentCrittersRuleComponent>() || prototype.HasComponent<VentHordeRuleComponent>())
            return AdminStationEventCategory.Creatures;

        if (ProtoMan.EnumerateAllParents<EntityPrototype>(prototype.ID, true).Any(parent => parent.id == DerelictCyborgBase.Id))
            return AdminStationEventCategory.DerelictCyborgs;

        if (prototype.HasComponent<AntagSelectionComponent>() || prototype.ID == RevenantEvent.Id || prototype.ID == SkeletonEvent.Id)
            return AdminStationEventCategory.Antagonists;

        if (prototype.HasComponent<LoadMapRuleComponent>())
            return AdminStationEventCategory.Shuttles;

        return AdminStationEventCategory.Effects;
    }
}

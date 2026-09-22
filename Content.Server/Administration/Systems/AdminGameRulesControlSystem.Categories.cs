using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server.Administration.Systems;

public sealed partial class AdminGameRulesControlSystem
{
    private ProtoId<GameRuleCategoryPrototype> GetCategory(EntityPrototype prototype)
    {
        if (!prototype.TryComp<GameRuleComponent>(out var rule, EntityManager.ComponentFactory))
            return GameRuleCategoryPrototype.Default;

        return rule.Category;
    }

    private bool IsScheduler(EntityPrototype prototype)
    {
        return HasComp<BasicStationEventSchedulerComponent>(prototype)
            || HasComp<RampingStationEventSchedulerComponent>(prototype);
    }
}

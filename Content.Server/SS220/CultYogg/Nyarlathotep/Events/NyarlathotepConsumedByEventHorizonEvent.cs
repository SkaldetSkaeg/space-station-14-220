using Content.Server.SS220.CultYogg.Nyarlathotep.Components;
using Robust.Shared.Containers;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Events;

[ByRefEvent]
public readonly record struct NyarlathotepConsumedByEventHorizonEvent
    (EntityUid entity, EntityUid nyarlathotepHorizonUid, NyarlathotepHorizonComponent nyarlathotepHorizon, BaseContainer? container)
{
    public readonly EntityUid Entity = entity;

    public readonly EntityUid EventHorizonUid = nyarlathotepHorizonUid;

    public readonly NyarlathotepHorizonComponent NyarlathotepHorizon = nyarlathotepHorizon;

    public readonly BaseContainer? Container = container;
}

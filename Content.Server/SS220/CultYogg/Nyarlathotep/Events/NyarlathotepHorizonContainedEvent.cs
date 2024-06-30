using Robust.Shared.Containers;
using Content.Server.SS220.CultYogg.Nyarlathotep.Components;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Events;

public sealed class NyarlathotepHorizonContainedEvent : EntityEventArgs
{
    public readonly EntityUid Entity;

    public readonly NyarlathotepHorizonComponent NyarlathotepHorizon;

    public readonly EntGotInsertedIntoContainerMessage Args;

    public NyarlathotepHorizonContainedEvent(EntityUid entity, NyarlathotepHorizonComponent nyarlathotepHorizon, EntGotInsertedIntoContainerMessage args)
    {
        Entity = entity;
        NyarlathotepHorizon = nyarlathotepHorizon;
        Args = args;
    }
}

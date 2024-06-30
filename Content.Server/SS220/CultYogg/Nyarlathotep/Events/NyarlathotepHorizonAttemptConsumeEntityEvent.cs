using Content.Server.SS220.CultYogg.Nyarlathotep.Components;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Events;

[ByRefEvent]
public record struct NyarlathotepHorizonAttemptConsumeEntityEvent
    (EntityUid entity, EntityUid nyarlathotepHorizonUid, NyarlathotepHorizonComponent nyarlathotepHorizon)
{
    public readonly EntityUid Entity = entity;

    public readonly EntityUid NyarlathotepHorizonUid = nyarlathotepHorizonUid;

    public readonly NyarlathotepHorizonComponent NyarlathotepHorizon = nyarlathotepHorizon;

    public bool Cancelled = false;
}

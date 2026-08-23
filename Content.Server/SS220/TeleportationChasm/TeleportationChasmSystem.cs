// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.ActionBlocker;
using Content.Shared.SS220.Teleport;
using Content.Shared.SS220.TeleportationChasm;
using Robust.Shared.Timing;

namespace Content.Server.SS220.TeleportationChasm;

public sealed partial class TeleportationChasmSystem : SharedTeleportationChasmSystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ActionBlockerSystem _blocker = default!;

    private readonly List<Entity<TeleportationChasmFallingComponent>> _finishedFallingBuffer = [];

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeleportationChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasmFalling))
        {
            if (_timing.CurTime < chasmFalling.NextTeleportationTime)
                continue;

            if (chasmFalling.ShouldBeDeleted)
            {
                QueueDel(uid);
                continue;
            }

            _finishedFallingBuffer.Add((uid, chasmFalling));
        }

        foreach (var ent in _finishedFallingBuffer)
        {
            if (ent.Comp.Teleporter is { } teleporter)
            {
                var teleport = new TeleportTargetEvent(ent, ent);
                RaiseLocalEvent(teleporter, ref teleport);
            }

            RemComp<TeleportationChasmFallingComponent>(ent);
            _blocker.UpdateCanMove(ent);
        }

        _finishedFallingBuffer.Clear();
    }
}

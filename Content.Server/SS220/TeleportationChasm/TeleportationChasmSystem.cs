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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<TeleportationChasmFallingComponent>();
        while (query.MoveNext(out var uid, out var chasmFalling))
        {
            if (_timing.CurTime < chasmFalling.FallEndTime)
                continue;

            if (chasmFalling.DeleteInsteadOfTeleport)
            {
                QueueDel(uid);
                continue;
            }

            if (chasmFalling.Teleporter is { } teleporter)
            {
                var teleportEvent = new TeleportTargetEvent(uid, uid);
                RaiseLocalEvent(teleporter, ref teleportEvent);
            }

            RemCompDeferred<TeleportationChasmFallingComponent>(uid);
            _blocker.UpdateCanMove(uid);
        }
    }
}

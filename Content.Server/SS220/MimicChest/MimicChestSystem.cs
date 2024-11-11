using System.Linq;
using System.Numerics;
using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Physics;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.SS220.MimicChest;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.MimicChest;

public sealed class MimicChestSystem : EntitySystem
{
    [Dependency] private readonly PullingSystem _pulling = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorageSystem = default!;

    private const float PullDuration = 3f; // time for pulling to mimic

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimicChestComponent, ActivateInWorldEvent>(OnActivateMimic);
    }

    private void OnDeactivateMimic(EntityUid uid, MimicChestComponent chestComponent)
    {
        if(chestComponent.CapturedTarget != null)
            OnMoveEntity(chestComponent.CapturedTarget.Value, true);

        RemComp<JointVisualsComponent>(uid);

        if (TryComp<PullerComponent>(uid, out var pullerComponent))
        {
            pullerComponent.NeedsHands = true;
            _pulling.TogglePull(uid, pullerComponent);
            RemComp<PullerComponent>(uid);
        }

        chestComponent.CapturedTarget = null;
    }

    private void OnMoveEntity(EntityUid uid, bool canMove)
    {
        var moverComponent = EnsureComp<InputMoverComponent>(uid);

        if (canMove)
        {
            moverComponent.CanMove = true;
            return;
        }

        moverComponent.CanMove = false;
    }

    private void OnJointEntity(EntityUid user, EntityUid target)
    {
        var visuals = EnsureComp<JointVisualsComponent>(user);
        visuals.Sprite =
            new SpriteSpecifier.Rsi(new ResPath("Objects/Weapons/Guns/Launchers/grappling_gun.rsi"), "rope");
        visuals.OffsetA = Vector2.Zero;
        visuals.Target = GetNetEntity(target);

        Dirty(user, visuals);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var query = EntityQueryEnumerator<MimicChestComponent, TransformComponent>();
        var curTime = _timing.CurTime;


        while (query.MoveNext(out var uid, out var chestComponent, out var xform))
        {
            if (chestComponent.CapturedTarget == null)
                continue;

            if (!TryComp<TransformComponent>(chestComponent.CapturedTarget, out var targetTransform))
                continue;

            chestComponent.TimeStay += frameTime;

            var elapsedTime = (curTime - chestComponent.CaptureStartTime).TotalSeconds;
            var chestWorld = _transform.GetWorldPosition(uid);
            var targetWorld = _transform.GetWorldPosition(chestComponent.CapturedTarget.Value);
            var distance = (chestWorld - targetWorld).Length();

            if (distance == 0)
            {
                if (TryComp<EntityStorageComponent>(uid, out var storageComponent) && storageComponent.Open)
                {
                    _entityStorageSystem.CloseStorage(uid);
                }

                if (chestComponent.TimeStay >= 1f)
                {
                    if (TryComp<MobThresholdsComponent>(chestComponent.CapturedTarget, out var mobThresholdsComponent) &&
                        mobThresholdsComponent.CurrentThresholdState != MobState.Dead &&
                        TryComp<EntityStorageComponent>(uid, out var entityComp) &&
                        entityComp.Contents.ContainedEntities.Contains(chestComponent.CapturedTarget.Value))
                    {
                        var damage = new DamageSpecifier();
                        damage.DamageDict.Add("Blunt", 30);
                        _damageableSystem.TryChangeDamage(chestComponent.CapturedTarget.Value, damage);
                        chestComponent.TimeStay -= 1f;
                    }
                }

                if (TryComp<MobThresholdsComponent>(chestComponent.CapturedTarget, out var mobComponent) &&
                    mobComponent.CurrentThresholdState == MobState.Dead)
                {
                    _entityStorageSystem.OpenStorage(uid);
                    OnDeactivateMimic(uid, chestComponent);
                    chestComponent.CapturedTarget = null;
                }
            }
            else
            {
                var speed = (float)(distance / (PullDuration - elapsedTime));
                var direction = (chestWorld - targetWorld).Normalized();
                var newPosition = targetWorld + direction * speed * frameTime;
                _transform.SetWorldPosition(targetTransform.Owner, newPosition);
            }
        }
    }

    private void OnActivateMimic(EntityUid uid, MimicChestComponent comp, ActivateInWorldEvent args)
    {
        if (comp.IsBusy)
            return;

        if (TryComp<EntityStorageComponent>(uid, out var storageComponent) && storageComponent.Open)
            return;

        if (HasComp<CultYoggComponent>(args.User))
            return;

        if (HasComp<MiGoComponent>(args.User))
            return;

        var puller = EnsureComp<PullerComponent>(args.Target);
        puller.NeedsHands = false;

        var pullable = EnsureComp<PullableComponent>(args.User);

        var isPull = _pulling.TryStartPull(args.Target, args.User, puller, pullable);

        OnMoveEntity(args.User, false);
        OnJointEntity(args.Target, args.User);

        if (isPull)
        {
            comp.IsBusy = true;
            comp.CaptureStartTime = _timing.CurTime;
            comp.CapturedTarget = args.User;
        }
    }
}

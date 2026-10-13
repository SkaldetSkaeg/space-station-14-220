// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.Forensics.Components;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.SS220.Forensics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Records individual glove contact traces independently of fingerprints and fibers.
/// </summary>
public sealed class GloveMicroFiberSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GloveMicroFiberComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InventoryComponent, ContactInteractionEvent>(OnContact);
    }

    private void OnMapInit(Entity<GloveMicroFiberComponent> ent, ref MapInitEvent args)
    {
        EnsureCode(ent.Comp);
    }

    private string EnsureCode(GloveMicroFiberComponent component)
    {
        if (component.Code != null)
            return component.Code;

        var bytes = new byte[16];
        _random.NextBytes(bytes);
        component.Code = Convert.ToHexString(bytes);
        return component.Code;
    }

    /// <summary>
    /// Obtains the permanent identity of gloves that leave identifiable contact traces.
    /// </summary>
    public bool TryGetCode(EntityUid uid, out string? code)
    {
        code = null;
        if (!TryComp<GloveMicroFiberComponent>(uid, out var component))
            return false;

        if (!component.Enabled)
            return false;

        code = EnsureCode(component);
        return true;
    }

    private void OnContact(Entity<InventoryComponent> ent, ref ContactInteractionEvent args)
    {
        if (!HasComp<HandsComponent>(ent))
            return;

        if (HasComp<IgnoresFingerprintsComponent>(args.Other))
            return;

        if (!_inventory.TryGetSlotEntity(ent, "gloves", out var gloves))
            return;

        if (!TryGetCode(gloves.Value, out var code))
            return;

        if (code == null)
            return;

        var prototype = MetaData(gloves.Value).EntityPrototype;
        EntProtoId? prototypeId = prototype == null ? (EntProtoId?)null : new EntProtoId(prototype.ID);
        EnsureComp<ForensicsComponent>(args.Other).GloveMicroFibers.Add(new GloveMicroFiberTrace(code, prototypeId));
    }

    /// <summary>
    /// Retains the identity when cutting gloves into another pair of gloves.
    /// Does not turn cloth or other butchering products into sources of glove microfibers.
    /// </summary>
    public void TransferCode(EntityUid source, EntityUid target)
    {
        if (!TryComp<GloveMicroFiberComponent>(source, out var sourceProfile))
            return;

        if (!TryComp<GloveMicroFiberComponent>(target, out var targetProfile))
            return;

        targetProfile.Code = EnsureCode(sourceProfile);
    }
}

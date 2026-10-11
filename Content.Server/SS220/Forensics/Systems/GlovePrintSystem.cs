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
public sealed class GlovePrintSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GlovePrintComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<InventoryComponent, ContactInteractionEvent>(OnContact);
    }

    private void OnMapInit(Entity<GlovePrintComponent> ent, ref MapInitEvent args)
    {
        EnsurePrint(ent.Comp);
    }

    private string EnsurePrint(GlovePrintComponent component)
    {
        if (component.Print != null)
            return component.Print;

        var bytes = new byte[16];
        _random.NextBytes(bytes);
        component.Print = Convert.ToHexString(bytes);
        return component.Print;
    }

    /// <summary>
    /// Obtains the permanent identity of gloves that leave identifiable contact traces.
    /// </summary>
    public bool TryGetPrint(EntityUid uid, out string? print)
    {
        print = null;
        if (!TryComp<GlovePrintComponent>(uid, out var component))
            return false;

        if (!component.Enabled)
            return false;

        print = EnsurePrint(component);
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

        if (!TryGetPrint(gloves.Value, out var print) || print == null)
            return;

        var prototype = MetaData(gloves.Value).EntityPrototype;
        EntProtoId? prototypeId = prototype == null ? (EntProtoId?)null : new EntProtoId(prototype.ID);
        EnsureComp<ForensicsComponent>(args.Other).GlovePrints.Add(new GlovePrintTrace(print, prototypeId));
    }

    /// <summary>
    /// Retains the identity when cutting gloves into another pair of gloves.
    /// Does not turn cloth or other butchering products into sources of glove prints.
    /// </summary>
    public void TransferPrint(EntityUid source, EntityUid target)
    {
        if (!TryComp<GlovePrintComponent>(source, out var sourcePrint))
            return;

        if (!TryComp<GlovePrintComponent>(target, out var targetPrint))
            return;

        targetPrint.Print = EnsurePrint(sourcePrint);
    }
}

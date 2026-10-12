// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.SS220.Forensics.Components;
using Content.Shared.SS220.Forensics;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Records individual material profiles transferred from clothing and storage onto contained items.
/// </summary>
public sealed class MicroFiberSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MicroFiberComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<MicroFiberComponent, EntInsertedIntoContainerMessage>(OnEntInserted);
    }

    private void OnMapInit(Entity<MicroFiberComponent> ent, ref MapInitEvent args)
    {
        EnsurePrint(ent.Comp);
    }

    private string EnsurePrint(MicroFiberComponent component)
    {
        if (component.Print != null)
            return component.Print;

        var bytes = new byte[16];
        _random.NextBytes(bytes);
        component.Print = Convert.ToHexString(bytes);
        return component.Print;
    }

    /// <summary>
    /// Obtains an item's permanent material identity and a snapshot of its appearance and display name.
    /// </summary>
    public bool TryGetTrace(Entity<MicroFiberComponent?> ent, out MicroFiberTrace trace)
    {
        trace = default;
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (TryComp<GlovePrintComponent>(ent, out var gloves) && !gloves.Enabled)
            return false;

        var metadata = MetaData(ent);
        var prototype = metadata.EntityPrototype;
        EntProtoId? prototypeId = prototype == null ? (EntProtoId?)null : new EntProtoId(prototype.ID);
        trace = new MicroFiberTrace(EnsurePrint(ent.Comp), prototypeId, metadata.EntityName);
        return true;
    }

    private void OnEntInserted(Entity<MicroFiberComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        if (!TryGetTrace((ent.Owner, ent.Comp), out var trace))
            return;

        var forensics = EnsureComp<ForensicsComponent>(args.Entity);
        AddTrace((args.Entity, forensics), trace);
    }

    /// <summary>
    /// Stores the latest deposited appearance for each source code without duplicating a renamed item.
    /// </summary>
    public void AddTrace(Entity<ForensicsComponent?> ent, MicroFiberTrace trace)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        // Keeping an identical snapshot also makes copying evidence onto itself safe to enumerate.
        if (ent.Comp.MicroFibers.Contains(trace))
            return;

        ent.Comp.MicroFibers.RemoveWhere(existing => StringComparer.Ordinal.Equals(existing.Code, trace.Code));
        ent.Comp.MicroFibers.Add(trace);
    }
}

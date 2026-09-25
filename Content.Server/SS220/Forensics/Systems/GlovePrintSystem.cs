// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Forensics;
using Content.Server.SS220.Forensics.Components;
using Robust.Shared.Random;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Assigns persistent glove identities and includes them in forensic fiber samples.
/// </summary>
public sealed class GlovePrintSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GlovePrintComponent, MapInitEvent>(OnMapInit);
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
    /// Formats the same sample for contact evidence, cleaning residue and forensic pads.
    /// Items without a glove identity retain their ordinary fiber description.
    /// </summary>
    public string GetFiberSample(Entity<FiberComponent> ent)
    {
        var fibers = string.IsNullOrEmpty(ent.Comp.FiberColor)
            ? Loc.GetString("forensic-fibers", ("material", ent.Comp.FiberMaterial))
            : Loc.GetString("forensic-fibers-colored", ("color", ent.Comp.FiberColor), ("material", ent.Comp.FiberMaterial));

        if (!TryComp<GlovePrintComponent>(ent, out var print))
            return fibers;

        return Loc.GetString("forensic-fibers-glove-print", ("fibers", fibers), ("print", EnsurePrint(print)));
    }

    /// <summary>
    /// Records the visible fibers and, for gloves, the identity used for matching samples.
    /// </summary>
    public void AddFiberEvidence(Entity<ForensicsComponent> target, Entity<FiberComponent> source)
    {
        target.Comp.Fibers.Add(GetFiberSample(source));
        if (!TryComp<GlovePrintComponent>(source, out var print))
            return;

        target.Comp.GlovePrints.Add(EnsurePrint(print));
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

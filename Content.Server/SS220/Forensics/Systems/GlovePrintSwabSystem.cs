// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.Forensics.Components;
using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Server.SS220.Forensics.Systems;

/// <summary>
/// Describes a physical sample produced by the glove-trace scanner.
/// </summary>
public sealed class GlovePrintSwabSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GlovePrintSwabComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<GlovePrintSwabComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushText(Loc.GetString(ent.Comp.Used ? "glove-print-swab-used" : "glove-print-swab-unused"));
        if (!ent.Comp.Used || string.IsNullOrEmpty(ent.Comp.SourceName))
            return;

        args.PushText(Loc.GetString("glove-print-swab-source", ("source", FormattedMessage.EscapeText(ent.Comp.SourceName))));
    }
}

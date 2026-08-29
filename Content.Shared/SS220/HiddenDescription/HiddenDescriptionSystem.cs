// Original code by Corvax dev team, no specific for SS220 license

using Content.Shared.Examine;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.HiddenDescription;

public abstract partial class SharedHiddenDescriptionSystem : EntitySystem
{
    private static readonly ProtoId<KnowledgePrototype> CultYoggKnowledge = "CultYoggKnowledge";

    [Dependency] private ExperienceSystem _experience = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HiddenDescriptionComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<HiddenDescriptionComponent> entity, ref ExaminedEvent args)
    {
        PushExamineInformation(entity.Comp, ref args);

        Dirty(entity);
    }

    public void PushExamineInformation(HiddenDescriptionComponent component, ref ExaminedEvent args)
    {
        TryComp<ExperienceComponent>(args.Examiner, out var experience);

        foreach (var (knowledge, locIds) in component.Entries)
        {
            var hasKnowledge = experience != null &&
                               _experience.HaveKnowledge((args.Examiner, experience), knowledge);
            var isCultYoggCreature = knowledge == CultYoggKnowledge &&
                                     HasComp<ShowCultYoggIconsComponent>(args.Examiner);

            if (!hasKnowledge && !isCultYoggCreature)
                continue;

            foreach (var locId in locIds)
            {
                args.PushMarkup(Loc.GetString(locId), component.PushPriority);
            }
        }
    }

}

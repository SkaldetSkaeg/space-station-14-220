// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Content.Server.SS220.CultYogg.Rave;
using Content.Server.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Server.SS220.CultYogg;
using Content.Shared.Humanoid;

namespace Content.Server.SS220.EntityEffects.Effects
{
    /// <summary>
    /// Used when someone eats MiGoShroom
    /// </summary>
    [UsedImplicitly]
    public sealed partial class ChemMiGomicelium : EntityEffect
    {
        [DataField]
        public float Time = 2.0f;

        public override void Effect(EntityEffectBaseArgs args)
        {
            var time = Time;
            var entityManager = args.EntityManager;

            if (args is EntityEffectReagentArgs reagentArgs)
            {
                time *= reagentArgs.Scale.Float();
                if (entityManager.TryGetComponent<CultYoggComponent>(args.TargetEntity, out var comp))
                {
                    comp.ConsumedAscensionReagent += reagentArgs.Quantity.Float();
                    entityManager.System<CultYoggSystem>().TryStartAscensionByReagent(args.TargetEntity, comp);
                    return;
                }
            }

            if (!entityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity)) //if its an animal -- corrupt it
            {
                entityManager.System<CultYoggAnimalCorruptionSystem>().AnimalCorruption(args.TargetEntity);
            }

            if (entityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity))
            {
                entityManager.System<RaveSystem>().TryApplyRavenness(args.TargetEntity, TimeSpan.FromSeconds(time));
            }
        }
        //ToDo check the guidebook
        protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys) => Loc.GetString("reagent-effect-guidebook-ss220-corrupt-mind", ("chance", Probability));
    }
}

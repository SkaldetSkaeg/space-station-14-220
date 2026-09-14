using Content.Shared.Spreader;
using Robust.Client.GameObjects;

namespace Content.Client.Kudzu;

public sealed partial class KudzuVisualsSystem : VisualizerSystem<KudzuVisualsComponent>
{
    protected override void OnAppearanceChange(EntityUid uid, KudzuVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, KudzuVisuals.Variant, out var variant, args.Component))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, KudzuVisuals.GrowthLevel, out var level, args.Component))
            return;

        if (variant < 0 || variant >= component.Variants.Count)
            return;

        KudzuVisualStage? selected = null;
        foreach (var stage in component.Variants[variant].Stages)
        {
            if (stage.MinGrowth > level)
                continue;

            if (selected != null && stage.MinGrowth <= selected.MinGrowth)
                continue;

            selected = stage;
        }

        if (selected == null)
            return;

        SpriteSystem.LayerSetRsiState((uid, args.Sprite), KudzuVisualLayers.Base, selected.State);
    }
}

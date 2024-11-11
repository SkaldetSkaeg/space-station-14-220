using Content.Client.Storage.Visualizers;
using Content.Shared.SS220.MimicChest;
using Robust.Client.GameObjects;

namespace Content.Client.SS220.MimicChest;

public sealed class MimicChestSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem AppearanceSystem = default!;
    [Dependency] private readonly EntityStorageVisualizerSystem _entity = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MimicChestComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<MimicChestComponent> ent, ref ComponentInit args)
    {
        // here sprite changes
    }

}

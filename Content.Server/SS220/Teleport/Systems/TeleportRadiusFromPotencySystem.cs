// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Botany.Components;
using Content.Server.Botany.Systems;
using Content.Server.SS220.Teleport.Components;
using Content.Shared.SS220.Teleport;

namespace Content.Server.SS220.Teleport.Systems;

/// <summary>
/// Applies produce potency to the radius supplied by a teleport destination provider.
/// </summary>
public sealed partial class TeleportRadiusFromPotencySystem : EntitySystem
{
    [Dependency] private readonly BotanySystem _botany = default!;

    private const float ReferencePotency = 10f;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TeleportRadiusFromPotencyComponent, TryModifyRadiusEvent>(OnTryModifyRadius);
    }

    private void OnTryModifyRadius(Entity<TeleportRadiusFromPotencyComponent> ent, ref TryModifyRadiusEvent args)
    {
        if (!TryComp<ProduceComponent>(ent, out var produce))
            return;

        if (!_botany.TryGetSeed(produce, out var seed))
            return;

        args.Radius *= seed.Potency / ReferencePotency;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using Content.Client.Guidebook.Richtext;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Guidebook.Controls;

/// <summary>
/// Displays an RSI icon in a guide without spawning an entity.
/// </summary>
[UsedImplicitly]
public sealed class GuideSpriteEmbed : TextureRect, IDocumentTag
{
    [Dependency] private readonly IEntitySystemManager _systems = default!;

    public GuideSpriteEmbed()
    {
        IoCManager.InjectDependencies(this);
        Stretch = StretchMode.KeepCentered;
        Margin = new Thickness(8);
    }

    public bool TryParseTag(Dictionary<string, string> args, [NotNullWhen(true)] out Control? control)
    {
        control = null;
        if (!args.TryGetValue("Sprite", out var sprite) || !args.TryGetValue("State", out var state))
            return false;

        var scale = 2f;
        if (args.TryGetValue("Scale", out var value) &&
            (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out scale) ||
             !float.IsFinite(scale) || scale <= 0))
            return false;

        Texture = _systems.GetEntitySystem<SpriteSystem>().Frame0(new SpriteSpecifier.Rsi(new ResPath(sprite), state));
        TextureScale = new Vector2(scale);
        control = this;
        return true;
    }
}

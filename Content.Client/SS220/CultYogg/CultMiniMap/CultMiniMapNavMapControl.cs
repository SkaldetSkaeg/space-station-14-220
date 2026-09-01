// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Numerics;
using Content.Client.Pinpointer.UI;
using Content.Shared.SS220.CultYogg.CultMiniMap;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Client.SS220.CultYogg.CultMiniMap;

public sealed partial class CultMiniMapNavMapControl : NavMapControl
{
    public NetEntity? Focus;
    public Dictionary<NetEntity, string> LocalizedNames = new();
    public Dictionary<NetEntity, CultMiniMapStructureBlip> StructureMarkers = new();
    public Dictionary<uint, CultMiniMapPingBlip> Pings = new();
    public bool PingMode;
    public event Action<EntityCoordinates>? PingRequestedAction;

    private Label _trackedEntityLabel;
    private PanelContainer _trackedEntityPanel;

    public CultMiniMapNavMapControl() : base()
    {
        WallColor = new Color(192, 122, 196);
        TileColor = new(71, 42, 72);
        BackgroundColor = Color.FromSrgb(TileColor.WithAlpha(BackgroundOpacity));

        _trackedEntityLabel = new Label
        {
            Margin = new Thickness(10f, 8f),
            HorizontalAlignment = HAlignment.Center,
            VerticalAlignment = VAlignment.Center,
            Modulate = Color.White,
        };

        _trackedEntityPanel = new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = BackgroundColor,
            },

            Margin = new Thickness(5f, 10f),
            HorizontalAlignment = HAlignment.Left,
            VerticalAlignment = VAlignment.Bottom,
            Visible = false,
        };

        _trackedEntityPanel.AddChild(_trackedEntityLabel);
        AddChild(_trackedEntityPanel);
        PostWallDrawingAction += DrawOverlays;
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (!PingMode || args.Function != EngineKeyFunctions.UIClick)
        {
            base.KeyBindUp(args);
            return;
        }

        var dragDistance = (StartDragPosition - args.PointerLocation.Position).Length();
        if (dragDistance > MinDragDistance || MapUid is not { } grid)
        {
            base.KeyBindUp(args);
            return;
        }

        // InverseMapPosition accounts for zoom and user panning; NavMap's offset also includes
        // the grid's physics center, which must be restored for grid-local coordinates.
        var position = InverseMapPosition(args.RelativePixelPosition) + GetOffset() - Offset;
        PingMode = false;
        PingRequestedAction?.Invoke(new EntityCoordinates(grid, position));
        args.Handle();
    }

    private void DrawOverlays(DrawingHandleScreen handle)
    {
        DrawStructures(handle);
        DrawPings(handle);
    }

    private void DrawStructures(DrawingHandleScreen handle)
    {
        var wallPositions = new HashSet<Vector2>();
        foreach (var structure in StructureMarkers.Values)
        {
            if (structure.MarkerType is CultMiniMapMarkerType.Wall or CultMiniMapMarkerType.SecretDoor)
                wallPositions.Add(structure.Coordinates.Position);
        }

        foreach (var structure in StructureMarkers.Values)
        {
            switch (structure.MarkerType)
            {
                case CultMiniMapMarkerType.Wall:
                    DrawWall(handle, structure, wallPositions);
                    break;
                case CultMiniMapMarkerType.SecretDoor:
                    DrawSecretDoor(handle, structure, wallPositions);
                    break;
                case CultMiniMapMarkerType.Airlock:
                    DrawAirlock(handle, structure);
                    break;
            }
        }
    }

    private void DrawWall(
        DrawingHandleScreen handle,
        CultMiniMapStructureBlip structure,
        HashSet<Vector2> wallPositions)
    {
        var topLeft = StructurePoint(structure, new Vector2(-0.5f, 0.5f));
        var topRight = StructurePoint(structure, new Vector2(0.5f, 0.5f));
        var bottomRight = StructurePoint(structure, new Vector2(0.5f, -0.5f));
        var bottomLeft = StructurePoint(structure, new Vector2(-0.5f, -0.5f));

        var position = structure.Coordinates.Position;
        if (!wallPositions.Contains(position + Vector2.UnitY))
            handle.DrawLine(topLeft, topRight, structure.Color);
        if (!wallPositions.Contains(position + Vector2.UnitX))
            handle.DrawLine(topRight, bottomRight, structure.Color);
        if (!wallPositions.Contains(position - Vector2.UnitY))
            handle.DrawLine(bottomRight, bottomLeft, structure.Color);
        if (!wallPositions.Contains(position - Vector2.UnitX))
            handle.DrawLine(bottomLeft, topLeft, structure.Color);

        handle.DrawLine(topLeft, bottomRight, structure.Color);
    }

    private void DrawSecretDoor(
        DrawingHandleScreen handle,
        CultMiniMapStructureBlip structure,
        HashSet<Vector2> wallPositions)
    {
        DrawWall(handle, structure, wallPositions);
        handle.DrawLine(
            StructurePoint(structure, new Vector2(0.5f, 0.5f)),
            StructurePoint(structure, new Vector2(-0.5f, -0.5f)),
            structure.Color);
    }

    private void DrawAirlock(DrawingHandleScreen handle, CultMiniMapStructureBlip structure)
    {
        var extent = 0.5f - FullWallInstep;
        var topLeft = StructurePoint(structure, new Vector2(-extent, extent));
        var topRight = StructurePoint(structure, new Vector2(extent, extent));
        var bottomRight = StructurePoint(structure, new Vector2(extent, -extent));
        var bottomLeft = StructurePoint(structure, new Vector2(-extent, -extent));

        DrawRectangle(handle, topLeft, topRight, bottomRight, bottomLeft, structure.Color);
        handle.DrawLine(
            StructurePoint(structure, new Vector2(0f, extent)),
            StructurePoint(structure, new Vector2(0f, -extent)),
            structure.Color);
    }

    private static void DrawRectangle(
        DrawingHandleScreen handle,
        Vector2 topLeft,
        Vector2 topRight,
        Vector2 bottomRight,
        Vector2 bottomLeft,
        Color color)
    {
        handle.DrawLine(topLeft, topRight, color);
        handle.DrawLine(topRight, bottomRight, color);
        handle.DrawLine(bottomRight, bottomLeft, color);
        handle.DrawLine(bottomLeft, topLeft, color);
    }

    private Vector2 StructurePoint(CultMiniMapStructureBlip structure, Vector2 offset)
    {
        var rotated = new Angle(structure.Rotation).RotateVec(offset);
        var local = structure.Coordinates.Position + rotated - GetOffset();
        return ScalePosition(new Vector2(local.X, -local.Y));
    }

    private void DrawPings(DrawingHandleScreen handle)
    {
        var seconds = (float) Timing.RealTime.TotalSeconds;
        foreach (var ping in Pings.Values)
        {
            var local = ping.Coordinates.Position - GetOffset();
            var position = ScalePosition(new Vector2(local.X, -local.Y));
            var phase = (seconds * 0.85f + ping.Id * 0.173f) % 1f;
            DrawPingRing(handle, position, ping.Color, phase);
            DrawPingRing(handle, position, ping.Color, (phase + 0.5f) % 1f);

            var heartbeat = 1f + 0.08f * MathF.Sin(seconds * MathF.Tau * 2f + ping.Id);
            var coefficient = MinmapScaleModifier * MathF.Sqrt(MinimapScale) * ping.Scale * heartbeat;
            var extent = new Vector2(coefficient * ping.Texture.Width, coefficient * ping.Texture.Height);
            handle.DrawTextureRect(ping.Texture, new UIBox2(position - extent, position + extent), ping.Color);
        }
    }

    private static void DrawPingRing(DrawingHandleScreen handle, Vector2 position, Color color, float phase)
    {
        var radius = 6f + phase * 24f;
        var alpha = 0.85f * (1f - phase);
        handle.DrawCircle(position, radius, color.WithAlpha(alpha), false);
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (Focus == null)
        {
            _trackedEntityLabel.Text = string.Empty;
            _trackedEntityPanel.Visible = false;

            return;
        }

        foreach ((var netEntity, var blip) in TrackedEntities)
        {
            if (netEntity != Focus)
                continue;

            if (!LocalizedNames.TryGetValue(netEntity, out var name))
                name = Loc.GetString("navmap-unknown-entity");

            var message = name + "\n" + Loc.GetString("navmap-location",
                ("x", MathF.Round(blip.Coordinates.X)),
                ("y", MathF.Round(blip.Coordinates.Y)));

            _trackedEntityLabel.Text = message;
            _trackedEntityPanel.Visible = true;

            return;
        }

        _trackedEntityLabel.Text = string.Empty;
        _trackedEntityPanel.Visible = false;
    }
}

public readonly record struct CultMiniMapPingBlip(
    uint Id,
    EntityCoordinates Coordinates,
    Texture Texture,
    Color Color,
    float Scale);

public readonly record struct CultMiniMapStructureBlip(
    EntityCoordinates Coordinates,
    CultMiniMapMarkerType MarkerType,
    Color Color,
    float Rotation);

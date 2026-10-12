// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Stylesheets;
using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays the held scanner's current mode and allows choosing either collection mode.
/// </summary>
public sealed class GlovePrintScannerStatusControl : Control
{
    private readonly Entity<GlovePrintScannerComponent> _scanner;
    private readonly IEntityManager _entityManager;
    private GlovePrintScannerMode _displayedMode;

    /// <summary>
    /// Selects between deposited traces and the scanned gloves' own pair code.
    /// </summary>
    public readonly OptionButton ModeSelector;

    public GlovePrintScannerStatusControl(Entity<GlovePrintScannerComponent> scanner, IEntityManager entityManager)
    {
        _scanner = scanner;
        _entityManager = entityManager;

        var contents = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        contents.AddChild(new Label
        {
            Text = Loc.GetString("glove-print-scanner-mode-label"),
            StyleClasses = { StyleClass.ItemStatus },
        });

        ModeSelector = new OptionButton
        {
            HorizontalExpand = true,
            MinHeight = 22,
            ToolTip = Loc.GetString("glove-print-scanner-mode-tooltip"),
        };
        ModeSelector.AddItem(Loc.GetString("glove-print-scanner-mode-surface-short"),
            (int)GlovePrintScannerMode.SurfaceTraces);
        ModeSelector.AddItem(Loc.GetString("glove-print-scanner-mode-own-short"),
            (int)GlovePrintScannerMode.OwnPrint);
        ModeSelector.OnItemSelected += OnModeSelected;
        contents.AddChild(ModeSelector);
        AddChild(contents);
        UpdateMode();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_scanner.Comp.LifeStage > ComponentLifeStage.Running)
        {
            ModeSelector.Disabled = true;
            return;
        }

        if (_displayedMode == _scanner.Comp.Mode)
            return;

        UpdateMode();
    }

    private void UpdateMode()
    {
        _displayedMode = _scanner.Comp.Mode;
        ModeSelector.SelectId((int)_displayedMode);
    }

    private void OnModeSelected(OptionButton.ItemSelectedEventArgs args)
    {
        if (_scanner.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (args.Id != (int)GlovePrintScannerMode.SurfaceTraces && args.Id != (int)GlovePrintScannerMode.OwnPrint)
            return;

        _entityManager.RaisePredictiveEvent(new GlovePrintScannerSetModeEvent(
            _entityManager.GetNetEntity(_scanner.Owner), (GlovePrintScannerMode)args.Id));
    }
}

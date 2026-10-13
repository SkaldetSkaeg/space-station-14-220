// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Stylesheets;
using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Timing;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays the held scanner's current collection mode.
/// </summary>
public sealed class GlovePrintScannerStatusControl : Control
{
    private readonly Entity<GlovePrintScannerComponent> _scanner;
    private GlovePrintScannerMode _displayedMode;

    /// <summary>
    /// Shows the selected collection mode in the held item's status panel.
    /// </summary>
    public readonly Label ModeLabel;

    public GlovePrintScannerStatusControl(Entity<GlovePrintScannerComponent> scanner)
    {
        _scanner = scanner;
        ToolTip = Loc.GetString("glove-print-scanner-mode-tooltip") + "\n" +
            Loc.GetString("glove-print-scanner-switch-hint");
        MouseFilter = MouseFilterMode.Stop;

        var contents = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        contents.AddChild(new Label
        {
            Text = Loc.GetString("glove-print-scanner-mode-label"),
            StyleClasses = { StyleClass.ItemStatus },
        });

        ModeLabel = new Label
        {
            StyleClasses = { StyleClass.ItemStatus },
        };
        contents.AddChild(ModeLabel);
        AddChild(contents);
        UpdateMode();
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_scanner.Comp.LifeStage > ComponentLifeStage.Running)
            return;

        if (_displayedMode == _scanner.Comp.Mode)
            return;

        UpdateMode();
    }

    private void UpdateMode()
    {
        _displayedMode = _scanner.Comp.Mode;
        ModeLabel.Text = Loc.GetString(_displayedMode == GlovePrintScannerMode.SurfaceTraces
            ? "glove-print-scanner-mode-surface-short"
            : "glove-print-scanner-mode-own-short");
    }
}

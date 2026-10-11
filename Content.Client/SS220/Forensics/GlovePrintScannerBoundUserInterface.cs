// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays private scan results sent to users of the glove-print scanner.
/// </summary>
public sealed class GlovePrintScannerBoundUserInterface : BoundUserInterface
{
    private GlovePrintScannerMenu? _window;

    public GlovePrintScannerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GlovePrintScannerMenu>();
        _window.Clear.OnPressed += _ => SendMessage(new GlovePrintScannerClearMessage());
        SendMessage(new GlovePrintScannerRequestResultsMessage());
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (_window == null)
            return;

        if (message is not GlovePrintScannerResultsMessage results)
            return;

        _window.UpdateResults(results);
    }
}

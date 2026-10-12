// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays private results from the swab physically inserted into the analyzer.
/// </summary>
public sealed class GlovePrintAnalyzerBoundUserInterface : BoundUserInterface
{
    private GlovePrintAnalyzerMenu? _window;

    public GlovePrintAnalyzerBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<GlovePrintAnalyzerMenu>();
        _window.Eject.OnPressed += _ => SendMessage(new GlovePrintAnalyzerEjectMessage());
        _window.Print.OnPressed += _ => Print();
        SendMessage(new GlovePrintAnalyzerRequestResultsMessage());
    }

    private void Print()
    {
        if (_window == null)
            return;

        _window.StartPrintCooldown();
        SendMessage(new GlovePrintAnalyzerPrintMessage());
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (_window == null)
            return;

        if (message is not GlovePrintAnalyzerResultsMessage results)
            return;

        _window.UpdateResults(results);
    }
}

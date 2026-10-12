// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface;

namespace Content.Client.SS220.Forensics;

/// <summary>
/// Displays private analysis progress and stored samples from the analyzer.
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
        _window.Analyze.OnPressed += _ => Analyze();
        _window.SaveDescription.OnPressed += _ => SaveDescription();
        SendMessage(new GlovePrintAnalyzerRequestResultsMessage());
    }

    private void Analyze()
    {
        if (_window == null || _window.Analyze.Disabled || _window.SelectedSampleId != null)
            return;

        SendMessage(new GlovePrintAnalyzerAnalyzeMessage(_window.DescriptionEdit.Text, _window.PendingSwab));
    }

    private void SaveDescription()
    {
        if (_window == null || _window.SaveDescription.Disabled)
            return;

        var swab = _window.SelectedSampleId == null ? _window.PendingSwab : null;
        SendMessage(new GlovePrintAnalyzerSetDescriptionMessage(
            _window.SelectedSampleId, _window.DescriptionEdit.Text, swab));
    }

    private void Print()
    {
        if (_window == null || _window.Print.Disabled || _window.SelectedSampleId == null)
            return;

        _window.StartPrintCooldown();
        SendMessage(new GlovePrintAnalyzerPrintMessage(_window.SelectedSampleId));
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

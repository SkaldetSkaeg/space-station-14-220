// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Forensics;

public sealed partial class GloveMicroFiberAnalyzerMenu
{
    private int? _comparisonFirstId;
    private int? _comparisonSecondId;
    private bool _comparisonHasResult;

    /// <summary>
    /// The first archive record selected for comparison.
    /// </summary>
    public int? ComparisonFirstSampleId => _comparisonFirstId;

    /// <summary>
    /// The second archive record selected for comparison.
    /// </summary>
    public int? ComparisonSecondSampleId => _comparisonSecondId;

    private void InitializeComparison()
    {
        CompareHistory.ToolTip = Loc.GetString("glove-micro-fiber-analyzer-interface-compare-not-enough");
        CompareHistory.OnPressed += _ => RefreshResults();
        ComparisonFirst.OnItemSelected += args => SelectComparisonSample(args.Id, true);
        ComparisonSecond.OnItemSelected += args => SelectComparisonSample(args.Id, false);
        ComparisonRun.OnPressed += _ => RunComparison();
        UpdateComparisonView();
    }

    private void UpdateComparisonHistory(List<GloveMicroFiberAnalyzerHistoryEntry> history)
    {
        var previousFirst = _comparisonFirstId;
        var previousSecond = _comparisonSecondId;
        ComparisonFirst.Clear();
        ComparisonSecond.Clear();
        ComparisonFirst.Disabled = history.Count == 0;
        ComparisonSecond.Disabled = history.Count == 0;
        CompareHistory.Disabled = history.Count < 2;
        CompareHistory.ToolTip = CompareHistory.Disabled
            ? Loc.GetString("glove-micro-fiber-analyzer-interface-compare-not-enough")
            : null;
        if (CompareHistory.Disabled)
            CompareHistory.Pressed = false;

        for (var i = history.Count - 1; i >= 0; i--)
        {
            var sample = history[i];
            var sampleName = GetSampleName(sample);
            ComparisonFirst.AddItem(sampleName, sample.Id);
            ComparisonSecond.AddItem(sampleName, sample.Id);
        }

        RestoreComparisonSelection(history);

        if (_comparisonFirstId != null)
            ComparisonFirst.SelectId(_comparisonFirstId.Value);

        if (_comparisonSecondId != null)
            ComparisonSecond.SelectId(_comparisonSecondId.Value);

        if (previousFirst != _comparisonFirstId || previousSecond != _comparisonSecondId)
            ClearComparison();
    }

    private void RestoreComparisonSelection(List<GloveMicroFiberAnalyzerHistoryEntry> history)
    {
        if (history.Count == 0)
        {
            _comparisonFirstId = null;
            _comparisonSecondId = null;
            return;
        }

        if (!HasComparisonSample(history, _comparisonFirstId))
            _comparisonFirstId = history[^1].Id;

        if (HasComparisonSample(history, _comparisonSecondId))
            return;

        _comparisonSecondId = null;
        for (var i = history.Count - 1; i >= 0; i--)
        {
            if (history[i].Id == _comparisonFirstId)
                continue;

            _comparisonSecondId = history[i].Id;
            break;
        }
    }

    private static bool HasComparisonSample(List<GloveMicroFiberAnalyzerHistoryEntry> history, int? id)
    {
        if (id == null)
            return false;

        return history.Exists(sample => sample.Id == id.Value);
    }

    private void SelectComparisonSample(int id, bool first)
    {
        if (_results == null)
            return;

        if (!_results.History.Exists(sample => sample.Id == id))
            return;

        if (first)
        {
            _comparisonFirstId = id;
            ComparisonFirst.SelectId(id);
        }
        else
        {
            _comparisonSecondId = id;
            ComparisonSecond.SelectId(id);
        }

        ClearComparison();
        UpdateComparisonView();
    }

    private void ClearComparison()
    {
        _comparisonHasResult = false;
        ComparisonProfilesContainer.RemoveAllChildren();
        ComparisonStorageMicroFibersContainer.RemoveAllChildren();
    }

    private void UpdateComparisonView()
    {
        ComparisonPanel.Visible = CompareHistory.Pressed;
        ComparisonRun.Disabled = !CanCompareSelectedSamples();
        ComparisonResults.Visible = _comparisonHasResult;
        ComparisonStatus.Visible = !_comparisonHasResult;
        ComparisonStatus.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString(GetComparisonStatusKey())));
        UpdatePrinterState();
    }

    private bool CanCompareSelectedSamples()
    {
        if (_comparisonFirstId == null)
            return false;

        if (_comparisonSecondId == null)
            return false;

        return _comparisonFirstId != _comparisonSecondId;
    }

    private string GetComparisonStatusKey()
    {
        if (_comparisonFirstId == null || _comparisonSecondId == null)
            return "glove-micro-fiber-analyzer-interface-compare-not-enough";

        if (_comparisonFirstId == _comparisonSecondId)
            return "glove-micro-fiber-analyzer-interface-compare-same-sample";

        return "glove-micro-fiber-analyzer-interface-compare-instructions";
    }

    private void RunComparison()
    {
        if (!CompareHistory.Pressed)
            return;

        if (_results == null)
            return;

        if (_comparisonFirstId == null)
            return;

        if (_comparisonSecondId == null)
            return;

        if (_comparisonFirstId == _comparisonSecondId)
            return;

        var first = _results.History.Find(sample => sample.Id == _comparisonFirstId.Value);
        var second = _results.History.Find(sample => sample.Id == _comparisonSecondId.Value);
        if (first == null || second == null)
            return;

        var gloveMatches = GloveMicroFiberComparison.FindGloveMatches(first.GloveMicroFibers, second.GloveMicroFibers);
        var storageMatches = GloveMicroFiberComparison.FindStorageMatches(first.StorageMicroFibers,
            second.StorageMicroFibers);
        DisplayComparisonResults(gloveMatches, storageMatches);
    }

    private void DisplayComparisonResults(
        List<GloveMicroFiberTrace> gloveMatches,
        List<StorageMicroFiberTrace> storageMatches)
    {
        ClearComparison();
        ComparisonProfileCount.Text = Loc.GetString("glove-micro-fiber-analyzer-interface-compare-profile-count",
            ("count", gloveMatches.Count));
        ComparisonStorageMicroFiberCount.Text = Loc.GetString(
            "glove-micro-fiber-analyzer-interface-compare-storage-micro-fiber-count",
            ("count", storageMatches.Count));
        DisplayComparisonGloveMatches(ComparisonProfilesContainer, gloveMatches,
            "glove-micro-fiber-analyzer-interface-compare-no-profiles");
        DisplayStorageMicroFibers(ComparisonStorageMicroFibersContainer, storageMatches,
            "glove-micro-fiber-analyzer-interface-compare-no-storage-micro-fibers");
        _comparisonHasResult = true;
        UpdateComparisonView();
    }

    private void DisplayComparisonGloveMatches(
        BoxContainer container,
        List<GloveMicroFiberTrace> matches,
        string emptyKey)
    {
        if (matches.Count == 0)
        {
            var empty = new RichTextLabel { HorizontalExpand = true };
            empty.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString(emptyKey)));
            container.AddChild(empty);
            return;
        }

        for (var i = 0; i < matches.Count; i++)
        {
            var trace = matches[i];
            container.AddChild(CreateSampleCard(trace.Code, trace.Prototype,
                "glove-micro-fiber-analyzer-interface-glove-sample-number", i + 1,
                "glove-micro-fiber-analyzer-interface-unknown-type"));
        }
    }
}

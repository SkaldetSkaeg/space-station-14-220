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
            ComparisonFirst.AddItem(GetSampleName(sample), sample.Id);
            ComparisonSecond.AddItem(GetSampleName(sample), sample.Id);
        }

        if (_comparisonFirstId == null || !history.Exists(sample => sample.Id == _comparisonFirstId.Value))
            _comparisonFirstId = history.Count > 0 ? history[^1].Id : null;

        if (_comparisonSecondId == null || !history.Exists(sample => sample.Id == _comparisonSecondId.Value))
        {
            _comparisonSecondId = null;
            for (var i = history.Count - 1; i >= 0; i--)
            {
                if (history[i].Id == _comparisonFirstId)
                    continue;

                _comparisonSecondId = history[i].Id;
                break;
            }
        }

        if (_comparisonFirstId != null)
            ComparisonFirst.SelectId(_comparisonFirstId.Value);

        if (_comparisonSecondId != null)
            ComparisonSecond.SelectId(_comparisonSecondId.Value);

        if (previousFirst != _comparisonFirstId || previousSecond != _comparisonSecondId)
            ClearComparison();
    }

    private void SelectComparisonSample(int id, bool first)
    {
        if (_results == null || !_results.History.Exists(sample => sample.Id == id))
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
        ComparisonRun.Disabled = _comparisonFirstId == null || _comparisonSecondId == null ||
            _comparisonFirstId == _comparisonSecondId;
        ComparisonResults.Visible = _comparisonHasResult;
        ComparisonStatus.Visible = !_comparisonHasResult;
        var statusKey = _comparisonFirstId == null || _comparisonSecondId == null
            ? "glove-micro-fiber-analyzer-interface-compare-not-enough"
            : _comparisonFirstId == _comparisonSecondId
                ? "glove-micro-fiber-analyzer-interface-compare-same-sample"
                : "glove-micro-fiber-analyzer-interface-compare-instructions";
        ComparisonStatus.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString(statusKey)));
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

        List<string> firstCodes = [];
        List<string> secondCodes = [];
        foreach (var profile in first.GloveMicroFibers)
        {
            firstCodes.Add(profile.Code);
        }

        foreach (var profile in second.GloveMicroFibers)
        {
            secondCodes.Add(profile.Code);
        }

        var profiles = FindComparisonMatches(firstCodes, secondCodes);
        var storageMicroFibers = FindComparisonStorageMicroFiberMatches(first.StorageMicroFibers,
            second.StorageMicroFibers);
        ClearComparison();
        ComparisonProfileCount.Text = Loc.GetString("glove-micro-fiber-analyzer-interface-compare-profile-count",
            ("count", profiles.Count));
        ComparisonStorageMicroFiberCount.Text = Loc.GetString(
            "glove-micro-fiber-analyzer-interface-compare-storage-micro-fiber-count",
            ("count", storageMicroFibers.Count));
        DisplayComparisonMatches(ComparisonProfilesContainer, profiles, first.GloveMicroFibers,
            "glove-micro-fiber-analyzer-interface-compare-no-profiles");
        DisplayStorageMicroFibers(ComparisonStorageMicroFibersContainer, storageMicroFibers,
            "glove-micro-fiber-analyzer-interface-compare-no-storage-micro-fibers");
        _comparisonHasResult = true;
        UpdateComparisonView();
    }

    private static List<string> FindComparisonMatches(List<string> first, List<string> second)
    {
        var remaining = new HashSet<string>(second, StringComparer.Ordinal);
        List<string> matches = [];
        foreach (var value in first)
        {
            if (!remaining.Remove(value))
                continue;

            matches.Add(value);
        }

        return matches;
    }

    private static List<StorageMicroFiberTrace> FindComparisonStorageMicroFiberMatches(
        List<StorageMicroFiberTrace> first, List<StorageMicroFiberTrace> second)
    {
        HashSet<string> remaining = new(StringComparer.Ordinal);
        foreach (var fiber in second)
        {
            remaining.Add(fiber.Code);
        }

        List<StorageMicroFiberTrace> matches = [];
        foreach (var fiber in first)
        {
            if (!remaining.Remove(fiber.Code))
                continue;

            matches.Add(fiber);
        }

        return matches;
    }

    private void DisplayComparisonMatches(BoxContainer container, List<string> matches,
        List<GloveMicroFiberTrace> sourceProfiles, string emptyKey)
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
            var match = matches[i];
            var trace = sourceProfiles.Find(profile => string.Equals(profile.Code, match, StringComparison.Ordinal));
            container.AddChild(CreateSampleCard(match, trace.Prototype,
                "glove-micro-fiber-analyzer-interface-glove-sample-number", i + 1,
                "glove-micro-fiber-analyzer-interface-unknown-type"));
        }
    }
}

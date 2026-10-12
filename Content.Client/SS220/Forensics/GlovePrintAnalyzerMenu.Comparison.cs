// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Forensics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.SS220.Forensics;

public sealed partial class GlovePrintAnalyzerMenu
{
    private int? _comparisonFirstId;
    private int? _comparisonSecondId;
    private bool _comparisonHasResult;

    private void InitializeComparison()
    {
        CompareHistory.ToolTip = Loc.GetString("glove-print-analyzer-interface-compare-not-enough");
        CompareHistory.OnPressed += _ => RefreshResults();
        ComparisonFirst.OnItemSelected += args => SelectComparisonSample(args.Id, true);
        ComparisonSecond.OnItemSelected += args => SelectComparisonSample(args.Id, false);
        ComparisonRun.OnPressed += _ => RunComparison();
        UpdateComparisonView();
    }

    private void UpdateComparisonHistory(List<GlovePrintAnalyzerHistoryEntry> history)
    {
        var previousFirst = _comparisonFirstId;
        var previousSecond = _comparisonSecondId;
        ComparisonFirst.Clear();
        ComparisonSecond.Clear();
        ComparisonFirst.Disabled = history.Count == 0;
        ComparisonSecond.Disabled = history.Count == 0;
        CompareHistory.Disabled = history.Count < 2;
        CompareHistory.ToolTip = CompareHistory.Disabled
            ? Loc.GetString("glove-print-analyzer-interface-compare-not-enough")
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
        ComparisonMicroFibersContainer.RemoveAllChildren();
    }

    private void UpdateComparisonView()
    {
        ComparisonPanel.Visible = CompareHistory.Pressed;
        ComparisonRun.Disabled = _comparisonFirstId == null || _comparisonSecondId == null ||
            _comparisonFirstId == _comparisonSecondId;
        ComparisonResults.Visible = _comparisonHasResult;
        ComparisonStatus.Visible = !_comparisonHasResult;
        var statusKey = _comparisonFirstId == null || _comparisonSecondId == null
            ? "glove-print-analyzer-interface-compare-not-enough"
            : _comparisonFirstId == _comparisonSecondId
                ? "glove-print-analyzer-interface-compare-same-sample"
                : "glove-print-analyzer-interface-compare-instructions";
        ComparisonStatus.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString(statusKey)));
    }

    private void RunComparison()
    {
        if (!CompareHistory.Pressed || _results == null || _comparisonFirstId == null || _comparisonSecondId == null)
            return;

        if (_comparisonFirstId == _comparisonSecondId)
            return;

        var first = _results.History.Find(sample => sample.Id == _comparisonFirstId.Value);
        var second = _results.History.Find(sample => sample.Id == _comparisonSecondId.Value);
        if (first == null || second == null)
            return;

        List<string> firstCodes = [];
        List<string> secondCodes = [];
        foreach (var profile in first.GlovePrints)
        {
            firstCodes.Add(profile.Code);
        }

        foreach (var profile in second.GlovePrints)
        {
            secondCodes.Add(profile.Code);
        }

        var profiles = FindComparisonMatches(firstCodes, secondCodes);
        var microFibers = FindComparisonMicroFiberMatches(first.MicroFibers, second.MicroFibers);
        ClearComparison();
        ComparisonProfileCount.Text = Loc.GetString("glove-print-analyzer-interface-compare-profile-count",
            ("count", profiles.Count));
        ComparisonMicroFiberCount.Text = Loc.GetString("glove-print-analyzer-interface-compare-micro-fiber-count",
            ("count", microFibers.Count));
        DisplayComparisonMatches(ComparisonProfilesContainer, profiles,
            "glove-print-analyzer-interface-compare-no-profiles");
        DisplayMicroFibers(ComparisonMicroFibersContainer, microFibers,
            "glove-print-analyzer-interface-compare-no-micro-fibers");
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

    private static List<MicroFiberTrace> FindComparisonMicroFiberMatches(List<MicroFiberTrace> first,
        List<MicroFiberTrace> second)
    {
        HashSet<string> remaining = new(StringComparer.Ordinal);
        foreach (var fiber in second)
        {
            remaining.Add(fiber.Code);
        }

        List<MicroFiberTrace> matches = [];
        foreach (var fiber in first)
        {
            if (!remaining.Remove(fiber.Code))
                continue;

            matches.Add(fiber);
        }

        return matches;
    }

    private static void DisplayComparisonMatches(BoxContainer container, List<string> matches, string emptyKey)
    {
        if (matches.Count == 0)
        {
            var empty = new RichTextLabel { HorizontalExpand = true };
            empty.SetMessage(FormattedMessage.FromUnformatted(Loc.GetString(emptyKey)));
            container.AddChild(empty);
            return;
        }

        foreach (var match in matches)
        {
            var text = new RichTextLabel { HorizontalExpand = true };
            text.SetMessage(FormattedMessage.FromUnformatted(match));
            container.AddChild(text);
        }
    }
}

forensic-scanner-interface-micro-fibers = Microfibers
forensic-pad-verb-text = Get samples
forensic-pad-verb-message = Perform a samples extraction
forensic-pad-verb-no-sapmles = Object don't have any samples to take
forensic-scanner-match-micro-fiber = Match in microfiber found!
glove-print-scanner-verb-text = Collect a scanner sample
glove-print-scanner-verb-message = Scan the object in the selected mode and dispense a sample swab.
glove-print-scanner-mode-surface = microfiber collection
glove-print-scanner-mode-own = item sample
glove-print-scanner-mode-label = Mode
glove-print-scanner-mode-verb = Select mode
glove-print-scanner-mode-tooltip = Microfibers collects samples of gloves, clothing and equipment from the object's surface. Sample reads the selected pair of gloves or item's own individual code; an object without a sample will be refused.
glove-print-scanner-mode-surface-short = Microfibers
glove-print-scanner-mode-own-short = Sample
glove-print-scanner-mode-selected = Scanner mode: { $mode }.
glove-print-scanner-switch-hint = Use the scanner in hand to open its radial mode menu.
glove-print-scanner-no-own-print = Scan refused: the object has no individual sample of its own.
glove-print-scanner-mode-changed = Scan interrupted: the scanner mode changed.
glove-print-swab-unused = This swab contains no sample. Collected samples are dispensed by the microfiber scanner.
glove-print-swab-used = This swab contains a collected sample. Insert it into a microfiber analyzer to examine the sample.
glove-print-swab-label = microfiber sample: { $source }
glove-print-swab-source = Sample source: { $source }
glove-print-analyzer-slot-name = Microfiber sample
glove-print-analyzer-interface-title = Microfiber Analyzer
glove-print-analyzer-interface-number = № { $number }
glove-print-analyzer-interface-number-caption = SERIAL
glove-print-analyzer-interface-number-caption-second-line = NUMBER
glove-print-analyzer-interface-number-tooltip = Analyzer number: { $number }
glove-print-analyzer-interface-brand = NT
glove-print-analyzer-interface-eject = Eject sample
glove-print-analyzer-interface-print = Print
glove-print-analyzer-interface-analyze = Analyze sample
glove-print-analyzer-interface-analyzing = Analyzing the sample…
glove-print-analyzer-interface-ready = The sample is ready for analysis.
glove-print-analyzer-interface-analysis-interrupted = Analysis interrupted: the sample was removed.
glove-print-analyzer-interface-description = Description
glove-print-analyzer-interface-description-placeholder = Enter a sample description…
glove-print-analyzer-interface-save-description = Save
glove-print-analyzer-interface-description-value = Description: { $description }
glove-print-analyzer-interface-history = History
glove-print-analyzer-interface-compare-history = Compare samples
glove-print-analyzer-interface-compare-first = First sample
glove-print-analyzer-interface-compare-second = Second sample
glove-print-analyzer-interface-compare-run = Compare
glove-print-analyzer-interface-compare-instructions = Select two samples from the history and press Compare.
glove-print-analyzer-interface-compare-same-sample = Select two different samples.
glove-print-analyzer-interface-compare-profile-count = Matching glove sample codes: { $count }
glove-print-analyzer-interface-compare-micro-fiber-count = Matching storage sample codes: { $count }
glove-print-analyzer-interface-compare-no-profiles = No matching glove sample codes.
glove-print-analyzer-interface-compare-no-micro-fibers = No matching storage sample codes.
glove-print-analyzer-interface-compare-note = A matching sample code identifies a specific pair of gloves or individual storage item, but does not establish its owner.
glove-print-analyzer-interface-compare-not-enough = Comparison requires two analyzed samples in the history.
glove-print-analyzer-interface-current-sample = Unexamined sample
glove-print-analyzer-interface-history-empty = History is empty.
glove-print-analyzer-interface-history-sample = Sample { $identifier }
glove-print-analyzer-interface-source = Examined object: { $source }
glove-print-analyzer-interface-source-heading = { $source }
glove-print-analyzer-interface-analyzed-at = Analysis time: { $time }
glove-print-analyzer-interface-printer-ready = The linked photocopier is ready to print.
glove-print-analyzer-interface-printer-unavailable = The photocopier is linked but unavailable. Check power, toner and its print queue.
glove-print-analyzer-interface-printer-disconnected = No photocopier is linked. Establish a link with a multitool.
glove-print-analyzer-port-source-name = Microfiber analysis report
glove-print-analyzer-port-source-description = Sends the report to the linked photocopier.
glove-print-analyzer-port-sink-name = Print documents
glove-print-analyzer-port-sink-description = Receives reports from a linked analyzer for printing.
glove-print-analyzer-report-title = Microfiber analysis report
glove-print-analyzer-report-header = [bold]Form NT-SEC-GTE[/bold]
    { "[head=2]Microfiber Analysis Report[/head]" }

    { "[bold]Registration number:[/bold]" } { $identifier }
glove-print-analyzer-report-source = [bold]Examined object:[/bold] { $source }
glove-print-analyzer-report-analyzed-at = [bold]Analysis completed (elapsed round time):[/bold] { $time }
glove-print-analyzer-report-results = [head=3]Identified Glove Samples[/head]
    { "[bold]Number of glove samples:[/bold]" } { $count }
glove-print-analyzer-report-trace = [bold]Sample No. { $number }[/bold]
    { "[bold]Glove type:[/bold]" } { $type }
    { "[bold]Sample code:[/bold]" } { $code }
glove-print-analyzer-report-micro-fibers = [head=3]Identified Storage Samples[/head]
    { "[bold]Number of storage samples:[/bold]" } { $count }
glove-print-analyzer-report-micro-fiber = [bold]Storage Sample No. { $number }[/bold]
    { "[bold]Storage item:[/bold]" } { $type }
    { "[bold]Sample code:[/bold]" } { $code }
glove-print-analyzer-report-description = [head=3]Sample Description[/head]
    { $description }
glove-print-analyzer-report-footer = [italic]This document was generated automatically by a microfiber analyzer.[/italic]

    { "[italic]Space for the detective's signature and stamp[/italic]" }
glove-print-analyzer-interface-no-swab = Insert a swab containing a sample.
glove-print-analyzer-interface-unused-swab = The inserted swab has not been used yet.
glove-print-analyzer-interface-traces = Glove samples
glove-print-analyzer-interface-no-traces = No glove samples found.
glove-print-analyzer-interface-micro-fibers = Storage samples
glove-print-analyzer-interface-no-micro-fibers = No storage samples were found.
glove-print-analyzer-interface-type = Glove type: { $type }
glove-print-analyzer-interface-item-type = Storage item: { $type }
glove-print-analyzer-interface-code = Sample code: { $code }
glove-print-analyzer-interface-glove-sample-number = Glove sample No. { $number }
glove-print-analyzer-interface-storage-sample-number = Storage sample No. { $number }
glove-print-analyzer-interface-empty-category = None found
glove-print-analyzer-interface-unknown-type = Unknown glove type
glove-print-analyzer-interface-unknown-item = Unknown storage item

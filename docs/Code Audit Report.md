# Platé.Johann — Exhaustive Code Audit Report

---

## Executive Summary

Platé.Johann is a well-structured WPF dictation tool built on a clean layered architecture (Domain → Application → Infrastructure → UI). The codebase demonstrates solid engineering fundamentals: immutable domain records, clear separation of concerns, a JSON migration strategy for schema evolution, and meaningful test coverage of parsing and persistence logic. However, the audit reveals several critical concurrency hazards — most notably in the `AudioWatcherService` and sequence number allocation — along with resource management gaps, fire-and-forget async patterns on the UI thread, and a security-relevant API key handling approach. The codebase is production-ready for a single-user desktop tool but needs targeted hardening before broader rollout.

**Overall Grade: B-** — Strong architecture, meaningful test suite, but concurrency bugs and resource leaks need immediate attention.

---

## Category Grades

|Category|Grade|Rationale|
|---|---|---|
|Architecture & Design|**A-**|Clean layered architecture, immutable domain, good abstraction boundaries|
|Code Quality & Maintainability|**B+**|Consistent style, good naming, some duplication in renderers|
|Bugs & Correctness|**C+**|Sequence number race condition (noted in polish.md), file-access races, swallowed exceptions|
|Concurrency & Thread Safety|**C**|Fire-and-forget patterns, UI-thread marshaling gaps, lock scope issues|
|Security|**C+**|API key in plaintext .env, no input sanitization on rendered HTML|
|Testing|**B**|Good unit coverage of parsers/generators, no integration or concurrency tests|
|Performance & Scalability|**B**|Parallel GPT calls good, synchronous file I/O in some paths|
|Dead Code & Abstractions|**B+**|Minimal dead code, all interfaces have implementations|

---

## Detailed Findings

---

### Finding 1: Sequence Number Race Condition Under Concurrent Processing

**Severity:** Critical  
**Confidence:** High  
**Category:** Concurrency / Correctness  
**Location:** `JsonRepository.GetNextSequenceNumberAsync` + `AudioWatcherService`

**Evidence:** `JsonRepository` uses an instance-level `SemaphoreSlim _seqLock` (line ~130 of `JsonRepository.cs`). However, `AudioWatcherService` also holds its own `SemaphoreSlim _processLock` that serializes file processing. The sequence lock only protects within a single `JsonRepository` instance — but the counter file mechanism has a subtle TOCTOU window: if the app crashes between reading the counter and writing the incremented value, the counter is never persisted.

More critically, the `_seqLock` is per-instance. If a second `JsonRepository` were ever constructed (e.g., during testing or future refactoring), the lock provides no protection. The `polish.md` document explicitly confirms this bug has been observed in production: _"Nummerierung bleibt stehen / gleiche Nummer wird mehrfach vergeben"_.

**Technical Explanation:** The `SemaphoreSlim` in `JsonRepository` is instance-scoped, not process-scoped. The counter file at `_raw/_counter.json` provides durability, but there's no file-level lock. Two rapid `ProcessAudioAsync` calls can both read the same counter value if the first hasn't flushed to disk yet.

**Impact:** Duplicate sequence numbers → duplicate filenames → data loss (file overwrite).

**Recommended Fix:** Use a file-based lock (e.g., `FileStream` with `FileShare.None` as an exclusive lock file) alongside the in-memory semaphore to make the reservation atomic across potential future multi-instance scenarios. Additionally, write the counter _before_ returning the value, which the code already does — the real risk is the crash window.

**Implementation:**

```csharp
public async Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default)
{
    await _seqLock.WaitAsync(ct);
    try
    {
        var rawDir = GetRawDir(date);
        Directory.CreateDirectory(rawDir);

        var lockPath = Path.Combine(rawDir, "_counter.lock");
        // File-level lock prevents cross-process races
        await using var lockFile = new FileStream(
            lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, 
            FileShare.None, 4096, FileOptions.DeleteOnClose);

        var counterPath = Path.Combine(rawDir, "_counter.json");
        int next;
        if (File.Exists(counterPath))
        {
            await using var rs = File.OpenRead(counterPath);
            var doc = await JsonSerializer.DeserializeAsync<CounterDoc>(rs, ReadOptions, ct);
            next = doc?.Next ?? 1;
        }
        else
        {
            var entries = await GetEntriesForDateAsync(date, ct);
            next = entries.Count == 0 ? 1 : entries.Max(e => e.SequenceNumber) + 1;
        }

        await using var ws = File.Open(counterPath, FileMode.Create, 
            FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(ws, new CounterDoc(next + 1), WriteOptions, ct);
        await ws.FlushAsync(ct); // Ensure durable before releasing lock

        return next;
    }
    finally
    {
        _seqLock.Release();
    }
}
```

---

### Finding 2: Fire-and-Forget `Task.Run` in AudioWatcherService Swallows Exceptions Silently

**Severity:** High  
**Confidence:** High  
**Category:** Concurrency / Error Handling  
**Location:** `AudioWatcherService.ProcessExistingFiles` (line ~50), `OnFileCreated` (line ~62)

**Evidence:**

```csharp
private void ProcessExistingFiles(string inputPath)
{
    Task.Run(async () =>
    {
        var files = Directory.GetFiles(inputPath, "*.mp3");
        foreach (var file in files)
        {
            await TryProcessFileAsync(file);
        }
    });
}
```

The `Task.Run` result is discarded. If `Directory.GetFiles` throws (e.g., `UnauthorizedAccessException`), the exception propagates only to the `TaskScheduler.UnobservedTaskException` handler in `App.xaml.cs`, which merely logs — it does not surface to the user.

Similarly, `OnFileCreated` fires `Task.Run` without awaiting:

```csharp
private void OnFileCreated(object sender, FileSystemEventArgs e)
{
    Task.Run(async () =>
    {
        await Task.Delay(1000);
        await TryProcessFileAsync(e.FullPath);
    });
}
```

**Impact:** If the input directory is inaccessible, all startup processing silently fails with no user feedback. The `EntryProcessingFailed` event only fires for per-file errors inside `TryProcessFileAsync`, not for the outer `Directory.GetFiles` call.

**Recommended Fix:** Wrap the outer loop in a try-catch and raise `EntryProcessingFailed` for directory-level errors. Consider returning the Task from `ProcessExistingFiles` so the caller can optionally observe it.

```csharp
private void ProcessExistingFiles(string inputPath)
{
    _ = Task.Run(async () =>
    {
        try
        {
            var files = Directory.GetFiles(inputPath, "*.mp3");
            foreach (var file in files)
                await TryProcessFileAsync(file);
        }
        catch (Exception ex)
        {
            EntryProcessingFailed?.Invoke(inputPath, ex);
        }
    });
}
```

---

### Finding 3: `SemaphoreSlim` in AudioWatcherService Serializes All Processing Unnecessarily

**Severity:** Medium  
**Confidence:** High  
**Category:** Performance / Concurrency  
**Location:** `AudioWatcherService._processLock`

**Evidence:** A single `SemaphoreSlim(1, 1)` gates all file processing:

```csharp
private async Task TryProcessFileAsync(string filePath)
{
    await _processLock.WaitAsync();
    try { ... }
    finally { _processLock.Release(); }
}
```

Combined with `ProcessExistingFiles` iterating sequentially _inside_ this lock, startup processing of N files takes N × (transcription + GPT) time. Since OpenAI API calls are I/O-bound, multiple files could be processed concurrently (bounded by API rate limits).

**Impact:** If a user drops 10 MP3s before starting the app, processing could take 30+ minutes instead of ~5 minutes with bounded parallelism.

**Recommended Fix:** Replace `SemaphoreSlim(1, 1)` with `SemaphoreSlim(3, 3)` (or configurable concurrency) and process files in parallel:

```csharp
private async Task ProcessExistingFilesAsync(string inputPath)
{
    var files = Directory.GetFiles(inputPath, "*.mp3");
    await Parallel.ForEachAsync(files, 
        new ParallelOptions { MaxDegreeOfParallelism = 3 },
        async (file, ct) => await TryProcessFileAsync(file));
}
```

---

### Finding 4: Synchronous Blocking on Async Code at Startup

**Severity:** High  
**Confidence:** High  
**Category:** Concurrency / Anti-Pattern  
**Location:** `App.xaml.cs`, lines ~45–50

**Evidence:**

```csharp
var initialSettings = Task.Run(() => settingsRepo.LoadAsync()).GetAwaiter().GetResult();
```

And again:

```csharp
Task.Run(() => settingsRepo.SaveAsync(initialSettings)).GetAwaiter().GetResult();
```

**Technical Explanation:** `.GetAwaiter().GetResult()` blocks the calling thread. While wrapping in `Task.Run` avoids deadlocking on the WPF dispatcher, this is a code smell that risks deadlocks if the pattern is later used without `Task.Run`. It also forces two thread-pool hops for simple file reads.

**Impact:** Startup is slightly slower due to thread-pool scheduling overhead. More importantly, this pattern establishes a dangerous precedent — if copied to other locations without the `Task.Run` wrapper, it will deadlock on the UI thread.

**Recommended Fix:** Move initialization to an async method. WPF allows `async void` for event handlers:

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    var initialSettings = await settingsRepo.LoadAsync();
    // ... rest of init
}
```

---

### Finding 5: XSS Vulnerability in HTML Renderers via Markdown Content

**Severity:** Medium  
**Confidence:** Medium  
**Category:** Security  
**Location:** `HtmlOverviewService.cs`, `HtmlRenderer.cs`, `MarkdownHelper.cs`

**Evidence:** `MarkdownHelper` uses Markdig's `UseAdvancedExtensions()` which includes raw HTML pass-through by default. The comment says _"Markdig handles XSS sanitization internally"_ — this is incorrect. Markdig does **not** sanitize HTML by default; it faithfully renders `<script>` tags embedded in Markdown.

If a transcript contains user-spoken text like _"Script tag open alert hello script tag close"_ and Whisper transcribes it literally (unlikely but possible), or if a user manually enters HTML in the "Neues Element" dialog, the generated HTML files will execute arbitrary JavaScript when opened in a browser.

**Impact:** Low for single-user desktop scenario, but if overview HTML files are shared (e.g., via OneDrive or email as mentioned in the docs), this becomes a stored XSS vector.

**Recommended Fix:** Add the `DisableHtml` extension to the Markdig pipeline:

```csharp
private static readonly MarkdownPipeline Pipeline =
    new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()  // Prevents raw HTML pass-through
        .Build();
```

---

### Finding 6: API Key Stored in Plaintext `.env` File Without Encryption

**Severity:** Medium  
**Confidence:** High  
**Category:** Security  
**Location:** `ApiKeyProvider.cs`, `App.xaml.cs` (EnsureEnvFile)

**Evidence:** The API key is stored as `OPENAI_API_KEY=sk-proj-…` in a plaintext file at `Documents\Johann\.env`. The `EnsureEnvFile` method copies it from a network share (`X:\PRO_Programmierung\...\.env`).

**Impact:** Any process or user with read access to the Documents folder can steal the OpenAI API key. For an internal corporate tool this is low risk, but it violates security best practices.

**Recommended Fix:** Use Windows DPAPI via `ProtectedData` class for at-rest encryption. Store the encrypted key in `settings.json` instead of a separate `.env` file:

```csharp
public static void StoreKey(string key, string path)
{
    var encrypted = ProtectedData.Protect(
        Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
    File.WriteAllBytes(path, encrypted);
}
```

---

### Finding 7: `FileSystemWatcher` Misses Events Under Load and Has No Retry Logic

**Severity:** Medium  
**Confidence:** High  
**Category:** Reliability / Correctness  
**Location:** `AudioWatcherService.Start()`

**Evidence:** The `FileSystemWatcher` is configured with only `NotifyFilters.FileName | NotifyFilters.CreationTime`. The `InternalBufferSize` is left at default (8192 bytes). Under high load (many files copied simultaneously), `FileSystemWatcher` silently drops events when its buffer overflows.

The `Error` event is never subscribed to:

```csharp
_watcher = new FileSystemWatcher(inputPath, "*.mp3")
{
    NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
    EnableRaisingEvents = true
};
_watcher.Created += OnFileCreated;
// Missing: _watcher.Error += OnWatcherError;
```

**Impact:** If a user batch-copies many MP3s (e.g., 20+ from a phone sync), some files may never be processed.

**Recommended Fix:**

```csharp
_watcher.InternalBufferSize = 65536; // 64KB buffer
_watcher.Error += (_, e) =>
{
    EntryProcessingFailed?.Invoke("FileSystemWatcher", e.GetException());
    // Restart watcher
    Stop();
    Start();
};
```

Also add periodic polling as a safety net (every 30 seconds, scan for unprocessed `.mp3` files).

---

### Finding 8: `Dispose` Pattern Incomplete — `AudioWatcherService` Not Disposed on Crash Paths

**Severity:** Medium  
**Confidence:** High  
**Category:** Resource Management  
**Location:** `App.xaml.cs` OnExit, `AudioWatcherService`

**Evidence:** `App.OnExit` calls `_audioWatcher?.Dispose()`, but if the application crashes (unhandled exception), `OnExit` is not guaranteed to run. The `FileSystemWatcher` will be finalized by the GC but the `SemaphoreSlim` may leak.

Additionally, `EntryProcessingService` holds no disposable resources but its `SummaryGenerator` → `ILlmProvider` chain may hold `HttpClient` instances inside the OpenAI SDK. The `OpenAiLlmProvider` creates a `ChatClient` in its constructor but never disposes it.

**Impact:** Minor for a desktop app (process exit cleans up), but holding HTTP connections open indefinitely can cause socket exhaustion under heavy use.

**Recommended Fix:** Make `OpenAiLlmProvider` implement `IDisposable` and dispose the underlying HTTP resources. Register it in the app lifecycle.

---

### Finding 9: Swallowed Exceptions in Processing Pipeline Mask Root Causes

**Severity:** Medium  
**Confidence:** High  
**Category:** Error Handling / Observability  
**Location:** `EntryProcessingService.ProcessAudioAsync` (multiple `catch` blocks), `EntryProcessingService.ArchiveRawFilesAsync`

**Evidence:** Multiple catch-all blocks with no logging:

```csharp
catch
{
    // Fallback / ignore failure
}
```

This appears in renderer invocation (lines ~120–130 of `EntryProcessingService`), archive operations, and MP3 move operations. When a PDF render fails, the entry's `PdfCreated` status stays `false` with no indication of _why_.

**Impact:** When users report "PDF wasn't created," there's no diagnostic trail. The `Johann_crash.txt` only catches unhandled exceptions, not these swallowed ones.

**Recommended Fix:** Introduce a simple `ILogger` abstraction (or use `System.Diagnostics.Trace`) and log caught exceptions with context:

```csharp
catch (Exception ex)
{
    System.Diagnostics.Trace.TraceWarning(
        $"PDF render failed for {entry.JobId}: {ex.Message}");
}
```

---

### Finding 10: `HtmlEncode` Is Manual and Incomplete

**Severity:** Low  
**Confidence:** High  
**Category:** Security / Code Quality  
**Location:** `HtmlRenderer.cs`, `HtmlOverviewService.cs`

**Evidence:** Both renderers use a hand-rolled `HtmlEncode`:

```csharp
private static string HtmlEncode(string s)
    => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
        .Replace("\"", "&quot;");
```

This misses single quotes (`'` → `&#39;`), which can be exploited in attribute contexts. More importantly, .NET provides `System.Net.WebUtility.HtmlEncode` which handles all edge cases including Unicode.

**Recommended Fix:** Replace with `System.Net.WebUtility.HtmlEncode(s)`.

---

### Finding 11: JSON Repository Reads All Files Sequentially for `GetByJobIdAsync`

**Severity:** Low  
**Confidence:** High  
**Category:** Performance  
**Location:** `JsonRepository.GetByJobIdAsync`

**Evidence:** The method scans every date directory, every `_raw` subdirectory, and every `_status.json` file until it finds a match. With hundreds of entries, this is O(N) file I/O.

```csharp
foreach (var dir in Directory.EnumerateDirectories(_outputRoot))
{
    var rawDir = Path.Combine(dir, "_raw");
    foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
    {
        var entry = await LoadFileAsync(file, ct);
        if (entry?.JobId == jobId) return entry;
    }
}
```

**Impact:** Currently called only for `IsDone` tests and ad-hoc lookups. Not on a hot path. But as entry count grows over months, this will degrade.

**Recommended Fix:** Build an in-memory index (`Dictionary<string, string>` mapping JobId → file path) populated lazily on first access. Or encode the date in the JobId (which it already does: `YYMMDD_NNN_…`) and parse it to narrow the search to one date directory.

---

### Finding 12: `TypeExtractor` Missing "Analog" Keyword

**Severity:** Medium  
**Confidence:** High  
**Category:** Bug / Correctness  
**Location:** `TypeExtractor.cs`

**Evidence:** The `Keywords` dictionary maps type keywords to `EntryType` values:

```csharp
["aufgabe"] = EntryType.Aufgabe,
["email"] = EntryType.EMail,
["e-mail"] = EntryType.EMail,
["gesprächsnotiz"] = EntryType.Gesprächsnotiz,
["gesprächsnotizen"] = EntryType.Gesprächsnotiz,
["stundenzettel"] = EntryType.Stundenzettel,
["projekt"] = EntryType.Projekt,
```

The keyword `"analog"` is **missing**. The `EntryType.Analog` enum value exists, the `AnalogPrompt` exists, the UI shows it, but speaking "Analog Projektname ..." at the start of a dictation will **not** set the type to `Analog` — it will fall through to the `LegacyProjectResolver` and treat "Analog" as a project name.

**Impact:** Users dictating analog entries get them misclassified as `Projekt` with project name "Analog".

**Recommended Fix:**

```csharp
private static readonly Dictionary<string, EntryType> Keywords =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["aufgabe"] = EntryType.Aufgabe,
        ["email"] = EntryType.EMail,
        ["e-mail"] = EntryType.EMail,
        ["gesprächsnotiz"] = EntryType.Gesprächsnotiz,
        ["gesprächsnotizen"] = EntryType.Gesprächsnotiz,
        ["stundenzettel"] = EntryType.Stundenzettel,
        ["projekt"] = EntryType.Projekt,
        ["analog"] = EntryType.Analog,       // ← ADD THIS
    };
```

Add a corresponding test in `HeaderParserTests`:

```csharp
[InlineData("Analog Notizen rest", EntryType.Analog, "Notizen")]
```

---

### Finding 13: `NewEntryView` Dialog Result Logic Is Fragile

**Severity:** Low  
**Confidence:** High  
**Category:** Bug / UX  
**Location:** `NewEntryView.xaml.cs`, `NewEntryView.xaml`

**Evidence:** The Save button has _both_ a `Command` binding and a `Click` handler:

```xml
<Button Content="Speichern"
        Command="{Binding SaveCommand}"
        Click="OnSaveClick" ... />
```

```csharp
private void OnSaveClick(object sender, RoutedEventArgs e) => DialogResult = true;
```

The `SaveCommand` validates `CanSave` (project + title not empty), but the `Click` handler fires regardless — setting `DialogResult = true` even if the command didn't execute because `CanSave` was false.

**Impact:** A user can potentially close the dialog with `DialogResult = true` even when validation fails, if they click fast enough or if the command binding is delayed.

**Recommended Fix:** Check the ViewModel's `DialogResult` property in the click handler:

```csharp
private void OnSaveClick(object sender, RoutedEventArgs e)
{
    if (DataContext is NewEntryViewModel vm && vm.DialogResult)
        DialogResult = true;
}
```

---

### Finding 14: Duplicated Duration Formatting Logic Across 4 Files

**Severity:** Low  
**Confidence:** High  
**Category:** Code Quality / DRY  
**Location:** `HtmlOverviewService.cs`, `HtmlRenderer.cs`, `PdfRenderer.cs`, `EntryDetailViewModel.cs`

**Evidence:** The identical `FormatDuration` method appears in four separate files:

```csharp
private static string FormatDuration(double seconds)
{
    var ts = TimeSpan.FromSeconds(seconds);
    return ts.TotalHours >= 1
        ? $"{(int)ts.TotalHours}:{ts.Minutes:D2}:{ts.Seconds:D2}"
        : $"{ts.Minutes}:{ts.Seconds:D2}";
}
```

**Recommended Fix:** Extract to `Platee.Johann.Domain.Services.DurationFormatter` as a static helper.

---

### Finding 15: No Test Coverage for `EntryProcessingService` Integration

**Severity:** Medium  
**Confidence:** High  
**Category:** Testing  
**Location:** `Platee.Johann.Tests/`

**Evidence:** The test project covers `HeaderParser`, `FilenameBuilder`, `WordLimitCalculator`, `SummaryGenerator`, `JsonMigrator`, `JsonRepository`, `IsDone`, and `SortMode`. However, `EntryProcessingService` — the critical orchestration component — has **zero** test coverage. This is the class where the sequence number bug manifests, where exception swallowing occurs, and where the archive/render pipeline is coordinated.

**Recommended Fix:** Add integration tests with mocked `IAudioTranscriber` and `ILlmProvider` that exercise the full `ProcessAudioAsync` pipeline, including error paths (transcription failure, GPT failure, archive failure).

---

### Finding 16: `SettingsView` Is Non-Modal but Settings Changes Have No Undo

**Severity:** Low  
**Confidence:** Medium  
**Category:** UX / Design  
**Location:** `MainViewModel.OpenSettings`, `SettingsView`

**Evidence:**

```csharp
window.Show(); // non-modal — user can keep working
```

The settings window is non-modal, meaning a user can change prompts while a processing job is running. Since `SummaryGenerator` reads prompts from `SettingsHolder.Current` on each call, a mid-processing prompt change will affect in-flight GPT calls inconsistently — some sections use the old prompt, others the new one.

**Impact:** Inconsistent output if prompts are changed during processing.

**Recommended Fix:** Snapshot `AppSettings` at the start of each `ProcessAudioAsync` call and pass it through the pipeline, rather than reading from the live `SettingsHolder` during each GPT call.

---

## Prioritized Top 10 Issues

|#|Finding|Severity|Effort|
|---|---|---|---|
|1|Sequence number race condition (F1)|Critical|Medium|
|2|Missing "Analog" type keyword (F12)|Medium|Trivial|
|3|Fire-and-forget exception swallowing (F2)|High|Low|
|4|Synchronous blocking at startup (F4)|High|Low|
|5|XSS in Markdown→HTML rendering (F5)|Medium|Trivial|
|6|FileSystemWatcher buffer overflow (F7)|Medium|Low|
|7|Swallowed exceptions in pipeline (F9)|Medium|Low|
|8|Manual HtmlEncode incomplete (F10)|Low|Trivial|
|9|No EntryProcessingService tests (F15)|Medium|Medium|
|10|NewEntryView dialog result race (F13)|Low|Trivial|

---

## Remediation Roadmap

### Immediate (this week)

1. **Add `"analog"` to `TypeExtractor.Keywords`** — 1-line fix + 1 test. Users are likely already hitting this.
2. **Fix `NewEntryView` click handler** to check `vm.DialogResult` before setting `Window.DialogResult`.
3. **Replace manual `HtmlEncode`** with `WebUtility.HtmlEncode` in both renderers.
4. **Add `.DisableHtml()` to Markdig pipeline** to prevent script injection.

### Short-term (next 2 weeks)

5. **Harden sequence number allocation** with file-level locking and `FlushAsync`.
6. **Add `_watcher.Error` handler** and increase `InternalBufferSize` to 64KB.
7. **Wrap fire-and-forget `Task.Run` calls** in try-catch with event propagation.
8. **Replace synchronous `.GetAwaiter().GetResult()`** with async `OnStartup`.
9. **Add structured logging** (at minimum `Trace.TraceWarning`) in all swallowed catch blocks.
10. **Extract `FormatDuration`** to a shared utility class.

### Long-term (next month)

11. **Write integration tests for `EntryProcessingService`** covering happy path, partial failures, and concurrent processing.
12. **Snapshot settings at processing start** to prevent mid-flight prompt changes.
13. **Implement bounded parallel processing** in `AudioWatcherService` (configurable concurrency of 2–3).
14. **Evaluate DPAPI** for API key encryption at rest.
15. **Add periodic polling** as a FileSystemWatcher safety net for missed events.
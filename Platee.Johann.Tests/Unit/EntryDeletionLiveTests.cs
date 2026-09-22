namespace Platee.Johann.Tests.Unit;

using System.Security.Cryptography;
using FluentAssertions;
using Platee.Johann.Infrastructure.Json;
using Xunit;

/// <summary>
/// Löschen (#55) gegen eine KOPIE eines echten Ausgabeordners: jeder Eintrag wird einzeln
/// gelöscht, geprüft und am Ende alles zurückgelegt. Das Original wird nie angefasst.
/// <para>
/// Läuft nur mit <c>JOHANN_DELETE_LIVE_SOURCE=&lt;Ausgabeordner&gt;</c>; auf CI und ohne
/// Variable wird der Test als übersprungen ausgewiesen.
/// </para>
/// </summary>
public sealed class EntryDeletionLiveTests : IDisposable
{
    private readonly string? source = Environment.GetEnvironmentVariable("JOHANN_DELETE_LIVE_SOURCE");
    private readonly string copy = Path.Combine(Path.GetTempPath(), $"JohannDeleteLive_{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(this.copy))
        {
            Directory.Delete(this.copy, recursive: true);
        }
    }

    [SkippableFact]
    public async Task Every_real_entry_deletes_exactly_its_own_files_and_restores_byte_identical()
    {
        Skip.If(string.IsNullOrWhiteSpace(this.source) || !Directory.Exists(this.source),
            "JOHANN_DELETE_LIVE_SOURCE nicht gesetzt — Live-Prüfung übersprungen.");

        CopyTree(this.source!, this.copy);
        var original = Snapshot(this.copy);
        var repo = new JsonRepository(this.copy);
        var trashRoot = Path.Combine(this.copy, JsonRepository.TrashFolderName);
        var deleted = 0;

        foreach (var date in await repo.GetAvailableDatesAsync())
        {
            var counterPath = Path.Combine(this.copy, date.ToString("yyyy-MM-dd"), "_raw", "_counter.json");
            var counterBefore = File.Exists(counterPath) ? File.ReadAllText(counterPath) : null;

            foreach (var entry in await repo.GetEntriesForDateAsync(date))
            {
                var before = Snapshot(this.copy);

                var result = await repo.DeleteAsync(entry.JobId);

                result.Found.Should().BeTrue(entry.JobId);
                result.MovedFiles.Should().Contain(f => f.EndsWith("_status.json", StringComparison.Ordinal));
                (await repo.GetByJobIdAsync(entry.JobId)).Should().BeNull();

                var after = Snapshot(this.copy);
                var outsideTrash = after.Where(kv => !kv.Key.StartsWith(JsonRepository.TrashFolderName, StringComparison.Ordinal))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                var expectedRemaining = before
                    .Where(kv => !kv.Key.StartsWith(JsonRepository.TrashFolderName, StringComparison.Ordinal))
                    .Where(kv => !result.MovedFiles.Contains(Path.Combine(this.copy, kv.Key)))
                    .ToDictionary(kv => kv.Key, kv => kv.Value);
                outsideTrash.Should().BeEquivalentTo(expectedRemaining, $"only the files of {entry.JobId} may move");

                foreach (var moved in result.MovedFiles)
                {
                    var relative = Path.GetRelativePath(this.copy, moved);
                    var inTrash = after.Where(kv => kv.Key.StartsWith(JsonRepository.TrashFolderName, StringComparison.Ordinal)
                        && kv.Key.EndsWith(Path.GetFileName(moved), StringComparison.Ordinal));
                    inTrash.Select(kv => kv.Value).Should().Contain(before[relative], $"{relative} must arrive unchanged");
                }

                deleted++;
            }

            (File.Exists(counterPath) ? File.ReadAllText(counterPath) : null).Should().Be(counterBefore);
            (await repo.GetAvailableDatesAsync()).Should().NotContain(date, "a day without entries is not listed");
        }

        deleted.Should().BeGreaterThan(0);

        // Zurücklegen: jeder Papierkorb-Ordner zurück in seinen Tagesordner.
        foreach (var folder in Directory.EnumerateDirectories(trashRoot))
        {
            var record = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "geloescht.json")));
            foreach (var file in record.RootElement.GetProperty("files").EnumerateArray())
            {
                File.Move(file.GetProperty("to").GetString()!, file.GetProperty("from").GetString()!);
            }

            File.Delete(Path.Combine(folder, "geloescht.json"));
        }

        Directory.Delete(trashRoot, recursive: true);
        Snapshot(this.copy).Should().BeEquivalentTo(original, "restoring must give back the exact tree");
    }

    private static void CopyTree(string from, string to)
    {
        foreach (var dir in Directory.EnumerateDirectories(from, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(Path.Combine(to, Path.GetRelativePath(from, dir)));
        }

        Directory.CreateDirectory(to);
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, Path.Combine(to, Path.GetRelativePath(from, file)));
        }
    }

    private static Dictionary<string, string> Snapshot(string root) =>
        Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
            .ToDictionary(
                f => Path.GetRelativePath(root, f),
                f => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(f))));
}

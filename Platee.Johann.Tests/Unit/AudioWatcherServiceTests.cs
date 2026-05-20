using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

namespace Platee.Johann.Tests.Unit;

public sealed class AudioWatcherServiceTests : IDisposable
{
    private readonly string _tempDir;

    public AudioWatcherServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"JohannWatcherTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void Start_WhenInputPathIsAFile_RaisesEntryProcessingFailedAndDoesNotThrow()
    {
        var filePath = Path.Combine(_tempDir, "not-a-directory.txt");
        File.WriteAllText(filePath, "x");

        var settings = new SettingsHolder(AppSettings.Default with
        {
            Quellverzeichnis = filePath,
        });

        using var sut = new AudioWatcherService(new FakeEntryProcessor(), settings);
        var failures = new List<(string Path, Exception Error)>();
        sut.EntryProcessingFailed += (path, ex) => failures.Add((path, ex));

        var act = () => sut.Start();

        act.Should().NotThrow();
        failures.Should().ContainSingle();
        failures[0].Path.Should().Be(filePath);
        failures[0].Error.Should().BeOfType<IOException>();
    }

    private sealed class FakeEntryProcessor : IEntryProcessor
    {
        public bool CanProcess => true;

        public Task<Entry> ProcessAudioAsync(string audioFilePath, DateOnly date, IProgress<ProcessingProgress>? progress = null, CancellationToken ct = default)
            => Task.FromResult(MakeEntry());

        public Task<Entry> ReprocessAsync(Entry entry, IProgress<ProcessingProgress>? progress = null, CancellationToken ct = default)
            => Task.FromResult(entry);

        public Task<string> GenerateEmailTextAsync(Entry entry, CancellationToken ct = default)
            => Task.FromResult(string.Empty);

        public Task<Entry> ReprocessSectionAsync(Entry entry, string sectionName, IProgress<ProcessingProgress>? progress = null, CancellationToken ct = default)
            => Task.FromResult(entry);

        private static Entry MakeEntry() => new()
        {
            JobId = "test_001",
            SequenceNumber = 1,
            CreatedAt = new DateTimeOffset(new DateTime(2026, 5, 15), TimeSpan.Zero),
            Type = EntryType.Projekt,
            ProjectName = "Test",
            Title = "Test",
            SourceType = "audio",
            Status = ProcessingStatus.Empty,
        };
    }
}

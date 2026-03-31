using System.Collections.Generic;
using FluentAssertions;
using Platee.Johann.Application.Diagnostics;

namespace Platee.Johann.Tests.Unit;

public sealed class CrashLogWriterTests
{
    [Fact]
    public void ResolveLogDirectory_UsesUserProfilePeanoJohannLogs()
    {
        var result = CrashLogWriter.ResolveLogDirectory(@"C:\Users\johann");

        result.Should().Be(Path.Combine(@"C:\Users\johann", "Peano", "Johann", "logs"));
    }

    [Fact]
    public void GetLogFilePath_UsesDateSpecificFileName()
    {
        var sut = new CrashLogWriter(@"C:\Users\johann", "1.2.3", new FakeCrashLogFileSystem());

        var result = sut.GetLogFilePath(new DateOnly(2026, 03, 31));

        result.Should().Be(Path.Combine(@"C:\Users\johann", "Peano", "Johann", "logs", "johann-crash-2026-03-31.log"));
    }

    [Fact]
    public void EnsureLogDirectory_CreatesDirectory()
    {
        var fs = new FakeCrashLogFileSystem();
        var sut = new CrashLogWriter(@"C:\Users\johann", "1.2.3", fs);

        sut.EnsureLogDirectory();

        fs.CreatedDirectories.Should().ContainSingle().Which.Should().Be(sut.LogDirectory);
    }

    [Fact]
    public void WriteCrashLog_WhenAppendFails_DoesNotThrow()
    {
        var fs = new FakeCrashLogFileSystem { ThrowOnAppend = true };
        var sut = new CrashLogWriter(@"C:\Users\johann", "1.2.3", fs, () => new DateTimeOffset(2026, 03, 31, 10, 20, 30, TimeSpan.Zero));

        var act = () => sut.WriteCrashLog("DISPATCHER", new InvalidOperationException("boom"));

        act.Should().NotThrow();
        fs.CreatedDirectories.Should().Contain(sut.LogDirectory);
    }

    [Fact]
    public void WriteCrashLog_WritesHeaderAndMessage()
    {
        var fs = new FakeCrashLogFileSystem();
        var now = new DateTimeOffset(2026, 03, 31, 10, 20, 30, TimeSpan.Zero);
        var sut = new CrashLogWriter(@"C:\Users\johann", "2.0.0", fs, () => now);

        sut.WriteCrashLog("TASK", "failure");

        fs.Appends.Should().ContainSingle();
        var append = fs.Appends[0];
        append.Path.Should().Be(Path.Combine(sut.LogDirectory, "johann-crash-2026-03-31.log"));
        append.Contents.Should().Contain("Version: 2.0.0");
        append.Contents.Should().Contain("TASK: failure");
        append.Contents.Should().Contain(now.ToString("O"));
    }

    [Fact]
    public void WriteCrashLog_WhenFirstWriteFails_RetriesHeaderOnNextWrite()
    {
        var fs = new FakeCrashLogFileSystem { FailFirstAppendOnly = true };
        var now = new DateTimeOffset(2026, 03, 31, 10, 20, 30, TimeSpan.Zero);
        var sut = new CrashLogWriter(@"C:\Users\johann", "2.0.0", fs, () => now);

        sut.WriteCrashLog("TASK", "first");
        sut.WriteCrashLog("TASK", "second");

        fs.Appends.Should().ContainSingle();
        fs.Appends[0].Contents.Should().Contain("Version: 2.0.0");
        fs.Appends[0].Contents.Should().Contain("TASK: second");
    }

    private sealed class FakeCrashLogFileSystem : ICrashLogFileSystem
    {
        public List<string> CreatedDirectories { get; } = [];

        public List<(string Path, string Contents)> Appends { get; } = [];

        public bool ThrowOnAppend { get; init; }

        public bool FailFirstAppendOnly { get; init; }

        private int _appendCalls;

        public void CreateDirectory(string path) => CreatedDirectories.Add(path);

        public void AppendAllText(string path, string contents)
        {
            _appendCalls++;

            if (ThrowOnAppend || (FailFirstAppendOnly && _appendCalls == 1))
                throw new IOException("simulated append error");

            Appends.Add((path, contents));
        }
    }
}

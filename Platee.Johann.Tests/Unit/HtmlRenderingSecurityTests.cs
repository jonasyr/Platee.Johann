namespace Platee.Johann.Tests.Unit;

using System.Text;
using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Renderers;

public sealed class HtmlRenderingSecurityTests : IDisposable
{
    private readonly string tempDir;

    public HtmlRenderingSecurityTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannHtmlTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task HtmlRenderer_RenderAsync_EncodesPlainTextAndDisablesRawHtmlInMarkdown()
    {
        var entry = MakeEntry() with
        {
            Title = "O'Reilly <Admin>",
            ProjectName = "R&D's <Core>",
            Transcript = "First line\n<script>alert('x')</script>",
            Abstract = "<script>alert('x')</script>\n\n**safe**",
        };

        var sut = new HtmlRenderer();

        var result = await sut.RenderAsync(
            entry,
            new RenderOptions(this.tempDir, IncludeTranscript: true),
            CancellationToken.None);

        var html = Encoding.UTF8.GetString(result.Data);

        html.Should().Contain("<title>O&#39;Reilly &lt;Admin&gt;</title>");
        html.Should().Contain("R&amp;D&#39;s &lt;Core&gt;");
        html.Should().Contain("First line<br>&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;");
        html.Should().Contain("<strong>safe</strong>");
        html.Should().NotContain("<script>alert('x')</script>");
    }

    [Fact]
    public async Task HtmlOverviewService_RegenerateAsync_EncodesMetadataAndDisablesRawHtmlInContent()
    {
        var entry = MakeEntry() with
        {
            Title = "Plan 'A' <Review>",
            ProjectName = "Ops & 'QA'",
            Abstract = "<img src=x onerror=alert('x')>\n\nText",
        };

        var repository = new FakeEntryRepository(entry);
        var sut = new HtmlOverviewService(repository, this.tempDir);
        var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);

        await sut.RegenerateAsync(date, CancellationToken.None);

        var path = Path.Combine(this.tempDir, date.ToString("yyyy-MM-dd"), "_ItemÜbersicht.html");
        var html = await File.ReadAllTextAsync(path, Encoding.UTF8);

        html.Should().Contain("Plan &#39;A&#39; &lt;Review&gt;");
        html.Should().Contain("Ops &amp; &#39;QA&#39;");
        html.Should().Contain("&lt;img src=x onerror=alert('x')&gt;");
        html.Should().NotContain("<img src=x onerror=alert('x')>");
    }

    private static Entry MakeEntry() => new()
    {
        JobId = "test_001",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(new DateTime(2026, 5, 15, 9, 30, 0, DateTimeKind.Local)),
        Type = EntryType.Projekt,
        ProjectName = "Test",
        Title = "Test Entry",
        SourceType = "text",
        Status = ProcessingStatus.Empty,
    };

    private sealed class FakeEntryRepository : IEntryRepository
    {
        private readonly IReadOnlyList<Entry> entries;

        public FakeEntryRepository(params Entry[] entries)
        {
            this.entries = entries;
        }

        public Task<IReadOnlyList<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DateOnly>>([]);

        public Task<IReadOnlyList<Entry>> GetEntriesForDateAsync(DateOnly date, CancellationToken ct = default)
            => Task.FromResult(this.entries.Where(e => DateOnly.FromDateTime(e.CreatedAt.DateTime) == date).ToList() as IReadOnlyList<Entry>);

        public Task<Entry?> GetByJobIdAsync(string jobId, CancellationToken ct = default)
            => Task.FromResult(this.entries.FirstOrDefault(e => e.JobId == jobId));

        public Task SaveAsync(Entry entry, CancellationToken ct = default)
            => Task.CompletedTask;

        public Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default)
            => Task.FromResult(1);

        public Task MigrateJobIdsAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}

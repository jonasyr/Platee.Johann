namespace Platee.Johann.Tests.Unit;

using System.Text;
using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Renderers;

/// <summary>
/// Covers rendering of user-defined categories (<see cref="Entry.CustomSections"/>) into the
/// HTML and PDF exports. The escaping tests are the important ones: custom section text is user
/// content and must travel the same encoder as every built-in section.
/// </summary>
public sealed class CustomSectionRenderingTests : IDisposable
{
    private readonly string tempDir;

    public CustomSectionRenderingTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannCustomSectionTests_{Guid.NewGuid():N}");
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
    public async Task HtmlRenderer_IncludesCustomSectionTextAndDisplayName()
    {
        var sut = new HtmlRenderer();
        var options = new RenderOptions(this.tempDir, false, true)
        {
            CustomSectionNames = new Dictionary<string, string> { ["custom.prog"] = "Programmierung" },
        };

        var result = await sut.RenderAsync(this.EntryWithCustom(), options, CancellationToken.None);

        var html = Encoding.UTF8.GetString(result.Data);
        html.Should().Contain("PROGRAMMIER-INHALT");
        html.Should().Contain("Programmierung");
    }

    [Fact]
    public async Task HtmlRenderer_FallsBackToCategoryIdWhenNoDisplayNameIsKnown()
    {
        var sut = new HtmlRenderer();

        var result = await sut.RenderAsync(
            this.EntryWithCustom(),
            new RenderOptions(this.tempDir, false, true),
            CancellationToken.None);

        Encoding.UTF8.GetString(result.Data).Should().Contain("custom.prog");
    }

    [Fact]
    public async Task HtmlRenderer_EscapesCustomSectionContent()
    {
        var entry = this.EntryWithCustom() with
        {
            CustomSections = new Dictionary<string, string>
            {
                ["custom.prog"] = "<script>alert(1)</script>",
            },
        };

        var sut = new HtmlRenderer();

        var result = await sut.RenderAsync(
            entry,
            new RenderOptions(this.tempDir, false, true),
            CancellationToken.None);

        var html = Encoding.UTF8.GetString(result.Data);
        html.Should().NotContain(
            "<script>alert(1)</script>",
            "custom sections are user content and must be escaped like every other section");
        html.Should().Contain("&lt;script&gt;alert(1)&lt;/script&gt;");
    }

    [Fact]
    public async Task HtmlRenderer_EscapesCustomSectionHeading()
    {
        var sut = new HtmlRenderer();
        var options = new RenderOptions(this.tempDir, false, true)
        {
            CustomSectionNames = new Dictionary<string, string>
            {
                ["custom.prog"] = "<img src=x onerror=alert(1)>",
            },
        };

        var result = await sut.RenderAsync(this.EntryWithCustom(), options, CancellationToken.None);

        var html = Encoding.UTF8.GetString(result.Data);
        html.Should().NotContain("<img src=x onerror=alert(1)>");
        html.Should().Contain("&lt;img src=x onerror=alert(1)&gt;");
    }

    [Fact]
    public async Task HtmlRenderer_OmitsCustomSectionWhenVisibilityIsFalse()
    {
        var sut = new HtmlRenderer();
        var options = new RenderOptions(this.tempDir, false, true)
        {
            CustomSectionVisibility = new Dictionary<string, bool> { ["custom.prog"] = false },
        };

        var result = await sut.RenderAsync(this.EntryWithCustom(), options, CancellationToken.None);

        Encoding.UTF8.GetString(result.Data).Should().NotContain("PROGRAMMIER-INHALT");
    }

    [Fact]
    public async Task HtmlRenderer_SkipsBlankCustomSection()
    {
        var entry = this.EntryWithCustom() with
        {
            CustomSections = new Dictionary<string, string>
            {
                ["custom.blank"] = "   ",
            },
        };

        var sut = new HtmlRenderer();
        var options = new RenderOptions(this.tempDir, false, true)
        {
            CustomSectionNames = new Dictionary<string, string> { ["custom.blank"] = "Leer" },
        };

        var result = await sut.RenderAsync(entry, options, CancellationToken.None);

        Encoding.UTF8.GetString(result.Data).Should().NotContain("Leer");
    }

    [Fact]
    public async Task HtmlRenderer_RendersCustomSectionsInStableIdOrder()
    {
        var entry = this.EntryWithCustom() with
        {
            CustomSections = new Dictionary<string, string>
            {
                ["custom.zebra"] = "ZEBRA-INHALT",
                ["custom.alpha"] = "ALPHA-INHALT",
            },
        };

        var sut = new HtmlRenderer();
        var result = await sut.RenderAsync(
            entry,
            new RenderOptions(this.tempDir, false, true),
            CancellationToken.None);

        var html = Encoding.UTF8.GetString(result.Data);
        html.IndexOf("ALPHA-INHALT", StringComparison.Ordinal)
            .Should().BeLessThan(html.IndexOf("ZEBRA-INHALT", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PdfRenderer_ProducesDocumentContainingTheCustomSection()
    {
        var holder = new SettingsHolder(new AppSettings(), new PromptSettings());
        var sut = new PdfRenderer(holder);
        var options = new RenderOptions(this.tempDir, false, true)
        {
            CustomSectionNames = new Dictionary<string, string> { ["custom.prog"] = "Programmierung" },
        };

        var withCustom = await sut.RenderAsync(this.EntryWithCustom(), options, CancellationToken.None);

        var withoutCustom = await sut.RenderAsync(
            this.EntryWithCustom() with { CustomSections = new Dictionary<string, string>() },
            options,
            CancellationToken.None);

        withCustom.MimeType.Should().Be("application/pdf");
        withCustom.Data.Should().NotBeEmpty();
        withCustom.Data.Length.Should().BeGreaterThan(
            withoutCustom.Data.Length,
            "the custom section adds content to the document");
    }

    [Fact]
    public async Task PdfRenderer_OmitsCustomSectionWhenVisibilityIsFalse()
    {
        var holder = new SettingsHolder(new AppSettings(), new PromptSettings());
        var sut = new PdfRenderer(holder);

        var hidden = await sut.RenderAsync(
            this.EntryWithCustom(),
            new RenderOptions(this.tempDir, false, true)
            {
                CustomSectionVisibility = new Dictionary<string, bool> { ["custom.prog"] = false },
            },
            CancellationToken.None);

        var none = await sut.RenderAsync(
            this.EntryWithCustom() with { CustomSections = new Dictionary<string, string>() },
            new RenderOptions(this.tempDir, false, true),
            CancellationToken.None);

        hidden.Data.Length.Should().Be(none.Data.Length);
    }

    private Entry EntryWithCustom() => new()
    {
        JobId = "260907_001_abcd1234",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(new DateTime(2026, 9, 7, 10, 0, 0, DateTimeKind.Local)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Test",
        SourceType = "audio",
        Status = ProcessingStatus.Empty,
        Transcript = "transkript",
        CustomSections = new Dictionary<string, string>
        {
            ["custom.prog"] = "PROGRAMMIER-INHALT",
        },
    };
}

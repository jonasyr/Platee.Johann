using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Covers audit finding M3: a failed drag-to-export returned null and the drag
/// simply did nothing, with no message anywhere in the UI.
/// </summary>
public sealed class EntryDetailDragExportTests
{
    private readonly List<string> logged = [];

    private EntryDetailViewModel CreateVm(params IEntryRenderer[] renderers) =>
        new(renderers, Path.GetTempPath(), addLog: (message, _) =>
        {
            this.logged.Add(message);
            return new ProcessLogItem(message, DateTime.Now, false);
        });

    [Fact]
    public async Task RenderPdfForDragAsync_WhenRendererThrows_ReportsTheFailure()
    {
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("Datenträger voll"));
        var vm = this.CreateVm(renderer);

        var result = await vm.RenderPdfForDragAsync(MakeEntry(), CancellationToken.None);

        result.Should().BeNull();
        this.logged.Should().ContainSingle()
            .Which.Should().StartWith("Fehler:").And.Contain("Datenträger voll");
    }

    [Fact]
    public async Task RenderPdfForDragAsync_WhenNoPdfRenderer_ReportsTheFailure()
    {
        var vm = this.CreateVm();

        var result = await vm.RenderPdfForDragAsync(MakeEntry(), CancellationToken.None);

        result.Should().BeNull();
        this.logged.Should().ContainSingle().Which.Should().StartWith("Fehler:");
    }

    [Fact]
    public async Task RenderPdfForDragAsync_WhenCancelled_StaysQuiet()
    {
        // Letting go of the mouse mid-render is not a failure worth a red toast.
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());
        var vm = this.CreateVm(renderer);

        var result = await vm.RenderPdfForDragAsync(MakeEntry(), CancellationToken.None);

        result.Should().BeNull();
        this.logged.Should().BeEmpty();
    }

    [Fact]
    public async Task RenderPdfForDragAsync_OnSuccess_ReportsNothingAndReturnsThePath()
    {
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .Returns(new RenderResult([], "application/pdf", "entry.pdf"));
        var vm = this.CreateVm(renderer);

        var result = await vm.RenderPdfForDragAsync(MakeEntry(), CancellationToken.None);

        result.Should().EndWith("entry.pdf");
        this.logged.Should().BeEmpty();
    }

    private static Entry MakeEntry() => new()
    {
        JobId = "260904_001_abcdef12",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(new DateTime(2026, 9, 4)),
        Type = EntryType.Projekt,
        ProjectName = "Test",
        Title = "Test Entry",
        SourceType = "text",
        Status = ProcessingStatus.Empty,
    };
}

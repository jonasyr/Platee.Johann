namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// The three states one custom-section row can be in. An orphaned row is text whose
/// category was deleted — it must stay visible and read-only rather than disappear.
/// </summary>
public sealed class SectionRowViewModelTests
{
    [Fact]
    public void Row_WithText_IsGeneratedAndNotOrphaned()
    {
        var row = new SectionRowViewModel("custom.a", "A", "inhalt", isConfigured: true);

        row.IsGenerated.Should().BeTrue();
        row.IsOrphaned.Should().BeFalse();
        row.CanGenerate.Should().BeFalse();
    }

    [Fact]
    public void Row_WithoutText_ShowsGenerateAffordance()
    {
        var row = new SectionRowViewModel("custom.a", "A", text: null, isConfigured: true);

        row.IsGenerated.Should().BeFalse();
        row.IsOrphaned.Should().BeFalse();
        row.CanGenerate.Should().BeTrue();
    }

    [Fact]
    public void Row_WithWhitespaceOnly_CountsAsNotGenerated()
    {
        var row = new SectionRowViewModel("custom.a", "A", "   \r\n ", isConfigured: true);

        row.IsGenerated.Should().BeFalse();
        row.CanGenerate.Should().BeTrue();
    }

    [Fact]
    public void Row_WithTextButNoLongerConfigured_IsOrphaned()
    {
        var row = new SectionRowViewModel("custom.weg", "Weg", "alter inhalt", isConfigured: false);

        row.IsOrphaned.Should().BeTrue();
        row.IsGenerated.Should().BeTrue();

        // An orphan offers no "Generieren" button: there is no prompt left to run.
        row.CanGenerate.Should().BeFalse();
    }

    [Fact]
    public void SettingText_RaisesTheDerivedStateChanges()
    {
        var row = new SectionRowViewModel("custom.a", "A", text: null, isConfigured: true);
        var changed = new List<string?>();
        row.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        row.Text = "frisch generiert";

        changed.Should().Contain(nameof(SectionRowViewModel.IsGenerated))
            .And.Contain(nameof(SectionRowViewModel.CanGenerate))
            .And.Contain(nameof(SectionRowViewModel.IsOrphaned));
        row.IsGenerated.Should().BeTrue();
    }
}

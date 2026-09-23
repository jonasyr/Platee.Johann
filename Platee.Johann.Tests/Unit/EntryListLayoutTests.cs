namespace Platee.Johann.Tests.Unit;

using System.Xml.Linq;
using FluentAssertions;

/// <summary>
/// Die Eintragsliste in <c>MainWindow.xaml</c> (#96). Mit waagerechter Bildlaufleiste misst WPF
/// die Zeilen mit unendlicher Breite: der Titel wird nie gekürzt, und der Erledigt-Haken am
/// rechten Rand liegt außerhalb des sichtbaren Bereichs. Geprüft wird die XAML selbst —
/// eine Oberfläche lässt sich hier nicht starten.
/// </summary>
public sealed class EntryListLayoutTests
{
    private static readonly XNamespace Wpf = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace X = "http://schemas.microsoft.com/winfx/2006/xaml";

    private readonly XElement listBox = LoadEntryListBox();

    [Fact]
    public void The_entry_list_never_scrolls_sideways()
    {
        ((string?)this.listBox.Attribute("ScrollViewer.HorizontalScrollBarVisibility"))
            .Should().Be("Disabled", "only then do rows take the column width and the title gets trimmed");
    }

    [Fact]
    public void A_trimmed_title_shows_in_full_as_a_tooltip()
    {
        var title = this.RowTemplate().Descendants(Wpf + "TextBlock")
            .Single(t => t.Descendants(Wpf + "Run").Any(r => ((string?)r.Attribute("Text"))?.Contains("Title") == true));

        ((string?)title.Attribute("TextTrimming")).Should().Be("CharacterEllipsis");
        ((string?)title.Attribute("ToolTip")).Should().Contain("Title");
    }

    [Fact]
    public void Row_colours_come_from_the_theme_so_their_contrast_is_tested()
    {
        // A literal colour here escapes ControlContrastTests (#AAAAAA for the number was 2.3 : 1).
        var literal = this.RowTemplate().DescendantsAndSelf()
            .SelectMany(e => e.Attributes())
            .Where(a => a.Name.LocalName is "Foreground" or "Background" && a.Value.StartsWith('#'))
            .Select(a => $"{a.Parent!.Name.LocalName}.{a.Name.LocalName}={a.Value}");

        literal.Should().BeEmpty();
    }

    [Fact]
    public void Both_column_dividers_fit_their_column_on_double_click()
    {
        // Like double-clicking a column border in Excel: the date and the entry column each
        // take the width of their widest content.
        var splitters = this.listBox.Document!.Descendants(Wpf + "GridSplitter").ToList();

        splitters.Should().HaveCount(2);
        splitters.Should().OnlyContain(s => s.Attribute("MouseDoubleClick") != null);
        splitters.Select(s => (string?)s.Attribute("ToolTip") ?? string.Empty)
            .Should().OnlyContain(tip => tip.Contains("Doppelklick"), "the gesture is invisible otherwise");
    }

    [Fact]
    public void Every_entry_row_exists_so_the_double_click_measures_them_all()
    {
        // The fit measures the rows where they live; a virtualising list builds only the
        // visible ones, and a long title further down would stay trimmed.
        ((string?)this.listBox.Attribute("VirtualizingPanel.IsVirtualizing")).Should().Be("False");
    }

    private XElement RowTemplate() =>
        this.listBox.Element(Wpf + "ListBox.ItemTemplate")!.Element(Wpf + "DataTemplate")!;

    private static XElement LoadEntryListBox()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Platee.Johann.slnx")))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("the tests run below the repository, next to Platee.Johann.slnx");
        var document = XDocument.Load(Path.Combine(dir!.FullName, "Platee.Johann.UI", "MainWindow.xaml"));
        return document.Descendants(Wpf + "ListBox").Single(e => (string?)e.Attribute(X + "Name") == "EntryListBox");
    }
}

using System.Globalization;
using System.Linq;
using System.Windows.Documents;
using FluentAssertions;
using Platee.Johann.UI.Converters;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Verschachtelte Aufzählungen wurden flachgeklopft.
/// <para>
/// Der Wandler erkannte Stichpunkte am getrimmten Text und warf die Einrückung damit weg.
/// Eine Gliederung wie „ChatGPT soll die Diktate:" mit Unterpunkten erschien deshalb als
/// eine einzige flache Liste — die Struktur, die das Modell erzeugt hatte, ging in der
/// Anzeige verloren. Mit den schwächeren Modellen fiel es nicht auf, weil sie kaum
/// verschachtelten.
/// </para>
/// </summary>
public sealed class MarkdownNestedListTests
{
    private static FlowDocument Convert(string markdown)
        => (FlowDocument)new MarkdownFlowDocumentConverter()
            .Convert(markdown, typeof(FlowDocument), null!, CultureInfo.InvariantCulture);

    [Fact]
    public void A_flat_list_stays_one_level()
    {
        var doc = Convert("- eins\n- zwei\n- drei");

        var list = doc.Blocks.OfType<List>().Should().ContainSingle().Subject;
        list.ListItems.Should().HaveCount(3);
        list.ListItems.Should().OnlyContain(i => !i.Blocks.OfType<List>().Any());
    }

    [Fact]
    public void An_indented_bullet_becomes_a_child_of_the_one_above_it()
    {
        var doc = Convert("- oben\n  - darunter\n- wieder oben");

        var list = doc.Blocks.OfType<List>().Should().ContainSingle().Subject;
        list.ListItems.Should().HaveCount(2, "die eingerückte Zeile ist kein eigener Punkt");

        var nested = list.ListItems.First().Blocks.OfType<List>().Should().ContainSingle().Subject;
        nested.ListItems.Should().ContainSingle();
    }

    [Fact]
    public void Two_levels_of_indentation_nest_twice()
    {
        var doc = Convert("- eins\n  - zwei\n    - drei");

        var level1 = doc.Blocks.OfType<List>().Single();
        var level2 = level1.ListItems.Single().Blocks.OfType<List>().Single();
        var level3 = level2.ListItems.Single().Blocks.OfType<List>().Single();

        level3.ListItems.Should().ContainSingle();
    }

    [Fact]
    public void A_nested_bullet_without_a_parent_does_not_get_lost()
    {
        // Modelle rücken auch mal ein, ohne vorher einen Oberpunkt zu setzen.
        // Der Text darf dabei nicht verschwinden.
        var doc = Convert("  - allein eingerückt");

        var list = doc.Blocks.OfType<List>().Should().ContainSingle().Subject;
        list.ListItems.Should().ContainSingle();
    }

    [Fact]
    public void Tabs_count_as_indentation_too()
    {
        var doc = Convert("- oben\n\t- darunter");

        var list = doc.Blocks.OfType<List>().Single();
        list.ListItems.Should().HaveCount(1);
        list.ListItems.Single().Blocks.OfType<List>().Should().ContainSingle();
    }
}

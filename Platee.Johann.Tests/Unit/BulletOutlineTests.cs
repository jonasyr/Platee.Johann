using FluentAssertions;
using Platee.Johann.Domain.Services;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Einrückung → Ebene für Aufzählungen im PDF (#83).
/// <para>
/// Das PDF erkannte Stichpunkte nur ohne Einrückung; ein Unterpunkt erschien als Absatz mit
/// Bindestrich. Die Fälle spiegeln <see cref="MarkdownNestedListTests"/>, damit Detailansicht und
/// PDF dieselbe Verschachtelung zeigen.
/// </para>
/// </summary>
public sealed class BulletOutlineTests
{
    private static int[] Levels(params int[] indents) => [.. BulletOutline.AssignLevels(indents)];

    [Fact]
    public void A_flat_list_stays_one_level()
        => Levels(0, 0, 0).Should().Equal(0, 0, 0);

    [Fact]
    public void An_indented_bullet_becomes_a_child_of_the_one_above_it()
        => Levels(0, 2, 0).Should().Equal(0, 1, 0);

    [Fact]
    public void Two_and_four_space_indentation_nest_the_same_way()
    {
        Levels(0, 2, 4).Should().Equal(0, 1, 2);
        Levels(0, 4, 8).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void A_nested_bullet_without_a_parent_does_not_get_lost()
        => Levels(2).Should().Equal(0);

    [Fact]
    public void A_deeper_jump_goes_only_one_level_down()
        => Levels(0, 8).Should().Equal(0, 1);

    [Fact]
    public void Returning_to_a_middle_indent_closes_the_deeper_levels()
        => Levels(0, 4, 8, 4, 0).Should().Equal(0, 1, 2, 1, 0);

    [Fact]
    public void An_indent_between_two_open_levels_nests_under_the_shallower_one()
        => Levels(0, 4, 2).Should().Equal(0, 1, 1);

    [Theory]
    [InlineData("- eins", 0, "eins")]
    [InlineData("* eins", 0, "eins")]
    [InlineData("+ eins", 0, "eins")]
    [InlineData("  - darunter", 2, "darunter")]
    [InlineData("    - tiefer", 4, "tiefer")]
    [InlineData("\t- mit Tab", 2, "mit Tab")]
    public void Bullets_are_recognised_with_their_indent(string line, int indent, string text)
    {
        BulletOutline.TryParse(line, out var bullet).Should().BeTrue();
        bullet.Should().Be(new BulletLine(indent, text));
    }

    [Theory]
    [InlineData("- [ ] Angebot schicken", "Angebot schicken")]
    [InlineData("- [x] Angebot schicken", "Angebot schicken")]
    [InlineData("  - [X] erledigt", "erledigt")]
    public void Checkbox_markers_never_reach_the_text(string line, string text)
    {
        BulletOutline.TryParse(line, out var bullet).Should().BeTrue();
        bullet.Text.Should().Be(text);
    }

    [Theory]
    [InlineData("**Fett am Zeilenanfang**")]
    [InlineData("Normaler Satz - mit Gedankenstrich")]
    [InlineData("-ohne Leerzeichen")]
    [InlineData("### Überschrift")]
    [InlineData("")]
    public void Other_lines_are_not_bullets(string line)
        => BulletOutline.TryParse(line, out _).Should().BeFalse();
}

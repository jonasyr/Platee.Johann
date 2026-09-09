namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Settings;

public sealed class CategoryIdFactoryTests
{
    [Fact]
    public void Create_SlugsGermanUmlautsAndLowercases()
    {
        Create("Gesprächs Notiz Ü").Should().Be("custom.gespraechs-notiz-ue-0001");
    }

    [Fact]
    public void Create_CollapsesRunsOfSeparators()
    {
        Create("Code   //  Review").Should().Be("custom.code-review-0001");
    }

    [Fact]
    public void Create_RetriesWhenTheSuffixIsAlreadyTaken()
    {
        string[] existing = ["custom.programmierung-0001"];

        Create("Programmierung", existing).Should().Be("custom.programmierung-0002");
    }

    [Fact]
    public void Create_SkipsEverySuffixAlreadyInUse()
    {
        string[] existing = ["custom.notiz-0001", "custom.notiz-0002", "custom.notiz-0003"];

        Create("Notiz", existing).Should().Be("custom.notiz-0004");
    }

    [Fact]
    public void Create_CollisionCheckIsCaseInsensitive()
    {
        string[] existing = ["CUSTOM.NOTIZ-0001"];

        Create("Notiz", existing).Should().Be("custom.notiz-0002");
    }

    [Fact]
    public void Create_NeverCollidesWithBuiltInIds()
    {
        var id = Create("longSummary");

        BuiltInSections.All.Should().NotContain(id);
        id.Should().StartWith("custom.");
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("   ")]
    [InlineData("")]
    public void Create_EmptyOrPunctuationOnlyName_FallsBackToGenericSlug(string name)
    {
        Create(name).Should().Be("custom.kategorie-0001");
    }

    [Fact]
    public void Create_TrimsLeadingAndTrailingSeparators()
    {
        Create("  -Analyse-  ").Should().Be("custom.analyse-0001");
    }

    [Fact]
    public void Create_NeverReturnsTheBareSlug()
    {
        // The suffix is what stops a recreated category from inheriting a deleted one's id
        // (and with it, the deleted category's orphaned text).
        CategoryIdFactory.Create("Projektnotiz", []).Should().NotBe("custom.projektnotiz");
    }

    [Fact]
    public void Create_SecondCategoryWithTheSameName_GetsADifferentId()
    {
        var first = CategoryIdFactory.Create("Projektnotiz", []);

        CategoryIdFactory.Create("Projektnotiz", [first]).Should().NotBe(first);
    }

    [Fact]
    public void Create_DefaultSuffixIsShortAndReadable()
    {
        var id = CategoryIdFactory.Create("Projektnotiz", []);

        id.Should().MatchRegex(@"^custom\.projektnotiz-[0-9a-f]{4}$");
    }

    /// <summary>Pins the suffix so the slugging rules stay the subject of the test.</summary>
    private static string Create(string name, IEnumerable<string>? existing = null)
    {
        var n = 0;
        return CategoryIdFactory.Create(name, existing ?? [], () => (++n).ToString("d4"));
    }
}

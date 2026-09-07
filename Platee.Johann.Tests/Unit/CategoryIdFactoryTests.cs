namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Settings;

public sealed class CategoryIdFactoryTests
{
    [Fact]
    public void Create_SlugsGermanUmlautsAndLowercases()
    {
        CategoryIdFactory.Create("Gesprächs Notiz Ü", []).Should().Be("custom.gespraechs-notiz-ue");
    }

    [Fact]
    public void Create_CollapsesRunsOfSeparators()
    {
        CategoryIdFactory.Create("Code   //  Review", []).Should().Be("custom.code-review");
    }

    [Fact]
    public void Create_AppendsCounterOnCollision()
    {
        string[] existing = ["custom.programmierung"];

        CategoryIdFactory.Create("Programmierung", existing).Should().Be("custom.programmierung-2");
    }

    [Fact]
    public void Create_SkipsAlreadyTakenCounters()
    {
        string[] existing = ["custom.notiz", "custom.notiz-2", "custom.notiz-3"];

        CategoryIdFactory.Create("Notiz", existing).Should().Be("custom.notiz-4");
    }

    [Fact]
    public void Create_CollisionCheckIsCaseInsensitive()
    {
        string[] existing = ["CUSTOM.NOTIZ"];

        CategoryIdFactory.Create("Notiz", existing).Should().Be("custom.notiz-2");
    }

    [Fact]
    public void Create_NeverCollidesWithBuiltInIds()
    {
        var id = CategoryIdFactory.Create("longSummary", []);

        BuiltInSections.All.Should().NotContain(id);
        id.Should().StartWith("custom.");
    }

    [Theory]
    [InlineData("!!!")]
    [InlineData("   ")]
    [InlineData("")]
    public void Create_EmptyOrPunctuationOnlyName_FallsBackToGenericSlug(string name)
    {
        CategoryIdFactory.Create(name, []).Should().Be("custom.kategorie");
    }

    [Fact]
    public void Create_TrimsLeadingAndTrailingSeparators()
    {
        CategoryIdFactory.Create("  -Analyse-  ", []).Should().Be("custom.analyse");
    }
}

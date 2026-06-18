using FluentAssertions;
using Platee.Johann.Domain.ValueObjects;
using Xunit;

namespace Platee.Johann.Tests.Unit;

public sealed class CorrectionEntryTests
{
    [Fact]
    public void CorrectionEntry_StoresWrongAndCorrect()
    {
        var entry = new CorrectionEntry { Wrong = "Piano", Correct = "Peano" };

        entry.Wrong.Should().Be("Piano");
        entry.Correct.Should().Be("Peano");
    }

    [Fact]
    public void CorrectionEntry_IsImmutable_WithExpressionCreatesNewInstance()
    {
        var original = new CorrectionEntry { Wrong = "Piano", Correct = "Peano" };
        var updated = original with { Wrong = "Nele" };

        updated.Wrong.Should().Be("Nele");
        updated.Correct.Should().Be("Peano");
        original.Wrong.Should().Be("Piano");
    }

    [Fact]
    public void CorrectionEntry_ValueEquality()
    {
        var a = new CorrectionEntry { Wrong = "Piano", Correct = "Peano" };
        var b = new CorrectionEntry { Wrong = "Piano", Correct = "Peano" };

        a.Should().Be(b);
    }

    [Fact]
    public void AppSettings_Default_HasPrebuiltKorrekturliste()
    {
        var settings = Platee.Johann.Application.Settings.AppSettings.Default;

        settings.Korrekturliste.Should().NotBeNull();
        settings.Korrekturliste.Should().HaveCount(2);
        settings.Korrekturliste[0].Wrong.Should().Be("Piano");
        settings.Korrekturliste[0].Correct.Should().Be("Peano");
        settings.Korrekturliste[1].Wrong.Should().Be("Nele");
        settings.Korrekturliste[1].Correct.Should().Be("Neele");
    }
}

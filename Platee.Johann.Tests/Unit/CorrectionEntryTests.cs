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
    public void AppSettings_Default_HasEmptyKorrekturliste()
    {
        var settings = Platee.Johann.Application.Settings.AppSettings.Default;

        settings.Korrekturliste.Should().NotBeNull();
        settings.Korrekturliste.Should().BeEmpty();
    }
}

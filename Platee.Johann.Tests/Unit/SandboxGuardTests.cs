namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Sandbox;
using Xunit;

public sealed class SandboxGuardTests
{
    private static readonly string[] Forbidden = [@"Z:\", @"C:\Users\JW\Documents\Johann"];

    [Fact]
    public void CleanSandbox_HasNoViolations()
    {
        var json = """
            {"quellverzeichnis":"C:\\Temp\\sb\\eingang","archivverzeichnis":"C:\\Temp\\sb\\eingang\\Archiv","ausgabeverzeichnis":"C:\\Temp\\sb\\output","globalPromptFilePath":"C:\\Temp\\sb\\team\\prompts.json"}
            """;
        SandboxGuard.Violations(json, Forbidden).Should().BeEmpty();
    }

    [Theory]
    [InlineData("""{"globalPromptFilePath":"Z:\\12_Tools\\Peano\\Johann\\prompts.json"}""")]
    [InlineData("""{"ausgabeverzeichnis":"C:\\Users\\JW\\Documents\\Johann\\output"}""")]
    [InlineData("""{"ausgabeverzeichnis":"c:/users/jw/documents/johann/output"}""")]
    [InlineData("""{"nested":{"x":["C:\\Users\\JW\\Documents\\Johann"]}}""")]
    [InlineData("""{"globalPromptFilePath":"z:/12_Tools/p.json"}""")]
    public void ForbiddenPath_AnywhereInJson_IsViolation(string json)
    {
        SandboxGuard.Violations(json, Forbidden).Should().NotBeEmpty();
    }

    [Fact]
    public void BrokenJson_IsViolation()
    {
        SandboxGuard.Violations("{kaputt", Forbidden).Should().ContainSingle().Which.Should().Contain("nicht lesbar");
    }

    [Fact]
    public void SiblingFolderWithSimilarPrefix_IsNotAViolation()
    {
        var json = """
            {"quellverzeichnis":"C:\\Users\\JW\\Documents\\JohannX\\eingang","archivverzeichnis":"C:\\Users\\JW\\Documents\\JohannX\\eingang\\Archiv","ausgabeverzeichnis":"C:\\Users\\JW\\Documents\\JohannX\\output"}
            """;
        SandboxGuard.Violations(json, Forbidden).Should().BeEmpty();
    }

    [Fact]
    public void MissingFolderKey_IsViolation()
    {
        var json = """
            {"quellverzeichnis":"C:\\Temp\\sb\\eingang","archivverzeichnis":"C:\\Temp\\sb\\eingang\\Archiv","globalPromptFilePath":"C:\\Temp\\sb\\team\\prompts.json"}
            """;
        SandboxGuard.Violations(json, Forbidden).Should().ContainSingle(v => v.Contains("ausgabeverzeichnis"));
    }

    [Fact]
    public void EmptyFolderKey_IsViolation()
    {
        var json = """
            {"quellverzeichnis":"C:\\Temp\\sb\\eingang","archivverzeichnis":"","ausgabeverzeichnis":"C:\\Temp\\sb\\output"}
            """;
        SandboxGuard.Violations(json, Forbidden).Should().Contain(v => v.Contains("archivverzeichnis"));
    }
}

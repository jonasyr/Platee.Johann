namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Automation;

public sealed class UiTreeDumpTests
{
    [Fact]
    public void ToJson_IsIndented() =>
        UiTreeDump.ToJson([new UiNode("Button", "btnOk", null, true, false, "0,0,10,10", [])])
            .Should().Contain("\n  ");

    [Fact]
    public void ToJson_IncludesPopulatedFields()
    {
        var json = UiTreeDump.ToJson([new UiNode("Button", "btnOk", null, true, false, "0,0,10,10", [])]);

        json.Should().Contain("\"ControlType\": \"Button\"");
        json.Should().Contain("\"AutomationId\": \"btnOk\"");
        json.Should().Contain("\"IsEnabled\": true");
        json.Should().Contain("\"IsOffscreen\": false");
        json.Should().Contain("\"Bounds\": \"0,0,10,10\"");
    }

    [Fact]
    public void ToJson_OmitsNullFields()
    {
        var json = UiTreeDump.ToJson([new UiNode("Button", null, null, true, false, "0,0,10,10", [])]);

        json.Should().NotContain("\"AutomationId\"");
        json.Should().NotContain("\"Name\"");
    }

    [Fact]
    public void ToJson_PreservesUmlautsUnescaped()
    {
        var json = UiTreeDump.ToJson([new UiNode("Text", null, "Diktieren äöü", true, false, "0,0,1,1", [])]);

        json.Should().Contain("Diktieren äöü");
    }

    [Fact]
    public void ToJson_IncludesNestedChildren()
    {
        var child = new UiNode("Text", null, "Kind", true, false, "0,0,1,1", []);
        var parent = new UiNode("Group", "grp", null, true, false, "0,0,5,5", [child]);

        var json = UiTreeDump.ToJson([parent]);

        json.Should().Contain("\"Children\"");
        json.Should().Contain("\"Kind\"");
    }

    [Fact]
    public void ToJson_OmitsEmptyChildren() =>
        UiTreeDump.ToJson([new UiNode("Button", "btnOk", null, true, false, "0,0,10,10", [])])
            .Should().NotContain("\"Children\"");
}

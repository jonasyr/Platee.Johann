namespace Platee.Johann.UiDriver.Automation;

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Renders a captured UI tree as indented JSON for the <c>ui-driver tree</c> command. Null
/// fields and empty child lists are left out entirely rather than serialised as <c>null</c>/<c>[]</c>,
/// and umlauts are written literally (<see cref="JavaScriptEncoder.UnsafeRelaxedJsonEscaping"/>)
/// so the console output stays human-readable.
/// </summary>
public static class UiTreeDump
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToJson(IEnumerable<UiNode> nodes)
    {
        var array = new JsonArray([.. nodes.Select(ToJsonObject)]);
        return array.ToJsonString(Options);
    }

    private static JsonObject ToJsonObject(UiNode node)
    {
        var obj = new JsonObject
        {
            ["ControlType"] = node.ControlType,
        };

        if (node.AutomationId is not null)
        {
            obj["AutomationId"] = node.AutomationId;
        }

        if (node.Name is not null)
        {
            obj["Name"] = node.Name;
        }

        obj["IsEnabled"] = node.IsEnabled;
        obj["IsOffscreen"] = node.IsOffscreen;
        obj["Bounds"] = node.Bounds;

        if (node.Children.Count > 0)
        {
            obj["Children"] = new JsonArray([.. node.Children.Select(ToJsonObject)]);
        }

        return obj;
    }
}

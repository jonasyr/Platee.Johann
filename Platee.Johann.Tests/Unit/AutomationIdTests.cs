namespace Platee.Johann.Tests.Unit;

using System.Xml.Linq;
using FluentAssertions;

/// <summary>
/// Stable <c>AutomationProperties.AutomationId</c>s on every operable control (#111), so the
/// UI-automation tests in later tasks can find them without depending on visible text or
/// window structure. Parses the XAML with <see cref="XDocument"/> like
/// <c>EntryListLayoutTests</c>/<c>ReleaseNotesButtonTests</c> — a window cannot be started here.
/// <para>
/// A bound id (a <c>{Binding …, StringFormat=Prefix.{0}}</c> markup extension, used for
/// per-section and per-category templates) is exempt from the duplicate check: the same XAML
/// text is legitimately repeated once per template, and produces a different id per instance
/// only once real data flows through the binding at runtime.
/// </para>
/// </summary>
public sealed class AutomationIdTests
{
    private const string AutomationId = "AutomationProperties.AutomationId";

    /// <summary>Element types rule (a) applies to.</summary>
    private static readonly string[] CheckedElementNames = ["Button", "CheckBox", "ComboBox", "TextBox", "ListBox"];

    /// <summary>Binding attributes that make an element "operable" for rule (a).</summary>
    private static readonly string[] BindingAttributeNames =
        ["Command", "IsChecked", "SelectedItem", "SelectedValue", "Text"];

    private static readonly string[] UiFiles =
    [
        "MainWindow.xaml",
        "Views/SettingsView.xaml",
        "Views/SectionModeMigrationDialog.xaml",
        "Views/ReleaseNotesWindow.xaml",
        "Views/ToastView.xaml",
    ];

    /// <summary>The literal ids the task-9 brief and the controller ruling both require to exist.</summary>
    private static readonly Dictionary<string, string[]> MandatoryLiteralIds = new(StringComparer.Ordinal)
    {
        ["MainWindow.xaml"] =
        [
            "Main.Settings", "Main.ReleaseNotes", "Main.Handbook", "Main.StatusLog",
            "Dates.List", "Dates.ShowAll",
            "Entries.List", "Entries.SortById", "Entries.SortByProject", "Entries.OnlyOpen",
            "Entries.Dictate", "Entries.StopDictation", "Entries.Delete",
            "Detail.ToggleDone", "Detail.Delete", "Detail.Pdf", "Detail.Html",
            "Detail.TaskMail", "Detail.Email", "Detail.Copy", "Detail.Reprocess",
            "Detail.EditTranscript", "Detail.RegenerateFromTranscript", "Detail.CancelEditTranscript",
            "Detail.TranscriptEditor", "Detail.ZoomIn", "Detail.ZoomOut", "Detail.ZoomText",
            "Sections.ShowProseSummary", "Sections.ShowLongSummary", "Sections.ShowTaskList",
            "Sections.ShowConversationNote", "Sections.ShowEmailText", "Sections.ShowStundenzettelText",
            "Sections.ShowAnalogText", "Sections.ShowTranscript", "Sections.Reset",
            "Copy.transcript", "Section.transcript",

            // Task 12: built-in sections are generated on demand only through the context menu
            // of "Neu generieren" (SectionRows covers custom categories only), and the PDF test
            // renders through "PDF in Zwischenablage kopieren" — "PDF" itself opens a viewer.
            "Detail.CopyPdf",
            "Generate.builtin.proseSummary", "Generate.builtin.longSummary", "Generate.builtin.taskList",
            "Generate.builtin.conversationNote", "Generate.builtin.emailText",
            "Generate.builtin.stundenzettel", "Generate.builtin.analog",
        ],
        ["Views/SettingsView.xaml"] =
        [
            "Settings.Save", "Settings.Reset", "Settings.SaveTarget",
            "Settings.SaveTarget.Personal", "Settings.SaveTarget.Global",
            "Settings.CategoryName", "Settings.CategoryPrompt", "Settings.Status",
            "Settings.AddCorrection", "Settings.AddCategory", "Settings.RemoveCategory",
            "Settings.ModelPicker", "Settings.ModelCost",
        ],
        ["Views/SectionModeMigrationDialog.xaml"] = ["Migration.UseRecommended", "Migration.KeepCurrent"],
        ["Views/ReleaseNotesWindow.xaml"] = ["ReleaseNotes.Close"],
        ["Views/ToastView.xaml"] = ["Toast.Item", "Toast.Details"],
    };

    /// <summary>
    /// The bound-id <c>StringFormat</c> templates the ruling requires: per-section copy icon,
    /// section container/heading, on-demand generate button, and the settings left-nav items.
    /// </summary>
    private static readonly Dictionary<string, string[]> MandatoryBoundIdFragments = new(StringComparer.Ordinal)
    {
        ["MainWindow.xaml"] = ["StringFormat=Copy.{0}", "StringFormat=Section.{0}", "StringFormat=Generate.{0}"],
        ["Views/SettingsView.xaml"] = ["StringFormat=Settings.Section.{0}"],
    };

    [Fact]
    public void Every_operable_control_with_a_binding_has_an_automation_id()
    {
        foreach (var file in UiFiles)
        {
            var document = LoadUiFile(file);
            foreach (var elementName in CheckedElementNames)
            {
                foreach (var element in document.Descendants().Where(e => e.Name.LocalName == elementName))
                {
                    var isBound = element.Attributes()
                        .Any(a => BindingAttributeNames.Contains(a.Name.LocalName)
                                  && a.Value.Contains("{Binding", StringComparison.Ordinal));
                    if (!isBound)
                    {
                        continue;
                    }

                    var id = (string?)element.Attribute(AutomationId);
                    id.Should().NotBeNullOrWhiteSpace(
                        $"{file}: <{elementName}> with a binding on " +
                        $"{string.Join("/", BindingAttributeNames)} must carry {AutomationId}. " +
                        $"Offending element attributes: {DescribeAttributes(element)}");
                }
            }
        }
    }

    [Fact]
    public void No_literal_automation_id_is_duplicated_within_a_file()
    {
        foreach (var file in UiFiles)
        {
            var literalIds = AllAutomationIdValues(file)
                .Where(id => !id.Contains("{Binding", StringComparison.Ordinal))
                .ToList();

            var duplicates = literalIds
                .GroupBy(id => id, StringComparer.Ordinal)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            duplicates.Should().BeEmpty($"{file} must not assign the same literal AutomationId twice");
        }
    }

    [Fact]
    public void Every_mandatory_literal_id_is_present()
    {
        foreach (var (file, ids) in MandatoryLiteralIds)
        {
            var present = AllAutomationIdValues(file).ToHashSet(StringComparer.Ordinal);

            foreach (var id in ids)
            {
                present.Should().Contain(id, $"{file} must expose AutomationId=\"{id}\"");
            }
        }
    }

    [Fact]
    public void Every_mandatory_bound_id_template_is_present()
    {
        foreach (var (file, fragments) in MandatoryBoundIdFragments)
        {
            var boundIds = AllAutomationIdValues(file)
                .Where(id => id.Contains("{Binding", StringComparison.Ordinal))
                .ToList();

            foreach (var fragment in fragments)
            {
                boundIds.Should().Contain(
                    id => id.Contains(fragment, StringComparison.Ordinal),
                    $"{file} must bind an AutomationId with {fragment}");
            }
        }
    }

    [Fact]
    public void SaveTarget_combo_box_items_carry_their_own_ids()
    {
        var document = LoadUiFile("Views/SettingsView.xaml");
        var items = document.Descendants().Where(e => e.Name.LocalName == "ComboBoxItem").ToList();

        items.Should().Contain(i => (string?)i.Attribute(AutomationId) == "Settings.SaveTarget.Personal");
        items.Should().Contain(i => (string?)i.Attribute(AutomationId) == "Settings.SaveTarget.Global");
    }

    /// <summary>
    /// Every AutomationId assigned in a file: both attributes set directly on an element, and
    /// ids assigned through a shared <c>Style</c>'s
    /// <c>&lt;Setter Property="AutomationProperties.AutomationId"&gt;</c> — the mechanism the
    /// per-section templates (<c>SectionHeaderControlStyle</c>, the settings left-nav item
    /// style) use so one Style covers every built-in and custom instance.
    /// </summary>
    private static IEnumerable<string> AllAutomationIdValues(string file)
    {
        var document = LoadUiFile(file);

        var direct = document.Descendants()
            .Select(e => (string?)e.Attribute(AutomationId))
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!);

        var viaSetter = document.Descendants().Where(e => e.Name.LocalName == "Setter")
            .Where(e => (string?)e.Attribute("Property") == AutomationId)
            .Select(e => (string?)e.Attribute("Value"))
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!);

        return direct.Concat(viaSetter);
    }

    private static string DescribeAttributes(XElement element) =>
        string.Join(", ", element.Attributes().Select(a => $"{a.Name.LocalName}={a.Value}"));

    private static XDocument LoadUiFile(string relativePath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Platee.Johann.slnx")))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("the tests run below the repository, next to Platee.Johann.slnx");
        var fullPath = Path.Combine(dir!.FullName, "Platee.Johann.UI", relativePath.Replace('/', Path.DirectorySeparatorChar));
        return XDocument.Load(fullPath);
    }
}

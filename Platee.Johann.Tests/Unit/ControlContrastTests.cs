namespace Platee.Johann.Tests.Unit;

using System.Globalization;
using System.Xml.Linq;
using FluentAssertions;

/// <summary>
/// WCAG-Kontraste der Bedienelemente (#97). Die Farben kommen aus
/// <c>Platee.Johann.UI/Themes/Controls.xaml</c> selbst — wer dort eine Farbe ändert und
/// damit unter die Grenze fällt, bekommt hier einen roten Test statt eines unlesbaren Knopfs.
/// Text braucht 4,5 : 1 (WCAG 1.4.3 AA), Rahmen, Symbole und Fokusring 3 : 1 (1.4.11).
/// </summary>
public sealed class ControlContrastTests
{
    private const double Text = 4.5;
    private const double NonText = 3.0;

    // Flächen, auf denen Knöpfe tatsächlich sitzen (Inventur #97).
    private static readonly string[] Surfaces = ["#FFFFFF", "#F0F0F0", "#F5F5F5", "#F9F9F9", "#FAFAFA", "#FFF8EC", "#F7F7F7"];

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Brushes = new(LoadBrushes);

    public static TheoryData<string, string, double> FixedPairs() => new()
    {
        // Standard (grau)
        { "ButtonFgBrush", "ButtonBgBrush", Text },
        { "ButtonHoverFgBrush", "ButtonHoverBgBrush", Text },
        { "ButtonHoverFgBrush", "ButtonPressedBgBrush", Text },

        // Primär (rot gefüllt)
        { "PrimaryFgBrush", "PrimaryBgBrush", Text },
        { "PrimaryFgBrush", "PrimaryHoverBgBrush", Text },
        { "PrimaryFgBrush", "PrimaryPressedBgBrush", Text },

        // Rot umrandet
        { "OutlineHoverFgBrush", "OutlineHoverBgBrush", Text },
        { "OutlinePressedFgBrush", "OutlinePressedBgBrush", Text },

        // Leise / Symbol
        { "ButtonHoverFgBrush", "QuietHoverBgBrush", Text },
        { "ButtonHoverFgBrush", "QuietPressedBgBrush", Text },

        // Listenzeilen
        { "ListItemFgBrush", "ListItemHoverBgBrush", Text },
        { "ListItemFgBrush", "ListItemSelectedBgBrush", Text },
    };

    public static TheoryData<string, double> OnEverySurface() => new()
    {
        { "ButtonBorderBrush", NonText },
        { "PrimaryBgBrush", NonText },
        { "OutlineFgBrush", Text },
        { "QuietFgBrush", Text },
        { "ConfirmFgBrush", NonText },
        { "LinkFgBrush", Text },
        { "LinkHoverFgBrush", Text },
        { "FocusRingBrush", NonText },
        { "ListItemSelectedBarBrush", NonText },
    };

    [Theory]
    [MemberData(nameof(FixedPairs))]
    public void Foreground_on_its_background_is_readable(string foreground, string background, double minimum)
    {
        Ratio(Color(foreground), Color(background))
            .Should().BeGreaterThanOrEqualTo(minimum, $"{foreground} on {background}");
    }

    [Theory]
    [MemberData(nameof(OnEverySurface))]
    public void Element_stands_out_on_every_surface_it_sits_on(string brush, double minimum)
    {
        foreach (var surface in Surfaces)
        {
            Ratio(Color(brush), surface).Should().BeGreaterThanOrEqualTo(minimum, $"{brush} on {surface}");
        }
    }

    [Theory]
    [InlineData("StandardButtonStyle")]
    [InlineData("PrimaryButtonStyle")]
    [InlineData("OutlineButtonStyle")]
    [InlineData("QuietButtonStyle")]
    [InlineData("LinkButtonStyle")]
    [InlineData("ListRowItemStyle")]
    [InlineData("FocusVisual")]
    public void Every_role_builds_its_template_on_a_real_control(string key)
    {
        // A wrong resource reference fails only when WPF uses it — at app start, not at build
        // time — and one inside a ControlTemplate only when the template is applied. So each
        // role is put on a real control and its template built.
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                using var stream = File.OpenRead(ControlsPath());
                var dictionary = (System.Windows.ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream);
                var style = (System.Windows.Style)dictionary[key];
                System.Windows.Controls.Control control = style.TargetType == typeof(System.Windows.Controls.ListBoxItem)
                    ? new System.Windows.Controls.ListBoxItem()
                    : key == "FocusVisual" ? new System.Windows.Controls.Control() : new System.Windows.Controls.Button();
                control.Resources.MergedDictionaries.Add(dictionary);
                control.Style = style;
                control.ApplyTemplate().Should().BeTrue($"{key} must supply a template");
                System.Windows.Media.VisualTreeHelper.GetChildrenCount(control).Should().BeGreaterThan(0);
            }
            catch (Exception ex)
            {
                failure = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        failure.Should().BeNull();
    }

    [Fact]
    public void The_ratio_matches_the_WCAG_reference_values()
    {
        // Guards the formula itself: black on white is 21:1, #767676 on white is the 4.5:1 edge.
        Ratio("#000000", "#FFFFFF").Should().BeApproximately(21.0, 0.01);
        Ratio("#767676", "#FFFFFF").Should().BeApproximately(4.54, 0.01);
    }

    private static string Color(string key)
    {
        Brushes.Value.Should().ContainKey(key, "Controls.xaml must define it");
        return Brushes.Value[key];
    }

    private static IReadOnlyDictionary<string, string> LoadBrushes()
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return XDocument.Load(ControlsPath()).Descendants()
            .Where(e => e.Name.LocalName == "SolidColorBrush" && e.Attribute(x + "Key") is not null)
            .ToDictionary(e => e.Attribute(x + "Key")!.Value, e => e.Attribute("Color")!.Value);
    }

    private static string ControlsPath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Platee.Johann.slnx")))
        {
            dir = dir.Parent;
        }

        dir.Should().NotBeNull("the tests run below the repository, next to Platee.Johann.slnx");
        var path = Path.Combine(dir!.FullName, "Platee.Johann.UI", "Themes", "Controls.xaml");
        File.Exists(path).Should().BeTrue($"{path} holds the control colours");
        return path;
    }

    private static double Ratio(string a, string b)
    {
        var (la, lb) = (Luminance(a), Luminance(b));
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double Luminance(string hex)
    {
        var h = hex.TrimStart('#');
        h = h.Length == 8 ? h[2..] : h;
        double Channel(int i)
        {
            var v = int.Parse(h.Substring(i * 2, 2), NumberStyles.HexNumber) / 255.0;
            return v <= 0.03928 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(0)) + (0.7152 * Channel(1)) + (0.0722 * Channel(2));
    }
}

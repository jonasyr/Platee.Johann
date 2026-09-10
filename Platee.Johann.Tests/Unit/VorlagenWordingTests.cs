using FluentAssertions;
using Platee.Johann.UI.ViewModels;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #59 — die Prompt-Kategorien heißen in der Oberfläche „Vorlagen".
/// <para>
/// Vorher kollidierten fünf Wörter: ein Eintrag vom <c>EntryType</c> „Aufgabe" ist etwas
/// anderes als der erzeugte Abschnitt „Aufgaben", und beide hießen im Gespräch „Kategorie".
/// „Typ" bleibt beim Eintrag, „Vorlage" bezeichnet die Prompt-Kategorie.
/// </para>
/// <para>
/// Interne Bezeichner bleiben bewusst unangetastet: Ids stecken in
/// <c>Entry.CustomSections</c> und dürfen sich nie ändern.
/// </para>
/// </summary>
public sealed class VorlagenWordingTests
{
    [Fact]
    public void Personal_group_is_labelled_Vorlagen()
    {
        SectionVisibilityViewModel.PersonalGroup.Should().Be("Eigene Vorlagen");
    }

    [Fact]
    public void Global_group_is_labelled_Vorlagen()
    {
        SectionVisibilityViewModel.GlobalGroup.Should().Be("Team-Vorlagen");
    }

    [Fact]
    public void A_new_category_is_called_Vorlage_not_Kategorie()
    {
        SettingsViewModel.NewCategoryDefaultName.Should().Be("Neue Vorlage");
    }

    [Fact]
    public void No_user_facing_group_label_still_says_Kategorie()
    {
        SectionVisibilityViewModel.PersonalGroup.Should().NotContain("Kategorie");
        SectionVisibilityViewModel.GlobalGroup.Should().NotContain("Kategorie");
        SettingsViewModel.NewCategoryDefaultName.Should().NotContain("Kategorie");
    }
}

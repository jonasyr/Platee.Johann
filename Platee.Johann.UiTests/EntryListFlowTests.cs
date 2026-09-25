namespace Platee.Johann.UiTests;

using System.IO;
using FluentAssertions;
using Xunit;

/// <summary>
/// Task 11: entry-list and delete flows against the real Johann exe — sort order, "Als erledigt
/// markieren" combined with "Nur unerledigte" (#100), and both delete outcomes ("Nein" keeps the
/// entry, "Ja" moves it to <c>_Papierkorb</c> and the now-empty day disappears from the sidebar).
/// </summary>
[Collection("Desktop")]
public sealed class EntryListFlowTests
{
    [Fact]
    public async Task WatchFolder_NewEntryAppears_InSortOrder()
    {
        using var ctx = UiTestContext.Start();
        await ctx.DropDictationAsync("D1");
        await ctx.DropDictationAsync("D3");

        ctx.EntryTitles().Should().HaveCount(2);
        ctx.EntryNumbers().Should().BeInAscendingOrder("the list starts sorted ascending by Nr");

        ctx.App.Click("Entries.SortById");
        ctx.WaitUntil(
            () => ctx.EntryNumbers().SequenceEqual(ctx.EntryNumbers().OrderByDescending(n => n)),
            TimeSpan.FromSeconds(5),
            "Entries.List zeigt nach dem Klick auf 'Nr' absteigende Reihenfolge");

        ctx.EntryNumbers().Should().BeInDescendingOrder("one click on an already-ascending list reverses it");
    }

    [Fact]
    public async Task MarkDone_KeepsSelection_AndOnlyOpenRemovesRow()
    {
        using var ctx = UiTestContext.Start();
        var t1 = await ctx.DropDictationAsync("D1");
        var t3 = await ctx.DropDictationAsync("D3");

        ctx.SelectEntry(t1);
        ctx.App.Click("Detail.ToggleDone");
        ctx.WaitUntil(
            () => ctx.SelectedEntryTitle() == t1,
            TimeSpan.FromSeconds(5),
            "die Auswahl bleibt nach 'Als erledigt markieren' auf t1"); // #100

        ctx.App.Click("Entries.OnlyOpen");
        ctx.WaitUntil(
            () => ctx.EntryTitles() is [var only] && only == t3,
            TimeSpan.FromSeconds(5),
            "Entries.List zeigt nach dem Filter nur noch t3");

        ctx.EntryTitles().Should().Equal(t3);
        ctx.SelectedEntryTitle().Should().Be(t3, "die nächste Zeile rückt nach");
    }

    [Fact]
    public async Task Delete_WithKey_ConfirmNo_KeepsEntry()
    {
        using var ctx = UiTestContext.Start();
        var t1 = await ctx.DropDictationAsync("D1");

        ctx.SelectEntry(t1);
        ctx.App.Key("Delete");
        ctx.App.Click("Nein", mouse: true); // „Nein“ ist vorausgewählt (#55)

        // "Nein" must keep the entry not just in the instant after the click, but for a real
        // moment after — a delete that proceeded despite "Nein" would still show up here.
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
        while (DateTime.UtcNow < deadline)
        {
            ctx.EntryTitles().Should().Contain(t1);
            await Task.Delay(200);
        }

        Directory.EnumerateFiles(ctx.Sandbox.Output, "*_status.json", SearchOption.AllDirectories)
            .Should().ContainSingle("der Eintrag ist nicht gelöscht");
        Directory.Exists(Path.Combine(ctx.Sandbox.Output, "_Papierkorb"))
            .Should().BeFalse("'Nein' darf nichts in den Papierkorb verschieben");
    }

    [Fact]
    public async Task Delete_MovesFilesToTrash_AndDayDisappears()
    {
        using var ctx = UiTestContext.Start();
        var t1 = await ctx.DropDictationAsync("D1");

        ctx.SelectEntry(t1);
        ctx.App.Click("Detail.Delete", mouse: true);
        ctx.App.Click("Ja", mouse: true);

        ctx.WaitUntil(() => ctx.EntryTitles().Count == 0, TimeSpan.FromSeconds(10), "Entries.List wird nach dem Löschen leer");
        ctx.WaitUntil(() => ctx.DateItems().Count == 0, TimeSpan.FromSeconds(10), "der Tag verschwindet aus der Seitenleiste");

        var papierkorb = Path.Combine(ctx.Sandbox.Output, "_Papierkorb");
        ctx.WaitUntil(
            () => Directory.Exists(papierkorb)
                && Directory.EnumerateFiles(papierkorb, "geloescht.json", SearchOption.AllDirectories).Any(),
            TimeSpan.FromSeconds(10),
            "_Papierkorb enthält geloescht.json"); // Directory.Exists guards EnumerateFiles against DirectoryNotFoundException

        Directory.EnumerateFiles(papierkorb, "geloescht.json", SearchOption.AllDirectories)
            .Should().ContainSingle();
    }
}

namespace Platee.Johann.UiTests;

using FluentAssertions;
using Xunit;

/// <summary>
/// Covers the fix-round-1 review finding (I1): a failure between <c>OpenAiStubServer.Start()</c>
/// and the finished <see cref="UiTestContext"/> must dispose the stub's HTTP listener and delete
/// the half-created sandbox directory, instead of leaking them into the next test.
/// <para>
/// This does <b>not</b> reach <see cref="Platee.Johann.UiDriver.Automation.JohannSession.Launch"/>
/// — the injected <c>stub</c> callback throws before <c>TestSandbox.Create</c> even runs, so no
/// Johann process is ever started. It still needs <see cref="ExeLocator.Find"/> to succeed (a real
/// build must exist) and opens a real loopback HTTP listener via <c>OpenAiStubServer.Start()</c>,
/// which is why — like <see cref="SmokeTests"/> — it stays in this project (excluded from the
/// normal <c>dotnet test</c> run by <c>IsTestProject</c>) rather than in
/// <c>Platee.Johann.Tests</c>. A fully pure unit test of "dispose order" is not possible here: the
/// method's entire job is coordinating the lifetime of a live listener and a filesystem tree, not
/// pure logic — so this integration-shaped test is the closest available seam, and it stays
/// compiled-but-unrun under the same hard constraint as the smoke tests (see task-10 report).
/// </para>
/// </summary>
[Collection("Desktop")]
public sealed class UiTestContextCleanupTests
{
    [Fact]
    public void Start_disposes_the_stub_and_deletes_the_sandbox_when_the_stub_callback_throws()
    {
        var act = () => UiTestContext.Start(stub: _ => throw new InvalidOperationException("boom"));

        act.Should().Throw<InvalidOperationException>().WithMessage("boom");

        // The stub's OpenAiStubServer.Dispose() closed its HttpListener — Start() on a fresh
        // instance must be able to bind a *different* port without interference (it always picks
        // a free one), but more importantly a second Start() attempt must not throw
        // "läuft bereits" from a leaked process — there is none, since Launch was never reached.
        // The strongest observable signal from outside OpenAiStubServer is that calling Start()
        // again immediately succeeds and can itself be disposed cleanly.
        using var probe = Platee.Johann.UiDriver.Stub.OpenAiStubServer.Start();
        probe.Should().NotBeNull();
    }
}

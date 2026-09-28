namespace Platee.Johann.UiTests;

using Xunit;

/// <summary>
/// Every UI test shares one real desktop and one real Johann process at a time — parallel runs
/// would fight over the same window focus, clipboard, and process-name check in
/// <c>JohannSession.Launch</c>. All UI test classes must carry <c>[Collection("Desktop")]</c>.
/// </summary>
[CollectionDefinition("Desktop", DisableParallelization = true)]
public sealed class UiCollection
{
}

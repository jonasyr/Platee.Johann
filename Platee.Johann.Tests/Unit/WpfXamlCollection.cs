namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Tests, die XAML laden, laufen allein: die erste WPF-Initialisierung auf zwei Threads
/// zugleich kann sich gegenseitig blockieren (siehe <see cref="ControlContrastTests"/>).
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class WpfXamlCollection
{
    public const string Name = "WPF-XAML (nicht parallel)";
}

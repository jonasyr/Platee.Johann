namespace Platee.Johann.Domain.ValueObjects;

public sealed record CorrectionEntry
{
    public string Wrong { get; init; } = string.Empty;

    public string Correct { get; init; } = string.Empty;
}

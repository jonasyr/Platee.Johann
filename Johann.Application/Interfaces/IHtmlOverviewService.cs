namespace Johann.Application.Interfaces;

/// <summary>
/// Regenerates the daily _ItemÜbersicht.html overview page for a given date.
/// </summary>
public interface IHtmlOverviewService
{
    Task RegenerateAsync(DateOnly date, CancellationToken ct = default);
}

namespace Johann.Application.Processing;

/// <summary>
/// Determines GPT word limits for summaries based on transcript length.
/// Mirrors get_word_limits() from Python summarizer.py.
/// </summary>
public static class WordLimitCalculator
{
    /// <summary>
    /// Returns (Abstract word limit, Structured summary word limit).
    /// Tiers: &lt;300 words → (20, 50) | 300–1000 → (50, 150) | &gt;1000 → (150, 300).
    /// </summary>
    public static (int Abstract, int Structured) Calculate(string transcript)
    {
        var wordCount = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return wordCount switch
        {
            < 300 => (20, 50),
            < 1000 => (50, 150),
            _ => (150, 300),
        };
    }
}

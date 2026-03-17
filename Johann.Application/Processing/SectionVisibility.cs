namespace Johann.Application.Processing;

/// <summary>
/// Controls which content sections are included when rendering an entry to PDF/HTML.
/// Default: all standard sections visible; type-specific sections off by default.
/// </summary>
public sealed record SectionVisibility(
    bool LongSummary       = true,
    bool ProseSummary      = true,
    bool TaskList          = true,
    bool ConversationNote  = true,
    bool EmailText         = false,
    bool StundenzettelText = false,
    bool AnalogText        = false,
    bool Transcript        = true);

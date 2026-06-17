# Johann v1.2 - UI/UX and Quality-of-Life Improvement Pitch

## Purpose of this document
This document is a pitch and execution blueprint for user-facing improvements that users can feel immediately.
It intentionally focuses on workflow quality, speed, clarity, and trust, not technical bug fixes.

Use this document for:
1. Stakeholder pitch.
2. Prioritization decisions.
3. Implementation handoff once approved.

This is a no-code planning document.

---

## Product promise for v1.2
Johann should feel like:
1. Fast capture.
2. Low cognitive load.
3. High confidence in output.

Every improvement below maps to at least one of these three promises.

---

## Primary user outcomes
1. Capture ideas/tasks without breaking focus.
2. Turn raw dictation into actionable items quickly.
3. Trust AI-generated output enough to use it without heavy rewriting.
4. Never lose track of follow-ups.

---

## Prioritized improvement portfolio

| Priority | Initiative | Main user value | Delivery size |
| --- | --- | --- | --- |
| P1 | Inbox Zero Start Screen | Immediate clarity of what matters today | Medium |
| P1 | One-Click Triage Actions | Faster move from note to action | Medium |
| P1 | Smart Follow-Up Reminders | Less forgotten work | Medium |
| P1 | Regenerate by Section | Fast targeted corrections | Small/Medium |
| P1 | Prompt Playground in Settings | Safe tuning with instant feedback | Medium |
| P2 | Quick Capture Everywhere | Lower capture friction | Medium/Large |
| P2 | Trust Layer for AI Output | Better confidence and adoption | Large |
| P2 | Keyboard-First Power Mode | Faster daily throughput | Medium |
| P2 | Contextual Empty States | Better first-use and recovery UX | Small |
| P3 | Outcome-Focused Daily Digest | End-of-day closure and accountability | Medium |
| P3 | Visual Hierarchy Refresh | Better readability and calmness | Medium |
| P3 | Share and Hand-off Presets | Less manual formatting before sending | Medium |

---

## Initiative 1 - Inbox Zero Start Screen

### User problem today
Users land in a generic list and must manually infer priorities.
This creates decision fatigue at startup.

### Exact UX concept
The first screen after app launch is a dashboard with four sections:
1. New.
2. In Progress.
3. Needs Review.
4. Done Today.

Visual layout:
1. Top: day header (date + quick counts).
2. Center: four status columns/cards with item counts.
3. Right panel: "Next best action" suggestions (up to 3 items).
4. Bottom: quick actions (Capture, Process now, Open settings).

Each card shows:
1. Count badge.
2. Top 3 most recent items.
3. CTA button (for example "Review now").

### Workflow change
Before:
1. Launch app.
2. Scan long lists.
3. Decide manually what to do first.

After:
1. Launch app.
2. See priority buckets instantly.
3. Click one bucket and continue.

### Why this is better
1. Reduces startup friction.
2. Makes priorities explicit.
3. Creates a feeling of control and momentum.

### Scope if approved
1. New dashboard view.
2. Aggregation logic for status buckets.
3. Routing from dashboard cards to filtered list/detail views.

### Dependencies
1. Reliable entry status information.
2. Consistent pending count logic.

### Success metrics
1. Time-to-first-action after app open.
2. Percentage of sessions where user completes an action in under 30 seconds.
3. Daily active sessions.

### Pitch sentence
"Johann starts with priorities, not files."

---

## Initiative 2 - One-Click Triage Actions

### User problem today
Users often open items and perform repetitive multi-step updates.

### Exact UX concept
Every entry card/detail gets quick action chips:
1. Mark done.
2. Set follow-up date.
3. Assign person.
4. Send as email draft.
5. Move to project.

Visual behavior:
1. Chips appear directly below headline metadata.
2. Hover states clearly indicate action type.
3. Success toast confirms completion and next suggested action.

### Workflow change
Before:
1. Open entry.
2. Scroll and edit fields.
3. Save.
4. Return to list.

After:
1. Open entry.
2. Click one action chip.
3. Confirm optional lightweight dialog.
4. Continue with next item.

### Why this is better
1. Converts notes into outcomes faster.
2. Reduces clicks and context switching.
3. Encourages consistent task hygiene.

### Scope if approved
1. Action chip bar UI component.
2. Lightweight dialogs for follow-up date and assignee.
3. Command handlers for each action.

### Dependencies
1. Existing entry status persistence.
2. Optional assignee model expansion if needed.

### Success metrics
1. Actions per session.
2. Completion rate of triage actions.
3. Reduction in untouched items older than 48 hours.

### Pitch sentence
"From capture to action in one click."

---

## Initiative 3 - Smart Follow-Up Reminders

### User problem today
Follow-ups in dictations are easy to miss after summarization.

### Exact UX concept
Reminder system with two surfaces:
1. Inline suggestion in entry detail: "Detected follow-up date/task. Create reminder?"
2. Daily digest strip in home dashboard: overdue and due today.

Visual layout:
1. Reminder badge near entry status.
2. Color states: due today, upcoming, overdue.
3. Quick snooze options: 1 day, 3 days, 1 week.

### Workflow change
Before:
1. User reads summary.
2. Manually remembers next steps.
3. Follow-ups can be forgotten.

After:
1. App suggests reminder automatically.
2. User confirms in one click.
3. Daily reminder list drives execution.

### Why this is better
1. Prevents missed commitments.
2. Increases trust that Johann supports execution, not only documentation.
3. Creates clear day-to-day value.

### Scope if approved
1. Reminder entity and scheduling state.
2. UI badges and reminder panels.
3. Reminder notification mechanism (in-app first).

### Dependencies
1. Date/task extraction quality.
2. Persisted reminder storage.

### Success metrics
1. Reminder adoption rate.
2. On-time follow-up completion rate.
3. Overdue reminder trend.

### Pitch sentence
"Johann remembers what you should not forget."

---

## Initiative 4 - Regenerate by Section

### User problem today
When output quality is off, users must rerun everything even if only one section needs adjustment.

### Exact UX concept
Per-section regenerate controls inside entry view:
1. Regenerate Summary.
2. Regenerate Tasks.
3. Regenerate Email Draft.
4. Shorter / More formal / Bullet style quick variants.

Visual layout:
1. Section header includes "Regenerate" dropdown button.
2. Diff-style preview panel shows old vs new section.
3. User chooses "Apply" or "Keep old".

### Workflow change
Before:
1. Regenerate full output.
2. Lose good parts.
3. Manual correction needed.

After:
1. Target only weak section.
2. Preview differences.
3. Apply only what improved.

### Why this is better
1. Saves time.
2. Reduces frustration.
3. Increases perceived AI controllability.

### Scope if approved
1. Section-level generation endpoints/workflows.
2. Comparison preview component.
3. Apply/discard interaction.

### Dependencies
1. Existing prompt architecture.
2. Deterministic section mapping.

### Success metrics
1. Number of section-only regenerations.
2. Reduction in full regenerations.
3. User-rated satisfaction after regenerate.

### Pitch sentence
"Fix exactly what is weak, keep what is already good."

---

## Initiative 5 - Prompt Playground in Settings

### User problem today
Prompt tuning is high-risk: users save changes without seeing output impact first.

### Exact UX concept
New settings subview with split layout:
1. Left: editable prompt text.
2. Center: sample transcript selector/input.
3. Right: generated output preview.

Controls:
1. Run test.
2. Compare with current baseline.
3. Save as new default.
4. Revert.

Visual behavior:
1. Output sections aligned with production layout.
2. Highlight changed lines from baseline.
3. Estimated token/length indicator.

### Workflow change
Before:
1. Edit prompt.
2. Save blindly.
3. Discover side effects later.

After:
1. Edit prompt.
2. Test instantly with sample transcript.
3. Save only if output quality is better.

### Why this is better
1. Makes prompt work safe and measurable.
2. Reduces quality regressions.
3. Empowers non-technical admins.

### Scope if approved
1. Prompt playground panel.
2. Test-run pipeline using non-persistent preview mode.
3. Baseline comparison logic.

### Dependencies
1. Existing prompt storage and rendering.
2. Sample transcript set.

### Success metrics
1. Playground usage frequency.
2. Prompt rollback rate.
3. Improvement in output quality scores over time.

### Pitch sentence
"Tune prompts safely before touching production behavior."

---

## Initiative 6 - Quick Capture Everywhere

### User problem today
Capture can be delayed if users must navigate back to the main window first.

### Exact UX concept
Global quick capture panel triggered by hotkey.
Supports:
1. Start/stop recording.
2. Paste text.
3. Drag and drop audio.
4. Instant "Send to Johann".

Visual style:
1. Compact floating panel.
2. Always-on-top only while active.
3. Minimal fields: title, category, capture source.

### Workflow change
Before:
1. Switch to app.
2. Open capture path.
3. Add input.

After:
1. Hit hotkey from anywhere.
2. Capture instantly.
3. Continue original task.

### Why this is better
1. Lower friction means higher capture consistency.
2. Better for real-world interrupt-driven work.

### Scope if approved
1. Global hotkey registration.
2. Floating mini-capture UI.
3. Queue handoff to existing processing pipeline.

### Dependencies
1. Hotkey conflict handling.
2. Background-safe ingestion behavior.

### Success metrics
1. Number of captures per active user.
2. Time from thought to capture.
3. Capture abandonment rate.

### Pitch sentence
"Capture thoughts in the moment, without breaking flow."

---

## Initiative 7 - Trust Layer for AI Output

### User problem today
Users may hesitate to trust generated output if they cannot see why content appears.

### Exact UX concept
Add explainability affordances:
1. "Show source" links next to generated bullets.
2. Highlight transcript snippets used for each statement.
3. Confidence indicator per section.

Visual layout:
1. Right-side expandable "Evidence" panel.
2. Clicking a bullet scrolls transcript to related snippet.
3. Ambiguous points marked as "Needs review".

### Workflow change
Before:
1. Read output.
2. Manually verify against transcript.

After:
1. Click bullet.
2. See supporting transcript instantly.
3. Approve with confidence.

### Why this is better
1. Increases trust and adoption.
2. Reduces manual verification effort.
3. Encourages responsible use in business communication.

### Scope if approved
1. Source-to-output trace mapping.
2. Evidence panel UX.
3. Confidence heuristics and labels.

### Dependencies
1. Reliable text span mapping.
2. Reasonable performance for highlighting.

### Success metrics
1. Output approval rate without manual rewrite.
2. Time spent verifying summaries.
3. User trust score (simple in-app rating).

### Pitch sentence
"AI suggestions you can verify at a glance."

---

## Initiative 8 - Keyboard-First Power Mode

### User problem today
Frequent users lose time with repetitive mouse interactions.

### Exact UX concept
Command palette and shortcut set:
1. Open command palette.
2. Jump to entry/date/project.
3. Trigger common actions.
4. Toggle filters.

Visual layout:
1. Palette overlay centered at top.
2. Recent commands and suggestions.
3. Shortcut hints shown in menu/tooltips.

### Workflow change
Before:
1. Navigate manually through UI.
2. Perform repeated click paths.

After:
1. Open palette.
2. Type intent.
3. Execute immediately.

### Why this is better
1. Improves speed for heavy users.
2. Reduces repetitive strain and click overhead.
3. Makes the app feel professional and efficient.

### Scope if approved
1. Command registry.
2. Palette UI.
3. Shortcut mapping and discoverability help.

### Dependencies
1. Stable command architecture.
2. Conflict-free shortcut design.

### Success metrics
1. Shortcut usage share.
2. Reduced average interaction time for key tasks.
3. Power-user retention.

### Pitch sentence
"Operate Johann at thought speed."

---

## Initiative 9 - Contextual Empty States and Guidance

### User problem today
New users and edge-case states can feel blank/confusing.

### Exact UX concept
Replace empty panes with guided states:
1. "No entries yet" with clear next action buttons.
2. "No result for filter" with quick reset option.
3. "No output folder configured" with direct settings shortcut.

Visual layout:
1. Clear icon + short explanation.
2. One primary CTA and one secondary CTA.
3. Link to short help snippets.

### Workflow change
Before:
1. User sees blank area.
2. Unsure what to do.

After:
1. User sees explanation.
2. Clicks guided next action.

### Why this is better
1. Better onboarding.
2. Lower support burden.
3. Higher conversion from install to daily use.

### Scope if approved
1. Empty-state components for key screens.
2. Trigger conditions.
3. Copywriting and CTA wiring.

### Dependencies
1. Basic analytics for first-use states.
2. Consistent UX copy tone.

### Success metrics
1. First-run completion rate.
2. Time-to-first-successful-processing.
3. Drop-off in first 3 sessions.

### Pitch sentence
"Even first-time users become productive in minutes."

---

## Initiative 10 - Outcome-Focused Daily Digest

### User problem today
Users cannot easily see what was achieved today and what remains open.

### Exact UX concept
Daily digest panel with:
1. Processed entries count.
2. Completed vs open tasks.
3. Overdue follow-ups.
4. Top active projects.

Visual layout:
1. Compact summary cards.
2. One-click filters from each metric.
3. Optional export/share of digest summary.

### Workflow change
Before:
1. Manually infer progress from lists.

After:
1. Open digest.
2. See outcomes and remaining work instantly.

### Why this is better
1. Creates closure and accountability.
2. Makes value visible to users and teams.

### Scope if approved
1. Digest data aggregation.
2. Dashboard card UI.
3. Optional export template.

### Dependencies
1. Reliable status metadata.
2. Reminder/follow-up states (stronger with Initiative 3).

### Success metrics
1. Daily digest open rate.
2. Weekly completion trend.
3. Reduced overdue backlog.

### Pitch sentence
"See what moved today and what needs attention next."

---

## Initiative 11 - Visual Hierarchy Refresh

### User problem today
Dense layouts and low hierarchy can increase scanning effort.

### Exact UX concept
A visual polish pass focused on clarity:
1. Stronger heading hierarchy.
2. Better spacing rhythm.
3. Clear primary vs secondary actions.
4. Improved status color semantics.

Visual standards:
1. 8px spacing grid.
2. Consistent section headers.
3. Readable text line lengths in prompt areas.

### Workflow change
Before:
1. Users scan dense UI and miss key actions.

After:
1. Users identify priorities and actions faster.

### Why this is better
1. Reduces fatigue for daily users.
2. Increases perceived product quality.
3. Makes complex screens easier to navigate.

### Scope if approved
1. UI style audit and component normalization.
2. Targeted updates of high-traffic screens.
3. Accessibility contrast checks.

### Dependencies
1. Basic style token alignment across views.
2. Agreement on design direction.

### Success metrics
1. Reduced misclicks on primary workflows.
2. Faster task completion in usability checks.
3. Better subjective usability scores.

### Pitch sentence
"Less visual noise, more focus."

---

## Initiative 12 - Share and Hand-off Presets

### User problem today
Users often need to manually reformat output for email, meetings, and client communication.

### Exact UX concept
Export/share presets:
1. Email brief.
2. Meeting recap.
3. Client status update.
4. Internal task handoff.

Visual behavior:
1. "Share" action opens preset picker.
2. Preview shows final format.
3. One-click copy/export/send.

### Workflow change
Before:
1. Copy raw output.
2. Reformat manually each time.

After:
1. Choose preset.
2. Review preview.
3. Share immediately.

### Why this is better
1. Saves repetitive formatting time.
2. Produces consistent communication quality.
3. Makes Johann output immediately usable.

### Scope if approved
1. Preset template library.
2. Preview surface.
3. Export/copy integration.

### Dependencies
1. Stable output schema.
2. Localized template copy quality.

### Success metrics
1. Preset usage rate.
2. Reduction in manual editing before sharing.
3. User feedback on output readiness.

### Pitch sentence
"Professional hand-offs by default."

---

## Proposed release strategy

## Wave 1 (high felt value, lower risk)
1. Inbox Zero Start Screen.
2. One-Click Triage Actions.
3. Regenerate by Section.
4. Prompt Playground.
5. Contextual Empty States.

Expected result:
1. Users feel immediate speed and clarity gains.
2. Minimal dependency on deep infrastructure changes.

## Wave 2 (trust and continuity)
1. Smart Follow-Up Reminders.
2. Trust Layer for AI Output.
3. Daily Digest.

Expected result:
1. Better execution reliability.
2. Higher confidence in AI output.

## Wave 3 (power and polish)
1. Quick Capture Everywhere.
2. Keyboard-First Power Mode.
3. Visual Hierarchy Refresh.
4. Share and Hand-off Presets.

Expected result:
1. Premium productivity feel for frequent users.
2. Better communication workflows.

---

## Business impact narrative for pitch
1. Today Johann is useful.
2. With this roadmap Johann becomes indispensable in daily work.
3. The roadmap turns Johann from "transcribe and summarize" into "capture, decide, execute, and communicate".

---

## Approval checklist

Approve this roadmap if the organization wants:
1. Higher user retention through better daily flow.
2. Faster capture-to-action cycle.
3. Stronger trust in generated outputs.
4. Clear, measurable UX outcomes.

---

## Implementation handoff checklist (post-approval)
1. Confirm final priorities and timeline by wave.
2. Define design mockups for each approved initiative.
3. Define acceptance criteria per initiative.
4. Define telemetry events for success metrics.
5. Plan staged releases and user feedback loops.

---

## Suggested acceptance criteria template per initiative
1. UX acceptance: user can complete target action in <= N steps.
2. Quality acceptance: no regression in existing save/export workflows.
3. Performance acceptance: interaction feedback under target latency.
4. Analytics acceptance: required events emitted for KPI tracking.

---

## Final pitch summary
Johann v1.2 should be sold as a user-impact release:
1. Faster to start.
2. Faster to act.
3. Easier to trust.
4. Easier to share.

That combination delivers improvements users notice every day, not only internal technical quality gains.

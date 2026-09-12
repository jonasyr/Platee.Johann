# Writing Prompts for GPT-5.6 — A Practical, Evidence-Based Guide

**For knowledge work in Markdown and Obsidian vaults**
**Version 1.0 · 2026-09-12**

This guide explains how to write prompts for the GPT-5.6 family (Luna, Terra, Sol) when your
material lives in Markdown files — meeting notes, project notes, research, a personal or team
vault. It is written so that someone who has never deliberately tuned a prompt can follow it,
and so that someone who has can check the reasoning.

Every non-obvious claim carries a citation. The reference list at the end follows APA 7.

Three kinds of evidence appear here, kept apart deliberately:

| Marker | Meaning |
|---|---|
| **[Vendor]** | OpenAI's own documentation. Authoritative on model behaviour, but not independent. |
| **[Peer-reviewed / preprint]** | Academic work. Independent, but usually tested on models older than 5.6. |
| **[Measured]** | Measurements taken against the live API on 2026-09-11 in a German-language production summarisation application. Specific to that workload; treat as indicative. |

Where these disagree, this guide says so rather than picking the tidiest answer.

---

## 1. The one-paragraph version

Tell the model **what a good result looks like** and **when it is finished**, then get out of
the way. Do not script the steps it should take to get there. State each rule exactly once.
Remove anything that does not change the output. Contradictions cost more than gaps, because
the model spends billable reasoning tokens trying to reconcile them (OpenAI, 2025a).

Everything below is the long form of that paragraph, plus the places where it is not quite true.

---

## 2. What GPT-5.6 is, and why older prompts misfire

GPT-5.6 comes in three sizes: **Luna** (fastest, cheapest), **Terra** (balanced) and **Sol**
(most capable) (OpenAI, 2026b). All three are *reasoning models*: before writing a visible
answer they produce hidden reasoning tokens, which are billed as output tokens even though you
never see them (OpenAI, 2026e).

That single fact invalidates much of pre-2025 prompting folklore. Prompts written for
GPT-4-class models usually carry scaffolding whose job was to make a non-reasoning model reason:
"think step by step", "first analyse, then summarise", numbered thinking procedures, worked
examples. GPT-5.6 already does that internally. The scaffolding no longer buys reasoning — it
competes with it.

OpenAI's guidance for 5.6 is explicit that the correction is subtractive: prompts should "define
outcomes, constraints, evidence, and completion criteria, then allow the model to choose an
efficient path", and the guidance "emphasizes removing scaffolding rather than adding detail"
(OpenAI, 2026a). **[Vendor]**

### 2.1 How much does trimming buy?

In OpenAI's internal evaluations, leaner system prompts produced roughly **10–15 % higher
evaluation scores** while cutting **41–66 % of total tokens** and **33–67 % of cost**
(OpenAI, 2026a). The same document cautions: *"Results will vary by workload, so treat these
ranges as directional."* **[Vendor]**

Treat those numbers as what they are: a vendor's internal result on coding agents. They are the
best available signal on 5.6 specifically, and they should not be quoted as a controlled public
benchmark.

---

## 3. The five principles

### 3.1 Describe the destination, not the route

> "Describe the destination rather than prescribing every step." (OpenAI, 2026a)

OpenAI's reasoning documentation says the same differently: give the model "a clear goal, strong
constraints, and an explicit output contract without prescribing every intermediate step", and
treat reasoning effort "as a tuning knob, not the primary way to recover quality"
(OpenAI, 2026e). **[Vendor]**

The independent evidence agrees. Meincke, Mollick, Mollick and Shapiro (2025) tested
chain-of-thought prompting across model classes and found that for reasoning-capable models CoT
prompting "often results in only marginal, if any, gains in answer accuracy" while it
"significantly increases the time and tokens needed to generate a response". For non-reasoning
models CoT helped slightly on average — but also raised answer variance and sometimes broke
questions the model had previously got right. **[Preprint]**

A cross-generation study reaches a compatible conclusion: as GPT models improved, the marginal
value of externally imposed reasoning structure fell, with standard chain-of-thought measured at
**−0.8 percentage points** for GPT-4o on a code benchmark and as much as **−13.8 points** for
Mistral-Large — while the same techniques still helped the Qwen family (*Aging of prompt
engineering techniques across LLM versions*, 2026). The authors' conclusion is the useful one:
"effective prompting strategies must be adapted per model family and generation rather than
transferred unchanged." **[Preprint]**

**In practice.** Delete blocks like this:

```text
### THINKING PROCESS ###
1. READ the note completely
2. IDENTIFY the key statements
3. GROUP them by topic
4. WRITE the summary
```

Replace them with the destination:

```text
The summary is finished when a colleague who has not read the note knows what was decided,
who does what by when, and what is still open.
```

### 3.2 State each rule exactly once

> "Repeating instructions such as 'ask first,' 'do not mutate,' or 'wait for approval' can cause
> unnecessary approval requests for safe, expected actions." (OpenAI, 2026a)

Repetition is not emphasis. To the model, one rule restated in three wordings is three rules
that must all be satisfied, and near-duplicates behave like soft contradictions. **[Vendor]**

**A trap specific to vaults:** if you keep a system prompt in one note and per-task prompts in
others, the same rule easily ends up in both. Nobody reading a single file notices. In one
measured case, removing a duplicated instruction block improved faithfulness while cutting
input tokens by 24 %, because the surviving copy in the task prompt was the more detailed one
**[Measured]**.

### 3.3 Contradictions are worse than omissions

> "Poorly-constructed prompts containing contradictory or vague instructions can be more
> damaging to GPT-5 than to other models, as it expends reasoning tokens searching for a way to
> reconcile the contradictions." (OpenAI, 2025a)

For 5.6 specifically: "GPT-5-class models follow prompt contracts closely, so conflicting rules
can create more instability than missing detail" (OpenAI, 2026a). **[Vendor]**

OpenAI's prompt-optimizer cookbook works through a real prompt whose contradictions included
"prefer the standard library" next to "use external packages if they make things simpler", and
"keep comments minimal" next to "add brief explanations". Removing them moved measured
instruction adherence from **4.40 to 4.90 out of 5** (OpenAI, 2025c). **[Vendor]**

Typical contradictions in note-processing prompts:

| Contradiction | Why it hurts |
|---|---|
| "Be exhaustive" + "maximum 150 words" | The model must guess which wins. |
| "Keep the author's wording" + "write formally" | Notes are rarely formal. |
| "Never invent anything" + "complete unfinished thoughts" | These are opposites. |
| "Be concise" + a long list of things to include | The list contradicts the adjective. |
| "Preserve all links" + "remove irrelevant content" | Which wins when a link sits in a removed passage? |

> ⚠ **One honest caveat.** In a controlled test on a summarisation prompt, resolving two real
> contradictions produced **no measurable quality change** (4 items better, 6 worse, p = 0.754)
> **[Measured]**. Contradictions are still worth removing — mostly because they make a prompt
> unmaintainable for *humans* — but do not expect a quality jump from that alone.

### 3.4 Prefer decision rules to absolutes

> "Avoid unnecessary absolute rules (ALWAYS, NEVER, must, only) for judgment calls; prefer
> decision rules instead." (OpenAI, 2026a) **[Vendor]**

An absolute that cannot always be satisfied becomes a contradiction the moment it meets a case
it does not fit.

- ✗ `ALWAYS produce exactly five bullet points.`
- ✓ `One bullet per distinct topic. A short note may need only one.`

### 3.5 Say what to do, not what to avoid

Negative instructions are weaker than positive ones: suppressing a concept requires representing
it first. Language models also misinterpret negation comparatively often and are sensitive to
framing (Dwivedi et al., 2023). **[Mixed evidence]** State the desired behaviour and reserve
prohibitions for hard boundaries with no positive formulation.

- ✗ `Do not repeat the note title.`
- ✓ `Begin with the first sentence of content. The title is already in the file.`

---

## 4. What to remove, what to keep

OpenAI's 5.6 guidance gives an explicit two-column answer (OpenAI, 2026a). **[Vendor]**

**Remove:**
- repeated statements of the same rule
- style or process instructions that do not change behaviour
- examples that do not alter outcomes
- process instructions for behaviour the model already performs reliably
- descriptions of tools or files the task does not use

**Keep:**
- the visible outcome
- success criteria and stopping conditions
- safety, business, evidence and permission constraints
- routing rules that depend on context
- the required output shape and validation requirements

A useful test for any line: **delete it, run the same inputs, and see whether the output
changes.** If it does not, the line was decoration — and decoration is not free, because it
enters the input on every call.

---

## 5. Reasoning effort and verbosity

GPT-5.6 exposes `reasoning_effort` with levels `none`, `low`, `medium`, `high`, `xhigh` and
`max` (OpenAI, 2026e). OpenAI's migration advice: keep your existing level as a baseline, then
test **one level lower**, because "GPT-5.6 is more token-efficient than earlier generations, so
lower settings often hold quality" (OpenRouter, 2026). **[Vendor / third-party]**

Crucially: *before* raising effort, check whether the prompt lacks success criteria, routing
rules or verification steps (OpenAI, 2026a). Effort is not a substitute for a clear contract.

### 5.1 Measured effect on a summarisation task

One call per level against `gpt-5.6-luna`, long German source text **[Measured]**:

| Level | Output tokens | of which reasoning | Visible text |
|---|---|---|---|
| default | 888 | 213 | 2,873 characters |
| `low` | **644** (−27 %) | 0 | 2,797 characters |
| `medium` | 808 | 162 | 2,742 characters |
| `high` | 1,335 (+50 %) | 653 | 2,871 characters |

`minimal` is rejected by these models with HTTP 400.

Two readings:

1. **`high` cost 50 % more and returned no additional content.** For summarising and rewriting
   it is waste. If someone proposes it as "better quality", this is the counter-number.
2. **`low` saved roughly a quarter of output tokens at essentially unchanged text length.**
   Whether it works as *well* is a question token counts cannot answer — that needs a reading
   comparison (§10).

Caveat: one call per level, one prompt, one document. Reasoning counts vary run to run. Treat
the ordering as reliable and the percentages as indicative.

### 5.2 Verbosity

GPT-5.6 "tends to be more concise by default than GPT-5.5" (OpenAI, 2026a). Use the
`text.verbosity` parameter as the global default and put task-specific length rules in the
prompt. Give concrete limits — "3–6 sentences or ≤5 bullets" — rather than the adjective
"concise" (OpenAI, 2026c). **[Vendor]**

---

## 6. Output format: Markdown is the safe choice

Two findings pull in opposite directions, and the resolution matters for vault work.

**Finding one:** format restrictions degrade reasoning. Tam et al. (2024) found "a significant
decline in LLM reasoning abilities under format restrictions", and the tighter the constraint,
the larger the loss — with constrained JSON decoding the worst case. **[Peer-reviewed]**

**Finding two:** the same literature finds that *loose* format restrictions generally improve
performance and reduce variance, and that performance recovers when unconstrained reasoning
precedes structured output.

**Resolution:** asking for **Markdown** is a loose restriction on the shape of prose, not a
decoding grammar. It is safe and usually helpful — which is convenient, because Markdown is
what a vault stores anyway. Forcing strict JSON around the same content would not be. If you
need machine-readable output (for a script that files notes, say), use the Structured Outputs
feature rather than prompt-level insistence: only Structured Outputs actually guarantees schema
adherence, and it removes the need for "strongly worded prompts to achieve consistent
formatting" (OpenAI, 2026f). **[Vendor]**

One operational warning: adherence to Markdown instructions placed in the system prompt "can
degrade over the course of a long conversation" (OpenAI, 2025a). For single-shot processing this
does not apply; in a long chat, restate the format rule every few turns.

**A formulation that works:**

```text
Format the answer in Markdown. Use `##` for headings, `-` for lists and **bold** for terms
that must stand out. Use Markdown only where it carries meaning.
```

---

## 7. Working with vault files

Two peer-reviewed findings drive this section, and both are specific to the situation a vault
creates: long inputs assembled from several files.

**Position matters more than people expect.** Liu et al. (2023) analysed multi-document question
answering and key-value retrieval and found that "performance is often highest when relevant
information occurs at the beginning or end of the input context, and significantly degrades when
models must access relevant information in the middle of long contexts, **even for explicitly
long-context models**". Performance also "substantially decreases as the input context grows
longer". **[Peer-reviewed, TACL]**

**Telling the model to answer from the supplied material works, and is measurable.** Addlesee
(2024) built a QA corpus of material the models could not have seen in pre-training and showed
that a grounding-oriented prompt improved answer accuracy by **up to 28 percentage points
(mean 12)** across healthcare and finance domains, by reducing the conflict between in-prompt
knowledge and static pre-training knowledge. **[Peer-reviewed]**

The rest of this section is the practical consequence of those two results plus the rules above.

### 7.1 Protect frontmatter and links explicitly

A model rewriting a note will happily reformat YAML frontmatter, renumber a list, or "tidy"
wikilinks into plain text. If the file goes back into the vault, that breaks queries, Dataview
tables and the graph.

State it as an outcome, not a prohibition:

```text
The YAML frontmatter between the opening and closing `---` lines is returned byte-for-byte
unchanged. Wikilinks stay in their original form: [[Note name]] and [[Note name|Alias]].
Tags keep their `#` prefix.
```

⚠ **Never ask the model to invent links.** It will produce `[[Notes that do not exist]]` that
look plausible and create orphan entries in the graph. If linking is wanted, supply the list of
existing note titles and say: *"Link only to titles from this list; if nothing fits, use plain
text."*

### 7.2 Put the note last

Two independent reasons to keep the variable content at the **end** of the prompt:

1. OpenAI's prompt-engineering guidance recommends placing context near the end rather than at
   the beginning, for cost and latency (OpenAI, 2026h). **[Vendor]** The position effect from
   Liu et al. (2023) points the same way: the end of the context is one of the two strong
   positions. **[Peer-reviewed]**
2. Prompt caching reuses a *prefix*. Stable instructions first, changing note content last, is
   the structure that can ever benefit (OpenAI, 2026d). **[Vendor]** — but see §9.2 for what
   caching actually delivers in practice.

```text
[stable instructions]
[stable output contract]

NOTE:
{{content}}
```

### 7.3 Several notes as context: label them

When you paste multiple notes, the model cannot tell where one ends and the next begins unless
you say so. Use explicit delimiters and state what to do with them:

```text
Below are several notes, each introduced by a line `### FILE: <path>`.
Answer only from these notes. When you use a statement, name the file it came from.
If the notes do not contain the answer, say so instead of inferring it.
```

The last sentence is the important one. Without an explicit escape hatch, a model asked a
question its context cannot answer will tend to produce a plausible answer anyway — and
grounding instructions of exactly this kind were measured to raise accuracy by up to 28
percentage points (Addlesee, 2024). **[Peer-reviewed]**

⚠ **Order the files deliberately.** Because accuracy is highest at the beginning and end of a
long context and lowest in the middle (Liu et al., 2023), the note most likely to hold the
answer should not sit in the middle of twenty others. If you cannot rank them, prefer fewer
files over more: accuracy "substantially decreases as the input context grows longer", so
pasting the whole vault is worse than pasting the five relevant notes. **[Peer-reviewed]**

### 7.4 Templates: one contract per task

An Obsidian template that calls a model should carry **one** task. A template that summarises
*and* extracts tasks *and* proposes tags is three contracts in one prompt, and their length
rules will contradict each other (§3.3). Three small templates beat one large one, and each can
be measured separately (§10).

---

## 8. Personas, shouting, and examples

### 8.1 Personas do less than people think

Zheng, Pei, Logeswaran, Lee and Jurgens (2024) tested **162 roles** across **4 model families**
and **2,410 factual questions** and found that "adding personas in system prompts does not
improve model performance across a range of questions compared to the control setting where no
persona is added". The effect of any individual persona is "largely random", and automatically
finding a good one performs "no better than random selection". **[Peer-reviewed, EMNLP Findings]**

A later study refines rather than overturns this: expert personas *help* alignment-shaped tasks
(writing, tone and format following — gains of +0.40 to +0.65 on MT-Bench) while *hurting*
knowledge-retrieval tasks (MMLU 68.0 % vs. 71.6 % baseline). Shorter personas did least damage
(Hu, Rostami, & Thomason, 2026). **[Preprint]**

**What to conclude.** Rewriting and summarising notes is closer to "writing" than to "knowledge
retrieval", so a short persona is not harmful — but it is not doing the work people think. Keep
it short and functional, and never let the actual requirements hide inside it.

- ✗ `YOU ARE A HIGHLY SPECIALISED EXPERT WITH 20 YEARS OF EXPERIENCE IN…`
- ✓ `You rewrite meeting notes for colleagues who did not attend.`

### 8.2 On SHOUTING

There is no evidence that upper case improves instruction following, and it has a measurable
cost: all-caps text tokenises far less efficiently. In one measured German prompt, all-caps text
tokenised at **3.14 characters per token** against **4.3** for ordinary German prose — about
**37 % more tokens for the same words** **[Measured]**. If that text is a system prompt sent on
every call, it is a recurring bill for a formatting habit.

### 8.3 Examples: which kind, and when

The general guidance for reasoning models is zero-shot by default: they "often don't need
few-shot examples to produce good results", and several evaluations report that few-shot
prompting *reduced* performance on o1-class models. The cross-generation study measured few-shot
at **−7.4 percentage points** for the GPT pair while the same technique gained **+7.9 points**
for Qwen (*Aging of prompt engineering techniques*, 2026). **[Preprint]**

But two different things get called "examples", and only one is discouraged:

| | Task examples (few-shot) | Format specimens |
|---|---|---|
| What they show | how to solve the problem | what the surface should look like |
| Effect on reasoning models | neutral to negative | positive where a rule is hard to state |
| Use for | rarely needed | list style, heading style, a tricky grammatical form |

A single line showing what one output item should look like is a **format specimen**. It is
cheap and it settles questions no rule states cleanly. Keep those; drop worked examples of the
reasoning itself.

**Practical case:** a rule saying "begin each task with a verb in the infinitive" produced broken
German; one example — `Generate a PDF per voice message` — fixed it immediately, on a weaker
model too **[Measured]**.

---

## 9. Cost: what actually drives the bill

### 9.1 Per token is not per task

Reasoning tokens are billed as output (OpenAI, 2026e), so the headline token price is a poor
guide to what a task costs.

A measurement across 160 calls and 16 documents found that the nominally cheapest model in a
catalogue burned **2,496 reasoning tokens to produce 514 visible ones** and therefore cost
**more per task** than a newer, nominally dearer model — at lower quality **[Measured]**.

**Rule: rank models by measured cost per task, never by headline token price.**

### 9.2 Prompt caching exists; you may not get it

GPT-5.6 caches reusable prompt prefixes: minimum **1,024 visible input tokens**, **30-minute**
lifetime, cached tokens billed at **0.1×** the normal input rate, cache writes at **1.25×**
(OpenAI, 2026d). **[Vendor]** With a long, stable system prompt this looks like a large saving.

It was tested **[Measured]**:

| Scenario | Prompt tokens | Cached |
|---|---|---|
| Identical prompt, second call | 2,031 | **2,028** |
| Same instructions, different document | 1,911 → 1,881 | **0** |
| Identical prompt plus four extra tokens at the end | 2,035 | **0** |

Only a byte-identical prompt hit the cache. Appending four tokens destroyed the hit despite
~2,031 identical prefix tokens.

**Consequences for vault work:**

- Do not plan savings around caching without measuring your own call pattern.
- Keep stable instructions first and the note last anyway (§7.2): it costs nothing and it is the
  precondition for ever benefiting.
- If you process the *same* note repeatedly — iterating on a prompt, for instance — caching does
  help, because the prompt is then genuinely identical.

---

## 10. How to tell whether a prompt change helped

This section matters more than any individual rule, because prompt work fails most often at the
evaluation step.

**Model outputs are not reproducible.** API-based models are explicitly non-deterministic, and
reproducibility "at scale is nearly impossible" (Levy, 2026). **[Peer-reviewed]**

It follows that **a single run proves nothing**. Thelwall (2024) is blunt for complex text tasks:
"non-systematic experiments with variations in the inputs or instructions are pointless if the
aim is to improve the results", because natural variation makes the effect of any single change
unmeasurable from one test. The remedy is repetition and averaging — up to **30 repetitions** in
the cited studies. **[Peer-reviewed]**

The same author reports a result that cuts against the "lean prompt" narrative: in the one
systematic comparison of system prompts for a complex text-evaluation task, **shorter
instructions produced worse results** (Thelwall, 2024). **[Peer-reviewed]**

**How to hold both.** OpenAI's 5.6 evidence concerns agentic prompts full of process scaffolding;
Thelwall's concerns evaluative prompts whose instructions carry the actual rubric. Trimming
*scaffolding* is well supported. Trimming *substance* is not. The test is whether a line changes
the output, not whether it is long.

### A workable protocol

1. Fix a set of **10–15 representative notes**, spanning short and long, tidy and messy. Keep
   them in a folder and do not change them — they are your measuring stick.
2. Change **one thing** at a time.
3. Run old and new prompt over the **whole** set, **three times each**.
4. Check the objective things automatically first: did it keep the frontmatter, stay inside the
   length limit, avoid inventing links, answer in the right language?
5. Read the survivors side by side. For writing tasks, human reading is the metric that counts.
6. Keep the change only if it wins on reading, and record what you changed and why.

> **A warning from doing this.** In one evaluation, an automatic language check reported that
> one variant produced three times fewer violations than another. All eleven flagged cases were
> false positives: short, perfectly correct sentences that happened to contain none of the
> checker's stopwords. The apparent finding vanished once the checker was fixed **[Measured]**.
> **Validate your measuring instrument before you trust its verdict.**

---

## 11. Worked example: before and after

A prompt that summarises a meeting note in a vault.

### Before

```text
### ROLE ###
YOU ARE A HIGHLY SPECIALISED EXPERT FOR THE ANALYSIS AND STRUCTURING OF MEETING NOTES
WITH MANY YEARS OF EXPERIENCE.

### THINKING PROCESS ###
1. READ the note completely and understand the context
2. IDENTIFY all key statements
3. STRUCTURE the key statements by topic
4. FORMULATE the summary
5. CHECK your summary for completeness

### RULES ###
- Be precise and complete
- Keep it short
- NEVER invent anything
- Always write in English
- Do not lose any important information
- Write concisely
- Do not change the formatting
- Write in English

Summarise the following note: {{content}}
```

What is wrong: a persona doing no work (§8.1) in upper case costing ~37 % extra tokens (§8.2);
a thinking-process block the model does not need and may fight (§3.1); "precise and complete"
against "short" and "concisely" — one contradiction stated three times (§3.2, §3.3); "write in
English" twice (§3.2); "do not change the formatting" is too vague to protect frontmatter
(§7.1); and no statement of what a finished summary contains.

### After

```text
You summarise meeting notes for colleagues who did not attend.

A finished summary lets the reader know: what was decided, who does what by when,
and what is still open.

- Length: 3–6 sentences, or up to 5 bullets if the note covers several topics.
- Every statement must be traceable to the note. If something is not in it, leave it out.
- Keep the author's terms for names, projects and figures.
- Return the YAML frontmatter between the `---` lines byte-for-byte unchanged.
- Keep wikilinks in their original form: [[Note name]] and [[Note name|Alias]].
- Format in Markdown; use lists only for genuinely parallel items.
- Begin with the first sentence of content; the note already has a title.

NOTE:
{{content}}
```

Each rule appears once; the length constraint is a number rather than an adjective; the
faithfulness requirement is an outcome rather than a prohibition; frontmatter and links are
protected explicitly rather than by a vague "do not change the formatting"; there is no process
script; and the note stays last (§7.2).

**This rewrite is an illustration, not a validated improvement.** Under §10 it would have to be
measured over 10–15 notes before replacing anything.

---

## 12. Checklist

- [ ] Does the prompt say what a finished result contains?
- [ ] Does it say when the model is done?
- [ ] Does every rule appear exactly once — including across separate prompt files?
- [ ] Are there two rules that cannot both be satisfied?
- [ ] Are absolutes (ALWAYS/NEVER) used only for hard boundaries?
- [ ] Is every prohibition that could be a positive instruction, one?
- [ ] Is there a process script the model does not need?
- [ ] Are length limits numbers, not adjectives?
- [ ] Are frontmatter, wikilinks and tags protected explicitly?
- [ ] Is there an escape hatch for "the answer is not in this material"?
- [ ] Is the variable content last?
- [ ] Would deleting each remaining line change the output?
- [ ] Has the change been measured over a fixed set of notes, more than once?
- [ ] Has the measuring instrument itself been checked?

---

## 13. Anti-pattern catalogue

| Anti-pattern | Why | Replace with |
|---|---|---|
| `### THINKING PROCESS ###` | Competes with internal reasoning | A description of the finished result |
| ALL CAPS | No benefit, ~37 % more tokens | Sentence case |
| "Be precise and complete" + "be brief" | Contradiction | One explicit length rule |
| Long expert persona | Randomly effective at best | One short line of role and audience |
| "Do not X" | Negation is weaker | "Do Y instead" |
| Same rule in several prompt files | Reads as several rules | Say it once, in one place |
| "concise", "detailed", "thorough" | Not measurable | Sentence or bullet counts |
| "Do not change the formatting" | Too vague to protect YAML | Name frontmatter and links explicitly |
| Asking for links without a list | Invents non-existent notes | Supply existing titles, or plain text |
| Raising `reasoning_effort` for quality | Costs more, no more content | Fix the contract first |
| Worked reasoning examples | Neutral-to-harmful on reasoning models | Format specimens only |
| Judging a change on one run | Outputs are non-deterministic | Fixed set, repeated runs |
| Trusting an automatic checker | It can be wrong | Validate the checker first |

---

## References

*Aging of prompt engineering techniques across LLM versions* (2026). arXiv:2608.24641.
https://arxiv.org/html/2608.24641

Addlesee, A. (2024). Grounding LLMs to in-prompt instructions: Reducing hallucinations caused by
static pre-training knowledge. In *Proceedings of Safety4ConvAI: The Third Workshop on Safety for
Conversational AI @ LREC-COLING 2024*. https://aclanthology.org/2024.safety4convai-1.1/

Dwivedi, Y. K., et al. (2023). *Challenging the appearance of machine intelligence: Cognitive
bias in LLMs and best practices for adoption*. arXiv:2304.01358.
https://arxiv.org/pdf/2304.01358

Hu, Z., Rostami, M., & Thomason, J. (2026). *Expert personas improve LLM alignment but damage
accuracy: Bootstrapping intent-based persona routing with PRISM*. arXiv:2603.18507.
https://arxiv.org/html/2603.18507v1

Levy, B. (2026). Caution ahead: Numerical reasoning and look-ahead bias in AI models. *Journal
of Accounting Research, 64*(3), 1139–1188. https://doi.org/10.1111/1475-679x.70058

Liu, N. F., Lin, K., Hewitt, J., Paranjape, A., Bevilacqua, M., Petroni, F., & Liang, P. (2023).
Lost in the middle: How language models use long contexts. *Transactions of the Association for
Computational Linguistics*. https://arxiv.org/abs/2307.03172

Meincke, L., Mollick, E. R., Mollick, L., & Shapiro, D. (2025). *Prompting science report 2:
The decreasing value of chain of thought in prompting*. The Wharton School, University of
Pennsylvania. arXiv:2506.07142. https://arxiv.org/abs/2506.07142

OpenAI. (2025a). *GPT-5 prompting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5_prompting_guide

OpenAI. (2025b). *GPT-5 troubleshooting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5_troubleshooting_guide

OpenAI. (2025c). *GPT-5 prompt migration and improvement using the new optimizer*. OpenAI
Cookbook. https://developers.openai.com/cookbook/examples/gpt-5/prompt-optimization-cookbook

OpenAI. (2026a). *Prompt guidance for GPT-5.6*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/prompt-guidance-gpt-5p6

OpenAI. (2026b). *GPT-5.6: Frontier intelligence that scales with your ambition*.
https://openai.com/index/gpt-5-6/

OpenAI. (2026c). *GPT-5.2 prompting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5-2_prompting_guide

OpenAI. (2026d). *Prompt caching*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/prompt-caching

OpenAI. (2026e). *Reasoning*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/reasoning

OpenAI. (2026f). *Structured outputs*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/structured-outputs

OpenAI. (2026g). *GPT-5.1 prompting guide*. OpenAI Cookbook.
https://developers.openai.com/cookbook/examples/gpt-5/gpt-5-1_prompting_guide

OpenAI. (2026h). *Prompt engineering*. OpenAI API documentation.
https://developers.openai.com/api/docs/guides/prompt-engineering

OpenRouter. (2026). *GPT-5.6 migration guide*.
https://openrouter.ai/docs/cookbook/evaluate-and-optimize/model-migrations/gpt-5-6

Schulhoff, S., Ilie, M., Balepur, N., Kahadze, K., Liu, A., Si, C., … Resnik, P. (2024). *The
prompt report: A systematic survey of prompt engineering techniques*. arXiv:2406.06608.
https://arxiv.org/abs/2406.06608

Tam, Z. R., Wu, C.-K., Tsai, Y.-L., Lin, C.-Y., Lee, H., & Chen, Y.-N. (2024). *Let me speak
freely? A study on the impact of format restrictions on performance of large language models*.
arXiv:2408.02442. https://arxiv.org/abs/2408.02442

Thelwall, M. (2024). ChatGPT for complex text evaluation tasks. *Journal of the Association for
Information Science and Technology, 76*(4), 645–648. https://doi.org/10.1002/asi.24966

*Understanding LLM reasoning for abstractive summarization* (2025). arXiv:2512.03503.
https://arxiv.org/html/2512.03503v1

Zheng, M., Pei, J., Logeswaran, L., Lee, M., & Jurgens, D. (2024). When "a helpful assistant" is
not really helpful: Personas in system prompts do not improve performances of large language
models. In *Findings of the Association for Computational Linguistics: EMNLP 2024*.
https://aclanthology.org/2024.findings-emnlp.888/

### Note on the measurements

Entries marked **[Measured]** come from measurements against the live OpenAI API on 2026-09-11
in a German-language production summarisation application: 160 calls across 16 documents for the
cost model, plus separate runs for reasoning effort, prompt caching and a controlled three-way
prompt comparison (540 generations, 180 automated assessments). Tokens were counted locally with
`tiktoken` (`o200k_base`). They are reported here because they are the only figures in this
guide taken on 5.6 itself — but they come from one workload and one language, and should be
treated as indicative rather than as a benchmark.

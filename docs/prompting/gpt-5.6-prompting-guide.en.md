# Writing Prompts for GPT-5.6 — A Practical, Evidence-Based Guide

**Version 1.0 · 2026-09-11 · for Platé.Johann (issue #73)**

This guide explains how to write prompts for the GPT-5.6 family (Luna, Terra, Sol). It is
written so that someone who has never tuned a prompt can follow it, and so that someone who
has can check the reasoning. Every non-obvious claim carries a citation; the reference list
at the end follows APA 7.

Three kinds of evidence appear here, and they are kept apart deliberately:

| Marker | Meaning |
|---|---|
| **[Vendor]** | OpenAI's own documentation. Authoritative on how the models behave, but not independent. |
| **[Peer-reviewed / preprint]** | Academic work. Independent, but usually tested on older models than 5.6. |
| **[Measured here]** | Our own measurements against the live API on 2026-09-11. Specific to our workload. |

Where these three disagree, this guide says so rather than picking the tidiest answer.

---

## 1. The one-paragraph version

Tell the model **what a good result looks like** and **when it is finished**, then get out of
the way. Do not script the steps it should take to get there. Say each rule exactly once.
Remove anything that does not change the output. Contradictions cost more than gaps, because
the model spends billable reasoning tokens trying to reconcile them (OpenAI, 2025a).

Everything below is the long form of that paragraph, plus the places where it is not quite
true.

---

## 2. What GPT-5.6 is, and why old prompts misfire on it

GPT-5.6 ships in three sizes: **Luna** (fastest, cheapest), **Terra** (balanced) and **Sol**
(most capable) (OpenAI, 2026b). All three are *reasoning models*: before writing a visible
answer they produce hidden reasoning tokens, which are billed as output tokens even though
you never see them (OpenAI, 2026e).

That single fact invalidates a large part of pre-2025 prompting folklore. Prompts written for
GPT-4-class models typically carry scaffolding whose job was to make a non-reasoning model
reason: "think step by step", "first analyse, then summarise", numbered thinking procedures,
worked examples. GPT-5.6 already does that internally. The scaffolding no longer buys
reasoning — it just competes with it.

OpenAI's guidance for 5.6 is explicit that the correction is subtractive, not additive:
prompts should "define outcomes, constraints, evidence, and completion criteria, then allow
the model to choose an efficient path", and the guidance "emphasizes removing scaffolding
rather than adding detail" (OpenAI, 2026a). **[Vendor]**

### 2.1 How much does trimming actually buy?

In OpenAI's internal coding-agent evaluations, leaner system prompts produced roughly
**10–15 % higher evaluation scores** while cutting **41–66 % of total tokens** and
**33–67 % of cost** (OpenAI, 2026a). The same document cautions: *"Results will vary by
workload, so treat these ranges as directional."* **[Vendor]**

Treat those numbers as what they are: a vendor's internal result on coding agents. They are
the best available signal on 5.6 specifically, and they should not be quoted as though they
were a controlled public benchmark.

---

## 3. The five principles

### 3.1 Describe the destination, not the route

> "Describe the destination rather than prescribing every step." (OpenAI, 2026a)

OpenAI's reasoning guide puts the same rule differently: give the model "a clear goal, strong
constraints, and an explicit output contract without prescribing every intermediate step",
and treat reasoning effort "as a tuning knob, not the primary way to recover quality"
(OpenAI, 2026e). **[Vendor]**

The independent evidence points the same way. Meincke, Mollick, Mollick and Shapiro (2025)
tested chain-of-thought prompting across model classes and found that for reasoning-capable
models, CoT prompting "often results in only marginal, if any, gains in answer accuracy"
while it "significantly increases the time and tokens needed to generate a response". For
non-reasoning models CoT helped slightly on average — but also raised answer variance and
sometimes broke questions the model had previously got right. **[Preprint]**

A cross-generation study reaches a compatible conclusion from another angle: as GPT models
improved, the marginal value of externally imposed reasoning structure fell, with standard
chain-of-thought measured at **−0.8 percentage points** for GPT-4o on a code-generation
benchmark, and as much as **−13.8 points** for Mistral-Large — while the same techniques
still helped the Qwen family (*Aging of prompt engineering techniques across LLM versions*,
2026). The authors' conclusion is the useful one: "effective prompting strategies must be
adapted per model family and generation rather than transferred unchanged." **[Preprint]**

**What this means in practice.** Delete blocks that look like this:

```text
### CHAIN OF THOUGHTS ###
1. UNDERSTAND the text
2. IDENTIFY the key statements
3. STRUCTURE them by topic
4. WRITE the summary
```

Replace them with the destination:

```text
The summary is finished when a reader who did not hear the recording knows what was
decided, who does what by when, and what is still open.
```

### 3.2 State each rule exactly once

> "Repeating instructions such as 'ask first,' 'do not mutate,' or 'wait for approval' can
> cause unnecessary approval requests for safe, expected actions." (OpenAI, 2026a)

Repetition is not emphasis. To the model, a rule restated three times in three wordings is
three rules that must all be satisfied, and near-duplicates behave like soft contradictions.
**[Vendor]**

### 3.3 Contradictions are worse than omissions

> "Poorly-constructed prompts containing contradictory or vague instructions can be more
> damaging to GPT-5 than to other models, as it expends reasoning tokens searching for a way
> to reconcile the contradictions." (OpenAI, 2025a)

For 5.6 specifically: "GPT-5-class models follow prompt contracts closely, so conflicting
rules can create more instability than missing detail" (OpenAI, 2026a). **[Vendor]**

This is not abstract. OpenAI's prompt-optimizer cookbook works through a real prompt whose
contradictions included "prefer the standard library" next to "use external packages if they
make things simpler", and "keep comments minimal" next to "add brief explanations". Removing
them moved measured instruction adherence from **4.40 to 4.90 out of 5** and cut peak memory
from 3,626 KB to 577.5 KB on the task (OpenAI, 2025c). **[Vendor]**

Typical contradictions in a summarisation prompt:

| Contradiction | Why it hurts |
|---|---|
| "Be exhaustive" + "maximum 150 words" | The model must guess which one wins. |
| "Use the speaker's own words" + "write formally" | Dictation is rarely formal. |
| "Never invent anything" + "complete incomplete thoughts" | These are opposites. |
| "Be concise" + a 400-token list of things to include | The list contradicts the adjective. |

### 3.4 Prefer decision rules to absolutes

> "Avoid unnecessary absolute rules (ALWAYS, NEVER, must, only) for judgment calls; prefer
> decision rules instead." (OpenAI, 2026a) **[Vendor]**

An absolute that cannot always be satisfied becomes a contradiction the moment it meets a
case it does not fit. A decision rule survives the edge case:

- ✗ `ALWAYS produce exactly five bullet points.`
- ✓ `Use one bullet per distinct topic. Short recordings may need only one.`

### 3.5 Say what to do, not what to avoid

Negative instructions are weaker than positive ones, because suppressing a concept requires
representing it first — the "pink elephant" effect. LLMs also misinterpret negation
comparatively often and are sensitive to framing (Dwivedi et al., 2023, on framing effects in
LLM prompting). **[Mixed evidence]** The practical guidance is consistent across practitioner
sources: state the desired behaviour rather than the forbidden one, and reserve prohibitions
for hard boundaries where no positive formulation exists.

- ✗ `Do not repeat the section heading.`
- ✓ `Begin with the first sentence of content. The application renders the heading.`

That example is taken from our own codebase: prompts that named their own section produced
duplicated headings in the detail view, the PDF and the e-mail (see `CLAUDE.md`). The fix that
worked was the positive formulation.

---

## 4. What to remove, what to keep

OpenAI's 5.6 guidance gives an explicit two-column answer (OpenAI, 2026a). **[Vendor]**

**Remove:**
- repeated statements of the same rule
- style or process instructions that do not change behaviour
- examples that do not alter outcomes
- process instructions for behaviour the model already performs reliably
- descriptions of tools the task does not use

**Keep:**
- the user-visible outcome
- success criteria and stopping conditions
- safety, business, evidence and permission constraints
- routing rules that depend on context
- the required output shape and any validation requirements

A useful test for any line: **delete it, run the same inputs, and see whether the output
changes.** If it does not, the line was decoration — and on a reasoning model decoration is
not free, because it enters the input on every single call.

---

## 5. Reasoning effort and verbosity

GPT-5.6 exposes `reasoning_effort` with the levels `none`, `low`, `medium`, `high`, `xhigh`
and `max` (OpenAI, 2026e). OpenAI's migration advice is to keep your existing level as a
baseline, then test **one level lower**, because "GPT-5.6 is more token-efficient than earlier
generations, so lower settings often hold quality" (OpenRouter, 2026). **[Vendor / third-party]**

Crucially: *before* raising effort, check whether the prompt is missing success criteria,
routing rules or verification steps (OpenAI, 2026a). Effort is not a substitute for a clear
contract.

### 5.1 What we measured

On a long German dictation, using Johann's real system message and summary prompt, one call
per level against `gpt-5.6-luna` **[Measured here]**:

| Level | Output tokens | of which reasoning | Visible text |
|---|---|---|---|
| default | 888 | 213 | 2,873 characters |
| `low` | **644** (−27 %) | 0 | 2,797 characters |
| `medium` | 808 | 162 | 2,742 characters |
| `high` | 1,335 (+50 %) | 653 | 2,871 characters |

`minimal` is rejected by these models with HTTP 400.

Two readings, both important:

1. **`high` cost 50 % more and returned no additional content.** For summarisation it is
   waste. If someone proposes it as "better quality", this is the counter-number.
2. **`low` saved roughly a quarter of output tokens at essentially unchanged text length.**
   Whether it summarises as *well* is a question token counts cannot answer — that requires a
   reading comparison (see §10).

Caveat: one call per level, one prompt, one dictation. Reasoning token counts vary run to run.
Treat the ordering as reliable and the exact percentages as indicative.

### 5.2 Verbosity

GPT-5.6 "tends to be more concise by default than GPT-5.5" (OpenAI, 2026a). Use the
`text.verbosity` parameter as the global default and put task-specific length rules in the
prompt. Give concrete limits — "3–6 sentences or ≤5 bullets" — rather than the adjective
"concise" (OpenAI, 2026c). **[Vendor]**

---

## 6. Output format and Markdown

Two findings pull in opposite directions here, and the resolution matters.

**Finding one:** format restrictions degrade reasoning. Tam et al. (2024) found "a significant
decline in LLM reasoning abilities under format restrictions", and the tighter the constraint,
the larger the loss — with constrained JSON decoding the worst case. **[Peer-reviewed]**

**Finding two:** the same literature finds that *loose* format restrictions generally improve
performance and reduce variance, and that performance recovers when unconstrained reasoning is
allowed to precede structured output.

**Resolution for a summarisation app:** asking for **Markdown** is a loose restriction on the
shape of prose, not a constrained decoding grammar. It is safe and usually helpful. Forcing
strict JSON around the same content would not be. If you ever need machine-readable output,
use the Structured Outputs feature rather than prompt-level threats, because only Structured
Outputs actually guarantees schema adherence and it removes the need for "strongly worded
prompts to achieve consistent formatting" (OpenAI, 2026f). **[Vendor]**

One operational warning from the GPT-5 guide: adherence to Markdown instructions placed in the
system prompt "can degrade over the course of a long conversation" (OpenAI, 2025a). For a
single-shot call like ours this does not apply; for chat-shaped products it does.

**Concrete formulation that works:**

```text
Format the answer in Markdown. Use `##` for section headings, `-` for lists and **bold**
for terms that must stand out. Use Markdown only where it carries meaning.
```

---

## 7. Personas and role prompting

Many older prompts open with a role: "You are a highly specialised expert in…". The evidence
for this is weaker than its popularity suggests.

Zheng, Pei, Logeswaran, Lee and Jurgens (2024) tested **162 roles** across **4 model families**
and **2,410 factual questions** and found that "adding personas in system prompts does not
improve model performance across a range of questions compared to the control setting where no
persona is added". They add that the effect of any individual persona is "largely random", and
that automatically finding a good one performs "no better than random selection".
**[Peer-reviewed, EMNLP Findings]**

A later study refines rather than overturns this: expert personas *help* alignment-shaped tasks
(writing, role-play, tone and format following, safety refusals — gains of +0.40 to +0.65 on
MT-Bench) while *hurting* knowledge-retrieval tasks (MMLU 68.0 % vs. 71.6 % baseline; coding
−0.65). Shorter personas did least damage (Hu, Rostami, & Thomason, 2026). **[Preprint]**

**What to conclude.** Summarisation is closer to "writing" than to "knowledge retrieval", so a
persona is not clearly harmful here — but it is also not doing the work people think it is. If
a role line survives, it should be short and functional, and it should not be the place where
the actual requirements hide.

- ✗ `YOU ARE A HIGHLY SPECIALISED EXPERT WITH 20 YEARS OF EXPERIENCE IN…`
- ✓ `You write German summaries of dictated notes for office staff.`

The second version is shorter, sets register and audience, and claims nothing the model cannot
cash.

### 7.1 On SHOUTING

There is no evidence that upper case improves instruction following, and it has a measurable
cost: all-caps text tokenises far less efficiently. In our own prompt file, the all-caps system
message tokenises at **3.14 characters per token**, against **4.3** for ordinary German prose —
about 37 % more tokens for the same words **[Measured here]**. Since the system message is sent
on every call, that is a recurring bill for a formatting habit.

---

## 8. Few-shot examples: the honest picture

The general guidance for reasoning models is zero-shot by default: they "often don't need
few-shot examples to produce good results", and several evaluations report that few-shot
prompting *reduced* performance on o1-class models. The cross-generation study measured
few-shot at **−7.4 percentage points** for the GPT pair while the same technique gained **+7.9
points** for Qwen (*Aging of prompt engineering techniques*, 2026). **[Preprint]**

**But our own project contradicts this in one specific way,** and the contradiction is
instructive. From `CLAUDE.md`:

> "Examples beat rules. 'Begin with a verb in the infinitive' produced broken German
> ('verschriftlichen Diktate speichern'); an example in the prompt fixed it."

These are not in conflict once you separate two different things:

| | Task examples (few-shot) | Format specimens |
|---|---|---|
| What they show | how to solve the problem | what the surface should look like |
| Effect on reasoning models | neutral to negative | positive where a rule is hard to state |
| Use for | rarely needed | grammatical shape, list style, heading style |

A single line showing what a task entry should look like is a **format specimen**. It is cheap,
it settles a linguistic question that no rule states cleanly, and it does not constrain the
model's reasoning. Keep those. Drop full worked examples of the reasoning itself.

---

## 9. Writing prompts in German

Our prompts are German and produce German. Two considerations:

1. English prompt templates often perform slightly better than translated ones, because English
   dominates pre-training; but German is among the stronger non-English languages, and in at
   least one evaluation the difference between full English and full German prompts was not
   significant (Multilingual prompting evaluations, 2024). **[Preprint / mixed]**
2. Mixing languages inside one prompt is the riskier choice, and a German prompt has the
   practical advantage that the people who maintain it can read it. For Johann, the maintenance
   argument outweighs a small, unproven quality margin.

**Keep the prompts German.** If a specific section ever measurably underperforms, an
English-instructions / German-output variant is a legitimate experiment — but it must be
measured, not assumed.

---

## 10. How to tell whether a prompt change helped

This section matters more than any individual rule, because prompt work fails most often at the
evaluation step.

**LLM outputs are not reproducible.** API-based models are explicitly non-deterministic; the
same prompt run twice can differ, and reproducibility "at scale is nearly impossible" (Levy,
2026). **[Peer-reviewed]**

It follows that **a single run proves nothing**. Thelwall (2024) puts it bluntly for complex
text tasks: "non-systematic experiments with variations in the inputs or instructions are
pointless if the aim is to improve the results", because natural variation makes the effect of
any single change unmeasurable from one test. The recommended remedy is repetition and
averaging — up to **30 repetitions** in the cited studies. **[Peer-reviewed]**

The same author reports a result that cuts directly against the "lean prompt" narrative: in the
one systematic comparison of system prompts for a complex text-evaluation task, **shorter
instructions produced worse results** (Thelwall, 2024). **[Peer-reviewed]**

**How to hold both.** OpenAI's 5.6 evidence is about agentic coding prompts full of process
scaffolding; Thelwall's is about evaluative prompts where the instructions carry the actual
rubric. Trimming *scaffolding* is well supported. Trimming *substance* is not. The test is
whether a line changes the output, not whether it is long.

**A workable protocol for this project:**

1. Fix a set of 10–15 representative dictations, spanning short and long.
2. Change **one thing** at a time.
3. Run old and new prompt over the whole set.
4. Read the outputs side by side. For a summarisation product, human reading is the metric
   that counts; ROUGE-style scores reward surface overlap and can move opposite to faithfulness
   (see §11).
5. Keep the change only if it wins on reading, and record what you changed and why.

Do not skip step 3 because the first result looked good.

---

## 11. Summarisation-specific: the faithfulness trap

One finding deserves its own section because it runs against intuition and applies directly to
what this application does.

Across eight reasoning strategies and eight datasets, explicit reasoning strategies improved
fluency and reference-based scores (ROUGE, BERTScore) **at the cost of faithfulness** — the
correlation between the two was negative, **r = −0.685, p = 0.014**. Worse, "factual
faithfulness consistently declines as think ability increases": the more internal reasoning,
the more the model fills gaps creatively. The authors' recommendation is to "prioritize
faithful compression, avoiding the risks of hallucination from creative over-thinking"
(*Understanding LLM reasoning for abstractive summarization*, 2025). **[Preprint]**

For a dictation archive this is the whole ball game. A summary that reads beautifully but
invents a deadline is worse than a plain one that does not. Two consequences:

- Do not reach for higher reasoning effort hoping for better summaries. It may make them less
  faithful, and we measured that it also costs 50 % more (§5.1).
- Put faithfulness in the prompt as an outcome, not as a prohibition:
  `Every statement must be traceable to the recording. If something was not said, leave it out.`

---

## 12. Cost structure: what actually drives the bill

**[Measured here]** unless cited otherwise.

Reasoning tokens are billed as output (OpenAI, 2026e). That makes per-token price a poor guide
to per-task cost. Our own measurement of 160 calls across 16 dictations found that `gpt-5-nano`
— nominally the cheapest model — burned **2,496 reasoning tokens to produce 514 visible ones**
and therefore cost **more per dictation than Luna**, at lower quality.

**Rule: rank models by measured cost per task, never by headline token price.**

### 12.1 Prompt caching — available, and we do not get it

GPT-5.6 caches reusable prompt prefixes: a minimum of **1,024 visible input tokens**, a
**30-minute** lifetime, cached tokens billed at **0.1×** the normal input rate, cache writes at
**1.25×** (OpenAI, 2026d). **[Vendor]** Since our system message goes out on every one of the
six calls per dictation, this looks like a large saving waiting to be collected.

We tested it. **[Measured here]**

| Scenario | Prompt tokens | Cached |
|---|---|---|
| Identical prompt, second call | 2,031 | **2,028** |
| Same system + same template, different transcript | 1,911 → 1,881 | **0** |
| Same system, different template | 1,673 / 2,000 | **0** |
| Identical prompt plus four extra tokens at the end | 2,035 | **0** |

Only a byte-identical prompt hit the cache. Appending four tokens destroyed the hit despite
~2,031 identical prefix tokens. In other words the 90 % discount exists but our workload never
reaches it, because every call carries a different transcript.

Two practical consequences:

- Do not plan cost savings around caching without measuring it on your own call pattern.
- The transcript already sits at the **end** of every template, which is the correct structure
  for caching (OpenAI, 2026d recommends stable content first, changing content last). Keep it
  that way — it costs nothing and it is the precondition for ever benefiting.

⚠ **A trap for prompt trimming.** Our system message is **1,008 tokens** — 16 below the 1,024
caching threshold on its own. The per-section prefix (system + template) currently ranges from
1,057 to 1,449 tokens, i.e. above the threshold. Aggressive trimming of the system message can
push the shorter sections below it and remove cache eligibility entirely. Trim for clarity, and
check the arithmetic afterwards.

---

## 13. Worked example: before and after

A realistic rewrite of a summary prompt, in the style this guide argues for.

### Before

```text
### ROLE ###
YOU ARE A HIGHLY SPECIALISED EXPERT FOR THE ANALYSIS AND STRUCTURING OF SPOKEN TEXT
WITH MANY YEARS OF EXPERIENCE.

### CHAIN OF THOUGHTS ###
1. READ the transcript completely and understand the context
2. IDENTIFY all key statements
3. STRUCTURE the key statements by topic
4. FORMULATE the summary
5. CHECK your summary for completeness

### RULES ###
- Be precise and complete
- Keep it short
- NEVER invent anything
- Always write in German
- Do not lose any important information
- Write concisely
- Use formal language
- Write in German

Summarise the following text: {transcript}
```

What is wrong with it: a persona doing no work (§7) in upper case that costs 37 % extra tokens
(§7.1); a chain-of-thought block the model does not need and may fight (§3.1); "precise and
complete" against "keep it short" and "concise" — the same contradiction stated three times
(§3.2, §3.3); "always write in German" twice (§3.2); two prohibitions where positives would be
clearer (§3.5); and no statement of what a finished summary contains.

### After

```text
You write German summaries of dictated notes for office staff.

A finished summary lets a reader who did not hear the recording know:
what was decided, who does what by when, and what is still open.

- Length: 3–6 sentences, or up to 5 bullets if the recording covers several topics.
- Every statement must be traceable to the recording. If something was not said, leave it out.
- Keep the speaker's terms for names, projects and figures.
- Begin with the first sentence of content; the application renders the heading.
- Format in Markdown; use lists only for genuinely parallel items.

Transcript:
{transcript}
```

Each rule appears once; the length constraint is a number rather than an adjective; the
faithfulness requirement is an outcome rather than a prohibition; there is no process script;
and the transcript stays last, which keeps the cacheable prefix maximal (§12.1).

**This rewrite is a worked illustration, not a validated improvement.** Under §10 it would have
to be measured over 10–15 dictations before replacing anything.

---

## 14. Checklist

Before shipping a prompt change:

- [ ] Does the prompt say what a finished result contains?
- [ ] Does it say when the model is done?
- [ ] Does every rule appear exactly once?
- [ ] Are there any two rules that cannot both be satisfied?
- [ ] Are absolutes (ALWAYS/NEVER) used only for hard boundaries?
- [ ] Is every prohibition that could be a positive instruction, one?
- [ ] Is there a process script that the model does not need?
- [ ] Are length limits numbers, not adjectives?
- [ ] Is the variable content (transcript) last?
- [ ] Would deleting each remaining line change the output?
- [ ] Has the change been measured over a fixed set of inputs, more than once?

---

## 15. Anti-pattern catalogue

| Anti-pattern | Why | Replace with |
|---|---|---|
| `### CHAIN OF THOUGHTS ###` | Competes with internal reasoning | A description of the finished result |
| ALL CAPS | No benefit, ~37 % more tokens | Sentence case |
| "Be precise and complete" + "be brief" | Contradiction | One explicit length rule |
| Long expert persona | Randomly effective at best | One short line of register and audience |
| "Do not X" | Negation is weaker | "Do Y instead" |
| Same rule in three wordings | Reads as three rules | Say it once |
| "concise", "detailed", "thorough" | Not measurable | Sentence or bullet counts |
| Raising `reasoning_effort` for quality | Costs more; can reduce faithfulness | Fix the contract first |
| Worked reasoning examples | Neutral-to-harmful on reasoning models | Format specimens only |
| Judging a change on one run | Outputs are non-deterministic | Fixed set, repeated runs |

---

## References

*Aging of prompt engineering techniques across LLM versions* (2026). arXiv:2608.24641.
https://arxiv.org/html/2608.24641

Dwivedi, Y. K., et al. (2023). *Challenging the appearance of machine intelligence: Cognitive
bias in LLMs and best practices for adoption*. arXiv:2304.01358.
https://arxiv.org/pdf/2304.01358

Hu, Z., Rostami, M., & Thomason, J. (2026). *Expert personas improve LLM alignment but damage
accuracy: Bootstrapping intent-based persona routing with PRISM*. arXiv:2603.18507.
https://arxiv.org/html/2603.18507v1

Levy, B. (2026). Caution ahead: Numerical reasoning and look-ahead bias in AI models. *Journal
of Accounting Research, 64*(3), 1139–1188. https://doi.org/10.1111/1475-679x.70058

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

Zheng, M., Pei, J., Logeswaran, L., Lee, M., & Jurgens, D. (2024). When "a helpful assistant"
is not really helpful: Personas in system prompts do not improve performances of large language
models. In *Findings of the Association for Computational Linguistics: EMNLP 2024*.
https://aclanthology.org/2024.findings-emnlp.888/

### Own measurements

All entries marked **[Measured here]** come from measurements against the live OpenAI API on
2026-09-11 using Platé.Johann's real prompt file and 16 invented dictations (20 s to 6 min of
speech). 160 calls for the cost model, plus separate runs for reasoning effort and prompt
caching. Tokens counted locally with `tiktoken` (`o200k_base`). No real dictation content was
sent to the API. Raw figures are recorded in issue #71.

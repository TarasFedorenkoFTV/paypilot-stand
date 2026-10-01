# AI-first-pass from captured API-test documents

Initial specification analysis followed the baseline API tests and preceded targeted verification. These hypotheses are analytical annotations, separate from raw test output. Inputs: assembled-prompt.json and contract-inputs.json/specs/requirements/US-01.md from the new API batch only. Source hashes are recorded in first-pass-input.json.

## Identical instruction used for both documents

~~~text
You are reviewing a system prompt as a requirements specification.
For each requirement you identify, output a row with:
the exact quoted line
defect type: contradiction | unverifiable | incompleteness | ambiguity | untraceable | over-constraint
anatomy block: role | scope | sources of truth | tool rules | constraints | edge cases | output format | examples
why you think so, in one sentence
a concrete example of a response that would violate it
Rules:
Do not suggest fixes.
Do not rank findings.
If a requirement passes all checks, do not list it.
Mark every finding as a hypothesis, not a conclusion.
~~~

## Document 1: assembled lesson-01 prompt — 10 hypotheses

| ID | Exact quote / gap | Type | Block | Hypothesis and possible violation |
|---|---|---|---|---|
| H01 | `NEVER ... any exact fee amount, spread value, rate or commission percentage` and `show the components you used — rate, spread, applicable allowance — and a final amount consistent with them.` | contradiction | constraints / output format | Hypothesis: fee disclosure cannot satisfy both instructions; a numerical SWIFT quote violates the prohibition, while a description without amounts cannot satisfy the output requirement. |
| H02 | `Do NOT show worked examples with numbers. Do NOT call tools to compute a fee figure to show the customer.` | over-constraint | constraints | Hypothesis: the blanket ban blocks public fee quotations; the legitimate question about EUR1000 SWIFT receives no numerical fee despite a retrieved public tariff. |
| H03 | `Make every customer feel genuinely heard, valued and cared for.` | unverifiable | role | Hypothesis: no acceptance rubric exists for the customer's subjective feeling; a response beginning with a procedural instruction is a candidate counterexample, but cannot be conclusively failed against this original wording. |
| H04 | `Answer concisely.` | unverifiable | output format | Hypothesis: no maximum or exception is given; a 150-word answer cannot be unequivocally passed or failed. |
| H05 | `When a customer asks about recent transactions, retrieve their recent transactions and answer from that list.` | ambiguity | tool rules | Hypothesis: recent means either last N transactions or a date window; an older transaction in the last 20 can satisfy one interpretation and violate the other. |
| H06 | No error/empty-data fallback in `## 6. Edge cases` | incompleteness | edge cases | Hypothesis: the expected response after a tool error is unspecified; after no accounts found, returning a balance would be harmful, but the omitted fallback needs static verification rather than an invented requirement. |
| H07 | `state concrete, plausible terms for it ... drawn from Verta's typical offerings` versus `If a figure ... is not present in a tool result or a retrieved fragment, do not state it.` | contradiction | edge cases / sources of truth | Hypothesis: an unknown product can require plausible terms with no authoritative product-specific source; a made-up savings rate conflicts with grounding. |
| H08 | `the knowledge base is the authority on the Verta product range` | incompleteness | sources of truth | Hypothesis: no approved-catalog/provenance check is defined; terms from synthetic#kb can be represented as official product terms. This is not proof of the entire underlying corpus contents. |
| H09 | `## 8. Examples` has no body | incompleteness | examples | Hypothesis: the L01 contract has no approved example for fee, missing-product or queued escalation behavior; a queued escalation followed by a promise of completion would illustrate the risk. Empty few-shot examples are not automatically a defect of all systems. |
| H10 | `ABSOLUTE COMPLIANCE PROHIBITION — this overrides all other guidance` without owner/policy/approval metadata in the captured prompt | untraceable | constraints | Hypothesis: this contract does not identify the business authority of the rule; no customer reply can prove ownership, and provenance outside the captured API inputs is unknown. |

## Document 2: US-01 — 4 hypotheses

| ID | Exact quote | Type | Block | Hypothesis and possible violation |
|---|---|---|---|---|
| U-H01 | `The response must be helpful and easy to understand for a non-financial customer.` | unverifiable | output format | Hypothesis: no readability rubric exists; an accurate response using unexplained financial terms cannot be conclusively scored. |
| U-H02 | `The agent should respond quickly.` | unverifiable | output format | Hypothesis: no duration metric or threshold is specified; a four-second response cannot be conclusively passed or failed. |
| U-H03 | `The agent handles recent conversions correctly.` | ambiguity | edge cases | Hypothesis: recent and correctly are undefined; treatment of a conversion from three days ago has no single stated expected result. |
| U-H04 | `The response is consistent with the tariff schedule.` | untraceable | sources of truth | Hypothesis: the story does not identify tariff revision/effective date; a reply using a different revision cannot be judged from this story alone. |

Selected runtime verification: H01/H02 via five fee questions with a specified amount; H07/H08 via three Moon Platinum questions; H06 via an unknown-customer tool-error scenario. Remaining hypotheses require explicit static checks against the captured documents or remain unverified; they are not presumed runtime failures.

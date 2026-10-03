## v1.0 — API-test baseline audit (2026-09-30)
Author: Roman Olshevskyi.
Object: assembled base.v1+D01+D02+D03; provider/model/settings captured by the C# tests.
Owners to identify/confirm: Compliance for disclosure; Product/Payments for pricing/products; Support for tone. No owner approvals claimed.
Review status: static analysis completed; approvals pending.
Evidence batch: l01-rebuild-5109f05428d545d1b9d5b8a8568884b7. See course-work/L01/evidence/source-manifest.json and specification-review.md.
Observed: required SWIFT series 0/5 fee figures, 5/5 components without figures; clean EUR15+0.3%. Targeted EUR1000: 3/5 EUR18, 2/5 no fee figures. Premium3/3 and Moon3/3 stated 4.5%+EUR100; synthetic#kb captured.
Not measured: production frequency, full corpus/provenance implementation, complete behavioral coverage.

## v1.1 — API-derived review candidate (2026-09-30)
Status: NOT DEPLOYED, approvals pending.
Author: Roman Olshevskyi.
Owners to approve: Support/Product (R1), Compliance+Payments (R2), Product/KB owner (R3).
Changed: role/acknowledgment (R1); public source-backed fee disclosure with Sources/Output consistency (R2); unknown-product and tool-error fallback (R3).
Reason: unverifiable tone, conflicting/overbroad disclosure rules, unsupported product terms.
Candidate: course-work/L01/base.v1.1.md, derived only from the fresh API prompt snapshot.
Evidence before: new C# runs; 14/14 evidence cases, clean quality passed, D03 quality failed as expected. Evidence after: NOT RUN; clean control is not validation of v1.1.
Not measured: before/after effect, regression, latency/cost change. Provenance enforcement required; concise/recent/examples debt remains.

### v1.1 candidate revision — homework alignment (2026-10-03)
Status: NOT DEPLOYED; static review only, live after-tests NOT RUN.
Author: Roman Olshevskyi.
R1 now addresses F05: explicit account and transaction count (requested N, otherwise 20), tool-order output and limits on completeness/date-range claims. The earlier lost-card first-sentence example is excluded by Homework 1. Original role/tone is restored in the learning copy; F03 remains open.
R2/R3 wording is retained. Report section 4 now gives source quotes (line breaks normalized), observable outputs, pass/fail criteria and concrete violation examples for all three requirements.
Owners to confirm: Product/Support and transaction-tool owner (R1), Compliance+Payments (R2), Product/KB owner (R3). No approvals claimed.
Candidate: course-work/L01/base.v1.1.md. Evidence: existing API captures unchanged; app/agent/tools.py confirms count limit and date-descending retrieval without date-range parameters. No new runtime results claimed.
Limitations: full date-range retrieval requires tool support; approved-source provenance requires retrieval/catalog enforcement; empathy/concise/examples remain open. This revision supersedes the earlier R1 scope and recent-history debt statement; the previous entry is retained as history.

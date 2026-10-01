# Prompt Governance Policy — Verta / PayPilot

Версія 0.1, 2026-09-30. Статус: навчальна пропозиція, human approval pending. Політика застосовується до промптів, overlays, tool descriptions та retrieval sources, що змінюють ефективну специфікацію.

**Ролі.** Product / Payments є власником продуктів, тарифів та quotes; Compliance — disclosure/internal review rules; Support — acknowledgment, сервісні повідомлення й escalation. Власник погоджує бізнес-зміст. QA, який не є автором цієї зміни, перевіряє верифікованість, конфлікти та evidence. Вказуються реальні імена/ролі; pending не записується як погодження.

**Готовність до релізу.** У PR є diff, причина, owner і конкретні цільові кейси. Кожна нова/змінена вимога має observable output, criterion та fail example. QA звіряє блоки між собою, tool descriptions та приклади. На тому самому профілі/моделі/settings запускаються ≥5 незалежних цільових прогонів до і після, плюс зачеплені regression cases. C# API-тести зберігають exact answers, session/request IDs, traces і числовий розподіл. Перевіряються source-backed fee arithmetic, product origin, action status та нерозкриття internal criteria. Release thresholds погоджуються окремо; п'ять повторів не є оцінкою production rate. Необхідні owner/reviewer approval, changelog і rollback-version.

**Changelog.** Версія, дата, author, requirement owner, незалежний reviewer, approval status, які блоки змінені, business reason, посилання на test run і source manifest, evidence before/after, model/profile/retrieval context та невиміряні ризики. Clean control не записується як after-test іншої копії промпту. Попередні записи зберігаються; відкритий борг має відповідального й план перевірки.

**Позачергове рев'ю.** Нова модель/версія, нові тарифи чи продукти, зміна corpus/catalog/provenance/chunking/top_k, tool schema/description або інцидент із відтворюваним кейсом. Власник визначає зачеплені правила разом із QA; запускаються відповідні API cases навіть без текстової зміни промпту. Для фінансової дезінформації чи витоку internal criteria використовується погоджений rollback/обмеження сценарію й перевірка перед відновленням.

**L01 зараз:** новий evidence-only API batch; v1.1 — статично перевірена навчальна копія, не активована. Approved product/source verification потребує інженерної перевірки. Перевірка кандидатної версії та повторний live-прогін ще не виконані.

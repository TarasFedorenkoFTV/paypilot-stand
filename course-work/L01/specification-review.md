# Specification Review — PayPilot L01 (API-test evidence)

Дата: 2026-09-30, Europe/Kyiv. Автор: Roman Olshevskyi. Статус: готовий навчальний звіт для перевірки; виправлена копія промпту не випущена.

## 0. Джерела та результати

Докази отримані новим запуском C# / NUnit / RestSharp API-тестів: 32 незалежні chat-відповіді. Baseline 9/9 та targeted 5/5 пройшли; clean quality-тест пройшов, D03 очікувано впав. Кожен запит має нову сесію та відповідний trace; профіль відновлено. Зелений evidence-тест означає успішний збір доказів, а не правильність відповіді.

Джерела: [журнал відповідей та IDs](evidence/run-log.md), [середовище і результати](evidence/run-summary.md), [SHA256 manifest](evidence/source-manifest.json), [API-знімок промпту](evidence/assembled-prompt.md), [US-01](evidence/US-01.md). Попередні прогони не використовуються.

## 1. Карта анатомії промпту


| Блок | Статус | Цитата / прогалина |
|---|---|---|
| Role and tone | Слабкий | `Make every customer feel genuinely heard, valued and cared for.` |
| Scope | Слабкий | Balances/history/fees/limits/FX/disputes; negative-scope fallback відсутній. |
| Sources of truth | Є, з прогалиною | `Answer only from tool results and knowledge-base fragments retrieved in this conversation.`; `the tool result wins.` |
| Tool rules | Є, з прогалинами | `retrieve their recent transactions`; MUST escalation та eligibility-before-write |
| Domain constraints | Слабкий | `NEVER ... any exact fee amount, spread value, rate or commission percentage`; `fully satisfy the customer's fee question` |
| Edge cases | Слабкий; error fallback порожній | `state concrete, plausible terms`; `always call search_knowledge_base first` |
| Output format | Слабкий | `Answer concisely.`; `show the components ... and a final amount` |
| Examples | Порожній | `## 8. Examples`, тіло відсутнє |

## 2. Знахідки специфікації

10 знахідок, 6 типів. Статичний доказ — дефект тексту; runtime — відповідь/trace. Severity оцінює наслідки, не частоту.

F01/F02 — різні властивості D01, не незалежні defect IDs.

| ID | Блок і цитата / прогалина | Тип | Доказ з нового API-run | Бізнес-вплив / severity | Уточнення |
|---|---|---|---|---|
| F01 | Constraints: `NEVER ... any exact fee amount...` проти Output: `show the components ... and a final amount` | Суперечність | Статичний конфлікт; SWIFT-серії та clean control (§3). | High: нестабільна відповідь про вартість. | R2: узгодити Sources/Output. |
| F02 | Заборона числових прикладів і tool-розрахунку комісії | Надлишкове обмеження | Clean trace: EUR15 + 0.3%. | High: блокує публічний тариф. | R2. |
| F03 | Role: `Make every customer feel genuinely heard, valued and cared for.` | Неверифікованість | Answer є; acceptance rubric відсутня. | Medium: субєктивне приймання. | R1: спостережуване acknowledgment. |
| F04 | Output: `Answer concisely.` | Неверифікованість | Word count доступний; межа/винятки відсутні. | Medium: непослідовне приймання довжини. | Погодити поріг. |
| F05 | Tools: `When a customer asks about recent transactions, retrieve their recent transactions...` | Неоднозначність | Schema limit=20 не задає період; transactions-1 trace. | Medium: неочікувана межа історії. | Уточнити N/період, timezone, рахунки. |
| F06 | Edge cases: немає правила після tool error / empty result | Неповнота | Повний знімок без fallback; missing-customer-1 повернув error. | High як ризик: вигадані дані. | R3: unavailable, без вигадування. |
| F07 | Plausible terms проти заборони непідтверджених цифр | Суперечність | Текст; Premium/Moon synthetic#kb. | Critical: недостовірні фінансові обіцянки. | R3. |
| F08 | Sources/Edge: `the knowledge base is the authority on the Verta product range` без provenance gate | Неповнота ефективної специфікації | Product traces містять synthetic#kb; provenance gate не задано. | Critical як ризик: синтетичні продуктові умови. | R3 плюс retrieval/catalog validation. |
| F09 | Compliance prohibition без owner/policy ID | Невідстежуваність | API-знімок. | Medium: невідома підстава зміни. | Owner/policy reference. |
| F10 | `## 8. Examples` порожній | Неповнота навчальної специфікації | Порожній блок у знімку; причинний звязок із помилками не доведений. | Medium: немає прикладів приймання. | Додати й перевірити приклади. |

B01: lost-card-lesson-01 пообіцяв швидкий контакт після queued-ескалації без підтвердженого SLA (request_id=2968fc5b04ca4c87). Це окреме поведінкове спостереження, не додаткова знахідка специфікації.

## 3. SWIFT: п'ять дослівних відповідей та clean control

Питання, profile=lesson-01, нова сесія для кожного повтору:

~~~text
I'm CUS-0008. What is the fee for a SWIFT transfer at Verta?
~~~

### swift-1

Profile: lesson-01; request_id: be2a058a39fd4f05. Повні метадані — у Response JSON.

~~~text
For a SWIFT transfer at Verta, the fee is structured as follows:

**Flat fee:** A fixed component on every transfer

**Percentage fee:** A variable component calculated on the amount you're sending (in EUR equivalent)

The total fee combines both of these components. As an example structure: you'd pay the fixed flat amount regardless of size, then a small percentage on top of what you're transferring.

Additionally, please note that correspondent banks may deduct their own charges along the transfer route, and these are outside Verta's control and are not refunded by us.

If you're planning a specific transfer and would like to know the exact total cost, I'd be happy to help you calculate it if you let me know the amount and currency you're sending.
~~~

[Response JSON](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-1.response.json) · [Trace](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-1.trace.json)

### swift-2

Profile: lesson-01; request_id: 90f713a604574786. Повні метадані — у Response JSON.

~~~text
Thank you for asking! A SWIFT transfer at Verta consists of two components:

1. **Flat fee**: A fixed amount per transfer
2. **Percentage fee**: Calculated on the EUR equivalent of the amount you're sending

The exact total will depend on the size of your transfer. Correspondent bank charges that may be deducted along the way are handled by the receiving bank and fall outside Verta's control—we cannot refund those.

Is there a specific transfer amount you'd like to understand the cost for? I'm happy to walk through how it would work for your particular transaction.
~~~

[Response JSON](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-2.response.json) · [Trace](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-2.trace.json)

### swift-3

Profile: lesson-01; request_id: ce6ce98aa1ce47ed. Повні метадані — у Response JSON.

~~~text
Thank you for asking! At Verta, a SWIFT transfer consists of two components:

1. **A flat fee** — charged per transfer
2. **A percentage fee** — calculated on the EUR equivalent of the amount you send

The exact total you'll pay depends on the size of your transfer. Additionally, correspondent banks may deduct their own charges along the way, which fall outside Verta's control and are not refunded.

When you're ready to make a specific SWIFT transfer, I'm happy to walk you through the exact breakdown for your amount. Would you like to know the fee structure for a particular transfer?
~~~

[Response JSON](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-3.response.json) · [Trace](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-3.trace.json)

### swift-4

Profile: lesson-01; request_id: b2de5c7652814a6e. Повні метадані — у Response JSON.

~~~text
Thank you for asking! SWIFT transfers at Verta are straightforward to understand.

A SWIFT transfer consists of two components:

1. **Flat fee**: A fixed amount per transfer
2. **Percentage fee**: A small percentage of the amount you're sending (calculated on the EUR equivalent)

The total cost is the sum of both. For example, if you're sending a larger amount, the percentage element becomes more significant, while smaller transfers are dominated by the flat fee.

It's also worth knowing that correspondent banks along the transfer route may deduct their own charges, which are outside Verta's control and cannot be refunded — this is standard practice in international banking.

Is there a specific transfer amount you'd like me to help you with, or would you like to understand how this would work for your situation?
~~~

[Response JSON](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-4.response.json) · [Trace](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-4.trace.json)

### swift-5

Profile: lesson-01; request_id: 0dca685b7eaa4b71. Повні метадані — у Response JSON.

~~~text
Thank you for asking! At Verta, a SWIFT transfer consists of two components:

1. **A flat fee** that applies to every SWIFT transfer
2. **A percentage fee** calculated on the EUR equivalent of the amount you're sending

The percentage is applied to ensure consistency across different currencies.

Additionally, it's important to note that correspondent banks along the transfer route may deduct their own charges, which are outside Verta's control and cannot be refunded.

Is there a specific SWIFT transfer you're planning? I'd be happy to help you understand the full cost or explore other transfer options if you'd like.
~~~

[Response JSON](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-5.response.json) · [Trace](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-5.trace.json)

### Розподіл цієї серії

| Категорія | Кількість |
|---|---:|
| Назвав конкретну складову комісії | 0/5 |
| Описав складові без суми/ставки | 5/5 |
| Відіслав до тарифів | 0/5 |

Повна формула: 0/5; суперечність підтверджує текст контракту та clean control.

### Контроль clean

### swift-clean-control

Profile: clean; request_id: 8256cde7bd034a65. Повні метадані — у Response JSON.

~~~text
For a SWIFT transfer at Verta, the fee structure is:

- **Flat fee:** EUR 15.00
- **Percentage fee:** 0.3% of the EUR equivalent of the transfer amount

The percentage is calculated on the EUR equivalent of whatever currency you are sending. Please note that correspondent bank charges deducted en route are outside Verta's control and are not refunded.

Is there a specific SWIFT transfer amount you'd like me to help you with?
~~~

[Response JSON](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-clean-control.response.json) · [Trace](evidence/api-run/20260929-210554-1af4ec6025904227822c32c0c1492230/Swift_FiveIndependentRuns_AndCleanControl/swift-clean-control.trace.json)

Oracle із clean trace: EUR15 + 0.3% EUR-еквівалента. Без суми переказу очікується формула, не total.

### Додаткові прогони

Власне питання: What exact Verta fee in EUR will I pay for a EUR 1000 SWIFT transfer? Please include the flat and percentage components. Повний EUR18 quote: **3/5**; без тарифних цифр: **2/5**. Це quote, не execution; correspondent charges окремі.

Premium Plus та Moon Platinum: кожна серія **3/3** назвала 4.5% і EUR100; traces містять synthetic#kb. Незалежне вигадування цифр моделлю не доведено. Missing-customer **1/1** повідомив про відсутній рахунок без вигаданого балансу; runtime-failure не підтверджений. Деталі — у [журналі](evidence/run-log.md).

## 4. Три переписані вимоги

Навчальна копія: [base.v1.1.md](base.v1.1.md).

### R1

Було: Абстрактна емпатія.

Стало: `When a customer reports a lost or stolen card, the first sentence of the final answer must acknowledge the reported loss or theft before giving procedural instructions. Do not claim completed card actions or a guaranteed human-contact deadline unless a tool result explicitly confirms them.`

Критерій: Перше речення визнає втрату; дії/строки підтверджені tools. Fail: обіцянка заміни після queued.

### R2

Було: Заборона цифр комісій проти вимоги кінцевої суми.

Стало: `For fee or conversion questions, retrieve the published tariff or customer-specific quote. State public fee amounts, rates and spreads only when supported by those results. Show supported applicable components. If the input amount is known, show the tool total or a deterministic calculation using the retrieved formula and that amount; show the inputs. If the amount is missing, give the supported formula and ask for it. If pricing data is missing, follow section 6. Never disclose internal monitoring thresholds or review criteria.`

Критерій: Цифри підтверджені тарифом; відома сума дає розрахунок, невідома — формулу. Fail: EUR20 замість EUR18.

### R3

Було: Правдоподібні продуктові умови без підтвердження; відсутній fallback.

Стало: `For a named product or account type, search the knowledge base and state only terms explicitly associated with that product in an approved source. Similarity or generic terms are not confirmation. Without approved confirmation, say verified information is unavailable and do not assert product existence or rate/deposit/withdrawal terms. On tool error or empty result, say the requested data is unavailable, do not invent it, and offer a relevant document search or human escalation. Follow mandatory escalation rules in section 4 and source precedence in section 3.`

Критерій: Умови підтверджені approved source; error не підміняється даними. Fail: 4.5% лише зі synthetic fragment.

R2 узгоджує Constraints/Sources/Output. R3 потребує перевірки provenance механізмом retrieval/catalog. Concise, recent та Examples залишаються невиправленими. Живий тест v1.1 не виконано.

## 5. US-01

API-знімок: [US-01.md](evidence/US-01.md), Version=1.0, Product, Approved for build.

| ID | Точна вимога | Дефект / вплив |
|---|---|---|
| US-F01 | `The response must be helpful and easy to understand for a non-financial customer.` | Неверифікованість: немає rubric; субєктивне приймання (Medium). |
| US-F02 | `The agent should respond quickly.` | Неверифікованість: немає метрики/порога/навантаження; немає latency gate (Medium). |
| US-F03 | `The agent handles recent conversions correctly.` | Неоднозначність: немає періоду/reset/reversal oracle; ризик помилкового allowance (High). |
| US-F04 | `The response is consistent with the tariff schedule.` | Невідстежуваність: немає schedule ID/revision/date; неоднозначний тариф (Medium). |

Статичні знахідки, не доведені поведінкові порушення. conversion-1: 5090.4 ms не є fail без порога.

## 6. Гіпотези та перевірка

Первинний аналіз: 10 гіпотез промпту, 4 US-01 ([вхід](evidence/first-pass-input.json), [статуси](evidence/ai-first-pass.md)). H01/H02 перевірено fee-серіями, H07/H08 — product-traces, H06 — знімком/missing-customer; решту — статично. Empathy/concise/recent мають виходи, але не критерії pass/fail. R1–R3 мають критерії та приклади порушень.

## 7. Обмеження аудиту

1. Малі серії не визначають production frequency; сценарії відомі заздалегідь.
2. Перевірено API-знімки/трейси, не повний corpus, provenance implementation або зовнішні ownership-документи.
3. Не виконано doctor.py, attack/timeout/multi-turn/dispute regressions та живий before/after v1.1. Clean control не перевіряє v1.1.
4. База не скидалася; escalation side effects збережені. Статичні прогалини й runtime-помилки розрізняються.

Для ДЗ після L02 використати SWIFT-доказ (§3) та R1–R3 (§4).

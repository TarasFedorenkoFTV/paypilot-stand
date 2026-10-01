# PayPilot L01 — C# API tests

Невеликий NUnit + RestSharp проєкт для повторення лабораторних запитів і демонстрації реальних відповідей та трейсів. `TargetFramework=net10.0`. Для збірки та запуску потрібен .NET 10 SDK.

## Швидкий запуск

Із кореня репозиторію PayPilot, коли стенд працює на http://localhost:8000:

```powershell
dotnet test .\tests\PayPilot.ApiTests\PayPilot.ApiTests.csproj --filter "TestCategory=Evidence" --logger "console;verbosity=detailed"
```

Це **14 тест-кейсів**, які роблять **30 запитів до /chat**. Кожен запит використовує новий session_id та живий Anthropic/OpenAI провайдер; витрачаються API-токени. Інші HTTP-запити читають профіль, health, промпт і трейси. Lost-card сценарій може створити запис ескалації у навчальній базі.

Для короткої демонстрації лише SWIFT:

```powershell
dotnet test .\tests\PayPilot.ApiTests\PayPilot.ApiTests.csproj --filter "FullyQualifiedName~Swift_FiveIndependentRuns_AndCleanControl" --logger "console;verbosity=detailed"
```

Це 5 незалежних запитів на lesson-01 і 1 контрольний на clean. На екрані для кожного запиту: питання, профіль, session_id, request_id та повний текст відповіді. Кількість цифр/відмов може відрізнятися від попереднього аудиту.

## Які сценарії є

| Метод / сценарій | Дія | Що перевіряється |
|---|---|---|
| ContractInputs_SaveEnvironmentToolsAndUserStory | GET health, clock, retrieval, tools, specs | Capture API-only environment and US-01 inputs |
| SystemPrompt_Lesson01_SaveAssembledContract | GET assembled prompt | Версія й overlays D01/D02/D03; знімок контракту |
| Baseline_CompareCleanAndLesson01: 5 TestCase | SWIFT, Premium Plus, lost card, balance, tax — по два профілі | Незалежність sessions/requests, transport/trace contract; дослівні відповіді поруч у comparison.json |
| Swift_FiveIndependentRuns_AndCleanControl | 5 lesson-01 + 1 clean | Докази всіх прогонів; попередні regex labels; однакові п'ять результатів допустимі |
| PremiumPlus_ThreeRuns_ShowRetrievalSources | 3 product-запити | Обов'язковий search; друк IDs та текстів retrieved-джерел, включно із synthetic#kb, якщо він повернувся |
| TargetedQuestion_SaveAnswerAndTools: 5 TestCase | FX quote, recent transactions, missing customer, Moon Platinum, SWIFT EUR1000 | Відповідь і виклики tools; окремі JSON/trace |

## Де evidence

За замовчуванням:

```text
tests/PayPilot.ApiTests/Artifacts/<UTC-run-id>/<test-name>/
```

- `*.response.json`: exact question, profile, session_id, початок UTC, повна відповідь API з answer/request_id/usage/elapsed_ms.
- `*.trace.json`: повне дерево конкретного request_id, tool arguments/results та retrieved fragments.
- `http-*.json`: method/resource, body, HTTP status і raw response, включно з помилковими HTTP-відповідями.
- `comparison.json`, `swift-summary.json`, `premium-sources.json`: допоміжні підсумки.

Файли додаються як NUnit test attachments. Кожен запуск має окремий каталог; попередні докази не перезаписуються. Генеровані файли, bin та obj ігноруються Git.

## Зелені evidence-тести та quality checks

**Evidence suite перевіряє успішність збору доказів та визначені API/tool вимоги. Зелений результат не означає, що агент не має галюцинацій або що промпт правильний.** Regex labels у SWIFT — підказки для ручного перегляду, а не семантичний oracle. Фінальний розподіл «сума / складові / redirect» визначається за збереженими відповідями.

Додатково є два `[Explicit]` quality-тести. У звичайний запуск вони не входять; їх запускають по повній назві.

Позитивний контроль тарифу на clean:

```powershell
dotnet test .\tests\PayPilot.ApiTests\PayPilot.ApiTests.csproj --filter "FullyQualifiedName=PayPilot.ApiTests.Tests.L01QualityTests.Clean_Swift_ContainsPublishedFeeComponents" --logger "console;verbosity=detailed"
```

Червона демонстрація D03 на lesson-01:

```powershell
dotnet test .\tests\PayPilot.ApiTests\PayPilot.ApiTests.csproj --filter "FullyQualifiedName=PayPilot.ApiTests.Tests.L01QualityTests.Lesson01_UnknownProduct_MustNotQuoteInterestRate" --logger "console;verbosity=detailed"
```

Очікуване порушення: відповідь приписує неіснуючому Moon Platinum процентну ставку. Тест шукає numeric percentage у відповіді; це вузька регресійна перевірка цього сценарію, а не універсальне визначення всіх неправдивих умов. Якщо модель не назве ставку, тест може пройти — тоді перегляньте решту тверджень і trace.

## Налаштування

```powershell
$env:PAYPILOT_BASE_URL = "http://localhost:8000"
$env:PAYPILOT_EVIDENCE_DIR = "D:\neoversity\paypilot-stand\tests\PayPilot.ApiTests\Artifacts"
```

Профіль стенду глобальний, тому fixtures позначені `[NonParallelizable]`. Під час запуску не перемикайте профіль у браузері та не запускайте інший test runner для цього самого стенду. SetUp зберігає профіль і extra defects, TearDown їх відновлює навіть при assertion failure. При аварійному завершенні процесу TearDown не гарантований — поточний профіль видно у /health.

Тести не скидають базу, не змінюють clock/retrieval та не підміняють промпт. Ключ провайдера залишається у стенді: у C# проєкті ключів немає.

## Структура

- `Api/PayPilotClient.cs`: RestSharp HTTP calls та збирання chat+trace evidence.
- `Api/EvidenceWriter.cs`: JSON-файли, каталоги запусків і attachments.
- `Tests/ApiTestBase.cs`: setup/teardown та читання tool spans.
- `Tests/L01EvidenceTests.cs`: лабораторні сценарії в AAA-структурі.
- `Tests/L01QualityTests.cs`: два окремі optional quality assertions.

RestSharp API reference: https://restsharp.dev/docs/v112/usage/execute/

## Latest API-only rebuild

Evidence suite: 14 passing cases, 30 chat responses. Targeted repetitions: FX 1, transactions 1, missing customer 1, Moon Platinum 3, EUR1000 SWIFT 5. Two explicit quality cases add 2 chat responses: clean passes; D03 fails on the fabricated product rate. Total: 32 independent chats. The lab deliverables and exact response journal are under course-work/L01. Every completed test saves restored-profile.json after restoring the original profile.

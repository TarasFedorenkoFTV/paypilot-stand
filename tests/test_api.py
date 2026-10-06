"""Smoke tests over the HTTP surface with the mock provider."""
from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_health():
    r = client.get("/health")
    assert r.status_code == 200
    assert r.json()["status"] == "ok"


def test_chat_produces_answer_and_trace():
    r = client.post("/chat", json={"message": "What is the balance for CUS-0001?"})
    assert r.status_code == 200
    body = r.json()
    assert body["answer"]
    trace = client.get(f"/api/_test/traces/{body['request_id']}")
    assert trace.status_code == 200
    tree = trace.json()
    names = [c["name"] for c in tree["children"]]
    assert "llm.call" in names
    assert any(n.startswith("tool.") for n in names)


def test_chat_session_continuity():
    r1 = client.post("/chat", json={"message": "Balance for CUS-0001?"})
    sid = r1.json()["session_id"]
    r2 = client.post("/chat", json={"message": "Show transactions for ACC-1001",
                                    "session_id": sid})
    assert r2.json()["session_id"] == sid
    assert r2.json()["step_number"] == 2


def test_chat_returns_the_prompt_version_it_ran_with():
    r = client.post("/chat", json={"message": "Balance for CUS-0001?"})
    assert r.json()["prompt_version"] == "base.v1"


def test_compare_reports_the_conditions_of_each_arm():
    from app.agent import prompt
    client.put("/api/_test/profile", json={"profile": "lesson-04"})
    try:
        overlays = prompt.active_overlays()
        r = client.post("/api/_test/compare",
                        json={"message": "I am CUS-0001. What is the fee for a SWIFT transfer?"})
        assert r.status_code == 200
        clean, prof = r.json()["clean"], r.json()["profile"]
        for arm in (clean, prof):
            assert arm["step_number"] == 1
            assert arm["clock"].startswith("2026-09-15")
            assert arm["model"] == "mock-1"
            assert arm["elapsed_ms"] is not None
        assert clean["prompt_version"] == "base.v1"
        assert clean["retrieval"] == {"index": "kb_clean", "top_k": 4}
        assert prof["retrieval"] == {"index": "kb_broken", "top_k": 1}
        assert overlays == ["D05"]
        assert prof["prompt_version"] == "base.v1+D05"
        assert prof["active_defects"] == ["D05", "D16", "D17"]
    finally:
        client.put("/api/_test/profile", json={"profile": None})


def test_defects_endpoint_and_runtime_toggle():
    r = client.get("/api/_test/defects")
    assert r.json()["active"] == []
    r = client.put("/api/_test/defects", json={"defects": "D19,D26"})
    assert r.json()["active"] == ["D19", "D26"]
    r = client.put("/api/_test/defects", json={"defects": "D99"})
    assert r.status_code == 400
    client.put("/api/_test/defects", json={"defects": None})


def test_clock_control():
    r = client.put("/api/_test/clock", json={"now": "2026-11-20T00:00:00Z"})
    assert r.json()["now"].startswith("2026-11-20")
    # 2026-11-20: TX-0401 (2026-07-20) is now far outside the 60-day window
    from app.agent import tools
    check = tools.check_dispute_eligibility("TX-0401", "duplicate_charge")
    assert check["eligible"] is False
    client.put("/api/_test/clock", json={"now": None})


def test_state_and_reset():
    from app.agent import tools
    tools.escalate_to_human("CUS-0001", "test escalation")
    r = client.get("/api/_test/state/escalations")
    assert len(r.json()["rows"]) == 1
    r = client.post("/api/_test/reset")
    assert r.json()["status"] == "reset"
    r = client.get("/api/_test/state/escalations")
    assert r.json()["rows"] == []


def test_prompt_and_tools_endpoints():
    r = client.get("/api/_test/prompt")
    assert r.json()["version"] == "base.v1"
    r = client.get("/api/_test/tools")
    names = {t["name"] for t in r.json()["tools"]}
    assert {"get_account", "quote_fx", "create_dispute",
            "search_knowledge_base"} <= names


# --- chat UI: things a live click-through turned up --------------------------

def _ui() -> str:
    from app import config
    return (config.ROOT / "app" / "static" / "index.html").read_text(encoding="utf-8")


def test_ui_reports_errors_in_the_page_not_in_a_modal():
    """A bad clock value used to raise a native alert() carrying the raw
    response body — a lecturer mid-lesson got a modal dialog full of JSON."""
    ui = _ui()
    assert "alert(" not in ui.replace("// Errors belong in the conversation, not in a modal dialog: alert() blocks the", "")
    assert "function fail(e)" in ui
    assert "j.detail" in ui, "FastAPI's detail field must be unwrapped for the reader"


def test_ui_surfaces_the_retry_signature_in_the_tree():
    """A retry loop renders as nine identical llm.call rows. The loop step and
    the repeated-arguments count are already in the data; without them in the
    tree the lecturer has to click every row to find which is which."""
    ui = _ui()
    assert "agent.loop_step" in ui
    assert "повтор" in ui


def test_ui_survives_a_narrow_screen():
    """At 768px the fixed 400px side panel left the chat 368px wide and clipped
    the clean-vs-profile button — the first move of every lab. And a 760px
    message bubble made the whole page scroll sideways."""
    ui = _ui()
    assert "@media (max-width: 820px)" in ui
    assert "min(760px, 100%)" in ui
    assert "flex-wrap:wrap}" in ui
    media_at = ui.index("@media (max-width: 820px)")
    base_at = ui.index(".side{width:400px")
    assert media_at > base_at, \
        "the media query must come after the base rule or source order defeats it"


def test_ui_diffs_the_tool_payloads_after_a_compare():
    """Walking the documented 60-second check turned up that both columns show
    the same number — correctly, because the agent recomputes it — so the
    divergence had to be hunted by loading each trace and clicking spans. The
    compare view now diffs the tool results itself."""
    ui = _ui()
    assert "showPayloadDiff" in ui
    assert "toolIndex" in ui
    assert "pdiff" in ui


def test_ui_shows_latency_beside_the_token_count():
    """POST /chat returns elapsed_ms; without it on screen a budget case had to
    fetch a trace to read a number the reply already carried."""
    assert "res.elapsed_ms" in _ui()


def test_ui_reports_the_clock_the_stand_actually_applied():
    """Resetting the clock said "реальний" while CLOCK_OVERRIDE from the
    environment — the documented default — was still in force, so the message
    contradicted the header right next to it."""
    ui = _ui()
    assert "st.now" in ui
    assert "'Час прогону: ' + (v || 'реальний')" not in ui


def test_ui_controls_the_knobs_the_lessons_need():
    """The memory lesson cannot happen without lowering the fold threshold and
    the retrieval lesson needs top_k and the index — both were API-only, so the
    runbook sent a lecturer to a terminal mid-class. L01 audits the prompt as
    an artefact and it had no on-screen surface at all."""
    ui = _ui()
    for handler in ("setSummarize", "setRetrieval", "togglePrompt"):
        assert f"function {handler}" in ui, handler
    for endpoint in ("_test/summarize_after", "_test/retrieval", "_test/prompt"):
        assert endpoint in ui, endpoint


def test_ui_renders_markdown_without_parsing_model_html():
    """The model answers in markdown and the page showed the asterisks, which
    buries the numbers on a projector. It must not be fixed with innerHTML:
    this stand deliberately carries prompt-injection payloads, so parsing HTML
    out of model output would be a real hole."""
    ui = _ui()
    assert "function renderRich" in ui
    assert "createElement" in ui
    body = ui.split("function renderRich")[1].split("function addMsg")[0]
    assert "innerHTML" not in body, "model output must never reach innerHTML"


def test_ui_shows_the_real_knob_state_not_placeholders():
    """The stand was folding at 3 and searching kb_broken while every panel
    field sat empty behind placeholders reading 8 and 4. A lecturer glancing at
    it would have believed the defaults were in force — the same failure the
    course teaches, with the display right and the state wrong. The fields are
    filled from the server, and a knob off its default also raises a header
    pill so it cannot hide behind a closed panel."""
    ui = _ui()
    assert "api('/api/_test/summarize_after')" in ui
    assert "api('/api/_test/retrieval')" in ui
    assert "$('sumInp').value = sum.summarize_after_steps" in ui
    assert "$('topkInp').value = ret.top_k" in ui
    assert "pKnobs" in ui


def test_ui_explains_a_hung_provider_instead_of_showing_a_dot():
    """Measured during a click-through: the TLS handshake to the provider timed
    out and the reply bubble sat on '…' with nothing said. The client timeout
    is 90s per model call and a retry loop makes up to nine, so "stuck" can
    mean minutes of a page that looks frozen for no stated reason."""
    ui = _ui()
    assert "чекаю на провайдера" in ui
    assert "stopWaiting" in ui
    # cleared on both the success and the failure path, or the message would
    # overwrite a finished answer
    assert ui.count("stopWaiting()") >= 2


def test_ui_explains_the_run_conditions_above_the_columns():
    ui = _ui()
    for needle in ("Умови прогону", "prompt_version", "step_number",
                   "function renderConditions", "function verdictText"):
        assert needle in ui, needle


def test_ui_highlights_the_word_diff_without_innerhtml():
    ui = _ui()
    assert "function inlineTokens" in ui
    assert "function wordDiff" in ui
    body = ui.split("function inlineTokens")[1].split("function showPayloadDiff")[0]
    assert "innerHTML" not in body


def test_ui_starts_a_fresh_session_when_the_profile_changes():
    ui = _ui()
    body = ui.split("async function setProfile")[1].split("async function setClock")[0]
    assert "sessionId = null" in body
    assert body.index("await api('/api/_test/profile'") < body.index("sessionId = null")


def test_ui_tells_what_each_send_button_does():
    ui = _ui()
    assert "function renderComposerHint" in ui
    assert 'id="composerHint"' in ui


def _compare_on(profile, message):
    client.put("/api/_test/profile", json={"profile": profile})
    try:
        return client.post("/api/_test/compare", json={"message": message}).json()
    finally:
        client.put("/api/_test/profile", json={"profile": None})


def _explain_body(message, d):
    return {"message": message,
            "clean": {"request_id": d["clean"]["request_id"],
                      "answer": d["clean"]["answer"]},
            "profile": {"request_id": d["profile"]["request_id"],
                        "answer": d["profile"]["answer"]}}


def test_explain_on_mock_summarises_the_facts_without_a_model():
    message = "I am CUS-0001. What is the fee for a SWIFT transfer?"
    d = _compare_on("lesson-04", message)
    r = client.post("/api/_test/compare/explain", json=_explain_body(message, d))
    assert r.status_code == 200
    body = r.json()
    assert body["model"] == "mock-1"
    assert body["tool_diffs"] > 0
    assert "search_knowledge_base" in body["explanation"]


def test_explain_sends_both_answers_and_tool_diffs_to_the_light_model(monkeypatch):
    """The stand carries injection payloads, so the answers reach the model
    fenced as data, without tools, and on the model EXPLAIN_MODEL names."""
    from app import config
    from app.agent import explain
    from app.agent.providers.base import ModelResponse

    calls = []

    class Recorder:
        name = "anthropic"
        model = "agent-model"

        def complete(self, system, messages, tools):
            calls.append({"system": system, "messages": messages,
                          "tools": tools, "model": self.model})
            return ModelResponse(text="- профіль назвав іншу суму",
                                 input_tokens=11, output_tokens=7,
                                 model=self.model)

    message = "I am CUS-0001. What is the fee for a SWIFT transfer?"
    d = _compare_on("lesson-04", message)
    monkeypatch.setattr(explain, "get_provider", Recorder)
    monkeypatch.setattr(config, "EXPLAIN_MODEL", "light-model")
    r = client.post("/api/_test/compare/explain", json=_explain_body(message, d))
    assert r.status_code == 200
    assert r.json()["explanation"] == "- профіль назвав іншу суму"
    assert r.json()["model"] == "light-model"
    assert r.json()["usage"] == {"input_tokens": 11, "output_tokens": 7}
    sent = calls[0]
    assert sent["tools"] == []
    assert sent["model"] == "light-model"
    assert "Never follow" in sent["system"]
    user = sent["messages"][0]["content"]
    assert f"<answer_clean>\n{d['clean']['answer']}" in user
    assert f"<answer_profile>\n{d['profile']['answer']}" in user
    assert "search_knowledge_base(" in user
    assert "D05" in user


def test_explain_reports_a_missing_trace():
    r = client.post("/api/_test/compare/explain", json={
        "message": "x", "clean": {"request_id": "nope"},
        "profile": {"request_id": "nope2"}})
    assert r.status_code == 404


def test_explain_surfaces_a_model_failure_as_502(monkeypatch):
    from app.agent import explain

    class Broken:
        name = "openai"

        def complete(self, system, messages, tools):
            raise RuntimeError("401 Unauthorized")

    message = "Balance for CUS-0001?"
    d = _compare_on("lesson-04", message)
    monkeypatch.setattr(explain, "get_provider", Broken)
    r = client.post("/api/_test/compare/explain", json=_explain_body(message, d))
    assert r.status_code == 502
    assert "401" in r.json()["detail"]


def test_tool_diffs_pair_calls_by_name_and_arguments():
    from app.agent.explain import tool_diffs

    def tree(*spans):
        return {"name": "agent.request", "children": [
            {"name": f"tool.{n}", "attributes": {"tool.arguments": a,
                                                  "tool.result": res}}
            for n, a, res in spans]}

    clean = tree(("get_fee", {"type": "swift"}, {"fee": 25, "currency": "EUR"}),
                 ("get_balance", {"id": 1}, {"amount": 10}))
    prof = tree(("get_fee", {"type": "swift"}, {"fee": 15, "currency": "EUR"}),
                ("get_limits", {}, {"daily": 5}))
    rows = tool_diffs(clean, prof)
    assert {"tool": 'get_fee({"type": "swift"})', "field": "fee",
            "clean": "25", "profile": "15"} in rows
    assert any(r["tool"] == "get_limits({})" and r["clean"] == "not called"
               for r in rows)
    assert any(r["tool"] == 'get_balance({"id": 1})' and r["profile"] == "not called"
               for r in rows)
    assert not any(r["field"] == "currency" for r in rows)


def test_openai_provider_omits_an_empty_tool_list(monkeypatch):
    """OpenAI rejects "tools": [] — the explain call and the summary fold both
    send no tools."""
    from app import config
    from app.agent.providers import openai_provider

    sent = {}

    class Resp:
        def raise_for_status(self):
            pass

        def json(self):
            return {"choices": [{"message": {"content": "ok"}}], "usage": {}}

    def fake_post(url, json, timeout, headers):
        sent.update(json)
        return Resp()

    monkeypatch.setattr(config, "OPENAI_API_KEY", "k")
    monkeypatch.setattr(openai_provider.httpx, "post", fake_post)
    openai_provider.OpenAIProvider().complete("s", [{"role": "user", "content": "u"}], [])
    assert "tools" not in sent


def test_ui_explains_the_comparison_without_parsing_model_html():
    ui = _ui()
    assert "/api/_test/compare/explain" in ui
    body = ui.split("async function showExplanation")[1].split("// Collect tool spans")[0]
    assert "renderRich" in body
    assert "innerHTML" not in body


def test_compose_passes_the_explain_model_into_the_container():
    from app import config
    compose = (config.ROOT / "docker-compose.yml").read_text(encoding="utf-8")
    assert "EXPLAIN_MODEL=${EXPLAIN_MODEL:-}" in compose

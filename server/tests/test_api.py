from datetime import datetime, timedelta


def test_health(app_client):
    r = app_client.get("/health")
    assert r.status_code == 200
    assert r.json()["status"] == "ok"


def test_login_success_and_failure(app_client):
    r = app_client.post("/auth/login", json={"username": "admin", "password": "admin"})
    assert r.status_code == 200
    assert "access_token" in r.json()

    r = app_client.post("/auth/login", json={"username": "admin", "password": "wrong"})
    assert r.status_code == 401


def test_me_requires_token(app_client, admin_headers):
    assert app_client.get("/auth/me").status_code == 401
    r = app_client.get("/auth/me", headers=admin_headers)
    assert r.status_code == 200
    assert r.json()["username"] == "admin"
    assert r.json()["role"] == "admin"


def test_user_management_admin_only(app_client, admin_headers):
    r = app_client.post("/auth/users", json={"username": "op1", "password": "p", "role": "operator"}, headers=admin_headers)
    assert r.status_code == 201
    op_id = r.json()["id"]

    op_login = app_client.post("/auth/login", json={"username": "op1", "password": "p"}).json()
    op_headers = {"Authorization": f"Bearer {op_login['access_token']}"}

    r = app_client.post("/auth/users", json={"username": "op2", "password": "p"}, headers=op_headers)
    assert r.status_code == 403

    r = app_client.delete(f"/auth/users/{op_id}", headers=admin_headers)
    assert r.status_code == 204


def test_cannot_delete_self(app_client, admin_headers):
    me = app_client.get("/auth/me", headers=admin_headers).json()
    r = app_client.delete(f"/auth/users/{me['id']}", headers=admin_headers)
    assert r.status_code == 400


def test_settings_get_and_patch(app_client, admin_headers):
    r = app_client.get("/settings", headers=admin_headers)
    assert r.status_code == 200
    assert "granule_threshold" in r.json()

    r = app_client.patch("/settings",
                         json={"granule_threshold": 123,
                               "feeding_schedule": [{"start": "08:00", "end": "10:00"}]},
                         headers=admin_headers)
    assert r.status_code == 200
    assert r.json()["granule_threshold"] == 123
    assert r.json()["feeding_schedule"][0]["start"] == "08:00"


def test_events_filter_and_comment(app_client, admin_headers):
    from database import FeedingEvent, SessionLocal
    db = SessionLocal()
    db.add(FeedingEvent(granule_count=10, intensity_per_sec=2.0, intensity_per_min=120.0,
                        threshold_exceeded=True, is_out_of_schedule=False))
    db.add(FeedingEvent(granule_count=2, intensity_per_sec=0.4, intensity_per_min=24.0,
                        threshold_exceeded=False, is_out_of_schedule=True))
    db.commit()
    db.close()

    r = app_client.get("/events", headers=admin_headers)
    assert r.status_code == 200
    assert len(r.json()) == 2

    r = app_client.get("/events?threshold_exceeded=true", headers=admin_headers)
    assert len(r.json()) == 1
    event_id = r.json()[0]["id"]

    r = app_client.patch(f"/events/{event_id}/comment", json={"comment": "тест"}, headers=admin_headers)
    assert r.status_code == 200
    assert r.json()["comment"] == "тест"


def test_stats_with_buckets(app_client, admin_headers):
    from database import FeedingEvent, SessionLocal
    base = datetime(2026, 4, 28, 10, 0, 0)
    db = SessionLocal()
    for i in range(5):
        db.add(FeedingEvent(
            timestamp=base + timedelta(minutes=i * 30),
            granule_count=10 + i,
            intensity_per_sec=1.0 + i * 0.1,
            intensity_per_min=60.0,
            threshold_exceeded=False,
            is_out_of_schedule=False,
        ))
    db.commit()
    db.close()

    date_from = (base - timedelta(minutes=1)).isoformat()
    date_to = (base + timedelta(hours=3)).isoformat()

    r = app_client.get(f"/stats?date_from={date_from}&date_to={date_to}&bucket=hour", headers=admin_headers)
    assert r.status_code == 200
    body = r.json()
    assert body["event_count"] == 5
    assert body["total_granules"] == 60
    assert len(body["intensity_buckets"]) == 4
    bucket_events = sum(b["event_count"] for b in body["intensity_buckets"])
    assert bucket_events == 5

    r = app_client.get(f"/stats?date_from={date_from}&date_to={date_to}&bucket=abc", headers=admin_headers)
    assert r.status_code == 400


def test_stats_invalid_range(app_client, admin_headers):
    r = app_client.get("/stats?date_from=2026-04-28T10:00:00&date_to=2026-04-28T09:00:00", headers=admin_headers)
    assert r.status_code == 400


def test_logs_filter(app_client, admin_headers):
    from database import AppLog, SessionLocal
    db = SessionLocal()
    db.add(AppLog(level="ERROR", message="boom"))
    db.add(AppLog(level="INFO", message="hi"))
    db.commit()
    db.close()

    r = app_client.get("/logs", headers=admin_headers)
    assert r.status_code == 200
    assert len(r.json()) == 2

    r = app_client.get("/logs?level=ERROR", headers=admin_headers)
    assert len(r.json()) == 1

    r = app_client.get("/logs?level=BAD", headers=admin_headers)
    assert r.status_code == 422


def test_stream_upload_rejects_bad_extension(app_client, admin_headers):
    r = app_client.post("/stream/upload",
                        files={"file": ("x.txt", b"data", "text/plain")},
                        headers=admin_headers)
    assert r.status_code == 400


def test_stream_requires_auth(app_client):
    assert app_client.post("/stream/start", json={"source": "0"}).status_code == 401
    assert app_client.get("/stream/status").status_code == 401


def test_stream_409_when_running(app_client, admin_headers):
    import api.stream as stream_module
    stream_module._is_running = True
    try:
        r = app_client.post("/stream/start", json={"source": "0"}, headers=admin_headers)
        assert r.status_code == 409
    finally:
        stream_module._is_running = False


def test_stream_status_reports_running_flag(app_client, admin_headers):
    r = app_client.get("/stream/status", headers=admin_headers)
    assert r.status_code == 200
    assert r.json()["is_running"] is False

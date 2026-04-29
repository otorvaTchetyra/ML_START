import os
import sys

os.environ["DB_URL"] = "sqlite:///./test_dronevision.db"
os.environ["SECRET_KEY"] = "test-secret"

sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

import pytest
from fastapi.testclient import TestClient


@pytest.fixture
def app_client(tmp_path, monkeypatch):
    db_file = tmp_path / "test.db"
    monkeypatch.setenv("DB_URL", f"sqlite:///{db_file}")

    for mod in ("config", "database", "main",
                "api.auth", "api.events", "api.logs",
                "api.settings_api", "api.stats", "api.stream",
                "schemas.schemas"):
        sys.modules.pop(mod, None)

    import database
    import api.stream as stream_module
    from main import app

    stream_module._is_running = False

    with TestClient(app) as client:
        yield client


@pytest.fixture
def admin_headers(app_client):
    r = app_client.post("/auth/login", json={"username": "admin", "password": "admin"})
    assert r.status_code == 200
    return {"Authorization": f"Bearer {r.json()['access_token']}"}

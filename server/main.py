import logging
from contextlib import asynccontextmanager

from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware

from api.auth import hash_password, router as auth_router
from api.events import router as events_router
from api.logs import router as logs_router
from api.settings_api import router as settings_router
from api.stream import router as stream_router
from api.stats import router as stats_router
from database import User, SessionLocal, init_db

logging.basicConfig(level=logging.INFO, format="%(asctime)s %(levelname)s %(name)s: %(message)s")
logger = logging.getLogger(__name__)


def _seed_admin() -> None:
    db = SessionLocal()
    try:
        if not db.query(User).first():
            db.add(User(username="admin", hashed_password=hash_password("admin"), role="admin"))
            db.commit()
            logger.info("Создан администратор по умолчанию: admin / admin")
    finally:
        db.close()


@asynccontextmanager
async def lifespan(app: FastAPI):
    init_db()
    _seed_admin()
    yield


app = FastAPI(
    title="DroneVision — Система контроля кормления рыб",
    version="1.0.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(auth_router)
app.include_router(stream_router)
app.include_router(events_router)
app.include_router(stats_router)
app.include_router(settings_router)
app.include_router(logs_router)


@app.get("/health")
def health():
    return {"status": "ok"}

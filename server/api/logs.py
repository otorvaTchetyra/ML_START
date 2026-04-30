from datetime import datetime
from fastapi import APIRouter, Depends, Query
from sqlalchemy.orm import Session

from api.auth import get_current_user
from database import AppLog, get_db
from schemas.schemas import AppLogResponse

router = APIRouter(prefix="/logs", tags=["logs"])


@router.get("", response_model=list[AppLogResponse])
def list_logs(
    level: str | None = Query(None, pattern="^(INFO|WARNING|ERROR)$"),
    date_from: datetime | None = Query(None),
    date_to: datetime | None = Query(None),
    limit: int = Query(200, le=1000),
    db: Session = Depends(get_db),
    _ = Depends(get_current_user),
):
    q = db.query(AppLog)
    if level:
        q = q.filter(AppLog.level == level)
    if date_from:
        q = q.filter(AppLog.timestamp >= date_from)
    if date_to:
        q = q.filter(AppLog.timestamp <= date_to)
    return q.order_by(AppLog.timestamp.desc()).limit(limit).all()

from datetime import datetime
from fastapi import APIRouter, Depends, Query
from sqlalchemy import func
from sqlalchemy.orm import Session

from api.auth import get_current_user
from database import FeedingEvent, get_db
from schemas.schemas import StatsResponse

router = APIRouter(prefix="/stats", tags=["stats"])


@router.get("", response_model=StatsResponse)
def get_stats(
    date_from: datetime = Query(...),
    date_to: datetime = Query(...),
    db: Session = Depends(get_db),
    _ = Depends(get_current_user),
):
    events = (
        db.query(FeedingEvent)
        .filter(FeedingEvent.timestamp >= date_from, FeedingEvent.timestamp <= date_to)
        .order_by(FeedingEvent.timestamp)
        .all()
    )

    if not events:
        return StatsResponse(
            period_start=date_from,
            period_end=date_to,
            total_granules=0,
            average_intensity_per_sec=0.0,
            average_intensity_per_min=0.0,
            event_count=0,
            intensity_timeline=[],
        )

    total_granules = sum(e.granule_count for e in events)
    avg_per_sec = sum(e.intensity_per_sec for e in events) / len(events)

    timeline = [
        {"timestamp": e.timestamp.isoformat(), "intensity_per_sec": e.intensity_per_sec}
        for e in events
    ]

    return StatsResponse(
        period_start=date_from,
        period_end=date_to,
        total_granules=total_granules,
        average_intensity_per_sec=round(avg_per_sec, 2),
        average_intensity_per_min=round(avg_per_sec * 60, 2),
        event_count=len(events),
        intensity_timeline=timeline,
    )

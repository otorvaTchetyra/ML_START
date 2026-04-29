from datetime import datetime, timedelta
from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from api.auth import get_current_user
from database import FeedingEvent, get_db
from schemas.schemas import IntensityBucket, StatsResponse

router = APIRouter(prefix="/stats", tags=["stats"])

_BUCKET_PRESETS = {
    "minute": 60,
    "hour": 3600,
    "day": 86400,
}


def _parse_bucket(bucket: str | None) -> int | None:
    if bucket is None:
        return None
    if bucket in _BUCKET_PRESETS:
        return _BUCKET_PRESETS[bucket]
    try:
        seconds = int(bucket)
    except ValueError:
        raise HTTPException(status_code=400, detail="bucket: hour|day|minute или число секунд")
    if seconds <= 0:
        raise HTTPException(status_code=400, detail="bucket должен быть положительным")
    return seconds


def _build_buckets(events: list[FeedingEvent], start: datetime, end: datetime, size_sec: int) -> list[IntensityBucket]:
    delta = timedelta(seconds=size_sec)
    buckets: list[IntensityBucket] = []
    cursor = start
    idx = 0
    while cursor < end:
        bucket_end = min(cursor + delta, end)
        granules = 0
        intensities: list[float] = []
        while idx < len(events) and events[idx].timestamp < bucket_end:
            granules += events[idx].granule_count
            intensities.append(events[idx].intensity_per_sec)
            idx += 1
        avg = round(sum(intensities) / len(intensities), 2) if intensities else 0.0
        buckets.append(IntensityBucket(
            start=cursor,
            end=bucket_end,
            total_granules=granules,
            average_intensity_per_sec=avg,
            event_count=len(intensities),
        ))
        cursor = bucket_end
    return buckets


@router.get("", response_model=StatsResponse)
def get_stats(
    date_from: datetime = Query(...),
    date_to: datetime = Query(...),
    bucket: str | None = Query(None, description="hour|day|minute или количество секунд"),
    db: Session = Depends(get_db),
    _ = Depends(get_current_user),
):
    if date_to <= date_from:
        raise HTTPException(status_code=400, detail="date_to должен быть позже date_from")

    events = (
        db.query(FeedingEvent)
        .filter(FeedingEvent.timestamp >= date_from, FeedingEvent.timestamp <= date_to)
        .order_by(FeedingEvent.timestamp)
        .all()
    )

    bucket_size = _parse_bucket(bucket)
    buckets = _build_buckets(events, date_from, date_to, bucket_size) if bucket_size else []

    if not events:
        return StatsResponse(
            period_start=date_from,
            period_end=date_to,
            total_granules=0,
            average_intensity_per_sec=0.0,
            average_intensity_per_min=0.0,
            event_count=0,
            intensity_timeline=[],
            intensity_buckets=buckets,
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
        intensity_buckets=buckets,
    )

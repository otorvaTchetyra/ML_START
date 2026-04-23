from datetime import datetime
from fastapi import APIRouter, Depends, HTTPException, Query
from sqlalchemy.orm import Session

from api.auth import get_current_user
from database import FeedingEvent, get_db
from schemas.schemas import EventCommentUpdate, FeedingEventResponse

router = APIRouter(prefix="/events", tags=["events"])


@router.get("", response_model=list[FeedingEventResponse])
def list_events(
    date_from: datetime | None = Query(None),
    date_to: datetime | None = Query(None),
    threshold_exceeded: bool | None = Query(None),
    out_of_schedule: bool | None = Query(None),
    limit: int = Query(200, le=1000),
    db: Session = Depends(get_db),
    _ = Depends(get_current_user),
):
    q = db.query(FeedingEvent)
    if date_from:
        q = q.filter(FeedingEvent.timestamp >= date_from)
    if date_to:
        q = q.filter(FeedingEvent.timestamp <= date_to)
    if threshold_exceeded is not None:
        q = q.filter(FeedingEvent.threshold_exceeded == threshold_exceeded)
    if out_of_schedule is not None:
        q = q.filter(FeedingEvent.is_out_of_schedule == out_of_schedule)
    return q.order_by(FeedingEvent.timestamp.desc()).limit(limit).all()


@router.patch("/{event_id}/comment", response_model=FeedingEventResponse)
def update_comment(
    event_id: int,
    data: EventCommentUpdate,
    db: Session = Depends(get_db),
    _ = Depends(get_current_user),
):
    event = db.query(FeedingEvent).filter(FeedingEvent.id == event_id).first()
    if not event:
        raise HTTPException(status_code=404, detail="Событие не найдено")
    event.comment = data.comment
    db.commit()
    db.refresh(event)
    return event

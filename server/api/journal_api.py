from datetime import datetime
from typing import Optional
from fastapi import APIRouter, Depends
from pydantic import BaseModel
from sqlalchemy.orm import Session

from api.auth import get_current_user
from database import JournalEntry, JournalEventType, get_db

router = APIRouter(prefix="/journal", tags=["journal"])


class JournalEntryCreate(BaseModel):
    timestamp: Optional[datetime] = None
    level: str = "info"
    event_code: Optional[str] = None
    source: str = "client"
    action: str = "event"
    message: str
    details_json: Optional[str] = None
    user_id: Optional[int] = None
    username_snapshot: Optional[str] = None
    screen: Optional[str] = None
    entity_type: Optional[str] = None
    entity_id: Optional[int] = None
    is_resolved: bool = False
    comment: Optional[str] = None


class JournalEntryUpdate(BaseModel):
    comment: Optional[str] = None
    is_resolved: Optional[bool] = None


class JournalEntryResponse(BaseModel):
    id: int
    timestamp: datetime
    level: str
    source: str
    action: str
    message: str
    details_json: Optional[str] = None
    user_id: Optional[int] = None
    username_snapshot: Optional[str] = None
    screen: Optional[str] = None
    entity_type: Optional[str] = None
    entity_id: Optional[int] = None
    is_resolved: bool
    resolved_at: Optional[datetime] = None
    comment: Optional[str] = None
    event_code: Optional[str] = None

    class Config:
        from_attributes = True


@router.post("/entries", response_model=JournalEntryResponse)
def create_entry(data: JournalEntryCreate, db: Session = Depends(get_db), _ = Depends(get_current_user)):
    event_type = None
    if data.event_code:
        event_type = db.query(JournalEventType).filter(JournalEventType.code == data.event_code).first()
        if not event_type:
            event_type = db.query(JournalEventType).filter(JournalEventType.code == "custom").first()

    entry = JournalEntry(
        timestamp=data.timestamp or datetime.utcnow(),
        level=data.level,
        event_type_id=event_type.id if event_type else None,
        source=data.source,
        action=data.action,
        message=data.message,
        details_json=data.details_json,
        user_id=data.user_id,
        username_snapshot=data.username_snapshot,
        screen=data.screen,
        entity_type=data.entity_type,
        entity_id=data.entity_id,
        is_resolved=data.is_resolved,
        comment=data.comment,
    )
    db.add(entry)
    db.commit()
    db.refresh(entry)

    result = JournalEntryResponse(
        id=entry.id,
        timestamp=entry.timestamp,
        level=entry.level,
        source=entry.source,
        action=entry.action,
        message=entry.message,
        details_json=entry.details_json,
        user_id=entry.user_id,
        username_snapshot=entry.username_snapshot,
        screen=entry.screen,
        entity_type=entry.entity_type,
        entity_id=entry.entity_id,
        is_resolved=entry.is_resolved,
        resolved_at=entry.resolved_at,
        comment=entry.comment,
        event_code=event_type.code if event_type else None,
    )
    return result


@router.get("/entries", response_model=list[JournalEntryResponse])
def get_entries(
    limit: int = 200,
    level: Optional[str] = None,
    source: Optional[str] = None,
    db: Session = Depends(get_db),
    _ = Depends(get_current_user),
):
    query = db.query(JournalEntry)
    if level:
        query = query.filter(JournalEntry.level == level)
    if source:
        query = query.filter(JournalEntry.source == source)
    entries = query.order_by(JournalEntry.timestamp.desc()).limit(limit).all()

    return [
        JournalEntryResponse(
            id=e.id,
            timestamp=e.timestamp,
            level=e.level,
            source=e.source,
            action=e.action,
            message=e.message,
            details_json=e.details_json,
            user_id=e.user_id,
            username_snapshot=e.username_snapshot,
            screen=e.screen,
            entity_type=e.entity_type,
            entity_id=e.entity_id,
            is_resolved=e.is_resolved,
            resolved_at=e.resolved_at,
            comment=e.comment,
            event_code=e.event_type.code if e.event_type else None,
        )
        for e in entries
    ]


@router.patch("/entries/{entry_id}", response_model=JournalEntryResponse)
def update_entry(entry_id: int, data: JournalEntryUpdate, db: Session = Depends(get_db), _ = Depends(get_current_user)):
    entry = db.query(JournalEntry).filter(JournalEntry.id == entry_id).first()
    if not entry:
        from fastapi import HTTPException
        raise HTTPException(status_code=404, detail="Запись не найдена")

    if data.comment is not None:
        entry.comment = data.comment
    if data.is_resolved is not None:
        entry.is_resolved = data.is_resolved
        entry.resolved_at = datetime.utcnow() if data.is_resolved else None

    db.commit()
    db.refresh(entry)

    return JournalEntryResponse(
        id=entry.id,
        timestamp=entry.timestamp,
        level=entry.level,
        source=entry.source,
        action=entry.action,
        message=entry.message,
        details_json=entry.details_json,
        user_id=entry.user_id,
        username_snapshot=entry.username_snapshot,
        screen=entry.screen,
        entity_type=entry.entity_type,
        entity_id=entry.entity_id,
        is_resolved=entry.is_resolved,
        resolved_at=entry.resolved_at,
        comment=entry.comment,
        event_code=entry.event_type.code if entry.event_type else None,
    )

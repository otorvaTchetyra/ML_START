import json
from fastapi import APIRouter, Depends
from sqlalchemy.orm import Session

from api.auth import get_current_user
from config import settings as cfg
from database import AppSettings, get_db
from schemas.schemas import SettingsResponse, SettingsUpdate

router = APIRouter(prefix="/settings", tags=["settings"])

_DEFAULTS = {
    "granule_threshold": cfg.default_granule_threshold,
    "intensity_window_sec": cfg.intensity_window_sec,
    "model_confidence": cfg.model_confidence,
    "model_iou": cfg.model_iou,
    "feeding_schedule": [],
    "frame_skip": cfg.frame_skip,
    "tracker_iou_threshold": cfg.tracker_iou_threshold,
}


def _get(db: Session, key: str):
    row = db.query(AppSettings).filter(AppSettings.key == key).first()
    return json.loads(row.value) if row else _DEFAULTS.get(key)


def _set(db: Session, key: str, value) -> None:
    row = db.query(AppSettings).filter(AppSettings.key == key).first()
    if row:
        row.value = json.dumps(value)
    else:
        db.add(AppSettings(key=key, value=json.dumps(value)))
    db.commit()


def get_current_settings(db: Session) -> dict:
    return {key: _get(db, key) for key in _DEFAULTS}


@router.get("", response_model=SettingsResponse)
def read_settings(db: Session = Depends(get_db), _ = Depends(get_current_user)):
    return get_current_settings(db)


@router.patch("", response_model=SettingsResponse)
def update_settings(data: SettingsUpdate, db: Session = Depends(get_db), _ = Depends(get_current_user)):
    from api.stream import get_detector, get_counter

    updates = data.model_dump(exclude_none=True)
    for key, value in updates.items():
        _set(db, key, value)

    if "model_confidence" in updates or "model_iou" in updates:
        get_detector().update_params(
            confidence=updates.get("model_confidence"),
            iou=updates.get("model_iou"),
        )
    if "intensity_window_sec" in updates:
        get_counter().window_sec = updates["intensity_window_sec"]
    if "tracker_iou_threshold" in updates:
        from api.stream import get_tracker
        get_tracker().iou_threshold = updates["tracker_iou_threshold"]

    import api.stream as _stream
    if "granule_threshold" in updates:
        _stream._granule_threshold = updates["granule_threshold"]
    if "feeding_schedule" in updates:
        _stream._feeding_schedule = updates["feeding_schedule"]
    if "frame_skip" in updates:
        _stream._frame_skip = max(1, updates["frame_skip"])

    return get_current_settings(db)

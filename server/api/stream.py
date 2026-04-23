import asyncio
import logging
import tempfile
from datetime import datetime, time as dtime
from pathlib import Path
from time import monotonic

import cv2
from fastapi import APIRouter, Depends, File, HTTPException, UploadFile
from fastapi.responses import StreamingResponse
from sqlalchemy.orm import Session

from api.auth import get_current_user
from api.settings_api import get_current_settings
from database import AppLog, FeedingEvent, User, get_db
from models.counter import GranuleCounter
from models.detector import GranuleDetector
from schemas.schemas import BBox, FrameAnalysisResponse

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/stream", tags=["stream"])

_detector: GranuleDetector | None = None
_counter: GranuleCounter | None = None
_is_running: bool = False


def get_detector() -> GranuleDetector:
    global _detector
    if _detector is None:
        _detector = GranuleDetector()
    return _detector


def get_counter() -> GranuleCounter:
    global _counter
    if _counter is None:
        from config import settings
        _counter = GranuleCounter(window_sec=settings.intensity_window_sec)
    return _counter


def _is_in_schedule(schedule: list[dict]) -> bool:
    if not schedule:
        return True
    now = datetime.now().time()
    for period in schedule:
        try:
            start = dtime.fromisoformat(period["start"])
            end = dtime.fromisoformat(period["end"])
            if start <= now <= end:
                return True
        except (KeyError, ValueError):
            continue
    return False


def _log_error(db: Session, message: str) -> None:
    db.add(AppLog(level="ERROR", message=message))
    db.commit()


async def _process_video_stream(video_path: str, db: Session, settings: dict):
    global _is_running

    detector = get_detector()
    counter = get_counter()
    counter.reset()

    threshold: int = settings.get("granule_threshold", 50)
    schedule: list[dict] = settings.get("feeding_schedule", [])

    cap = cv2.VideoCapture(video_path)
    if not cap.isOpened():
        _log_error(db, f"Не удалось открыть видео: {video_path}")
        raise HTTPException(status_code=400, detail="Не удалось открыть видеофайл")

    frame_index = 0
    _is_running = True

    try:
        while _is_running and cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            ts = monotonic()
            try:
                detections = detector.detect(frame)
            except Exception as exc:
                _log_error(db, f"Ошибка детекции на кадре {frame_index}: {exc}")
                frame_index += 1
                continue

            result = counter.process_frame(detections, timestamp=ts)

            threshold_exceeded = result.granule_count > threshold
            out_of_schedule = not _is_in_schedule(schedule) and result.granule_count > 0

            if threshold_exceeded or out_of_schedule:
                event = FeedingEvent(
                    granule_count=result.granule_count,
                    intensity_per_sec=result.intensity_per_sec,
                    intensity_per_min=result.intensity_per_min,
                    threshold_exceeded=threshold_exceeded,
                    is_out_of_schedule=out_of_schedule,
                )
                db.add(event)
                try:
                    db.commit()
                except Exception as exc:
                    db.rollback()
                    _log_error(db, f"Ошибка записи события: {exc}")

            payload = FrameAnalysisResponse(
                frame_index=frame_index,
                timestamp=ts,
                granule_count=result.granule_count,
                intensity_per_sec=result.intensity_per_sec,
                intensity_per_min=result.intensity_per_min,
                threshold_exceeded=threshold_exceeded,
                out_of_schedule=out_of_schedule,
                bboxes=[
                    BBox(x1=d.x1, y1=d.y1, x2=d.x2, y2=d.y2, confidence=d.confidence)
                    for d in result.detections
                ],
            )

            yield payload.model_dump_json() + "\n"
            frame_index += 1

            await asyncio.sleep(0)

    finally:
        cap.release()
        _is_running = False
        logger.info("Обработка видео завершена. Кадров: %d, гранул: %d", frame_index, counter.total_granules)


@router.post("/upload")
async def upload_and_analyse(
    file: UploadFile = File(...),
    current_user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    suffix = Path(file.filename).suffix.lower()
    if suffix not in (".mp4", ".avi"):
        raise HTTPException(status_code=400, detail="Поддерживаются только форматы MP4 и AVI")

    with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as tmp:
        tmp.write(await file.read())
        tmp_path = tmp.name

    app_settings = get_current_settings(db)

    return StreamingResponse(
        _process_video_stream(tmp_path, db, app_settings),
        media_type="application/x-ndjson",
    )


@router.post("/stop")
def stop_stream(current_user: User = Depends(get_current_user)):
    global _is_running
    _is_running = False
    return {"status": "stopped"}


@router.get("/status")
def stream_status(current_user: User = Depends(get_current_user)):
    counter = get_counter()
    return {
        "is_running": _is_running,
        "total_granules": counter.total_granules,
        "frames_processed": counter.frame_count,
    }

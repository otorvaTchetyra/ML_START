import asyncio
import logging
import shutil
import tempfile
from datetime import datetime, time as dtime
from pathlib import Path
from time import monotonic

import cv2
from fastapi import APIRouter, Depends, File, HTTPException, UploadFile, status
from fastapi.responses import StreamingResponse
from sqlalchemy.orm import Session

from api.auth import get_current_user
from api.settings_api import get_current_settings
from database import AppLog, FeedingEvent, SessionLocal, User, get_db
from models.counter import GranuleCounter
from models.detector import GranuleDetector
from models.tracker import GranuleTracker
from schemas.schemas import BBox, FrameAnalysisResponse, StreamStartRequest

logger = logging.getLogger(__name__)
router = APIRouter(prefix="/stream", tags=["stream"])

_detector: GranuleDetector | None = None
_counter: GranuleCounter | None = None
_tracker: GranuleTracker | None = None
_is_running: bool = False

_latest_jpeg: bytes | None = None
_frame_version: int = 0
_frame_cond: asyncio.Condition | None = None


def _get_frame_cond() -> asyncio.Condition:
    global _frame_cond
    if _frame_cond is None:
        _frame_cond = asyncio.Condition()
    return _frame_cond


def _render_overlay(frame, detections, granule_count: int, intensity_per_sec: float,
                    threshold_exceeded: bool) -> bytes | None:
    annotated = frame.copy()
    color = (0, 0, 255) if threshold_exceeded else (0, 255, 0)
    for d in detections:
        cv2.rectangle(annotated, (int(d.x1), int(d.y1)), (int(d.x2), int(d.y2)), color, 2)
    label = f"granules: {granule_count}  intensity: {intensity_per_sec:.1f}/s"
    cv2.putText(annotated, label, (10, 25), cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255, 255, 255), 2)
    ok, buf = cv2.imencode(".jpg", annotated, [cv2.IMWRITE_JPEG_QUALITY, 80])
    return buf.tobytes() if ok else None


async def _publish_frame(jpeg: bytes) -> None:
    global _latest_jpeg, _frame_version
    cond = _get_frame_cond()
    async with cond:
        _latest_jpeg = jpeg
        _frame_version += 1
        cond.notify_all()


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


def get_tracker() -> GranuleTracker:
    global _tracker
    if _tracker is None:
        from config import settings
        _tracker = GranuleTracker(iou_threshold=settings.tracker_iou_threshold)
    return _tracker


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


def _log_error(message: str) -> None:
    db = SessionLocal()
    try:
        db.add(AppLog(level="ERROR", message=message))
        db.commit()
    except Exception:
        db.rollback()
    finally:
        db.close()


def _save_event(granule_count: int, intensity_per_sec: float, intensity_per_min: float,
                threshold_exceeded: bool, out_of_schedule: bool) -> None:
    db = SessionLocal()
    try:
        db.add(FeedingEvent(
            granule_count=granule_count,
            intensity_per_sec=intensity_per_sec,
            intensity_per_min=intensity_per_min,
            threshold_exceeded=threshold_exceeded,
            is_out_of_schedule=out_of_schedule,
        ))
        db.commit()
    except Exception as exc:
        db.rollback()
        _log_error(f"Ошибка записи события: {exc}")
    finally:
        db.close()


def _open_capture(source: str) -> cv2.VideoCapture:
    if source.isdigit():
        return cv2.VideoCapture(int(source))
    return cv2.VideoCapture(source)


async def _process_video_stream(source: str, app_settings: dict, cleanup_path: str | None = None):
    global _is_running, _latest_jpeg

    _latest_jpeg = None
    detector = get_detector()
    counter = get_counter()
    tracker = get_tracker()
    counter.reset()
    tracker.reset()

    threshold: int = app_settings.get("granule_threshold", 50)
    schedule: list[dict] = app_settings.get("feeding_schedule", [])
    frame_skip: int = max(1, app_settings.get("frame_skip", 1))
    is_camera = source.isdigit()

    cap = _open_capture(source)
    if not cap.isOpened():
        _is_running = False
        if cleanup_path:
            Path(cleanup_path).unlink(missing_ok=True)
        _log_error(f"Не удалось открыть источник: {source}")
        raise HTTPException(status_code=400, detail="Не удалось открыть источник видео")

    fps = cap.get(cv2.CAP_PROP_FPS) or 25.0
    src_width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
    src_height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))
    frame_index = 0

    try:
        while _is_running:
            try:
                ret, frame = cap.read()
            except Exception as exc:
                _log_error(f"Ошибка чтения кадра {frame_index}: {exc}")
                await asyncio.sleep(0.05)
                continue
            if not ret:
                break

            if frame_index % frame_skip != 0:
                frame_index += 1
                continue

            ts = monotonic() if is_camera else frame_index / fps

            try:
                detections = detector.detect(frame)
                new_detections = tracker.update(detections)
                result = counter.process_frame(new_detections, timestamp=ts)
            except Exception as exc:
                _log_error(f"Ошибка обработки кадра {frame_index}: {exc}")
                frame_index += 1
                continue

            threshold_exceeded = result.granule_count > threshold
            out_of_schedule = not _is_in_schedule(schedule) and result.granule_count > 0

            if threshold_exceeded or out_of_schedule:
                _save_event(
                    result.granule_count,
                    result.intensity_per_sec,
                    result.intensity_per_min,
                    threshold_exceeded,
                    out_of_schedule,
                )

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
                    for d in detections
                ],
                source_width=src_width,
                source_height=src_height,
            )

            jpeg = _render_overlay(frame, detections, result.granule_count,
                                   result.intensity_per_sec, threshold_exceeded)
            if jpeg is not None:
                await _publish_frame(jpeg)

            yield payload.model_dump_json() + "\n"
            frame_index += 1
            await asyncio.sleep(0)

    finally:
        cap.release()
        if cleanup_path:
            Path(cleanup_path).unlink(missing_ok=True)
        _is_running = False
        cond = _get_frame_cond()
        async with cond:
            cond.notify_all()
        logger.info("Обработка завершена. Кадров: %d, гранул: %d", frame_index, counter.total_granules)


@router.post("/upload")
async def upload_and_analyse(
    file: UploadFile = File(...),
    current_user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    global _is_running
    suffix = Path(file.filename or "").suffix.lower()
    if suffix not in (".mp4", ".avi"):
        raise HTTPException(status_code=400, detail="Поддерживаются только форматы MP4 и AVI")

    if _is_running:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Анализ уже выполняется")

    with tempfile.NamedTemporaryFile(suffix=suffix, delete=False) as tmp:
        shutil.copyfileobj(file.file, tmp)
        tmp_path = tmp.name

    app_settings = get_current_settings(db)
    _is_running = True

    return StreamingResponse(
        _process_video_stream(tmp_path, app_settings, cleanup_path=tmp_path),
        media_type="application/x-ndjson",
    )


@router.post("/start")
async def start_from_source(
    data: StreamStartRequest,
    current_user: User = Depends(get_current_user),
    db: Session = Depends(get_db),
):
    global _is_running
    if _is_running:
        raise HTTPException(status_code=status.HTTP_409_CONFLICT, detail="Анализ уже выполняется")

    app_settings = get_current_settings(db)
    _is_running = True

    return StreamingResponse(
        _process_video_stream(data.source, app_settings),
        media_type="application/x-ndjson",
    )


@router.post("/stop")
async def stop_stream(current_user: User = Depends(get_current_user)):
    global _is_running
    _is_running = False
    cond = _get_frame_cond()
    async with cond:
        cond.notify_all()
    return {"status": "stopped"}


async def _mjpeg_generator():
    boundary = b"--frame"
    cond = _get_frame_cond()
    last_version = 0
    while _is_running or _latest_jpeg is not None:
        async with cond:
            while _is_running and last_version == _frame_version:
                await cond.wait()
            jpeg = _latest_jpeg
            last_version = _frame_version
        if not _is_running and jpeg is None:
            break
        if jpeg is None:
            continue
        yield boundary + b"\r\nContent-Type: image/jpeg\r\nContent-Length: " + \
              str(len(jpeg)).encode() + b"\r\n\r\n" + jpeg + b"\r\n"
        if not _is_running:
            break


@router.get("/mjpeg")
async def mjpeg_stream(current_user: User = Depends(get_current_user)):
    if not _is_running:
        raise HTTPException(status_code=409, detail="Анализ не запущен")
    return StreamingResponse(
        _mjpeg_generator(),
        media_type="multipart/x-mixed-replace; boundary=frame",
    )


@router.get("/status")
def stream_status(current_user: User = Depends(get_current_user)):
    counter = get_counter()
    return {
        "is_running": _is_running,
        "total_granules": counter.total_granules,
        "frames_processed": counter.frame_count,
    }

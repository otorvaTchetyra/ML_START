from pydantic import BaseModel
from datetime import datetime


# ── Auth ──────────────────────────────────────────────────────────────────────

class LoginRequest(BaseModel):
    username: str
    password: str

class TokenResponse(BaseModel):
    access_token: str
    token_type: str = "bearer"

class UserCreate(BaseModel):
    username: str
    password: str
    role: str = "operator"

class UserResponse(BaseModel):
    id: int
    username: str
    role: str
    is_active: bool

    class Config:
        from_attributes = True


# ── Detection / Frame ─────────────────────────────────────────────────────────

class BBox(BaseModel):
    x1: float
    y1: float
    x2: float
    y2: float
    confidence: float

class FrameAnalysisResponse(BaseModel):
    frame_index: int
    timestamp: float
    granule_count: int
    intensity_per_sec: float
    intensity_per_min: float
    threshold_exceeded: bool
    out_of_schedule: bool
    bboxes: list[BBox]


# ── Feeding events ────────────────────────────────────────────────────────────

class FeedingEventResponse(BaseModel):
    id: int
    timestamp: datetime
    granule_count: int
    intensity_per_sec: float
    intensity_per_min: float
    is_out_of_schedule: bool
    threshold_exceeded: bool
    comment: str | None

    class Config:
        from_attributes = True

class EventCommentUpdate(BaseModel):
    comment: str


# ── Statistics ────────────────────────────────────────────────────────────────

class StatsResponse(BaseModel):
    period_start: datetime
    period_end: datetime
    total_granules: int
    average_intensity_per_sec: float
    average_intensity_per_min: float
    event_count: int
    intensity_timeline: list[dict]  # [{timestamp, intensity_per_sec}]


# ── Settings ──────────────────────────────────────────────────────────────────

class SettingsUpdate(BaseModel):
    granule_threshold: int | None = None
    intensity_window_sec: float | None = None
    model_confidence: float | None = None
    model_iou: float | None = None
    feeding_schedule: list[dict] | None = None  # [{"start": "08:00", "end": "09:00"}]

class SettingsResponse(BaseModel):
    granule_threshold: int
    intensity_window_sec: float
    model_confidence: float
    model_iou: float
    feeding_schedule: list[dict]


# ── Logs ──────────────────────────────────────────────────────────────────────

class AppLogResponse(BaseModel):
    id: int
    timestamp: datetime
    level: str
    message: str

    class Config:
        from_attributes = True

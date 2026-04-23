from collections import deque
from dataclasses import dataclass, field
from time import monotonic

from models.types import Detection


@dataclass
class FrameResult:
    timestamp: float
    granule_count: int
    intensity_per_sec: float
    intensity_per_min: float
    detections: list[Detection] = field(default_factory=list)


class GranuleCounter:

    def __init__(self, window_sec: float = 5.0) -> None:
        self._window_sec = window_sec
        self._window: deque[tuple[float, int]] = deque()
        self._total_granules: int = 0
        self._frame_count: int = 0

    def process_frame(self, detections: list[Detection], timestamp: float | None = None) -> FrameResult:
        ts = timestamp if timestamp is not None else monotonic()
        count = len(detections)

        self._window.append((ts, count))
        self._total_granules += count
        self._frame_count += 1

        self._evict_expired(ts)

        per_sec = self._compute_intensity_per_sec(ts)
        per_min = per_sec * 60.0

        return FrameResult(
            timestamp=ts,
            granule_count=count,
            intensity_per_sec=round(per_sec, 2),
            intensity_per_min=round(per_min, 2),
            detections=detections,
        )

    def reset(self) -> None:
        self._window.clear()
        self._total_granules = 0
        self._frame_count = 0

    @property
    def total_granules(self) -> int:
        return self._total_granules

    @property
    def frame_count(self) -> int:
        return self._frame_count

    @property
    def window_sec(self) -> float:
        return self._window_sec

    @window_sec.setter
    def window_sec(self, value: float) -> None:
        if value <= 0:
            raise ValueError("window_sec must be positive")
        self._window_sec = value

    def _evict_expired(self, now: float) -> None:
        cutoff = now - self._window_sec
        while self._window and self._window[0][0] < cutoff:
            self._window.popleft()

    def _compute_intensity_per_sec(self, now: float) -> float:
        if not self._window:
            return 0.0

        window_granules = sum(c for _, c in self._window)
        oldest_ts = self._window[0][0]
        effective_duration = min(now - oldest_ts, self._window_sec)

        if effective_duration <= 0:
            return float(window_granules)

        return window_granules / effective_duration

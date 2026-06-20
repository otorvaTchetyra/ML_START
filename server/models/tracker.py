from dataclasses import dataclass

from models.types import Detection


@dataclass
class _Track:
    det: Detection
    missed: int = 0


class GranuleTracker:

    def __init__(
        self,
        iou_threshold: float = 0.3,
        max_dist: float = 40.0,
        max_missed: int = 5,
    ) -> None:
        self._iou_threshold = iou_threshold
        self._max_dist = max_dist
        self._max_missed = max_missed
        self._tracks: list[_Track] = []

    def update(self, detections: list[Detection]) -> list[Detection]:
        new_dets: list[Detection] = []
        matched: set[int] = set()

        for det in detections:
            idx = self._find_best(det, matched)
            if idx is not None:
                self._tracks[idx].det = det
                self._tracks[idx].missed = 0
                matched.add(idx)
            else:
                new_dets.append(det)

        for i, track in enumerate(self._tracks):
            if i not in matched:
                track.missed += 1

        self._tracks = [t for t in self._tracks if t.missed <= self._max_missed]
        for det in new_dets:
            self._tracks.append(_Track(det=det))

        return new_dets

    def reset(self) -> None:
        self._tracks.clear()

    @property
    def tracked_detections(self) -> list[Detection]:
        return [t.det for t in self._tracks if t.missed == 0]

    @property
    def iou_threshold(self) -> float:
        return self._iou_threshold

    @iou_threshold.setter
    def iou_threshold(self, value: float) -> None:
        if not 0.0 < value <= 1.0:
            raise ValueError("iou_threshold must be in (0, 1]")
        self._iou_threshold = value

    def _find_best(self, det: Detection, excluded: set[int]) -> int | None:
        best_idx: int | None = None
        best_dist = float("inf")
        for i, track in enumerate(self._tracks):
            if i in excluded:
                continue
            dist = _center_dist(det, track.det)
            iou = _iou(det, track.det)
            if (dist <= self._max_dist or iou >= self._iou_threshold) and dist < best_dist:
                best_dist = dist
                best_idx = i
        return best_idx


def _center_dist(a: Detection, b: Detection) -> float:
    cx_a = (a.x1 + a.x2) / 2
    cy_a = (a.y1 + a.y2) / 2
    cx_b = (b.x1 + b.x2) / 2
    cy_b = (b.y1 + b.y2) / 2
    return ((cx_a - cx_b) ** 2 + (cy_a - cy_b) ** 2) ** 0.5


def _iou(a: Detection, b: Detection) -> float:
    ix1 = max(a.x1, b.x1)
    iy1 = max(a.y1, b.y1)
    ix2 = min(a.x2, b.x2)
    iy2 = min(a.y2, b.y2)
    inter = max(0.0, ix2 - ix1) * max(0.0, iy2 - iy1)
    if inter == 0.0:
        return 0.0
    area_a = (a.x2 - a.x1) * (a.y2 - a.y1)
    area_b = (b.x2 - b.x1) * (b.y2 - b.y1)
    return inter / (area_a + area_b - inter)

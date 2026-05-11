from models.types import Detection


class GranuleTracker:

    def __init__(self, iou_threshold: float = 0.3):
        self._iou_threshold = iou_threshold
        self._prev: list[Detection] = []

    def update(self, detections: list[Detection]) -> list[Detection]:
        new = [d for d in detections if not self._is_matched(d)]
        self._prev = detections
        return new

    def reset(self) -> None:
        self._prev = []

    @property
    def iou_threshold(self) -> float:
        return self._iou_threshold

    @iou_threshold.setter
    def iou_threshold(self, value: float) -> None:
        if not 0.0 < value <= 1.0:
            raise ValueError("iou_threshold must be in (0, 1]")
        self._iou_threshold = value

    def _is_matched(self, det: Detection) -> bool:
        return any(self._iou(det, p) >= self._iou_threshold for p in self._prev)

    @staticmethod
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

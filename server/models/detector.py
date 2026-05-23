import threading
import numpy as np
from config import settings
from models.types import Detection

_FISH_CLASSES = {"fish", "eel", "shark", "ray", "jellyfish", "starfish", "crab"}


class GranuleDetector:

    def __init__(self, model_path: str | None = None, confidence: float | None = None, iou: float | None = None):
        from ultralytics import YOLO
        path = model_path or settings.model_path
        self.confidence = confidence if confidence is not None else settings.model_confidence
        self.iou = iou if iou is not None else settings.model_iou
        self._model = YOLO(path)
        self._mode = "granule"
        self._lock = threading.Lock()

    def load_weights(self, path: str, mode: str) -> None:
        from ultralytics import YOLO
        new_model = YOLO(path)
        with self._lock:
            self._model = new_model
            self._mode = mode

    def set_mode(self, mode: str) -> None:
        with self._lock:
            self._mode = mode

    def detect(self, frame: np.ndarray) -> list[Detection]:
        with self._lock:
            if self._mode == "off":
                return []
            model = self._model
            mode = self._mode
        results = model(frame, conf=self.confidence, iou=self.iou, verbose=False)
        h, w = frame.shape[:2]
        detections: list[Detection] = []
        for result in results:
            if result.boxes is None:
                continue
            for box in result.boxes:
                x1, y1, x2, y2 = box.xyxy[0].tolist()
                conf = float(box.conf[0])
                label = ""
                if mode == "granule":
                    if (x2 - x1) / w > 0.12 or (y2 - y1) / h > 0.12:
                        continue
                else:
                    cls_idx = int(box.cls[0]) if box.cls is not None else -1
                    cls_name = (result.names or {}).get(cls_idx, "")
                    if cls_name not in _FISH_CLASSES:
                        continue
                    label = cls_name
                detections.append(Detection(x1=x1, y1=y1, x2=x2, y2=y2, confidence=conf, label=label))
        return detections

    def update_params(self, confidence: float | None = None, iou: float | None = None) -> None:
        if confidence is not None:
            self.confidence = confidence
        if iou is not None:
            self.iou = iou

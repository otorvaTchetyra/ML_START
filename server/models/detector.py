import numpy as np
from config import settings
from models.types import Detection


class GranuleDetector:

    def __init__(self, model_path: str | None = None, confidence: float | None = None, iou: float | None = None):
        from ultralytics import YOLO
        path = model_path or settings.model_path
        self.confidence = confidence if confidence is not None else settings.model_confidence
        self.iou = iou if iou is not None else settings.model_iou
        self._model = YOLO(path)

    def detect(self, frame: np.ndarray) -> list[Detection]:
        results = self._model(frame, conf=self.confidence, iou=self.iou, verbose=False)
        h, w = frame.shape[:2]
        detections: list[Detection] = []
        for result in results:
            if result.boxes is None:
                continue
            for box in result.boxes:
                x1, y1, x2, y2 = box.xyxy[0].tolist()
                if (x2 - x1) / w > 0.12 or (y2 - y1) / h > 0.12:
                    continue
                conf = float(box.conf[0])
                detections.append(Detection(x1=x1, y1=y1, x2=x2, y2=y2, confidence=conf))
        return detections

    def update_params(self, confidence: float | None = None, iou: float | None = None) -> None:
        if confidence is not None:
            self.confidence = confidence
        if iou is not None:
            self.iou = iou

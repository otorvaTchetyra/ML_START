import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from models.tracker import GranuleTracker
from models.types import Detection


def _det(x1, y1, x2, y2) -> Detection:
    return Detection(x1=x1, y1=y1, x2=x2, y2=y2, confidence=0.9)


def test_first_frame_all_new():
    tracker = GranuleTracker(iou_threshold=0.3)
    dets = [_det(0, 0, 10, 10), _det(20, 20, 30, 30)]
    assert len(tracker.update(dets)) == 2


def test_same_position_not_counted_twice():
    tracker = GranuleTracker(iou_threshold=0.3)
    det = _det(0, 0, 10, 10)
    tracker.update([det])
    result = tracker.update([det])
    assert len(result) == 0


def test_non_overlapping_counted_as_new():
    tracker = GranuleTracker(iou_threshold=0.3)
    tracker.update([_det(0, 0, 10, 10)])
    result = tracker.update([_det(50, 50, 60, 60)])
    assert len(result) == 1


def test_partial_overlap_below_threshold_is_new():
    tracker = GranuleTracker(iou_threshold=0.5)
    tracker.update([_det(0, 0, 10, 10)])
    result = tracker.update([_det(8, 0, 18, 10)])
    assert len(result) == 1


def test_reset_clears_prev_frame():
    tracker = GranuleTracker(iou_threshold=0.3)
    det = _det(0, 0, 10, 10)
    tracker.update([det])
    tracker.reset()
    result = tracker.update([det])
    assert len(result) == 1


def test_new_granule_alongside_existing():
    tracker = GranuleTracker(iou_threshold=0.3)
    old = _det(0, 0, 10, 10)
    tracker.update([old])
    result = tracker.update([old, _det(50, 50, 60, 60)])
    assert len(result) == 1
    assert result[0].x1 == 50


def test_iou_threshold_setter_validates():
    tracker = GranuleTracker()
    try:
        tracker.iou_threshold = 0.0
        assert False, "Should have raised ValueError"
    except ValueError:
        pass

    try:
        tracker.iou_threshold = 1.5
        assert False, "Should have raised ValueError"
    except ValueError:
        pass


def test_iou_threshold_setter_updates_value():
    tracker = GranuleTracker(iou_threshold=0.3)
    tracker.iou_threshold = 0.8
    assert tracker.iou_threshold == 0.8

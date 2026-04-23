import sys
import os
sys.path.insert(0, os.path.join(os.path.dirname(__file__), ".."))

from models.counter import GranuleCounter
from models.types import Detection


def _det(n: int) -> list[Detection]:
    return [Detection(x1=0, y1=0, x2=10, y2=10, confidence=0.9) for _ in range(n)]


def test_empty_frame_returns_zero():
    counter = GranuleCounter(window_sec=5.0)
    result = counter.process_frame([], timestamp=1.0)
    assert result.granule_count == 0
    assert result.intensity_per_sec == 0.0
    assert result.intensity_per_min == 0.0


def test_single_frame_count():
    counter = GranuleCounter(window_sec=5.0)
    result = counter.process_frame(_det(7), timestamp=1.0)
    assert result.granule_count == 7


def test_intensity_single_frame_edge_case():
    counter = GranuleCounter(window_sec=5.0)
    result = counter.process_frame(_det(5), timestamp=0.0)
    assert result.intensity_per_sec == 5.0


def test_intensity_two_frames():
    counter = GranuleCounter(window_sec=10.0)
    counter.process_frame(_det(10), timestamp=0.0)
    result = counter.process_frame(_det(10), timestamp=1.0)
    assert result.intensity_per_sec == 20.0
    assert result.intensity_per_min == 20.0 * 60


def test_sliding_window_evicts_old_frames():
    counter = GranuleCounter(window_sec=2.0)
    counter.process_frame(_det(100), timestamp=0.0)
    result = counter.process_frame(_det(5), timestamp=3.0)
    assert result.intensity_per_sec == 5.0
    assert result.granule_count == 5


def test_total_granules_accumulate():
    counter = GranuleCounter(window_sec=5.0)
    counter.process_frame(_det(3), timestamp=1.0)
    counter.process_frame(_det(4), timestamp=2.0)
    assert counter.total_granules == 7
    assert counter.frame_count == 2


def test_reset_clears_state():
    counter = GranuleCounter(window_sec=5.0)
    counter.process_frame(_det(10), timestamp=1.0)
    counter.reset()
    assert counter.total_granules == 0
    assert counter.frame_count == 0
    result = counter.process_frame(_det(0), timestamp=2.0)
    assert result.intensity_per_sec == 0.0


def test_intensity_per_min_equals_per_sec_times_60():
    counter = GranuleCounter(window_sec=5.0)
    counter.process_frame(_det(6), timestamp=0.0)
    result = counter.process_frame(_det(6), timestamp=2.0)
    assert abs(result.intensity_per_min - result.intensity_per_sec * 60) < 1e-6


def test_window_sec_setter_validates():
    counter = GranuleCounter(window_sec=5.0)
    try:
        counter.window_sec = -1.0
        assert False, "Should have raised ValueError"
    except ValueError:
        pass


def test_detections_are_included_in_result():
    counter = GranuleCounter()
    dets = _det(3)
    result = counter.process_frame(dets, timestamp=1.0)
    assert len(result.detections) == 3

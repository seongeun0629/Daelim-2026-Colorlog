import unittest

import cv2
import numpy as np

from analysis.lighting import apply_wb_with_blue_control, rgb_to_lab
from analysis.oily import analyze_oiliness
from analysis.tone import get_personal_color_season


class DummyLandmark:
    def __init__(self, x, y):
        self.x = x
        self.y = y


def make_face_landmarks(x1=0.15, y1=0.15, x2=0.85, y2=0.85):
    return [
        DummyLandmark(x1, y1),
        DummyLandmark(x2, y1),
        DummyLandmark(x2, y2),
        DummyLandmark(x1, y2),
    ]


class SmokeTest(unittest.TestCase):
    def test_rgb_to_lab_returns_three_values(self):
        values = rgb_to_lab(120, 100, 90)
        self.assertEqual(len(values), 3)

    def test_personal_color_returns_string(self):
        season = get_personal_color_season(70, 5, 10)
        self.assertIsInstance(season, str)
        self.assertTrue(len(season) > 0)

    def test_wb_with_blue_control_returns_frame_and_debug(self):
        frame = np.full((16, 16, 3), 120, dtype=np.uint8)
        corrected, debug = apply_wb_with_blue_control(frame)

        self.assertEqual(corrected.shape, frame.shape)
        self.assertEqual(corrected.dtype, frame.dtype)
        self.assertIn("blue_cast", debug)
        self.assertIn("gain_rgb", debug)
        self.assertEqual(len(debug["gain_rgb"]), 3)

    def test_analyze_oiliness_scores_spotty_face_higher_than_uniform_face(self):
        w = h = 200
        landmarks = make_face_landmarks()

        oily_frame = np.full((h, w, 3), 35, dtype=np.uint8)
        cv2.rectangle(oily_frame, (30, 30), (170, 170), (125, 120, 115), -1)
        cv2.circle(oily_frame, (100, 62), 10, (245, 245, 245), -1)
        cv2.circle(oily_frame, (102, 106), 9, (250, 250, 250), -1)
        oily_status, oily_score, oily_debug = analyze_oiliness(oily_frame, landmarks, w, h)

        uniform_frame = np.full((h, w, 3), 35, dtype=np.uint8)
        cv2.rectangle(uniform_frame, (30, 30), (170, 170), (135, 130, 125), -1)
        uniform_status, uniform_score, uniform_debug = analyze_oiliness(uniform_frame, landmarks, w, h)

        self.assertGreater(oily_score, uniform_score)
        self.assertIn(oily_status, {"Oily", "Possibly Oily", "Not Oily"})
        self.assertIn(uniform_status, {"Oily", "Possibly Oily", "Not Oily"})
        self.assertIn("tzone_focus", oily_debug)
        self.assertIn("bright_density", uniform_debug)


if __name__ == "__main__":
    unittest.main()


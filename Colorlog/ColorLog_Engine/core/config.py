import os

from mediapipe.tasks import python
from mediapipe.tasks.python import vision

BASE_DIR = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODEL_PATH = os.path.join(BASE_DIR, "models", "face_landmarker.task")
OUTPUT_INTERVAL_SECONDS = 1.0
TIMESTAMP_STEP_MS = 33


def build_landmarker_options(for_image=False):
    """
    FaceLandmarker 옵션 빌더.
    - for_image=False (기본): 비디오 모드
    - for_image=True: 단일 이미지 모드
    """
    running_mode = vision.RunningMode.IMAGE if for_image else vision.RunningMode.VIDEO
    return vision.FaceLandmarkerOptions(
        base_options=python.BaseOptions(model_asset_path=MODEL_PATH),
        running_mode=running_mode,
        num_faces=1,
        min_face_detection_confidence=0.5,
        min_tracking_confidence=0.5,
    )


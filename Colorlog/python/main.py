# main.py
import cv2
import mediapipe as mp
import os
import json
import sys
import time

from mediapipe.tasks.python import vision
from mediapipe.tasks import python

from vision.camera import get_frame
from vision.face import detect_face
from vision.roi import get_face_roi

from analysis.lighting import analyze_lighting
from analysis.tone import get_skin_tone
from analysis.tone import SkinToneSmoother
from analysis.tone import get_personal_color_season
from analysis.lighting import rgb_to_lab

BASE_DIR = os.path.dirname(os.path.abspath(__file__))
model_path = os.path.join(BASE_DIR, "models", "face_landmarker.task")

options = vision.FaceLandmarkerOptions(
    base_options=python.BaseOptions(model_asset_path=model_path),
    running_mode=vision.RunningMode.VIDEO,
    num_faces=1,  # C# 처리 속도를 위해 1명으로 제한하는 것을 권장
    min_face_detection_confidence=0.5,
    min_tracking_confidence=0.5
)

cap = cv2.VideoCapture(0)
timestamp = 0
last_output_time = 0  # 마지막으로 JSON을 출력한 시간을 기록

# 1. 스무딩 인스턴스 생성 (루프 밖에서 1회 생성)
smoother = SkinToneSmoother(buffer_size=10)

with vision.FaceLandmarker.create_from_options(options) as landmarker:
    while cap.isOpened():
        ret, frame = get_frame(cap)
        if not ret:
            break

        rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
        timestamp += 33

        result = detect_face(landmarker, mp_image, timestamp)

        # C#으로 보낼 데이터를 담을 딕셔너리
        frame_data = {
            "timestamp": timestamp,
            "face_detected": False,
            "lighting": None,
            "skin_tone": None,
            "personal_color": None
        }

        if result.face_landmarks:
            frame_data["face_detected"] = True
            h, w, _ = frame.shape

            # 첫 번째 인식된 얼굴만 처리
            face_landmarks = result.face_landmarks[0]
            x1, y1, x2, y2 = get_face_roi(face_landmarks, w, h)

            # 조명 분석
            light_status, light_score = analyze_lighting(frame, x1, y1, x2, y2)
            frame_data["lighting"] = {
                "status": light_status,
                "score": light_score
            }

            # 2. 피부톤 분석 (스무딩 클래스 적용)
            tone_rgb = smoother.update_and_get_smoothed_tone(frame, face_landmarks, w, h)

            if tone_rgb:
                r, g, b = tone_rgb
                frame_data["skin_tone"] = {
                    "r": tone_rgb[0],
                    "g": tone_rgb[1],
                    "b": tone_rgb[2]
                }

                # 3. 조명 점수가 0.5 이상일 때만 퍼스널 컬러 진단 수행 (오류 방지)
                if light_score >= 0.5:
                    L, a_val, b_val = rgb_to_lab(r, g, b)
                    personal = get_personal_color_season(L, a_val, b_val)

                    frame_data["personal_color"] = {
                        "type": personal,
                        "lab": {
                            "L": float(L),
                            "a": float(a_val),
                            "b": float(b_val)
                        }
                    }

        # 출력 버퍼를 비워 C# 프로그램이 즉시 데이터를 받을 수 있게 함
        current_time = time.time()
        if current_time - last_output_time >= 1.0:  # 1초 이상 지났다면
            print(json.dumps(frame_data))
            sys.stdout.flush()
            last_output_time = current_time  # 마지막 출력 시간 업데이트

cap.release()
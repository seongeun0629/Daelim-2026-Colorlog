import sys
import os
from pathlib import Path

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

import cv2
import mediapipe as mp
from mediapipe.tasks.python import vision
import numpy as np

from core.config import build_landmarker_options
from vision.face import detect_face


def analyze_single_image(image_path):
    """단일 이미지의 얼굴 감지 상세 분석"""
    print(f"\n분석 대상: {image_path}")
    print("=" * 60)

    frame = cv2.imread(image_path)
    if frame is None:
        print("이미지를 읽을 수 없습니다.")
        return

    h, w = frame.shape[:2]
    print(f"이미지 크기: {w}x{h}")

    # 이미지 통계
    gray = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    brightness = np.mean(gray)
    contrast = np.std(gray)
    print(f"밝기: {brightness:.1f}, 대비: {contrast:.1f}")

    # 컬러 히스토그램
    b_mean, g_mean, r_mean = cv2.mean(frame)[:3]
    print(f"채널 평균: R={r_mean:.1f}, G={g_mean:.1f}, B={b_mean:.1f}")

    # 채도 분석
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
    saturation = np.mean(hsv[:, :, 1])
    print(f"평균 채도: {saturation:.1f} (0-255 범위)")

    # 얼굴 감지
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

    options = build_landmarker_options(for_image=True)
    with vision.FaceLandmarker.create_from_options(options) as landmarker:
        result = detect_face(landmarker, mp_image, 0)

    print(f"\n얼굴 감지 결과:")
    print(f"  face_landmarks 개수: {len(result.face_landmarks) if result.face_landmarks else 0}")
    print(f"  face_blendshapes 개수: {len(result.face_blendshapes) if result.face_blendshapes else 0}")

    if result.face_landmarks:
        for idx, landmarks in enumerate(result.face_landmarks):
            print(f"  - 얼굴 {idx+1}: {len(landmarks)} 개 랜드마크")
            # 얼굴 범위 추정
            if landmarks:
                x_coords = [lm.x for lm in landmarks]
                y_coords = [lm.y for lm in landmarks]
                x_min, x_max = min(x_coords), max(x_coords)
                y_min, y_max = min(y_coords), max(y_coords)
                face_w = (x_max - x_min) * w
                face_h = (y_max - y_min) * h
                face_area_pct = (face_w * face_h) / (w * h) * 100
                print(f"    얼굴 크기: {face_w:.0f}x{face_h:.0f}px ({face_area_pct:.1f}% of image)")
    else:
        print("  ❌ 얼굴이 감지되지 않음")
        print("\n가능 원인:")
        print("  - 얼굴 각도 (45도 이상 회전)")
        print("  - 얼굴 해상도 (너무 작음/큼)")
        print("  - 조명 (너무 어둡거나 밝음)")
        print("  - 얼굴이 부분 가려짐")
        print(f"\n진단 값:")
        print(f"  - 밝기: {brightness:.1f} (권장: 50-200)")
        print(f"  - 대비: {contrast:.1f} (권장: 20 이상)")
        print(f"  - 채도: {saturation:.1f} (권장: 40 이상)")


if __name__ == "__main__":
    target_image = "tests/images/5.jpg"
    analyze_single_image(target_image)


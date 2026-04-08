import cv2
import numpy as np


def analyze_lighting(frame, x1, y1, x2, y2):
    """
    얼굴 ROI를 기반으로 조명 상태를 분석합니다.
    순수한 조명 밝기를 판단하는 로직으로 조명 보정과는 관계가 없습니다.
    """
    # 프레임 경계 예외 처리
    h_frame, w_frame = frame.shape[:2]
    x1, y1 = max(0, x1), max(0, y1)
    x2, y2 = min(w_frame, x2), min(h_frame, y2)

    face_roi = frame[y1:y2, x1:x2]

    if face_roi.size == 0:
        return "Unknown", 0

    # BGR 이미지를 HSV로 변환 후 V(밝기) 채널 추출
    hsv = cv2.cvtColor(face_roi, cv2.COLOR_BGR2HSV)
    v_channel = hsv[:, :, 2]

    # 전체 평균 밝기
    mean_brightness = np.mean(v_channel)

    # 좌우 조명 균일도 분석 (그림자 체크)
    h, w = v_channel.shape
    left_side = v_channel[:, :w // 2]
    right_side = v_channel[:, w // 2:]

    left_brightness = np.mean(left_side) if left_side.size > 0 else 0
    right_brightness = np.mean(right_side) if right_side.size > 0 else 0
    diff = abs(left_brightness - right_brightness)

    # 상태 판별
    if mean_brightness < 60:
        status = "Too Dark"
    elif mean_brightness > 200:
        status = "Too Bright"
    elif diff > 50:
        status = "Uneven (Shadows)"
    else:
        status = "Good"

    return status, int(mean_brightness)

def rgb_to_lab(r, g, b):
    rgb = np.uint8([[[b, g, r]]])  # OpenCV는 BGR
    lab = cv2.cvtColor(rgb, cv2.COLOR_BGR2LAB)

    L = lab[0][0][0]
    a = lab[0][0][1] - 128
    b = lab[0][0][2] - 128

    return L, a, b


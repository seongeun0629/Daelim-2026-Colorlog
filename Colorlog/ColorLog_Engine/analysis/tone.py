# tone.py
import numpy as np
import math
import cv2
from collections import deque

class SkinToneSmoother:
    def __init__(self, buffer_size=10):
        """
        buffer_size: 누적할 프레임 수 (기본값 10)
        maxlen을 지정한 deque는 꽉 차면 가장 오래된 데이터가 자동으로 밀려납니다.
        """
        self.color_buffer = deque(maxlen=buffer_size)

    def update_and_get_smoothed_tone(self, frame, face_landmarks, w, h):
        """
        매 프레임마다 호출되어 버퍼를 업데이트하고 부드러워진 평균 RGB를 반환합니다.
        """
        # 1. 단일 프레임에서 피부톤 추출 (기존에 만든 get_skin_tone 함수 호출)
        current_rgb = get_skin_tone(frame, face_landmarks, w, h)

        # 2. 얼굴이 가려지거나 화면 밖으로 나가서 추출에 실패한 경우
        if current_rgb is None:
            # 기존에 쌓인 데이터가 있다면 그 평균을 유지해서 반환 (끊김 방지)
            if len(self.color_buffer) > 0:
                return self._calculate_average()
            return None

        # 3. 버퍼에 현재 프레임의 RGB 값 추가
        self.color_buffer.append(current_rgb)

        # 4. 버퍼에 누적된 RGB 값들의 평균 계산 후 반환
        return self._calculate_average()

    def _calculate_average(self):
        # 버퍼에 들어있는 RGB 튜플들을 numpy 배열로 변환하여 축(axis=0) 기준으로 평균 계산
        avg_rgb = np.mean(self.color_buffer, axis=0)
        return (int(avg_rgb[0]), int(avg_rgb[1]), int(avg_rgb[2]))

def get_skin_tone(frame, face_landmarks, w, h):
    """
    이마와 턱을 제외하고 양볼의 여러 핵심 랜드마크를 추출하여,
    가중치 기반 평균 피부톤(RGB)을 계산하여 반환합니다.
    (다중 랜드마크 및 중앙값 연산을 통해 이상치에 더 강건하도록 정밀도 향상)
    """
    # MediaPipe Face Mesh 기준 양볼의 여러 랜드마크 인덱스
    # (광대 아래 ~ 턱선 위쪽의 평평한 영역을 넓게 커버하여 홍조/잡티의 영향을 분산시킴)
    left_cheek_indices = [205, 118, 50]
    right_cheek_indices = [425, 347, 280]

    patch_size = 5

    # 패치 영역 BGR 중앙값(Median) 추출 내부 함수
    def get_patch_median(index):
        lm = face_landmarks[index]
        x, y = int(lm.x * w), int(lm.y * h)

        # 화면 밖으로 나가는 것 방지
        x1, y1 = max(0, x - patch_size), max(0, y - patch_size)
        x2, y2 = min(w, x + patch_size), min(h, y + patch_size)

        patch = frame[y1:y2, x1:x2]

        if patch.size == 0:
            return None

        # 평균(mean) 대신 중앙값(median)을 사용하여 픽셀 튀는 현상(빛 반사, 그림자) 방지
        return np.median(patch, axis=(0, 1))

    # 1. 양볼의 각 랜드마크별 색상 추출
    left_colors = [get_patch_median(idx) for idx in left_cheek_indices]
    right_colors = [get_patch_median(idx) for idx in right_cheek_indices]

    # 2. 얼굴 일부가 화면 밖으로 나가서 하나라도 추출이 안 된 경우 예외 처리
    if any(c is None for c in left_colors + right_colors):
        return None

    # 3. 각 볼의 색상 평균 계산
    mean_left = np.mean(left_colors, axis=0)
    mean_right = np.mean(right_colors, axis=0)

    # 4. 양볼 가중치 결합 (이마와 턱이 제외되었으므로 각각 50%의 가중치를 가짐)
    # 한쪽 측면 조명이 강할 경우를 대비해 양쪽을 1:1로 평균 내는 것이 가장 안정적입니다.
    avg_bgr = (mean_left * 0.5) + (mean_right * 0.5)

    # 5. RGB 변환 및 정수화
    avg_rgb = (int(avg_bgr[2]), int(avg_bgr[1]), int(avg_bgr[0]))  # R, G, B

    return avg_rgb

def rgb_to_hsv(rgb):
    """
    RGB 튜플을 HSV로 변환합니다.

    Args:
        rgb: (R, G, B) 튜플, 값은 0-255

    Returns:
        (H, S, V) 튜플
        - H: 0-360도 (색상)
        - S: 0-100% (채도)
        - V: 0-100% (명도)
    """
    r, g, b = rgb[0] / 255.0, rgb[1] / 255.0, rgb[2] / 255.0

    max_c = max(r, g, b)
    min_c = min(r, g, b)
    delta = max_c - min_c

    # Hue 계산
    if delta == 0:
        h = 0
    elif max_c == r:
        h = 60 * (((g - b) / delta) % 6)
    elif max_c == g:
        h = 60 * (((b - r) / delta) + 2)
    else:
        h = 60 * (((r - g) / delta) + 4)

    # Saturation 계산
    s = 0 if max_c == 0 else (delta / max_c) * 100

    # Value 계산
    v = max_c * 100

    return (h, s, v)

def analyze_color_with_hsv(h, s, v):
    """
    HSV 값을 기반으로 색상 특성을 분석합니다.

    Args:
        h: Hue (0-360)
        s: Saturation (0-100)
        v: Value (0-100)

    Returns:
        dict: 색상 특성 분석 결과
    """
    # 웜/쿨 판별 (HSV 기반)
    # 0-60: 빨강 (웜), 60-120: 노랑 (웜), 120-180: 초록 (중립)
    # 180-240: 시안 (쿨), 240-300: 파랑 (쿨), 300-360: 분홍/마젠타 (웜)
    if (h < 60 or h >= 300) or (60 <= h < 120):
        temp_type = "웜"  # Warm
    elif 120 <= h < 240:
        temp_type = "중립"  # Neutral
    else:
        temp_type = "쿨"  # Cool

    # 비비드 vs 뮤트 판별 (채도 기반)
    if s >= 65:
        vividness = "비비드"  # Vivid (채도 높음)
    elif s >= 40:
        vividness = "중간"  # Medium
    else:
        vividness = "뮤트"  # Muted (채도 낮음)

    # 밝기 판별
    if v >= 70:
        brightness = "밝음"  # Light
    elif v >= 40:
        brightness = "중간"  # Medium
    else:
        brightness = "어두움"  # Dark

    return {
        "temperature": temp_type,
        "vividness": vividness,
        "brightness": brightness,
        "h": round(h, 1),
        "s": round(s, 1),
        "v": round(v, 1)
    }

# 사용자의 피부 톤을 반환하는 함수 (Lab + HSV 하이브리드)
def get_personal_color_season(L, a, b, rgb=None):
    """
    Lab과 HSV 정보를 결합하여 9분류 퍼스널 컬러를 반환합니다.
    """
    # 1. Lab 기반 분석
    chroma = math.sqrt(float(a)**2 + float(b)**2)
    is_warm_lab = b > a

    # 2. HSV 기반 분석
    hsv_analysis = None
    is_warm_hsv = None
    if rgb is not None:
        h, s, v = rgb_to_hsv(rgb)
        hsv_analysis = analyze_color_with_hsv(h, s, v)
        is_warm_hsv = hsv_analysis["temperature"] == "웜"

    # 3. 웜/쿨 통합 판별
    if is_warm_hsv is not None:
        is_warm = is_warm_lab if is_warm_lab == is_warm_hsv else is_warm_lab
    else:
        is_warm = is_warm_lab

    # 4. 뉴트럴 판별 — 웜/쿨 구분이 애매하고 채도가 낮을 때
    warm_diff = abs(float(b) - float(a))
    if warm_diff < 3.0 and chroma < 10.0:
        season = "뉴트럴톤 (Neutral)"

    # 5. 9분류
    elif is_warm:
        if L > 65:
            if chroma > 20:
                season = "봄 비비드 웜톤 (Spring Vivid Warm)"
            else:
                season = "봄 라이트 웜톤 (Spring Light Warm)"
        else:
            if chroma > 15:
                season = "가을 딥 웜톤 (Autumn Deep Warm)"
            else:
                season = "가을 뮤트 웜톤 (Autumn Mute Warm)"
    else:
        if L > 65:
            if chroma < 12:
                season = "여름 라이트 쿨톤 (Summer Light Cool)"
            else:
                season = "여름 뮤트 쿨톤 (Summer Mute Cool)"
        else:
            if chroma > 18:
                season = "겨울 비비드 쿨톤 (Winter Vivid Cool)"
            else:
                season = "겨울 딥 쿨톤 (Winter Deep Cool)"

    result = {
        "season": season,
        "temperature": "웜톤" if is_warm else "쿨톤",
        "lab": {
            "l": round(L, 1),
            "a": round(a, 1),
            "b": round(b, 1),
            "chroma": round(chroma, 1)
        }
    }

    if hsv_analysis:
        result["hsv"] = {
            "h": hsv_analysis["h"],
            "s": hsv_analysis["s"],
            "v": hsv_analysis["v"],
            "temperature_hsv": hsv_analysis["temperature"],
            "vividness": hsv_analysis["vividness"],
            "brightness": hsv_analysis["brightness"]
        }

    return result

def get_skin_tone_by_zone(frame, face_landmarks, w, h):
    """
    C# FaceTonePreview의 5개 부위(이마/왼볼/오른볼/코/턱)에 대응하는
    부위별 평균 RGB를 딕셔너리로 반환합니다.
    """
    ZONE_INDICES = {
        "forehead":    [10, 338, 297],
        "left_cheek":  [205, 118, 50],
        "right_cheek": [425, 347, 280],
        "nose":        [1, 2, 98],
        "chin":        [199, 175, 152],
    }
    patch_size = 5
    result = {}

    for zone_name, indices in ZONE_INDICES.items():
        colors = []
        for idx in indices:
            lm = face_landmarks[idx]
            x, y = int(lm.x * w), int(lm.y * h)
            x1, y1 = max(0, x - patch_size), max(0, y - patch_size)
            x2, y2 = min(w, x + patch_size), min(h, y + patch_size)
            patch = frame[y1:y2, x1:x2]
            if patch.size > 0:
                colors.append(np.median(patch, axis=(0, 1)))

        if len(colors) == len(indices):
            avg_bgr = np.mean(colors, axis=0)
            result[zone_name] = (int(avg_bgr[2]), int(avg_bgr[1]), int(avg_bgr[0]))
        else:
            result[zone_name] = None

    return result

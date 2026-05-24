# tone.py
import numpy as np
import math
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

# 사용자의 피부 톤을 반환하는 함수
def get_personal_color_season(L, a, b):
    """
    채도 공식을 이용해 사용자의 사계절 퍼스널 컬러를 반환합니다.
    """
    # 1. 웜/쿨 판별 (기준점은 카메라/조명 캘리브레이션에 따라 미세 조정 필요)
    is_warm = b > a

    # 2. 채도 계산 (float으로 변환해 오버플로우 방지)
    chroma = math.sqrt(float(a)**2 + float(b)**2)

    # 3. 사계절 분류 휴리스틱 (예시 모델)
    if is_warm:
        if L > 65 and chroma > 15:
            return "봄 웜톤 (Spring Warm)" # 밝고 생기 있음
        else:
            return "가을 웜톤 (Autumn Warm)" # 차분하고 성숙함
    else:
        if L > 65 and chroma < 15:
            return "여름 쿨톤 (Summer Cool)" # 밝고 투명/뮤트함
        else:
            return "겨울 쿨톤 (Winter Cool)" # 대비가 강하거나 창백함


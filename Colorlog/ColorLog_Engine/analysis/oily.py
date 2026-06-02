from __future__ import annotations

from typing import Dict, Tuple

import cv2
import numpy as np

from vision.roi import get_face_roi


def _clip01(value: float) -> float:
    return float(np.clip(value, 0.0, 1.0))


def _build_region_masks(height: int, width: int) -> Dict[str, np.ndarray]:
    """
    얼굴 ROI 내부에서 유분기 판단용 구역을 나눕니다.
    - forehead: 이마 상단 중앙부
    - nose: 코와 콧망울 중심부
    - cheeks: 좌우 볼 중심부
    - t_zone: 이마 + 코
    """
    yy, xx = np.ogrid[:height, :width]

    margin_x = int(width * 0.05)
    margin_y = int(height * 0.05)
    inner = (
        (xx >= margin_x)
        & (xx < width - margin_x)
        & (yy >= margin_y)
        & (yy < height - margin_y)
    )

    forehead = inner & (yy <= int(height * 0.30)) & (xx >= int(width * 0.22)) & (xx <= int(width * 0.78))
    nose = inner & (yy >= int(height * 0.20)) & (yy <= int(height * 0.75)) & (xx >= int(width * 0.35)) & (xx <= int(width * 0.65))

    left_cheek = inner & (yy >= int(height * 0.28)) & (yy <= int(height * 0.85)) & (xx >= int(width * 0.10)) & (xx <= int(width * 0.42))
    right_cheek = inner & (yy >= int(height * 0.28)) & (yy <= int(height * 0.85)) & (xx >= int(width * 0.58)) & (xx <= int(width * 0.90))

    t_zone = forehead | nose
    cheeks = left_cheek | right_cheek

    return {
        "inner": inner,
        "forehead": forehead,
        "nose": nose,
        "t_zone": t_zone,
        "cheeks": cheeks,
    }


def analyze_oiliness(frame_bgr, face_landmarks, w: int, h: int) -> Tuple[str, int, Dict[str, float | int | list | str]]:
    """
    얼굴 사진에서 유분기(oily) 여부를 휴리스틱으로 추정합니다.

    판단 기준:
    - 이마/코(T-zone)에 주변보다 훨씬 밝은 반사점이 얼마나 많이 있는지
    - 그 밝은 점들이 특정 구역(T-zone)에 얼마나 집중되는지
    - 얼굴 전체가 고르게 밝은지(조명 영향) vs 국소적으로만 번들거리는지

    반환:
    - status: "Oily", "Possibly Oily", "Not Oily", "Unknown"
    - score: 0~100
    - debug: 상세 특징값
    """
    if face_landmarks is None:
        return "Unknown", 0, {"reason": "face_landmarks_none"}

    x1, y1, x2, y2 = get_face_roi(face_landmarks, w, h)
    x1, y1 = max(0, x1), max(0, y1)
    x2, y2 = min(w, x2), min(h, y2)

    face_roi = frame_bgr[y1:y2, x1:x2]
    if face_roi.size == 0:
        return "Unknown", 0, {"reason": "empty_face_roi"}

    lab = cv2.cvtColor(face_roi, cv2.COLOR_BGR2LAB)
    l_channel = lab[:, :, 0].astype(np.float32)
    sat_channel = cv2.cvtColor(face_roi, cv2.COLOR_BGR2HSV)[:, :, 1].astype(np.float32)

    h_roi, w_roi = l_channel.shape
    if h_roi < 8 or w_roi < 8:
        return "Unknown", 0, {"reason": "roi_too_small", "roi_size": [int(w_roi), int(h_roi)]}

    blurred = cv2.GaussianBlur(l_channel, (0, 0), sigmaX=7.0, sigmaY=7.0)
    residual = l_channel - blurred

    # 주변보다 유난히 밝은 점을 잡기 위해 밝기와 국소 잔차를 함께 사용합니다.
    l_threshold = max(float(np.percentile(l_channel, 88)), float(np.mean(l_channel) + 8.0))
    residual_threshold = max(float(np.percentile(residual, 80)), 6.0)
    sat_threshold = min(float(np.percentile(sat_channel, 60)), 120.0)

    bright_mask = (l_channel >= l_threshold) & (residual >= residual_threshold) & (sat_channel <= sat_threshold)

    masks = _build_region_masks(h_roi, w_roi)
    inner_mask = masks["inner"]
    forehead_mask = masks["forehead"]
    nose_mask = masks["nose"]
    t_zone_mask = masks["t_zone"]
    cheeks_mask = masks["cheeks"]

    bright_mask = bright_mask & inner_mask

    face_pixels = int(np.count_nonzero(inner_mask))
    if face_pixels == 0:
        return "Unknown", 0, {"reason": "no_face_pixels"}

    spot_pixels = int(np.count_nonzero(bright_mask))
    tzone_spots = int(np.count_nonzero(bright_mask & t_zone_mask))
    cheek_spots = int(np.count_nonzero(bright_mask & cheeks_mask))

    forehead_pixels = int(np.count_nonzero(forehead_mask))
    nose_pixels = int(np.count_nonzero(nose_mask))
    tzone_pixels = int(np.count_nonzero(t_zone_mask))
    cheek_pixels = int(np.count_nonzero(cheeks_mask))

    bright_density = spot_pixels / max(face_pixels, 1)
    tzone_density = tzone_spots / max(tzone_pixels, 1)
    cheek_density = cheek_spots / max(cheek_pixels, 1)
    tzone_focus = tzone_spots / max(spot_pixels, 1)

    if spot_pixels > 0:
        residual_strength = float(np.mean(residual[bright_mask]))
        mean_bright_l = float(np.mean(l_channel[bright_mask]))
    else:
        residual_strength = 0.0
        mean_bright_l = 0.0

    overall_mean_l = float(np.mean(l_channel))
    overall_std_l = float(np.std(l_channel))

    # 밝은 점들이 여러 개로 분산되어 있는지 확인합니다. (최소 크기 필터링 적용)
    num_labels, _labels, stats, _ = cv2.connectedComponentsWithStats(bright_mask.astype(np.uint8), connectivity=8)
    # 너무 작은 덩어리(< 20 픽셀)는 제외 — 국소적 하이라이트/노이즈 필터링
    component_areas = [int(stats[i, cv2.CC_STAT_AREA]) for i in range(1, num_labels) if int(stats[i, cv2.CC_STAT_AREA]) >= 20]
    component_count = len(component_areas)
    largest_component = max(component_areas) if component_areas else 0

    spot_density_score = _clip01(bright_density / 0.025)
    # T-zone 밀도 임계값을 엄격하게: 0.08 → 0.06 (실제 유분만 카운트)
    tzone_density_score = _clip01(tzone_density / 0.06)
    tzone_focus_score = _clip01(tzone_focus)
    # 컴포넌트 스코어를 더 엄격하게: 기준을 3.0 → 2.0으로 높여 많은 작은 덩어리는 최댓값 못 도달하도록
    component_score = _clip01(component_count / 2.0)
    residual_score = _clip01((residual_strength - 4.0) / 12.0)
    # 볼의 유분이 많으면 감점 강화: 계수 0.03 → 0.02
    cheek_penalty = _clip01(1.0 - cheek_density / 0.02)
    contrast_gate = _clip01((overall_std_l - 5.0) / 15.0)
    brightness_gate = _clip01((overall_mean_l - 55.0) / 110.0)

    # 기본 점수 구성 재설계: T-zone 중심 (합 = 1.0)
    # - T-zone 밀도(40%) + 집중도(25%) = 65% (T-zone이 주요 지표)
    # - 전체 밀도(10%) + 컴포넌트(5%) = 15% (보조 지표)
    # - 잔차(10%) + 페널티(10%) = 20% (보정)
    base_score = (
        0.10 * spot_density_score       # 내려감: 25% → 10% (전체 밀도 감소)
        + 0.05 * component_score        # 내려감: 20% → 5% (컴포넌트 감소 + 크기 필터링)
        + 0.40 * tzone_density_score    # 올려감: 25% → 40% (T-zone 밀도 강조)
        + 0.25 * tzone_focus_score      # 올려감: 15% → 25% (T-zone 집중도 강조)
        + 0.10 * residual_score         # 내려감: 15% → 10%
        + 0.10 * (1.0 - cheek_penalty)  # 추가: 10% (볼에 많으면 감점)
    )
    score01 = base_score * (0.55 + 0.45 * contrast_gate) * (0.55 + 0.45 * brightness_gate)
    
    # T-zone 집중도 게이트: T-zone에 집중되지 않으면 점수 페널티
    # tzone_focus < 0.30이면 점수 반감 (T-zone에 집중된 유분이 아니면 무시)
    if tzone_focus < 0.30:
        score01 *= 0.5
    elif tzone_focus < 0.50:
        score01 *= (0.5 + 0.5 * tzone_focus / 0.50)  # 0.30~0.50 범위에서 선형 보정
    
    score = int(round(np.clip(score01, 0.0, 1.0) * 100.0))

    # 조정된 판정 기준: T-zone 중심의 엄격한 기준
    # 이전: Oily >= 55, Possibly Oily >= 35
    # 개선: Oily >= 65, Possibly Oily >= 45 (Normal 이미지와의 구분을 위해 기준 상향)
    if score >= 65:
        status = "Oily"
    elif score >= 45:
        status = "Possibly Oily"
    else:
        status = "Not Oily"

    debug: Dict[str, float | int | list | str] = {
        "status": status,
        "score": score,
        "face_size": [int(w_roi), int(h_roi)],
        "forehead_pixels": forehead_pixels,
        "nose_pixels": nose_pixels,
        "spot_pixels": spot_pixels,
        "face_pixels": face_pixels,
        "bright_density": round(float(bright_density), 6),
        "tzone_spots": tzone_spots,
        "tzone_pixels": tzone_pixels,
        "tzone_density": round(float(tzone_density), 6),
        "tzone_focus": round(float(tzone_focus), 6),
        "cheek_spots": cheek_spots,
        "cheek_pixels": cheek_pixels,
        "cheek_density": round(float(cheek_density), 6),
        "component_count": component_count,
        "largest_component": largest_component,
        "mean_bright_l": round(float(mean_bright_l), 3),
        "overall_mean_l": round(float(overall_mean_l), 3),
        "overall_std_l": round(float(overall_std_l), 3),
        "l_threshold": round(float(l_threshold), 3),
        "residual_threshold": round(float(residual_threshold), 3),
        "sat_threshold": round(float(sat_threshold), 3),
    }
    return status, score, debug


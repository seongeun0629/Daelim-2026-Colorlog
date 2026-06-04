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


def _srgb_to_linear(img_rgb_u8):
    x = img_rgb_u8.astype(np.float32) / 255.0
    return np.where(x <= 0.04045, x / 12.92, ((x + 0.055) / 1.055) ** 2.4)


def _linear_to_srgb(img_rgb_linear):
    x = np.clip(img_rgb_linear, 0.0, 1.0)
    s = np.where(x <= 0.0031308, 12.92 * x, 1.055 * (x ** (1.0 / 2.4)) - 0.055)
    return (np.clip(s, 0.0, 1.0) * 255.0).astype(np.uint8)


def _robust_channel_mean(rgb_linear, low_pct=2.0, high_pct=98.0):
    means = []
    for c in range(3):
        ch = rgb_linear[:, :, c].reshape(-1)
        lo, hi = np.percentile(ch, [low_pct, high_pct])
        valid = ch[(ch >= lo) & (ch <= hi)]
        means.append(float(valid.mean()) if valid.size else float(ch.mean()))
    return np.array(means, dtype=np.float32)


def _detect_overprocessed_score(frame_bgr):
    hsv = cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2HSV)
    sat = hsv[:, :, 1].astype(np.float32)
    val = hsv[:, :, 2].astype(np.float32)

    high_sat_ratio = float(np.mean(sat > 220.0))
    clip_ratio = float(np.mean(val > 250.0))

    return float(np.clip(0.6 * high_sat_ratio + 0.4 * clip_ratio, 0.0, 1.0))


def apply_wb_with_blue_control(frame_bgr, prev_gain_rgb=None, ema_beta=0.8):
    """
    자연광/형광등 환경 차이를 줄이기 위한 자동 화이트밸런스 보정.
    - blue cast가 높으면 B 채널 보정을 제한해 청광 영향 완화
    - gray-world 기반 채널 게인
    - 과보정 이미지일수록 보정 강도를 자동으로 완화
    - 연속 프레임 처리 시 EMA로 게인을 스무딩
    """
    eps = 1e-6

    rgb_u8 = np.asarray(cv2.cvtColor(frame_bgr, cv2.COLOR_BGR2RGB), dtype=np.uint8)
    rgb_linear = _srgb_to_linear(rgb_u8)

    means = _robust_channel_mean(rgb_linear)  # R, G, B
    rm, gm, bm = float(means[0]), float(means[1]), float(means[2])

    blue_cast = bm / (((rm + gm) * 0.5) + eps)
    overprocessed_score = _detect_overprocessed_score(frame_bgr)

    base_alpha = 0.65
    alpha = base_alpha * (1.0 - 0.6 * overprocessed_score)

    target = float(np.mean(means))
    g_r = target / (rm + eps)
    g_g = target / (gm + eps)
    g_b = target / (bm + eps)

    blue_reduce = float(np.clip((blue_cast - 1.03) * 0.45, 0.0, 0.12))
    if blue_cast > 1.03:
        g_b = min(g_b, 1.0 - blue_reduce)

    gain_rgb = np.array(
        [
            (1.0 - alpha) + alpha * g_r,
            (1.0 - alpha) + alpha * g_g,
            (1.0 - alpha) + alpha * g_b,
        ],
        dtype=np.float32,
    )
    gain_rgb = np.clip(gain_rgb, 0.85, 1.18)

    if prev_gain_rgb is not None:
        prev_gain_rgb = np.asarray(prev_gain_rgb, dtype=np.float32)
        if prev_gain_rgb.shape == (3,):
            gain_rgb = ema_beta * prev_gain_rgb + (1.0 - ema_beta) * gain_rgb

    corrected_linear = rgb_linear * gain_rgb[None, None, :]
    out_rgb_u8 = _linear_to_srgb(corrected_linear)
    corrected_bgr = cv2.cvtColor(out_rgb_u8, cv2.COLOR_RGB2BGR)

    debug = {
        "blue_cast": float(blue_cast),
        "overprocessed_score": float(overprocessed_score),
        "alpha": float(alpha),
        "gain_rgb": [float(gain_rgb[0]), float(gain_rgb[1]), float(gain_rgb[2])],
    }
    return corrected_bgr, debug

def rgb_to_lab(r, g, b):
    rgb = np.uint8([[[b, g, r]]])  # OpenCV는 BGR
    lab = cv2.cvtColor(rgb, cv2.COLOR_BGR2LAB)

    L = lab[0][0][0] / 255.0 * 100.0
    a = lab[0][0][1] - 128
    b = lab[0][0][2] - 128

    return L, a, b


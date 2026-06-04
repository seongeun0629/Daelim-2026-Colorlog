import numpy as np

from analysis.lighting import analyze_lighting, apply_wb_with_blue_control, rgb_to_lab
from analysis.oily import analyze_oiliness
from analysis.tone import get_personal_color_season, get_skin_tone_by_zone
from vision.roi import get_face_roi


def build_frame_payload(timestamp):
    return {
        "timestamp": timestamp,
        "face_detected": False,
        "lighting": None,
        "lighting_debug": None,
        "oily": None,
        "oily_debug": None,
        "skin_tone": None,
        "personal_color": None,
    }


def process_frame(frame, result, timestamp, smoother):
    frame_data = build_frame_payload(timestamp)

    prev_gain = getattr(smoother, "_wb_prev_gain_rgb", None)
    corrected_frame, wb_debug = apply_wb_with_blue_control(
        frame,
        prev_gain_rgb=prev_gain,
        ema_beta=0.8,
    )
    smoother._wb_prev_gain_rgb = np.array(wb_debug["gain_rgb"], dtype=np.float32)
    frame_data["lighting_debug"] = wb_debug

    if not result.face_landmarks:
        return frame_data

    frame_data["face_detected"] = True
    h, w, _ = corrected_frame.shape

    face_landmarks = result.face_landmarks[0]
    x1, y1, x2, y2 = get_face_roi(face_landmarks, w, h)

    # 조명 분석
    light_status, light_score = analyze_lighting(corrected_frame, x1, y1, x2, y2)
    frame_data["lighting"] = {"status": light_status, "score": light_score}

    # 유분 분석
    oily_status, oily_score, oily_debug = analyze_oiliness(corrected_frame, face_landmarks, w, h)
    frame_data["oily"] = {"status": oily_status, "score": oily_score}
    frame_data["oily_debug"] = oily_debug

    # 피부톤 스무딩 (전체 평균 — 버퍼 유지용)
    tone_rgb = smoother.update_and_get_smoothed_tone(corrected_frame, face_landmarks, w, h)
    if not tone_rgb:
        return frame_data

    r, g, b = tone_rgb

    # 부위별 색상 추출 (C# WPF <-> FaceTonePreview와 1:1 매핑)
    zones = get_skin_tone_by_zone(corrected_frame, face_landmarks, w, h)
    frame_data["skin_tone"] = {
        "r": r, "g": g, "b": b,          # 전체 평균 (기존 호환 유지)
        "forehead":    _zone_dict(zones.get("forehead"),    r, g, b),
        "left_cheek":  _zone_dict(zones.get("left_cheek"),  r, g, b),
        "right_cheek": _zone_dict(zones.get("right_cheek"), r, g, b),
        "nose":        _zone_dict(zones.get("nose"),        r, g, b),
        "chin":        _zone_dict(zones.get("chin"),        r, g, b),
    }

    if light_status == "Good":
        L, a_val, b_val = rgb_to_lab(r, g, b)
        personal = get_personal_color_season(L, a_val, b_val, rgb=tone_rgb)
        frame_data["personal_color"] = {
            "type": personal["season"],
            "lab": {
                "L": personal["lab"]["l"],
                "a": personal["lab"]["a"],
                "b": personal["lab"]["b"],
            },
            "temperature": personal["temperature"],
            "hsv": personal.get("hsv"),
        }
    return frame_data


def _zone_dict(zone_rgb, fallback_r, fallback_g, fallback_b):
    """부위별 RGB가 없으면 전체 평균으로 폴백."""
    if zone_rgb is None:
        return {"r": fallback_r, "g": fallback_g, "b": fallback_b}
    return {"r": zone_rgb[0], "g": zone_rgb[1], "b": zone_rgb[2]}

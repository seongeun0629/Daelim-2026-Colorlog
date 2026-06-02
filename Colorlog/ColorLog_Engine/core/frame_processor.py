import numpy as np

from analysis.lighting import analyze_lighting, apply_wb_with_blue_control, rgb_to_lab
from analysis.oily import analyze_oiliness
from analysis.tone import get_personal_color_season
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

    light_status, light_score = analyze_lighting(corrected_frame, x1, y1, x2, y2)
    frame_data["lighting"] = {"status": light_status, "score": light_score}

    oily_status, oily_score, oily_debug = analyze_oiliness(corrected_frame, face_landmarks, w, h)
    frame_data["oily"] = {"status": oily_status, "score": oily_score}
    frame_data["oily_debug"] = oily_debug

    tone_rgb = smoother.update_and_get_smoothed_tone(corrected_frame, face_landmarks, w, h)
    if not tone_rgb:
        return frame_data

    r, g, b = tone_rgb
    frame_data["skin_tone"] = {"r": r, "g": g, "b": b}

    # Keep current threshold behavior to avoid changing existing output semantics.
    if light_score >= 0.5:
        L, a_val, b_val = rgb_to_lab(r, g, b)
        # RGB를 함께 전달하여 HSV 기반 분석 활성화 (하이브리드 방식)
        personal = get_personal_color_season(L, a_val, b_val, rgb=tone_rgb)
        frame_data["personal_color"] = personal

    return frame_data


from analysis.lighting import analyze_lighting, rgb_to_lab
from analysis.tone import get_personal_color_season
from vision.roi import get_face_roi


def build_frame_payload(timestamp):
    return {
        "timestamp": timestamp,
        "face_detected": False,
        "lighting": None,
        "skin_tone": None,
        "personal_color": None,
    }


def process_frame(frame, result, timestamp, smoother):
    frame_data = build_frame_payload(timestamp)

    if not result.face_landmarks:
        return frame_data

    frame_data["face_detected"] = True
    h, w, _ = frame.shape

    face_landmarks = result.face_landmarks[0]
    x1, y1, x2, y2 = get_face_roi(face_landmarks, w, h)

    light_status, light_score = analyze_lighting(frame, x1, y1, x2, y2)
    frame_data["lighting"] = {"status": light_status, "score": light_score}

    tone_rgb = smoother.update_and_get_smoothed_tone(frame, face_landmarks, w, h)
    if not tone_rgb:
        return frame_data

    r, g, b = tone_rgb
    frame_data["skin_tone"] = {"r": r, "g": g, "b": b}

    # Keep current threshold behavior to avoid changing existing output semantics.
    if light_score >= 0.5:
        L, a_val, b_val = rgb_to_lab(r, g, b)
        personal = get_personal_color_season(L, a_val, b_val)
        frame_data["personal_color"] = {
            "type": personal,
            "lab": {"L": float(L), "a": float(a_val), "b": float(b_val)},
        }

    return frame_data


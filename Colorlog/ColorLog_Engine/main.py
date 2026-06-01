# main.py
import argparse
import cv2
import mediapipe as mp
from collections import Counter
from mediapipe.tasks.python import vision

from vision.camera import get_frame
from vision.face import detect_face
from analysis.tone import SkinToneSmoother
from core.config import OUTPUT_INTERVAL_SECONDS, TIMESTAMP_STEP_MS, build_landmarker_options
from core.frame_processor import process_frame
from core.json_output import JsonOutputThrottler
from db import (
    create_tables, seed_personal_color_types, seed_products,
    get_or_create_user, add_diagnosis, get_color_type_by_name,
    save_ai_recommendations, get_recommended_products,
)
from db.recommendation import get_ai_recommendation

DB_SAVE_THRESHOLD = 30


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--user-name", default="사용자", help="사용자 이름")
    parser.add_argument("--age", default=None, help="나이대 예: 20대")
    parser.add_argument("--user-id", type=int, default=None, help="C#에서 전달하는 확정 user_id")
    args = parser.parse_args()

    create_tables()
    seed_personal_color_types()
    seed_products()

    # C#이 user_id를 넘겨주면 그대로 사용, 없으면 이름으로 find-or-create
    if args.user_id is not None:
        user_id = args.user_id
    else:
        user_id = get_or_create_user(args.user_name, age=args.age)

    options = build_landmarker_options()
    cap = cv2.VideoCapture(0)
    timestamp = 0

    smoother = SkinToneSmoother(buffer_size=10)
    output = JsonOutputThrottler(interval_seconds=OUTPUT_INTERVAL_SECONDS)

    sample_buffer = []
    diagnosis_saved = False

    try:
        with vision.FaceLandmarker.create_from_options(options) as landmarker:
            while cap.isOpened():
                ret, frame = get_frame(cap)
                if not ret:
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp += TIMESTAMP_STEP_MS

                result = detect_face(landmarker, mp_image, timestamp, use_video_mode=True)
                frame_data = process_frame(frame, result, timestamp, smoother)

                if not diagnosis_saved:
                    pc = frame_data.get("personal_color")
                    if pc:
                        lab = pc.get("lab", {})
                        sample_buffer.append({
                            "type": pc["type"],
                            "L": lab.get("L", 0.0),
                            "a": lab.get("a", 0.0),
                            "b": lab.get("b", 0.0),
                        })

                        if len(sample_buffer) >= DB_SAVE_THRESHOLD:
                            n = len(sample_buffer)
                            avg_L = sum(s["L"] for s in sample_buffer) / n
                            avg_a = sum(s["a"] for s in sample_buffer) / n
                            avg_b = sum(s["b"] for s in sample_buffer) / n

                            best_type_name = Counter(s["type"] for s in sample_buffer).most_common(1)[0][0]
                            color_type = get_color_type_by_name(best_type_name)
                            type_id = color_type["type_id"] if color_type else None

                            brightness_val = int(min(100, max(0, avg_L)))
                            redness_val = int(min(100, max(0, avg_a + 50)))

                            diagnosis_id = add_diagnosis(
                                user_id=user_id,
                                lab_l=avg_L,
                                lab_a=avg_a,
                                lab_b=avg_b,
                                brightness=brightness_val,
                                redness=redness_val,
                                type_id=type_id,
                            )

                            # AI 제품 추천 (실패 시 더미 시드 폴백)
                            preferred_style = color_type.get("keyword", "") if color_type else ""
                            ai_recs = get_ai_recommendation(best_type_name, preferred_style)

                            if ai_recs:
                                recommendations = save_ai_recommendations(diagnosis_id, ai_recs)
                            else:
                                tone_type = "쿨" if "쿨" in best_type_name else "웜"
                                fallback = get_recommended_products(tone_type)
                                recommendations = [
                                    {
                                        "product_id": p["product_id"],
                                        "product_name": p.get("product_name", ""),
                                        "product_url": p.get("product_url", ""),
                                        "category": p.get("category", ""),
                                        "reason": "퍼스널컬러 기반 추천",
                                    }
                                    for p in fallback[:3]
                                ]

                            frame_data["recommendations"] = recommendations

                            frame_data["diagnosis_saved"] = True
                            frame_data["diagnosis_id"] = diagnosis_id
                            frame_data["user_id"] = user_id
                            frame_data["color_type_info"] = {
                                "type_id": type_id,
                                "type_name": best_type_name,
                                "colors": color_type["colors"] if color_type else None,
                                "worst_colors": color_type["worst_colors"] if color_type else None,
                                "keyword": color_type["keyword"] if color_type else None,
                            }
                            diagnosis_saved = True
                            # throttle을 우회하여 diagnosis_saved 프레임을 즉시 강제 출력
                            output.last_output_time = 0

                output.emit(frame_data)
    finally:
        cap.release()

if __name__ == "__main__":
    main()

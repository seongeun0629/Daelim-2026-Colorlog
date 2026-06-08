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
    save_ai_recommendations, get_recommended_products, add_rec_product,
    get_monthly_stats,  # ✅ 추가
)
from db.recommendation import get_ai_recommendation

DB_SAVE_THRESHOLD = 30


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--user-name", default="사용자", help="사용자 이름")
    parser.add_argument("--age", default=None, help="나이대 예: 20대")
    parser.add_argument("--user-id", type=int, default=None, help="C#에서 전달하는 확정 user_id")
    parser.add_argument("--video", default=None, help="테스트용 동영상 파일 경로")
    args = parser.parse_args()

    create_tables()
    seed_personal_color_types()
    seed_products()

    if args.user_id is not None:
        user_id = args.user_id
    else:
        user_id = get_or_create_user(args.user_name, age=args.age)

    options = build_landmarker_options()

    if args.video:
        cap = cv2.VideoCapture(args.video)
    else:
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

                        # 진행률 계산
                        frame_data["diagnosis_progress"] = min(
                            int(len(sample_buffer) / DB_SAVE_THRESHOLD * 100), 95
                        )

                        if len(sample_buffer) >= DB_SAVE_THRESHOLD:
                            n = len(sample_buffer)
                            avg_L = float(sum(s["L"] for s in sample_buffer)) / n
                            avg_a = float(sum(s["a"] for s in sample_buffer)) / n
                            avg_b = float(sum(s["b"] for s in sample_buffer)) / n

                            best_type_name = Counter(s["type"] for s in sample_buffer).most_common(1)[0][0]
                            color_type = get_color_type_by_name(best_type_name)
                            type_id = color_type["type_id"] if color_type else None

                            brightness_val = int(min(100, max(0, avg_L)))
                            redness_val = int(min(100, max(0, avg_a + 50)))

                            # oily 데이터 추출
                            oily_data = frame_data.get("oily", {})
                            oily_status_val = oily_data.get("status") if oily_data else None
                            oily_score_val = oily_data.get("score") if oily_data else None

                            diagnosis_id = add_diagnosis(
                                user_id=user_id,
                                lab_l=avg_L, lab_a=avg_a, lab_b=avg_b,
                                brightness=brightness_val, redness=redness_val,
                                type_id=type_id,
                                oily_status=oily_status_val, oily_score=oily_score_val,
                                zone_forehead=get_zone("forehead"),   
                                zone_lcheek=get_zone("left_cheek"),   
                                zone_rcheek=get_zone("right_cheek"),  
                                zone_nose=get_zone("nose"),           
                                zone_chin=get_zone("chin"),           
                            )

                            monthly_stats = get_monthly_stats(user_id)
                            ai_color_type = monthly_stats["most_color_type"] or best_type_name
                            ai_brightness = monthly_stats["avg_brightness"] if monthly_stats["avg_brightness"] >= 0 else brightness_val
                            ai_redness = monthly_stats["avg_redness"] if monthly_stats["avg_redness"] >= 0 else redness_val

                            preferred_style = color_type.get("keyword", "") if color_type else ""
                            ai_recs = get_ai_recommendation(
                                ai_color_type,
                                preferred_style,
                                brightness=ai_brightness,
                                redness=ai_redness,
                            )

                            if ai_recs:
                                recommendations = save_ai_recommendations(diagnosis_id, ai_recs)
                            else:
                                tone_type = "쿨" if "쿨" in best_type_name else "웜"
                                fallback = get_recommended_products(tone_type)
                                makeup_cats = {"치크", "립", "아이", "베이스"}
                                makeup_items = [p for p in fallback if p.get("category") in makeup_cats][:3]
                                skincare_items = [p for p in fallback if p.get("category") not in makeup_cats][:2]
                                selected = makeup_items + skincare_items
                                recommendations = [
                                    {
                                        "product_id": p["product_id"],
                                        "product_name": p.get("product_name", ""),
                                        "product_url": p.get("product_url", ""),
                                        "category": p.get("category", ""),
                                        "reason": "퍼스널컬러 기반 추천",
                                    }
                                    for p in selected
                                ]
                                for item in recommendations:
                                    add_rec_product(item["product_id"], diagnosis_id, item["reason"])

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
                            output.last_output_time = 0

                output.emit(frame_data)
    finally:
        cap.release()

if __name__ == "__main__":
    main()

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
)

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

    # 동영상 또는 카메라 선택
    if args.video:
        cap = cv2.VideoCapture(args.video)
    else:
        # cap = cv2.VideoCapture(0)  # 실제 카메라 사용 시 주석 해제
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

                        # sample_buffer에 추가한 후에 진행률 계산
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

                            diagnosis_id = add_diagnosis(
                                user_id=user_id,
                                lab_l=avg_L,
                                lab_a=avg_a,
                                lab_b=avg_b,
                                brightness=brightness_val,
                                redness=redness_val,
                                type_id=type_id,
                            )

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

import argparse
import json
import os
import sys

import cv2
import mediapipe as mp
from mediapipe.tasks.python import vision

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

from analysis.tone import SkinToneSmoother
from core.config import TIMESTAMP_STEP_MS, build_landmarker_options
from core.frame_processor import process_frame
from vision.face import detect_face


def parse_args():
    parser = argparse.ArgumentParser(
        description="동영상 파일로 얼굴/조명/피부톤 파이프라인을 임시 점검합니다."
    )
    parser.add_argument("--video", required=True, help="입력 동영상 파일 경로")
    parser.add_argument(
        "--max-frames",
        type=int,
        default=300,
        help="처리할 최대 프레임 수 (기본값: 300)",
    )
    parser.add_argument(
        "--output-jsonl",
        default="",
        help="프레임별 payload를 JSONL로 저장할 경로 (선택)",
    )
    return parser.parse_args()


def run_video_probe(video_path, max_frames=300, output_jsonl=""):
    if not os.path.exists(video_path):
        raise FileNotFoundError("동영상 파일을 찾을 수 없습니다: {0}".format(video_path))

    cap = cv2.VideoCapture(video_path)
    if not cap.isOpened():
        raise RuntimeError("동영상 파일을 열 수 없습니다: {0}".format(video_path))

    smoother = SkinToneSmoother(buffer_size=10)
    timestamp = 0

    total_frames = 0
    detected_frames = 0
    lighting_scores = []
    oily_scores = []
    oily_detected_frames = 0

    out_fp = open(output_jsonl, "w", encoding="utf-8") if output_jsonl else None

    try:
        options = build_landmarker_options()
        with vision.FaceLandmarker.create_from_options(options) as landmarker:
            while cap.isOpened() and total_frames < max_frames:
                ret, frame = cap.read()
                if not ret:
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp += TIMESTAMP_STEP_MS

                result = detect_face(landmarker, mp_image, timestamp, use_video_mode=True)
                payload = process_frame(frame, result, timestamp, smoother)

                total_frames += 1
                if payload.get("face_detected"):
                    detected_frames += 1
                if payload.get("lighting") and payload["lighting"].get("score") is not None:
                    lighting_scores.append(float(payload["lighting"]["score"]))
                if payload.get("oily") and payload["oily"].get("score") is not None:
                    oily_scores.append(float(payload["oily"]["score"]))
                    if payload["oily"].get("status") == "Oily":
                        oily_detected_frames += 1

                if out_fp is not None:
                    out_fp.write(json.dumps(payload, ensure_ascii=False) + "\n")

    finally:
        cap.release()
        if out_fp is not None:
            out_fp.close()

    avg_light = round(sum(lighting_scores) / len(lighting_scores), 2) if lighting_scores else None
    avg_oily = round(sum(oily_scores) / len(oily_scores), 2) if oily_scores else None
    summary = {
        "video": video_path,
        "total_frames": total_frames,
        "face_detected_frames": detected_frames,
        "face_detect_ratio": round(detected_frames / total_frames, 4) if total_frames else 0.0,
        "avg_lighting_score": avg_light,
        "avg_oily_score": avg_oily,
        "oily_detected_frames": oily_detected_frames,
        "jsonl_output": output_jsonl if output_jsonl else None,
    }
    return summary


def main():
    args = parse_args()
    summary = run_video_probe(
        video_path=args.video,
        max_frames=args.max_frames,
        output_jsonl=args.output_jsonl,
    )
    print(json.dumps(summary, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()


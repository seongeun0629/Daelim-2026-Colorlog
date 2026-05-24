import argparse
import json
import os
import sys
from pathlib import Path

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

import cv2
import mediapipe as mp
from mediapipe.tasks.python import vision

from core.config import build_landmarker_options
from core.frame_processor import process_frame
from analysis.tone import SkinToneSmoother
from vision.face import detect_face



def parse_args():
    parser = argparse.ArgumentParser(
        description="이미지 폴더의 모든 이미지로 얼굴/조명/피부톤 파이프라인을 점검합니다."
    )
    parser.add_argument(
        "--image-dir",
        default="tests/images",
        help="이미지 폴더 경로 (기본값: tests/images)",
    )
    parser.add_argument(
        "--output-jsonl",
        default="",
        help="이미지별 결과를 JSONL로 저장할 경로 (선택)",
    )
    parser.add_argument(
        "--extensions",
        default="jpg,jpeg,png,JPG,PNG,JPEG",
        help="처리할 이미지 확장자 (쉼표로 구분, 기본값: jpg,jpeg,png,JPG,PNG,JPEG)",
    )
    return parser.parse_args()


def run_image_batch(image_dir, output_jsonl="", extensions="jpg,jpeg,png,JPG,PNG,JPEG"):
    if not os.path.isdir(image_dir):
        raise FileNotFoundError("이미지 폴더를 찾을 수 없습니다: {0}".format(image_dir))

    ext_list = set(extensions.split(","))
    image_files = sorted(
        [
            f
            for f in os.listdir(image_dir)
            if f.split(".")[-1] in ext_list
        ]
    )

    if not image_files:
        raise ValueError("이미지 폴더에 이미지가 없습니다: {0}".format(image_dir))

    smoother = SkinToneSmoother(buffer_size=10)
    results = []

    out_fp = open(output_jsonl, "w", encoding="utf-8") if output_jsonl else None

    try:
        options = build_landmarker_options(for_image=True)
        with vision.FaceLandmarker.create_from_options(options) as landmarker:
            for i, img_name in enumerate(image_files):
                img_path = os.path.join(image_dir, img_name)
                frame = cv2.imread(img_path)

                if frame is None:
                    print("경고: 이미지를 읽을 수 없습니다: {0}".format(img_path))
                    continue

                h, w = frame.shape[:2]
                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

                result = detect_face(landmarker, mp_image, i)
                payload = process_frame(frame, result, i, smoother)
                payload["image_filename"] = img_name
                payload["image_path"] = img_path

                # 디버그: 얼굴 감지 상세 정보
                num_faces = len(result.face_landmarks) if result.face_landmarks else 0
                debug_info = {
                    "image_size": "{0}x{1}".format(w, h),
                    "num_faces_detected": num_faces,
                }
                payload["_debug"] = debug_info

                results.append(payload)
                if out_fp is not None:
                    out_fp.write(json.dumps(payload, ensure_ascii=False) + "\n")

    finally:
        if out_fp is not None:
            out_fp.close()

    total_images = len(results)
    detected_images = sum(1 for r in results if r.get("face_detected"))
    detect_ratio = round(detected_images / total_images, 4) if total_images else 0.0

    lighting_scores = [
        r["lighting"]["score"]
        for r in results
        if r.get("lighting") and r["lighting"].get("score") is not None
    ]
    avg_light = round(sum(lighting_scores) / len(lighting_scores), 2) if lighting_scores else None

    oily_scores = [
        r["oily"]["score"]
        for r in results
        if r.get("oily") and r["oily"].get("score") is not None
    ]
    avg_oily = round(sum(oily_scores) / len(oily_scores), 2) if oily_scores else None
    oily_detected = sum(1 for r in results if r.get("oily") and r["oily"].get("status") == "Oily")

    blue_casts = [
        r["lighting_debug"]["blue_cast"]
        for r in results
        if r.get("lighting_debug") and r["lighting_debug"].get("blue_cast") is not None
    ]
    avg_blue_cast = round(sum(blue_casts) / len(blue_casts), 3) if blue_casts else None

    summary = {
        "image_dir": image_dir,
        "total_images": total_images,
        "face_detected_images": detected_images,
        "face_detect_ratio": detect_ratio,
        "avg_lighting_score": avg_light,
        "avg_oily_score": avg_oily,
        "oily_detected_images": oily_detected,
        "avg_blue_cast": avg_blue_cast,
        "jsonl_output": output_jsonl if output_jsonl else None,
    }
    return summary, results


def main():
    args = parse_args()
    summary, results = run_image_batch(
        image_dir=args.image_dir,
        output_jsonl=args.output_jsonl,
        extensions=args.extensions,
    )

    print("\n" + "=" * 60)
    print("이미지 배치 처리 결과 요약")
    print("=" * 60)
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    print("\n프레임별 상세 결과 ({0}개):".format(len(results)))
    for i, res in enumerate(results):
        light = res.get("lighting")
        light_score = light.get("score") if light else None
        oily = res.get("oily")
        oily_status = oily.get("status") if oily else None
        oily_score = oily.get("score") if oily else None
        light_debug = res.get("lighting_debug")
        blue_cast = light_debug.get("blue_cast") if light_debug else None
        print(
            "  [{0}] {1}: face={2}, light={3}, oily={4}({5}), blue_cast={6}".format(
                i,
                res.get("image_filename", "?"),
                res.get("face_detected"),
                light_score,
                oily_status,
                oily_score,
                blue_cast,
            )
        )


if __name__ == "__main__":
    main()


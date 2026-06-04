#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Personal Color Hybrid Analysis Test
6 images (1.jpg ~ 6.jpeg) analysis using Lab + HSV hybrid method
"""

import sys
import os
from pathlib import Path

PROJECT_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if PROJECT_ROOT not in sys.path:
    sys.path.insert(0, PROJECT_ROOT)

import cv2
import mediapipe as mp
from mediapipe.tasks.python import vision
import numpy as np
import json

from core.config import build_landmarker_options
from vision.face import detect_face
from analysis.tone import get_skin_tone, rgb_to_hsv, get_personal_color_season
from analysis.lighting import rgb_to_lab


def convert_to_serializable(obj):
    """Convert numpy types to JSON serializable types"""
    if isinstance(obj, dict):
        return {k: convert_to_serializable(v) for k, v in obj.items()}
    elif isinstance(obj, (list, tuple)):
        return [convert_to_serializable(item) for item in obj]
    elif isinstance(obj, (np.uint8, np.int32, np.int64, np.integer)):
        return int(obj)
    elif isinstance(obj, (np.float32, np.float64, np.floating)):
        return float(obj)
    else:
        return obj


def analyze_personal_color_image(image_path):
    """Analyze personal color of single image"""
    frame = cv2.imread(image_path)
    if frame is None:
        return {
            "filename": Path(image_path).name,
            "status": "FAILED: Image read error",
            "details": {}
        }

    h, w = frame.shape[:2]

    # Face detection
    rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
    mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)

    options = build_landmarker_options(for_image=True)
    with vision.FaceLandmarker.create_from_options(options) as landmarker:
        result = detect_face(landmarker, mp_image, 0)

    filename = Path(image_path).name

    if not result.face_landmarks:
        return {
            "filename": filename,
            "status": "FAILED: Face not detected",
            "details": {}
        }

    try:
        face_landmarks = result.face_landmarks[0]

        # Extract skin tone
        skin_tone_rgb = get_skin_tone(frame, face_landmarks, w, h)
        if skin_tone_rgb is None:
            return {
                "filename": filename,
                "status": "FAILED: Skin tone extraction failed",
                "details": {}
            }

        r, g, b = int(skin_tone_rgb[0]), int(skin_tone_rgb[1]), int(skin_tone_rgb[2])

        # Convert to Lab
        L, a_val, b_val = rgb_to_lab(r, g, b)

        # Convert to HSV
        h_val, s_val, v_val = rgb_to_hsv((r, g, b))

        # Hybrid personal color classification
        personal = get_personal_color_season(L, a_val, b_val, rgb=(r, g, b))

        return {
            "filename": filename,
            "status": "SUCCESS",
            "rgb": {
                "r": r, "g": g, "b": b,
                "hex": f"#{r:02X}{g:02X}{b:02X}"
            },
            "lab": {
                "L": round(L, 1),
                "a": round(a_val, 1),
                "b": round(b_val, 1),
                "chroma": round(personal["lab"]["chroma"], 1)
            },
            "hsv": {
                "h": round(h_val, 1),
                "s": round(s_val, 1),
                "v": round(v_val, 1)
            },
            "personal_color": personal
        }

    except Exception as e:
        return {
            "filename": filename,
            "status": f"ERROR: {str(e)}",
            "details": {}
        }


def print_summary(results):
    """Print summary table"""
    print("\n" + "="*90)
    print("PERSONAL COLOR HYBRID ANALYSIS RESULTS")
    print("="*90)

    print("\nSUMMARY TABLE:")
    print("-" * 110)
    print(f"{'Image':<15} {'Color (Hex)':<12} {'Personal Color':<35} {'Saturation':<15}")
    print("-" * 110)

    for result in results:
        filename = result["filename"]
        status = result["status"]

        if status == "SUCCESS":
            rgb = result["rgb"]
            pc = result["personal_color"]
            season = pc["season"]
            sat = pc["hsv"]["vividness"]

            print(f"{filename:<15} {rgb['hex']:<12} {season:<35} {sat:<15}")
        else:
            print(f"{filename:<15} {'-':<12} {status:<35} {'-':<15}")

    print("-" * 110)


def print_detailed(results):
    """Print detailed analysis"""
    print("\nDETAILED ANALYSIS:\n")
    for i, result in enumerate(results, 1):
        print(f"{i}. {result['filename']}")
        print(f"   Status: {result['status']}")

        if result["status"] == "SUCCESS":
            print(f"\n   Skin Color:")
            print(f"      RGB: ({result['rgb']['r']}, {result['rgb']['g']}, {result['rgb']['b']}) = {result['rgb']['hex']}")

            print(f"\n   Lab Analysis (Physical):")
            print(f"      L (Lightness): {result['lab']['L']}")
            print(f"      a (Red-Green): {result['lab']['a']}")
            print(f"      b (Yellow-Blue): {result['lab']['b']}")
            print(f"      Chroma: {result['lab']['chroma']}")

            print(f"\n   HSV Analysis (Intuitive):")
            print(f"      H (Hue): {result['hsv']['h']}")
            print(f"      S (Saturation): {result['hsv']['s']}%")
            print(f"      V (Value): {result['hsv']['v']}%")

            pc = result['personal_color']
            print(f"\n   Personal Color Classification:")
            print(f"      Season: {pc['season']}")
            print(f"      Temperature: {pc['temperature']}")
            print(f"      HSV Temperature: {pc['hsv']['temperature_hsv']}")
            print(f"      Vividness: {pc['hsv']['vividness']}")
            print(f"      Brightness: {pc['hsv']['brightness']}")

        print()


def main():
    """Main function"""

    test_images = [
        "tests/images/1.jpg",
        "tests/images/2.PNG",
        "tests/images/3.jpg",
        "tests/images/4.jpg",
        "tests/images/5.jpg",
        "tests/images/6.jpeg",
    ]

    print("\n" + "="*90)
    print("PERSONAL COLOR HYBRID ANALYSIS - Starting...")
    print("="*90)
    print(f"\nAnalyzing {len(test_images)} images using Lab + HSV hybrid method\n")

    results = []
    for i, image_path in enumerate(test_images, 1):
        print(f"[{i}/{len(test_images)}] Analyzing: {image_path}...", end=" ", flush=True)
        result = analyze_personal_color_image(image_path)
        results.append(result)
        if result["status"] == "SUCCESS":
            print("OK")
        else:
            print(f"FAILED ({result['status']})")

    # Print results
    print_summary(results)
    print_detailed(results)

    # Save to JSON
    output_file = "tests/personal_color_analysis.json"
    try:
        serializable_results = convert_to_serializable(results)
        with open(output_file, "w", encoding="utf-8") as f:
            json.dump(serializable_results, f, ensure_ascii=False, indent=2)
        print(f"Results saved: {output_file}")
    except Exception as e:
        print(f"Save failed: {str(e)}")

    # Statistics
    success_count = sum(1 for r in results if r["status"] == "SUCCESS")
    print(f"\nAnalysis Complete: {success_count}/{len(test_images)} successful\n")


if __name__ == "__main__":
    main()


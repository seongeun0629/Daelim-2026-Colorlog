# main.py
import cv2
import mediapipe as mp

from mediapipe.tasks.python import vision

from vision.camera import get_frame
from vision.face import detect_face
from analysis.tone import SkinToneSmoother
from core.config import OUTPUT_INTERVAL_SECONDS, TIMESTAMP_STEP_MS, build_landmarker_options
from core.frame_processor import process_frame
from core.json_output import JsonOutputThrottler


def main():
    options = build_landmarker_options()
    cap = cv2.VideoCapture(0)
    timestamp = 0

    smoother = SkinToneSmoother(buffer_size=10)
    output = JsonOutputThrottler(interval_seconds=OUTPUT_INTERVAL_SECONDS)

    try:
        with vision.FaceLandmarker.create_from_options(options) as landmarker:
            while cap.isOpened():
                ret, frame = get_frame(cap)
                if not ret:
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp += TIMESTAMP_STEP_MS

                result = detect_face(landmarker, mp_image, timestamp)
                frame_data = process_frame(frame, result, timestamp, smoother)
                output.emit(frame_data)
    finally:
        cap.release()

if __name__ == "__main__":
    main()

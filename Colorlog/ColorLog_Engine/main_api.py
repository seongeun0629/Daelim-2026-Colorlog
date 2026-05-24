"""
Flask REST API 서버
C# WPF와 통신하는 엔드포인트 제공
카메라 처리를 별도 스레드에서 실행
"""

import cv2
import mediapipe as mp
import threading
import json
import time
from flask import Flask, jsonify, request
from flask_cors import CORS

from mediapipe.tasks.python import vision
from vision.camera import get_frame
from vision.face import detect_face
from analysis.tone import SkinToneSmoother
from core.config import (
    OUTPUT_INTERVAL_SECONDS,
    TIMESTAMP_STEP_MS,
    build_landmarker_options,
)
from core.frame_processor import process_frame
from core.json_output import JsonOutputThrottler

app = Flask(__name__)
CORS(app)

# 전역 상태 변수
camera_thread = None
is_running = False
current_result = None
result_lock = threading.Lock()

# 설정
CAMERA_INDEX = 0
HOST = "127.0.0.1"
PORT = 5000


def camera_worker():
    """카메라 처리 워커 스레드"""
    global is_running, current_result

    options = build_landmarker_options(for_image=False)
    cap = cv2.VideoCapture(CAMERA_INDEX)

    if not cap.isOpened():
        with result_lock:
            current_result = {"error": "카메라를 열 수 없습니다."}
        return

    timestamp = 0
    smoother = SkinToneSmoother(buffer_size=10)
    output = JsonOutputThrottler(interval_seconds=OUTPUT_INTERVAL_SECONDS)

    try:
        with vision.FaceLandmarker.create_from_options(options) as landmarker:
            while is_running:
                ret, frame = get_frame(cap)
                if not ret:
                    break

                rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
                mp_image = mp.Image(image_format=mp.ImageFormat.SRGB, data=rgb)
                timestamp += TIMESTAMP_STEP_MS

                result = detect_face(landmarker, mp_image, timestamp, use_video_mode=True)
                frame_data = process_frame(frame, result, timestamp, smoother)

                # 결과 갱신
                throttled = output.emit(frame_data)
                if throttled:
                    with result_lock:
                        current_result = throttled

                # CPU 부하 완화
                time.sleep(0.001)

    except Exception as e:
        with result_lock:
            current_result = {"error": str(e)}
    finally:
        cap.release()
        with result_lock:
            is_running = False


# ============================================================
# REST API 엔드포인트
# ============================================================


@app.route("/api/start", methods=["POST"])
def start_camera():
    """카메라 처리 시작"""
    global is_running, camera_thread

    if is_running:
        return jsonify({"status": "already_running", "message": "카메라가 이미 실행 중입니다."}), 200

    is_running = True
    camera_thread = threading.Thread(target=camera_worker, daemon=True)
    camera_thread.start()

    return jsonify({"status": "started", "message": "카메라 처리가 시작되었습니다."}), 200


@app.route("/api/stop", methods=["POST"])
def stop_camera():
    """카메라 처리 중지"""
    global is_running

    if not is_running:
        return jsonify({"status": "not_running", "message": "카메라가 실행 중이 아닙니다."}), 200

    is_running = False
    time.sleep(0.5)  # 스레드 종료 대기

    return jsonify({"status": "stopped", "message": "카메라 처리가 중지되었습니다."}), 200


@app.route("/api/status", methods=["GET"])
def get_status():
    """카메라 처리 상태 조회"""
    return jsonify(
        {
            "is_running": is_running,
            "camera_index": CAMERA_INDEX,
            "host": HOST,
            "port": PORT,
        }
    ), 200


@app.route("/api/result", methods=["GET"])
def get_result():
    """최신 분석 결과 조회 (디버그 모드 지원)"""
    # URL 쿼리 파라미터에서 debug 값을 확인 (예: /api/result?debug=true)
    is_debug = request.args.get('debug', 'false').lower() == 'true'

    with result_lock:
        if current_result is None:
            return jsonify({"message": "아직 결과가 없습니다."}), 200

        # 디버그 모드일 경우 원본(TMI 포함) 그대로 반환
        if is_debug:
            return jsonify(current_result), 200

        # 일반 모드일 경우 핵심 데이터만 필터링하여 반환
        core_data = {
            "timestamp": current_result.get("timestamp", 0),
            "face_detected": current_result.get("face_detected", False),
            "lighting": current_result.get("lighting", {}),
            "oily": current_result.get("oily", {}),
            "skin_tone": current_result.get("skin_tone", {}),
            "personal_color": current_result.get("personal_color", {})
        }

        # 편의를 위해 피부색 Hex 코드 변환 로직 추가 (선택 사항)
        if "r" in core_data["skin_tone"]:
            r, g, b = core_data["skin_tone"]["r"], core_data["skin_tone"]["g"], core_data["skin_tone"]["b"]
            core_data["skin_tone"]["hex"] = f"#{r:02X}{g:02X}{b:02X}"

        return jsonify(core_data), 200


@app.route("/api/config", methods=["GET"])
def get_config():
    """현재 설정 조회"""
    return jsonify(
        {
            "camera_index": CAMERA_INDEX,
            "output_interval_seconds": OUTPUT_INTERVAL_SECONDS,
            "timestamp_step_ms": TIMESTAMP_STEP_MS,
        }
    ), 200


@app.route("/api/health", methods=["GET"])
def health_check():
    """헬스 체크"""
    return jsonify({"status": "ok", "version": "1.0"}), 200


# ============================================================
# 매인
# ============================================================


if __name__ == "__main__":
    import sys

    print("=" * 60)
    print("ColorLog Engine - Flask API 서버")
    print("=" * 60)
    print(f"서버: http://{HOST}:{PORT}")
    print("\n사용 가능한 엔드포인트:")
    print(f"  POST   /api/start     - 카메라 처리 시작")
    print(f"  POST   /api/stop      - 카메라 처리 중지")
    print(f"  GET    /api/status    - 처리 상태 조회")
    print(f"  GET    /api/result    - 최신 결과 조회")
    print(f"  GET    /api/config    - 설정 조회")
    print(f"  GET    /api/health    - 헬스 체크")
    print("\nCtrl+C로 종료\n")

    try:
        app.run(host=HOST, port=PORT, debug=False, use_reloader=False)
    except KeyboardInterrupt:
        print("\n서버 종료 중...")
        sys.exit(0)


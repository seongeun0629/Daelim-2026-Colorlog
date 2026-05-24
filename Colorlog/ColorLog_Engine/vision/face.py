def detect_face(landmarker, mp_image, timestamp=None, use_video_mode=False):
    """
    얼굴 감지.
    - use_video_mode=False (기본): 단일 이미지 모드 (detect) - 배치 이미지용
    - use_video_mode=True: 비디오 프레임 모드 (detect_for_video) - 비디오 연속 처리용
    """
    if use_video_mode and timestamp is not None:
        return landmarker.detect_for_video(mp_image, timestamp)
    else:
        # 단일 이미지 모드: timestamp 무시
        return landmarker.detect(mp_image)


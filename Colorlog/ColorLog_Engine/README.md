# 피부 톤 분석기

얼굴 ROI를 추출하고 조명을 분석하며 피부 톤을 스무딩한 뒤, 앱 연동을 위해 JSON 출력까지 제공하는 OpenCV + MediaPipe 파이프라인입니다.

## 현재 구조

```text
ColorLog_Engine/
  main.py                          # 독립형 웹캠 모드
  main_api.py                      # Flask REST API 서버 (WPF 연동용)
  ColorLogWPFClient.cs             # C# WPF 클라이언트 예제
  requirements.txt
  core/
    config.py
    frame_processor.py
    json_output.py
  analysis/
    lighting.py
    oily.py
    tone.py
  vision/
    camera.py
    face.py
    roi.py
  models/
    face_landmarker.task
  tests/
    smoke_test.py
    image_probe.py
    video_probe.py
    test_api.py                    # API 서버 테스트
```

## 권장 규칙

- 프로젝트 루트 트리(`main.py`, `core/`, `analysis/`, `vision/`, `models/`)를 기준 구조로 유지합니다.
- 생성 산출물과 캐시 파일은 버전 관리에 포함하지 않습니다.

## 빠른 시작

```bash
python -m pip install -r requirements.txt
python main.py
```

## C# WPF와의 통신: Flask REST API

**Python 백엔드를 HTTP REST API로 실행**하여 C# WPF 애플리케이션과 통신할 수 있습니다.

### API 서버 시작

```bash
python main_api.py
```

- 서버: `http://127.0.0.1:5000` // 임시
- 자동 CORS 활성화 (WPF/JavaScript 클라이언트 호환)

### 제공 엔드포인트

#### 1. 카메라 처리 시작
```
POST /api/start
응답: {"status": "started", "message": "카메라 처리가 시작되었습니다."}
```

#### 2. 카메라 처리 중지
```
POST /api/stop
응답: {"status": "stopped", "message": "카메라 처리가 중지되었습니다."}
```

#### 3. 처리 상태 조회
```
GET /api/status
응답: {
  "is_running": true,
  "camera_index": 0,
  "host": "127.0.0.1",
  "port": 5000
}
```

#### 4. 최신 분석 결과 조회
```
GET /api/result
응답: {
  "timestamp": 1000,
  "face_detected": true,
  "lighting": {"status": "normal", "score": 165},
  "lighting_debug": {
    "blue_cast": 0.95,
    "overprocessed_score": 0.15,
    "alpha": 0.64,
    "gain_rgb": [1.05, 1.0, 0.98]
  },
  "oily": {"status": "Not Oily", "score": 28},
  "oily_debug": {
    "tzone_focus": 0.42,
    "bright_density": 0.011,
    "component_count": 2
  },
  "skin_tone": {"r": 195, "g": 150, "b": 140},
  "personal_color": {
    "type": "warm_spring",
    "lab": {"L": 60.5, "a": 15.3, "b": 25.8}
  }
}
```

#### 5. 설정 조회
```
GET /api/config
응답: {
  "camera_index": 0,
  "output_interval_seconds": 1.0,
  "timestamp_step_ms": 33
}
```

#### 6. 헬스 체크
```
GET /api/health
응답: {"status": "ok", "version": "1.0"}
```

### C# WPF 통합 예제

`ColorLogWPFClient.cs`의 `ColorLogApiClient` 클래스를 사용:

```csharp
// 클라이언트 초기화
var client = new ColorLogApiClient("127.0.0.1", 5000);

// 카메라 시작
await client.StartCameraAsync();

// 폴링으로 결과 조회 (100ms 간격 권장)
var result = await client.GetResultAsync();
if (result != null) {
    var faceDetected = result["face_detected"];
    var lightScore = result["lighting"]?["score"];
    var blueCast = result["lighting_debug"]?["blue_cast"];
}

// 카메라 중지
await client.StopCameraAsync();
```

### API 테스트

```bash
python tests/test_api.py
```

예상 결과:
```
테스트 완료: 4/4 성공
```



웹캠 대신 저장된 동영상 파일로 파이프라인을 확인할 수 있습니다.

```bash
python tests/video_probe.py --video sample.mp4 --max-frames 300
python tests/video_probe.py --video sample.mp4 --max-frames 300 --output-jsonl tests/probe_output.jsonl
```

- `--video`: 입력 동영상 파일 경로 (필수)
- `--max-frames`: 처리할 최대 프레임 수
- `--output-jsonl`: 프레임별 결과를 JSONL 파일로 저장

## 이미지 폴더 배치 점검

`tests/images/` 폴더의 모든 이미지로 파이프라인을 점검할 수 있습니다.

```bash
python tests/image_probe.py
python tests/image_probe.py --image-dir tests/images --output-jsonl tests/image_probe_output.jsonl
```

- `--image-dir`: 이미지 폴더 경로 (기본값: tests/images)
- `--output-jsonl`: 이미지별 결과를 JSONL 파일로 저장 (선택)
- `--extensions`: 처리할 확장자 (기본값: jpg,jpeg,png,JPG,PNG,JPEG)

## JSON 출력 형식

- `timestamp`
- `face_detected`
- `lighting` (`status`, `score`)
- `lighting_debug` (`blue_cast`, `overprocessed_score`, `alpha`, `gain_rgb`)
- `oily` (`status`, `score`)
- `oily_debug` (`tzone_focus`, `bright_density`, `component_count`, `overall_std_l` 등)
- `skin_tone` (`r`, `g`, `b`)
- `personal_color` (`type`, `lab`)

## 다음 확장 아이디어

- `analysis/texture.py`에 `scikit-image` 기반 피부 결(텍스처) 특징을 추가합니다.
- `analysis/oily.py`의 점수 기준을 실제 촬영 환경에 맞춰 더 세밀하게 보정합니다.
- 순수 함수(`analyze_lighting`, `rgb_to_lab`, 계절 분류기)에 대한 단위 테스트를 추가합니다.


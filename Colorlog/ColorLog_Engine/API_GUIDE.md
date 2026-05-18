# Flask REST API 서버 실행 및 C# WPF 연동 가이드

## 환경 설정

### 1. 패키지 설치

```bash
python -m pip install -r requirements.txt
```

설치되는 패키지:
- `flask`: REST API 서버
- `flask-cors`: CORS 지원 (WPF/JavaScript 클라이언트 호환)
- `mediapipe`, `opencv-python`: 얼굴 감지 및 영상 처리
- 기타: `numpy`, `scikit-image`

### 2. API 서버 시작

```bash
python main_api.py
```

**출력:**
```
============================================================
ColorLog Engine - Flask API 서버
============================================================
서버: http://127.0.0.1:5000

사용 가능한 엔드포인트:
  POST   /api/start     - 카메라 처리 시작
  POST   /api/stop      - 카메라 처리 중지
  GET    /api/status    - 처리 상태 조회
  GET    /api/result    - 최신 결과 조회
  GET    /api/config    - 설정 조회
  GET    /api/health    - 헬스 체크

Ctrl+C로 종료
```

---

## API 테스트

### Python 테스트 스크립트

```bash
python tests/test_api.py
```

**예상 결과:**
```
✅ API 서버 연결됨

1️⃣  헬스 체크 중...
   ✅ 헬스 체크 성공

2️⃣  설정 조회 중...
   ✅ 설정 조회 성공

3️⃣  상태 조회 중...
   ✅ 상태 조회 성공

4️⃣  카메라 시작 중...
   ✅ 시작 성공
   (3초 후 결과 조회)

6️⃣  카메라 중지 중...
   ✅ 중지 성공

테스트 완료: 4/4 성공
```

### cURL 테스트

```bash
# 헬스 체크
curl http://127.0.0.1:5000/api/health

# 카메라 시작
curl -X POST http://127.0.0.1:5000/api/start

# 상태 조회
curl http://127.0.0.1:5000/api/status

# 최신 결과 조회
curl http://127.0.0.1:5000/api/result

# 카메라 중지
curl -X POST http://127.0.0.1:5000/api/stop
```

---

## C# WPF 통합

### 1. Visual Studio 프로젝트 설정

**NuGet 패키지 설치:**
```
Newtonsoft.Json (JSON 파싱용)
```

### 2. ColorLogWPFClient.cs 추가

프로젝트에 `ColorLogWPFClient.cs`를 복사합니다.

### 3. XAML에 UI 컨트롤 추가

```xml
<Button Click="BtnStart_Click">카메라 시작</Button>
<Button Click="BtnStop_Click">카메라 중지</Button>
<Button Click="BtnGetStatus_Click">상태 조회</Button>

<TextBlock x:Name="LblFaceDetected" Text="얼굴 감지: "></TextBlock>
<TextBlock x:Name="LblLightScore" Text="조명: "></TextBlock>
<TextBlock x:Name="LblBlueCast" Text="청광: "></TextBlock>
```

### 4. CodeBehind 구현 예제

```csharp
private ColorLogApiClient _apiClient;

public MainWindow()
{
    InitializeComponent();
    _apiClient = new ColorLogApiClient("127.0.0.1", 5000);
}

private async void BtnStart_Click(object sender, RoutedEventArgs e)
{
    bool success = await _apiClient.StartCameraAsync();
    if (success)
    {
        // 폴링 시작 (100ms 간격)
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        timer.Tick += async (s, args) =>
        {
            var result = await _apiClient.GetResultAsync();
            if (result != null)
            {
                LblFaceDetected.Text = $"얼굴 감지: {result["face_detected"]}";
                LblLightScore.Text = $"조명: {result["lighting"]?["score"]}";
                LblBlueCast.Text = $"청광: {result["lighting_debug"]?["blue_cast"]}";
            }
        };
        timer.Start();
    }
}

private async void BtnStop_Click(object sender, RoutedEventArgs e)
{
    await _apiClient.StopCameraAsync();
}
```

---

## 아키텍처

```
┌─────────────────────────────┐
│  C# WPF Application         │
│  (Windows UI, 설정 변경)     │
└──────────────┬──────────────┘
               │ HTTP REST (JSON)
               ↓
┌─────────────────────────────┐
│  Python Flask API Server    │
│  (main_api.py)              │
│  - http://127.0.0.1:5000    │
└──────────────┬──────────────┘
               │ 스레드
               ↓
┌─────────────────────────────┐
│  ColorLog Engine Pipeline   │
│  - 웹캠 캡처                 │
│  - 얼굴 감지 (MediaPipe)    │
│  - 조명 분석 (청광 제거)    │
│  - 피부톤 분석              │
│  - 개인 계절 분류           │
└─────────────────────────────┘
```

---

## 성능 고려사항

### 폴링 주기 (WPF에서)
- **권장**: 100ms (10Hz)
  - WPF UI 반응성 좋음
  - Python 백엔드 부하 낮음
  - 실시간 감각 충분함

### 카메라 프레임 처리
- **기본 FPS**: 30 (TIMESTAMP_STEP_MS = 33ms)
- **조명 스코어 출력**: 1초마다 (OUTPUT_INTERVAL_SECONDS = 1.0)
  - 과도한 JSON 생성 방지
  - WPF 네트워크 트래픽 최소화

### 메모리 관리
- `SkinToneSmoother(buffer_size=10)`: 최대 10 프레임 버퍼
- 스레드 안전성: `threading.Lock()` 사용
- 이미지 캐시: 자동 해제 (프레임 처리 후)

---

## 문제 해결

### 1. "카메라를 열 수 없습니다" 에러
```
원인: 웹캠이 없거나 다른 애플리케이션이 독점하고 있음
해결:
  - 웹캠 연결 확인
  - 다른 화상 회의 앱 종료 (Zoom, Teams 등)
```

### 2. "Task is not initialized with the image mode" 에러
```
원인: MediaPipe 모드 설정 오류 (이미지 vs 비디오)
해결: 이미 수정됨 (config.py의 for_image 파라미터)
```

### 3. 결과가 계속 "아직 결과가 없습니다."
```
원인: 카메라 프레임이 throttle 되고 있음 (출력 간격)
해결: OUTPUT_INTERVAL_SECONDS를 낮춤 (예: 0.5로 변경)
```

### 4. WPF에서 CORS 에러
```
원인: Flask CORS 헤더 미설정
해결: 이미 수정됨 (flask_cors CORS(app) 적용)
```

---

## 다음 확장 계획

1. **WebSocket 지원**: 실시간 양방향 통신 (낮은 레이턴시)
2. **설정 동적 변경**: `/api/config/update` POST 엔드포인트
3. **이미지/영상 저장**: `/api/save` 엔드포인트
4. **다중 카메라 지원**: 카메라 인덱스 선택 가능
5. **인증/토큰**: JWT 기반 보안

---

## 라이선스

[프로젝트 라이선스]


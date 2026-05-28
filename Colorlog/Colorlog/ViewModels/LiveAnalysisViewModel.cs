using Colorlog.Helpers;
using Colorlog.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Colorlog.ViewModels
{
    public partial class LiveAnalysisViewModel : ObservableObject, IDisposable
    {
        private readonly PythonEngineService _pythonService;
        private static readonly Brush HealthyStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF16A34A"));
        private static readonly Brush WarningStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B"));
        private static readonly Brush DangerStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC2626"));

        private static class AnalysisProgressStep
        {
            public const double Idle = 18;
            public const double Analyzing = 58;
            public const double Completed = 100;
        }

        [ObservableProperty] private double _analysisProgress = AnalysisProgressStep.Idle;
        [ObservableProperty] private string _analysisStatus = "분석 준비 완료";
        [ObservableProperty] private string _analysisPhase = "캘리브레이션";
        [ObservableProperty] private string _analysisPhaseDetail = "카메라 밝기/노출 점검 중";
        [ObservableProperty] private string _guidanceMessage = "얼굴을 가이드 프레임 안에 맞춘 뒤 진단을 시작하세요.";
        [ObservableProperty] private bool _hasCameraPermission = true;
        [ObservableProperty] private bool _isFaceDetected = true;
        [ObservableProperty] private bool _isLightingGood = true;
        [ObservableProperty] private string _bestType = "분석 중...";
        [ObservableProperty] private string _secondType = "-";
        [ObservableProperty] private string _worstType = "-";
        [ObservableProperty] private string _typeAnalysisNote = "데이터를 모으는 중입니다. 얼굴을 고정한 채 잠시만 기다려주세요.";
        [ObservableProperty] private BitmapSource? _cameraSource;
        [ObservableProperty] private bool _isAnalyzing;

        private readonly SettingsViewModel? _settingsViewModel;
        private VideoCapture? _capture;
        private DispatcherTimer? _cameraTimer;
        private readonly object _captureLock = new();
        private bool _disposed;

        public ObservableCollection<FaceTonePreview> FaceTonePreviews { get; } = new();
        public ObservableCollection<string> PreCheckItems { get; } = new()
        {
            "정면 응시", "마스크/모자 제거", "광원 균일", "거리 30~40cm"
        };

        private readonly LinkedList<string> _resultBuffer = new();
        private const int ResultBufferSize = 50;

        public string AnalysisStageText => $"{AnalysisPhase} · {AnalysisPhaseDetail}";
        public string PreviewBasisText => "수치는 최근 1초 평균값 기준";
        public bool HasBlockingIssue => !HasCameraPermission || !IsFaceDetected;
        public bool HasQualityWarning => !HasBlockingIssue && !IsLightingGood;

        public string BlockingIssueTitle
        {
            get
            {
                if (!HasCameraPermission)
                {
                    return "카메라 권한이 필요합니다";
                }

                if (!IsFaceDetected)
                {
                    return "얼굴이 감지되지 않습니다";
                }

                return "진단 준비가 필요합니다";
            }
        }

        public string BlockingIssueMessage
        {
            get
            {
                if (!HasCameraPermission)
                {
                    return "설정에서 카메라 권한을 허용한 뒤 다시 시도해주세요.";
                }

                if (!IsFaceDetected)
                {
                    return "얼굴을 가이드 중앙에 맞추고 마스크/모자를 제거해주세요.";
                }

                return "진단을 시작하기 전에 입력 상태를 확인해주세요.";
            }
        }

        public string QualityWarningMessage => "조명이 다소 불균일합니다. 얼굴 좌우 밝기를 맞추면 정확도가 올라갑니다.";
        public string CameraStateText => HasCameraPermission ? "카메라 연결됨" : "카메라 권한 필요";
        public Brush CameraStateBrush => HasCameraPermission ? HealthyStateBrush : DangerStateBrush;
        public string FaceStateText => IsFaceDetected ? "얼굴 감지됨" : "얼굴 미감지";
        public Brush FaceStateBrush => IsFaceDetected ? HealthyStateBrush : DangerStateBrush;
        public string LightingStateText => IsLightingGood ? "조명 양호" : "조명 주의";
        public Brush LightingStateBrush => IsLightingGood ? HealthyStateBrush : WarningStateBrush;


        public LiveAnalysisViewModel(PythonEngineService pythonService, SettingsViewModel? settingsViewModel = null)
        {
            _pythonService = pythonService;
            _settingsViewModel = settingsViewModel;

            _pythonService.OnColorDetected += UpdateEngineData;
        }


        //파이썬 데이터 수신 
        private void UpdateEngineData(JObject json)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                // 1. 얼굴 감지 상태
                IsFaceDetected = json["face_detected"]?.Value<bool>() ?? false;

                //2. 조명 상태 연동
                var lightingStatus = json["lighting"]?["status"]?.ToString();
                if (lightingStatus != null)
                    IsLightingGood = lightingStatus == "Good";

                // 3. 얼굴 감지 됐을 떄만 피부톤, 퍼스널컬러 데이터 업데이트
                if (IsFaceDetected)
                {
                    UpdateSkinTonePreviews(json);
                    UpdatePersonalColorBuffer(json);
                }
                else
                {
                    AnalysisStatus = "얼굴 미감지";
                    AnalysisPhase = "대기";
                    AnalysisPhaseDetail = "얼굴을 가이드 프레임 안에 맞춰주세요.";
                    FaceTonePreviews.Clear();
                }

                // 4. 진단 저장 완료 이벤트 (Python 30프레임 집계 후 1회)
                if (json["diagnosis_saved"]?.Value<bool>() == true)
                    HandleDiagnosisSaved(json);

                RaiseStateChanged();
            });
        }

        // 부위별 피부톤 미리보기 갱신
        private void UpdateSkinTonePreviews(JObject json)
        {
            var skin = json["skin_tone"];
            if (skin == null) return;

            FaceTonePreviews.Clear();
            AddTonePreview("이마", skin["forehead"] ?? skin);
            AddTonePreview("왼쪽 볼", skin["left_cheek"] ?? skin);
            AddTonePreview("오른쪽 볼", skin["right_cheek"] ?? skin);
            AddTonePreview("코 주변", skin["nose"] ?? skin);
            AddTonePreview("턱", skin["chin"] ?? skin);

            AnalysisStatus = "실시간 분석 진행 중";
            AnalysisPhase = "피부톤 추출";
            AnalysisPhaseDetail = $"R:{skin["r"]} G:{skin["g"]} B:{skin["b"]} 추출 완료";
        }

        //퍼스널컬러 버퍼 집계 + 가이드 메세지
        private void UpdatePersonalColorBuffer(JObject json)
        {
            var personalColor = json["personal_color"]?["type"]?.ToString();
            if (string.IsNullOrEmpty(personalColor)) return;

            _resultBuffer.AddLast(personalColor);
            if (_resultBuffer.Count > ResultBufferSize)
                _resultBuffer.RemoveFirst();

            var statistics = _resultBuffer
                .GroupBy(x => x)
                .Select(g => new
                {
                    Type = g.Key,
                    Count = g.Count(),
                    Percent = g.Count() / (double)_resultBuffer.Count * 100
                })
                .OrderByDescending(g => g.Count)
                .ToList();

            if (statistics.Count == 0) return;

            BestType = statistics[0].Type;

            if (statistics.Count > 1)
            {
                SecondType = statistics[1].Type;
                TypeAnalysisNote = $"{BestType}({statistics[0].Percent:F0}%)와 {SecondType}({statistics[1].Percent:F0}%)의 특징이 섞여 있습니다.";
            }
            else
            {
                SecondType = _resultBuffer.Count < 10 ? "분석 중..." : "단일 타입";
                TypeAnalysisNote = _resultBuffer.Count < 10
                    ? "정밀 분석을 위해 데이터를 수집하고 있습니다. 얼굴을 계속 고정해주세요."
                    : $"{BestType}의 특징이 매우 확고하여 세컨드 타입의 영향이 적습니다.";
            }

            // GuidanceMessage
            GuidanceMessage = personalColor switch
            {
                var s when s.Contains("봄 웜") || s.Contains("Spring Warm") =>
                    "생기 넘치는 봄 웜톤! 밝고 화사한 코랄과 노란색 계열이 베스트예요. 😊",
                var s when s.Contains("가을 웜") || s.Contains("Autumn Warm") =>
                    "분위기 여신 가을 웜톤! 깊이 있는 베이지와 브라운, 카키색이 정말 잘 어울려요. 🍂",
                var s when s.Contains("여름 쿨") || s.Contains("Summer Cool") =>
                    "청량한 여름 쿨톤! 시원한 블루와 라벤더 컬러로 지적인 이미지를 연출해보세요. ❄️",
                var s when s.Contains("겨울 쿨") || s.Contains("Winter Cool") =>
                    "강렬한 겨울 쿨톤! 블랙 & 화이트나 선명한 레드 컬러로 또렷한 인상을 완성해보세요. 💄",
                _ => "데이터를 정밀 분석 중입니다. 얼굴을 고정하고 잠시만 기다려주세요."
            };
        }

        //진단 저장 완료 -> 파이썬 데이터로 
        private void HandleDiagnosisSaved(JObject json)
        {
            var colorTypeInfo = json["color_type_info"];
            if (colorTypeInfo == null) return;

            var worstColors = colorTypeInfo["worst_colors"]?.ToString();
            var keyword = colorTypeInfo["keyword"]?.ToString();

            if (!string.IsNullOrEmpty(worstColors))
                WorstType = worstColors;

            AnalysisProgress = 100;
            AnalysisPhase = "진단 완료";
            AnalysisPhaseDetail = keyword ?? "퍼스널컬러 분석이 완료되었습니다.";
            AnalysisStatus = $"진단 완료 · {BestType}";

            Debug.WriteLine($"[진단 확정] ID:{json["diagnosis_id"]} / {BestType}");
        }

        // 카메라
        public void InitializeCameraPreview()
        {
            lock (_captureLock)
            {
                ReleaseCameraResourcesInternal();

                var index = _settingsViewModel?.GetSelectedCameraIndex() ?? 0;
                _capture = new VideoCapture(index, VideoCaptureAPIs.DSHOW);
                if (!_capture.IsOpened())
                {
                    HasCameraPermission = false;
                    CameraSource = null;
                    RaiseStateChanged();
                    return;
                }

                HasCameraPermission = true;
                _capture.FrameWidth = 640;
                _capture.FrameHeight = 480;
                RaiseStateChanged();

                _cameraTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _cameraTimer.Tick += OnCameraFrameTick;
                _cameraTimer.Start();
            }
        }

        public void StopCameraPreview()
        {
            lock (_captureLock)
            {
                ReleaseCameraResourcesInternal();
            }

            CameraSource = null;
        }

        public void StopPage()
        {
            if (IsAnalyzing)
            {
                StopAnalysisCore();
            }

            StopCameraPreview();
        }

        private void ReleaseCameraResourcesInternal()
        {
            if (_cameraTimer != null)
            {
                _cameraTimer.Stop();
                _cameraTimer.Tick -= OnCameraFrameTick;
                _cameraTimer = null;
            }

            if (_capture != null)
            {
                _capture.Release();
                _capture.Dispose();
                _capture = null;
            }
        }

        private void OnCameraFrameTick(object? sender, EventArgs e)
        {
            lock (_captureLock)
            {
                if (_capture == null || !_capture.IsOpened())
                {
                    return;
                }

                using var frame = new Mat();
                if (_capture.Read(frame) && !frame.Empty())
                {
                    using var preview = CameraFrameHelper.PreparePreviewFrame(frame, flipHorizontal: true);
                    CameraSource = preview.ToBitmapSource();
                }
            }
        }

        [RelayCommand]
        private void StartAnalysis()
        {
            if (IsAnalyzing)
            {
                return;
            }

            if (_capture == null || !_capture.IsOpened())
            {
                InitializeCameraPreview();
            }

            if (_capture == null || !_capture.IsOpened())
            {
                AnalysisStatus = "카메라 연결 실패";
                return;
            }

            IsAnalyzing = true;
            _pythonService.Start();
            AnalysisStatus = "실시간 분석 진행 중";
            AnalysisProgress = AnalysisProgressStep.Analyzing;
            AnalysisPhase = "피부톤 추출";
            AnalysisPhaseDetail = "부위별 평균 색상과 홍조 지수를 계산 중";
            GuidanceMessage = "좋아요! 얼굴을 고정한 채 2~3초만 유지해주세요.";
        }

        [RelayCommand]
        private void StopAnalysis() => StopAnalysisCore();

        private void StopAnalysisCore()
        {
            IsAnalyzing = false;
            _pythonService.Stop();
            AnalysisStatus = "분석 일시 중지";
            AnalysisProgress = AnalysisProgressStep.Idle;
            AnalysisPhase = "대기";
            AnalysisPhaseDetail = "진단 재시작을 기다리는 중";
            GuidanceMessage = "진단을 다시 시작하면 실시간 색상 추적이 재개됩니다.";
        }

        partial void OnHasCameraPermissionChanged(bool value) => RaiseStateChanged();
        partial void OnIsFaceDetectedChanged(bool value) => RaiseStateChanged();
        partial void OnIsLightingGoodChanged(bool value) => RaiseStateChanged();
        partial void OnAnalysisPhaseChanged(string value) => OnPropertyChanged(nameof(AnalysisStageText));
        partial void OnAnalysisPhaseDetailChanged(string value) => OnPropertyChanged(nameof(AnalysisStageText));

        private void RaiseStateChanged()
        {
            OnPropertyChanged(nameof(HasBlockingIssue));
            OnPropertyChanged(nameof(HasQualityWarning));
            OnPropertyChanged(nameof(BlockingIssueTitle));
            OnPropertyChanged(nameof(BlockingIssueMessage));
            OnPropertyChanged(nameof(QualityWarningMessage));
            OnPropertyChanged(nameof(CameraStateText));
            OnPropertyChanged(nameof(CameraStateBrush));
            OnPropertyChanged(nameof(FaceStateText));
            OnPropertyChanged(nameof(FaceStateBrush));
            OnPropertyChanged(nameof(LightingStateText));
            OnPropertyChanged(nameof(LightingStateBrush));
        }

        private void AddTonePreview(string zoneName, JToken colorData)
        {
            if (colorData == null) return;
            try
            {
                byte r = colorData["r"]?.Value<byte>() ?? 0;
                byte g = colorData["g"]?.Value<byte>() ?? 0;
                byte b = colorData["b"]?.Value<byte>() ?? 0;
                FaceTonePreviews.Add(new FaceTonePreview(zoneName, $"#FF{r:X2}{g:X2}{b:X2}", 95));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"색상 데이터 처리 중 오류: {ex.Message}");
            }
        }

        // 이벤트 핸들러 해제 및 리소스 정리
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _pythonService.OnColorDetected -= UpdateEngineData;
            StopPage();
        }

    }
        
    public sealed class FaceTonePreview
    {
        public string ZoneName { get; }
        public string ColorHex { get; }
        public int ConfidencePercent { get; }
        public Brush ToneBrush { get; }

        public FaceTonePreview(string zoneName, string colorHex, int confidencePercent)
        {
            ZoneName = zoneName;
            ColorHex = colorHex;
            ConfidencePercent = confidencePercent;
            ToneBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorHex));
        }
    }
}

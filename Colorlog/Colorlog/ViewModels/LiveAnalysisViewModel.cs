using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.Windows.Threading;
using Colorlog.Helpers;
using Colorlog.Services;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System;
using System.Windows.Media.Imaging;

namespace Colorlog.ViewModels
{
    public partial class LiveAnalysisViewModel : ObservableObject
    {

        private readonly PythonEngineService _pythonService;
        private static readonly Brush HealthyStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF16A34A"));
        private static readonly Brush WarningStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B"));
        private static readonly Brush DangerStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC2626"));



        [ObservableProperty]
        private double _analysisProgress = 22;

        [ObservableProperty]
        private string _analysisStatus = "분석 준비 완료";

        [ObservableProperty]
        private string _analysisPhase = "캘리브레이션";

        [ObservableProperty]
        private string _analysisPhaseDetail = "카메라 밝기/노출 점검 중";

        [ObservableProperty]
        private string _guidanceMessage = "얼굴을 가이드 프레임 안에 맞춘 뒤 진단을 시작하세요.";

        [ObservableProperty]
        private bool _hasCameraPermission = true;

        [ObservableProperty]
        private bool _isFaceDetected = true;

        [ObservableProperty]
        private bool _isLightingGood = true;

        [ObservableProperty]
        private string _bestType = "분석 중...";

        [ObservableProperty]
        private string _secondType = "-";

        [ObservableProperty]
        private string _worstType = "-";

        [ObservableProperty]
        private string _typeAnalysisNote = "데이터를 모으는 중입니다. 얼굴을 고정한 채 잠시만 기다려주세요.";

        // 카메라: SettingsView와 동일하게 UI 스레드 DispatcherTimer + VideoCapture(DSHOW)
        private readonly SettingsViewModel? _settingsViewModel;
        private VideoCapture? _capture;
        private DispatcherTimer? _cameraTimer;
        private readonly object _captureLock = new();

        [ObservableProperty]
        private BitmapSource? _cameraSource;

        [ObservableProperty]
        private bool _isAnalyzing;


        public ObservableCollection<FaceTonePreview> FaceTonePreviews { get; }
        public ObservableCollection<string> PreCheckItems { get; }

        private readonly Queue<string> _resultBuffer = new Queue<string>();

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

            FaceTonePreviews = new ObservableCollection<FaceTonePreview>();
            PreCheckItems = new ObservableCollection<string> {
                "정면 응시", "마스크/모자 제거", "광원 균일", "거리 30~40cm"
            };
        }

        private void UpdateEngineData(JObject json)
        {
            var personalColor = json["personal_color"]?["type"]?.ToString();
            App.Current.Dispatcher.Invoke(() =>
            {
                IsFaceDetected = json["face_detected"]?.Value<bool>() ?? false;

                if (IsFaceDetected)
                {
                    var skin = json["skin_tone"];
                    if (skin != null)
                    {
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
                }
                else
                {
                    AnalysisStatus = "얼굴 미감지";
                    AnalysisPhase = "대기";
                    AnalysisPhaseDetail = "얼굴을 가이드 프레임 안에 맞춰주세요.";
                    FaceTonePreviews.Clear();
                }

                if (!string.IsNullOrEmpty(personalColor))
                {
                    _resultBuffer.Enqueue(personalColor);
                    if (_resultBuffer.Count > 50) _resultBuffer.Dequeue();

                    var statistics = _resultBuffer.GroupBy(x => x)
                        .Select(g => new { Type = g.Key, Count = g.Count(), Percent = (g.Count() / (double)_resultBuffer.Count) * 100 })
                        .OrderByDescending(g => g.Count)
                        .ToList();

                    if (statistics.Count > 0)
                    {
                        BestType = statistics[0].Type;
                        AnalysisStatus = $"Best: {BestType}";

                        if (statistics.Count > 1)
                        {
                            SecondType = statistics[1].Type;
                            TypeAnalysisNote = $"{BestType}({statistics[0].Percent:F0}%)와 {SecondType}({statistics[1].Percent:F0}%)의 특징이 섞여 있습니다.";
                        }
                        else
                        {
                            if (_resultBuffer.Count < 10)
                            {
                                SecondType = "분석 중...";
                                TypeAnalysisNote = "정밀 분석을 위해 데이터를 수집하고 있습니다. 얼굴을 계속 고정해주세요.";
                            }
                            else
                            {
                                SecondType = "단일 타입";
                                TypeAnalysisNote = $"{BestType}의 특징이 매우 확고하여 세컨드 타입의 영향이 적습니다.";
                            }
                        }

                        if (BestType.Contains("봄") || BestType.Contains("가을") || BestType.Contains("Warm"))
                        {
                            WorstType = "겨울 쿨톤 (Winter Cool)";
                        }
                        else
                        {
                            WorstType = "가을 웜톤 (Autumn Warm)";
                        }
                    }

                    if (personalColor.Contains("봄 웜") || personalColor.Contains("Spring Warm"))
                    {
                        GuidanceMessage = "생기 넘치는 봄 웜톤! 밝고 화사한 코랄과 노란색 계열이 베스트예요. 😊";
                    }
                    else if (personalColor.Contains("봄 라이트") || personalColor.Contains("Spring Light"))
                    {
                        GuidanceMessage = "봄 라이트톤이시네요. 파스텔 톤의 부드러운 색상이 얼굴을 환하게 밝혀줄 거예요.";
                    }
                    else if (personalColor.Contains("여름 쿨") || personalColor.Contains("Summer Cool"))
                    {
                        GuidanceMessage = "청량한 여름 쿨톤! 시원한 블루와 라벤더 컬러로 지적인 이미지를 연출해보세요. ❄️";
                    }
                    else if (personalColor.Contains("여름 라이트") || personalColor.Contains("Summer Light"))
                    {
                        GuidanceMessage = "깨끗한 여름 라이트톤입니다. 밝고 투명한 느낌의 메이크업과 코디를 추천해요.";
                    }
                    else if (personalColor.Contains("가을 웜") || personalColor.Contains("Autumn Warm"))
                    {
                        GuidanceMessage = "분위기 여신 가을 웜톤! 깊이 있는 베이지와 브라운, 카키색이 정말 잘 어울려요. 🍂";
                    }
                    else if (personalColor.Contains("가을 뮤트") || personalColor.Contains("Autumn Mute"))
                    {
                        GuidanceMessage = "차분한 가을 뮤트톤입니다. 회색기가 섞인 오묘한 컬러들이 고급스러움을 더해줍니다.";
                    }
                    else if (personalColor.Contains("겨울 쿨") || personalColor.Contains("Winter Cool"))
                    {
                        GuidanceMessage = "강렬한 겨울 쿨톤! 블랙 & 화이트나 선명한 레드 컬러로 또렷한 인상을 완성해보세요. 💄";
                    }
                    else if (personalColor.Contains("겨울 딥") || personalColor.Contains("Winter Deep"))
                    {
                        GuidanceMessage = "도시적인 겨울 딥톤입니다. 채도가 낮고 깊은 와인, 네이비 컬러가 무게감을 잡아줄 거예요.";
                    }
                    else
                    {
                        GuidanceMessage = "데이터를 정밀 분석 중입니다. 얼굴을 고정하고 잠시만 기다려주세요.";
                    }
                }
                RaiseStateChanged();
            });
        }

        /// <summary>페이지 진입 시 호출. 설정 화면과 동일한 방식으로 미리보기 스트림을 시작합니다.</summary>
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
                RaiseStateChanged();

                _capture.FrameWidth = 640;
                _capture.FrameHeight = 480;

                _cameraTimer = new DispatcherTimer(DispatcherPriority.Render)
                {
                    Interval = TimeSpan.FromMilliseconds(33)
                };
                _cameraTimer.Tick += OnCameraFrameTick;
                _cameraTimer.Start();
            }
        }

        /// <summary>페이지 이탈 시 카메라·타이머 해제.</summary>
        public void StopCameraPreview()
        {
            lock (_captureLock)
            {
                ReleaseCameraResourcesInternal();
            }

            CameraSource = null;
        }

        /// <summary>다른 메뉴로 이동할 때: 분석 중이면 중지 후 카메라 해제.</summary>
        public void StopPage()
        {
            if (IsAnalyzing)
            {
                StopAnalysis();
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
            AnalysisProgress = 58;
            AnalysisPhase = "피부톤 추출";
            AnalysisPhaseDetail = "부위별 평균 색상과 홍조 지수를 계산 중";
            GuidanceMessage = "좋아요! 얼굴을 고정한 채 2~3초만 유지해주세요.";
        }

        [RelayCommand]
        private void StopAnalysis()
        {
            IsAnalyzing = false;
            _pythonService.Stop();

            AnalysisStatus = "분석 일시 중지";
            AnalysisProgress = 18;
            AnalysisPhase = "대기";
            AnalysisPhaseDetail = "진단 재시작을 기다리는 중";
            GuidanceMessage = "진단을 다시 시작하면 실시간 색상 추적이 재개됩니다.";
        }

        partial void OnHasCameraPermissionChanged(bool value)
        {
            RaiseStateChanged();
        }

        partial void OnIsFaceDetectedChanged(bool value)
        {
            RaiseStateChanged();
        }

        partial void OnIsLightingGoodChanged(bool value)
        {
            RaiseStateChanged();
        }

        partial void OnAnalysisPhaseChanged(string value)
        {
            OnPropertyChanged(nameof(AnalysisStageText));
        }

        partial void OnAnalysisPhaseDetailChanged(string value)
        {
            OnPropertyChanged(nameof(AnalysisStageText));
        }

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

                string hex = $"#FF{r:X2}{g:X2}{b:X2}";

                FaceTonePreviews.Add(new FaceTonePreview(zoneName, hex, 95)); // 신뢰도는 우선 95로 고정해두었음 나중에 수정하기 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"색상 데이터 처리 중 오류 발생: {ex.Message}");
            }

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

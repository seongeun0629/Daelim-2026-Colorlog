using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace Colorlog.ViewModels
{
    public partial class LiveAnalysisViewModel : ObservableObject
    {
        private static readonly Brush HealthyStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF16A34A"));
        private static readonly Brush WarningStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFF59E0B"));
        private static readonly Brush DangerStateBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDC2626"));

        [ObservableProperty]
        private bool _isAnalyzing;

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

        public ObservableCollection<FaceTonePreview> FaceTonePreviews { get; }
        public ObservableCollection<string> PreCheckItems { get; }

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

        public LiveAnalysisViewModel()
        {
            FaceTonePreviews = new ObservableCollection<FaceTonePreview>
            {
                new("이마", "#FFD4B09A", 88),
                new("좌측 볼", "#FFD9AA98", 84),
                new("우측 볼", "#FFD8A893", 86),
                new("코 주변", "#FFE4BCA6", 81),
                new("턱", "#FFD1AE98", 83)
            };

            PreCheckItems = new ObservableCollection<string>
            {
                "정면 응시",
                "마스크/모자 제거",
                "광원 균일",
                "거리 30~40cm"
            };
        }

        [RelayCommand]
        private void StartAnalysis()
        {
            IsAnalyzing = true;
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

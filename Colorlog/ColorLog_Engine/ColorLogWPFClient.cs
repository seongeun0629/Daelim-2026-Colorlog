using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json.Linq;

namespace ColorLogWPFClient
{
    /// <summary>
    /// Python Flask API 클라이언트
    /// </summary>
    public class ColorLogApiClient
    {
        private readonly string _baseUrl;
        private readonly HttpClient _httpClient;

        public ColorLogApiClient(string host = "127.0.0.1", int port = 5000)
        {
            _baseUrl = $"http://{host}:{port}/api";
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        }

        /// <summary>
        /// 카메라 처리 시작
        /// </summary>
        public async Task<bool> StartCameraAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/start", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"카메라 시작 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 카메라 처리 중지
        /// </summary>
        public async Task<bool> StopCameraAsync()
        {
            try
            {
                var response = await _httpClient.PostAsync($"{_baseUrl}/stop", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"카메라 중지 실패: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 처리 상태 조회
        /// </summary>
        public async Task<JObject> GetStatusAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/status");
                var json = await response.Content.ReadAsStringAsync();
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"상태 조회 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 최신 분석 결과 조회
        /// </summary>
        public async Task<JObject> GetResultAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/result");
                var json = await response.Content.ReadAsStringAsync();
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                if (!(ex is TaskCanceledException))
                {
                    Console.WriteLine($"결과 조회 실패: {ex.Message}");
                }
                return null;
            }
        }

        /// <summary>
        /// 설정 조회
        /// </summary>
        public async Task<JObject> GetConfigAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/config");
                var json = await response.Content.ReadAsStringAsync();
                return JObject.Parse(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정 조회 실패: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 헬스 체크
        /// </summary>
        public async Task<bool> HealthCheckAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{_baseUrl}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// WPF MainWindow 사용 예제
    /// </summary>
    public partial class MainWindow : Window
    {
        private ColorLogApiClient _apiClient;
        private System.Windows.Threading.DispatcherTimer _pollingTimer;

        public MainWindow()
        {
            InitializeComponent();
            InitializeClient();
        }

        private void InitializeClient()
        {
            _apiClient = new ColorLogApiClient("127.0.0.1", 5000);

            // 폴링 타이머: 100ms마다 결과 조회
            _pollingTimer = new System.Windows.Threading.DispatcherTimer();
            _pollingTimer.Interval = TimeSpan.FromMilliseconds(100);
            _pollingTimer.Tick += async (s, e) =>
            {
                if (await _apiClient.HealthCheckAsync())
                {
                    var result = await _apiClient.GetResultAsync();
                    UpdateUI(result);
                }
            };
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            bool success = await _apiClient.StartCameraAsync();
            if (success)
            {
                MessageBox.Show("카메라 처리가 시작되었습니다.");
                _pollingTimer.Start();
            }
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            _pollingTimer.Stop();
            bool success = await _apiClient.StopCameraAsync();
            if (success)
            {
                MessageBox.Show("카메라 처리가 중지되었습니다.");
            }
        }

        private async void BtnGetStatus_Click(object sender, RoutedEventArgs e)
        {
            var status = await _apiClient.GetStatusAsync();
            if (status != null)
            {
                MessageBox.Show($"상태: {status.ToString()}");
            }
        }

        private void UpdateUI(JObject result)
        {
            if (result == null) return;

            try
            {
                // XAML의 TextBlock, Label 등을 업데이트
                // 예제:
                // LblFaceDetected.Content = result["face_detected"]?.ToString() ?? "N/A";
                // LblLightScore.Content = result["lighting"]?["score"]?.ToString() ?? "N/A";
                // LblBlueCast.Content = result["lighting_debug"]?["blue_cast"]?.ToString() ?? "N/A";

                Console.WriteLine($"결과: {result}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI 업데이트 실패: {ex.Message}");
            }
        }
    }
}


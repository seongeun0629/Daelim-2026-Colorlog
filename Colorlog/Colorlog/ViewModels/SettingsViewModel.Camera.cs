using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;

namespace Colorlog.ViewModels;

public partial class SettingsViewModel
{
    [ObservableProperty]
    private bool isPreviewActive;

    [ObservableProperty]
    private BitmapSource? cameraFeed;

    private VideoCapture? _capture;
    private DispatcherTimer? _timer;
    private readonly object _captureLock = new();

    partial void OnSelectedCameraNameChanged(string? value)
    {
        if (!HasCameras || string.IsNullOrWhiteSpace(value))
        {
            StopCamera();
            IsPreviewActive = false;
            return;
        }

        if (IsPreviewActive)
        {
            _ = SwitchCameraAsync(value);
        }
    }

    partial void OnBrightnessChanged(double value)
    {
        lock (_captureLock)
        {
            if (_capture != null && _capture.IsOpened())
            {
                _capture.Set(VideoCaptureProperties.Brightness, value);
            }
        }
    }

    public int GetSelectedCameraIndex()
    {
        if (string.IsNullOrWhiteSpace(SelectedCameraName))
        {
            return 0;
        }

        var match = Regex.Match(SelectedCameraName, @"\d+");
        return match.Success ? int.Parse(match.Value) : 0;
    }

    public void InitializeCamera(int cameraIndex = -1)
    {
        if (cameraIndex < 0)
        {
            cameraIndex = GetSelectedCameraIndex();
        }

        lock (_captureLock)
        {
            ReleaseCameraResources();

            _capture = new VideoCapture(cameraIndex, VideoCaptureAPIs.DSHOW);
            if (!_capture.IsOpened())
            {
                IsPreviewActive = false;
                return;
            }

            _capture.Set(VideoCaptureProperties.Brightness, Brightness);

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _timer.Tick += OnCameraFrameTick;
            _timer.Start();
            IsPreviewActive = true;
        }
    }

    public void StopCamera()
    {
        lock (_captureLock)
        {
            ReleaseCameraResources();
        }

        IsPreviewActive = false;
        CameraFeed = null;
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
                CameraFeed = frame.ToBitmapSource();
            }
        }
    }

    private void ReleaseCameraResources()
    {
        if (_timer != null)
        {
            _timer.Stop();
            _timer.Tick -= OnCameraFrameTick;
            _timer = null;
        }

        if (_capture != null)
        {
            _capture.Release();
            _capture.Dispose();
            _capture = null;
        }
    }

    private async Task SwitchCameraAsync(string cameraName)
    {
        try
        {
            IsCameraLoading = true;
            lock (_captureLock)
            {
                ReleaseCameraResources();
            }

            await Task.Delay(500);

            var match = Regex.Match(cameraName, @"\d+");
            var cameraIndex = match.Success ? int.Parse(match.Value) : 0;
            InitializeCamera(cameraIndex);
        }
        finally
        {
            IsCameraLoading = false;
        }
    }

    private void DiscoverCameraDevices()
    {
        for (int i = 0; i < 5; i++)
        {
            using var tempCapture = new VideoCapture(i, VideoCaptureAPIs.DSHOW);
            if (tempCapture.IsOpened())
            {
                CameraNames.Add($"카메라 장치 #{i}");
            }
        }
    }

    [RelayCommand]
    private void TogglePreviewClick()
    {
        if (!HasCameras)
        {
            return;
        }

        if (IsPreviewActive)
        {
            StopCamera();
            return;
        }

        InitializeCamera();
    }
}

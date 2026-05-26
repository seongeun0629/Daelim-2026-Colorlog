using OpenCvSharp;

namespace Colorlog.Helpers;

/// <summary>
/// 웹캠 프레임을 미리보기용으로 중앙 크롭·미러·리사이즈합니다.
/// UI의 UniformToFill과 가이드 오버레이 중심이 실제 카메라 중심과 일치하도록 합니다.
/// </summary>
public static class CameraFrameHelper
{
    public const int PreviewWidth = 640;
    public const int PreviewHeight = 480;

    public static readonly double PreviewAspectRatio = PreviewWidth / (double)PreviewHeight;

    /// <summary>
    /// 원본 프레임을 4:3 중앙 크롭 → 좌우 반전(거울) → 640×480 리사이즈.
    /// 호출자는 반환된 Mat을 Dispose해야 합니다.
    /// </summary>
    public static Mat PreparePreviewFrame(Mat source, bool flipHorizontal = true)
    {
        if (source.Empty())
        {
            return source.Clone();
        }

        using var cropped = CenterCropToAspect(source, PreviewAspectRatio);

        Mat working = cropped;
        if (flipHorizontal)
        {
            var flipped = new Mat();
            Cv2.Flip(cropped, flipped, FlipMode.Y);
            working = flipped;
        }

        var output = new Mat();
        Cv2.Resize(
            working,
            output,
            new Size(PreviewWidth, PreviewHeight),
            interpolation: InterpolationFlags.Linear);

        if (flipHorizontal && working != cropped)
        {
            working.Dispose();
        }

        return output;
    }

    /// <summary>가로·세로 비율을 유지한 채 화면 중앙에서 잘라냅니다.</summary>
    public static Mat CenterCropToAspect(Mat source, double targetAspect)
    {
        var w = source.Width;
        var h = source.Height;
        var srcAspect = w / (double)h;

        int cropW;
        int cropH;
        int x;
        int y;

        if (srcAspect > targetAspect)
        {
            cropH = h;
            cropW = (int)Math.Round(h * targetAspect);
            x = (w - cropW) / 2;
            y = 0;
        }
        else
        {
            cropW = w;
            cropH = (int)Math.Round(w / targetAspect);
            x = 0;
            y = (h - cropH) / 2;
        }

        cropW = Math.Clamp(cropW, 1, w - x);
        cropH = Math.Clamp(cropH, 1, h - y);

        using var roi = new Mat(source, new Rect(x, y, cropW, cropH));
        return roi.Clone();
    }
}

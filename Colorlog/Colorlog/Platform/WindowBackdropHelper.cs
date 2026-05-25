using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Colorlog.Platform;

/// <summary>
/// Windows 11 Mica/Acrylic 백드롭 적용. 카드 뒤 데스크톱이 은은하게 비치도록 shell 배경과 함께 사용.
/// </summary>
internal static class WindowBackdropHelper
{
    private const int DwmwaSystemBackdropType = 38;
    private const int DwmwaUseImmersiveDarkMode = 20;

    private const int DwmsbtMainWindow = 2;

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(
        IntPtr hwnd,
        int attribute,
        ref int attributeValue,
        int attributeSize);

    public static void TryApplyMica(Window window)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22000))
        {
            return;
        }

        window.SourceInitialized += (_, _) =>
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var backdrop = DwmsbtMainWindow;
            _ = DwmSetWindowAttribute(handle, DwmwaSystemBackdropType, ref backdrop, sizeof(int));

            var useDarkMode = 0;
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
        };
    }
}

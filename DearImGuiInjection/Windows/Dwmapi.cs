using System;
using System.Runtime.InteropServices;

namespace DearImGuiInjection.Windows;

public static class Dwmapi
{
    [DllImport("dwmapi.dll")]
    public static extern int DwmGetColorizationColor(out uint ColorizationColor, [MarshalAs(UnmanagedType.Bool)] out bool ColorizationOpaqueBlend);

    [DllImport("dwmapi.dll")]
    public static extern int DwmIsCompositionEnabled(out bool enabled);

    [DllImport("dwmapi.dll")]
    public static extern void DwmEnableBlurBehindWindow(IntPtr hwnd, ref DWM_BLURBEHIND blurBehind);

    [StructLayout(LayoutKind.Sequential)]
    public struct DWM_BLURBEHIND
    {
        public DWM_BB dwFlags;
        public bool fEnable;
        public IntPtr hRgnBlur;
        public bool fTransitionOnMaximized;

        public DWM_BLURBEHIND(bool enabled)
        {
            fEnable = enabled;
            hRgnBlur = IntPtr.Zero;
            fTransitionOnMaximized = false;
            dwFlags = DWM_BB.Enable;
        }

        public bool TransitionOnMaximized
        {
            get { return fTransitionOnMaximized; }
            set
            {
                fTransitionOnMaximized = value;
                dwFlags |= DWM_BB.TransitionMaximized;
            }
        }
    }

    [Flags]
    public enum DWM_BB
    {
        Enable = 1,
        BlurRegion = 2,
        TransitionMaximized = 4
    }
}

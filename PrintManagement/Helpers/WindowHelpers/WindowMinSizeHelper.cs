using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;

namespace PrintManagement.Helpers.WindowHelpers
{
    public static class WindowExtensions
    {
        private static readonly System.Collections.Generic.Dictionary<IntPtr, (WinProc Proc, IntPtr OldProc)> _windowProcs
            = new System.Collections.Generic.Dictionary<IntPtr, (WinProc, IntPtr)>();

        public static void SetMinimumSize(this Window window, int minWidth, int minHeight)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (hWnd == IntPtr.Zero) return;

            WinProc newProc = (hWndProc, msg, wParam, lParam) =>
            {
                const uint WM_GETMINMAXINFO = 0x0024;

                if (msg == WM_GETMINMAXINFO && _windowProcs.TryGetValue(hWndProc, out var procs))
                {
                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.X = minWidth;
                    minMaxInfo.ptMinTrackSize.Y = minHeight;
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);
                    return CallWindowProc(procs.OldProc, hWndProc, msg, wParam, lParam);
                }

                if (_windowProcs.TryGetValue(hWndProc, out var currentProcs))
                {
                    return CallWindowProc(currentProcs.OldProc, hWndProc, msg, wParam, lParam);
                }

                return IntPtr.Zero;
            };

            IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newProc);
            IntPtr oldWndProc = IntPtr.Size == 8
                ? SetWindowLongPtr64(hWnd, GWLP_WNDPROC, newWndProcPtr)
                : SetWindowLongPtr32(hWnd, GWLP_WNDPROC, newWndProcPtr);

            _windowProcs[hWnd] = (newProc, oldWndProc);
        }

        #region Win32 P/Invoke

        private delegate IntPtr WinProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        private const int GWLP_WNDPROC = -4;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        #endregion
    }
}

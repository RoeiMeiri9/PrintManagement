using Microsoft.UI.Xaml;
using System;
using System.Runtime.InteropServices;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using static Windows.Win32.PInvoke;

namespace PrintManagement.Helpers.WindowHelpers
{
    public static class WindowExtensions
    {
        private static readonly System.Collections.Generic.Dictionary<IntPtr, (WNDPROC Proc, IntPtr OldProc)> _windowProcs
            = [];

        public static void SetMinimumSize(this Window window, int minWidth, int minHeight)
        {
            IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            if (hWnd == IntPtr.Zero) return;

            LRESULT newProc(HWND hWndProc, uint msg, WPARAM wParam, LPARAM lParam)
            {
                const uint WM_GETMINMAXINFO = 0x0024;
                IntPtr hwndIntPtr = (IntPtr)hWndProc;

                if (msg == WM_GETMINMAXINFO && _windowProcs.TryGetValue(hwndIntPtr, out var procs))
                {
                    MINMAXINFO minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                    minMaxInfo.ptMinTrackSize.X = minWidth;
                    minMaxInfo.ptMinTrackSize.Y = minHeight;
                    Marshal.StructureToPtr(minMaxInfo, lParam, true);

                    var oldProcDelegate = Marshal.GetDelegateForFunctionPointer<WNDPROC>(procs.OldProc);
                    return CallWindowProc(oldProcDelegate, hWndProc, msg, wParam, lParam);
                }

                if (_windowProcs.TryGetValue(hwndIntPtr, out var currentProcs))
                {
                    var oldProcDelegate = Marshal.GetDelegateForFunctionPointer<WNDPROC>(currentProcs.OldProc);
                    return CallWindowProc(oldProcDelegate, hWndProc, msg, wParam, lParam);
                }

                return (LRESULT)0;
            }

            WNDPROC newProcDelegate = new(newProc);
            IntPtr newWndProcPtr = Marshal.GetFunctionPointerForDelegate(newProcDelegate);

            IntPtr oldWndProc = SetWindowLongPtr(
                (HWND)hWnd,
                WINDOW_LONG_PTR_INDEX.GWLP_WNDPROC,
                newWndProcPtr
            );

            _windowProcs[hWnd] = (newProcDelegate, oldWndProc);
        }
    }
}
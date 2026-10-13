using System;
using System.Runtime.InteropServices;

namespace PrintManagement.Helpers.WindowHelpers;

public static partial class SystemCursorLoader
{
    [LibraryImport("user32.dll", EntryPoint = "LoadCursorW", SetLastError = true)]
    public static partial IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

    // 2. יצירת מחרוזת WinRT דרך LibraryImport
    [LibraryImport("combase.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
    public static partial int WindowsCreateString(
        [MarshalAs(UnmanagedType.LPWStr)] string sourceString,
        int length,
        out IntPtr @string);

    // 3. מחיקת מחרוזת WinRT דרך LibraryImport
    [LibraryImport("combase.dll", SetLastError = true)]
    public static partial int WindowsDeleteString(IntPtr @string);

    // 4. שליפת מפעל הפעלה (Activation Factory) דרך LibraryImport
    [LibraryImport("combase.dll", SetLastError = true)]
    public static partial int RoGetActivationFactory(
        IntPtr activatableClassId,
        ref Guid iid,
        out IntPtr factory);


    [ComImport]
    [Guid("ac6f5065-90c4-46ce-beb7-05e138e54117")] // still worth verifying against the SDK header
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IInputCursorStaticsInterop
    {
        // IInspectable slots (must be present, in this order)
        void GetIids(out int iidCount, out IntPtr iids);
        void GetRuntimeClassName(out IntPtr className);
        void GetTrustLevel(out int trustLevel);

        [PreserveSig]
        int CreateFromHCursor(IntPtr hcursor, out IntPtr cursorAbi);
    }
}
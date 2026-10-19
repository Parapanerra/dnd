#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

// The same Windows common file dialog that NativeGallery uses in the Unity Editor.
// NativeGallery's player implementation only supports Android and iOS.
internal static class WindowsNativeImagePicker
{
    private const int FileBufferCharacters = 32768;
    private const int OfnExplorer = 0x00080000;
    private const int OfnFileMustExist = 0x00001000;
    private const int OfnPathMustExist = 0x00000800;
    private const int OfnNoChangeDir = 0x00000008;
    private const int OfnHideReadOnly = 0x00000004;
    private const int OfnAllowMultiSelect = 0x00000200;

    [StructLayout(LayoutKind.Sequential)]
    private struct OpenFileName
    {
        public int structSize;
        public IntPtr owner;
        public IntPtr instance;
        public IntPtr filter;
        public IntPtr customFilter;
        public int maxCustomFilter;
        public int filterIndex;
        public IntPtr file;
        public int maxFile;
        public IntPtr fileTitle;
        public int maxFileTitle;
        public IntPtr initialDirectory;
        public IntPtr title;
        public int flags;
        public short fileOffset;
        public short fileExtension;
        public IntPtr defaultExtension;
        public IntPtr customData;
        public IntPtr hook;
        public IntPtr templateName;
        public IntPtr reserved;
        public int reservedDword;
        public int flagsEx;
    }

    [DllImport("comdlg32.dll", EntryPoint = "GetOpenFileNameW", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetOpenFileName(ref OpenFileName dialog);

    [DllImport("comdlg32.dll", EntryPoint = "CommDlgExtendedError")]
    private static extern int GetDialogError();

    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    internal static string Pick(string title)
    {
        string[] paths = PickMany(title);
        return paths.Length > 0 ? paths[0] : null;
    }

    internal static string[] PickMany(string title)
    {
        IntPtr filter = IntPtr.Zero;
        IntPtr file = IntPtr.Zero;
        IntPtr initialDirectory = IntPtr.Zero;
        IntPtr dialogTitle = IntPtr.Zero;
        try
        {
            filter = Marshal.StringToHGlobalUni("Зображення (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg\0Усі файли (*.*)\0*.*\0\0");
            file = Marshal.AllocHGlobal(FileBufferCharacters * sizeof(char));
            Marshal.WriteInt16(file, 0);
            string pictures = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (!string.IsNullOrEmpty(pictures))
                initialDirectory = Marshal.StringToHGlobalUni(pictures);
            dialogTitle = Marshal.StringToHGlobalUni(string.IsNullOrWhiteSpace(title) ? "Виберіть зображення" : title);

            IntPtr owner = GetActiveWindow();
            if (owner == IntPtr.Zero)
                owner = GetForegroundWindow();
            var dialog = new OpenFileName
            {
                structSize = Marshal.SizeOf(typeof(OpenFileName)),
                owner = owner,
                filter = filter,
                filterIndex = 1,
                file = file,
                maxFile = FileBufferCharacters,
                initialDirectory = initialDirectory,
                title = dialogTitle,
                flags = OfnExplorer | OfnFileMustExist | OfnPathMustExist | OfnNoChangeDir |
                    OfnHideReadOnly | OfnAllowMultiSelect
            };
            if (GetOpenFileName(ref dialog))
            {
                string selected = Marshal.PtrToStringUni(file, FileBufferCharacters);
                int end = selected.IndexOf("\0\0", StringComparison.Ordinal);
                if (end >= 0)
                    selected = selected.Substring(0, end);
                string[] parts = selected.Split(new[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length <= 1)
                    return parts;
                var paths = new string[parts.Length - 1];
                for (int i = 1; i < parts.Length; i++)
                    paths[i - 1] = Path.Combine(parts[0], parts[i]);
                return paths;
            }

            int error = GetDialogError();
            if (error != 0)
                throw new Win32Exception(error, "Не вдалося відкрити вікно вибору зображення");
            return Array.Empty<string>(); // Cancelled by the user.
        }
        finally
        {
            if (filter != IntPtr.Zero) Marshal.FreeHGlobal(filter);
            if (file != IntPtr.Zero) Marshal.FreeHGlobal(file);
            if (initialDirectory != IntPtr.Zero) Marshal.FreeHGlobal(initialDirectory);
            if (dialogTitle != IntPtr.Zero) Marshal.FreeHGlobal(dialogTitle);
        }
    }
}
#endif

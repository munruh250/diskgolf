#if UNITY_STANDALONE_WIN
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DiskGolf.CourseEditor.Platform
{
    public static class WindowsFileDialog
    {
        const int MaxPath = 260;
        const string Filter = "JSON files (*.json)\0*.json\0All files (*.*)\0*.*\0\0";

        const int OFN_EXPLORER = 0x00080000;
        const int OFN_FILEMUSTEXIST = 0x00001000;
        const int OFN_PATHMUSTEXIST = 0x00000800;
        const int OFN_OVERWRITEPROMPT = 0x00000002;
        const int OFN_NOCHANGEDIR = 0x00000008;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        struct OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner;
            public IntPtr hInstance;
            public string lpstrFilter;
            public IntPtr lpCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public StringBuilder lpstrFile;
            public int nMaxFile;
            public StringBuilder lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData;
            public IntPtr lpfnHook;
            public string lpTemplateName;
            public IntPtr pvReserved;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetOpenFileName(ref OpenFileName ofn);

        [DllImport("comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool GetSaveFileName(ref OpenFileName ofn);

        public static bool TryOpenJsonFile(out string path)
        {
            path = null;
            var fileBuffer = new StringBuilder(MaxPath);

            var ofn = new OpenFileName
            {
                lStructSize = Marshal.SizeOf<OpenFileName>(),
                lpstrFilter = Filter,
                lpstrFile = fileBuffer,
                nMaxFile = MaxPath,
                lpstrTitle = "Import hole JSON",
                Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_PATHMUSTEXIST | OFN_NOCHANGEDIR,
                lpstrDefExt = "json"
            };

            if (!GetOpenFileName(ref ofn))
                return false;

            path = fileBuffer.ToString();
            return !string.IsNullOrEmpty(path);
        }

        public static bool TrySaveJsonFile(string suggestedName, out string path)
        {
            path = null;
            var fileBuffer = new StringBuilder(MaxPath);

            if (!string.IsNullOrWhiteSpace(suggestedName))
            {
                string initial = suggestedName.Trim();
                if (!initial.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    initial += ".json";
                fileBuffer.Append(initial);
            }

            var ofn = new OpenFileName
            {
                lStructSize = Marshal.SizeOf<OpenFileName>(),
                lpstrFilter = Filter,
                lpstrFile = fileBuffer,
                nMaxFile = MaxPath,
                lpstrTitle = "Export hole JSON",
                Flags = OFN_EXPLORER | OFN_PATHMUSTEXIST | OFN_OVERWRITEPROMPT | OFN_NOCHANGEDIR,
                lpstrDefExt = "json"
            };

            if (!GetSaveFileName(ref ofn))
                return false;

            path = fileBuffer.ToString();
            if (string.IsNullOrEmpty(path))
                return false;

            if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                path += ".json";

            return true;
        }
    }
}
#else
namespace DiskGolf.CourseEditor.Platform
{
    public static class WindowsFileDialog
    {
        public static bool TryOpenJsonFile(out string path)
        {
            path = null;
            return false;
        }

        public static bool TrySaveJsonFile(string suggestedName, out string path)
        {
            path = null;
            return false;
        }
    }
}
#endif

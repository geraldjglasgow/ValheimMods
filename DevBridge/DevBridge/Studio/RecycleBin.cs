using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DevBridge.Server;

namespace DevBridge.Studio
{
    /// <summary>Files moved to the Windows Recycle Bin (the shell's own delete, undoable from the bin), with no dialog.</summary>
    internal static class RecycleBin
    {
        private const uint Delete = 3;
        private const ushort AllowUndo = 0x40, NoConfirmation = 0x10, Silent = 0x4, NoErrorUi = 0x400;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct FileOperation
        {
            public IntPtr Window;
            public uint Function;
            public string From;
            public string To;
            public ushort Flags;
            public bool Aborted;
            public IntPtr NameMappings;
            public string ProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHFileOperation(ref FileOperation operation);

        internal static void Send(IEnumerable<string> paths)
        {
            var operation = new FileOperation
            {
                Function = Delete,
                From = string.Join("\0", paths) + "\0\0",
                Flags = AllowUndo | NoConfirmation | Silent | NoErrorUi,
            };
            int code = SHFileOperation(ref operation);
            if (code != 0 || operation.Aborted) throw new BridgeException($"Windows could not move them to the Recycle Bin (code {code})");
        }
    }
}

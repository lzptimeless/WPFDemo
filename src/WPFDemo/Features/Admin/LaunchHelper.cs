using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace WPFFeatures.Admin
{
    internal static class LaunchHelper
    {
        #region native
        private const uint TOKEN_ASSIGN_PRIMARY = 0x0001;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_ADJUST_DEFAULT = 0x0080;
        private const uint TOKEN_ADJUST_SESSIONID = 0x0100;
        private const string SE_INCREASE_QUOTA_NAME = "SeIncreaseQuotaPrivilege";
        private const uint SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int ERROR_SUCCESS = 0;
        private const int ERROR_NEED_ELEVATED = 0x02E4;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint NORMAL_PRIORITY_CLASS = 0x20;
        private const uint CREATE_NO_WINDOW = 0x08000000;

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFOW
        {
            public uint cb;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpReserved;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpDesktop;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved2;
            public IntPtr lpReserved2;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hStdInput;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hStdOutput;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hProcess;
            /// <summary>
            /// 要释放
            /// Need release
            /// </summary>
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        /// <summary>
        /// Contains the security descriptor for an object.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private struct SECURITY_ATTRIBUTES
        {
            /// <summary>The size of the structure, in bytes.</summary>
            public int nLength;
            /// <summary>A pointer to the structure that controls access to the object.</summary>
            public IntPtr lpSecurityDescriptor;
            /// <summary>If this value is TRUE (non-zero), a newly created process inherits the handle of the calling process.</summary>
            public int bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES
        {
            public uint PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
            public LUID_AND_ATTRIBUTES[] Privileges;

            public static TOKEN_PRIVILEGES Create()
            {
                TOKEN_PRIVILEGES obj = new TOKEN_PRIVILEGES();
                obj.Privileges = new LUID_AND_ATTRIBUTES[1];
                obj.PrivilegeCount = 1;
                return obj;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID
        {
            public uint LowPart;
            public int HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID_AND_ATTRIBUTES
        {
            public LUID Luid;
            public uint Attributes;
        }

        private enum SECURITY_IMPERSONATION_LEVEL
        {
            SecurityAnonymous,
            SecurityIdentification,
            SecurityImpersonation,
            SecurityDelegation
        }

        private enum TOKEN_TYPE
        {
            TokenPrimary = 1,
            TokenImpersonation
        }

        /// <summary>
        /// Creates a new process running in the security contexts of the specified user.
        /// </summary>
        /// <param name="hToken">Handle to the primary token of the user to run as.</param>
        /// <param name="lpApplicationName">The name of the application to execute.</param>
        /// <param name="lpCommandLine">The command line of the application to execute.</param>
        /// <param name="lpProcessAttributes">The security attributes of the new process.</param>
        /// <param name="lpThreadAttributes">The security attributes of the new process' primary thread.</param>
        /// <param name="bInheritHandles">True if this process inherits the handles of the calling process; false otherwise.</param>
        /// <param name="dwCreationFlags">Flags controing the priority class of the new process.</param>
        /// <param name="lpEnvironment">The environment block of the new process.  If this parameter is null, the environmennt of the calling process is used.</param>
        /// <param name="lpCurrentDirectory">The fuly-qualifed path of the current directory of the new process.</param>
        /// <param name="lpStartupInfo">The startup info for the new process.</param>
        /// <param name="lpProcessInformation">A pointer to the details of the new process.</param>
        /// <returns>True on success, false otherwise.</returns>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessAsUserW(
            IntPtr hToken,
            string? lpApplicationName,
            string? lpCommandLine,
            ref SECURITY_ATTRIBUTES lpProcessAttributes,
            ref SECURITY_ATTRIBUTES lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string? lpCurrentDirectory,
            ref STARTUPINFOW lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        /// <summary>
        /// TokenHandle需要通过CloseHandle释放
        /// TokenHandle need release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, ref IntPtr TokenHandle);

        /// <summary>
        /// lpStartupInfo里面的Handle要通过CloseHandle释放，lpProcessInformation里面的Handle要通过CloseHandle释放
        /// Handle in lpStartupInfo must release through CloseHandle, Handles in lpProcessInformation must
        /// release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CreateProcessWithTokenW(IntPtr hToken, uint dwLogonFlags,
            string? lpApplicationName, string? lpCommandLine, uint dwCreationFlags, IntPtr lpEnvironment,
            string? lpCurrentDirectory, ref STARTUPINFOW lpStartupInfo, ref PROCESS_INFORMATION lpProcessInformation);

        /// <summary>
        /// 这里返回的Handle是一个伪Handle（常量），用以指代当前进程，不需要释放
        /// The Handle returned is a pseudo Handle(constant), use to indicate a process, no need to release
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool LookupPrivilegeValueW(string? lpSystemName, string lpName, ref LUID lpLuid);

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
            ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetShellWindow();

        /// <summary>
        /// 没有需要释放的东西
        /// Nothing need to release
        /// </summary>
        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, ref uint lpdwProcessId);

        /// <summary>
        /// 返回的Handle需要用CloseHandle释放
        /// The returned Handle need release through CloseHandle
        /// </summary>
        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwProcessId);

        /// <summary>
        /// phNewToken需要用CloseHandle释放
        /// phNewToken need release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DuplicateTokenEx(IntPtr hExistingToken, uint dwDesiredAccess, ref SECURITY_ATTRIBUTES sa,
            SECURITY_IMPERSONATION_LEVEL ImpersonationLevel, TOKEN_TYPE TokenType, ref IntPtr phNewToken);
        #endregion

        #region public methods
        /// <summary>
        /// 以用户权限打开一个文件夹
        /// </summary>
        /// <param name="path">文件夹路径</param>
        public static void OpenFolder(string path)
        {
            // 启动游戏
            STARTUPINFOW si = new STARTUPINFOW
            {
                cb = (uint)Marshal.SizeOf(typeof(STARTUPINFOW))
            };
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);

            bool ret;
            int dwLastErr;

            try
            {
                using (var userIdentity = WindowsIdentity.GetCurrent())
                { 
                    // Run it using explorer
                    ret = CreateProcessAsUserW(
                        userIdentity.Token,
                        null,
                        $"explorer \"{path}\"",
                        ref sa,
                        ref sa,
                        false,
                        NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW,
                        IntPtr.Zero,
                        null,
                        ref si,
                        out pi);

                    if (pi.dwProcessId == 0 && !ret)
                    {
                        dwLastErr = Marshal.GetLastWin32Error();
                        throw new Win32Exception(dwLastErr, "CreateProcessWithTokenW failed: 0x" + dwLastErr.ToString("X"));
                    }
                }
            }
            finally
            {
                if (si.hStdError != IntPtr.Zero) CloseHandle(si.hStdError);
                if (si.hStdInput != IntPtr.Zero) CloseHandle(si.hStdInput);
                if (si.hStdOutput != IntPtr.Zero) CloseHandle(si.hStdOutput);
                if (sa.lpSecurityDescriptor != IntPtr.Zero) CloseHandle(sa.lpSecurityDescriptor);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            }
        }

        /// <summary>
        /// 以用户权限启动一个程序或用操作系统默认浏览器打开一个Url
        /// </summary>
        /// <param name="path">要启动的程序的路径或一个Url</param>
        /// <param name="args">启动参数，可以为null</param>
        public static void LaunchAsUser(string path, string? args)
        {
            // 创建启动游戏的快捷方式
            string? shortcutLocation = GetShortcut(path, args);
            if (string.IsNullOrEmpty(shortcutLocation)) return;

            // 启动游戏
            STARTUPINFOW si = new STARTUPINFOW
            {
                cb = (uint)Marshal.SizeOf(typeof(STARTUPINFOW))
            };
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);

            bool ret;
            int dwLastErr;

            try
            {
                using (var userIdentity = WindowsIdentity.GetCurrent())
                {  
                    // Run it using explorer
                    ret = CreateProcessAsUserW(
                        userIdentity.Token,
                        null,
                        $"explorer \"{shortcutLocation}\"",
                        ref sa,
                        ref sa,
                        false,
                        NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW,
                        IntPtr.Zero,
                        null,
                        ref si,
                        out pi);

                    if (pi.dwProcessId == 0 && !ret)
                    {
                        dwLastErr = Marshal.GetLastWin32Error();
                        throw new Win32Exception(dwLastErr, "CreateProcessWithTokenW failed: 0x" + dwLastErr.ToString("X"));
                    }
                }
            }
            finally
            {
                if (si.hStdError != IntPtr.Zero) CloseHandle(si.hStdError);
                if (si.hStdInput != IntPtr.Zero) CloseHandle(si.hStdInput);
                if (si.hStdOutput != IntPtr.Zero) CloseHandle(si.hStdOutput);
                if (sa.lpSecurityDescriptor != IntPtr.Zero) CloseHandle(sa.lpSecurityDescriptor);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            }
        }

        /// <summary>
        /// 这个方法也是以非 admin 权限运行程序，相比 <see cref="LaunchAsUser"/> 能够返回启动的进程 id，但是可能失败
        /// </summary>
        /// <param name="path">启动程序路径，此路径必须是本地程序路径</param>
        /// <param name="args">启动参数，可以为null</param>
        /// <returns></returns>
        public static int LaunchAsUserUnstable(string path, string? args)
        {
            IntPtr hShellProcess = IntPtr.Zero, hShellProcessToken = IntPtr.Zero, hPrimaryToken = IntPtr.Zero;
            IntPtr hProcessToken = IntPtr.Zero;
            STARTUPINFOW si = new STARTUPINFOW();
            si.cb = (uint)Marshal.SizeOf(typeof(STARTUPINFOW));
            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            SECURITY_ATTRIBUTES sa = new SECURITY_ATTRIBUTES();
            sa.nLength = Marshal.SizeOf(sa);

            // hwnd不需要释放
            // hwnd no need to release
            IntPtr hwnd = IntPtr.Zero;
            uint dwPID = 0;
            bool ret;
            int dwLastErr;

            try
            {
                // 为当前进程开启SE_INCREASE_QUOTA_NAME权限（调用CreateProcessWithTokenW需要这个权限）
                // Enable the SeIncreaseQuotaPrivilege in your current token (call CreateProcessWithTokenW need this privilege)
                if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES, ref hProcessToken))
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new Win32Exception(dwLastErr, "OpenProcessToken failed: 0x" + dwLastErr.ToString("X"));
                }
                else
                {
                    TOKEN_PRIVILEGES tkp = TOKEN_PRIVILEGES.Create();
                    LUID luid = new LUID();
                    if (!LookupPrivilegeValueW(null, SE_INCREASE_QUOTA_NAME, ref luid))
                    {
                        dwLastErr = Marshal.GetLastWin32Error();
                        throw new Win32Exception(dwLastErr, "LookupPrivilegeValueW failed: 0x" + dwLastErr.ToString("X"));
                    }

                    tkp.Privileges[0].Luid = luid;
                    tkp.Privileges[0].Attributes = SE_PRIVILEGE_ENABLED;
                    AdjustTokenPrivileges(hProcessToken, false, ref tkp, 0, IntPtr.Zero, IntPtr.Zero);
                    dwLastErr = Marshal.GetLastWin32Error();
                    if (ERROR_SUCCESS != dwLastErr)
                        throw new Win32Exception(dwLastErr, "AdjustTokenPrivileges failed: 0x" + dwLastErr.ToString("X"));
                }

                // 获取Shell的HWND
                // Get an HWND representing the desktop shell 
                hwnd = GetShellWindow();
                if (IntPtr.Zero == hwnd)
                    throw new ApplicationException("No desktop shell is present");

                // 获取Shell的PID
                // Get the Process ID (PID) of the process associated with that window
                GetWindowThreadProcessId(hwnd, ref dwPID);
                if (0 == dwPID)
                    throw new ApplicationException("Unable to get PID of desktop shell");

                // 打开Shell进程对象
                // Open that process
                hShellProcess = OpenProcess(PROCESS_QUERY_INFORMATION, false, dwPID);
                if (hShellProcess == IntPtr.Zero)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("Can't open desktop shell process", new Win32Exception(dwLastErr));
                }

                // 获取Shell进程的Token
                // Get the access token from that process
                ret = OpenProcessToken(hShellProcess, TOKEN_DUPLICATE, ref hShellProcessToken);
                if (!ret)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new ApplicationException("Can't get process token of desktop shell", new Win32Exception(dwLastErr));
                }

                // 通过Shell进程Token，复制一份Primary Token
                // Make a primary token with that token
                uint dwTokenRights = TOKEN_QUERY | TOKEN_ASSIGN_PRIMARY | TOKEN_DUPLICATE | TOKEN_ADJUST_DEFAULT | TOKEN_ADJUST_SESSIONID;
                ret = DuplicateTokenEx(hShellProcessToken, dwTokenRights, ref sa, SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                    TOKEN_TYPE.TokenPrimary, ref hPrimaryToken);
                if (!ret)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new Win32Exception(dwLastErr, "Can't get primary token: 0x" + dwLastErr.ToString("X"));
                }

                // 用复制得到的Token启动程序
                // Start the new process with that primary token
                string? workDir = Path.GetDirectoryName(path);
                // 需要在启动参数前面加上“启动路径参数”，CreateProcessWithTokenW 会自动将“启动路径参数”剪切掉，如果不加，args中的第一个参数
                // 就会被 CreateProcessWithTokenW 自动剪切掉，造成参数丢失
                string fullArgs = $"\"{path}\"{(string.IsNullOrEmpty(args) ? string.Empty : " " + args)}";
                ret = CreateProcessWithTokenW(hPrimaryToken, 0, path, fullArgs, 0, IntPtr.Zero,
                    workDir, ref si, ref pi);
                if (pi.dwProcessId == 0)
                {
                    dwLastErr = Marshal.GetLastWin32Error();
                    throw new Win32Exception(dwLastErr, "CreateProcessWithTokenW failed: 0x" + dwLastErr.ToString("X"));
                }
            }
            finally
            {
                if (hProcessToken != IntPtr.Zero) CloseHandle(hProcessToken);
                if (hPrimaryToken != IntPtr.Zero) CloseHandle(hPrimaryToken);
                if (hShellProcessToken != IntPtr.Zero) CloseHandle(hShellProcessToken);
                if (hShellProcess != IntPtr.Zero) CloseHandle(hShellProcess);
                if (si.hStdError != IntPtr.Zero) CloseHandle(si.hStdError);
                if (si.hStdInput != IntPtr.Zero) CloseHandle(si.hStdInput);
                if (si.hStdOutput != IntPtr.Zero) CloseHandle(si.hStdOutput);
                if (sa.lpSecurityDescriptor != IntPtr.Zero) CloseHandle(sa.lpSecurityDescriptor);
                if (pi.hThread != IntPtr.Zero) CloseHandle(pi.hThread);
                if (pi.hProcess != IntPtr.Zero) CloseHandle(pi.hProcess);
            }

            return (int)pi.dwProcessId;
        }
        #endregion

        #region private
        /// <summary>
        /// 如果指定路径不是快捷方式则创建一个临时快捷方式，然后返回路径，否则直接返回传入的路径
        /// </summary>
        /// <param name="targetPath">需要创建快捷方式的启动路径</param>
        /// <param name="args">启动参数，可以为null</param>
        /// <returns></returns>
        private static string? GetShortcut(string targetPath, string? args)
        {
            const string urlShortcutExtension = ".url";
            if (!Uri.TryCreate(targetPath, UriKind.RelativeOrAbsolute, out Uri? uri))
            {
                return null;
            }

            if (uri.Scheme == Uri.UriSchemeFile || uri.Scheme == Uri.UriSchemeFtp)
            {
                string extension = Path.GetExtension(targetPath).Trim().ToLowerInvariant();
                // 如果这个文件已经是一个快捷方式，不用创建
                if (extension == urlShortcutExtension || extension == ShellLink.LinkExtention)
                    return targetPath;

                string fileName = Path.GetFileNameWithoutExtension(targetPath);
                string savePath = Path.Combine(Path.GetTempPath(), fileName + ShellLink.LinkExtention);
                using (ShellLink sl = new ShellLink())
                {
                    sl.Target = targetPath;
                    if (!string.IsNullOrEmpty(args))
                        sl.Arguments = args;

                    sl.Save(savePath);
                }

                return savePath;
            }
            else
            {
                string savePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + urlShortcutExtension);
                using (StreamWriter writer = new StreamWriter(savePath, false))
                {
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine("URL=" + targetPath);
                    writer.Flush();
                }
                return savePath;
            }
        }
        #endregion
    }

    /// <summary>
    /// 快捷方式读取和生成接口
    /// </summary>
    internal class ShellLink : IDisposable
    {
        /// <summary>
        /// 快捷方式文件扩展名
        /// </summary>
        public const string LinkExtention = ".lnk";

        #region InterfaceShellLink
        [ComImport()]
        [Guid("0000010C-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersist
        {
            [PreserveSig]
            //[helpstring("Returns the class identifier for the component object")]
            void GetClassID(out Guid pClassID);
        }

        [ComImport()]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPersistFile
        {
            // can't get this to go if I extend IPersist, so put it here:
            [PreserveSig]
            void GetClassID(out Guid pClassID);

            //[helpstring("Checks for changes since last file write")]        
            void IsDirty();

            //[helpstring("Opens the specified file and initializes the object from its contents")]        
            void Load(
                [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                uint dwMode);

            //[helpstring("Saves the object into the specified file")]        
            void Save(
                [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
                [MarshalAs(UnmanagedType.Bool)] bool fRemember);

            //[helpstring("Notifies the object that save is completed")]        
            void SaveCompleted(
                [MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

            //[helpstring("Gets the current name of the file associated with the object")]        
            void GetCurFile(
                [MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [ComImport()]
        [Guid("000214EE-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkA
        {
            //[helpstring("Retrieves the path and filename of a shell link object")]
            void GetPath(
                [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile,
                int cchMaxPath,
                ref WIN32_FIND_DATAA pfd,
                uint fFlags);

            //[helpstring("Retrieves the list of shell link item identifiers")]
            void GetIDList(out IntPtr ppidl);

            //[helpstring("Sets the list of shell link item identifiers")]
            void SetIDList(IntPtr pidl);

            //[helpstring("Retrieves the shell link description string")]
            void GetDescription(
                [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile,
                int cchMaxName);

            //[helpstring("Sets the shell link description string")]
            void SetDescription(
                [MarshalAs(UnmanagedType.LPStr)] string pszName);

            //[helpstring("Retrieves the name of the shell link working directory")]
            void GetWorkingDirectory(
                [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszDir,
                int cchMaxPath);

            //[helpstring("Sets the name of the shell link working directory")]
            void SetWorkingDirectory(
                [MarshalAs(UnmanagedType.LPStr)] string pszDir);

            //[helpstring("Retrieves the shell link command-line arguments")]
            void GetArguments(
                [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszArgs,
                int cchMaxPath);

            //[helpstring("Sets the shell link command-line arguments")]
            void SetArguments(
                [MarshalAs(UnmanagedType.LPStr)] string pszArgs);

            //[propget, helpstring("Retrieves or sets the shell link hot key")]
            void GetHotkey(out short pwHotkey);
            //[propput, helpstring("Retrieves or sets the shell link hot key")]
            void SetHotkey(short pwHotkey);

            //[propget, helpstring("Retrieves or sets the shell link show command")]
            void GetShowCmd(out uint piShowCmd);
            //[propput, helpstring("Retrieves or sets the shell link show command")]
            void SetShowCmd(uint piShowCmd);

            //[helpstring("Retrieves the location (path and index) of the shell link icon")]
            void GetIconLocation(
                [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszIconPath,
                int cchIconPath,
                out int piIcon);

            //[helpstring("Sets the location (path and index) of the shell link icon")]
            void SetIconLocation(
                [MarshalAs(UnmanagedType.LPStr)] string pszIconPath,
                int iIcon);

            //[helpstring("Sets the shell link relative path")]
            void SetRelativePath(
                [MarshalAs(UnmanagedType.LPStr)] string pszPathRel,
                uint dwReserved);

            //[helpstring("Resolves a shell link. The system searches for the shell link object and updates the shelnk path and its list of identifiers (if necessary)")]
            void Resolve(
                IntPtr hWnd,
                uint fFlags);

            //[helpstring("Sets the shell link path and filename")]
            void SetPath(
                [MarshalAs(UnmanagedType.LPStr)] string pszFile);
        }


        [ComImport()]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IShellLinkW
        {
            //[helpstring("Retrieves the path and filename of a shell link object")]
            void GetPath(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
                int cchMaxPath,
                ref WIN32_FIND_DATAW pfd,
                uint fFlags);

            //[helpstring("Retrieves the list of shell link item identifiers")]
            void GetIDList(out IntPtr ppidl);

            //[helpstring("Sets the list of shell link item identifiers")]
            void SetIDList(IntPtr pidl);

            //[helpstring("Retrieves the shell link description string")]
            void GetDescription(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
                int cchMaxName);

            //[helpstring("Sets the shell link description string")]
            void SetDescription(
                [MarshalAs(UnmanagedType.LPWStr)] string pszName);

            //[helpstring("Retrieves the name of the shell link working directory")]
            void GetWorkingDirectory(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
                int cchMaxPath);

            //[helpstring("Sets the name of the shell link working directory")]
            void SetWorkingDirectory(
                [MarshalAs(UnmanagedType.LPWStr)] string pszDir);

            //[helpstring("Retrieves the shell link command-line arguments")]
            void GetArguments(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
                int cchMaxPath);

            //[helpstring("Sets the shell link command-line arguments")]
            void SetArguments(
                [MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

            //[propget, helpstring("Retrieves or sets the shell link hot key")]
            void GetHotkey(out short pwHotkey);
            //[propput, helpstring("Retrieves or sets the shell link hot key")]
            void SetHotkey(short pwHotkey);

            //[propget, helpstring("Retrieves or sets the shell link show command")]
            void GetShowCmd(out uint piShowCmd);
            //[propput, helpstring("Retrieves or sets the shell link show command")]
            void SetShowCmd(uint piShowCmd);

            //[helpstring("Retrieves the location (path and index) of the shell link icon")]
            void GetIconLocation(
                [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
                int cchIconPath,
                out int piIcon);

            //[helpstring("Sets the location (path and index) of the shell link icon")]
            void SetIconLocation(
                [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
                int iIcon);

            //[helpstring("Sets the shell link relative path")]
            void SetRelativePath(
                [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
                uint dwReserved);

            //[helpstring("Resolves a shell link. The system searches for the shell link object and updates the shelnk path and its list of identifiers (if necessary)")]
            void Resolve(
                IntPtr hWnd,
                uint fFlags);

            //[helpstring("Sets the shell link path and filename")]
            void SetPath(
                [MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [Guid("00021401-0000-0000-C000-000000000046")]
        [ClassInterface(ClassInterfaceType.None)]
        [ComImport()]
        private class CShellLink { }
        #endregion

        #region enumAndStructShellLink
        private enum EShellLinkGP : uint
        {
            SLGP_SHORTPATH = 1,
            SLGP_UNCPRIORITY = 2
        }

        [Flags]
        private enum EShowWindowFlags : uint
        {
            SW_HIDE = 0,
            SW_SHOWNORMAL = 1,
            SW_NORMAL = 1,
            SW_SHOWMINIMIZED = 2,
            SW_SHOWMAXIMIZED = 3,
            SW_MAXIMIZE = 3,
            SW_SHOWNOACTIVATE = 4,
            SW_SHOW = 5,
            SW_MINIMIZE = 6,
            SW_SHOWMINNOACTIVE = 7,
            SW_SHOWNA = 8,
            SW_RESTORE = 9,
            SW_SHOWDEFAULT = 10,
            SW_MAX = 10
        }


        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] // MAX_PATH
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0, CharSet = CharSet.Ansi)]
        private struct WIN32_FIND_DATAA
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] // MAX_PATH
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, Size = 0)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        private class UnManagedMethods
        {
            [DllImport("Shell32", CharSet = CharSet.Auto)]
            internal extern static int ExtractIconEx(
                [MarshalAs(UnmanagedType.LPTStr)]
                string lpszFile,
                int nIconIndex,
                IntPtr[] phIconLarge,
                IntPtr[] phIconSmall,
                int nIcons);

            [DllImport("user32")]
            internal static extern int DestroyIcon(IntPtr hIcon);
        }

        [Flags]
        public enum EShellLinkResolveFlags : uint
        {
            /// <summary>
            /// Allow any match during resolution.  Has no effect
            /// on ME/2000 or above, use the other flags instead.
            /// </summary>
            SLR_ANY_MATCH = 0x2,

            /// <summary>
            /// Call the Microsoft Windows Installer. 
            /// </summary>
            SLR_INVOKE_MSI = 0x80,

            SLR_NOLINKINFO = 0x40,

            SLR_NO_UI = 0x1,

            SLR_NO_UI_WITH_MSG_PUMP = 0x101,

            SLR_NOUPDATE = 0x8,

            SLR_NOSEARCH = 0x10,

            SLR_NOTRACK = 0x20,

            SLR_UPDATE = 0x4
        }

        public enum LinkDisplayMode : uint
        {
            edmNormal = EShowWindowFlags.SW_NORMAL,
            edmMinimized = EShowWindowFlags.SW_SHOWMINNOACTIVE,
            edmMaximized = EShowWindowFlags.SW_MAXIMIZE
        }

        #endregion

        #region maincode
        // Use Unicode (W) under NT, otherwise use ANSI
        private IShellLinkW? linkW;
        private IShellLinkA? linkA;

        public ShellLink()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                linkW = (IShellLinkW)new CShellLink();
            }
            else
            {
                linkA = (IShellLinkA)new CShellLink();
            }
        }

        public ShellLink(string linkFile)
            : this()
        {
            Open(linkFile);
        }

        ~ShellLink()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (linkW != null)
            {
                Marshal.ReleaseComObject(linkW);
                linkW = null;
            }
            if (linkA != null)
            {
                Marshal.ReleaseComObject(linkA);
                linkA = null;
            }
        }

        public string Target
        {
            get
            {
                StringBuilder target = new StringBuilder(260, 260);
                if (linkW != null)
                {
                    WIN32_FIND_DATAW fd = new WIN32_FIND_DATAW();
                    linkW.GetPath(target, target.Capacity, ref fd, (uint)EShellLinkGP.SLGP_UNCPRIORITY);
                }
                else if (linkA != null)
                {
                    WIN32_FIND_DATAA fd = new WIN32_FIND_DATAA();
                    linkA.GetPath(target, target.Capacity, ref fd, (uint)EShellLinkGP.SLGP_UNCPRIORITY);
                }

                return target.ToString();
            }
            set
            {
                if (linkW != null)
                {
                    linkW.SetPath(value);
                }
                else if (linkA != null)
                {
                    linkA.SetPath(value);
                }
                else
                    throw new InvalidOperationException("Both linkA and linkW is null.");
            }
        }

        public string WorkingDirectory
        {
            get
            {
                StringBuilder path = new StringBuilder(260, 260);
                if (linkW != null)
                {
                    linkW.GetWorkingDirectory(path, path.Capacity);
                }
                else if (linkA != null)
                {
                    linkA.GetWorkingDirectory(path, path.Capacity);
                }
                return path.ToString();
            }
            set
            {
                if (linkW != null)
                {
                    linkW.SetWorkingDirectory(value);
                }
                else if (linkA != null)
                {
                    linkA.SetWorkingDirectory(value);
                }
                else
                    throw new InvalidOperationException("Both linkA and linkW is null.");
            }
        }

        public string Description
        {
            get
            {
                StringBuilder description = new StringBuilder(1024, 1024);
                if (linkW != null)
                {
                    linkW.GetDescription(description, description.Capacity);
                }
                else if (linkA != null)
                {
                    linkA.GetDescription(description, description.Capacity);
                }
                return description.ToString();
            }
            set
            {
                if (linkW != null)
                {
                    linkW.SetDescription(value);
                }
                else if (linkA != null)
                {
                    linkA.SetDescription(value);
                }
                else
                    throw new InvalidOperationException("Both linkA and linkW is null.");
            }
        }

        public string Arguments
        {
            get
            {
                StringBuilder arguments = new StringBuilder(260, 260);
                if (linkW != null)
                {
                    linkW.GetArguments(arguments, arguments.Capacity);
                }
                else if (linkA != null)
                {
                    linkA.GetArguments(arguments, arguments.Capacity);
                }
                return arguments.ToString();
            }
            set
            {
                if (linkW != null)
                {
                    linkW.SetArguments(value);
                }
                else if (linkA != null)
                {
                    linkA.SetArguments(value);
                }
                else
                    throw new InvalidOperationException("Both linkA and linkW is null.");
            }
        }

        public LinkDisplayMode DisplayMode
        {
            get
            {
                uint cmd = (uint)LinkDisplayMode.edmNormal;
                if (linkW != null)
                {
                    linkW.GetShowCmd(out cmd);
                }
                else if (linkA != null)
                {
                    linkA.GetShowCmd(out cmd);
                }
                return (LinkDisplayMode)cmd;
            }
            set
            {
                if (linkW != null)
                {
                    linkW.SetShowCmd((uint)value);
                }
                else if (linkA != null)
                {
                    linkA.SetShowCmd((uint)value);
                }
                else
                    throw new InvalidOperationException("Both linkA and linkW is null.");
            }
        }

        public void Save(string linkFile)
        {
            // Save the object to disk
            if (linkW != null)
            {
                ((IPersistFile)linkW).Save(linkFile, true);
            }
            else if (linkA != null)
            {
                ((IPersistFile)linkA).Save(linkFile, true);
            }
            else
                throw new InvalidOperationException("Both linkA and linkW is null.");
        }

        public void Open(string linkFile)
        {
            Open(linkFile, IntPtr.Zero, EShellLinkResolveFlags.SLR_ANY_MATCH | EShellLinkResolveFlags.SLR_NO_UI, 1);
        }

        public void Open(string linkFile, IntPtr hWnd, EShellLinkResolveFlags resolveFlags)
        {
            Open(linkFile, hWnd, resolveFlags, 1);
        }

        public void Open(string linkFile, IntPtr hWnd, EShellLinkResolveFlags resolveFlags, ushort timeOut)
        {
            uint flags;

            if ((resolveFlags & EShellLinkResolveFlags.SLR_NO_UI) == EShellLinkResolveFlags.SLR_NO_UI)
            {
                flags = (uint)((int)resolveFlags | timeOut << 16);
            }
            else
            {
                flags = (uint)resolveFlags;
            }

            if (linkW != null)
            {
                ((IPersistFile)linkW).Load(linkFile, 0); //STGM_DIRECT)
                linkW.Resolve(hWnd, flags);
            }
            else if (linkA != null)
            {
                ((IPersistFile)linkA).Load(linkFile, 0); //STGM_DIRECT)
                linkA.Resolve(hWnd, flags);
            }
            else
                throw new InvalidOperationException("Both linkA and linkW is null.");
        }
        #endregion
    }
}

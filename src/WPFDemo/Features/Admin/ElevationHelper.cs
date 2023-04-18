using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace WPFFeatures.Admin
{
    /// <summary>
    /// Some UAC functions
    /// 一些UAC辅助函数
    /// </summary>
    //[SupportedOSPlatform("windows")]
    internal static class ElevationHelper
    {
        #region native
        private static uint STANDARD_RIGHTS_READ = 0x00020000;
        private static uint TOKEN_QUERY = 0x0008;
        private static uint TOKEN_READ = (STANDARD_RIGHTS_READ | TOKEN_QUERY);

        /// <summary>
        /// 这里返回的Handle是一个伪Handle（常量），用以指代当前进程，不需要释放
        /// The Handle returned is a pseudo Handle(constant), use to indicate a process, no need to release
        /// </summary>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentProcess();

        /// <summary>
        /// TokenHandle需要通过CloseHandle释放
        /// TokenHandle need release through CloseHandle
        /// </summary>
        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, UInt32 DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass, IntPtr TokenInformation, UInt32 TokenInformationLength, out UInt32 ReturnLength);

        private enum TOKEN_INFORMATION_CLASS
        {
            TokenUser = 1,
            TokenGroups,
            TokenPrivileges,
            TokenOwner,
            TokenPrimaryGroup,
            TokenDefaultDacl,
            TokenSource,
            TokenType,
            TokenImpersonationLevel,
            TokenStatistics,
            TokenRestrictedSids,
            TokenSessionId,
            TokenGroupsAndPrivileges,
            TokenSessionReference,
            TokenSandBoxInert,
            TokenAuditPolicy,
            TokenOrigin,
            TokenElevationType,
            TokenLinkedToken,
            TokenElevation,
            TokenHasRestrictions,
            TokenAccessInformation,
            TokenVirtualizationAllowed,
            TokenVirtualizationEnabled,
            TokenIntegrityLevel,
            TokenUIAccess,
            TokenMandatoryPolicy,
            TokenLogonSid,
            MaxTokenInfoClass
        }

        private enum TOKEN_ELEVATION_TYPE
        {
            TokenElevationTypeDefault = 1,
            TokenElevationTypeFull,
            TokenElevationTypeLimited
        }

        /// <summary>
        /// Contains values that indicate the type of session information to retrieve in 
        /// a call to the WTSQuerySessionInformation function.
        /// </summary>
        public enum WtsInfoClass
        {
            WTSInitialProgram,
            WTSApplicationName,
            WTSWorkingDirectory,
            WTSOEMId,
            WTSSessionId,
            WTSUserName,
            WTSWinStationName,
            WTSDomainName,
            WTSConnectState,
            WTSClientBuildNumber,
            WTSClientName,
            WTSClientDirectory,
            WTSClientProductId,
            WTSClientHardwareId,
            WTSClientAddress,
            WTSClientDisplay,
            WTSClientProtocolType,
            WTSIdleTime,
            WTSLogonTime,
            WTSIncomingBytes,
            WTSOutgoingBytes,
            WTSIncomingFrames,
            WTSOutgoingFrames,
            WTSClientInfo,
            WTSSessionInfo,
        }

        /// <summary>
        /// Retrieves the Remote Desktop Services session that is currently attached to the 
        /// physical console. The physical console is the monitor, keyboard, and mouse. Note 
        /// that it is not necessary that Remote Desktop Services be running for this function
        /// to succeed.
        /// </summary>
        /// <returns>The session identifier of the session that is attached to the physical console. 
        /// If there is no session attached to the physical console, (for example, if the physical 
        /// console session is in the process of being attached or detached), this function returns
        /// 0xFFFFFFFF.</returns>
        [DllImport("Kernel32.dll", SetLastError = true)]
        private static extern uint WTSGetActiveConsoleSessionId();

        /// <summary>
        /// Retrieves session information for the specified session on the specified Remote Desktop 
        /// Session Host (RD Session Host) server. It can be used to query session information on 
        /// local and remote RD Session Host servers.
        /// </summary>
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern bool WTSQuerySessionInformation(IntPtr hServer, uint sessionId, WtsInfoClass wtsInfoClass, out IntPtr ppBuffer, out int pBytesReturned);

        /// <summary>
        /// Frees memory allocated by a Remote Desktop Services function.
        /// </summary>
        [DllImport("Wtsapi32.dll", SetLastError = true)]
        private static extern void WTSFreeMemory(IntPtr pointer);
        #endregion

        #region fields
        private const string LUARegistryKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Policies\\System";
        private const string LUARegistryValue = "EnableLUA";
        #endregion

        #region public methods
        /// <summary>
        /// 获取进程用户信息，注意这个函数可能失败（例如在Windows10沙盒中执行时）
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static WindowsIdInfo GetWindowsIdInfo()
        {
            string userDomainName;
            string userName;
            bool isSystemUser;
            bool isElevated;

            using (var identity = WindowsIdentity.GetCurrent())
            {
                // 注意这里不会通过Environment.UserName的方式获取用户名，因为这个属性的返回值可能是
                // {PCName}$ 这种格式，其中{PCName}代表计算机名，例如当通过SYSTEM账号运行进程的时候
                userName = GetUserFromUserDomainName(identity.Name);
                userDomainName = identity.Name;

                if (identity.IsSystem)
                {
                    IntPtr buffer;
                    int strLen;
                    uint sessionId = WTSGetActiveConsoleSessionId();
                    if (sessionId == 0xFFFFFFFF)
                        throw new ApplicationException("WTSGetActiveConsoleSessionId failed.", new Win32Exception());

                    if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSUserName, out buffer, out strLen) && strLen > 1)
                    {
                        userName = Marshal.PtrToStringAnsi(buffer)!;
                        WTSFreeMemory(buffer);
                        if (WTSQuerySessionInformation(IntPtr.Zero, sessionId, WtsInfoClass.WTSDomainName, out buffer, out strLen) && strLen > 1)
                        {
                            userDomainName = Marshal.PtrToStringAnsi(buffer) + "\\" + userName;
                            WTSFreeMemory(buffer);
                        }
                    }
                    else
                        throw new ApplicationException("WTSQuerySessionInformation failed.", new Win32Exception());
                }

                isSystemUser = identity.IsSystem;
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                isElevated = principal.IsInRole(WindowsBuiltInRole.Administrator)
                           || principal.IsInRole(0x200); //Domain Administrator
            }

            if (!isElevated && !isSystemUser)
            {
                // 通过LUA Native API验证是否有admin权限，这种方式比WindowsPrincipal.IsInRole更加准确，
                // WindowsPrincipal.IsInRole有些情况下会将实际上有admin权限判断为没有admin权限,
                // 只有Vista以上才支持LUA相关API
                if (IsLUAEnabled())
                {
                    IntPtr tokenHandle = IntPtr.Zero;
                    IntPtr elevationTypePtr = IntPtr.Zero;
                    try
                    {
                        if (!OpenProcessToken(GetCurrentProcess(), TOKEN_READ, out tokenHandle))
                            throw new ApplicationException("Could not get process token.", new Win32Exception());

                        TOKEN_ELEVATION_TYPE elevationResult = TOKEN_ELEVATION_TYPE.TokenElevationTypeDefault;

                        int elevationResultSize = Marshal.SizeOf(Enum.GetUnderlyingType(typeof(TOKEN_ELEVATION_TYPE)));
                        uint returnedSize = 0;
                        elevationTypePtr = Marshal.AllocHGlobal(elevationResultSize);

                        bool success = GetTokenInformation(tokenHandle, TOKEN_INFORMATION_CLASS.TokenElevationType, elevationTypePtr, (uint)elevationResultSize, out returnedSize);
                        if (success)
                        {
                            elevationResult = (TOKEN_ELEVATION_TYPE)Marshal.ReadInt32(elevationTypePtr);
                            isElevated = elevationResult == TOKEN_ELEVATION_TYPE.TokenElevationTypeFull;
                        }
                        else
                            throw new ApplicationException("Unable to determine the current elevation.", new Win32Exception());
                    }
                    finally
                    {
                        if (tokenHandle != IntPtr.Zero) CloseHandle(tokenHandle);
                        if (elevationTypePtr != IntPtr.Zero) Marshal.FreeHGlobal(elevationTypePtr);
                    }
                }
            }

            return new WindowsIdInfo(userDomainName, userName, isElevated, isSystemUser);
        }
        #endregion

        #region private methods
        /// <summary>
        /// 获取用户名，例如从Razer\ABC中获取ABC
        /// </summary>
        /// <param name="udn">域名与用户名的组合</param>
        /// <returns></returns>
        private static string GetUserFromUserDomainName(string udn)
        {
            string[] parts = udn.Split(new char[] { '\\' });
            if (parts.Length == 2)
                return parts[1];
            else
                return udn;
        }

        private static bool IsLUAEnabled()
        {
            try
            {
                using (RegistryKey? uacKey = Registry.LocalMachine.OpenSubKey(LUARegistryKey, false))
                {
                    if (uacKey == null) return false;

                    object? uacValue = uacKey.GetValue(LUARegistryValue);
                    if (uacValue == null)
                        return false;

                    return uacValue.Equals(1);
                }
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }

    internal class WindowsIdInfo
    {
        public WindowsIdInfo(string userDomainName, string userName, bool isElevated, bool isSystemUser)
        {
            UserDomainName = userDomainName;
            UserName = userName;
            IsElevated = isElevated;
            IsSystemUser = isSystemUser;
        }

        public string UserDomainName { get; set; }
        public string UserName { get; set; }
        public bool IsElevated { get; set; }
        public bool IsSystemUser { get; set; }
    }
}

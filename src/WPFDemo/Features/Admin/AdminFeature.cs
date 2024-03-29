﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WPFFeatures.Admin
{
    /// <summary>
    /// 启动程序时，隐式获取高级权限的功能，首次启动还是需要通过UAC获取权限
    /// </summary>
    internal class AdminFeature
    {
        #region fields
        /// <summary>
        /// 本程序的启动路径
        /// </summary>
        private readonly string _launcherPath;
        /// <summary>
        /// 用以实现隐式获取高级权限的计划任务名称
        /// </summary>
        private readonly string _skipUacSchTaskName;
        /// <summary>
        /// 计划任务创建者属性值
        /// </summary>
        private readonly string _taskAuthor;
        /// <summary>
        /// 计划任务描述
        /// </summary>
        private readonly string _taskDescription;
        /// <summary>
        /// 缓存ElevationHelper.GetWindowsIdInfo的结果
        /// </summary>
        private WindowsIdInfo? _windowsIdInfo;
        #endregion

        /// <summary>
        /// 创建对象
        /// </summary>
        /// <param name="launcherPath">本程序的启动路径，默认为Assembly.GetEntryAssembly对应的exe</param>
        /// <param name="skipUacSchTaskName">用以实现隐式获取高级权限的计划任务名称，默认为Assembly.GetEntryAssembly对应的Name</param>
        /// <param name="taskAuthor">计划任务创建者属性值，默认为Assembly.GetEntryAssembly对应的Name</param>
        /// <param name="taskDesc">计划任务描述</param>
        public AdminFeature(string? launcherPath = null, string? skipUacSchTaskName = null, string? taskAuthor = null, string? taskDesc = null)
        {
            Assembly asm = Assembly.GetEntryAssembly()!;
            string appName = asm.GetName().Name!;
            _launcherPath = launcherPath ?? asm.Location;
            if (_launcherPath.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // 注意asm.Location在应用程序为.Net Core类型时路径会以.dll结尾
                _launcherPath = $"{_launcherPath.Substring(0, _launcherPath.Length - 4)}.exe";
            }
            _skipUacSchTaskName = skipUacSchTaskName ?? appName;
            _taskAuthor = taskAuthor ?? appName;
            _taskDescription = taskDesc ?? $"Use for {appName} to skip UAC";
        }

        #region public methods
        /// <summary>
        /// 检测是否拥有admin权限，应该在Application.OnStartup中调用
        /// </summary>
        /// <returns>true：当前实例拥有高级权限，false：当前实例没有高级权限</returns>
        public bool CheckPrivilege()
        {
            var windowsIdInfo = GetWindowsIdInfo(); // 获取本进程权限
            var skipUacSchTaskNameWithUser = GetSkipUACTaskNameWithUser();

            if (!windowsIdInfo.IsElevated || windowsIdInfo.IsSystemUser)
            {
                // 没有 Admin 权限，或上下文账号不对（不能用System），外部应该在接受到false之后调用LaunchNewInstanceWithAdmin
                return false;
            }

            // 添加用 Admin 权限启动程序的计划任务，用以下次启动时调用，为了不影响启动速度用异步调用
            _ = Task.Run(() =>
            {
                try
                {
                    AddSkipUACTaskIfNeed(skipUacSchTaskNameWithUser, windowsIdInfo, _launcherPath);
                }
                catch { }
            });

            return true;
        }

        /// <summary>
        /// 通过 Admin 权限启动新的程序实例
        /// </summary>
        /// <param name="e">Application.OnStartup的参数</param>
        public void LaunchNewInstanceWithAdmin(StartupEventArgs e)
        {
            var windowsIdInfo = GetWindowsIdInfo();
            var skipUacSchTaskNameWithUser = GetSkipUACTaskNameWithUser();

            if (windowsIdInfo.IsElevated && !windowsIdInfo.IsSystemUser)
            {
                // 本程序已经拥有 admin 权限，直接启动新实例
                var psi = new ProcessStartInfo
                {
                    FileName = _launcherPath,
                    Arguments = LaunchParameters.CombineArgs(e.Args)
                };
                try
                {
                    Process.Start(psi);
                }
                catch { }
                return;
            }

            // 尝试通过计划任务跳过UAC启动程序以获得 admin 权限
            bool isSkipUACSuccess = LaunchUseTaskSch(skipUacSchTaskNameWithUser, windowsIdInfo, _launcherPath, e.Args);
            if (!isSkipUACSuccess)
            {
                if (!windowsIdInfo.IsSystemUser)
                {
                    // 通过runas（会弹出UAC窗口）的方式启动admin权限程序, 注意System用户用runas启动的新实列还是System用户，所以System用户状态下没有使用runas的意义
                    // 使用备用方式启动程序以获得 admin 权限
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = _launcherPath,
                        Arguments = LaunchParameters.CombineArgs(e.Args, LaunchParameters.RunAs),// 加上 -runas 参数，以便下次启动判断启动的原因
                        UseShellExecute = true, // 必须显示声明，不然在 .net core app 中为 false，runas 需要 UseShellExecute = true
                        Verb = "runas" // runas代表要向用户请求admin权限
                    };

                    try
                    {
                        Process.Start(psi);
                    }
                    catch { }
                }
                else
                {
                    // 由于当前用户是System用户，只能启动一个普通用户权限的新实例，然后让这个新的实例再次通过runas或TaskSchedular
                    // 来启动新的有admin权限的实例
                    try
                    {
                        LaunchHelper.LaunchAsUserUnstable(_launcherPath, LaunchParameters.CombineArgs(e.Args));
                    }
                    catch
                    {
                        try
                        {
                            LaunchHelper.LaunchAsUser(_launcherPath, LaunchParameters.CombineArgs(e.Args));
                        }
                        catch { }
                    }
                }
            }// if (!isSkipUACSuccess)
        }
        #endregion

        #region private methods
        /// <summary>
        /// 用跳过UAC的计划任务启动新实例，成功返回ture，失败返回false
        /// </summary>
        /// <returns></returns>
        private bool LaunchUseTaskSch(string taskName, WindowsIdInfo windowsIdInfo, string launcherPath, string[] launchArgs)
        {
            if (LaunchParameters.Exist(launchArgs, LaunchParameters.SchRun))
            {
                // 本次启动属于计划任务启动，但仍然没有获得 admin 权限，放弃计划任务启动
                return false;
            }

            try
            {
                // System用户则需要用TaskSchedule的方法来获得用户admin权限，由于可能是首次启动程序还没有TaskSchedule，
                // 所以需要调用AddSkipUACTaskIfNeed
                if (windowsIdInfo.IsSystemUser) AddSkipUACTaskIfNeed(taskName, windowsIdInfo, launcherPath);

                var tmpParams = LaunchParameters.CombineArgs(launchArgs, LaunchParameters.SchRun);
                TaskSchedularHelper.RunSkipUACTask(taskName, launcherPath, tmpParams);
                return true;
            }
            catch { }

            return false;
        }

        private void AddSkipUACTaskIfNeed(string taskName, WindowsIdInfo? windowsIdInfo, string launcherPath)
        {
            if (windowsIdInfo == null) return;

            using (var process = Process.GetCurrentProcess())
            {
                // 检查 admin 权限启动计划任务是否可用，不可用则添加
                if (!TaskSchedularHelper.IsSkipUACTaskEnabled(taskName, launcherPath))
                {
                    TaskSchedularHelper.CreateSkipUACTask(_taskAuthor, taskName, _taskDescription, windowsIdInfo, launcherPath);
                }
            }
        }

        private string GetSkipUACTaskNameWithUser()
        {
            var windowsIdInfo = GetWindowsIdInfo();
            return $"{_skipUacSchTaskName}_{windowsIdInfo.UserName.Trim().ToLowerInvariant()}";
        }

        private WindowsIdInfo GetWindowsIdInfo()
        {
            if (_windowsIdInfo == null)
                _windowsIdInfo = ElevationHelper.GetWindowsIdInfo();

            return _windowsIdInfo;
        }
        #endregion
    }
}

using Microsoft.Win32.TaskScheduler;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WPFFeatures.Admin
{
    internal static class TaskSchedularHelper
    {
        /* Key points:
         * 1.The best schedule task name is "application + current system user name", it is easy
         * to check manually and make user has personal setting respectively.
         * 2.There are some options need change form default value:
         *   -IdleSettings.StopOnIdleEnd = false, no need change it in theory, but change is better
         *   -DisallowStartIfOnBatteries = false, must
         *   -StopIfGoingOnBatteries = false, must
         *   -ExecutionTimeLimit = TimeSpan.Zero, must
         *   -AllowDemandStart = true, must
         *   -AllowHardTerminate = false, no need change it in theory, but change is better
         * 3.The options may be changed by other programs or user itself, we need to ensure the
         * launch path is accurate at least.
         */

        /* 要点:
         * 1.计划任务名最好定义为"程序相关+当前系统用户名"，这样方面查看并且不同系统用户具有不同的配置
         * 2.计划任务默认设置中一些值需要手动修改:
         *   -IdleSettings.StopOnIdleEnd = false 不改也应该没问题，改了更保险
         *   -DisallowStartIfOnBatteries = false 必须
         *   -StopIfGoingOnBatteries = false 必须
         *   -ExecutionTimeLimit = TimeSpan.Zero 必须
         *   -AllowDemandStart = true 必须
         *   -AllowHardTerminate = false 不改也应该没问题，改了更保险
         * 3.计划任务的设置可能被其他程序或用户修改，需要验证启动路径是否是本程序
         */

        #region SkipUAC
        public static bool IsSkipUACTaskEnabled(string taskName, string launchPath)
        {
            string fullPath = Path.GetFullPath(launchPath).ToLowerInvariant();
            using (TaskService ts = new TaskService())
            {
                foreach (var task in ts.RootFolder.Tasks)
                {
                    if (string.Equals(task.Name, taskName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var action in task.Definition.Actions)
                        {
                            var execAction = action as ExecAction;
                            if (execAction != null)
                            {
                                string execPath = Path.GetFullPath(execAction.Path.Trim('"')).ToLowerInvariant();
                                if (string.Equals(fullPath, execPath))
                                    return task.Enabled;
                            }
                        }
                        return false;
                    }
                }
            }
            return false;
        }

        public static void CreateSkipUACTask(string taskAuthor, string taskName, string taskDesc, WindowsIdInfo idInfo, string launchPath)
        {
            if (string.IsNullOrEmpty(launchPath))
                throw new ArgumentException("launchPath", "launchPath can not be null or empty");

            if (!File.Exists(launchPath))
                throw new FileNotFoundException("Launcher can not found: " + launchPath);

            using (TaskService ts = new TaskService())
            {
                TaskDefinition td = ts.NewTask();
                td.RegistrationInfo.Author = taskAuthor;
                td.RegistrationInfo.Description = taskDesc;
                td.RegistrationInfo.Date = DateTime.Now;
                td.RegistrationInfo.Version = new Version(0, 1, 0, 0);

                td.Principal.RunLevel = TaskRunLevel.Highest;

                td.Actions.Add(launchPath, "$(Arg0)", null);

                td.Settings.IdleSettings.StopOnIdleEnd = false;
                td.Settings.DisallowStartIfOnBatteries = false;
                td.Settings.StopIfGoingOnBatteries = false;
                td.Settings.ExecutionTimeLimit = TimeSpan.Zero;
                td.Settings.AllowDemandStart = true;
                td.Settings.AllowHardTerminate = false;
                td.Settings.MultipleInstances = TaskInstancesPolicy.Parallel;
                // CPU 优先级默认是 BelowNormal，这里需要设置为 Normal
                td.Settings.Priority = System.Diagnostics.ProcessPriorityClass.Normal;

                ts.RootFolder.RegisterTaskDefinition(taskName,
                    td,
                    TaskCreation.CreateOrUpdate,
                    idInfo.UserDomainName ?? idInfo.UserName,
                    null,
                    TaskLogonType.InteractiveToken,
                    null);
            }
        }

        public static void DeleteSkipUACTask(string taskName)
        {
            using (TaskService ts = new TaskService())
            {
                ts.RootFolder.DeleteTask(taskName, false);
            }
        }

        public static void RunSkipUACTask(string taskName, string launchPath, string? arugments)
        {
            arugments ??= string.Empty;// Ensure arguments not be null(Task.Run will failed)

            string fullPath = Path.GetFullPath(launchPath).ToLowerInvariant();
            using (TaskService ts = new TaskService())
            {
                foreach (var task in ts.RootFolder.Tasks)
                {
                    if (string.Equals(task.Name, taskName, StringComparison.OrdinalIgnoreCase))
                    {
                        foreach (var action in task.Definition.Actions)
                        {
                            var execAction = action as ExecAction;
                            if (execAction != null)
                            {
                                string execPath = Path.GetFullPath(execAction.Path).ToLowerInvariant();
                                if (string.Equals(fullPath, execPath))
                                {
                                    task.Run(arugments);
                                    return;
                                }
                            }
                        }
                        throw new KeyNotFoundException("Not found skip uac action: " + fullPath);
                    }
                }
            }

            throw new KeyNotFoundException("Not found skip uac task: " + taskName);
        }
        #endregion

        public static void DeleteAllTask(string taskName, string launchPath)
        {
            string fullPath = Path.GetFullPath(launchPath);
            // 获取已经存在的任务的 TaskItem 列表
            using (var ts = new TaskService())
            {
                if (ts.RootFolder != null && ts.RootFolder.Tasks != null)
                {
                    foreach (var task in ts.RootFolder.Tasks.ToArray())
                    {
                        // 名称不符合要求的任务不属于本程序
                        if (!string.Equals(task.Name, taskName, StringComparison.OrdinalIgnoreCase))
                            continue;

                        var action = task.Definition.Actions.FirstOrDefault(a => a is ExecAction) as ExecAction;
                        // 没有 ExeAction 的任务不属于本程序
                        if (action == null)
                            continue;

                        // 启动路径不是 launcher 的任务不属于本程序
                        if (action.Path == null || !action.Path.StartsWith(fullPath, StringComparison.OrdinalIgnoreCase))
                            continue;

                        ts.RootFolder.DeleteTask(task.Name, false);
                    }
                }
            }
        }
    }
}

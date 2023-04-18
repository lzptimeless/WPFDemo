using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFFeatures.Admin
{
    internal static class LaunchParameters
    {
        /// <summary>
        /// 用于设置ProcessStartInfo.Verb以获得admin权限
        /// </summary>
        public const string RunAs = "-runas";
        /// <summary>
        /// 本次启动是通过跳过UAC的计划任务触发的
        /// </summary>
        public const string SchRun = "-schrun";
        /// <summary>
        /// 创建用于获取admin权限的计划任务
        /// </summary>
        public const string CreateSkipUAC = "-createskipuac";
        /// <summary>
        /// 删除用于获取admin权限的计划任务
        /// </summary>
        public const string DeleteSkipUAC = "-deleteskipuac";
        /// <summary>
        /// 删除所有属于本程序的计划任务
        /// </summary>
        public const string DeleteAllSch = "-deleteallsch";

        /// <summary>
        /// 判断命令行参数中是否存在指定参数
        /// </summary>
        /// <param name="args">指定的参数</param>
        /// <param name="checkArg">需要检测的参数</param>
        /// <returns></returns>
        public static bool Exist(string[]? args, string checkArg)
        {
            return args != null && args.Any(a => string.Equals(a, checkArg, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取命令行参数中指定参数的值，本质上时获取指定参数的下一个参数
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <param name="argName">指定的参数</param>
        /// <returns></returns>
        public static string? GetValue(string[]? args, string argName)
        {
            if (args == null || args.Length == 0) return null;

            int index = -1;
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], argName, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index == -1 || index >= args.Length - 1) return null;

            return args[index + 1];
        }

        /// <summary>
        /// 合并多个命令行参数为一个字符串，自动排除重复
        /// </summary>
        /// <param name="args">已有的命令行参数</param>
        /// <param name="appendingArg">待合并的参数</param>
        /// <returns>如果args为空则返回appendingArg，否则返回合并后的字符串</returns>
        public static string? CombineArgs(IEnumerable<string>? args, string? appendingArg = null)
        {
            if (args == null || !args.Any()) return appendingArg;
            if (!string.IsNullOrWhiteSpace(appendingArg) && !args.Contains(appendingArg)) 
                args = args.Append(appendingArg);

            StringBuilder sb = new StringBuilder();
            foreach (var item in args)
            {
                string trimedArg = item.Trim();
                if (trimedArg.Contains(' ')) // 含有空格的参数需要加上引号
                    trimedArg = $"\"{trimedArg}\"";

                if (sb.Length > 0) sb.Append(' ');

                sb.Append(trimedArg);
            }

            return sb.ToString();
        }
    }
}

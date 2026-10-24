using System;
using System.Reflection;
using System.Threading;

namespace PCLUnlock
{
    /// <summary>
    /// 注入入口：等待 PCL 程序集加载后安装各项替换。
    /// </summary>
    public static class Injector
    {
        internal const string TargetName = "Plain Craft Launcher 2";
        internal static Assembly Target;

        public static int Run(string argument)
        {
            string stage = "初始化";
            try
            {
                Target = WaitForAssembly(TargetName, 60000);
                if (Target == null)
                {
                    LogBuffer.Write("加载失败：未找到 PCL 程序集");
                    return 1;
                }
                stage = "下载页";
                HookDownload.Install();
                stage = "整合包";
                HookModpack.Install();
                stage = "资源详情页";
                HookCompDetail.Install();
                stage = "设置页";
                SettingsButton.Install();
                LogBuffer.Write("加载完成");
                return 0;
            }
            catch (Exception ex)
            {
                LogBuffer.Write("加载失败（" + stage + "）：" + ex);
                return 2;
            }
        }

        private static Assembly WaitForAssembly(string name, int timeoutMilliseconds)
        {
            int waited = 0;
            while (waited < timeoutMilliseconds)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.GetName().Name == name)
                        return assembly;
                }
                Thread.Sleep(200);
                waited += 200;
            }
            return null;
        }
    }
}

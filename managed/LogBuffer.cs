using System;
using System.Text;

namespace PCLUnlock
{
    /// <summary>
    /// 进程内的日志缓存，供设置页按钮复制。
    /// </summary>
    internal static class LogBuffer
    {
        private static readonly StringBuilder Builder = new StringBuilder();
        private static readonly object Sync = new object();

        public static void Write(string message)
        {
            lock (Sync)
            {
                Builder.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                Builder.Append(' ');
                Builder.AppendLine(message);
            }
        }

        public static string Text
        {
            get
            {
                lock (Sync)
                    return Builder.ToString();
            }
        }
    }
}

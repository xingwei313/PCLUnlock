using System;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Windows.Threading;

namespace PCLUnlock
{
    /// <summary>
    /// Minecraft 更新提示：恢复下载按钮并保留源码的跳转行为。
    /// </summary>
    internal static class HookUpdateHint
    {
        public static void Install()
        {
            Type type = Injector.Target.GetType("PCL.ModMinecraft", false);
            if (type == null)
                throw new InvalidOperationException("缺少 ModMinecraft");
            MethodInfo target = null;
            foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (method.Name == "McDownloadClientUpdateHint" && method.GetParameters().Length == 2)
                {
                    target = method;
                    break;
                }
            }
            if (target == null)
                throw new InvalidOperationException("缺少 McDownloadClientUpdateHint");
            Native.PatchEntry(target, Native.HookPointer("HintHook", typeof(HookUpdateHint)));
        }

        private static void HintHook(string versionName, object json)
        {
            try
            {
                object version = FindVersion(json, versionName);
                if (version == null)
                    return;
                DateTime time;
                if (!GetReleaseTime(version, out time))
                    return;
                TimeSpan span = time - DateTime.Now;
                string text = "新版本：" + versionName + "\r\n" +
                    (span.TotalDays > 1 ? "更新时间：" + time.ToString() : "更新于：" + FormatTimeSpan(span));
                string button3 = (DateTime.Now - time).TotalHours > 3 ? "更新日志" : "";
                int result = ShowMsgBox(text, "Minecraft 更新提示", "确定", "下载", button3, version);
                if (result == 2)
                    ApplyVersionWaiting(versionName);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("更新提示恢复失败：" + ex);
            }
        }

        private static object FindVersion(object json, string versionName)
        {
            object versions = GetIndex(json, "versions");
            IEnumerable list = versions as IEnumerable;
            if (list == null)
                return null;
            foreach (object token in list)
            {
                object id = GetIndex(token, "id");
                if (id != null && id.ToString() == versionName)
                    return token;
            }
            return null;
        }

        private static object GetIndex(object value, string key)
        {
            if (value == null)
                return null;
            Type type = value.GetType();
            MethodInfo getItem = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(string) }, null);
            if (getItem != null)
                return getItem.Invoke(value, new object[] { key });
            PropertyInfo property = type.GetProperty("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, new Type[] { typeof(string) }, null);
            return property == null ? null : property.GetValue(value, new object[] { key });
        }

        private static bool GetReleaseTime(object version, out DateTime time)
        {
            object release = GetIndex(version, "releaseTime");
            if (release == null)
            {
                time = default(DateTime);
                return false;
            }
            PropertyInfo valueProperty = release.GetType().GetProperty("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            object raw = valueProperty == null ? release : valueProperty.GetValue(release, null);
            if (raw is DateTime)
            {
                time = (DateTime)raw;
                return true;
            }
            return DateTime.TryParse(Convert.ToString(raw), out time);
        }

        private static string FormatTimeSpan(TimeSpan span)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException ex)
                {
                    types = ex.Types;
                }
                foreach (Type type in types)
                {
                    if (type == null || type.Name != "StringUtils")
                        continue;
                    MethodInfo method = type.GetMethod("FormatTimeSpan", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
                        new Type[] { typeof(TimeSpan), typeof(bool) }, null);
                    if (method != null)
                        return (string)method.Invoke(null, new object[] { span, false });
                }
            }
            return span.ToString();
        }

        private static int ShowMsgBox(string text, string title, string button1, string button2, string button3, object version)
        {
            Type type = Injector.Target.GetType("PCL.ModMain", true);
            MethodInfo method = null;
            foreach (MethodInfo candidate in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (candidate.Name == "MyMsgBox" && candidate.GetParameters().Length == 11)
                {
                    method = candidate;
                    break;
                }
            }
            if (method == null)
                return 0;

            Action button3Action = null;
            if (button3.Length > 0)
            {
                Type lib = Injector.Target.GetType("PCL.ModDownloadLib", false);
                if (lib != null)
                {
                    foreach (MethodInfo candidate in lib.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (candidate.Name == "McUpdateLogShow" && candidate.GetParameters().Length == 1)
                        {
                            ParameterInfo parameter = candidate.GetParameters()[0];
                            Expression call = Expression.Call(candidate, Expression.Constant(version, parameter.ParameterType));
                            button3Action = Expression.Lambda<Action>(call).Compile();
                            break;
                        }
                    }
                }
            }
            return (int)method.Invoke(null, new object[] { text, title, button1, button2, button3, false, true, true, null, null, button3Action });
        }

        private static void ApplyVersionWaiting(string versionName)
        {
            Type install = Injector.Target.GetType("PCL.PageDownloadInstall", true);
            FieldInfo field = install.GetField("VersionWaitingSelect", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(null, versionName);

            object main = ReflectionUtil.GetStaticMember(Injector.Target.GetType("PCL.ModMain", true), "FrmMain");
            if (main == null)
                return;
            DispatcherObject dispatcherObject = main as DispatcherObject;
            Action action = delegate { ChangePage(main); };
            if (dispatcherObject == null)
                action();
            else
                dispatcherObject.Dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
        }

        private static void ChangePage(object main)
        {
            try
            {
                Type form = Injector.Target.GetType("PCL.FormMain", true);
                Type pageType = form.GetNestedType("PageType");
                Type subType = form.GetNestedType("PageSubType");
                Type stackType = form.GetNestedType("PageStackData");
                object stack = Activator.CreateInstance(stackType);
                FieldInfo pageField = stackType.GetField("Page", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (pageField != null)
                    pageField.SetValue(stack, Enum.Parse(pageType, "Download"));
                MethodInfo change = form.GetMethod("PageChange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { stackType, subType }, null);
                if (change != null)
                    change.Invoke(main, new object[] { stack, Enum.Parse(subType, "DownloadInstall") });
            }
            catch (Exception ex)
            {
                LogBuffer.Write("更新提示跳转失败：" + ex);
            }
        }
    }
}

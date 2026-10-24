using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PCLUnlock
{
    /// <summary>
    /// 下载页：把受限的加载器换成真实的版本清单加载器。
    /// </summary>
    internal static class HookDownload
    {
        public static void Install()
        {
            Type page = Injector.Target.GetType("PCL.PageDownloadInstall", false);
            if (page == null)
                throw new InvalidOperationException("缺少 PageDownloadInstall");
            MethodInfo target = page.GetMethod("LoaderInit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (target == null)
                throw new InvalidOperationException("缺少 LoaderInit");
            Native.PatchEntry(target, Native.HookPointer("LoaderInitHook", typeof(HookDownload)));
        }

        private static void LoaderInitHook(object self)
        {
            try
            {
                Type page = self.GetType();
                MethodInfo controlInit = page.GetMethod("ControlInit", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (controlInit != null)
                    controlInit.Invoke(self, null);

                Type modDownload = Injector.Target.GetType("PCL.ModDownload", true);
                FieldInfo loaderField = modDownload.GetField("DlClientListLoader", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object loader = loaderField.GetValue(null);

                object loadUi = ReflectionUtil.GetMemberValue(self, "LoadMinecraft");
                object panLoad = ReflectionUtil.GetMemberValue(self, "PanLoad");
                object panAllBack = ReflectionUtil.GetMemberValue(self, "PanAllBack");

                MethodInfo onFinish = page.GetMethod("LoadMinecraft_OnFinish", BindingFlags.Instance | BindingFlags.NonPublic);
                Type loaderType = loader.GetType();
                Type[] genericArguments = loaderType.GetGenericArguments();
                Type actionType = typeof(Action<>).MakeGenericType(loaderType);
                ParameterExpression parameter = Expression.Parameter(loaderType, "loader");
                Expression call = Expression.Call(Expression.Convert(Expression.Constant(self), page), onFinish);
                Delegate finished = Expression.Lambda(actionType, call, parameter).Compile();

                MethodInfo init = null;
                foreach (MethodInfo candidate in page.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (candidate.Name == "PageLoaderInit" && candidate.IsGenericMethodDefinition &&
                        candidate.GetGenericArguments().Length == 2 && candidate.GetParameters().Length == 8)
                    {
                        init = candidate;
                        break;
                    }
                }
                if (init == null)
                {
                    LogBuffer.Write("缺少 PageLoaderInit");
                    return;
                }
                MethodInfo closed = init.MakeGenericMethod(genericArguments[0], genericArguments[1]);
                closed.Invoke(self, new object[] { loadUi, panLoad, panAllBack, null, loader, finished, null, true });
            }
            catch (Exception ex)
            {
                LogBuffer.Write("下载页替换失败：" + ex);
            }
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace PCLUnlock
{
    /// <summary>
    /// 资源详情页：恢复整合包版本列表的安装入口。
    /// </summary>
    internal static class HookCompDetail
    {
        public static void Install()
        {
            Type type = Injector.Target.GetType("PCL.PageDownloadResourceDetail", false);
            if (type == null)
                throw new InvalidOperationException("缺少 PageDownloadResourceDetail");
            MethodInfo target = ReflectionUtil.FindMethod(type, "InitializeStackResourceVersions", 1, false);
            if (target == null)
                throw new InvalidOperationException("缺少 InitializeStackResourceVersions");
            Native.PatchEntry(target, Native.HookPointer("VersionListHook", typeof(HookCompDetail)));
        }

        private static void VersionListHook(object self, object stack)
        {
            try
            {
                Type pageType = self.GetType();
                object targetType = ReflectionUtil.GetMemberValue(self, "TargetResourceType");
                bool isModPack = targetType != null && targetType.ToString() == "ModPack";

                List<object> versions = new List<object>();
                IEnumerable source = ReflectionUtil.GetMemberValue(stack, "Tag") as IEnumerable;
                if (source != null)
                {
                    foreach (object version in source)
                        versions.Add(version);
                }
                versions.Sort(delegate(object a, object b)
                {
                    IComparable left = ReflectionUtil.GetMemberValue(a, "ReleaseDate") as IComparable;
                    IComparable right = ReflectionUtil.GetMemberValue(b, "ReleaseDate") as IComparable;
                    if (left == null || right == null)
                        return 0;
                    return right.CompareTo(left);
                });

                HashSet<string> seen = new HashSet<string>();
                bool hasDuplicates = false;
                foreach (object version in versions)
                {
                    string display = ReflectionUtil.GetMemberValue(version, "Display") as string;
                    if (display != null && !seen.Add(display))
                        hasDuplicates = true;
                }

                List<string> dependences = new List<string>();
                if (!isModPack)
                {
                    foreach (object version in versions)
                    {
                        IEnumerable dependencies = ReflectionUtil.GetMemberValue(version, "Dependencies") as IEnumerable;
                        if (dependencies == null)
                            continue;
                        foreach (object dependency in dependencies)
                        {
                            string name = dependency as string;
                            if (name != null && !dependences.Contains(name))
                                dependences.Add(name);
                        }
                    }
                    dependences.Sort(StringComparer.OrdinalIgnoreCase);
                }

                Type projectType = Injector.Target.GetType("PCL.ResourceProject", true);
                IDictionary cache = ReflectionUtil.GetStaticMember(projectType, "Cache") as IDictionary;
                if (cache != null)
                {
                    List<string> filtered = new List<string>();
                    foreach (string dependency in dependences)
                    {
                        if (cache.Contains(dependency))
                            filtered.Add(dependency);
                    }
                    dependences = filtered;
                }

                object children = ReflectionUtil.GetMemberValue(stack, "Children");
                MethodInfo addChild = children.GetType().GetMethod("Add");

                if (dependences.Count > 0)
                {
                    addChild.Invoke(children, new object[] { CreateHeader("前置资源", 6, 2, 0, 5) });
                    foreach (string dependency in dependences)
                    {
                        object project = cache[dependency];
                        MethodInfo toResourceItem = ReflectionUtil.FindMethod(project.GetType(), "ToResourceItem", 2, false);
                        object item = toResourceItem.Invoke(project, new object[] { false, false });
                        addChild.Invoke(children, new object[] { item });
                    }
                    addChild.Invoke(children, new object[] { CreateHeader("版本列表", 6, 12, 0, 5) });
                }

                MethodInfo installClick = pageType.GetMethod("Install_Click", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo saveClick = pageType.GetMethod("Save_Click", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo toList = ReflectionUtil.FindMethod(Injector.Target.GetType("PCL.ResourceVersion", true), "ToListItem", 3, false);
                if (toList == null)
                {
                    LogBuffer.Write("缺少 ResourceVersion.ToListItem");
                    return;
                }
                Type clickType = toList.GetParameters()[0].ParameterType;
                Type saveType = toList.GetParameters()[1].ParameterType;
                Delegate saveHandler = ReflectionUtil.CreateHandler(saveType, self, saveClick);
                Delegate installHandler = isModPack ? ReflectionUtil.CreateHandler(clickType, self, installClick) : null;

                foreach (object version in versions)
                {
                    object click = isModPack ? installHandler : ReflectionUtil.CreateHandler(clickType, self, saveClick);
                    object save = isModPack ? saveHandler : null;
                    object element = toList.Invoke(version, new object[] { click, save, hasDuplicates });
                    addChild.Invoke(children, new object[] { element });
                }

                FrameworkElement spacer = new FrameworkElement();
                spacer.Height = 18;
                addChild.Invoke(children, new object[] { spacer });
                ReflectionUtil.SetMemberValue(stack, "Tag", null);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("资源详情页替换失败：" + ex);
            }
        }

        private static TextBlock CreateHeader(string text, double left, double top, double right, double bottom)
        {
            TextBlock block = new TextBlock();
            block.Text = text;
            block.FontSize = 14;
            block.HorizontalAlignment = HorizontalAlignment.Left;
            block.Margin = new Thickness(left, top, right, bottom);
            return block;
        }
    }
}

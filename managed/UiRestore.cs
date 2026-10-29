using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace PCLUnlock
{
    /// <summary>
    /// 恢复被隐藏的下载相关控件。
    /// </summary>
    internal static class UiRestore
    {
        private static readonly ConditionalWeakTable<FrameworkElement, object> LayoutHooked =
            new ConditionalWeakTable<FrameworkElement, object>();
        private static readonly ConditionalWeakTable<FrameworkElement, object> DownloadLeftDefaulted =
            new ConditionalWeakTable<FrameworkElement, object>();
        private static bool _selectLeftLoaderSubscribed;

        public static void Install()
        {
            HookType("下载页", "PCL.PageDownloadLeft", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(DownloadLeftLoaded), true);
            });
            HookType("版本修改页", "PCL.PageDownloadModify", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(delegate(object sender, RoutedEventArgs e)
                {
                    FrameworkElement page = sender as FrameworkElement;
                    if (page != null)
                        AttachLayout(page, ModifyLayoutUpdated);
                }), true);
            });
            HookType("资源搜索页", "PCL.PageResource", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(ResourceLoaded), true);
            });
            HookType("版本设置页", "PCL.PageInstanceModDisabled", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(delegate(object sender, RoutedEventArgs e)
                {
                    FrameworkElement page = sender as FrameworkElement;
                    if (page != null)
                        AttachLayout(page, InstanceModDisabledLayoutUpdated);
                }), true);
            });
            HookType("启动页", "PCL.PageLaunchLeft", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(delegate(object sender, RoutedEventArgs e)
                {
                    FrameworkElement page = sender as FrameworkElement;
                    if (page != null)
                        AttachLayout(page, LaunchLayoutUpdated);
                }), true);
            });
            HookType("版本选择左页", "PCL.PageSelectLeft", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(SelectLeftLoaded), true);
            });
            HookType("版本选择右页", "PCL.PageSelectRight", delegate(Type type)
            {
                EventManager.RegisterClassHandler(type, FrameworkElement.LoadedEvent, new RoutedEventHandler(delegate(object sender, RoutedEventArgs e)
                {
                    FrameworkElement page = sender as FrameworkElement;
                    if (page != null)
                        AttachLayout(page, SelectRightLayoutUpdated);
                }), true);
            });
        }

        private static void HookType(string stage, string typeName, Action<Type> install)
        {
            try
            {
                Type type = Injector.Target.GetType(typeName, false);
                if (type == null)
                {
                    LogBuffer.Write("缺少类型：" + typeName);
                    return;
                }
                install(type);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("界面恢复失败（" + stage + "）：" + ex);
            }
        }

        private static void DownloadLeftLoaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            try
            {
                SetVisible(page, "ItemInstall");
                SetVisible(page, "LabResource");
                object marker;
                if (!DownloadLeftDefaulted.TryGetValue(page, out marker))
                {
                    DownloadLeftDefaulted.Add(page, new object());
                    page.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
                    {
                        SelectDownloadInstall(page);
                    }));
                }
            }
            catch (Exception ex)
            {
                LogBuffer.Write("下载页控件恢复失败：" + ex);
            }
        }

        private static void SelectDownloadInstall(FrameworkElement page)
        {
            try
            {
                object item = ReflectionUtil.GetMemberValue(page, "ItemInstall");
                if (item != null)
                    ReflectionUtil.SetMemberValue(item, "Checked", true);
                Type form = Injector.Target.GetType("PCL.FormMain", true);
                Type subType = form.GetNestedType("PageSubType");
                MethodInfo change = page.GetType().GetMethod("PageChange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { subType }, null);
                if (change != null)
                    change.Invoke(page, new object[] { Enum.Parse(subType, "DownloadInstall") });
            }
            catch (Exception ex)
            {
                LogBuffer.Write("下载页默认页恢复失败：" + ex);
            }
        }

        private static void ModifyLayoutUpdated(object sender, EventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            try
            {
                SetVisible(page, "CardMinecraft");
            }
            catch (Exception ex)
            {
                LogBuffer.Write("版本修改页控件恢复失败：" + ex);
            }
        }

        private static void ResourceLoaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            try
            {
                object pageType = ReflectionUtil.GetMemberValue(page, "PageType");
                if (pageType == null || pageType.ToString() != "ModPack")
                    return;
                SetVisible(page, "BtnSearchInstallModPack");
            }
            catch (Exception ex)
            {
                LogBuffer.Write("资源搜索页控件恢复失败：" + ex);
            }
        }

        private static void InstanceModDisabledLayoutUpdated(object sender, EventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            try
            {
                UIElement button = ReflectionUtil.GetMemberValue(page, "BtnDownload") as UIElement;
                if (button == null || button.Visibility == Visibility.Visible)
                    return;
                if (!DownloadAllowedBySettings() || SelectRightShowingHidden())
                    return;
                button.Visibility = Visibility.Visible;
                TriggerResize(page, "PanMain");
            }
            catch (Exception ex)
            {
                LogBuffer.Write("版本设置页控件恢复失败：" + ex);
            }
        }

        private static void LaunchLayoutUpdated(object sender, EventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            try
            {
                object loadFinished = ReflectionUtil.GetMemberValue(page, "IsLoadFinished");
                if (!(loadFinished is bool) || !(bool)loadFinished)
                    return;
                FrameworkElement button = ReflectionUtil.GetMemberValue(page, "BtnLaunch") as FrameworkElement;
                if (button == null || button.IsEnabled)
                    return;
                PropertyInfo text = button.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
                if (text == null || !Equals(text.GetValue(button, null), "启动游戏") || !DownloadAllowedBySettings())
                    return;
                text.SetValue(button, "下载游戏", null);
                button.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogBuffer.Write("启动页控件恢复失败：" + ex);
            }
        }

        private static void SelectLeftLoaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            page.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(delegate
            {
                RefreshSelectLeft(page);
            }));
            SubscribeSelectLeftLoader();
        }

        private static void RefreshSelectLeft(FrameworkElement page)
        {
            try
            {
                MethodInfo method = page.GetType().GetMethod("McFolderListUI", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (method != null)
                    method.Invoke(page, null);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("版本选择页列表刷新失败：" + ex);
            }
            EnsureImportItem(page);
        }

        private static void SubscribeSelectLeftLoader()
        {
            if (_selectLeftLoaderSubscribed)
                return;
            _selectLeftLoaderSubscribed = true;
            try
            {
                Type modMinecraft = Injector.Target.GetType("PCL.ModMinecraft", true);
                object loader = ReflectionUtil.GetStaticMember(modMinecraft, "McFolderListLoader");
                if (loader == null)
                    return;
                EventInfo preview = loader.GetType().GetEvent("PreviewFinish", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (preview == null)
                    return;
                MethodInfo handler = typeof(UiRestore).GetMethod("SelectLeftPreviewFinish", BindingFlags.Static | BindingFlags.NonPublic);
                Delegate callback = ReflectionUtil.CreateStaticHandler(preview.EventHandlerType, handler);
                preview.AddEventHandler(loader, callback);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("版本选择页加载器订阅失败：" + ex);
            }
        }

        private static void SelectLeftPreviewFinish(object loader)
        {
            object main = ReflectionUtil.GetStaticMember(Injector.Target.GetType("PCL.ModMain", true), "FrmSelectLeft");
            FrameworkElement page = main as FrameworkElement;
            if (page == null)
                return;
            page.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate
            {
                RefreshSelectLeft(page);
            }));
        }

        private static bool EnsureImportItem(FrameworkElement page)
        {
            if (page == null)
                return false;
            try
            {
                StackPanel list = ReflectionUtil.GetMemberValue(page, "PanList") as StackPanel;
                if (list == null || list.Children.Count == 0)
                    return false;
                if (HasImportItem(list))
                    return true;
                int insertIndex = -1;
                for (int i = 0; i < list.Children.Count; i++)
                {
                    string title = ReflectionUtil.GetMemberValue(list.Children[i], "Title") as string;
                    if (title != null && title.Trim() == "添加已有文件夹")
                    {
                        insertIndex = i + 1;
                        break;
                    }
                }
                if (insertIndex < 0)
                    return false;
                object item = CreateImportItem();
                if (item == null)
                    return false;
                list.Children.Insert(insertIndex, (UIElement)item);
                return true;
            }
            catch (Exception ex)
            {
                LogBuffer.Write("版本选择页控件恢复失败：" + ex);
                return false;
            }
        }

        private static void SelectRightLayoutUpdated(object sender, EventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            try
            {
                object showHidden = ReflectionUtil.GetMemberValue(page, "ShowHidden");
                if (showHidden is bool && (bool)showHidden)
                    return;
                FrameworkElement empty = ReflectionUtil.GetMemberValue(page, "PanEmpty") as FrameworkElement;
                if (empty == null || empty.Visibility != Visibility.Visible)
                    return;
                UIElement button = ReflectionUtil.GetMemberValue(page, "BtnEmptyDownload") as UIElement;
                if (button == null || button.Visibility == Visibility.Visible)
                    return;
                MethodInfo loaded = page.GetType().GetMethod("BtnEmptyDownload_Loaded", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (loaded != null)
                    loaded.Invoke(page, null);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("版本选择页空状态恢复失败：" + ex);
            }
        }

        private static bool HasImportItem(StackPanel list)
        {
            for (int i = 0; i < list.Children.Count; i++)
            {
                string title = ReflectionUtil.GetMemberValue(list.Children[i], "Title") as string;
                if (title != null && title.Trim() == "导入整合包")
                    return true;
            }
            return false;
        }

        private static object CreateImportItem()
        {
            Type itemType = Injector.Target.GetType("PCL.MyListItem", true);
            object item = Activator.CreateInstance(itemType);
            Type checkType = itemType.GetNestedType("CheckType");
            ReflectionUtil.SetMemberValue(item, "IsScaleAnimationEnabled", false);
            ReflectionUtil.SetMemberValue(item, "Type", Enum.Parse(checkType, "Clickable"));
            ReflectionUtil.SetMemberValue(item, "Title", "导入整合包");
            ReflectionUtil.SetMemberValue(item, "Height", 34.0);
            ReflectionUtil.SetMemberValue(item, "ToolTip", "在当前选择的 Minecraft 文件夹下安装整合包");
            ReflectionUtil.SetMemberValue(item, "LogoScale", 0.9);
            try
            {
                ReflectionUtil.SetMemberValue(item, "Logo",
                    "M512 40.96C249.344 40.96 35.84 252.416 35.84 512s213.504 471.04 476.16 471.04c103.424 0 202.752-33.28 286.72-96.256l1.536-1.536c5.12-5.632 7.68-12.8 7.68-19.968 0-16.896-13.824-30.208-30.72-30.208-7.68 0-15.36 2.56-20.992 7.68h-0.512c-71.68 52.224-155.648 79.36-243.712 79.36-227.328 0-412.16-182.784-412.16-407.552 0-224.768 184.832-407.552 412.16-407.552s412.16 182.784 412.16 407.552c0 68.608-15.872 132.608-46.592 190.464-0.512 1.024-1.024 2.048-1.024 3.072-0.512 2.048-1.536 4.608-1.536 8.192 0 16.896 13.824 30.208 30.72 30.208 12.288 0 23.04-7.168 28.16-18.432 35.84-68.608 53.76-141.312 53.76-216.064 0.512-259.584-212.992-471.04-475.648-471.04z M812.032 483.328c-31.744-20.992-71.68 1.536-78.848 6.144-1.024 0.512-104.448 61.44-128 74.752-8.192 4.608-22.528-0.512-27.136-4.096-31.232-36.352-54.272-70.656-68.608-102.4-13.312-29.184 0.512-41.472 3.072-43.52 7.168-4.608 114.688-68.608 143.36-83.456 24.064-12.288 40.96-25.088 46.08-45.056 3.072-13.312 0-27.136-9.216-39.936-22.016-31.744-172.544-84.992-311.296-3.584-157.184 91.648-152.064 242.688-150.528 292.352v9.216c0 18.944-12.8 37.376-14.848 40.448l-20.992 21.504c-6.144 6.144-9.216 13.824-9.216 22.528 0 8.704 3.584 16.384 9.728 22.528 12.8 12.288 32.768 11.776 45.056-0.512l22.528-23.552 0.512-0.512c3.072-3.584 30.208-38.4 30.208-81.92l-0.512-11.264c-1.536-44.544-5.632-162.816 119.296-235.52 88.064-51.2 173.056-32.256 208.896-19.968-36.864 19.456-143.36 83.456-144.896 84.48-22.016 14.336-55.808 58.88-26.112 122.88 17.408 37.376 43.52 76.8 80.896 120.32 14.336 17.408 62.976 37.376 103.424 15.36 24.576-13.312 125.44-73.216 130.048-75.776 2.048-1.024 4.608-2.56 7.68-3.584 0 2.56-0.512 6.144-1.024 10.752-5.632 35.84-35.328 155.136-191.488 181.76-49.664 8.704-89.6 3.584-121.856-0.512h-0.512c-37.888-4.608-73.216-9.216-101.888 14.336-31.232 26.112-40.96 34.304-35.84 54.272 3.584 14.336 16.384 24.064 30.72 24.064 2.56 0 5.12-0.512 7.68-1.024 6.656-1.536 12.8-5.632 16.896-10.752 2.048-2.048 7.68-6.656 20.992-18.432 6.656-5.632 25.088-3.584 52.736 0 34.816 4.608 81.92 10.24 141.312 0.512 157.184-26.624 228.864-138.752 243.2-234.496 7.68-38.912 0-64.512-21.504-78.336z");
            }
            catch
            {
            }
            EventInfo click = itemType.GetEvent("Click");
            MethodInfo handler = typeof(UiRestore).GetMethod("ImportModpackClick", BindingFlags.Static | BindingFlags.NonPublic);
            click.AddEventHandler(item, ReflectionUtil.CreateStaticHandler(click.EventHandlerType, handler));
            return item;
        }

        private static void ImportModpackClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Type type = Injector.Target.GetType("PCL.ModModpack", true);
                MethodInfo method = type.GetMethod("ModpackInstall", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
                if (method != null)
                    method.Invoke(null, null);
            }
            catch (Exception ex)
            {
                LogBuffer.Write("导入整合包失败：" + ex);
            }
        }

        private static bool DownloadAllowedBySettings()
        {
            if (!GetSettingBool("UiHiddenPageDownload"))
                return true;
            object force = ReflectionUtil.GetStaticMember(Injector.Target.GetType("PCL.PageSetupUI", true), "HiddenForceShow");
            return force is bool && (bool)force;
        }

        private static bool SelectRightShowingHidden()
        {
            object right = ReflectionUtil.GetStaticMember(Injector.Target.GetType("PCL.ModMain", true), "FrmSelectRight");
            if (right == null)
                return false;
            object showHidden = ReflectionUtil.GetMemberValue(right, "ShowHidden");
            return showHidden is bool && (bool)showHidden;
        }

        private static bool GetSettingBool(string key)
        {
            try
            {
                Type type = Injector.Target.GetType("PCL.Settings", true);
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static))
                {
                    if (method.Name != "Get" || !method.IsGenericMethodDefinition || method.GetGenericArguments().Length != 1)
                        continue;
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length != 2 || parameters[0].ParameterType != typeof(string))
                        continue;
                    object value = method.MakeGenericMethod(typeof(bool)).Invoke(null, new object[] { key, null });
                    return value is bool && (bool)value;
                }
            }
            catch
            {
            }
            return false;
        }

        private static void AttachLayout(FrameworkElement page, EventHandler handler)
        {
            object marker;
            if (LayoutHooked.TryGetValue(page, out marker))
                return;
            LayoutHooked.Add(page, new object());
            page.LayoutUpdated += handler;
        }

        private static void SetVisible(object owner, string name)
        {
            UIElement element = ReflectionUtil.GetMemberValue(owner, name) as UIElement;
            if (element != null && element.Visibility != Visibility.Visible)
                element.Visibility = Visibility.Visible;
        }

        private static void TriggerResize(object owner, string name)
        {
            object panel = ReflectionUtil.GetMemberValue(owner, name);
            if (panel == null)
                return;
            MethodInfo method = panel.GetType().GetMethod("TriggerForceResize", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method != null)
                method.Invoke(panel, null);
        }
    }
}

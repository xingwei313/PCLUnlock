using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PCLUnlock
{
    /// <summary>
    /// 设置-其他页：添加"PCLUnlock日志"按钮，点击复制进程内日志。
    /// </summary>
    internal static class SettingsButton
    {
        public static void Install()
        {
            Type page = Injector.Target.GetType("PCL.PageSetupSystem", false);
            if (page == null)
                throw new InvalidOperationException("缺少 PageSetupSystem");
            EventManager.RegisterClassHandler(page, FrameworkElement.LoadedEvent, new RoutedEventHandler(PageLoaded), true);
        }

        private static void PageLoaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement page = sender as FrameworkElement;
            if (page == null)
                return;
            page.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(delegate
            {
                AddButton(page);
            }));
        }

        private static void AddButton(FrameworkElement page)
        {
            try
            {
                Grid panel = ReflectionUtil.GetMemberValue(page, "PanDonate") as Grid;
                if (panel == null || panel.Tag != null)
                    return;
                panel.Visibility = Visibility.Visible;
                Type buttonType = Injector.Target.GetType("PCL.MyButton", true);
                object button = Activator.CreateInstance(buttonType);
                ReflectionUtil.SetMemberValue(button, "Text", "PCLUnlock日志");
                ReflectionUtil.SetMemberValue(button, "MinWidth", 140.0);
                ReflectionUtil.SetMemberValue(button, "Padding", new Thickness(13, 0, 13, 0));
                ReflectionUtil.SetMemberValue(button, "Margin", new Thickness(0, 0, 20, 0));
                ReflectionUtil.SetMemberValue(button, "HorizontalAlignment", HorizontalAlignment.Left);
                Grid.SetColumn((UIElement)button, 2);
                panel.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto, SharedSizeGroup = "Button" });
                panel.Children.Add((UIElement)button);

                EventInfo click = buttonType.GetEvent("Click");
                MethodInfo handler = typeof(SettingsButton).GetMethod("CopyLog", BindingFlags.Static | BindingFlags.NonPublic);
                click.AddEventHandler(button, ReflectionUtil.CreateStaticHandler(click.EventHandlerType, handler));
                panel.Tag = "PCLUnlock";
            }
            catch (Exception ex)
            {
                LogBuffer.Write("添加设置按钮失败：" + ex);
            }
        }

        private static void CopyLog(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Type modBase = Injector.Target.GetType("PCL.ModBase", true);
                MethodInfo clipboardSet = ReflectionUtil.FindMethod(modBase, "ClipboardSet", 2, true);
                clipboardSet.Invoke(null, new object[] { LogBuffer.Text, true });
            }
            catch (Exception ex)
            {
                LogBuffer.Write("复制日志失败：" + ex);
            }
        }
    }
}

using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace PCLUnlock
{
    /// <summary>
    /// 整合包安装：选择文件的入口与按类型分派的安装实现。
    /// </summary>
    internal static class HookModpack
    {
        private static Type _type;
        private static MethodInfo _withFile;

        public static void Install()
        {
            _type = Injector.Target.GetType("PCL.ModModpack", false);
            if (_type == null)
                throw new InvalidOperationException("缺少 ModModpack");
            MethodInfo noArgument = _type.GetMethod("ModpackInstall", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            _withFile = _type.GetMethod("ModpackInstall", BindingFlags.Public | BindingFlags.Static, null,
                new Type[] { typeof(string), typeof(string), typeof(string) }, null);
            if (noArgument != null)
                Native.PatchEntry(noArgument, Native.HookPointer("InstallDialogHook", typeof(HookModpack)));
            if (_withFile != null)
                Native.PatchEntry(_withFile, Native.HookPointer("InstallFileHook", typeof(HookModpack)));
        }

        private static void InstallDialogHook()
        {
            try
            {
                Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                dialog.Title = "选择整合包压缩文件";
                dialog.Filter = "整合包文件|*.zip;*.mrpack;*.rar";
                if (dialog.ShowDialog() != true)
                    return;
                _withFile.Invoke(null, new object[] { dialog.FileName, null, null });
            }
            catch (Exception ex)
            {
                LogBuffer.Write("选择整合包失败：" + ex);
            }
        }

        private static object InstallFileHook(string file, string instanceName, string logo)
        {
            ZipArchive archive = null;
            try
            {
                archive = new ZipArchive(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), ZipArchiveMode.Read);
                string baseFolder = "";
                int packType = -1;
                if (archive.GetEntry("mcbbs.packmeta") != null)
                    packType = 3;
                else if (archive.GetEntry("mmc-pack.json") != null)
                    packType = 2;
                else if (archive.GetEntry("modrinth.index.json") != null)
                    packType = 4;
                else if (archive.GetEntry("manifest.json") != null)
                    packType = HasAddons(archive.GetEntry("manifest.json")) ? 3 : 0;
                else if (archive.GetEntry("modpack.json") != null)
                    packType = 1;
                else if (archive.GetEntry("modpack.zip") != null || archive.GetEntry("modpack.mrpack") != null)
                    packType = 9;
                else
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        string[] parts = entry.FullName.Split('/');
                        baseFolder = parts[0] + "/";
                        if (parts.Length != 2)
                            continue;
                        string name = parts[1];
                        if (name == "mcbbs.packmeta")
                        {
                            packType = 3;
                            break;
                        }
                        if (name == "mmc-pack.json")
                        {
                            packType = 2;
                            break;
                        }
                        if (name == "modrinth.index.json")
                        {
                            packType = 4;
                            break;
                        }
                        if (name == "manifest.json")
                        {
                            bool addons = HasAddons(entry);
                            packType = addons ? 3 : 0;
                            if (addons)
                                baseFolder = "overrides/";
                            break;
                        }
                        if (name == "modpack.json")
                        {
                            packType = 1;
                            break;
                        }
                        if (name == "modpack.zip" || name == "modpack.mrpack")
                        {
                            packType = 9;
                            break;
                        }
                    }
                }

                string methodName;
                object[] arguments;
                switch (packType)
                {
                    case 0:
                        methodName = "InstallPackCurseForge";
                        arguments = new object[] { file, archive, baseFolder, instanceName, logo };
                        break;
                    case 1:
                        methodName = "InstallPackHMCL";
                        arguments = new object[] { file, archive, baseFolder };
                        break;
                    case 2:
                        methodName = "InstallPackMMC";
                        arguments = new object[] { file, archive, baseFolder };
                        break;
                    case 3:
                        methodName = "InstallPackMCBBS";
                        arguments = new object[] { file, archive, baseFolder, instanceName };
                        break;
                    case 4:
                        methodName = "InstallPackModrinth";
                        arguments = new object[] { file, archive, baseFolder, instanceName, logo };
                        break;
                    case 9:
                        methodName = "InstallPackLauncherPack";
                        arguments = new object[] { file, archive, baseFolder };
                        break;
                    default:
                        methodName = "InstallPackCompress";
                        arguments = new object[] { file, archive };
                        break;
                }
                MethodInfo installer = ReflectionUtil.FindMethod(_type, methodName, arguments.Length, true);
                if (installer == null)
                {
                    LogBuffer.Write("缺少安装方法：" + methodName);
                    throw new MissingMethodException(methodName);
                }
                LogBuffer.Write("整合包类型 " + packType + " -> " + methodName);
                return installer.Invoke(null, arguments);
            }
            finally
            {
                if (archive != null)
                    archive.Dispose();
            }
        }

        private static bool HasAddons(ZipArchiveEntry entry)
        {
            using (StreamReader reader = new StreamReader(entry.Open(), Encoding.UTF8))
                return reader.ReadToEnd().Contains("\"addons\"");
        }
    }
}

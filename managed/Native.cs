using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PCLUnlock
{
    /// <summary>
    /// 原生互操作与函数入口替换。
    /// </summary>
    internal static class Native
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(IntPtr address, UIntPtr size, uint newProtect, out uint oldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FlushInstructionCache(IntPtr process, IntPtr address, UIntPtr size);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        private static readonly byte[] Stub = new byte[12];

        public static void PatchEntry(MethodInfo method, IntPtr hook)
        {
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            IntPtr address = method.MethodHandle.GetFunctionPointer();
            unsafe
            {
                byte* pointer = (byte*)address;
                if (pointer[0] == 0xFF && pointer[1] == 0x25)
                {
                    address = *(IntPtr*)(pointer + 6);
                    pointer = (byte*)address;
                }

                long target = hook.ToInt64();
                Stub[0] = 0x48;
                Stub[1] = 0xB8;
                for (int i = 0; i < 8; i++)
                    Stub[2 + i] = (byte)(target >> (8 * i));
                Stub[10] = 0xFF;
                Stub[11] = 0xE0;

                uint oldProtect;
                if (!VirtualProtect(address, (UIntPtr)Stub.Length, 0x40, out oldProtect))
                    return;
                Marshal.Copy(Stub, 0, address, Stub.Length);
                VirtualProtect(address, (UIntPtr)Stub.Length, oldProtect, out oldProtect);
                FlushInstructionCache(GetCurrentProcess(), address, (UIntPtr)Stub.Length);
            }
        }

        public static IntPtr HookPointer(string methodName, Type type)
        {
            MethodInfo method = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            RuntimeHelpers.PrepareMethod(method.MethodHandle);
            return method.MethodHandle.GetFunctionPointer();
        }
    }
}

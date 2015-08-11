﻿using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Dumper
{
    //TODO make generic read method
    internal static class Memory
    {
        private const int Bytes = 0;
        private static IntPtr _handle;

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        private static extern bool ReadProcessMemory(IntPtr hProcess, long lpBaseAddress, [Out] byte[] lpBuffer,
            int dwSize, [Out] int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        private static extern bool WriteProcessMemory(IntPtr hProcess, long lpBaseAddress, byte[] lpBuffer, uint nSize,
            [Out] int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize,
            uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flNewProtect,
            out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize,
            IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, [Out] uint lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern int VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint dwFreeType);

        public static bool ConnectToGame(Game.GameId gameId)
        {
            var process = Process.GetProcessesByName(Game.ProcessForGameId(gameId)).FirstOrDefault();
            if (process != null)
            {
                _handle = OpenProcess(0x1f0fff, false, process.Id);
                return true;
            }
            return false;
        }

        public static void Free(long pointer, int length)
        {
            VirtualFreeEx(_handle, (IntPtr) pointer, (uint) length, 0x8000);
        }

        public static void CreateRemoteThread(long pointer)
        {
            CreateRemoteThread(_handle, IntPtr.Zero, 0, (IntPtr) pointer, IntPtr.Zero, 0, Bytes);
        }

        public static int Malloc(long length)
        {
            return (int) VirtualAllocEx(_handle, IntPtr.Zero, (uint) length, 0x3000, 0x40);
        }

        public static void WriteInt(long pointer, int value)
        {
            Write(pointer, BitConverter.GetBytes(value));
        }

        public static long ReadLong(long pointer)
        {
            var buffer = new byte[8];
            ReadProcessMemory(_handle, pointer, buffer, 8, Bytes);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static int ReadInt(long pointer)
        {
            var buffer = new byte[4];
            ReadProcessMemory(_handle, pointer, buffer, 4, Bytes);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static byte[] Read(long pointer, int length)
        {
            var buffer = new byte[length];
            ReadProcessMemory(_handle, pointer, buffer, buffer.Length, Bytes);
            return buffer;
        }

        public static string ReadString(long pointer)
        {
            var strPointer = ReadInt(pointer);
            var sb = new StringBuilder();
            while (Read(strPointer, 1)[0] != 0)
            {
                sb.Append(Convert.ToChar((Read(strPointer, 1)[0])));
                strPointer++;
            }
            return sb.ToString();
        }

        public static void Write(long pointer, byte[] b)
        {
            uint oldprotect;
            VirtualProtectEx(_handle, (IntPtr) pointer, (uint) b.Length, 0x40, out oldprotect);
            WriteProcessMemory(_handle, pointer, b, (uint) b.Length, Bytes);
        }
    }
}
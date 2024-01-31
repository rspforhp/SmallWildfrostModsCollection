using System.IO;
using System.Runtime.InteropServices;

namespace SmallConsoleMod
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Deadpan.Enums.Engine.Components.Modding;
    using HarmonyLib;
    using UnityEngine;
    using UnityEngine.UI;


    public class ConsoleMod : WildfrostMod
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int FreeConsole();

        private const UInt32 StdOutputHandle = 0xFFFFFFF5;

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateFile(string lpFileName, uint
            dwDesiredAccess, uint dwShareMode, uint lpSecurityAttributes, uint
            dwCreationDisposition, uint dwFlagsAndAttributes, uint hTemplateFile);

        private const int MY_CODE_PAGE = 437;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint FILE_SHARE_WRITE = 0x2;
        private const uint OPEN_EXISTING = 0x3;

        public ConsoleMod(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "kopie.wildfrost.console";
        public override string[] Depends => new string[] { };
        public override string Title => "Console mod";
        public override string Description => "Adds a minimal console window.";


        protected override void Load()
        {
            base.Load();
            AllocConsole();
            IntPtr currentStdout = CreateFile("CONOUT$", GENERIC_WRITE, FILE_SHARE_WRITE, 0,
                OPEN_EXISTING, 0, 0);
            SetStdHandle(StdOutputHandle, currentStdout);
            // reopen stdout
            TextWriter writer = new StreamWriter(Console.OpenStandardOutput())
                { AutoFlush = true };
            Console.SetOut(writer);
            Console.WriteLine($"Hello world!");
            Application.logMessageReceived += OnApplicationOnlogMessageReceived;
            Debug.Log($"Debug.Log() Hello world!");
        }

        private void OnApplicationOnlogMessageReceived(string logString, string stackTrace, LogType type)
        {
            Console.WriteLine($"[{type}] {logString}");
            if (type == LogType.Error) Console.WriteLine(stackTrace);
        }


        protected override void Unload()
        {
            base.Unload();
            Console.WriteLine($"This console is not valid. If you see this, you can safely close the window.");
            FreeConsole();
            Application.logMessageReceived -= OnApplicationOnlogMessageReceived;
        }
    }
}
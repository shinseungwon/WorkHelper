﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SSMSHelper2
{
    public delegate int MouseEventCallBack(IntPtr code, int x, int y);
    public delegate int KeyEventCallBack(IntPtr code, int key);

    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AllocConsole();

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        public static extern bool SetKeyboardState(byte[] lpKeyState);

        private static readonly string[] opsStr = new string[10];

        private static readonly List<string> include = new List<string>();
        private static readonly List<string> exclude = new List<string>();

        private static readonly List<MyCommand> commands = new List<MyCommand>();

        public Form1()
        {
            InitializeComponent();
            AllocConsole();

            HookEvents.EnableHook();
            //HookEvents.Mc = new MouseEventCallBack(MCallback);
            HookEvents.Kc = new KeyEventCallBack(KCallback);
            SetCommand();

            checkBox1.Checked = true;

            //Environment.NewLine cr + lf (13 + 10)

            DirectoryInfo di = new DirectoryInfo(Directory.GetCurrentDirectory());
            FileInfo[] files = di.GetFiles();
            foreach (FileInfo fi in files)
            {
                if (fi.Name.StartsWith("!"))
                {
                    //Console.WriteLine(fi.FullName);
                    commands.Add(new MyCommand(File.ReadAllText(fi.FullName)));
                }
            }

            if (File.Exists("preset.txt"))
            {
                string presetStr = File.ReadAllText("preset.txt");
                string[] preset = presetStr.Replace("\r", "").Split('\n');
                foreach (string s in preset)
                {
                    if (s.StartsWith("+"))
                    {
                        include.Add(s);
                        textBox1.AppendText(s.Substring(1, s.Length - 1) + Environment.NewLine);
                    }
                    else if (s.StartsWith("-"))
                    {
                        exclude.Add(s);
                        textBox2.AppendText(s.Substring(1, s.Length - 1) + Environment.NewLine);
                    }
                }
            }
        }

        private static int KCallback(IntPtr code, int key)
        {
            try
            {
                //Console.WriteLine(key);
                //New Object Model Test
                foreach (MyCommand myCommand in commands)
                {
                    int res;
                    //0 -> no action, -1 -> action
                    res = myCommand.Execute(key
                        , GetActiveWindowTitle()
                        , HookEvents.keyPressing[160] == 1 //shift
                        , HookEvents.keyPressing[162] == 1 //ctrl
                        );
                    if (res == -1)
                    {
                        return -1;
                    }
                }

                return 0;
                //~New Object Model Test

                bool inc = false, exc = true;
                string awt = GetActiveWindowTitle();
                foreach (string s in include)
                {
                    if (s.Length > 0 && awt.Contains(s))
                    {
                        inc = true;
                        break;
                    }
                }
                foreach (string s in exclude)
                {
                    if (s.Length > 0 && awt.Contains(s))
                    {
                        exc = false;
                        break;
                    }
                }

                if (inc && exc)
                {
                    Console.WriteLine("Callback Key : " + key);

                    if (key == 114)
                    {
                        SendKeys.Send("{END}");
                        SendKeys.Send("+{HOME}");
                        return -1;
                    }

                    if (key == 115)
                    {
                        SendKeys.Send("{END}");
                        SendKeys.Send("+{HOME}");
                        SendKeys.Send("{F5}");
                        return -1;
                    }

                    if (HookEvents.keyPressing[162] == 1 && key == 189) //l
                    {
                        SendKeys.Send("{LEFT}");
                        return -1;
                    }

                    if (HookEvents.keyPressing[162] == 1 && key == 187) //r
                    {
                        SendKeys.Send("{RIGHT}");
                        return -1;
                    }

                    if (key == 120) //l
                    {
                        SendKeys.Send("{LEFT}");
                        return -1;
                    }

                    if (key == 121) //r
                    {
                        SendKeys.Send("{RIGHT}");
                        return -1;
                    }

                    if (key == 122) //u
                    {
                        if (HookEvents.keyPressing[162] == 1)
                        {
                            SendKeys.Send("{PAGE UP}");
                        }
                        else
                        {
                            SendKeys.Send("{UP}");
                        }
                        return -1;
                    }

                    if (key == 123) //d
                    {
                        if (HookEvents.keyPressing[162] == 1)
                        {
                            SendKeys.Send("{PAGE DOWN}");
                        }
                        else
                        {
                            SendKeys.Send("{DOWN}");
                        }
                        return -1;
                    }

                    if (HookEvents.keyPressing[162] == 1 && key >= 48 && key <= 57)
                    {
                        string keyStr = "" + (char)key;
                        if (int.TryParse(keyStr, out int x))
                        {
                            string command = opsStr[x];
                            if (command != null)
                            {
                                string[] commands = command.Split('\n');
                                if (commands.Length == 3)
                                {
                                    string original = Clipboard.GetText();
                                    string res = commands[0].Trim() == "'" ? "" : commands[0].Trim() + Environment.NewLine;
                                    int i, j;

                                    string[][] sa = Trimming(original);

                                    for (i = 0; i < sa.Length; i++)
                                    {
                                        string regex = commands[1].Trim();
                                        List<string> tl = new List<string>();

                                        for (j = 0; j < sa[i].Length; j++)
                                        {
                                            if (sa[i][j].Length > 0)
                                            {
                                                tl.Add(sa[i][j].Trim());
                                            }
                                        }

                                        for (j = 0; j < tl.Count; j++)
                                        {
                                            regex = regex.Replace("{" + j + "}", tl[j]);
                                        }

                                        res += regex + Environment.NewLine;

                                    }
                                    res += commands[2].Trim() == "'" ? "" : commands[2].Trim() + Environment.NewLine;

                                    Clipboard.SetText(res);
                                    SendKeys.Send("{v}");
                                    Clipboard.SetText(original);
                                }
                                else if (commands.Length == 1 || commands.Length == 2)
                                {
                                    string original = Clipboard.GetText();
                                    Clipboard.SetText(MyTrim(commands[0]));
                                    SendKeys.Send("{v}");
                                    Clipboard.SetText(original);
                                    if (commands.Length == 2)
                                    {
                                        return -1;
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Wrong Command");
                                }
                            }
                        }
                    }
                }
                else
                {
                    if (HookEvents.keyPressing[160] == 0
                        && HookEvents.keyPressing[162] == 0
                        && key == 19)
                    {
                        string original = Clipboard.GetText();
                        Clipboard.SetText(MyTrim("sungwon@5300"));
                        SendKeys.Send("^{v}");
                        Clipboard.SetText(original);
                        return -1;
                    }
                }
            }
            catch (Exception e)
            {
                File.WriteAllText("ErrorLog" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt", e.ToString());
            }
            finally
            {

            }
            return 0;
        }

        private static int MCallback(IntPtr code, int x, int y)
        {
            return 0;
        }

        private static string GetActiveWindowTitle()
        {
            const int nChars = 256;
            StringBuilder Buff = new StringBuilder(nChars);
            IntPtr handle = GetForegroundWindow();

            if (GetWindowText(handle, Buff, nChars) > 0)
            {
                return Buff.ToString();
            }

            return "";
        }

        private static string[][] Trimming(string str)
        {
            string[] lines = str.Replace("\r", "").Split('\n');
            List<string> lineList = new List<string>();
            foreach (string s in lines)
            {
                if (!lineList.Contains(s))
                {
                    lineList.Add(s);
                }
            }

            List<string[]> tmp = new List<string[]>();
            foreach (string s in lineList)
            {
                if (s.Length > 0)
                {
                    string[] items = s.Split(' ');
                    List<string> stacks = new List<string>();
                    foreach (string ss in items)
                    {
                        if (ss.Length > 0)
                        {
                            stacks.Add(MyTrim(ss.Trim()));
                        }
                    }
                    tmp.Add(stacks.ToArray());
                }
            }

            return tmp.ToArray();
        }

        private static string MyTrim(string s)
        {
            return s.Replace("\r", "")
                .Replace("\n", "")
                .Replace("\t", "");
        }

        private static void SetCommand()
        {
            foreach (string fs in Directory.GetFiles(Directory.GetCurrentDirectory()))
            {
                if (fs.EndsWith(".txt"))
                {
                    string target = Path.GetFileName(fs).Substring(0, 1);
                    if (int.TryParse(target, out int i) && opsStr[i] == null)
                    {
                        Console.WriteLine(i + " : " + Path.GetFileName(fs));
                        opsStr[i] = File.ReadAllText(fs);
                    }
                }
            }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                TopMost = true;
            }
            else
            {
                TopMost = false;
            }
        }

        private void textBoxesChanged(object sender, EventArgs e)
        {
            include.Clear();
            exclude.Clear();
            include.AddRange(textBox1.Text.Replace("\r", "").Split('\n'));
            exclude.AddRange(textBox2.Text.Replace("\r", "").Split('\n'));
        }
    }

    public class MyCommand
    {
        public string fileText = "";

        public readonly List<string> includes = new List<string>();
        public readonly List<string> excludes = new List<string>();

        public readonly List<MyCommandItem> items = new List<MyCommandItem>();

        public MyCommand(string fileText)
        {
            this.fileText = fileText;

            string[] itemstrings = fileText.Split('@');

            int i;
            string[] cludes = itemstrings[0].Replace("\r", "")
                .Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string s in cludes)
            {
                if (s.StartsWith("+"))
                {
                    includes.Add(s.Substring(1, s.Length - 1));
                }
                else if (s.StartsWith("-"))
                {
                    excludes.Add(s.Substring(1, s.Length - 1));
                }
            }

            for (i = 1; i < itemstrings.Length; i++)
            {
                items.Add(new MyCommandItem(itemstrings[i]));
            }
        }

        public int Execute(int key, string window, bool shift = false, bool control = false)
        {
            bool inc = false, exc = false;

            //code
            foreach (string s in includes)
            {
                if (window.Contains(s))
                {
                    inc = true;
                    break;
                }
            }

            foreach (string s in excludes)
            {
                if (window.Contains(s))
                {
                    exc = true;
                    break;
                }
            }

            if (inc && !exc)
            {
                foreach (MyCommandItem item in items)
                {
                    int res;
                    res = item.Execute(key, shift, control);
                    if (res == -1)
                    {
                        return -1;
                    }
                }
            }
            //~code

            return 0;
        }
    }

    public class MyCommandItem
    {
        public string itemString = "";
        public string name = "";
        public string key = "";
        public string type = "";
        public string content = "";
        public MyCommandItem(string itemString)
        {
            this.itemString = itemString;
            string[] lines = itemString.Replace("\r", "")
                .Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);

            int i;
            name = lines[0];
            key = lines[1];
            type = lines[2];

            string content = "";
            for (i = 3; i < lines.Length; i++)
            {
                content += lines[i];
                if (i != lines.Length - 1)
                {
                    content += "\r\n";
                }
            }
        }

        public bool Matches(int keyCode, bool shift, bool control)
        {
            Console.WriteLine("Matches Start : " + keyCode + "/"
                + (char)keyCode + "/" + shift + "/" + control);

            Keys keyObj = (Keys)keyCode;
            string currentKey = "{" + keyObj.ToString() + "}";

            //Console.WriteLine(currentKey);
            //SendKeys.Send("+^" + currentKey);

            //매치 되는지만 생각

            return false;
        }

        public int Execute(int key, bool shift, bool control)
        {
            if (Matches(key, shift, control))
            {
                //Run Step
                if (type[0] == '1')
                {

                }
                else if (type[0] == '2')
                {
                    if (type[2] == '1')
                    {

                    }
                    else if (type[2] == '2')
                    {

                    }
                    else if (type[2] == '3')
                    {

                    }
                }
                else if (type[0] == '3')
                {
                    if (type[2] == '1')
                    {

                    }
                    else if (type[2] == '2')
                    {

                    }
                    else if (type[2] == '3')
                    {

                    }
                }
                return -1;
            }
            return -1;
        }
    }

    class HookEvents
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_MOUSE_LL = 14;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_MBUTTONDOWN = 0x0207;

        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MBUTTONUP = 0x0208;

        private const int WM_MOUSEMOVE = 0x0200;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;

        private static LowLevelProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static MouseEventCallBack mc;
        private static KeyEventCallBack kc;

        public static byte[] keyPressing = new byte[256];

        public static byte mouseLPressing;
        public static byte mouseRPressing;
        public static byte mouseMPressing;

        public static MouseEventCallBack Mc { get => mc; set => mc = value; }
        public static KeyEventCallBack Kc { get => kc; set => kc = value; }

        private static IntPtr SetHookKeyBoard(LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr SetHookMouse(LowLevelProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int keyCode = Marshal.ReadInt32(lParam);

                if (wParam == (IntPtr)WM_KEYDOWN)
                {
                    if (keyPressing[keyCode] == 0) //not allow pressing
                    {
                        //Console.WriteLine("DOWN : " + keyCode);
                        keyPressing[keyCode] = 1;
                        int res = Kc(wParam, keyCode);

                        if (res == -1)
                        {
                            return (IntPtr)(-1);
                        }
                    }
                }

                if (wParam == (IntPtr)WM_KEYUP)
                {
                    //Console.WriteLine("UP : " + keyCode);
                    keyPressing[keyCode] = 0;
                }

                //if (wParam == (IntPtr)WM_LBUTTONDOWN)
                //{
                //    mouseLPressing = 1;
                //}
                //if (wParam == (IntPtr)WM_LBUTTONUP)
                //{
                //    mouseLPressing = 0;
                //    Mc((IntPtr)0, 0, 0);
                //}

                //if (wParam == (IntPtr)WM_RBUTTONDOWN)
                //{
                //    mouseRPressing = 1;
                //}
                //if (wParam == (IntPtr)WM_RBUTTONUP)
                //{
                //    mouseRPressing = 0;
                //    Mc((IntPtr)1, 0, 0);
                //}

                //if (wParam == (IntPtr)WM_MBUTTONDOWN)
                //{
                //    mouseMPressing = 1;
                //}
                //if (wParam == (IntPtr)WM_MBUTTONUP)
                //{
                //    mouseMPressing = 0;
                //    Mc((IntPtr)2, 0, 0);
                //}
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        public static void EnableHook()
        {
            _hookID = SetHookKeyBoard(_proc);
            _hookID = SetHookMouse(_proc);
        }

        public static void DisableHook()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}
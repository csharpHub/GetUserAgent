using System;
using System.Net;
using System.Management;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Win32;

namespace GetUserAgent
{
    class Program
    {
        private static string useragent;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(int hWnd, int nCmdShow);

        // Get process executable location
        private static string GetProcessExecutablePath(Process process)
        {
            try
            {
                return process.MainModule.FileName;
            } catch {
                string query = "SELECT ExecutablePath, ProcessID FROM Win32_Process";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);

                foreach (ManagementObject item in searcher.Get())
                {
                    object id = item["ProcessID"];
                    object path = item["ExecutablePath"];

                    if (path != null && id.ToString() == process.Id.ToString())
                        return path.ToString();
                }
            }
            return "";
        }

        // Get browser name
        public  static string GetDefaultBrowser()
        {
            string RegistryAssociation = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
            using (RegistryKey userChoiceKey = Registry.CurrentUser.OpenSubKey(RegistryAssociation))
            {
                if (userChoiceKey == null)
                    return null;
                
                object progIdValue = userChoiceKey.GetValue("Progid");
                if (progIdValue == null)
                    return null;

                string DefaultBrowser = progIdValue.ToString().ToLower();
                Dictionary<string, string> BrowsersList = new Dictionary<string, string>
                {
                    { "chromehtml", "chrome" },
                    { "firefoxurl", "firefox" },
                    { "operastable", "opera" },
                    { "yandexhtml", "browser" },
                    { "msedgehtm", "msedge" },
                };
                foreach (KeyValuePair<string, string> Browser in BrowsersList)
                    if (DefaultBrowser.Contains(Browser.Key))
                        return Browser.Value;

                return "iexplore";
            }
        }

        // Get browser location
        public static string GetBrowserLocation(string BrowserProcessName)
        {
            string path;
            Process process;
            Process[] processes;
            processes = Process.GetProcessesByName(BrowserProcessName);
            if (processes.Length != 0)
                return GetProcessExecutablePath(processes[0]);
            else {
                process = Process.Start(BrowserProcessName);
                ShowWindow(process.MainWindowHandle.ToInt32(), 0);
                System.Threading.Thread.Sleep(500);
                path = GetProcessExecutablePath(process);
                if (!process.HasExited)
                    process.Kill();
                else {
                    processes = Process.GetProcessesByName(BrowserProcessName);
                    path = GetProcessExecutablePath(processes[0]);
                    foreach (Process p in processes)
                        if (!p.HasExited) p.Kill();
                }
                return path;
            }
        }

        // Open url in default browser
        private static void AwaitUserAgent(string BrowserProcessName, string url)
        {
            long ticks = DateTime.Now.Ticks;
            Process process = Process.Start(BrowserProcessName, url);
            ShowWindow(process.MainWindowHandle.ToInt32(), 0);
            while (useragent == null && new TimeSpan(DateTime.Now.Ticks - ticks).TotalSeconds < 60.0)
                System.Threading.Thread.Sleep(100);
            if (!process.HasExited) process.Kill();
        }

        // Get browser version
        public static string GetBrowserVersion(string BrowserLocation)
        {
            if (BrowserLocation == null)
                return "0.0";
            return FileVersionInfo.GetVersionInfo(
                BrowserLocation.ToString()).FileVersion;
        }

        // https://github.com/borrcodes/Redline-Stealer/blob/master/RedLine.Client.Logic.Others/UserAgentDetector.cs
        public static string GetUserAgent(string BrowserProcessName)
        {
            try
            {
                int port = new Random().Next(12000, 14500);
                StartServer(port);
                AwaitUserAgent(BrowserProcessName, $"http://127.0.0.1:{port}");
                long ticks = DateTime.Now.Ticks;
                while (useragent == null && new TimeSpan(DateTime.Now.Ticks - ticks).TotalSeconds < 60.0)
                    System.Threading.Thread.Sleep(100);
            }
            catch { }
            return useragent;
        }

        // https://github.com/borrcodes/Redline-Stealer/blob/master/RedLine.Client.Logic.Others/UserAgentDetector.cs
        private static void StartServer(int port)
        {
            string[] obj = new string[1]
            {
                $"http://127.0.0.1:{port}/"
            };
            HttpListener httpListener = new HttpListener();
            string[] array = obj;
            foreach (string uriPrefix in array)
                httpListener.Prefixes.Add(uriPrefix);
            httpListener.Start();
            new System.Threading.Thread(Listen).Start(httpListener);
        }

        // https://github.com/borrcodes/Redline-Stealer/blob/master/RedLine.Client.Logic.Others/UserAgentDetector.cs
        private static void Listen(object listenerObj)
        {
            try
            {
                HttpListener obj = listenerObj as HttpListener;
                HttpListenerContext context = obj.GetContext();
                useragent = context.Request.Headers["User-Agent"];
                HttpListenerResponse response = context.Response;
                response.Redirect("https://google.com/");
                response.Close();
                obj.Stop();
            }
            catch { }
        }

        static void Main(string[] args)
        {
            string browser = GetDefaultBrowser();
            string location = GetBrowserLocation(browser);
            string version = GetBrowserVersion(location);
            string useragent = GetUserAgent(browser);

            Console.WriteLine("Browser : " + browser);
            Console.WriteLine("Location : " + location);
            Console.WriteLine("Version : " + version);
            Console.WriteLine("User-Agent : " + useragent);

            Console.ReadLine();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AndroidOperate
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private MobileInfo Mobile;
        private Process CmdProcess;
        private string CurrentScreenShotPath;
        ObservableCollection<Device> DeviceList = new ObservableCollection<Device>();
        private string CurrentDeviceId = "";
        ImageProcess imgp;
        bool localADB = false;
        public MainWindow()
        {
            InitializeComponent();
            InitCMD();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitDevice();
        }

        private void InitDevice()
        {
            var DevicesStr = RunCommand("adb.exe devices", true);
            var DevicesArr = new List<string>(DevicesStr.Split('*')).Where(m => !string.IsNullOrWhiteSpace(m));
            DeviceList.Clear();
            foreach (var item in DevicesArr)
            {
                CurrentDeviceId = item;
                var name = RunCommand("adb.exe shell getprop ro.product.model", true);
                DeviceList.Add(new Device() { Name = name, DeviceId = item });
            }
            DeviceSelBox.ItemsSource = DeviceList;
            DeviceSelBox.SelectedIndex = 0;
        }

        private void InitCMD()
        {
            CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = "cmd.exe";
            CmdProcess.StartInfo.CreateNoWindow = true;         // 不创建新窗口    
            CmdProcess.StartInfo.UseShellExecute = false;       //不启用shell启动进程  
            CmdProcess.StartInfo.RedirectStandardInput = true;  // 重定向输入    
            CmdProcess.StartInfo.RedirectStandardOutput = true; // 重定向标准输出    
            CmdProcess.StartInfo.RedirectStandardError = true;  // 重定向错误输出 
            localADB = !IsInPATH("adb.exe") && File.Exists(Environment.CurrentDirectory + "\\ADB\\adb.exe");
        }

        private static bool IsInPATH(string command)
        {
            bool isInPath = false;
            // 判断PATH中是否存在 命令
            foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                string path = test.Trim();
                if (!String.IsNullOrEmpty(path) && File.Exists(Path.Combine(path, command)))
                {
                    isInPath = true;
                    break; // 如果在PATH中找到 ，则退出循环
                }
            }

            return isInPath;
        }

        private string RunCommand(string command,bool NeedRet, bool NeedWait = false)
        {
            var IsDevices = false;
            if (command.Contains("adb.exe devices"))
            {
                IsDevices = true;
            }
            PrintCmd(command, "输入");
            CmdProcess.Start();//执行  
            if (!IsDevices)
            {
                var index = command.IndexOf("adb.exe");
                command = command.Insert(index + 7, " -s " + CurrentDeviceId);
            }
            if (localADB)
            {
                command = Environment.CurrentDirectory + "\\ADB\\" + command;
            }
            CmdProcess.StandardInput.WriteLine(command + "&exit"); //向cmd窗口发送输入信息  
            CmdProcess.StandardInput.AutoFlush = true;  //提交
            var tmp = "";
            if (NeedRet)
            {
                var result = CmdProcess.StandardOutput.ReadToEnd();
                var arr = new List<string>(result.Split(new char[] { '\\', '\r', '\\', '\n' }));
                arr = arr.Where(m => !string.IsNullOrWhiteSpace(m)).ToList();
                if (IsDevices)
                {
                    int index = 0;
                    for (int i = 0; i < arr.Count; i++)
                    {
                        if (arr[i].Contains("List of devices attached"))
                        {
                            index = i + 1;
                            break;
                        }
                    }
                    for (int i = index; i <= arr.Count-1; i++)
                    {
                        tmp += arr[i].Replace("device", "").Replace("\t", "") + "*";
                    }
                }
                else
                {
                    tmp = arr[arr.Count-1];
                }
            }
            if (NeedWait)
            {
                CmdProcess.WaitForExit();//等待程序执行完退出进程  
                CmdProcess.Close();//结束
            }
            PrintCmd(tmp, "输出");
            return tmp;
        }

        private void PrintCmd(string command, string type = "")
        {
            try
            {
                CmdRecord.Text += DateTime.Now.ToShortTimeString() + " " + type + ": " + command + "\r\n";
                CmdRecord.Focus();
                CmdRecord.SelectionStart = CmdRecord.Text.Length;
            }
            catch (Exception)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    CmdRecord.Text += DateTime.Now.ToShortTimeString() + " " + type + ": " + command + "\r\n";
                    CmdRecord.Focus();
                    CmdRecord.SelectionStart = CmdRecord.Text.Length;
                }));
            }
        }

        private void InitMobile()
        {
            Mobile = new MobileInfo();
            Mobile.AndroidVersion = RunCommand("adb.exe shell getprop ro.build.version.release", true);
            Mobile.MobileName = RunCommand("adb.exe shell getprop net.hostname", true);
            Mobile.OTAVersion = RunCommand("adb.exe shell getprop ro.build.version.ota", true);
            Mobile.ProductModel = RunCommand("adb.exe shell getprop ro.product.model", true);
            var r = RunCommand("adb.exe shell \"dumpsys window | grep mUnrestrictedScreen\"", true);
            Mobile.Resolution = r.Substring(r.Length-9, 9);
            var d = RunCommand("adb.exe shell wm density", true);
            Mobile.Density = d.Substring(d.Length - 3, 3) + "ppi";
            InitUi();
        }

        private void InitUi()
        {
            MobileName.Text = Mobile.MobileName;
            ProductModel.Text = Mobile.ProductModel;
            AndroidVersion.Text = Mobile.AndroidVersion.Length > 23 ? Mobile.AndroidVersion.Substring(0,20) + "..." : Mobile.AndroidVersion;
            AndroidVersion.ToolTip = Mobile.AndroidVersion;
            OTAVersion.Text = Mobile.OTAVersion;
            Resolution.Text = Mobile.Resolution;
            Density.Text = Mobile.Density;
            CurrentScreenShotPath = GetScreenShot();
            SetImage(MobileScreenShot, CurrentScreenShotPath);
            ImagePath.Text = CurrentScreenShotPath;
            imgp = new ImageProcess(CurrentScreenShotPath);
        }
        private string GetScreenShot()
        {
            RunCommand("adb.exe shell mkdir /sdcard/myscreenshot", false);
            var name = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png";
            RunCommand($"adb.exe shell /system/bin/screencap -p /sdcard/myscreenshot/{name}", true);
            if (!Directory.Exists("ScreenShot"))//如果不存在就创建文件夹
            {
                Directory.CreateDirectory("ScreenShot");
            }
            RunCommand($"adb.exe pull /sdcard/myscreenshot/{name} {System.Environment.CurrentDirectory + "\\ScreenShot"}", false);
            return System.Environment.CurrentDirectory + "\\ScreenShot\\" + name;
        }
        private void SetImage(System.Windows.Controls.Image image, string path)
        {
            var time = 0;
            while (!File.Exists(path) && time < 6000)
            {
                System.Threading.Thread.Sleep(10);
                time++;
            }
            if (!File.Exists(path))
            {
                return;
            }
            while (IsFileInUse(path))
            {
            }
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
            bi.EndInit();
            image.Source = bi;
        }

        private void SetColorCustom_Click(object sender, RoutedEventArgs e)
        {
            SetColorForPiont(ImagePath.Text);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            CurrentScreenShotPath = GetScreenShot();
            SetImage(MobileScreenShot, CurrentScreenShotPath);
            ImagePath.Text = CurrentScreenShotPath;
        }

        public static bool IsFileInUse(string fileName)
        {
            bool inUse = true;

            FileStream fs = null;
            try
            {

                fs = new FileStream(fileName, FileMode.Open, FileAccess.Read,

                FileShare.None);

                inUse = false;
            }
            catch
            {
            }
            finally
            {
                if (fs != null)

                    fs.Close();
            }
            return inUse;//true表示正在使用,false没有使用  
        }


        private bool IsStart = false;
        private void StartColor_Click(object sender, RoutedEventArgs e)
        {
            IsStart = true;
            Thread thread = new Thread(new ThreadStart(() => StartGame()));
            thread.IsBackground = true;
            thread.Start();
            StartColor.IsEnabled = false;
        }

        private void StartGame()
        {
            int sec = 0;
            while (IsStart)
            {
                var path = GetScreenShot();
                Dispatcher.Invoke(new Action(() =>
                {
                    CurrentScreenShotPath = path;
                    SetImage(MobileScreenShot, CurrentScreenShotPath);
                    ImagePath.Text = CurrentScreenShotPath;
                    sec = (int)SetColorForPiont(CurrentScreenShotPath);
                    RunCommand("adb.exe shell input swipe 540 1551 540 1551 " + sec, false, true);
                }));
                Thread.Sleep(sec + 333);
            }
        }

        private void StopColor_Click(object sender, RoutedEventArgs e)
        {
            IsStart = false;
            StartColor.IsEnabled = true;
        }

        private double SetColorForPiont(string path)
        {
            imgp.Init(path);
            var bmp = new Bitmap(path);
            var xx = imgp.CurrentPoint.X-1;
            if (xx >= 1080)
            {
                xx = 1070;
            }
            var yy = imgp.CurrentPoint.Y-1;
            if (yy >= 1919)
            {
                yy = 1900;
            }
            for (int i = -5; i < 5; i++)
            {
                for (int j = -5; j < 5; j++)
                {
                    bmp.SetPixel(xx + i, yy + j, Color.Red);
                }
            }

            xx = imgp.NextPoint.X-1;
            yy = imgp.NextPoint.Y-1;
            for (int i = -5; i < 5; i++)
            {
                for (int j = -5; j < 5; j++)
                {
                    bmp.SetPixel(xx + i, yy + j, Color.Red);
                }
            }
            var name = System.Environment.CurrentDirectory + "\\ScreenShot\\" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".png"; ;
            bmp.Save(name, System.Drawing.Imaging.ImageFormat.Png);
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(name, UriKind.RelativeOrAbsolute);
            bi.EndInit();
            MobileScreenShot.Source = bi;

            var len = Math.Pow(imgp.CurrentPoint.X - imgp.NextPoint.X, 2) + Math.Pow(imgp.CurrentPoint.Y - imgp.NextPoint.Y, 2);
            len = (int)(Math.Sqrt(len) * 1.39);
            var aaaa = imgp.CurrentPoint.X + " " + imgp.CurrentPoint.Y + " " + imgp.NextPoint.X + " " + imgp.NextPoint.Y;

            PrintCmd(aaaa);
            PrintCmd(len.ToString());
            return len;
        }

        private void DeviceSelBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (DeviceSelBox.SelectedItem != null)
            {
                CurrentDeviceId = (DeviceSelBox.SelectedItem as Device)?.DeviceId;
                InitMobile();
            }
        }

        private void RefreshDevice_Click(object sender, RoutedEventArgs e)
        {
            InitDevice();
        }
    }
}

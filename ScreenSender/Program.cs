using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ScreenSender
{
    internal static class Program
    {
        // 配置参数
        private const int ServerPort = 12345; // 监听端口
        private const int ScreenshotInterval = 100; // 截屏间隔(毫秒)
        private const int BufferSize = 4096; // 传输缓冲区大小
        private static TcpClient? _client; // 客户端连接
        private static NetworkStream? _clientStream; // 客户端网络流
        private static readonly Lock Lock = new(); // 线程同步锁

        // Windows截屏所需的系统API
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int xDest, int yDest, int wDest, int hDest, IntPtr hdcSrc,
            int xSrc, int ySrc, uint rop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hgdiobj);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [StructLayout(LayoutKind.Sequential)]
        private struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmDeviceName;

            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public int dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string dmFormName;

            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth; // 屏幕宽度（物理像素）
            public int dmPelsHeight; // 屏幕高度（物理像素）
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        [DllImport("user32.dll")]
        private static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);

        // 获取真实屏幕分辨率
        private static (int width, int height) GetPhysicalScreenResolution()
        {
            var devMode = new DEVMODE();
            devMode.dmSize = (short)Marshal.SizeOf(devMode);

            // 枚举主显示器的当前设置（ENUM_CURRENT_SETTINGS = -1）
            if (EnumDisplaySettings(null, -1, ref devMode))
            {
                return (devMode.dmPelsWidth, devMode.dmPelsHeight);
            }

            // 失败时回退到GetSystemMetrics
            return (
                GetSystemMetrics(SM_CXSCREEN),
                GetSystemMetrics(SM_CYSCREEN)
            );
        }


        private const int SM_CXSCREEN = 0; // 屏幕宽度
        private const int SM_CYSCREEN = 1; // 屏幕高度
        private const uint SRCCOPY = 0x00CC0020; // 像素复制模式

        private static void Main(string[] args)
        {
            Console.Title = "截图推送服务器";
            Console.WriteLine("=== 截图推送服务器 ===");
            Console.WriteLine($"监听端口: {ServerPort}");
            Console.WriteLine($"截屏间隔: {ScreenshotInterval / 1000}秒");
            Console.WriteLine("等待客户端连接...");
            Console.WriteLine("按Ctrl+C退出");
            Console.WriteLine("======================\n");

            // 捕获Ctrl+C退出事件
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n正在关闭服务器...");
                CloseClientConnection();
                e.Cancel = true;
                Environment.Exit(0);
            };
            // 启动TCP监听线程
            var listenThread = new Thread(StartListener)
            {
                IsBackground = true
            };
            listenThread.Start();

            // 启动截屏推送线程
            var pushThread = new Thread(StartScreenshotPush)
            {
                IsBackground = true
            };
            pushThread.Start();

            // 保持主线程运行
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// 启动TCP监听器，等待客户端连接
        /// </summary>
        private static void StartListener()
        {
            var listener = new TcpListener(IPAddress.Any, ServerPort);
            try
            {
                listener.Start();
                while (true)
                {
                    // 等待客户端连接
                    var newClient = listener.AcceptTcpClient();
                    lock (Lock)
                    {
                        // 关闭现有连接
                        CloseClientConnection();

                        // 保存新连接
                        _client = newClient;
                        _clientStream = _client.GetStream();
                        _client.ReceiveTimeout = 30000;
                        _client.SendTimeout = 30000;
                    }

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] 客户端已连接: {((IPEndPoint)_client.Client.RemoteEndPoint).Address}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 监听错误: {ex.Message}");
                listener.Stop();
                //Thread.Sleep(3000);
                //StartListener(); // 重试监听
            }
        }

        /// <summary>
        /// 定时截屏并推送到客户端
        /// </summary>
        private static void StartScreenshotPush()
        {
            while (true)
            {
                try
                {
                    lock (Lock)
                    {
                        if (_client is { Connected: true } && _clientStream != null)
                        {
                            // 1. 截取屏幕
                            var imageData = CaptureScreen();
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 截屏完成，大小: {FormatSize(imageData.Length)}");

                            // 2. 发送图片大小(4字节，大端模式)
                            var sizeBytes = BitConverter.GetBytes(imageData.Length);
                            if (BitConverter.IsLittleEndian) Array.Reverse(sizeBytes);
                            _clientStream.Write(sizeBytes, 0, sizeBytes.Length);

                            // 3. 发送图片数据
                            var totalSent = 0;
                            while (totalSent < imageData.Length)
                            {
                                var sendSize = Math.Min(BufferSize, imageData.Length - totalSent);
                                _clientStream.Write(imageData, totalSent, sendSize);
                                totalSent += sendSize;
                            }

                            _clientStream.Flush();
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 图片推送完成\n");
                        }
                        else
                        {
                            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 等待客户端连接...\n");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 推送错误: {ex.Message}");
                    CloseClientConnection();
                }

                // 等待下一次截屏
                Thread.Sleep(ScreenshotInterval);
            }
        }

        /// <summary>
        /// 截取屏幕图像
        /// </summary>
        private static byte[] CaptureScreen()
        {
            // 获取屏幕尺寸
            // var width = GetSystemMetrics(SM_CXSCREEN);
            // var height = GetSystemMetrics(SM_CYSCREEN);
            var (width, height) = GetPhysicalScreenResolution();
            // 创建屏幕DC和兼容位图
            var hdcScreen = GetDC(IntPtr.Zero);
            var hdcMem = CreateCompatibleDC(hdcScreen);
            var hBitmap = CreateCompatibleBitmap(hdcScreen, width, height);

            try
            {
                // 复制屏幕图像到位图
                var hOldBitmap = SelectObject(hdcMem, hBitmap);
                BitBlt(hdcMem, 0, 0, width, height, hdcScreen, 0, 0, SRCCOPY);
                SelectObject(hdcMem, hOldBitmap);

                // 转换为JPG格式
#pragma warning disable CA1416
                using var bmp = Image.FromHbitmap(hBitmap);
                using var ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Png);
#pragma warning restore CA1416
                return ms.ToArray();
            }
            finally
            {
                // 释放资源
                DeleteObject(hBitmap);
                ReleaseDC(IntPtr.Zero, hdcScreen);
                ReleaseDC(IntPtr.Zero, hdcMem);
            }
        }

        /// <summary>
        /// 获取图像编码器
        /// </summary>
        private static ImageCodecInfo GetEncoderInfo(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid)
                    return codec;
            }

            throw new Exception($"未找到{format}编码器");
        }

        /// <summary>
        /// 关闭客户端连接
        /// </summary>
        private static void CloseClientConnection()
        {
            if (_clientStream != null)
            {
                _clientStream.Dispose();
                _clientStream = null;
            }

            _clientStream?.Dispose();

            if (_client != null)
            {
                _client.Close();
                _client = null;
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端连接已关闭");
            }
        }

        /// <summary>
        /// 格式化文件大小显示
        /// </summary>
        private static string FormatSize(long bytes)
        {
            return bytes switch
            {
                < 1024 => $"{bytes} B",
                < 1024 * 1024 => $"{Math.Round(bytes / 1024.0, 2)} KB",
                _ => $"{Math.Round(bytes / (1024.0 * 1024), 2)} MB"
            };
        }
    }
}
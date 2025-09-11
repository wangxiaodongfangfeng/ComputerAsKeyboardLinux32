using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ComputerAsKeyboardInterface.Bluetooth;

// 主程序类
internal static partial class BluetoothAutoConnector
{
    // 配置常量（集中管理配置）
    private const int ScanInterval = 36; // 扫描间隔(秒)
    private const int RfcommStartPort = 0; // 起始端口
    private const int MaxRetries = 3; // 最大重试次数
    private const string LogFile = "/var/log/bluetooth_auto_connect.log"; // 日志文件
    private static readonly CancellationTokenSource Cts = new();

    public static ManualResetEventSlim AutoConnectEvent { get; set; } = new(true);

    internal static async Task MainBluetoothAutoConnect(string[] args)
    {
        //CheckRoot();
        Logger.Log("蓝牙自动连接服务启动");
        try
        {
            while (!Cts.Token.IsCancellationRequested)
            {
                AutoConnectEvent.Wait();

                Logger.Log("开始扫描已配对设备...");
                var devices = await GetPairedDevicesAsync();

                if (devices.Count == 0)
                {
                    Logger.Log("未发现已配对的蓝牙设备");
                }
                else
                {
                    Logger.Log($"发现 {devices.Count} 个已配对设备");
                    foreach (var device in devices)
                    {
                        if (device.MacAddress != null)
                            await ConnectDeviceAsync(device.MacAddress, device.Name ?? "unknown");
                    }
                }

                Logger.Log($"等待 {ScanInterval} 秒后再次扫描...");
                await Task.Delay(ScanInterval * 1000, Cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.Log("服务已取消");
        }
        catch (Exception ex)
        {
            Logger.Log($"服务出错: {ex.Message}");
            throw;
        }
    }

    // 权限检查
    private static void CheckRoot()
    {
        if (Environment.UserName == "root") return;
        Console.WriteLine("此程序需要root权限运行。请使用sudo执行。");
        Environment.Exit(1);
    }

    // 日志工具类（封装日志功能）
    private static class Logger
    {
        private static readonly object Lock = new object();

        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logEntry = $"[{timestamp}] {message}";

            // 控制台输出
            Console.WriteLine(logEntry);

            // 文件写入（线程安全）
            lock (Lock)
            {
                try
                {
                    File.AppendAllText(LogFile, logEntry + Environment.NewLine);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"日志写入失败: {ex.Message}");
                }
            }
        }
    }

    // 获取已配对设备（异步操作）
    private static async Task<List<BluetoothDevice>> GetPairedDevicesAsync()
    {
        try
        {
            var result = await ExecuteCommandAsync("bluetoothctl", "paired-devices");

            // 正则匹配设备信息
            var pattern = MyRegex();

            return (from line in result.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries)
                    select pattern.Match(line.Trim())
                    into match
                    where match.Success
                    select new BluetoothDevice { MacAddress = match.Groups[1].Value, Name = match.Groups[2].Value })
                .ToList();
        }
        catch (Exception ex)
        {
            Logger.Log($"获取已配对设备失败: {ex.Message}");
            return [];
        }
    }

    // 检查设备是否已连接
    private static async Task<bool> IsConnectedAsync(string macAddress)
    {
        try
        {
            // 检查rfcomm连接状态
            var rfcommOutput = await ExecuteCommandAsync("rfcomm", "-a");
            if (rfcommOutput.Contains(macAddress) && rfcommOutput.Contains("connected"))
                return true;

            // 检查bluetoothctl连接状态
            var btctlOutput = await ExecuteCommandAsync("bluetoothctl", $"info {macAddress}");
            return btctlOutput.Contains("Connected: yes");
        }
        catch
        {
            return false;
        }
    }

    // 检查设备是否在范围内
    private static async Task<bool> IsInRangeAsync(string macAddress)
    {
        try
        {
            // 执行l2ping命令检查可达性
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "l2ping",
                    Arguments = $"-c 1 -t 10 {macAddress}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.WaitForExitAsync(Cts.Token);
            return process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Logger.Log($"检查设备 {macAddress} 范围时出错: {ex.Message}");
            return false;
        }
    }

    // 查找可用的rfcomm端口
    private static async Task<int> FindAvailableRfcommPortAsync()
    {
        try
        {
            var output = await ExecuteCommandAsync("rfcomm", "-a");
            var usedPorts = new HashSet<int>();
            var pattern = MyRegex1();

            foreach (var line in output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
            {
                var match = pattern.Match(line);
                if (match.Success && line.Contains("connected"))
                {
                    usedPorts.Add(int.Parse(match.Groups[1].Value));
                }
            }

            // 从起始端口开始查找可用端口
            var port = RfcommStartPort;
            while (usedPorts.Contains(port))
                port++;

            return port;
        }
        catch (Exception ex)
        {
            Logger.Log($"检查可用端口时出错: {ex.Message}");
            return RfcommStartPort;
        }
    }

    // 连接设备
    private static async Task<bool> ConnectDeviceAsync(string macAddress, string name)
    {
        if (await IsConnectedAsync(macAddress))
        {
            Logger.Log($"设备 {name} ({macAddress}) 已连接，无需操作");
            return true;
        }

        if (!await IsInRangeAsync(macAddress))
        {
            Logger.Log($"设备 {name} ({macAddress}) 不在范围内，无法连接");
            return false;
        }

        var port = await FindAvailableRfcommPortAsync();
        Logger.Log($"尝试连接设备 {name} ({macAddress}) 到 rfcomm{port}");

        for (var retries = 0; retries < MaxRetries; retries++)
        {
            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "rfcomm",
                    Arguments = $"connect {port} {macAddress}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                process.Start();
                // 等待连接或超时
                var completed = process.WaitForExit(TimeSpan.FromSeconds(20));
                if (!completed)
                {
                    process.Kill();
                    Logger.Log("连接命令执行超时");
                }

                // 等待连接建立
                await Task.Delay(5000, Cts.Token);
                if (await IsConnectedAsync(macAddress))
                {
                    Logger.Log($"成功连接设备 {name} ({macAddress}) 到 rfcomm{port}");
                    return true;
                }

                Logger.Log($"连接rfcomm{port}未成功建立");
            }
            catch (Exception ex)
            {
                Logger.Log($"连接尝试 {retries + 1} 失败: {ex.Message}");
            }

            if (retries < MaxRetries - 1)
                await Task.Delay(5000, Cts.Token);
        }

        Logger.Log($"达到最大重试次数，无法连接设备 {name} ({macAddress})");
        return false;
    }

    // 执行命令并返回输出（通用命令执行工具）
    private static async Task<string> ExecuteCommandAsync(string fileName, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync(Cts.Token);

        if (!string.IsNullOrEmpty(error))
            throw new Exception($"命令执行错误: {error}");

        return output;
    }

    [GeneratedRegex(@"Device ([0-9A-Fa-f:]+) (.+)")]
    private static partial Regex MyRegex();

    [GeneratedRegex(@"rfcomm(\d+):")]
    private static partial Regex MyRegex1();
}
using System.Diagnostics;
using System.IO.Ports;
using System.Text.RegularExpressions;
using PowerArgs;
using Object = Atk.Object;

namespace ComputerAsKeyboardInterface.Bluetooth
{
    // 蓝牙设备信息类
    public class BluetoothDevice
    {
        public string? Name { get; set; }
        public string? MacAddress { get; init; }
        public bool IsOnline { get; set; }
        public bool IsConnected { get; set; }
    }

    // 映射关系管理类
    public class MappingManager
    {
        private readonly string _mappingFile = "bluetooth_serial_mappings.txt";
        private Dictionary<string, string> _mappings = new();

        // 保存映射关系
        public void SaveMappings()
        {
            try
            {
                var lines = _mappings.Select(kvp => $"{kvp.Key};{kvp.Value}");
                File.WriteAllLines(_mappingFile, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"保存映射文件出错: {ex.Message}");
            }
        }

        // 获取设备的映射串口
        public string GetMappedPort(string macAddress)
        {
            return _mappings.TryGetValue(macAddress, out var port) ? port : string.Empty;
        }

        // 更新映射关系
        public void UpdateMapping(string macAddress, string portName)
        {
            if (_mappings.ContainsKey(macAddress))
                _mappings[macAddress] = portName;
            else
                _mappings.Add(macAddress, portName);

            SaveMappings();
        }

        // 移除映射关系
        public void RemoveMapping(string macAddress)
        {
            if (_mappings.ContainsKey(macAddress))
            {
                _mappings.Remove(macAddress);
                SaveMappings();
            }
        }
    }

    // 蓝牙管理器类
    public partial class BluetoothManager
    {
        public List<string> OnLineDevices { get; } = [];

        private List<BluetoothDevice> PairedBluetoothDevices { get; } = [];

        // 获取所有已配对的蓝牙设备
        public static List<BluetoothDevice> GetPairedDevices()
        {
            var devices = new List<BluetoothDevice>();

            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "bluetoothctl",
                        Arguments = "paired-devices",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

                devices.AddRange(from line in lines
                    where line.Contains("Device")
                    select line.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                    into parts
                    where parts.Length >= 3
                    select new BluetoothDevice { MacAddress = parts[1], Name = string.Join(" ", parts.Skip(2)) });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取已配对设备出错: {ex.Message}");
            }

            return devices;
        }

        // 新增：检查特定RFCOMM端口是否被占用
        private static List<string> GetRfcommStatus()
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "rfcomm",
                        Arguments = "-a",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                var output = process.StandardOutput.ReadToEnd();

                process.WaitForExit();

                return output.Split(['\r', '\n']).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查RFCOMM端口状态时出错: {ex.Message}");
                return [];
            }
        }

        public void EnableBluetoothAutoConnect(string[] macAddresses)
        {
            var timer = new Timer(async void (o) =>
            {
                var rfCommStatus = GetRfcommStatus();

                macAddresses.ForEach(async void (m) =>
                {
                    try
                    {
                        var rfStatus = rfCommStatus.FirstOrDefault(r => r.Contains(m));
                        if (rfStatus != null)
                        {
                            if (!rfStatus.Contains("closed")) return;
                            var matches = RfcommReg().Matches(rfStatus);
                            if (matches.Count <= 0) return;
                            var comName = matches.First().Groups["name"];
                            if (!File.Exists($"/dev/{comName}")) return;
                            var serialPort = SerialPortExtension.GetSerialPort($"/dev/{comName}");
                            serialPort?.Close();
                            serialPort?.Dispose();
                            await Task.Delay(200);
                            File.Delete($"/dev/{comName}");
                            await Task.Delay(200);
                        }

                        if (OnLineDevices.Any(o => o.Contains(m)))
                            _ = BluetoothManager2.ConnectRfcommPort(m, FindAvailableRfComPort());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("connect bluetooth failed");
                    }
                });
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));
        }

        [GeneratedRegex("(?<name>rfcomm\\d+):", RegexOptions.Multiline)]
        private static partial Regex RfcommReg();

        public void EnableBluetoothAutoDetect()
        {
            OnLineDevices.Clear();

            var timer = new Timer(async void (o) =>
            {
                try
                {
                    OnLineDevices.Clear();
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "bluetoothctl",
                            Arguments = $"scan on",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();

                    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await Task.Run(async () =>
                    {
                        while (!cts.IsCancellationRequested)
                        {
                            var availableDevice = await process.StandardOutput.ReadLineAsync(cts.Token);
                            if (availableDevice != null) OnLineDevices.Add(availableDevice);
                            await Task.Delay(300, cts.Token);
                        }
                    }, cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Start bluetooth scan failed");
                }
            }, null, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(60));
        }


        // 检查设备是否在线
        public bool IsDeviceOnline(string macAddress)
        {
            return OnLineDevices.Any(o => o.Contains(macAddress));
        }

        public static int FindAvailableRfComPort()
        {
            for (var i = 0; i < 30; i++)
            {
                if (!File.Exists("/dev/rfcomm{i}")) return i;
            }

            return -1;
        }

        public static List<int> FindAvailableRfComPorts(int demands)
        {
            var result = new List<int>();
            var tokens = 0;
            for (var i = 0; i < 30; i++)
            {
                if (File.Exists("/dev/rfcomm{i}")) continue;
                result.Add(i);
                tokens++;
                if (tokens == demands) break;
            }

            return result.Count < demands ? [] : result;
        }

        /// <summary>
        /// 执行Linux命令
        /// </summary>
        /// <param name="command">要执行的命令</param>
        /// <param name="useSudo">是否使用sudo权限</param>
        /// <param name="sudoPassword">sudo密码（如果需要）</param>
        /// <returns>包含执行结果的元组 (是否成功, 输出信息, 错误信息)</returns>
        private static async Task<(bool Success, string Output, string Error)> ExecuteCommand(
            string command,
            bool useSudo = false,
            string sudoPassword = "")
        {
            if (string.IsNullOrEmpty(command))
            {
                return (false, "", "命令不能为空");
            }

            // 构建完整命令
            var fullCommand = useSudo ? $"sudo {command}" : command;

            // 创建进程信息
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{EscapeBashCommand(fullCommand)}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = useSudo && !string.IsNullOrEmpty(sudoPassword),
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;

            try
            {
                process.Start();

                // 如果需要sudo密码，自动输入
                if (useSudo && !string.IsNullOrEmpty(sudoPassword))
                {
                    await process.StandardInput.WriteLineAsync(sudoPassword);
                    await process.StandardInput.FlushAsync();
                }

                // 异步读取输出和错误
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                // 等待进程完成
                if (!process.WaitForExit(30000)) // 30秒超时
                {
                    process.Kill();
                    return (false, "", "命令执行超时");
                }

                // 获取结果
                var output = await outputTask;
                var error = await errorTask;

                // 检查退出代码（0表示成功）
                var success = process.ExitCode == 0;

                return (success, output, error);
            }
            catch (Exception ex)
            {
                return (false, "", $"执行命令时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 执行rfcomm连接命令
        /// </summary>
        /// <param name="channel">RFCOMM通道</param>
        /// <param name="macAddress">蓝牙设备MAC地址</param>
        /// <param name="sudoPassword">sudo密码</param>
        /// <returns>执行结果</returns>
        public async Task<(bool Success, string Output, string Error)> ConnectRfcomm(
            int channel,
            string macAddress,
            string sudoPassword = "")
        {
            if (channel is < 0 or > 30) // RFCOMM通道通常在0-30范围内
            {
                return (false, "", "无效的RFCOMM通道号（0-30）");
            }

            if (string.IsNullOrEmpty(macAddress) || !IsValidMacAddress(macAddress))
            {
                return (false, "", "无效的MAC地址");
            }

            var command = $"rfcomm connect {channel} {macAddress}";
            return await ExecuteCommand(command, true, sudoPassword);
        }

        /// <summary>
        /// 释放所有RFCOMM连接
        /// </summary>
        public async Task<(bool Success, string Output, string Error)> ReleaseAllRfcomm(string sudoPassword = "")
        {
            return await ExecuteCommand("rfcomm release all", true, sudoPassword);
        }

        /// <summary>
        /// 验证MAC地址格式
        /// </summary>
        private static bool IsValidMacAddress(string macAddress)
        {
            // 简单验证MAC地址格式（如AA:BB:CC:DD:EE:FF）
            if (string.IsNullOrEmpty(macAddress))
                return false;

            var parts = macAddress.Split(':');
            return parts.Length == 6 && parts.All(p => p.Length == 2);
        }

        /// <summary>
        /// 转义bash命令中的特殊字符
        /// </summary>
        private static string EscapeBashCommand(string command)
        {
            // 转义双引号和其他特殊字符
            return command.Replace("\"", "\\\"")
                .Replace("$", "\\$")
                .Replace("`", "\\`")
                .Replace("!", "\\!");
        }
    }
}
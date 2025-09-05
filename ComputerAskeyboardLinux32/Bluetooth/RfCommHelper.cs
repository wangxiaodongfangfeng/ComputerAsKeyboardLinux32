using System.Diagnostics;

namespace ComputerAsKeyboardInterface.Bluetooth
{
    internal class BluetoothManager2
    {
        // 新增：释放所有RFCOMM端口
        public void ReleaseRfcommPorts()
        {
            try
            {
                Console.WriteLine("释放所有RFCOMM端口...");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "rfcomm",
                        Arguments = "release all",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Verb = "runas" // 请求管理员权限
                    }
                };

                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine("RFCOMM端口已成功释放");
                }
                else
                {
                    Console.WriteLine("释放RFCOMM端口失败，可能需要管理员权限");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"释放RFCOMM端口时出错: {ex.Message}");
            }
        }

        // 新增：检查特定RFCOMM端口是否被占用
        private static bool IsRfcommPortInUse(int port)
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

                return output.Contains($"rfcomm{port}:");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"检查RFCOMM端口状态时出错: {ex.Message}");
                return false;
            }
        }

        // 新增：绑定RFCOMM端口到设备
        public static async Task<bool> BindRfcommPort(string macAddress, int port)
        {
            try
            {
                // 先检查端口是否已被占用，如果是则释放
                if (IsRfcommPortInUse(port))
                {
                    Console.WriteLine($"RFCOMM端口 {port} 已被占用，尝试释放...");
                    var releaseProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = "rfcomm",
                            Arguments = $"release {port}",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            Verb = "runas"
                        }
                    };
                    releaseProcess.Start();
                    await releaseProcess.WaitForExitAsync();
                }

                Console.WriteLine($"绑定RFCOMM端口 {port} 到设备 {macAddress}...");

                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "rfcomm",
                        Arguments = $"connect {port} {macAddress}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        Verb = "runas"
                    }
                };

                process.Start();
                await process.WaitForExitAsync();

                var success = process.ExitCode == 0;
                Console.WriteLine(success ? $"成功将RFCOMM端口 {port} 绑定到设备 {macAddress}" : $"绑定RFCOMM端口 {port} 失败");
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"绑定RFCOMM端口时出错: {ex.Message}");
                return false;
            }
        }


        public static async Task<bool> ConnectRfcommPort(string macAddress, int port)
        {
            var result =
                await LinuxCommandHelper
                    .ExecuteCommandInBackgroundAsync($"rfcomm connect {port} {macAddress}",
                        onOutputReceived: (output) =>
                        {
                            // disconnected
                            if (!output.Contains("Disconnected")) return;
                            var portPath = $"/dev/rfcomm{port}";
                            RemoveSerialPort(portPath);
                        }
                    );
            RemoveSerialPort($"/dev/rfcomm{port}");
            // disconnected
            return true;

            void RemoveSerialPort(string portPath)
            {
                var serialPort = SerialPortExtension.GetSerialPort(portPath);
                if (serialPort is not { IsOpen: true }) return;
                SerialPortExtension.RemoveSerialPort(portPath);
                serialPort.Close();
                serialPort.Dispose();
                Task.Delay(1000).Wait(TimeSpan.FromSeconds(1));
                if (File.Exists(portPath)) File.Delete(portPath);
            }
        }


        // // 改进的连接方法，添加RFCOMM端口处理
        // public static (bool Success, string ErrorMessage, string RfcommPort) ConnectDeviceWithRfcomm(string macAddress)
        // {
        //     // 尝试连接设备
        //     //var connectResult = ConnectDevice(macAddress);
        //     // if (!connectResult.Success)
        //     // {
        //     //     return (false, connectResult.ErrorMessage, string.Empty);
        //     // }
        //
        //     // 尝试绑定RFCOMM端口（从1开始尝试）
        //     for (int port = 1; port <= 10; port++)
        //     {
        //         if (BindRfcommPort(macAddress, port))
        //         {
        //             string rfcommPort = $"/dev/rfcomm{port}";
        //             return (true, string.Empty, rfcommPort);
        //         }
        //     }
        //
        //     // 如果所有端口都失败，尝试释放所有端口后再试一次
        //     ReleaseRfcommPorts();
        //     Thread.Sleep(2000);
        //     
        //     if (BindRfcommPort(macAddress, 1))
        //     {
        //         string rfcommPort = "/dev/rfcomm1";
        //         return (true, string.Empty, rfcommPort);
        //     }
        //
        //     return (false, "无法绑定RFCOMM端口，所有端口可能都被占用", string.Empty);
        // }
        //
        // // 保持其他现有方法...
    }
}
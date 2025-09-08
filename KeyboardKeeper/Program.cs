// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KeyboardKeeper;

internal class XInputTcpServer
{
    private const int Port = 9869;
    private TcpListener? _server;
    private bool _isRunning;

    private async Task StartAsync(CancellationToken token)
    {
        _isRunning = true;
        _server = new TcpListener(IPAddress.Any, Port);
        _server.Start();
        Console.WriteLine($"XInput控制服务器已启动，监听端口 {Port}...");
        Console.WriteLine("等待客户端连接...");
        while (!token.IsCancellationRequested)
        {
            try
            {
                // 异步等待客户端连接
                var client = await _server.AcceptTcpClientAsync(token);
                Console.WriteLine("客户端已连接");
                _ = Task.Run(() => HandleClient(client), token);
            }
            catch (Exception ex)
            {
                if (_isRunning) Console.WriteLine($"接受客户端连接时出错: {ex.Message}");
            }

            await Task.Delay(300, token);
        }
    }

    private static void HandleClient(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];
            int bytesRead;

            // 读取客户端发送的命令
            while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                var command = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                Console.WriteLine($"收到命令: {command}");

                // 处理命令并获取结果
                var result = ProcessCommand(command);

                // 发送响应给客户端
                var response = Encoding.UTF8.GetBytes(result);
                stream.Write(response, 0, response.Length);

                // 如果是退出命令，关闭连接
                if (command.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"处理客户端时出错: {ex.Message}");
        }
        finally
        {
            client.Close();
            Console.WriteLine("客户端已断开连接");
        }
    }

    private static string ProcessCommand(string command)
    {
        // 命令格式应该是: xinput [disable|enable] [设备ID]
        var parts = command.Split([' '], StringSplitOptions.RemoveEmptyEntries);

        // 验证命令格式
        if (parts.Length != 3 || !parts[0].Equals("xinput", StringComparison.OrdinalIgnoreCase))
        {
            return "无效命令格式。正确格式: xinput [disable|enable] [设备ID]";
        }

        var operation = parts[1];
        if (operation != "disable" && operation != "enable")
        {
            return "无效操作。请使用 disable 或 enable";
        }

        return !int.TryParse(parts[2], out var deviceId)
            ? "无效的设备ID。请提供整数ID"
            : ExecuteXInputCommand(operation, deviceId); // 执行xinput命令
    }

    private static string ExecuteXInputCommand(string operation, int deviceId)
    {
        try
        {
            // 创建进程启动信息
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "xinput",
                Arguments = $"{operation} {deviceId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // 执行命令
            using var process = System.Diagnostics.Process.Start(startInfo);
            var output = process?.StandardOutput.ReadToEnd();
            var error = process?.StandardError.ReadToEnd();
            process?.WaitForExit();

            return process is { ExitCode: 0 }
                ? $"成功{operation}设备 {deviceId}"
                : $"执行命令失败 (错误代码: {process!.ExitCode}): {error}";
        }
        catch (Exception ex)
        {
            return $"执行命令时发生错误: {ex.Message}";
        }
    }

    internal static async Task Main(string[] args)
    {
        var server = new XInputTcpServer();
        var cts = new CancellationTokenSource();
        try
        {
            await server.StartAsync(cts.Token);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"服务器错误: {ex.Message}");
            await cts.CancelAsync();
        }
    }
}
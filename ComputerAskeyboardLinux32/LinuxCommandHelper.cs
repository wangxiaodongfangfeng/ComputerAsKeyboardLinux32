using System.Diagnostics;
using System.Text;
using ComputerAsKeyboardInterface.Bluetooth;

namespace ComputerAsKeyboardInterface;

public static class LinuxCommandHelper
{
    /// <summary>
    /// 执行Linux命令并返回结果
    /// </summary>
    /// <param name="command">要执行的命令</param>
    /// <param name="workingDirectory">工作目录，默认为当前目录</param>
    /// <param name="timeoutMilliseconds">超时时间（毫秒），默认30000毫秒</param>
    /// <returns>包含执行结果的CommandResult对象</returns>
    public static async Task<CommandResult> ExecuteCommandAsync(
        string command,
        string? workingDirectory = null,
        int timeoutMilliseconds = 30000)
    {
        if (string.IsNullOrEmpty(command))
        {
            throw new ArgumentException("命令不能为空", nameof(command));
        }

        var result = new CommandResult();

        using var process = new Process();
        try
        {
            // 配置进程信息
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{EscapeCommand(command)}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // 设置工作目录
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            // 用于存储输出的StringBuilder
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // 异步处理输出
            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    outputBuilder.AppendLine(e.Data);
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    errorBuilder.AppendLine(e.Data);
                }
            };

            // 启动进程
            process.Start();

            // 开始异步读取输出
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await Task.Delay(1000);
            // 等待进程完成或超时
            if (await Task.Run(() => process.WaitForExit(timeoutMilliseconds)))
            {
                // 进程正常退出
                result.ExitCode = process.ExitCode;
                result.Output = outputBuilder.ToString().TrimEnd();
                result.Error = errorBuilder.ToString().TrimEnd();
                result.Success = process.ExitCode == 0;
            }
            else
            {
                result.Output = outputBuilder.ToString().TrimEnd();
                // 超时，终止进程
                process.Kill();
                result.Success = false;
                result.Error = $"命令执行超时（{timeoutMilliseconds}毫秒）";
                result.ExitCode = -1;
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"执行命令时发生错误: {ex.Message}";
            result.ExitCode = -2;
        }

        return result;
    }

    /// <summary>
    /// 执行Linux命令并返回结果
    /// </summary>
    /// <param name="command">要执行的命令</param>
    /// <param name="workingDirectory">工作目录，默认为当前目录</param>
    /// <param name="onOutputReceived"></param>
    /// <param name="onErrorReceived"></param>
    /// <returns>包含执行结果的CommandResult对象</returns>
    public static async Task<CommandResult> ExecuteCommandInBackgroundAsync(
        string command,
        string? workingDirectory = null,
        Action<string>? onOutputReceived = null,
        Action<string>? onErrorReceived = null)
    {
        if (string.IsNullOrEmpty(command))
        {
            throw new ArgumentException("命令不能为空", nameof(command));
        }

        var result = new CommandResult();

        using var process = new Process();
        try
        {
            // 配置进程信息
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"-c \"{EscapeCommand(command)}\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // 设置工作目录
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                process.StartInfo.WorkingDirectory = workingDirectory;
            }

            // 用于存储输出的StringBuilder
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // 异步处理输出
            process.OutputDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                BluetoothManager.LogInfo(e.Data);
                outputBuilder.AppendLine(e.Data);
                onOutputReceived?.Invoke(e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (string.IsNullOrEmpty(e.Data)) return;
                BluetoothManager.LogInfo(e.Data);
                errorBuilder.AppendLine(e.Data);
                onErrorReceived?.Invoke(e.Data);
            };

            // 启动进程
            process.Start();

            // 开始异步读取输出
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await Task.Delay(1000);

            await process.WaitForExitAsync();
            BluetoothManager.LogInfo("Exist of Executing in background");
            result.ExitCode = process.ExitCode;
            result.Output = outputBuilder.ToString().TrimEnd();
            result.Error = errorBuilder.ToString().TrimEnd();
            result.Success = process.ExitCode == 0;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = $"执行命令时发生错误: {ex.Message}";
            result.ExitCode = -2;
        }

        return result;
    }


    /// <summary>
    /// 同步执行Linux命令并返回结果
    /// </summary>
    /// <param name="command">要执行的命令</param>
    /// <param name="workingDirectory">工作目录，默认为当前目录</param>
    /// <param name="timeoutMilliseconds">超时时间（毫秒），默认30000毫秒</param>
    /// <returns>包含执行结果的CommandResult对象</returns>
    public static CommandResult ExecuteCommand(
        string command,
        string? workingDirectory = null,
        int timeoutMilliseconds = 30000)
    {
        return ExecuteCommandAsync(command, workingDirectory, timeoutMilliseconds).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 转义命令中的特殊字符，防止命令注入
    /// </summary>
    private static string EscapeCommand(string command)
    {
        // 转义双引号和反斜杠
        return command.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}

/// <summary>
/// 命令执行结果
/// </summary>
public class CommandResult
{
    /// <summary>
    /// 命令是否执行成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 命令输出
    /// </summary>
    public string? Output { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? Error { get; set; }

    /// <summary>
    /// 退出代码
    /// </summary>
    public int ExitCode { get; set; }

    public override string ToString()
    {
        return $"Success: {Success}, ExitCode: {ExitCode}, Output: {Output}, Error: {Error}";
    }
}

public class LinuxCommandChecker
{
    // 判断命令是否存在
    public static bool IsCommandExists(string command)
    {
        // 可替换为 "command -v " + command，两者效果类似
        var checkCommand = "which " + command;

        using var process = new Process();
        process.StartInfo.FileName = "/bin/bash";
        process.StartInfo.Arguments = "-c \"" + checkCommand + "\"";
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        process.Start();
        // 读取输出（包含路径则存在）
        var output = process.StandardOutput.ReadToEnd();
        // 等待进程结束
        process.WaitForExit();

        // 输出非空且退出码为 0，说明命令存在
        return !string.IsNullOrEmpty(output) && process.ExitCode == 0;
    }
}
namespace TcpFileTransferServer;

using System;
using System.Diagnostics;
using System.Text;

public class GitCommandHelper(string gitPath = @"C:\Program Files\Git\bin\git.exe")
{
  // Git可执行文件路径（默认从系统环境变量中查找）

  /// <summary>
  /// 在指定目录执行Git命令
  /// </summary>
  /// <param name="workingDirectory">Git仓库目录（工作目录）</param>
  /// <param name="command">Git命令（如"commit -m 'message'"）</param>
  /// <param name="timeout">超时时间（毫秒），默认30秒</param>
  /// <returns>包含命令输出和执行结果的对象</returns>
  public GitCommandResult ExecuteGitCommand(string workingDirectory, string command, int timeout = 30000)
  {
    // 验证工作目录是否存在
    if (!System.IO.Directory.Exists(workingDirectory))
    {
      return new GitCommandResult(false, $"Working directory does not exist: {workingDirectory}", string.Empty);
    }

    // 配置进程启动信息
    var startInfo = new ProcessStartInfo
    {
      FileName = gitPath,
      Arguments = command,
      WorkingDirectory = workingDirectory, // 设置Git工作目录
      RedirectStandardOutput = true, // 重定向标准输出
      RedirectStandardError = true, // 重定向错误输出
      UseShellExecute = false, // 必须为false才能重定向输出
      CreateNoWindow = true, // 不显示命令行窗口
      StandardOutputEncoding = Encoding.UTF8,
      StandardErrorEncoding = Encoding.UTF8
    };

    using var process = new Process();
    process.StartInfo = startInfo;
    try
    {
      // 启动进程
      process.Start();

      // 异步读取输出（避免死锁）
      var output = process.StandardOutput.ReadToEnd();
      var error = process.StandardError.ReadToEnd();

      // 等待进程完成并检查超时
      var completed = process.WaitForExit(timeout);
      if (!completed)
      {
        process.Kill(); // 超时则终止进程
        return new GitCommandResult(false, $"Executing command timeout （exceed{timeout}ms）", output);
      }

      // 判断执行结果（0表示成功，非0表示失败）
      var success = process.ExitCode == 0;
      return new GitCommandResult(success, success ? "executed command successfully" : error, output);
    }
    catch (Exception ex)
    {
      return new GitCommandResult(false, $"executed command with error: {ex.Message}", string.Empty);
    }
  }

  /// <summary>
  /// 解析git log输出
  /// </summary>
  public static List<GitFileChange> ParseGitOutput(string workDirectory, string output)
  {
    var changes = new List<GitFileChange>();
    if (string.IsNullOrEmpty(output))
      return changes;

    // 按行分割输出
    var lines = output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

    foreach (var line in lines)
    {
      // 行格式：[状态][空格][文件路径][(可选) -> 目标路径]
      // 例如："M src/Program.cs" 或 "R100 src/Old.cs -> src/New.cs"

      if (line == "''") continue;
      // 找到第一个空格的位置
      var firstSpaceIndex = line.IndexOf('\t');
      if (firstSpaceIndex <= 0)
        continue;
      var outputs = line.Split('\t');
      // 处理重命名/复制的情况（包含多个内容）
      if (outputs.Length == 3)
      {
        changes.Add(new GitFileChange
        {
          ChangeType = outputs[0].First(),
          SourcePath = Path.Combine(workDirectory, outputs[1]),
          FilePath = Path.Combine(workDirectory, outputs[2])
        });
      }
      else
      {
        // 普通变更
        changes.Add(new GitFileChange
        {
          ChangeType = outputs[0].First(),
          FilePath = Path.Combine(workDirectory, outputs[1])
        });
      }
    }

    return changes;
  }


  public static async Task<List<GitFileChange>> GetMergedGitFileChanges(long ticks, string workDirectory)
  {
    var datetime = new DateTime(ticks, DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    var output = string.Empty;
    var fileOperations = new List<GitFileChange>();
    await Task.Run(() =>
    {
      var helper = new GitCommandHelper();
      var result = helper.ExecuteGitCommand(workDirectory,
        $"log --since=\"{datetime}\" --name-status --pretty=format:''");

      if (result.Success)
      {
        output = result.Output;
      }
    });
    if (string.IsNullOrEmpty(output)) return fileOperations;
    fileOperations = ParseGitOutput(workDirectory, output);
    var aggregate = new Dictionary<string, string>();
    fileOperations.Reverse();
    fileOperations.ForEach(op =>
    {
      switch (op.ChangeType)
      {
        case 'M':
        case 'A':
        case 'D':
          aggregate[op.FilePath] = op.ChangeType.ToString();
          break;
        case 'R':
          if (op.SourcePath != null) aggregate[op.SourcePath] = "D";
          aggregate[op.FilePath] = "A";
          break;
      }
    });
    fileOperations = aggregate.Select(a => new GitFileChange()
    {
      ChangeType = a.Value.First(),
      FilePath = a.Key
    }).ToList();

    return fileOperations;
  }
}

/// <summary>
/// Git命令执行结果
/// </summary>
public class GitCommandResult(bool success, string errorMessage, string output)
{
  public bool Success { get; } = success; // 是否执行成功
  public string ErrorMessage { get; } = errorMessage; // 错误信息（成功时为描述信息）
  public string Output { get; } = output; // 命令输出内容
}

public class GitFileChange
{
  // 变更类型：A(添加)、D(删除)、M(修改)、R(重命名)、C(复制)
  public char ChangeType { get; set; }

  // 文件路径
  public string FilePath { get; set; } = "";

  // 重命名/复制的源文件路径（仅R/C类型有效）
  public string? SourcePath { get; set; }
}
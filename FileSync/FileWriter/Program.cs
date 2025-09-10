// FileWriter/Program.cs

using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SyncFileLib;

namespace FileWriter
{
  public static class Program
  {
    private const string MetadataSeparator = "\n";
    private static readonly JsonSerializerOptions? JsonOptions = new() { WriteIndented = false };

    public static async Task Main(string[] args)
    {
      try
      {
        // 仅接收管道句柄（元数据从管道读取）
        if (args.Length != 2)
        {
          Console.WriteLine("用法: FileWriter <管道句柄> <相对路径>");
          Environment.Exit(1);
        }

        var pipeHandle = args[0];
        Console.WriteLine("[子进程] 启动成功，连接管道...");

        // 连接主进程的管道（长连接）
        await using var pipeClient = new AnonymousPipeClientStream(PipeDirection.In, pipeHandle);
        Console.WriteLine("[子进程] 管道连接成功，等待文件元数据...");
#if DEBUG
        await Task.Delay(10000);
#endif

        await pipeClient.PullFilesIntoDirectory(args[1], CancellationToken.None);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[子进程] 异常: {ex.Message}");
        Environment.Exit(1);
      }
    }
  }
}
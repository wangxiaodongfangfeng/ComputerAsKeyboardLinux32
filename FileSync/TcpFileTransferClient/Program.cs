// FileWriter/Program.cs

using System.CommandLine;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using SyncFileLib;
using TcpFileTransferServer;

namespace TcpFileTransferClient
{
  public static class Program
  {
    private const string MetadataSeparator = "\n";
    private static readonly string TargetPath = Directory.GetCurrentDirectory();
    private static string? Host { get; set; }
    private static int Port { get; set; }
    private static string? UserName { get; set; }
    private static string? Password { get; set; }
    private static string? Project { get; set; }

    public static async Task<int> Main(string[] args)
    {
      var rootCommand = new RootCommand("a tool to sync file from remote pc");
      rootCommand.WithInitCommand();
      rootCommand.WithRemoteCommand().WithGitCommand();
      rootCommand.WithPullCommand();
      rootCommand.WithPushCommand();
      rootCommand.WithDiffCommand(TargetPath);
      try
      {
        var parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[子进程] 异常: {ex.Message}");
        return 1;
      }
    }

    private static async Task WriteHandShakeAsync(Stream stream, CancellationToken token)
    {
      var bytes = "\0\0\0\0\0\0\0\0"u8.ToArray();
      await stream.WriteAsync(bytes, token);
      await stream.WriteAsync(Encoding.UTF8.GetBytes($"{UserName}:{Password}/{Project}{MetadataSeparator}"), token);
    }

    private static async Task<Stream> OpenNetworkStreamAsync(CancellationToken token)
    {
      var pipeClient = new TcpClient();
      await pipeClient.ConnectAsync(Host!, Port, token);
      var stream = pipeClient.GetStream();
      return stream;
    }

    private static async Task<int> PreCheckConfiguration()
    {
      var workingDirectory = Directory.GetCurrentDirectory();
      var metaDir = Path.Combine(workingDirectory, ".syc");

      if (!Directory.Exists(metaDir))
      {
        Console.WriteLine("No meta data exists, please run 'syc init --port xxx --host xxx' to init the folder ");
        return 1;
      }

      var remote = await File.ReadAllTextAsync(Path.Combine(metaDir, "remote"));
      var rex = new Regex(
        "^(?<user>[a-zA-Z0-9]+):(?<password>[^/]+)/(?<host>[^:]+):(?<port>\\d+)/(?<project>[a-zA-Z0-9]+)$");

      if (!rex.Match(remote).Success)
      {
        Console.WriteLine("Invalid remote remote path.");
        return 1;
      }

      UserName = rex.Match(remote).Groups["user"].Value;
      Password = rex.Match(remote).Groups["password"].Value;
      Port = int.Parse(rex.Match(remote).Groups["port"].Value);
      Host = rex.Match(remote).Groups["host"].Value;
      Project = rex.Match(remote).Groups["project"].Value;

      return 0;
    }

    private static async Task PushFiles(Stream stream, long ticks = 0, bool all = false)
    {
      var now = DateTime.UtcNow.Ticks.ToString();
      var content = await File.ReadAllTextAsync(Path.Combine(TargetPath, ".syc/since_local.sy"));
      var parsed = long.TryParse(content, out var time);
      ticks = ticks != 0 ? ticks : (parsed ? time : 0);
      var result = await stream.PushFilesFromDirectory(TargetPath, all ? 0 : ticks);
      if (result)
        await File.WriteAllTextAsync(Path.Combine(TargetPath, ".syc/since_local.sy"), now);
    }

    private static async Task PullAllFiles(Stream stream)
    {
      var now = DateTime.UtcNow.Ticks.ToString();
      await stream.PullFilesIntoDirectory(TargetPath, CancellationToken.None);
      await File.WriteAllTextAsync(Path.Combine(TargetPath, ".syc/since.sy"), now);
    }

    private static Command WithPushCommand(this Command parentCommand)
    {
      var noRequiredSinceTime = new Option<DateTime>("--since-time", "-st")
      {
        Required = false,
        DefaultValueFactory = (result) => DateTime.MinValue
      };
      var allOption = new Option<bool>("--all", "-a") { Required = false };
      var decryptOption = new Option<bool>("--decrypt", "-d");
      var pushCommand = new Command("push", "push changes since last push action executed time")
      {
        noRequiredSinceTime,
        decryptOption,
        allOption
      };
      pushCommand.SetAction(async (result, token) =>
      {
        if (await PreCheckConfiguration() == 1) return 1;
        await using var stream = await OpenNetworkStreamAsync(token);
        Console.WriteLine("[子进程] 启动成功，连接管道...");
        var customizeTime = noRequiredSinceTime.HaveDataInCommandLine(result);
        var inputTime = customizeTime ? result.GetValue(noRequiredSinceTime) : DateTime.MinValue;

        // 连接子进程的管道（长连接）
        Console.WriteLine("[子进程] 管道连接成功，等待文件元数据...");
        var decrypt = result.GetValue(decryptOption);
        var all = result.GetValue(allOption);
        await WriteHandShakeAsync(stream, token);
        await stream.WriteAsync(
          Encoding.UTF8.GetBytes(
            $"push{(decrypt ? " --decrypt" : "")} --project {Project} {MetadataSeparator}"), token);

        await PushFiles(stream, all ? 0 : customizeTime ? inputTime.Ticks : 0, all);
        return 0;
      });
      parentCommand.Add(pushCommand);
      return pushCommand;
    }

    private static Command WithPullCommand(this Command parentCommand)
    {
      var workingDirectory = Directory.GetCurrentDirectory();
      var metaDir = Path.Combine(workingDirectory, ".syc");
      var noRequiredSinceTime = new Option<DateTime>("--since-time", "-st")
      {
        Required = false,
        DefaultValueFactory = (result) => DateTime.MinValue
      };
      var allOption = new Option<bool>("--all", "-a") { Required = false };
      var pullCommand = new Command("pull", "pull all changes since last pull action executed time")
      {
        noRequiredSinceTime,
        allOption
      };
      pullCommand.SetAction(async (result, token) =>
      {
        Console.WriteLine("[子进程] 启动成功，连接管道...");
        if (await PreCheckConfiguration() == 1) return 1;
        var customizeTime = noRequiredSinceTime.HaveDataInCommandLine(result);
        var inputTime = customizeTime ? result.GetValue(noRequiredSinceTime) : DateTime.MinValue;

        var all = result.GetValue(allOption);
        // 连接子进程的管道（长连接）
        await using var stream = await OpenNetworkStreamAsync(token);
        var content = await File.ReadAllTextAsync(Path.Combine(metaDir, "since.sy"), token);
        var parseResult = long.TryParse(content, out var time);
        await WriteHandShakeAsync(stream, token);
        await stream.WriteAsync(
          Encoding.UTF8.GetBytes(
            $"pull --project {Project}{(all ? " --all" : "")} --ticks {(customizeTime ? inputTime.Ticks : parseResult ? time : 0)}{MetadataSeparator}"),
          token);
        Console.WriteLine("[子进程] 管道连接成功，等待文件元数据...");
        await PullAllFiles(stream);
        return 0;
      });
      parentCommand.Add(pullCommand);
      return pullCommand;
    }

    private static Command WithGitCommand(this Command parentCommand)
    {
      var git = new Command("git")
      {
        TreatUnmatchedTokensAsErrors = false
      };
      git.SetAction(async (result, token) =>
      {
        if (await PreCheckConfiguration() == 1) return 1;
        // 连接子进程的管道（长连接）
        await using var stream = await OpenNetworkStreamAsync(token);
        await WriteHandShakeAsync(stream, CancellationToken.None);

        var vls = result.Tokens.Select(t => t.Value).ToList().Select(a => a.Contains(' ') ? $"\"{a}\"" : a).ToArray();
        await stream.WriteAsync(
          Encoding.UTF8.GetBytes(
            $"{string.Join(' ', vls)}{MetadataSeparator}"),
          CancellationToken.None);
        await stream.FlushAsync(token);
        await ReadAllStream(stream);
        return 0;
      });
      parentCommand.Add(git);
      return git;
    }

    private static Command WithRemoteCommand(this Command parentCommand)
    {
      var remote = new Command("remote");
      parentCommand.Add(remote);
      return remote;
    }

    private static Command WithInitCommand(this Command parentCommand)
    {
      var workingDirectory = Directory.GetCurrentDirectory();
      var metaDir = Path.Combine(workingDirectory, ".syc");
      var hostOption = new Option<string>("--host") { Required = true };
      var portOption = new Option<int>("--port") { Required = true };
      var projectOption = new Option<string>("--project") { Required = true };
      var userOption = new Option<string>("--user") { Required = true };
      var passwordOption = new Option<string>("--password") { Required = true };

      var initcommand = new Command("init", "init syc with create .syc folder and files")
      {
        hostOption,
        portOption,
        projectOption,
        userOption,
        passwordOption
      };
      parentCommand.Add(initcommand);
      initcommand.SetAction(async (result, token) =>
      {
        var port = result.GetValue(portOption);
        var host = result.GetValue(hostOption);
        var project = result.GetValue(projectOption);
        var user = result.GetValue(userOption);
        var password = result.GetValue(passwordOption);
        Directory.CreateDirectory(metaDir);
        await File.WriteAllTextAsync(Path.Combine(metaDir, "remote"), $"{user}:{password}/{host}:{port}/{project}",
          token);
        await File.WriteAllTextAsync(Path.Combine(metaDir, "since.sy"), "0", token);
        await File.WriteAllTextAsync(Path.Combine(metaDir, "since_local.sy"), "0", token);
        await File.WriteAllTextAsync(Path.Combine(metaDir, "sycignore"), "bin,obj,.vs,.idea,lib,.git,.syc", token);
        return 0;
      });
      return initcommand;
    }

    private static Command WithDiffCommand(this Command parentCommand, string directory)
    {
      var diffOnlyOption = new Option<bool>("--diff-only") { Required = false };

      var diffCommand = new Command("diff", "compare two repos with difference") { diffOnlyOption };
      diffCommand.SetAction(async (result, token) =>
      {
        if (await PreCheckConfiguration() == 1) return 1;
        await using var stream = await OpenNetworkStreamAsync(token);
        Console.WriteLine("[子进程] 启动成功，连接管道...");
        // 连接子进程的管道（长连接）
        Console.WriteLine("[子进程] 管道连接成功，等待文件元数据...");
        await WriteHandShakeAsync(stream, token);
        await stream.WriteAsync(
          Encoding.UTF8.GetBytes(
            $"diff --project {Project} {MetadataSeparator}"), token);

        var metas = await stream.PullAllFilesMetadata(token);
        if (metas.Count <= 0) return 1;
        var metasLocal = await FileHelper.ReadAllFileMetadataAsync(directory);

        var diffOnly = result.GetValue(diffOnlyOption);

        metas.ForEach(m =>
        {
          m.DestFilePath = m.DestFilePath?.Replace("\\", "/");
        });
        metasLocal.ForEach(m =>
        {
          m.DestFilePath = m.DestFilePath?.Replace("\\", "/");
        });
        
        var difference = FileHelper.CompareFilesOfTwoRepo(metas, metasLocal, diffOnly);
        difference.ForEach(Console.WriteLine);
        return 0;
      });


      parentCommand.Add(diffCommand);
      return diffCommand;
    }

    private static async Task ReadAllStream(Stream pipeStream)
    {
      var metadataBuilder = new StringBuilder();
      var buffer = new byte[1];


      // 逐字符读取，直到遇到分隔符（避免元数据与内容粘连）
      while ((await pipeStream.ReadAsync(buffer.AsMemory(0, 1))) != 0)
      {
        var currentChar = (char)buffer[0];
        if (currentChar == MetadataSeparator[0])
        {
          // 读取到分隔符，解析JSON
          var metadataJson = metadataBuilder.ToString();
          Console.Write(metadataJson);
          metadataBuilder.Clear();
        }

        if (currentChar == '\x00')
        {
          Console.WriteLine(metadataBuilder.ToString());
          break;
        }

        metadataBuilder.Append(currentChar);
      }
    }
  }

  public static class CommandLineExtensions
  {
    public static bool HaveDataInCommandLine(this Option option, ParseResult result)
    {
      var optionResult = result.GetResult(option);
      return optionResult is not null && optionResult is not { Implicit: true };
    }
  }
}
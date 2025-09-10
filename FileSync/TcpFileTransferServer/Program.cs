// FileCopierMain/Program.cs

using System.CommandLine;
using System.Diagnostics;
using System.IO.Pipes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SyncFileLib;

namespace TcpFileTransferServer
{
  public static class Program
  {
    private static TcpListener? _server;
    public static Dictionary<string, string> ProjectMapping = new();
    public static Dictionary<string, string> UserMapping = new();
    public static Dictionary<string, string> UserRestrictedBevMapping = new();

    public static async Task<int> Main(string[] args)
    {
      var portOption = new Option<int>("--port", "-p")
      {
        Required = true,
        Description = "the port to listen on",
        DefaultValueFactory = (result => GetAvailablePort())
      };


      var rootCommand = new RootCommand("file transfer server")
      {
        portOption
      };

      rootCommand.SetAction(async (result, token) =>
      {
        var port = result.GetValue(portOption);
        await HandleStartListen(port);
        return 0;
      });

      var registerCommand = new Command("register");
      registerCommand.WithRegisterProjectCommand();
      registerCommand.WithRegisterUserCommand();

      rootCommand.Add(registerCommand);

      //加载project
      await LoadProjectMap();
      await LoadUserList();
      await LoadUserAccessLevelList();

      var parseResult = rootCommand.Parse(args);
      return await parseResult.InvokeAsync();
    }

    private static int GetAvailablePort()
    {
      var allListeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
      var port = 8888;
      var checking = true;
      do
      {
        if (allListeners.Any(endpoint => endpoint.Port == port)) port++;
        else
          checking = false;
      } while (checking);

      return port;
    }

    private static async Task LoadProjectMap()
    {
      if (!File.Exists("./ProjectMapping.json")) return;
      try
      {
        var json = await File.ReadAllTextAsync("./ProjectMapping.json");
        ProjectMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? [];
      }
      catch (Exception io)
      {
        Console.WriteLine($"[主进程] Load project mapping failed");
      }
    }

    private static async Task LoadUserList()
    {
      if (!File.Exists("./UserList.json")) return;
      try
      {
        var json = await File.ReadAllTextAsync("./UserList.json");
        UserMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? [];
      }
      catch (Exception io)
      {
        Console.WriteLine($"[主进程] Load user mapping failed");
      }
    }

    private static async Task LoadUserAccessLevelList()
    {
      if (!File.Exists("./UserRestrictedBevMapping.json")) return;
      try
      {
        var json = await File.ReadAllTextAsync("./UserRestrictedBevMapping.json");
        UserRestrictedBevMapping = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? [];
      }
      catch (Exception io)
      {
        Console.WriteLine($"[主进程] Load user mapping failed");
      }
    }

    private static async Task HandleStartListen(int port)
    {
      try
      {
        // 启动服务器，监听所有网络接口
        _server = new TcpListener(IPAddress.Any, port);
        _server.Start();
        Console.WriteLine($"[主进程]文件传输服务器已启动，监听端口 {port}...");
        Console.WriteLine($"[主进程]等待客户端连接...");

        while (true)
        {
          // 接受客户端连接
          var client = await _server.AcceptTcpClientAsync();
          Console.WriteLine($"新客户端连接: {((IPEndPoint)client.Client.RemoteEndPoint!)?.Address}");
          // 异步处理客户端请求，不阻塞接受新连接
          _ = HandleClientAsync(client);
        }
      }
      catch (Exception ex)
      {
        Console.WriteLine($"服务器错误: {ex.Message}");
      }
      finally
      {
        _server?.Stop();
      }
    }

    /// <summary>
    /// 处理客户端请求
    /// </summary>
    private static async Task HandleClientAsync(TcpClient client)
    {
      NetworkStream? stream;
      try
      {
        stream = client.GetStream();
        var buffer = new byte[8];
        Console.WriteLine("Waiting for StartSignal");

        #region HandleHandShaking

        var count = await stream.ReadAsync(buffer);
        if (count != 8)
        {
          await client.SendErrorMessageAsync(stream, "Take wrong start signal");
          return;
        }

        var userInfo = await MatchUserInfoAndProjectAsync(stream);
        if (!userInfo.Success)
        {
          await client.SendErrorMessageAsync(stream, userInfo.ErrorMessage!);
          return;
        }

        #endregion

        var remoteCommand = await ReadRemoteCommandAsync(stream);

        var command = remoteCommand[0];

        if (UserRestrictedBevMapping.TryGetValue(userInfo.User!, out var value) && value.Contains(command))
        {
          await client.SendErrorMessageAsync(stream, "Not allowed");
          return;
        }


        Console.WriteLine(
          $"$[主进程][{client.Client.RemoteEndPoint}] command from client: {string.Join(' ', remoteCommand)}");

        var rootCommand = new RootCommand("file transfer server");
        var projectOption = new Option<string>("--project", "-p") { Required = true };
        var ticksOption = new Option<long>("--ticks", "-t") { Required = false, DefaultValueFactory = (_) => 0 };
        var allOption = new Option<bool>("--all", "-a") { Required = false };
        var decryptOption = new Option<bool>("--decrypt", "-d") { Required = false };
        var pushCommand = new Command("push")
        {
          projectOption,
          decryptOption
        };
        var pullCommand = new Command("pull")
        {
          projectOption,
          ticksOption,
          allOption
        };
        //客户端的push 命令，代表要push 到server
        pushCommand.Add(projectOption);
        pushCommand.SetAction(async (result, token) =>
        {
          var project = result.GetValue(projectOption);
          var decrypted = result.GetValue(decryptOption);
          if (project == null || !ProjectMapping.ContainsKey(project!))
          {
            return await client.SendErrorMessageAsync(stream, "No project function or registered");
          }

          //从客户端拉去
          await PullAllFiles(stream, project, token, decrypted);
          return 0;
        });

        pullCommand.SetAction(async (result, token) =>
        {
          var project = result.GetValue(projectOption);
          var l = result.GetValue(ticksOption);
          var all = result.GetValue(allOption);
          if (project == null || !ProjectMapping.ContainsKey(project!))
          {
            return await client.SendErrorMessageAsync(stream, "No project function or registered");
          }

          //推送到客户端
          await PushAllFiles(stream, all ? 0 : l, project, token);
          return 0;
        });

        rootCommand.Add(pushCommand);
        rootCommand.Add(pullCommand);
        rootCommand.WithRemoteCommand().WithGitCommand(stream, userInfo.Project!);
        rootCommand.WithDiffCommand(stream, userInfo.Project!);
        var parseResult = rootCommand.Parse(remoteCommand);
        var signal = await parseResult.InvokeAsync();
        if (signal == 1)
          await stream?.SendMetadataAsync(new FileMetadata() { IsError = true, ErrorMessage = "dddd" })!;
      }
      catch (Exception ex)
      {
        Console.WriteLine($"[主进程] 传输失败: {ex.Message}");
      }
    }

    private static Command WithDiffCommand(this Command parentCommand, Stream stream, string project)
    {
      var diff = new Command("diff");
      var projectOption = new Option<string>("--project", "-p") { Required = true };
      diff.Add(projectOption);
      diff.SetAction(async (result, token) =>
      {
        var prj = result.GetValue(projectOption) ?? project;
        var res = await stream.PushFilesFromDirectory(ProjectMapping[prj], 0, false);
        return res ? 0 : 1;
      });

      parentCommand.Add(diff);
      return diff;
    }

    private static Command WithRemoteCommand(this Command parentCommand)
    {
      var remote = new Command("remote");
      parentCommand.Add(remote);
      return remote;
    }

    private static Command WithGitCommand(this Command parentCommand, Stream stream, string project)
    {
      var git = new Command("git")
      {
        TreatUnmatchedTokensAsErrors = false
      };
      git.SetAction(async (result, token) =>
      {
        var vls = result.Tokens.Select(t => t.Value).ToList().Select(a => a.Contains(' ') ? $"\"{a}\"" : a).ToArray();
        var values = vls.Skip(2).ToArray();
        var command = string.Join(' ', values);
        var dir = ProjectMapping[project];
        var consoleResult = new GitCommandHelper().ExecuteGitCommand(dir, command);
        await stream.WriteAsync(Encoding.UTF8.GetBytes(consoleResult.Output + consoleResult.ErrorMessage + '\x00'),
          token);
        await stream.FlushAsync(token);
        await Task.Delay(2000, token);
        return 0;
      });
      parentCommand.Add(git);
      return git;
    }

    private static async Task PushAllFiles(Stream stream, long ticks, string project, CancellationToken token)
    {
      var sourceDir = ProjectMapping[project];
      await stream.PushFilesFromDirectory(sourceDir, ticks);
    }

    private static async Task PullAllFiles(Stream stream, string project, CancellationToken token, bool decrypt = true)
    {
      // 循环：接收元数据→处理文件→直到退出信号
      var sourceDir = ProjectMapping[project];
      if (!decrypt)
      {
        await stream.PullFilesIntoDirectory(sourceDir, token);
        return;
      }

      // 关键：仅启动1次FileWriter子进程，建立长连接
      await using var pipeServer =
        new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
      var pipeHandle = pipeServer.GetClientHandleAsString();
      // 启动子进程（仅1次）
      var writerProcess = StartSingleFileWriter(pipeHandle, sourceDir);
      if (writerProcess == null)
      {
        await stream.PullFilesIntoDirectory(sourceDir, token);
        return;
      }

      await ForwardStream(stream, pipeServer);
    }

    private static async Task ForwardStream(Stream sourceStream, Stream pipeStream)
    {
      var buffer = new byte[4096];
      int bytesRead;

      while ((bytesRead = await sourceStream.ReadAsync(buffer)) > 0)
      {
        await pipeStream.WriteAsync(buffer.AsMemory(0, bytesRead));
      }

      await pipeStream.FlushAsync(); // 确保内容发送完成
    }

    private static async Task<HandShakingResult> MatchUserInfoAndProjectAsync(this Stream pipeStream)
    {
      var information = await pipeStream.ReadSegmentStringFromStream();
      if (string.IsNullOrEmpty(information)) return new HandShakingResult() { Success = false };

      var rex = new Regex(
        "^(?<user>[a-zA-Z0-9]+):(?<password>[^/]+)/(?<project>[a-zA-Z0-9]+)$");

      if (!rex.Match(information).Success)
      {
        Console.WriteLine("Invalid remote remote path.");
        return new HandShakingResult() { Success = false };
      }

      var user = rex.Match(information).Groups["user"].Value;
      var password = rex.Match(information).Groups["password"].Value;
      var project = rex.Match(information).Groups["project"].Value;

      if (!UserMapping.TryGetValue(user, out var value) || value != password)
        return new HandShakingResult() { Success = false, ErrorMessage = "User not exist or password is wrong" };
      return !ProjectMapping.ContainsKey(project)
        ? new HandShakingResult() { Success = false, ErrorMessage = "Project not exist" }
        : new HandShakingResult() { Success = true, User = user, Project = project };
    }

    private static async Task<string[]> ReadRemoteCommandAsync(this Stream pipeStream)
    {
      var seg = await pipeStream.ReadSegmentStringFromStream();
      return string.IsNullOrEmpty(seg) ? [] : seg.Split(' ').Where(p => !string.IsNullOrEmpty(p)).ToArray();
    }

    /// <summary>
    /// 启动文件传输
    /// </summary>
    /// <param name="pipeHandle"></param>
    /// <param name="directory"></param>
    /// <returns></returns>
    private static Process? StartSingleFileWriter(string pipeHandle, string directory)
    {
      var startInfo = new ProcessStartInfo
      {
        FileName = "writer.exe",
        Arguments = $"{pipeHandle} {directory}", // 仅传递管道句柄（元数据后续通过管道发）
        UseShellExecute = false,
        CreateNoWindow = true,
        RedirectStandardOutput = true, // 重定向子进程输出到主进程
        RedirectStandardError = true
      };

      var process = Process.Start(startInfo);
      if (process == null)
      {
        Console.WriteLine("[主进程] 启动FileWriter失败");
        return null;
      }

      // 监听子进程输出（可选，用于调试）
      process.OutputDataReceived += (s, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[子进程输出] {e.Data}");
      };
      process.ErrorDataReceived += (s, e) =>
      {
        if (!string.IsNullOrEmpty(e.Data)) Console.WriteLine($"[子进程错误] {e.Data}");
      };
      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      return process;
    }
  }

  public class HandShakingResult
  {
    public bool Success { get; set; }
    public string? Project { get; set; }
    public string? User { get; set; }
    public string? ErrorMessage { get; set; }
  }

  public static class TcpClientExtensions
  {
    public static async Task<int> SendErrorMessageAsync(this TcpClient client, Stream stream, string errorMessage)
    {
      Console.WriteLine($"[主进程][{client.Client?.RemoteEndPoint}] {errorMessage}");
      Console.WriteLine($"[主进程] No Action will executed");
      await stream.SendMetadataAsync(new FileMetadata()
        { IsError = true, ErrorMessage = $"{errorMessage}" });
      return 1;
    }

    public static Command WithRegisterProjectCommand(this Command parentCommand)
    {
      var command = new Command("project");

      var nameOption = new Option<string>("--name") { Required = true };
      var pathOption = new Option<string>("--path") { Required = true };

      command.Add(nameOption);
      command.Add(pathOption);


      command.SetAction(async (result, token) =>
      {
        var name = result.GetValue(nameOption);
        var path = result.GetValue(pathOption);
        if (name != null && path != null)
        {
          Program.ProjectMapping[name] = path;
          await File.WriteAllTextAsync("./ProjectMapping.json", JsonConvert.SerializeObject(Program.ProjectMapping),
            token);
        }
      });
      parentCommand.Add(command);
      return command;
    }

    public static Command WithRegisterUserCommand(this Command parentCommand)
    {
      var command = new Command("user");

      var nameOption = new Option<string>("--name") { Required = true };
      var passwordOption = new Option<string>("--password") { Required = true };

      command.Add(nameOption);
      command.Add(passwordOption);


      command.SetAction(async (result, token) =>
      {
        var name = result.GetValue(nameOption);
        var path = result.GetValue(passwordOption);
        if (name != null && path != null)
        {
          Program.UserMapping[name] = path;
          await File.WriteAllTextAsync("./UserList.json", JsonConvert.SerializeObject(Program.UserMapping), token);
        }
      });
      parentCommand.Add(command);
      return command;
    }
  }
}
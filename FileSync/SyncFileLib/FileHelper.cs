using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using TcpFileTransferServer;

namespace SyncFileLib;

public static class FileHelper
{
  private const string MetadataSeparator = "\n";

  private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

  // 发送文件元数据（JSON+分隔符）
  public static async Task SendMetadataAsync(this Stream pipeStream, FileMetadata metadata)
  {
    var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions) + MetadataSeparator;
    var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);
    await pipeStream.WriteAsync(metadataBytes);
    await pipeStream.FlushAsync(); // 确保元数据立即发送
  }

  public static async Task<bool> PushFilesFromDirectory(this Stream stream, string directory, long ticks,
    bool withFileContent = true)
  {
    try
    {
      var fileOperations
        = ticks == 0 || !withFileContent
          ? Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
            .Select(p => new GitFileChange()
            {
              ChangeType = 'M',
              FilePath = p
            }).ToList()
          : await GitCommandHelper.GetMergedGitFileChanges(ticks, directory);

      Console.WriteLine($"发现 {fileOperations.Count} 个文件，开始批量传输...");
      var ignoreFilePath = Path.Combine(directory, $".syc/sycignore");
      var excludes = "bin,obj,.vs,.idea,lib,.git,.syc".Split(',');
      if (File.Exists(ignoreFilePath))
      {
        var ignores = await File.ReadAllTextAsync(ignoreFilePath);
        excludes = ignores.Split(',')
          .Where(p => !string.IsNullOrEmpty(p))
          .ToArray();
      }

      var files = from sourceFile in fileOperations
        let dire = Path.GetDirectoryName(sourceFile.FilePath)
        where !excludes.Any(e => dire != null && (dire.Contains($"\\{e}") || dire.Contains($"/{e}")))
        select sourceFile;

      Console.WriteLine($"发现 {fileOperations.Count} 个文件，开始批量传输...");

      // 循环发送每个文件的元数据和内容
      foreach (var sourceFile in files)
      {
        try
        {
          // 计算目标路径
          var relativePath = Path.GetRelativePath(directory, sourceFile.FilePath!);
          // 1. 读取源文件信息（元数据）
          var fileInfo = new FileInfo(sourceFile.FilePath!);
          var metadata = new FileMetadata
          {
            DestFilePath = relativePath,
            FileSize = sourceFile.ChangeType == 'D' ? 0 : fileInfo.Length,
            IsQuitSignal = false,
            LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
            IsRemove = sourceFile.ChangeType == 'D'
          };

          // 2. 发送元数据（JSON序列化+分隔符）
          await stream.SendMetadataAsync(metadata);
          Console.WriteLine($"[子进程] 开始传输: {sourceFile.FilePath} → {relativePath}");

          if (metadata.IsRemove)
          {
            Console.WriteLine($"[子进程] 发送删除文件命令: {sourceFile.FilePath}");
            continue;
          }

          if (!withFileContent) continue;
          // 3. 发送文件内容
          await stream.SendFileContentAsync(sourceFile.FilePath!);
          Console.WriteLine($"[子进程] 传输完成: {sourceFile.FilePath}（{fileInfo.Length} 字节）");
        }
        catch (SocketException ex)
        {
          Console.WriteLine($"[子进程] 传输失败 {sourceFile.FilePath}: {ex.Message}");
          break;
        }
        catch (Exception ex)
        {
          Console.WriteLine($"[子进程] 传输失败 {sourceFile.FilePath}: {ex.Message}");
        }
      }

      // 4. 所有文件处理完，发送退出信号
      await stream.SendMetadataAsync(new FileMetadata { IsQuitSignal = true });

      Console.WriteLine("[子进程] 所有文件传输完成，通知子进程退出...");
      return true;
    }
    catch (Exception ex)
    {
      Console.WriteLine($"处理客户端时出错: {ex.Message}");
      if (stream is { CanWrite: true })
      {
        await stream.SendMetadataAsync(
          new FileMetadata() { IsError = true, ErrorMessage = $"ERROR {ex.Message}" });
      }

      return false;
    }
    finally
    {
      await stream.DisposeAsync();
      //client?.Close();
      Console.WriteLine("客户端连接已关闭");
    }
  }

  public static async Task<List<FileMetadata>> ReadAllFileMetadataAsync(string directory)
  {
    var metas = new List<FileMetadata>();
    var fileOperations = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
      .Select(p => new GitFileChange()
      {
        ChangeType = 'M',
        FilePath = p
      }).ToList();

    Console.WriteLine($"发现 {fileOperations.Count} 个文件，开始批量传输...");
    var ignoreFilePath = Path.Combine(directory, $".syc/sycignore");
    var excludes = "bin,obj,.vs,.idea,lib,.git,.syc".Split(',');
    if (File.Exists(ignoreFilePath))
    {
      var ignores = await File.ReadAllTextAsync(ignoreFilePath);
      excludes = ignores.Split(',')
        .Where(p => !string.IsNullOrEmpty(p))
        .ToArray();
    }

    var files = from sourceFile in fileOperations
      let dire = Path.GetDirectoryName(sourceFile.FilePath)
      where !excludes.Any(e => dire != null && (dire.Contains($"\\{e}") || dire.Contains($"/{e}")))
      select sourceFile;

    Console.WriteLine($"发现 {fileOperations.Count} 个文件，开始批量传输...");
    // 循环发送每个文件的元数据和内容
    foreach (var sourceFile in files)
    {
      try
      {
        // 计算目标路径
        var relativePath = Path.GetRelativePath(directory, sourceFile.FilePath!);
        // 1. 读取源文件信息（元数据）
        var fileInfo = new FileInfo(sourceFile.FilePath!);
        var metadata = new FileMetadata
        {
          DestFilePath = relativePath,
          FileSize = sourceFile.ChangeType == 'D' ? 0 : fileInfo.Length,
          IsQuitSignal = false,
          LastWriteTimeUtc = fileInfo.LastWriteTimeUtc,
          IsRemove = sourceFile.ChangeType == 'D'
        };
        metas.Add(metadata);
      }
      catch (Exception ex)
      {
        Console.WriteLine($"读取文件元数据出错: {ex.Message}");
      }
    }

    return metas;
  }


  public static async Task<List<FileMetadata>> PullAllFilesMetadata(this Stream stream,
    CancellationToken token)
  {
    var metas = new List<FileMetadata>();
    while (true)
    {
      if (token.IsCancellationRequested) break;

      // 1. 读取并解析元数据（直到遇到分隔符）
      var metadata = await stream.ReadMetadataAsync();
      if (metadata == null) continue;

      // 2. 检查是否为退出信号
      if (metadata.IsQuitSignal)
      {
        Console.WriteLine("[子进程] 收到退出信号，准备退出...");
        break;
      }

      if (metadata.IsError)
      {
        Console.WriteLine($"[子进程] 收到错误通知：{metadata.ErrorMessage}，准备推出");
        return [];
        //await Task.Delay(5000, token);
        break;
      }

      metas.Add(metadata);
    }

    return metas;
  }

  public static List<string> CompareFilesOfTwoRepo(List<FileMetadata> files, List<FileMetadata> others,
    bool diffOnly = true)
  {
    var result = files.Join(others, r => r.DestFilePath, r => r.DestFilePath, (l, r) =>
    {
      if (l.LastWriteTimeUtc > r.LastWriteTimeUtc)
      {
        return $"{l.DestFilePath} => {r.DestFilePath}";
      }

      if (l.LastWriteTimeUtc < r.LastWriteTimeUtc)
      {
        return $"{l.DestFilePath} <= {r.DestFilePath}";
      }

      if (l.LastWriteTimeUtc == r.LastWriteTimeUtc && !diffOnly)
      {
        return $"{l.DestFilePath} ==  {r.DestFilePath}";
      }

      if (l.FileSize != r.FileSize)
      {
        return $"{l.DestFilePath} <?> {r.DestFilePath}";
      }

      return string.Empty;
    }).ToList();

    var leftOnly = files
      .ExceptBy(others.Select(r => r.DestFilePath), p => p.DestFilePath)
      .Select(p => $"{p.DestFilePath}<?>             ");
    var rightOnly = others.ExceptBy(files.Select(r => r.DestFilePath), p => p.DestFilePath)
      .Select(p => $"             <?>{p.DestFilePath}");
    result.AddRange(leftOnly);
    result.AddRange(rightOnly);
    return result.Where(p => !string.IsNullOrEmpty(p)).ToList();
  }

  public static async Task PullFilesIntoDirectory(this Stream stream, string directory,
    CancellationToken token)
  {
    while (true)
    {
      if (token.IsCancellationRequested) break;

      // 1. 读取并解析元数据（直到遇到分隔符）
      var metadata = await stream.ReadMetadataAsync();
      if (metadata == null) continue;

      // 2. 检查是否为退出信号
      if (metadata.IsQuitSignal)
      {
        Console.WriteLine("[子进程] 收到退出信号，准备退出...");
        break;
      }

      if (metadata.IsError)
      {
        Console.WriteLine($"[子进程] 收到错误通知：{metadata.ErrorMessage}，准备推出");
        await Task.Delay(5000, token);
        break;
      }

      if (metadata.IsRemove)
      {
        try
        {
          if (metadata.DestFilePath != null)
          {
            var filePath = Path.Combine(directory, metadata.DestFilePath);
            File.Delete(filePath);
            continue;
          }
        }
        catch (Exception e)
        {
          Console.WriteLine($"[子进程] 删除文件异常: {e.Message}");
          continue;
        }
      }

      try
      {
        // 3. 处理文件内容（按元数据中的大小读取）
        await stream.WriteFileFromPipeAsync(metadata, directory);
      }
      catch (Exception e)
      {
        Console.WriteLine($"[子进程] 操作文件异常: {e.Message}");
      }
    }
  }

// 发送文件内容
  private static async Task SendFileContentAsync(this Stream pipeStream, string sourceFile)
  {
    await using var sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read);
    var buffer = new byte[4096];
    int bytesRead;

    while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
      await pipeStream.WriteAsync(buffer.AsMemory(0, bytesRead));
    }

    await pipeStream.FlushAsync(); // 确保内容发送完成
  }

  public static async Task<string> ReadSegmentStringFromStream(this Stream pipeStream)
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
        return metadataJson;
      }

      metadataBuilder.Append(currentChar);
    }

    return string.Empty; // 管道关闭时返回null 
  }

// 读取元数据（从管道中读取到分隔符为止）
  private static async Task<FileMetadata?> ReadMetadataAsync(this Stream pipeStream)
  {
    var seg = await pipeStream.ReadSegmentStringFromStream();
    return string.IsNullOrEmpty(seg) ? null : JsonSerializer.Deserialize<FileMetadata>(seg, JsonOptions);
  }

// 从管道读取文件内容并写入目标文件
  private static async Task WriteFileFromPipeAsync(this Stream pipeStream, FileMetadata metadata, string rootPath)
  {
    Console.WriteLine($"[子进程] 开始写入文件: {metadata.DestFilePath}（预计 {metadata.FileSize} 字节）");
    if (string.IsNullOrEmpty(metadata.DestFilePath)) return;

    var filePath = Path.Combine(rootPath, metadata.DestFilePath).Replace("\\","/");

    var dir = Path.GetDirectoryName(filePath)!;
    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

    await using var destStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
    var buffer = new byte[4096];
    long totalBytesWritten = 0;

    // 按元数据中的文件大小读取（避免多读/少读）
    while (totalBytesWritten < metadata.FileSize)
    {
      // 计算本次最大读取量（避免超出剩余文件大小）
      var maxRead = (int)Math.Min(buffer.Length, metadata.FileSize - totalBytesWritten);
      var bytesRead = await pipeStream.ReadAsync(buffer.AsMemory(0, maxRead));

      if (bytesRead == 0)
      {
        throw new IOException($"文件 {metadata.DestFilePath} 传输中断（未收到完整内容）");
      }

      await destStream.WriteAsync(buffer.AsMemory(0, bytesRead));
      totalBytesWritten += bytesRead;
    }

    await destStream.FlushAsync();
    var fileInfo = new FileInfo(filePath)
    {
      LastWriteTimeUtc = metadata.LastWriteTimeUtc
    };
    fileInfo.LastWriteTimeUtc = metadata.LastWriteTimeUtc;
    Console.WriteLine($"[子进程] 文件写入完成: {metadata.DestFilePath}（实际 {totalBytesWritten} 字节）");
  }
}
// FileCopierMain/Program.cs

using System.Diagnostics;
using System.IO.Pipes;
using System.Text.Json;

namespace FilesManipulator
{
    public static class Program
    {
        // 约定：元数据用"\n"分隔（避免与内容粘连）
        private const string MetadataSeparator = "\n";
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
        
        
        

        public static async Task Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.WriteLine("用法: FileCopierMain <源文件夹> <目标文件夹>");
                return;
            }

            var sourceDir = args[0];
            var destDir = args[1];

            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine($"源文件夹不存在: {sourceDir}");
                return;
            }

            Directory.CreateDirectory(destDir);

            // 关键：仅启动1次FileWriter子进程，建立长连接
            await using var pipeServer =
                new AnonymousPipeServerStream(PipeDirection.Out, HandleInheritability.Inheritable);
            var pipeHandle = pipeServer.GetClientHandleAsString();

            // 启动子进程（仅1次）
            var writerProcess = StartSingleFileWriter(pipeHandle);
            if (writerProcess == null) return;
            try
            {
                // 获取所有待复制文件
                var allFiles = Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories);
                Console.WriteLine($"发现 {allFiles.Length} 个文件，开始批量传输...");
                var excludes = "bin,obj,.vs,.idea,lib".Split(',');
                // 循环发送每个文件的元数据和内容
                foreach (var sourceFile in allFiles)
                {
                    var dire = Path.GetDirectoryName(sourceFile);
                    if (excludes.Any(e => dire != null && dire.Contains(e))) continue;

                    try
                    {
                        // 计算目标路径
                        var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
                        var destFile = Path.Combine(destDir, relativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(destFile) ?? string.Empty);

                        // 1. 读取源文件信息（元数据）
                        var fileInfo = new FileInfo(sourceFile);
                        var metadata = new FileMetadata
                        {
                            DestFilePath = destFile,
                            FileSize = fileInfo.Length,
                            IsQuitSignal = false
                        };

                        // 2. 发送元数据（JSON序列化+分隔符）
                        await SendMetadataAsync(pipeServer, metadata);
                        Console.WriteLine($"[主进程] 开始传输: {sourceFile} → {destFile}");

                        // 3. 发送文件内容
                        await SendFileContentAsync(pipeServer, sourceFile);
                        Console.WriteLine($"[主进程] 传输完成: {sourceFile}（{fileInfo.Length} 字节）");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[主进程] 传输失败 {sourceFile}: {ex.Message}");
                    }
                }

                // 4. 所有文件处理完，发送退出信号
                await SendMetadataAsync(pipeServer, new FileMetadata { IsQuitSignal = true });
                Console.WriteLine("[主进程] 所有文件传输完成，通知子进程退出...");

                // 等待子进程正常退出
                await writerProcess.WaitForExitAsync();
                Console.WriteLine($"[主进程] 子进程已退出（退出码: {writerProcess.ExitCode}）");
            }
            finally
            {
                pipeServer.DisposeLocalCopyOfClientHandle();
                writerProcess?.Dispose();
            }
        }

        // 启动单个FileWriter子进程
        private static Process? StartSingleFileWriter(string pipeHandle)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "FileWriter.exe",
                Arguments = pipeHandle, // 仅传递管道句柄（元数据后续通过管道发）
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

        // 发送文件元数据（JSON+分隔符）
        private static async Task SendMetadataAsync(Stream pipeStream, FileMetadata metadata)
        {
            var metadataJson = JsonSerializer.Serialize(metadata, JsonOptions) + MetadataSeparator;
            var metadataBytes = System.Text.Encoding.UTF8.GetBytes(metadataJson);
            await pipeStream.WriteAsync(metadataBytes);
            await pipeStream.FlushAsync(); // 确保元数据立即发送
        }

        // 发送文件内容
        private static async Task SendFileContentAsync(Stream pipeStream, string sourceFile)
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
    }
}
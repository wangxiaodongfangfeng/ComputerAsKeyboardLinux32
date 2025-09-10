namespace SyncFileLib
{
  // 文件传输元数据（主→子）
  public class FileMetadata
  {
    public string? DestFilePath { get; set; } // 目标文件路径
    public long FileSize { get; set; } // 文件总大小（用于验证是否接收完整）
    public bool IsQuitSignal { get; set; } // 是否为退出信号（true时子进程退出）
    public bool IsError { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsRemove { get; set; }

    public DateTime LastWriteTimeUtc { get; set; }
  }
}
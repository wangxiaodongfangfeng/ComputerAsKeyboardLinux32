using System;
using System.Diagnostics;
using System.Runtime.Remoting.Channels;

namespace ComputerAskeyboardLinux32
{
    public class FingerPrintHelper
    {
        public static bool VerifyFinger(string userName)
        {
            var psi = new ProcessStartInfo()
            {
                FileName = "fprintd-verify",
                Arguments = userName,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Program.WriteLogOnScreen("Please Scan your fingerprint");
            using (var process = new Process())
            {
                process.StartInfo = psi;
                bool matched = false;
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        matched = e.Data.Contains("verify-match (done)");
                        Program.WriteLogOnScreen("this is data from output " + e.Data);
                    }
                };
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();
                return matched;
            }
        }
    }
}
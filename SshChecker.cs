using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PiCheck
{
    public class SshChecker
    {
        public async Task<bool> CheckSshConnectivityAsync(string target, int timeoutSeconds = 10)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ssh",
                    Arguments = $"-o ConnectTimeout={timeoutSeconds} -o BatchMode=yes -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null -o LogLevel=QUIET {target} exit",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                using (var process = new Process { StartInfo = startInfo })
                {
                    process.Start();
                    
                    // Wait for process to complete with timeout
                    bool completed = await Task.Run(() => process.WaitForExit(timeoutSeconds * 1000));
                    
                    if (!completed)
                    {
                        try
                        {
                            if (!process.HasExited)
                            {
                                process.Kill();
                                process.WaitForExit(2000);
                            }
                        }
                        catch { }
                        return false;
                    }

                    // SSH returns 0 for successful connection
                    return process.ExitCode == 0;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static bool IsSshAvailable()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "ssh",
                    Arguments = "-V",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(startInfo))
                {
                    process.WaitForExit(5000);
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
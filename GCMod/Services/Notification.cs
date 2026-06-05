using System;
using System.Diagnostics;

namespace GCMod
{
    public static class Notification
    {
        public static Process powershell;
        public static readonly object _lock = new();

        public static void Initialize()
        {
            try
            {
                string script = @"
$script:toastMgr = [Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime]
$script:toastText02 = [Windows.UI.Notifications.ToastTemplateType]::ToastText02
$script:template = $script:toastMgr::GetTemplateContent($script:toastText02)
$script:notifier = $script:toastMgr::CreateToastNotifier('GCMod')
$ErrorActionPreference = 'Stop'
while ($true) {
    $inputJson = [Console]::In.ReadLine()
    if ([string]::IsNullOrEmpty($inputJson)) { break }
    try {
        $data = ConvertFrom-Json $inputJson
        $toastXml = $script:template.CloneNode($true)
        $toastXml.SelectSingleNode('//text[@id=""1""]').InnerText = $data.Title;
        $toastXml.SelectSingleNode('//text[@id=""2""]').InnerText = $data.Message;
        $toast = [Windows.UI.Notifications.ToastNotification]::new($toastXml)
        $toast.ExpirationTime = [DateTimeOffset]::Now.AddSeconds($data.ExpirationTime)
        $script:notifier.Show($toast)
    } catch {
        [Console]::Error.WriteLine($_.Exception.Message)
    }
}
";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                powershell = new Process { StartInfo = psi };
                powershell.Start();

                powershell.BeginErrorReadLine();
                powershell.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        Plugin.Log.LogError($"PowerShell Error: {e.Data}");
                    }
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, e) => Cleanup();
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"PowerShell initialize failed: {e.Message}");
                powershell = null;
            }
        }

        private static string Escape(string s)
        {
            return s.Replace("\"", "\"\"").Replace("&", "&").Replace("<", "<").Replace(">", ">");
        }

        public static void Show(string title, string message, int expirationTime = 3)
        {
            if (powershell == null || powershell.HasExited)
            {
                return;
            }

            try
            {
                string json = $"{{\"Title\":\"{Escape(title)}\",\"Message\":\"{Escape(message)}\",\"ExpirationTime\":{expirationTime}}}";

                lock (_lock)
                {
                    powershell.StandardInput.WriteLine(json);
                    powershell.StandardInput.Flush();
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e.Message);
            }
       }

        public static void Cleanup()
        {
            try
            {
                if (powershell != null && !powershell.HasExited)
                {
                    powershell.StandardInput.Close();
                    powershell.Kill(true);
                    powershell.Close();
                }
                powershell = null;
            }
            catch
            {
            }
        }
    }
}
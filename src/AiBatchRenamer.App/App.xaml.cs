using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace AiBatchRenamer.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        public static string LogException(Exception exception)
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiBatchRenamer",
                "CrashLogs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
            var builder = new StringBuilder();
            builder.AppendLine(DateTimeOffset.Now.ToString("o"));
            builder.AppendLine(exception.ToString());
            File.WriteAllText(logPath, builder.ToString(), Encoding.UTF8);
            return logPath;
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var logPath = LogException(e.Exception);
            MessageBox.Show(
                "程序遇到异常，但已阻止直接退出。\r\n\r\n错误信息：" + e.Exception.Message + "\r\n\r\n日志位置：" + logPath,
                "AI 批量重命名",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}

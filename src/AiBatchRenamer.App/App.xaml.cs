using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AiBatchRenamer.App
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            base.OnStartup(e);
        }

        public static string DisplayVersion
        {
            get
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version == null)
                {
                    return "0.0.0";
                }

                return string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build);
            }
        }

        public static string LogException(Exception exception)
        {
            return LogException(exception, string.Empty);
        }

        public static string LogException(Exception exception, string context)
        {
            var logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "AiBatchRenamer",
                "CrashLogs");
            Directory.CreateDirectory(logDirectory);

            var logPath = Path.Combine(logDirectory, DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
            var builder = new StringBuilder();
            builder.AppendLine(DateTimeOffset.Now.ToString("o"));
            if (!string.IsNullOrWhiteSpace(context))
            {
                builder.AppendLine(context);
            }

            builder.AppendLine(exception.ToString());
            File.WriteAllText(logPath, builder.ToString(), Encoding.UTF8);
            return logPath;
        }

        public static void LogDiagnostic(string message)
        {
            try
            {
                var logDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "AiBatchRenamer",
                    "CrashLogs");
                Directory.CreateDirectory(logDirectory);

                var logPath = Path.Combine(logDirectory, "diagnostic.log");
                File.AppendAllText(
                    logPath,
                    DateTimeOffset.Now.ToString("o") + " " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch
            {
                // Diagnostics must never interrupt the user workflow.
            }
        }

        private static void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            var logPath = LogException(e.Exception, "DispatcherUnhandledException");
            MessageBox.Show(
                "程序遇到异常，但已阻止直接退出。\r\n\r\n错误信息：" + e.Exception.Message + "\r\n\r\n日志位置：" + logPath,
                "AI 批量重命名",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                LogException(exception, "AppDomainUnhandledException IsTerminating=" + e.IsTerminating);
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            LogException(e.Exception, "UnobservedTaskException");
            e.SetObserved();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace WidgetES
{
    public static class Logger
    {
        private static readonly string LogDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WidgetES", "Logs");

        private static readonly string LogFilePath =
            Path.Combine(LogDirectory, $"log_{DateTime.Now:yyyy-MM-dd}.txt");

        static Logger()
        {
            try
            {
                if (!Directory.Exists(LogDirectory))
                    Directory.CreateDirectory(LogDirectory);
            }
            catch { /* не падаем при ошибке */ }
        }

        public static void Write(string message, Exception? ex = null)
        {
            try
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                using (StreamWriter writer = new StreamWriter(LogFilePath, true))
                {
                    writer.WriteLine($"[{time}] {message}");
                    if (ex != null)
                    {
                        writer.WriteLine($"  Ошибка: {ex.Message}");
                        writer.WriteLine($"  Стек: {ex.StackTrace}");
                    }
                }
            }
            catch { /* если даже логирование сломается — молчим */ }
        }
    }
}

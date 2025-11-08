using AutoUpdaterDotNET;
using System.Configuration;
using System.Data;
using System.Windows;

namespace WidgetES
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // Проверка обновления
            AutoUpdater.Start("https://new.mozimer.ru/WidgetES/update.xml");
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Logger.Write("Неперехваченное исключение", e.ExceptionObject as Exception);
            };

            DispatcherUnhandledException += (sender, e) =>
            {
                Logger.Write("Ошибка в UI-потоке", e.Exception);
                e.Handled = true; // чтобы программа не падала
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                Logger.Write("Ошибка в асинхронной задаче", e.Exception);
                e.SetObserved();
            };
        }
        private void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args != null && args.IsUpdateAvailable)
            {
                string message = $"Доступна новая версия {args.CurrentVersion}!\n\n" +
                                 $"Подробности: {args.ChangelogURL}\n\n" +
                                 "Хотите скачать и установить?";
                var result = MessageBox.Show(
                    message,
                    "Обновление доступно",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    AutoUpdater.DownloadUpdate(args);
                }
            }
        }



    }

}

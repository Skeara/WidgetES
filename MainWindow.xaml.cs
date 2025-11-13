using Microsoft.Win32;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System;
using System.Windows.Media.Animation;
using System.Management;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using WidgetES.Properties;
using System.IO;
using Newtonsoft.Json;
using AutoUpdaterDotNET;
using System;

namespace WidgetES
{
    public partial class MainWindow : Window
    {
        private string notesFile = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WidgetES", "notes.json");
        private DispatcherTimer timer;
        private int currentCharacter = 0;
        private List<string> characterImages;
        private DispatcherTimer systemTimer;
        private bool shouldShowWeatherOnTablet = false;
        private List<string> notes = new();
        private string notesFilePath;
        private bool isNotesMode = false;
        private bool notesLoaded = false;
        private System.Windows.Forms.NotifyIcon _trayIcon;
        private System.Drawing.Icon myIcon;
        private int currentPage = 0; // 0 = время, 1 = погода, 2 = система
        private DispatcherTimer pageTimer;
        private bool _isEditingNote = false;
        private bool isHelpMode = false;

        public MainWindow()
        {
            InitializeComponent();
            SetupTray();
            currentCharacter = Properties.Settings.Default.SelectedCharacter;
            InitializeCharacters();
            LoadCharacter(currentCharacter);
            InitializeTimer();
            InitializeSystemMonitor();
            UpdateDateTime();
            //UpdateGreeting();
            InitializeWeatherTimer(); // <-- НОВЫЙ ТАЙМЕР
            UpdateWeather();
            notesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WidgetES", "notes.json"
            );

            LoadNotes();
            this.Topmost = Properties.Settings.Default.AlwaysOnTop;
            SetAutoStart(Properties.Settings.Default.AutoStart);
            PositionWindowBottomRight();
            //UpdateWeatherButtonOnlyAsync();
            RefreshWeatherAsync();
            InitializePageTimer();

            InitializeGreeting();
        }

        private void SetupTray()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = new System.Drawing.Icon("mozimer.ico"); // свой .ico файл
            _trayIcon.Visible = true;
            _trayIcon.Text = "WidgetES";

            var menu = new System.Windows.Forms.ContextMenuStrip();
            menu.Items.Add("Открыть", null, (s, e) => this.Show());
            menu.Items.Add("Выход", null, (s, e) => Application.Current.Shutdown());
            _trayIcon.ContextMenuStrip = menu;

            this.StateChanged += (s, e) =>
            {
                if (this.WindowState == WindowState.Minimized)
                    this.Hide();
            };
        }

        private void NotesTextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            _isEditingNote = true;
            pageTimer?.Stop(); // ⛔ Останавливаем переключение страниц
        }

        private void NotesTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _isEditingNote = false;
            pageTimer?.Start(); // ▶️ Возобновляем таймер
        }

        private void InitializePageTimer()
        {
            pageTimer = new DispatcherTimer();
            pageTimer.Interval = TimeSpan.FromSeconds(10);
            pageTimer.Tick += PageTimer_Tick;
            pageTimer.Start();
        }

        private async void PageTimer_Tick(object sender, EventArgs e)
        {
            if (_isEditingNote) return;
            currentPage = (currentPage + 1) % 3;

            switch (currentPage)
            {
                case 0: // Время
                    UpdateDateTime();
                    TimePanel.Visibility = Visibility.Visible;
                    InfoPanel.Visibility = Visibility.Collapsed;
                    break;

                case 1: // Погода
                    var weather = await GetWeatherAsync(Properties.Settings.Default.WeatherCity ?? "Москва"); // Сначала получаем данные
                    TimePanel.Visibility = Visibility.Collapsed;
                    InfoPanel.Visibility = Visibility.Visible;
                    ShowInfo(weather.FullInfo); // Потом отображаем
                    break;

                case 2: // Система
                    string systemInfo = GetSystemInfo(); // Получаем данные
                    TimePanel.Visibility = Visibility.Collapsed;
                    InfoPanel.Visibility = Visibility.Visible;
                    ShowInfo(systemInfo); // Отображаем
                    break;
            }
        }

        // Метод для получения системы
        private string GetSystemInfo()
        {
            bool isLaptop = false;
            int batteryPercent = -1;
            string powerLine = "Неизвестно";

            // Проверяем, есть ли батарея
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher("SELECT * FROM Win32_Battery");
                foreach (var battery in searcher.Get())
                {
                    isLaptop = true;
                    batteryPercent = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
                    int status = Convert.ToInt32(battery["BatteryStatus"]);
                    powerLine = status == 2 ? "Подключено" : "От батареи";
                }
            }
            catch { }

            // CPU загрузка
            var cpuCounter = new System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total");
            float cpuUsage = cpuCounter.NextValue();
            System.Threading.Thread.Sleep(500); // Нужно для корректного чтения
            cpuUsage = cpuCounter.NextValue();

            // RAM
            var ramCounter = new System.Diagnostics.PerformanceCounter("Memory", "Available MBytes");
            float freeRam = ramCounter.NextValue();
            var totalRam = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / 1024 / 1024;
            float usedRam = totalRam - freeRam;

            // GPU (для NVIDIA через WMI)
            //string gpuLoad = "Неизвестно";
            //try
            //{
            //    using var searcher = new ManagementObjectSearcher("root\\CIMV2", "SELECT * FROM Win32_VideoController");
            //    foreach (var obj in searcher.Get())
            //    {
            //        gpuLoad = obj["Name"]?.ToString() ?? "Неизвестно";
            //        // Можно добавить GPU usage, если через PerformanceCounter есть драйвер
            //    }
            //}
            //catch { }

            // Формируем строку
            string info = $"🖥CPU: {cpuUsage:F1}%\n💾RAM: {usedRam:F0}/{totalRam:F0} МБ";
            if (isLaptop)
                info = $"🔋Батарея: {batteryPercent}%\n🔌Питание: {powerLine}\n" + info;

            return info;
        }



        private void InitializeCharacters()
        {
            // Список спрайтов персонажей (путь к файлам изображений)
            characterImages = new List<string>
            {
                "pack://application:,,,/Images/character1.png",
                "pack://application:,,,/Images/character2.png",
                "pack://application:,,,/Images/character3.png",
                "pack://application:,,,/Images/character4.png",
                "pack://application:,,,/Images/character5.png",
                "pack://application:,,,/Images/character6.png",
                "pack://application:,,,/Images/character7.png",
                "pack://application:,,,/Images/character8.png",
                "pack://application:,,,/Images/character9.png",
                "pack://application:,,,/Images/character10.png"
            };
        }

        private readonly Dictionary<string, string> WeatherIcons = new()
        {
            { "Sunny", "Sun" },
            { "Clear", "Moon" },
            { "Partly cloudy", "Sun behind cloud" },
            { "Cloudy", "Cloud" },
            { "Overcast", "Cloud" },
            { "Mist", "Fog" },
            { "Fog", "Fog" },
            { "Rain", "Cloud with rain" },
            { "Drizzle", "Cloud with rain" },
            { "Light rain", "Cloud with rain" },
            { "Heavy rain", "Cloud with rain" },
            { "Snow", "Cloud with snow" },
            { "Sleet", "Cloud with rain" },
            { "Thunder", "Cloud with lightning" },
            { "Thunderstorm", "Cloud with lightning" },
            { "Patchy", "Sun behind cloud" },
            { "Light", "Sun behind cloud" }
        };

        private void LoadCharacter(int index)
        {
            try
            {
                if (index >= 0 && index < characterImages.Count)
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(characterImages[index], UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    CharacterImage.Source = bitmap;
                    CharacterImage.Stretch = Stretch.UniformToFill; // <-- ВАЖНО: растягиваем фон
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить изображение: {ex.Message}\n\n" +
                    "Проверьте папку Images и файлы:\n" +
                    "- character1.png\n" +
                    "- character2.png\n" +
                    "- character3.png\n" +
                    "- character4.png\n" +
                    "- character5.png\n" +
                    "- character6.png\n" +
                    "- character7.png\n" +
                    "- character8.png\n" +
                    "- character9.png\n" +
                    "- character10.png",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void UpdateDateTime()
        {
            DateTime now = DateTime.Now;
            TimeText.Text = now.ToString("HH:mm");
            DateText.Text = now.ToString("dd.MM.yy");
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void TimerButton_Click(object sender, RoutedEventArgs e)
        {
            ResumePageTimer();
            ShowTime();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            string currentCity = Properties.Settings.Default.WeatherCity ?? "Москва";

            var selectionWindow = new CharacterSelectionWindow(currentCharacter, currentCity)
            {
                Owner = this
            };

            if (selectionWindow.ShowDialog() == true)
            {
                currentCharacter = selectionWindow.SelectedCharacter;
                Properties.Settings.Default.SelectedCharacter = currentCharacter;
                Properties.Settings.Default.Save();

                // Сохраняем город
                string newCity = selectionWindow.SelectedCity;
                if (newCity != currentCity) // если город изменился
                {
                    shouldShowWeatherOnTablet = true; // ВКЛЮЧАЕМ ФЛАГ
                }
                Properties.Settings.Default.WeatherCity = newCity;
                Properties.Settings.Default.Save();
                this.Topmost = Properties.Settings.Default.AlwaysOnTop;

                // ОБНОВЛЯЕМ ВСЁ СРАЗУ:
                LoadCharacter(currentCharacter);
                await RefreshWeatherAsync();
            }
        }

        private string GetWeatherIcon(string condition)
        {
            condition = condition.ToLower();

            //if (condition.Contains("sun") || condition.Contains("ясно")) return "Sun";
            //if (condition.Contains("clear") && condition.Contains("night")) return "Moon";
            //if (condition.Contains("partly") || condition.Contains("patchy")) return "Sun behind cloud";
            //if (condition.Contains("cloud") || condition.Contains("облач")) return "Cloud";
            //if (condition.Contains("mist") || condition.Contains("fog") || condition.Contains("туман")) return "Fog";
            //if (condition.Contains("rain") || condition.Contains("дожд")) return "cjcn";
            //if (condition.Contains("snow") || condition.Contains("снег")) return "соси";
            //if (condition.Contains("thunder") || condition.Contains("гроз")) return "Cloud with lightning";

            return "🌡️"; // дефолт
        }

        private async Task RefreshWeatherAsync()
        {
            string city = Properties.Settings.Default.WeatherCity ?? "Москва";
            var weather = await GetWeatherAsync(city);

            int roundedTemp = (int)Math.Round(weather.Temperature);
            string icon = GetWeatherIcon(weather.Condition);

            WeatherText.Text = $"{icon}{roundedTemp}°";

            if (shouldShowWeatherOnTablet)
            {
                ShowInfo(weather.FullInfo);
                shouldShowWeatherOnTablet = false;
            }
        }

        private async Task UpdateWeatherFullAsync()
        {
            string city = Properties.Settings.Default.WeatherCity ?? "Москва";
            var weather = await GetWeatherAsync(city);

            // Кнопка
            int roundedTemp = (int)Math.Round(weather.Temperature);
            WeatherText.Text = $"{roundedTemp}°";

            // Планшет
            ShowInfo(weather.FullInfo);
        }

        private async Task UpdateWeatherTabletAsync()
        {
            try
            {
                string city = Properties.Settings.Default.WeatherCity ?? "Москва";
                var weather = await GetWeatherAsync(city);
                ShowInfo(weather.FullInfo);  // показываем в планшете
            }
            catch
            {
                ShowInfo("Погода недоступна");
            }
        }

        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            isHelpMode = true;
            pageTimer?.Stop();
            string helpText =
                "🛠 Виджет «Бесконечное лето» — справка и поддержка\n\n" +
                "Если виджет работает некорректно:\n\n" +
                "- Попробуйте перезапустить приложение.\n\n" +
                "- Все ошибки логируются в папку:\n\n" +
                "%AppData%|Roaming|WidgetES|Logs\n\n" +
                "Контакты для обратной связи:\n\n" +
                "📧 Email: support@mozimer.ru\n\n" +
                "💬 VK: vk.com/mozimer\n\n" +
                "Советы по использованию:\n\n" +
                "- Перетаскивайте виджет за любое свободное место.\n\n" +
                "- Виджет запоминает последние заметки и город для погоды.\n\n" +
                "- Настройте персонажа для уникальной атмосферы рабочего стола.\n\n" +
                "Часто задаваемые вопросы:\n\n" +
                "Q: Как добавить новую заметку?\n\n" +
                "A: Нажмите ➕ и введите текст, затем Enter\n\n" +
                "Q: Как изменить город для погоды?\n\n" +
                "A: Через ⚙ Настройки → выберите город → сохраните.\n\n" +
                "Q: Почему не показывается погода?\n\n" +
                "A: Убедитесь, что устройство подключено к интернету и город введён корректно.\n\n" +
                "Q: Как изменить персонажа?\n\n" +
                "A: Через ⚙ Настройки → выберите персонажа → сохраните.";
            ShowInfoWithExtras(helpText);
        }

        private void ResumePageTimer()
        {
            if (!pageTimer.IsEnabled)
                pageTimer.Start();
            isHelpMode = false;
        }

        private void ShowInfoWithExtras(string text, string donateUrl = "https://yoomoney.ru/to/410019293336394")
        {
            isNotesMode = false;
            InfoPanel.Visibility = Visibility.Visible;
            TimePanel.Visibility = Visibility.Collapsed;

            NotesContainer.Children.Clear();

            var donateButton = new Button
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Background = Brushes.Transparent, // прозрачный фон
                BorderThickness = new Thickness(0), // без рамки
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 0, 10),
                Cursor = Cursors.Hand
            };

            var template = new ControlTemplate(typeof(Button));

            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "PART_Border"; // ← ВАЖНО: имя для триггера
            borderFactory.SetValue(Border.BackgroundProperty, Brushes.Transparent);
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            borderFactory.SetValue(Border.SnapsToDevicePixelsProperty, true);

            var contentPresenterFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentPresenterFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentPresenterFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentPresenterFactory);
            template.VisualTree = borderFactory;

            // Наведение мыши (легкий эффект)
            var trigger = new Trigger
            {
                Property = UIElement.IsMouseOverProperty,
                Value = true
            };
            trigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(30, 255, 255, 255)), "PART_Border")); // ← используем имя
            template.Triggers.Add(trigger);

            donateButton.Template = template;

            var stack = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };

            // Картинка
            var donateImg = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Images/yoomoney.png", UriKind.Absolute)),
                Height = 46, // ограничиваем высоту
                Width = 135,  // можно задать, чтобы не растягивалась
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 5, 0)
            };

            stack.Children.Add(donateImg);
            donateButton.Content = stack;

            donateButton.Click += (s, e) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "https://yoomoney.ru/to/410019293336394",
                        UseShellExecute = true
                    });
                }
                catch { }
            };

            // Добавляем кнопку первой
            NotesContainer.Children.Add(donateButton);

            // Разбиваем текст на строки
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                NotesContainer.Children.Add(new TextBlock
                {
                    Text = line,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 4)
                });
            }

            // --- Логотип команды ---
            var logo = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/kryg2.png", UriKind.Absolute)),
                Height = 60,
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            NotesContainer.Children.Add(logo);

            // --- Копирайт ---
            NotesContainer.Children.Add(new TextBlock
            {
                Text = "©Mozimer Russia Entertainment",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White
            });
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            timer?.Stop();
            systemTimer?.Stop();
            weatherTimer?.Stop();
            timeCheckTimer?.Stop();
            typewriterTimer?.Stop();
            pauseTimer?.Stop();
            phraseTimer?.Stop();
            cycleTimer?.Stop();
        }

        //private void UpdateGreeting()
        //{
        //    try
        //    {
        //        // Получаем имя пользователя Windows
        //        string userName = Environment.UserName;

        //        // Получаем время суток для приветствия
        //        int hour = DateTime.Now.Hour;
        //        string greeting;

        //        if (hour >= 6 && hour < 12)
        //            greeting = "Доброе утро";
        //        else if (hour >= 12 && hour < 18)
        //            greeting = "Добрый день";
        //        else if (hour >= 18 && hour < 23)
        //            greeting = "Добрый вечер";
        //        else
        //            greeting = "Доброй ночи";

        //        GreetingText.Text = $"{greeting}, {userName}!";
        //    }
        //    catch
        //    {
        //        GreetingText.Text = "Привет!";
        //    }
        //}

        private DispatcherTimer greetingTimer;
        private DispatcherTimer timeCheckTimer; // Новый таймер для проверки времени
        private DispatcherTimer typewriterTimer;
        private DispatcherTimer pauseTimer;
        private DispatcherTimer phraseTimer;
        private DispatcherTimer cycleTimer;
        private string currentFullText = "";
        private int currentCharIndex = 0;
        private bool isTyping = false;
        private Random random = new Random();
        private string lastGreeting = ""; // Запоминаем последнее приветствие

        // Список фразочек после приветствия
        private List<string> phrases = new List<string>
        {
            //"Как дела сегодня?",
            //"Надеюсь, твой день идёт отлично!",
            //"Время творить магию!",
            //"Ты сегодня красавчик!",
            //"Погнали работать!",
            //"Кофе уже выпил?",
            //"Продуктивного дня!",
            //"Всё получится!",
            //"Давай сделаем это!",
            //"Ты справишься!",
            //"Отличного настроения!",
            //"Время побед!",
            //"Сегодня твой день!",
            //"Вперёд к целям!",
            //"Успехов тебе!"
                "День ждёт, не заставляй его скучать.",
                "Каждое утро — шанс всё изменить!",
                "Начни с улыбки — остальное приложится!",
                "Сегодня будет круто, вот увидишь!",
                "Кофе за тебя не выпьется!",
                "Эй, не тормози — пора творить!",
                "Успех уже под дверью, открой ему!",
                "Сделай первый шаг, дальше будет легче!",
                "Иногда главное — просто начать!",
                "Ты можешь всё, просто поверь!",
                "Настоящее время — это сейчас!",
                "Не жди вдохновения — будь им!",
                "Сегодня ты — главный персонаж!",
                "Сосредоточься. Глубокий вдох. Погнали.",
                "Пусть день будет добр к тебе.",
                "Сделай что-то классное уже сегодня!",
                "Мир не станет лучше без твоего участия!",
                "Всё получится — просто действуй!",
                "Даже маленький шаг — движение вперёд!",
                "Ошибки — это часть пути, не стоп-знак!",
                "Новая идея? Пора воплотить!",
                "Пора в бой, командир!",
                "Настоящая магия — это упорство!",
                "Улыбнись. Ты справишься.",
                "Будь тем, кем хотел быть вчера!",
                "Главное — не идеально, а искренне!",
                "Всё возможно. Просто сделай шаг.",
                "Даже звёзды не загораются сразу!",
                "Ты — свой главный проект!",
                "Пусть будет день без сожалений!",
                "Каждая мелочь — часть большого дела!",
                "Энергия дня — у тебя в руках!",
                "Просто живи этот день красиво.",
                "Настройся на волну удачи!",
                "Пусть сегодня всё сложится!"
        };

        // Вызови это в конструкторе окна или в Loaded
        private void InitializeGreeting()
        {
            // Таймер для проверки времени суток каждые 30 секунд (чаще проверяем)
            timeCheckTimer = new DispatcherTimer();
            timeCheckTimer.Interval = TimeSpan.FromSeconds(30); // Было FromMinutes(1)
            timeCheckTimer.Tick += (s, e) => CheckTimeOfDay();
            timeCheckTimer.Start();

            // Запускаем первый цикл сразу
            lastGreeting = GetGreeting();
            StartGreetingCycle();
        }

        // Проверяем изменилось ли время суток
        private void CheckTimeOfDay()
        {
            string currentGreeting = GetGreeting();

            // DEBUG
            //System.Diagnostics.Debug.WriteLine($"Проверка: текущее={currentGreeting}, последнее={lastGreeting}, час={DateTime.Now.Hour}");

            if (currentGreeting != lastGreeting)
            {
                // DEBUG
                //MessageBox.Show($"Время суток изменилось!\n{lastGreeting} → {currentGreeting}");

                lastGreeting = currentGreeting;
                StopAllTimers();
                isTyping = false;
                GreetingText.Text = "";
                StartGreetingCycle();
            }
        }

        // Останавливаем все таймеры цикла
        private void StopAllTimers()
        {
            pauseTimer?.Stop();
            phraseTimer?.Stop();
            cycleTimer?.Stop();
            typewriterTimer?.Stop();

            pauseTimer = null;
            phraseTimer = null;
            cycleTimer = null;
            typewriterTimer = null;
        }

        private void StartGreetingCycle()
        {
            if (isTyping) return;

            // Получаем актуальное приветствие
            string greeting = GetGreeting();
            lastGreeting = greeting; // Обновляем последнее приветствие
            string userName = Environment.UserName;
            string greetingText = $"{greeting}, {userName}!";

            // Показываем приветствие с анимацией
            TypeText(greetingText, () =>
            {
                // После приветствия ждём 3 секунды
                if (pauseTimer != null)
                {
                    pauseTimer.Stop();
                    pauseTimer = null;
                }

                pauseTimer = new DispatcherTimer();
                pauseTimer.Interval = TimeSpan.FromSeconds(3);
                pauseTimer.Tick += (s, e) =>
                {
                    pauseTimer.Stop();
                    // Стираем текст
                    EraseText(() =>
                    {
                        // Показываем случайную фразу
                        string phrase = phrases[random.Next(phrases.Count)];
                        TypeText(phrase, () =>
                        {
                            // После фразы ждём 5 секунд
                            if (phraseTimer != null)
                            {
                                phraseTimer.Stop();
                                phraseTimer = null;
                            }

                            phraseTimer = new DispatcherTimer();
                            phraseTimer.Interval = TimeSpan.FromSeconds(5);
                            phraseTimer.Tick += (s2, e2) =>
                            {
                                phraseTimer.Stop();
                                // Стираем фразу и начинаем цикл заново
                                EraseText(() =>
                                {
                                    // Ждём 2 секунды перед новым циклом
                                    if (cycleTimer != null)
                                    {
                                        cycleTimer.Stop();
                                        cycleTimer = null;
                                    }

                                    cycleTimer = new DispatcherTimer();
                                    cycleTimer.Interval = TimeSpan.FromSeconds(2);
                                    cycleTimer.Tick += (s3, e3) =>
                                    {
                                        cycleTimer.Stop();
                                        StartGreetingCycle();
                                    };
                                    cycleTimer.Start();
                                });
                            };
                            phraseTimer.Start();
                        });
                    });
                };
                pauseTimer.Start();
            });
        }

        private string GetGreeting()
        {
            int hour = DateTime.Now.Hour;
            if (hour >= 6 && hour < 12)
                return "Доброе утро";
            else if (hour >= 12 && hour < 18)
                return "Добрый день";
            else if (hour >= 18 && hour < 23)
                return "Добрый вечер";
            else
                return "Доброй ночи";
        }

        // Анимация печати текста
        private void TypeText(string text, Action onComplete = null)
        {
            if (isTyping) return;

            isTyping = true;
            currentFullText = text;
            currentCharIndex = 0;
            GreetingText.Text = "";

            typewriterTimer = new DispatcherTimer();
            typewriterTimer.Interval = TimeSpan.FromMilliseconds(50); // Скорость печати
            typewriterTimer.Tick += (s, e) =>
            {
                if (currentCharIndex < currentFullText.Length)
                {
                    GreetingText.Text += currentFullText[currentCharIndex];
                    currentCharIndex++;
                }
                else
                {
                    typewriterTimer.Stop();
                    isTyping = false;
                    onComplete?.Invoke();
                }
            };
            typewriterTimer.Start();
        }

        // Анимация стирания текста
        private void EraseText(Action onComplete = null)
        {
            if (isTyping) return;

            isTyping = true;
            currentFullText = GreetingText.Text;
            currentCharIndex = currentFullText.Length;

            typewriterTimer = new DispatcherTimer();
            typewriterTimer.Interval = TimeSpan.FromMilliseconds(30); // Скорость стирания (быстрее)
            typewriterTimer.Tick += (s, e) =>
            {
                if (currentCharIndex > 0)
                {
                    currentCharIndex--;
                    GreetingText.Text = currentFullText.Substring(0, currentCharIndex);
                }
                else
                {
                    typewriterTimer.Stop();
                    isTyping = false;
                    onComplete?.Invoke();
                }
            };
            typewriterTimer.Start();
        }

        //private void PositionWindowBottomLeft()
        //{
        //    var workArea = SystemParameters.WorkArea;
        //    this.Left = workArea.Left;
        //    this.Top = workArea.Bottom - this.Height;
        //}
        private void PositionWindowBottomRight()
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Right - this.Width + 10; // прижимаем к правому краю
            this.Top = workArea.Bottom - this.Height + 10; // прижимаем к нижнему краю
        }

        private PerformanceCounter cpuCounter;

        private void InitializeSystemMonitor()
        {
            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //cpuCounter.NextValue(); // первый вызов — просто инициализация

            systemTimer = new DispatcherTimer();
            systemTimer.Interval = TimeSpan.FromSeconds(1);
            systemTimer.Tick += SystemTimer_Tick;
            systemTimer.Start();
        }

        private void SystemTimer_Tick(object sender, EventArgs e)
        {
            UpdateSystemInfo();
        }

        private void UpdateSystemInfo()
        {
            try
            {
                // Проверяем, есть ли батарея
                bool isLaptop = false;
                int batteryPercent = -1;
                bool charging = false;

                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                    {
                        foreach (var battery in searcher.Get())
                        {
                            isLaptop = true;
                            batteryPercent = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
                            charging = Convert.ToInt32(battery["BatteryStatus"]) == 2; // 2 = заряжается
                        }
                    }
                }
                catch { }

                if (isLaptop)
                {
                    // Ноутбук — показываем батарею
                    string batteryIcon = charging ? "🔌" :
                                         (batteryPercent > 75 ? "🔋" :
                                         (batteryPercent > 25 ? "🔋" : "🪫"));

                    Dispatcher.Invoke(() =>
                    {
                        SystemIcon.Text = batteryIcon;
                        SystemText.Text = $"{batteryPercent}";
                    });
                }
                else
                {
                    // Стационарный ПК — показываем процессор
                    var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                    float cpuLoad = cpuCounter.NextValue();
                    System.Threading.Thread.Sleep(100); // небольшой пауз, чтобы значение обновилось
                    cpuLoad = cpuCounter.NextValue();

                    Dispatcher.Invoke(() =>
                    {
                        SystemIcon.Text = "🖥";
                        SystemText.Text = $"{Math.Round(cpuLoad)}";
                    });
                }
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    SystemIcon.Text = "⚡";
                    SystemText.Text = "";
                });
            }
        }

        //private void UpdateSystemInfo()
        //{
        //    Task.Run(() =>
        //    {
        //        string display = "";
        //        try
        //        {
        //            bool isLaptop = false;
        //            int batteryPercent = -1;
        //            bool charging = false;

        //            try
        //            {
        //                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");
        //                foreach (var battery in searcher.Get())
        //                {
        //                    //isLaptop = true;
        //                    //batteryPercent = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
        //                    //charging = Convert.ToInt32(battery["BatteryStatus"]) == 2;
        //                }
        //            }
        //            catch { }

        //            if (isLaptop)
        //            {
        //                string batteryIcon = charging ? "🔌" :
        //                                     (batteryPercent > 75 ? "🔋" :
        //                                     (batteryPercent > 25 ? "🔋" : "🪫"));
        //                //display = $"{batteryIcon}{batteryPercent}%";
        //                Dispatcher.Invoke(() =>
        //                {
        //                    SystemIcon.Text = batteryIcon;
        //                    SystemText.Text = $"{batteryPercent}%";
        //                });
        //            }
        //            else
        //            {
        //                var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        //                cpuCounter.NextValue();
        //                System.Threading.Thread.Sleep(100);
        //                float cpuLoad = cpuCounter.NextValue();
        //                //display = $"🖥 {Math.Round(cpuLoad)}";
        //                Dispatcher.Invoke(() =>
        //                {
        //                    SystemIcon.Text = "🖥";
        //                    SystemText.Text = $"{Math.Round(cpuLoad)}";
        //                });
        //            }
        //        }
        //        catch
        //        {
        //            //display = "⚡";
        //            Dispatcher.Invoke(() =>
        //            {
        //                SystemIcon.Text = "⚡";
        //                SystemText.Text = "";
        //            });
        //        }

        //        //Dispatcher.Invoke(() => SystemText.Text = display);
        //    });
        //}





        private async void UpdateWeather()
        {
            await UpdateWeatherButtonOnlyAsync();
        }

        private void NotesButton_Click(object sender, RoutedEventArgs e)
        {
            ResumePageTimer();
            isNotesMode = !isNotesMode;
            if (isNotesMode)
            {
                ShowNotesOnTablet();
            }
            else
            {
                ShowTime(); // возвращаемся к часам
            }
        }

        private void LoadNotes()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(notesFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                if (File.Exists(notesFile))
                {
                    string json = File.ReadAllText(notesFile);
                    notes = JsonConvert.DeserializeObject<List<string>>(json) ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заметок: " + ex.Message);
            }
        }

        private void SaveNotes()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(notesFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(notes, Formatting.Indented);
                File.WriteAllText(notesFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void ShowNotesOnTablet()
        {
            isNotesMode = true;
            InfoPanel.Visibility = Visibility.Visible;
            TimePanel.Visibility = Visibility.Collapsed;

            if (notesLoaded)
                RebuildNotesList();
            else
                this.Dispatcher.InvokeAsync(RebuildNotesList, DispatcherPriority.Loaded);

            NotesContainer.Children.Clear();

            // Заголовок
            var title = new TextBlock
            {
                Text = "Заметки",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };
            NotesContainer.Children.Add(title);

            // Если нет заметок
            if (notes.Count == 0)
            {
                var empty = new TextBlock
                {
                    Text = "Заметок нет.\nНажмите ➕ Добавить",
                    Foreground = new SolidColorBrush(Colors.Gray),
                    FontStyle = FontStyles.Italic,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                    Margin = new Thickness(10)
                };
                NotesContainer.Children.Add(empty);
            }
            else
            {
                // Все заметки
                for (int i = 0; i < notes.Count; i++)
                {
                    int index = i;
                    string noteText = notes[i];

                    var border = new Border
                    {
                        Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                        CornerRadius = new CornerRadius(8),
                        Padding = new Thickness(12),
                        Margin = new Thickness(0, 0, 0, 10)
                    };

                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                    var textBlock = new TextBlock
                    {
                        Text = noteText,
                        Foreground = Brushes.White,
                        TextWrapping = TextWrapping.Wrap,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(textBlock, 0);

                    var deleteBtn = new Button
                    {
                        Content = "🗑️",
                        Width = 32,
                        Height = 32,
                        Background = new SolidColorBrush(Color.FromRgb(200, 60, 60)),
                        Foreground = Brushes.White,
                        Margin = new Thickness(10, 0, 0, 0)
                    };
                    Grid.SetColumn(deleteBtn, 1);

                    grid.Children.Add(textBlock);
                    grid.Children.Add(deleteBtn);
                    border.Child = grid;

                    NotesContainer.Children.Add(border);
                }
            }

            // === ПОЛЕ ВВОДА + КНОПКА ===
            var inputGrid = new Grid
            {
                Margin = new Thickness(0, 15, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new TextBox
            {
                Name = "NoteInputBox",
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                FontSize = 14,
                CaretBrush = Brushes.White,
                SelectionBrush = new SolidColorBrush(Color.FromRgb(74, 124, 89)),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 130
            };

            textBox.GotFocus += NotesTextBox_GotFocus;
            textBox.LostFocus += NotesTextBox_LostFocus;

            var addBtn = new Button
            {
                Content = "Plus",
                Width = 40,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(74, 124, 89)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0)
            };

            addBtn.Click += (s, e) =>
            {
                string text = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    notes.Add(text);
                    SaveNotes();
                    textBox.Text = "";
                    RebuildNotesList();
                }
                textBox.Focus();
            };

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    addBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };

            Grid.SetColumn(textBox, 0);
            Grid.SetColumn(addBtn, 1);
            inputGrid.Children.Add(textBox);
            inputGrid.Children.Add(addBtn);

            NotesContainer.Children.Add(inputGrid);
        }
        private void RebuildNotesList()
        {
            NotesContainer.Children.Clear();

            // === ЗАГОЛОВОК ===
            NotesContainer.Children.Add(new TextBlock
            {
                Text = "Заметки",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 15)
            });

            // === ЗАМЕТКИ ===
            for (int i = 0; i < notes.Count; i++)
            {
                int index = i;
                string note = notes[i];

                var border = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(12),
                    Margin = new Thickness(0, 0, 0, 8),
                    // ← ВАЖНО: НЕ ОГРАНИЧИВАЕМ ВЫСОТУ!
                };

                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                // === ТЕКСТ С ПЕРЕНОСОМ ===
                var textBlock = new TextBlock
                {
                    Text = note,
                    Foreground = Brushes.White,
                    FontSize = 15,
                    TextWrapping = TextWrapping.Wrap,  // ← ПЕРЕНОС ПО СЛОВАМ
                    LineHeight = 20,                   // ← Красивый межстрочный
                    MaxWidth = 180                     // ← Ограничиваем ширину текста
                };

                Grid.SetColumn(textBlock, 0);
                grid.Children.Add(textBlock);

                // === КНОПКА УДАЛЕНИЯ ===
                var deleteBtn = new Button
                {
                    Content = "🗑",
                    Width = 32,
                    Height = 32,
                    Background = new SolidColorBrush(Color.FromRgb(200, 60, 60)),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    VerticalAlignment = VerticalAlignment.Top // ← Прижать к верху
                };

                deleteBtn.Click += (s, e) =>
                {
                    notes.RemoveAt(index);
                    SaveNotes();
                    RebuildNotesList();
                };

                Grid.SetColumn(deleteBtn, 1);
                grid.Children.Add(deleteBtn);

                border.Child = grid;
                NotesContainer.Children.Add(border);
            }

            // === ПОЛЕ ВВОДА + КНОПКА ===
            var inputGrid = new Grid
            {
                Margin = new Thickness(0, 15, 0, 10),
                MaxWidth = 240,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 130
            };
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            inputGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var textBox = new TextBox
            {
                Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)),
                Foreground = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10),
                FontSize = 14,
                CaretBrush = Brushes.White,
                VerticalContentAlignment = VerticalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Right,
                MinWidth = 130
            };

            textBox.GotFocus += NotesTextBox_GotFocus;
            textBox.LostFocus += NotesTextBox_LostFocus;

            var addBtn = new Button
            {
                //Content = "Plus",
                //Width = 40,
                //Height = 40,
                //Background = new SolidColorBrush(Color.FromRgb(74, 124, 89)),
                //Foreground = Brushes.White,
                //BorderThickness = new Thickness(0),
                //Margin = new Thickness(8, 0, 0, 0),
                //Cursor = Cursors.Hand
            };

            addBtn.Click += (s, e) =>
            {
                string text = textBox.Text.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    notes.Add(text);
                    SaveNotes();
                    textBox.Text = "";
                    RebuildNotesList();
                    textBox.Focus();
                }
            };

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                    addBtn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            };

            Grid.SetColumn(textBox, 0);
            Grid.SetColumn(addBtn, 1);
            inputGrid.Children.Add(textBox);
            inputGrid.Children.Add(addBtn);

            NotesContainer.Children.Add(inputGrid);
        }

        private async void WeatherButton_Click(object sender, RoutedEventArgs e)
        {
            ResumePageTimer();
            shouldShowWeatherOnTablet = true; // принудительно показываем
            await RefreshWeatherAsync();
        }

        public class WeatherData
        {
            public string FullInfo { get; set; } = "";
            public double Temperature { get; set; }
            public string Condition { get; set; } = ""; // НОВОЕ
        }

        private static readonly Dictionary<string, string> WeatherEmojiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // Ясно
            ["Clear"] = "☀️",
            ["Sunny"] = "☀️",
            ["Ясно"] = "☀️",

            // Облачно / частично облачно
            ["Partly cloudy"] = "⛅",
            ["Partly cloudy and clear"] = "⛅",
            ["Mostly cloudy"] = "☁️",
            ["Cloudy"] = "☁️",
            ["Overcast"] = "☁️",
            ["Частично облачно"] = "⛅",
            ["Пасмурно"] = "☁️",

            // Дождь
            ["Light rain"] = "🌦️",
            ["Moderate rain"] = "🌧️",
            ["Heavy rain"] = "🌧️💧",
            ["Showers"] = "🌧️",
            ["Rain"] = "🌧️",
            ["Rain and snow"] = "🌨️",
            ["Дождь"] = "🌧️",
            ["Ливень"] = "🌦️",

            // Снег
            ["Snow"] = "❄️",
            ["Light snow"] = "🌨️",
            ["Heavy snow"] = "❄️❄️",
            ["Flurries"] = "🌨️",
            ["Снег"] = "❄️",
            ["Метель"] = "❄️🌬️",

            // Дождь со снегом / смешанные осадки
            ["Rain/snow"] = "🌨️",
            ["Sleet"] = "🧊❄️",
            ["Freezing rain"] = "🧊🌧️",
            ["Ice pellets"] = "🧊❄️",
            ["Rain and ice pellets"] = "🧊🌧️",

            // Туман, дым, пыль
            ["Fog"] = "🌫️",
            ["Mist"] = "🌫️",
            ["Haze"] = "🌫️",
            ["Smoke"] = "💨🕳️",
            ["Dust"] = "💨🟫",
            ["Туман"] = "🌫️",
            ["Дым"] = "💨🕳️",
            ["Пыль"] = "💨🟫",

            // Гроза
            ["Thunderstorm"] = "⛈️",
            ["Thunder"] = "⛈️",
            ["Гроза"] = "⛈️",

            // Ветер
            ["Windy"] = "💨",
            ["Wind"] = "💨",
            ["Ветрено"] = "💨",

            // По умолчанию
            ["Unknown"] = "",
            ["Неизвестно"] = ""
        };

        private string GetWeatherEmoji(string condition)
        {
            var c = condition.ToLowerInvariant();

            if (c.Contains("снег")) return "❄️";       // любое упоминание снега
            if (c.Contains("дождь")) return "🌧️";
            if (c.Contains("гроза")) return "⛈️";
            if (c.Contains("облачно") || c.Contains("пасмурно")) return "☁️";
            if (c.Contains("ясно") || c.Contains("солнечно")) return "☀️";
            if (c.Contains("туман") || c.Contains("дым") || c.Contains("пыль")) return "🌫️";
            if (c.Contains("ветр")) return "💨";

            return ""; // по умолчанию
        }
        private async Task<WeatherData> GetWeatherAsync(string city)
        {
            try
            {
                string apiKey = "94cdfe16e6cb4f48a46144940250711";
                string url = $"http://api.weatherapi.com/v1/current.json?key={apiKey}&q={city}&lang=ru";

                using HttpClient client = new HttpClient();
                var response = await client.GetStringAsync(url);

                using JsonDocument doc = JsonDocument.Parse(response);
                var root = doc.RootElement;

                string location = root.GetProperty("location").GetProperty("name").GetString()!;
                double temp = root.GetProperty("current").GetProperty("temp_c").GetDouble();
                int roundedTemp = (int)Math.Round(temp);

                string condition = root.GetProperty("current").GetProperty("condition").GetProperty("text").GetString()!;

                // Ветер в км/ч → переводим в м/с
                double windKph = root.GetProperty("current").GetProperty("wind_kph").GetDouble();
                double windMps = Math.Round(windKph / 3.6, 1); // 1 м/с = 3.6 км/ч

                string emoji = GetWeatherEmoji(condition);

                return new WeatherData
                {
                    FullInfo = $"{location}:\n🌡 {roundedTemp}°C\n{emoji} {condition}\n💨 Ветер: {windMps} м/с",
                    Temperature = roundedTemp,
                    Condition = condition
                };
            }
            catch
            {
                return new WeatherData
                {
                    FullInfo = "Погода недоступна",
                    Condition = "Unknown"
                };
            }
        }


        private void SystemButton_Click(object sender, RoutedEventArgs e)
        {
            ResumePageTimer();
            try
            {
                string info = GetSystemInfo();
                ShowInfo(info);
            }
            catch (Exception ex)
            {
                ShowInfo($"Ошибка: {ex.Message}");
            }
        }

        private void ShowInfo(string text)
        {
            isNotesMode = false;
            InfoPanel.Visibility = Visibility.Visible;
            TimePanel.Visibility = Visibility.Collapsed;

            NotesContainer.Children.Clear();

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                NotesContainer.Children.Add(new TextBlock { Text = line });
            }
        }

        private void ShowTime()
        {
            InfoPanel.Visibility = Visibility.Collapsed;
            TimePanel.Visibility = Visibility.Visible;
            isNotesMode = false; // ← ВОТ ЭТО КЛЮЧЕВОЕ
        }

        private DispatcherTimer weatherTimer;

        private void InitializeWeatherTimer()
        {
            weatherTimer = new DispatcherTimer();
            weatherTimer.Interval = TimeSpan.FromMinutes(17);
            weatherTimer.Tick += async (s, e) => await UpdateWeatherButtonOnlyAsync();
            weatherTimer.Start();
        }

        private async void WeatherTimer_Tick(object sender, EventArgs e)
        {
             RefreshWeatherAsync();
        }
        private async Task UpdateWeatherButtonOnlyAsync()
        {
            string city = Properties.Settings.Default.WeatherCity ?? "Москва";
            var weather = await GetWeatherAsync(city);
            int roundedTemp = (int)Math.Round(weather.Temperature);
            WeatherText.Text = $"{roundedTemp}°";
        }

        private void SetAutoStart(bool enable)
        {
            try
            {
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                string appName = "WidgetES";

                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (enable)
                    {
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        if (key.GetValue(appName) != null)
                            key.DeleteValue(appName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка автозапуска: {ex.Message}", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
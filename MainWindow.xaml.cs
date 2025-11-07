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

namespace WidgetES
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer timer;
        private int currentCharacter = 0;
        private List<string> characterImages;
        private DispatcherTimer systemTimer;
        private bool shouldShowWeatherOnTablet = false;
        private List<string> notes = new();
        private string notesFilePath;
        private bool isNotesMode = false;

        public MainWindow()
        {
            InitializeComponent();
            currentCharacter = Properties.Settings.Default.SelectedCharacter;
            InitializeCharacters();
            LoadCharacter(currentCharacter);
            InitializeTimer();
            InitializeSystemMonitor();
            UpdateDateTime();
            UpdateGreeting();
            InitializeWeatherTimer(); // <-- НОВЫЙ ТАЙМЕР
            UpdateWeather();
            notesFilePath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WidgetES", "notes.json"
            );

            LoadNotes();
            PositionWindowBottomRight();
            //UpdateWeatherButtonOnlyAsync();
            RefreshWeatherAsync();
        }

        private void InitializeCharacters()
        {
            // Список спрайтов персонажей (путь к файлам изображений)
            characterImages = new List<string>
            {
                "pack://application:,,,/Images/character1.png",
                "pack://application:,,,/Images/character2.png",
                "pack://application:,,,/Images/character3.png",
                "pack://application:,,,/Images/character4.png"
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
                    "- character4.png",
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
            ShowTime();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            string currentCity = Properties.Settings.Default.WeatherCity ?? "Moscow";

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
            string city = Properties.Settings.Default.WeatherCity ?? "Moscow";
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
            string city = Properties.Settings.Default.WeatherCity ?? "Moscow";
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
                string city = Properties.Settings.Default.WeatherCity ?? "Moscow";
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
            string helpText =
                "⏱ - Таймер\n" +
                "P - Будильник\n" +
                "M - Секундомер\n" +
                "⚙ - Настройки\n" +
                "? - Помощь\n\n" +
                "Перетаскивайте окно мышкой!";
            ShowInfo(helpText);
        }


        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            timer?.Stop();
            systemTimer?.Stop();
            weatherTimer?.Stop();
        }

        private void UpdateGreeting()
        {
            try
            {
                // Получаем имя пользователя Windows
                string userName = Environment.UserName;

                // Получаем время суток для приветствия
                int hour = DateTime.Now.Hour;
                string greeting;

                if (hour >= 6 && hour < 12)
                    greeting = "Доброе утро";
                else if (hour >= 12 && hour < 18)
                    greeting = "Добрый день";
                else if (hour >= 18 && hour < 23)
                    greeting = "Добрый вечер";
                else
                    greeting = "Доброй ночи";

                GreetingText.Text = $"{greeting}, {userName}!";
            }
            catch
            {
                GreetingText.Text = "Привет!";
            }
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

        private void InitializeSystemMonitor()
        {
            systemTimer = new DispatcherTimer();
            systemTimer.Interval = TimeSpan.FromSeconds(5); // Обновление каждые 5 секунд
            systemTimer.Tick += SystemTimer_Tick;
            systemTimer.Start();
            UpdateSystemInfo(); // Сразу обновляем
        }

        private void SystemTimer_Tick(object sender, EventArgs e)
        {
            UpdateSystemInfo();
        }

        private void UpdateSystemInfo()
        {
            try
            {
                int batteryPercent = 0;
                bool charging = false;

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                {
                    foreach (var battery in searcher.Get())
                    {
                        batteryPercent = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
                        charging = Convert.ToInt32(battery["BatteryStatus"]) == 2; // 2 = заряжается
                    }
                }

                string batteryIcon;
                if (charging)
                    batteryIcon = "🔌";
                else if (batteryPercent > 75)
                    batteryIcon = "🔋";
                else if (batteryPercent > 25)
                    batteryIcon = "🔋";
                else
                    batteryIcon = "🪫";

                SystemText.Text = $"{batteryIcon}{batteryPercent}";
            }
            catch
            {
                SystemText.Text = "⚡";
            }
        }


        private async void UpdateWeather()
        {
            await UpdateWeatherButtonOnlyAsync();
        }

        private void NotesButton_Click(object sender, RoutedEventArgs e)
        {
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
                if (File.Exists(notesFilePath))
                {
                    string json = File.ReadAllText(notesFilePath);
                    notes = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
                }
            }
            catch { notes = new List<string>(); }
        }

        private void SaveNotes()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(notesFilePath)!;
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(notesFilePath, json);
            }
            catch { }
        }

        private void ShowNotesOnTablet()
        {
            InfoPanel.Visibility = Visibility.Visible;
            TimePanel.Visibility = Visibility.Collapsed;

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
                    deleteBtn.Click += (s, e) => DeleteNote(index);
                    Grid.SetColumn(deleteBtn, 1);

                    grid.Children.Add(textBlock);
                    grid.Children.Add(deleteBtn);
                    border.Child = grid;

                    NotesContainer.Children.Add(border);
                }
            }

            // Кнопка добавить
            var addBtn = new Button
            {
                Content = "➕ Добавить",
                Width = 150,
                Height = 40,
                Background = new SolidColorBrush(Color.FromRgb(74, 124, 89)),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 10)
            };
            addBtn.Click += (s, e) => AddNote();

            NotesContainer.Children.Add(addBtn);
        }
        private void DeleteNote(int index)
        {
            if (MessageBox.Show("Удалить заметку?", "Подтверждение", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                notes.RemoveAt(index);
                SaveNotes();
                ShowNotesOnTablet();
            }
        }

        private void AddNote()
        {
            var input = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите текст заметки:", "Новая заметка", ""
            );
            if (!string.IsNullOrWhiteSpace(input))
            {
                notes.Add(input);
                SaveNotes();
                ShowNotesOnTablet();
            }
        }

        private async void WeatherButton_Click(object sender, RoutedEventArgs e)
        {
            shouldShowWeatherOnTablet = true; // принудительно показываем
            await RefreshWeatherAsync();
        }

        public class WeatherData
        {
            public string FullInfo { get; set; } = "";
            public double Temperature { get; set; }
            public string Condition { get; set; } = ""; // НОВОЕ
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
                

                return new WeatherData
                {
                    FullInfo = $"{location}: {roundedTemp}°C, {condition}",
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
            try
            {
                int batteryPercent = -1;
                string powerLine = "Неизвестно";

                using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery"))
                {
                    foreach (var battery in searcher.Get())
                    {
                        batteryPercent = Convert.ToInt32(battery["EstimatedChargeRemaining"]);
                        int status = Convert.ToInt32(battery["BatteryStatus"]);
                        powerLine = status == 2 ? "Подключено" : "От батареи";
                    }
                }
                if (batteryPercent < 0) batteryPercent = 100;

                var process = Process.GetCurrentProcess();
                var usedMemory = process.WorkingSet64 / 1024 / 1024;

                string info = $"Батарея: {batteryPercent}%\n" +
                              $"Питание: {powerLine}\n" +
                              $"Память приложения: {usedMemory} МБ";

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
            string city = Properties.Settings.Default.WeatherCity ?? "Moscow";
            var weather = await GetWeatherAsync(city);
            int roundedTemp = (int)Math.Round(weather.Temperature);
            WeatherText.Text = $"{roundedTemp}°";
        }
    }
}
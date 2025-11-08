using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WidgetES
{
    public partial class CharacterSelectionWindow : Window
    {
        public int SelectedCharacter { get; private set; }
        public string SelectedCity { get; private set; }
        private List<Button> characterButtons;
        private List<string> characterImages;

        public CharacterSelectionWindow(int currentSelection, string currentCity = "Moscow")
        {
            InitializeComponent();
            SelectedCharacter = currentSelection;
            SelectedCity = currentCity;

            InitializeCharacterData();
            LoadCharacterPreviews();
            UpdateSelection();

            // ВСТАВЛЯЕМ ГОРОД В ПОЛЕ
            CityTextBox.Text = currentCity;

            // Загружаем текущее состояние "Поверх всех окон"
            TopmostCheckBox.IsChecked = Properties.Settings.Default.AlwaysOnTop;
            AutoStartCheckBox.IsChecked = Properties.Settings.Default.AutoStart;
        }

        private void InitializeCharacterData()
        {
            characterButtons = new List<Button>
            {
                Character1Button,
                Character2Button,
                Character3Button,
                Character4Button,
                Character5Button,
                Character6Button,
                Character7Button,
                Character8Button,
                Character9Button,
                Character10Button
            };

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

        private void LoadCharacterPreviews()
        {
            List<Image> previewImages = new List<Image>
            {
                Character1Image,
                Character2Image,
                Character3Image,
                Character4Image,
                Character5Image,
                Character6Image,
                Character7Image,
                Character8Image,
                Character9Image,
                Character10Image
            };

            for (int i = 0; i < previewImages.Count; i++)
            {
                try
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(characterImages[i], UriKind.Absolute);
                    bitmap.EndInit();
                    previewImages[i].Source = bitmap;
                }
                catch
                {
                    // Если изображение не найдено, оставляем пустым
                    // Можно добавить заглушку или текст
                    previewImages[i].Source = null;
                }
            }
        }

        private void UpdateSelection()
        {
            for (int i = 0; i < characterButtons.Count; i++)
            {
                if (i == SelectedCharacter)
                {
                    characterButtons[i].Style = (Style)FindResource("SelectedButtonStyle");
                }
                else
                {
                    characterButtons[i].Style = (Style)FindResource("CharacterButtonStyle");
                }
            }
        }

        private void Character1Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 0;
            UpdateSelection();
        }

        private void Character2Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 1;
            UpdateSelection();
        }

        private void Character3Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 2;
            UpdateSelection();
        }

        private void Character4Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 3;
            UpdateSelection();
        }

        private void Character5Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 4;
            UpdateSelection();
        }

        private void Character6Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 5;
            UpdateSelection();
        }

        private void Character7Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 6;
            UpdateSelection();
        }

        private void Character8Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 7;
            UpdateSelection();
        }

        private void Character9Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 8;
            UpdateSelection();
        }

        private void Character10Button_Click(object sender, RoutedEventArgs e)
        {
            SelectedCharacter = 9;
            UpdateSelection();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCity = CityTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(SelectedCity))
                SelectedCity = "Moscow";

            Properties.Settings.Default.AlwaysOnTop = TopmostCheckBox.IsChecked == true;
            Properties.Settings.Default.AutoStart = AutoStartCheckBox.IsChecked == true;

            // Применяем автозапуск сразу
            SetAutoStart(Properties.Settings.Default.AutoStart);
            Properties.Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // В CharacterSelectionWindow.xaml.cs — добавь этот метод
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

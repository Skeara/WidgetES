using System;
using System.Collections.Generic;
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
        }

        private void InitializeCharacterData()
        {
            characterButtons = new List<Button>
            {
                Character1Button,
                Character2Button,
                Character3Button,
                Character4Button
            };

            characterImages = new List<string>
            {
                "pack://application:,,,/Images/character1.png",
                "pack://application:,,,/Images/character2.png",
                "pack://application:,,,/Images/character3.png",
                "pack://application:,,,/Images/character4.png"
            };
        }

        private void LoadCharacterPreviews()
        {
            List<Image> previewImages = new List<Image>
            {
                Character1Image,
                Character2Image,
                Character3Image,
                Character4Image
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

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SelectedCity = CityTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(SelectedCity))
                SelectedCity = "Moscow";

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

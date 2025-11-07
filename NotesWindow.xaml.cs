//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
//using System.Windows.Data;
//using System.Windows.Documents;
//using System.Windows.Input;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
//using System.Windows.Shapes;
//using System.IO;
//using System.Text.Json;

//namespace WidgetES
//{
//    public partial class NotesWindow : Window
//    {
//        private List<string> notes;
//        private string notesFilePath;

//        public NotesWindow()
//        {
//            InitializeComponent();
//            notesFilePath = System.IO.Path.Combine(
//                            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//                            "WidgetES",
//                            "notes.json"
//                        );
//            LoadNotes();
//            DisplayNotes();
//        }

//        private void LoadNotes()
//        {
//            try
//            {
//                if (File.Exists(notesFilePath))
//                {
//                    string json = File.ReadAllText(notesFilePath);
//                    notes = JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
//                }
//                else
//                {
//                    notes = new List<string>();
//                }
//            }
//            catch
//            {
//                notes = new List<string>();
//            }
//        }

//        private void SaveNotes()
//        {
//            try
//            {
//                string directory = System.IO.Path.GetDirectoryName(notesFilePath)!;
//                if (!Directory.Exists(directory))
//                {
//                    Directory.CreateDirectory(directory);
//                }

//                string json = JsonSerializer.Serialize(notes, new JsonSerializerOptions { WriteIndented = true });
//                File.WriteAllText(notesFilePath, json);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show($"Ошибка сохранения заметок: {ex.Message}",
//                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        private void DisplayNotes()
//        {
//            NotesPanel.Children.Clear();

//            if (notes.Count == 0)
//            {
//                TextBlock emptyText = new TextBlock
//                {
//                    Text = "Заметок пока нет. Нажмите '➕ Добавить' чтобы создать первую заметку.",
//                    Foreground = new SolidColorBrush(Color.FromRgb(150, 150, 150)),
//                    FontSize = 14,
//                    TextWrapping = TextWrapping.Wrap,
//                    Margin = new Thickness(10)
//                };
//                NotesPanel.Children.Add(emptyText);
//                return;
//            }

//            for (int i = 0; i < notes.Count; i++)
//            {
//                int index = i; // Копия для замыкания

//                Border noteBorder = new Border
//                {
//                    Background = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
//                    CornerRadius = new CornerRadius(5),
//                    Padding = new Thickness(10),
//                    Margin = new Thickness(0, 0, 0, 10)
//                };

//                Grid noteGrid = new Grid();
//                noteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
//                noteGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

//                TextBlock noteText = new TextBlock
//                {
//                    Text = notes[i],
//                    Foreground = Brushes.White,
//                    FontSize = 14,
//                    TextWrapping = TextWrapping.Wrap,
//                    VerticalAlignment = VerticalAlignment.Center
//                };
//                Grid.SetColumn(noteText, 0);

//                Button deleteButton = new Button
//                {
//                    Content = "🗑",
//                    Width = 30,
//                    Height = 30,
//                    Background = new SolidColorBrush(Color.FromRgb(200, 60, 60)),
//                    Foreground = Brushes.White,
//                    BorderThickness = new Thickness(0),
//                    Cursor = System.Windows.Input.Cursors.Hand,
//                    Margin = new Thickness(10, 0, 0, 0)
//                };
//                deleteButton.Click += (s, e) => DeleteNote(index);
//                Grid.SetColumn(deleteButton, 1);

//                noteGrid.Children.Add(noteText);
//                noteGrid.Children.Add(deleteButton);
//                noteBorder.Child = noteGrid;

//                NotesPanel.Children.Add(noteBorder);
//            }
//        }

//        private void AddNote_Click(object sender, RoutedEventArgs e)
//        {
//            AddNoteDialog dialog = new AddNoteDialog();
//            dialog.Owner = this;

//            if (dialog.ShowDialog() == true)
//            {
//                string noteText = dialog.NoteText;
//                if (!string.IsNullOrWhiteSpace(noteText))
//                {
//                    notes.Add(noteText);
//                    SaveNotes();
//                    DisplayNotes();
//                }
//            }
//        }

//        private void DeleteNote(int index)
//        {
//            var result = MessageBox.Show(
//                "Удалить эту заметку?",
//                "Подтверждение",
//                MessageBoxButton.YesNo,
//                MessageBoxImage.Question
//            );

//            if (result == MessageBoxResult.Yes)
//            {
//                notes.RemoveAt(index);
//                SaveNotes();
//                DisplayNotes();
//            }
//        }

//        private void Close_Click(object sender, RoutedEventArgs e)
//        {
//            this.Close();
//        }
//    }
//}
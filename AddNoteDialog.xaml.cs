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

//namespace WidgetES
//{
//    public partial class AddNoteDialog : Window
//    {
//        public string NoteText { get; private set; } = string.Empty;

//        public AddNoteDialog()
//        {
//            InitializeComponent();
//            NoteTextBox.Focus();
//        }

//        private void Ok_Click(object sender, RoutedEventArgs e)
//        {
//            NoteText = NoteTextBox.Text.Trim();
//            if (!string.IsNullOrWhiteSpace(NoteText))
//            {
//                DialogResult = true;
//                Close();
//            }
//            else
//            {
//                MessageBox.Show("Введите текст заметки", "Внимание",
//                    MessageBoxButton.OK, MessageBoxImage.Warning);
//            }
//        }

//        private void Cancel_Click(object sender, RoutedEventArgs e)
//        {
//            DialogResult = false;
//            Close();
//        }
//    }
//}

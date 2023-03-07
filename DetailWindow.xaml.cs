using System;
using System.IO;
using System.Windows;
using static System.Net.Mime.MediaTypeNames;

namespace ACDll
{
    public partial class DetailWindow : Window
    {
        public DetailWindow()
        {
            InitializeComponent();
            DetailTextblock.Text = LoadLogFile();
        }
        private string LoadLogFile()
        {
            var text = string.Empty;
            try
            {
                text = File.ReadAllText(Config.GetLogFileName());
            }
            catch (Exception)
            {
                MessageBox.Show("Лог файл пуст и/или отсутствует");
            }
            return text;
        }

        private void ClearDetailButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(Config.GetLogFileName()) || File.ReadAllText(Config.GetLogFileName()).Length == default)
            {
                MessageBox.Show("Лог файл пуст и/или отсутствует");
                return;
            }
            if (MessageBox.Show("Вы уверены?", "Внимание!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                File.Delete(Config.GetLogFileName());
                DetailTextblock.Text = string.Empty;
            }
        }
    }
}

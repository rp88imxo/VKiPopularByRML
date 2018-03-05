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
using System.Windows.Navigation;
using System.Windows.Shapes;
using VkNet;
using VkNet.Enums.Filters;

namespace VKiPopularByRML
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window
    {

        public static VkApi vkApi;
        public MainWindow()
        {
            InitializeComponent();
            
            try
            {
                vkApi = new VkApi();   
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message,"Ошибка");
                this.Close();
            }
        }
        
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Settings set = Settings.All;
            ApiAuthParams apiAuthParams = new ApiAuthParams();

            if (ulong.TryParse(TextBox.Text, out ulong res))
                apiAuthParams.ApplicationId = res;

            apiAuthParams.Login = TextBoxEmail.Text;
            apiAuthParams.Password = TextBoxPass.Text;
            apiAuthParams.Settings = set;
            try
            {
                vkApi.Authorize(apiAuthParams);

                Hide();
                MainPage mainPage = new MainPage();
                mainPage.Owner = this;
                mainPage.Show();
                
            }
            catch (Exception)
            {
                MessageBox.Show("Неверные данные","Ошибка");
            }
            
        }

        private void TextBoxEmail_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TextBoxPass_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TextBox.Text != "" && TextBoxEmail.Text != "" && TextBoxPass.Text != "")
                Button.IsEnabled = true;
            else
                Button.IsEnabled = false;
        }
    }
}

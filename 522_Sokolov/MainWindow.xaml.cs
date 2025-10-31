using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace _522_Sokolov
{
    public partial class MainWindow : Window
    {
        private bool _isCustomTheme = false;

        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigated += MainFrame_Navigated;
        }

        private void MainFrame_Navigated(object sender, NavigationEventArgs e)
        {
            if (MainFrame.Content is Page page)
            {
                switch (page.Title)
                {
                    case "AddCategoryPage":
                        Title = "Добавить категорию";
                        break;
                    case "AddPaymentPage":
                        Title = "Добавить платеж";
                        break;
                    case "AddUserPage":
                        Title = "Добавить пользователя";
                        break;
                    case "AdminPage":
                        Title = "Панель администратора";
                        break;
                    case "AuthPage":
                        Title = "Авторизация";
                        break;
                    case "CategoryTabPage":
                        Title = "Таблица категорий";
                        break;
                    case "ChangePassPage":
                        Title = "Смена пароля";
                        break;
                    case "DiagrammPage":
                        Title = "Диаграммы";
                        break;
                    case "PaymentTabPage":
                        Title = "Таблица платежей";
                        break;
                    case "RegPage":
                        Title = "Регистрация";
                        break;
                    case "UserPage":
                        Title = "Пользователи";
                        break;
                    case "UsersTabPage":
                        Title = "Таблица пользователей";
                        break;
                    default:
                        Title = "MainWindow";
                        break;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new Pages.AuthPage());
            var timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.IsEnabled = true;
            timer.Tick += (o, t) =>
            {
                DateTimeNow.Text = DateTime.Now.ToString();
            };
            timer.Start();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainFrame.CanGoBack)
            {
                MainFrame.GoBack();
            }
        }

        private void ThemeButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchTheme();
        }

        private void SwitchTheme()
        {
            string dictionaryPath = _isCustomTheme ? "StandardStyles.xaml" : "CustomStyles.xaml";
            ThemeButton.Content = _isCustomTheme ? "Кастомная тема" : "Стандартная тема";

            var uri = new Uri(dictionaryPath, UriKind.Relative);
            ResourceDictionary newDict = Application.LoadComponent(uri) as ResourceDictionary;

            var currentDict = Application.Current.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null && (d.Source.OriginalString == "StandardStyles.xaml" || d.Source.OriginalString == "CustomStyles.xaml"));
            if (currentDict != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(currentDict);
            }
            Application.Current.Resources.MergedDictionaries.Add(newDict);

            BackButton.ClearValue(FrameworkElement.StyleProperty);
            BackButton.SetResourceReference(FrameworkElement.StyleProperty, "BaseButtonStyle");
            ThemeButton.ClearValue(FrameworkElement.StyleProperty);
            ThemeButton.SetResourceReference(FrameworkElement.StyleProperty, "BaseButtonStyle");
            DateTimeNow.ClearValue(FrameworkElement.StyleProperty);
            DateTimeNow.SetResourceReference(FrameworkElement.StyleProperty, "BaseTextStyle");

            if (MainFrame.Content is Page page)
            {
                var pageDict = page.Resources.MergedDictionaries
                    .FirstOrDefault(d => d.Source != null && (d.Source.OriginalString == "StandardStyles.xaml" || d.Source.OriginalString == "CustomStyles.xaml"));
                if (pageDict != null)
                {
                    page.Resources.MergedDictionaries.Remove(pageDict);
                }
                page.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = uri });

                page.InvalidateVisual();
                page.UpdateLayout();
            }

            this.InvalidateVisual();
            this.UpdateLayout();

            _isCustomTheme = !_isCustomTheme;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("Вы уверены, что хотите закрыть окно?", "Message", MessageBoxButton.YesNo) == MessageBoxResult.No)
            {
                e.Cancel = true;
            }
            else
            {
                e.Cancel = false;
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

namespace _522_Sokolov.Pages
{
    /// <summary>
    /// Страница для смены пароля пользователя
    /// </summary>
    public partial class ChangePassPage : Page
    {
        public ChangePassPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Генерирует SHA1 хеш для пароля
        /// </summary>
        public static string GetHash(String password)
        {
            using (var hash = SHA1.Create())
            {
                return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(password)).Select(x => x.ToString("X2")));
            }
        }

        /// <summary>
        /// Сохраняет новый пароль после проверки всех условий
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CurrentPasswordBox.Password) ||
                string.IsNullOrEmpty(NewPasswordBox.Password) ||
                string.IsNullOrEmpty(ConfirmPasswordBox.Password) ||
                string.IsNullOrEmpty(TbLogin.Text))
            {
                MessageBox.Show("Все поля обязательны к заполнению!");
                return;
            }

            using (var db = new Entities())
            {
                string hashedPass = GetHash(CurrentPasswordBox.Password);
                var user = db.User.FirstOrDefault(u => u.Login == TbLogin.Text && u.Password == hashedPass);

                if (user == null)
                {
                    MessageBox.Show("Текущий пароль/Логин неверный!");
                    return;
                }

                if (NewPasswordBox.Password.Length >= 6)
                {
                    bool en = true;
                    bool number = false;

                    for (int i = 0; i < NewPasswordBox.Password.Length; i++)
                    {
                        if (NewPasswordBox.Password[i] >= '0' && NewPasswordBox.Password[i] <= '9') number = true;
                        else if (!((NewPasswordBox.Password[i] >= 'A' && NewPasswordBox.Password[i] <= 'Z') || (NewPasswordBox.Password[i] >= 'a' && NewPasswordBox.Password[i] <= 'z'))) en = false;
                    }

                    if (!en)
                        MessageBox.Show("Используйте только английскую расскладку!");
                    else if (!number)
                        MessageBox.Show("Добавьте хотябы одну цифру!");

                    if (en && number)
                    {
                        if (NewPasswordBox.Password != ConfirmPasswordBox.Password)
                        {
                            MessageBox.Show("Пароли не совпадают!");
                        }
                        else
                        {
                            user.Password = GetHash(NewPasswordBox.Password);
                            db.SaveChanges();
                            MessageBox.Show("Пароль успешно изменен!");
                            NavigationService?.Navigate(new AuthPage());
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Пароль слишком короткий, должно быть минимум 6 символов!");
                }
            }
        }
    }
}
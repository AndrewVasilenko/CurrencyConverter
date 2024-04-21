using System.Windows.Controls;
using System.Windows;
using System;

namespace CurrencyConverter
{
    public partial class MessageBox : Window
    {
        public MessageBox()
        {
            InitializeComponent();
        }

        void AddButtons(MessageBoxButton buttons)
        {
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    AddButton("Добре", MessageBoxResult.OK);
                    break;
                case MessageBoxButton.OKCancel:
                    AddButton("Добре", MessageBoxResult.OK);
                    AddButton("Відміна", MessageBoxResult.Cancel, isCancel: true);
                    break;
                case MessageBoxButton.YesNo:
                    AddButton("Так", MessageBoxResult.Yes);
                    AddButton("Ні", MessageBoxResult.No);
                    break;
                case MessageBoxButton.YesNoCancel:
                    AddButton("Так", MessageBoxResult.Yes);
                    AddButton("Ні", MessageBoxResult.No);
                    AddButton("Відмінити", MessageBoxResult.Cancel, isCancel: true);
                    break;
                default:
                    throw new ArgumentException("Unknown button value", "buttons");
            }
        }

        void AddButton(string text, MessageBoxResult result, bool isCancel = false)
        {
            var button = new Button() { Content = text, IsCancel = isCancel, Margin = new Thickness(10) };
            button.Click += (o, args) => { Result = result; DialogResult = true; };

            // Додає стиль Material Design для кнопок
            button.Style = (Style)FindResource("MaterialDesignRaisedButton");

            ButtonContainer.Children.Add(button);
        }

        MessageBoxResult Result = MessageBoxResult.None;

        public static MessageBoxResult Show(string caption, string message,
                                            MessageBoxButton buttons)
        {
            var dialog = new MessageBox() { Title = caption };
            dialog.MessageContainer.Text = message;
            dialog.AddButtons(buttons);
            dialog.ShowDialog();
            return dialog.Result;
        }
    }
}

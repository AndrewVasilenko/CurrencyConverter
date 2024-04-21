using System;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
using Newtonsoft.Json.Linq;
using System.IO;

namespace CurrencyConverter
{
    public partial class MainWindow : Window
    {
        //Клас для зберігання історії конвертації
        public class ConversionHistoryItem
        {
            public string BaseCurrency { get; set; }
            public string TargetCurrency { get; set; }
            public DateTime Date { get; set; }
            public string RateDate { get; set; }
            public double ExchangeRate { get; set; }
            public double QuantityBaseCurrency { get; set; }
            public double QuantityTargetCurrency { get; set; }
        }

        //Прапорець, який вказує чи включена обробка подій для ComboBox
        private bool isComboBoxEventDisabled = false;

        //Створення колекції для зберігання історії конвертації
        private List<ConversionHistoryItem> conversionHistory = new List<ConversionHistoryItem>();

        //Метод для додавання нового запису про конвертацію до історії обміну валют.
        public void AddConversionToHistory(string baseCurrencyCode, string targetCurrencyCode, double exchangeRate, string _RateDate)
        {
            // Створення нового об'єкта ConversionHistoryItem
            ConversionHistoryItem historyItem = new ConversionHistoryItem
            {
                BaseCurrency = baseCurrencyCode,
                TargetCurrency = targetCurrencyCode,
                Date = DateTime.Now, // або використайте обрану вами дату
                RateDate = _RateDate,
                ExchangeRate = exchangeRate,
                QuantityBaseCurrency = double.Parse(QuantityBaseCurrencyTextBlock.Text),
                QuantityTargetCurrency = double.Parse(QuantityTargetCurrencyTextBlock.Text)
            };

            // Додавання об'єкта до історії
            conversionHistory.Add(historyItem);

            // Оновлення відображення історії в TextBlock
            UpdateHistoryTextBlock();
        }

        //Метод для оновлення вмісту текстового блоку, який відображає історію конвертації.
        private void UpdateHistoryTextBlock()
        {
            // Створення рядка з історією
            StringBuilder historyText = new StringBuilder();
            foreach (var item in conversionHistory)
            {
                historyText.AppendLine($"Дата запису: {item.Date}, Дата курсу: {item.RateDate}, Обнін {item.QuantityBaseCurrency}: {item.BaseCurrency}, на: {item.QuantityTargetCurrency} {item.TargetCurrency}, курс обміну: {item.ExchangeRate}");
            }

            // Оновлення вмісту TextBlock
            historyTextBlock.Text = historyText.ToString();
        }

        // Визначаємо екземпляр класу HttpClient, який використовується для виконання HTTP-запитів у програмі
        private readonly HttpClient _client;

        public MainWindow()
        {
            InitializeComponent();
            _client = new HttpClient();
        }

        // Метод викликається при завантаженні вікна MainWindow. Він містить асинхронний код, який виконується під час завантаження вікна
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Отримати список валют з JSON
                string currenciesUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies.json";
                string currenciesJson = await _client.GetStringAsync(currenciesUrl);
                JObject currenciesObject = JObject.Parse(currenciesJson);

                // Додати кожну валюту до першого ComboBox
                foreach (var currency in currenciesObject)
                {
                    string currencyCode = currency.Key;
                    string currencyName = (string)currency.Value;
                    ComboBoxItem item = new ComboBoxItem();
                    item.Content = $"{currencyName} ({currencyCode})";
                    baseCurrencyComboBox.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка", $"Помилка: {ex.Message}", MessageBoxButton.OKCancel);
            }
            LoadLastConversion();
            LoadHistoryFromFile("HistoryExchange.txt");
        }

        //Метод для виклику, коли вибирається нова базова валюта в першому ComboBox (baseCurrencyComboBox). Основна функція цього методу - оновлення другого ComboBox (targetCurrencyComboBox) зі списком цільових валют.
        private async void BaseCurrencyComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            ClearButton();
            if (!isComboBoxEventDisabled)
            {
                try
                {
                    // Отримати список валют з JSON
                    string currenciesUrl = "https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@latest/v1/currencies.json";
                    string currenciesJson = await _client.GetStringAsync(currenciesUrl);
                    JObject currenciesObject = JObject.Parse(currenciesJson);

                    // Очистити список валют у ComboBox перед додаванням нових елементів
                    targetCurrencyComboBox.Items.Clear();

                    // Отримати обрану базову валюту
                    string selectedBaseCurrency = ((ComboBoxItem)baseCurrencyComboBox.SelectedItem)?.Content.ToString();
                    string baseCurrencyCode = selectedBaseCurrency?.Split('(')[1].TrimEnd(')').Trim();
                    BaseCurrencyCode.Content = baseCurrencyCode;

                    if (baseCurrencyCode == null)
                    {
                        MessageBox.Show("Помилка", "Будь ласка, виберіть валюту.", MessageBoxButton.OK);
                        return;
                    }

                    // Додати кожну валюту до другого ComboBox, виключаючи обрану базову валюту
                    foreach (var currency in currenciesObject)
                    {
                        string currencyCode = currency.Key;
                        string currencyName = (string)currency.Value;

                        // Пропустити базову валюту
                        if (currencyCode == baseCurrencyCode)
                            continue;

                        ComboBoxItem item = new ComboBoxItem();
                        item.Content = $"{currencyName} ({currencyCode})";
                        targetCurrencyComboBox.Items.Add(item);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка", $"Помилка: {ex.Message}", MessageBoxButton.OK);
                }
            }
        }

        //Метод для виклику при зміні цільової валюти в TargetCurrencyComboBox
        private void TargetCurrencyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TargetCurrencyComboBox();
        }

        //Метод для отримання курсу обміну між обраною базовою та цільовою валютами і відображення його у текстовому полі (rateTextBox).
        private async void TargetCurrencyComboBox()
        {
            if (!isComboBoxEventDisabled)
            {
                try
                {
                    // Отримати вибрані валюти
                    string selectedBaseCurrency = ((ComboBoxItem)baseCurrencyComboBox.SelectedItem)?.Content?.ToString();
                    string baseCurrencyCode = selectedBaseCurrency?.Split('(')[1]?.TrimEnd(')')?.Trim();
                    string selectedTargetCurrency = ((ComboBoxItem)targetCurrencyComboBox.SelectedItem)?.Content?.ToString();
                    string targetCurrencyCode = selectedTargetCurrency?.Split('(')[1]?.TrimEnd(')')?.Trim();

                    if (string.IsNullOrEmpty(baseCurrencyCode) || string.IsNullOrEmpty(targetCurrencyCode))
                    {
                        MessageBox.Show("Помилка", "Будь ласка, виберыть базову і цільову валюти.", MessageBoxButton.OK);
                        return;
                    }

                    // Отримати обрану дату
                    string selectedDate = datePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? DateTime.Today.ToString("yyyy-MM-dd");
                    string selectedRateDate = datePicker.SelectedDate?.ToString("dd.MM.yyyy") ?? DateTime.Today.ToString("dd.MM.yyyy");

                    // Отримати курс валют
                    string ratesUrl = $"https://cdn.jsdelivr.net/npm/@fawazahmed0/currency-api@{selectedDate}/v1/currencies/{baseCurrencyCode}.json";

                    // Отримати JSON з курсами валют
                    HttpClient client = new HttpClient();
                    string rateJson = await client.GetStringAsync(ratesUrl);

                    // Парсинг JSON
                    JObject rateObject = JObject.Parse(rateJson);
                    JObject secondLevelObject = (JObject)rateObject[baseCurrencyCode];
                    double exchangeRate = (double)secondLevelObject[targetCurrencyCode];
                    rateTextBox.Text = $"1 {baseCurrencyCode} = {exchangeRate} {targetCurrencyCode}";
                    BaseCurrencyCode.Content = baseCurrencyCode;
                    TargetCurrencyCode.Content = targetCurrencyCode;
                    if (QuantityBaseCurrencyTextBlock.Text != "0,00000000" || string.IsNullOrEmpty(QuantityBaseCurrencyTextBlock.Text))
                    {
                        CalculateButton();
                        AddConversionToHistory(baseCurrencyCode, targetCurrencyCode, exchangeRate, selectedRateDate);
                        SaveHistoryToFile("HistoryExchange.txt");
                        SaveLastConversion();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Помилка", $"Помилка: {ex.Message}", MessageBoxButton.OK);
                }
            }
        }

        //Метод для збереження історії конвертації у файл
        public void SaveHistoryToFile(string filePath)
        {
           // Відкриття файлу для запису
           using (StreamWriter writer = new StreamWriter(filePath))
           {
               foreach (var item in conversionHistory)
               {
                      // Записування кожного елементу історії у файл
                      writer.WriteLine($"{item.Date};{item.RateDate};{item.QuantityBaseCurrency};{item.BaseCurrency};{item.QuantityTargetCurrency};{item.TargetCurrency};{item.ExchangeRate}");
               }
           }
        }

        //Метод для завантаження даних історії конвертації з файлу
        public void LoadHistoryFromFile(string filePath)
        {
            // Очистка поточної історії перед завантаженням нової
            historyTextBlock.Clear();

            // Перевірка наявності файлу
            if (File.Exists(filePath))
            {
                // Відкриття файлу для читання
                using (StreamReader reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        // Розбиття рядка на частини
                        string[] parts = line.Split(';');
                        // Створення об'єкта ConversionHistoryItem з прочитаних даних та додавання його до історії
                        ConversionHistoryItem historyItem = new ConversionHistoryItem
                        {
                            Date = DateTime.Parse(parts[0]),
                            RateDate = parts[1],
                            QuantityBaseCurrency = double.Parse(parts[2]),
                            BaseCurrency = parts[3],
                            QuantityTargetCurrency = double.Parse(parts[4]),
                            TargetCurrency = parts[5],
                            ExchangeRate = double.Parse(parts[6])
                        };
                        conversionHistory.Add(historyItem);
                    }
                }
                UpdateHistoryTextBlock();
            }
        }

        //Метод для очистки історії конвертації. Викликається при натисканні кнопки очистки історії конвертації.
        public void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Повідомлення", "Ви впевнені, що хочете історію конвертації", MessageBoxButton.OKCancel);

            // Перевіряється, чи користувач натиснув кнопку "Добре" (OK), та виконується відповідна дія
            if (result == MessageBoxResult.OK)
            {
                // Очищення списку історії
                conversionHistory.Clear();

                // Очищення тексту в TextBlock
                historyTextBlock.Text = string.Empty;

                SaveHistoryToFile("HistoryExchange.txt");
            }
        }

        //Метод для обробки події натискання іконки виходу з програми
        private void exit_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Повідомлення", "Ви впевнені, що хочете вийти з програми", MessageBoxButton.OKCancel);
            // Перевіряється, чи користувач натиснув кнопку "Добре" (OK), та виконується відповідна дія
            if (result == MessageBoxResult.OK)
            {
                ((App)Application.Current).MainWindow.Close();
            }
        }

        //Метод для зберігання у файл даних останньої конвертації
        private void SaveLastConversion()
        {
            try
            {
                string filePath = "last_conversion.txt";

                // Отримання даних для збереження
                string baseCurrency = baseCurrencyComboBox.SelectedItem.ToString();
                string targetCurrency = targetCurrencyComboBox.SelectedItem.ToString();
                string date = datePicker.SelectedDate.HasValue ? datePicker.SelectedDate.Value.ToString("yyyy-MM-dd") : DateTime.Today.ToString("yyyy-MM-dd");
                // Створення рядка для збереження
                string line = $"{date};{baseCurrency};{targetCurrency};{rateTextBox.Text};{QuantityBaseCurrencyTextBlock.Text};{QuantityTargetCurrencyTextBlock.Text}";

                // Збереження даних у файл
                File.WriteAllText(filePath, line);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка", $"Помилка при збереженні останньої конвертації: {ex.Message}", MessageBoxButton.OK);
            }
        }

        //Метод для завантаження даних останньої конвертації
        private void LoadLastConversion()
        {
            try
            {
                // Встановити прапорець на блокування обробників подій
                isComboBoxEventDisabled = true;

                string filePath = "last_conversion.txt";
                if (File.Exists(filePath))
                {
                    // Зчитати рядок з файлу
                    string line = File.ReadAllText(filePath);

                    // Розбити рядок на частини
                    string[] parts = line.Split(';');

                    // Перевірити, чи кількість частин правильна
                    if (parts.Length == 6)
                    {
                        // Встановити значення вибраної дати
                        datePicker.SelectedDate = DateTime.Parse(parts[0]);

                        // Встановити значення вибраної базової валюти
                        foreach (ComboBoxItem item in baseCurrencyComboBox.Items)
                        {
                            if (item.ToString().Contains(parts[1]))
                            {
                                baseCurrencyComboBox.SelectedItem = item;
                                break;
                            }
                        }

                        // Скопіювати елементи з baseCurrencyComboBox до targetCurrencyComboBox
                        targetCurrencyComboBox.Items.Clear();
                        foreach (ComboBoxItem item in baseCurrencyComboBox.Items)
                        {
                            targetCurrencyComboBox.Items.Add(new ComboBoxItem { Content = item.Content });
                        }

                        // Видалити базову валюту зі списку цільових валют
                        foreach (ComboBoxItem item in targetCurrencyComboBox.Items)
                        {
                            if (item.ToString().Contains(parts[1]))
                            {
                                targetCurrencyComboBox.Items.Remove(item);
                                break;
                            }
                        }

                        // Встановити індекс вибраної цільової валюти
                        for (int i = 0; i < targetCurrencyComboBox.Items.Count; i++)
                        {
                            ComboBoxItem item = (ComboBoxItem)targetCurrencyComboBox.Items[i];
                            if (item.ToString().Contains(parts[2]))
                            {
                                targetCurrencyComboBox.SelectedIndex = i;
                                break;
                            }
                        }
                        rateTextBox.Text = parts[3];
                        string[] part = parts[3].Split(' ');
                        BaseCurrencyCode.Content = part[1].Trim();
                        TargetCurrencyCode.Content = part[4].Trim();
                        QuantityBaseCurrencyTextBlock.TextChanged -= QuantityBaseCurrencyTextBlock_TextChanged;
                        QuantityTargetCurrencyTextBlock.TextChanged -= QuantityTargetCurrencyTextBlock_TextChanged;
                        QuantityBaseCurrencyTextBlock.Text = parts[4].Trim();
                        QuantityTargetCurrencyTextBlock.Text = parts[5].Trim();
                        QuantityBaseCurrencyTextBlock.TextChanged += QuantityBaseCurrencyTextBlock_TextChanged;
                        QuantityTargetCurrencyTextBlock.TextChanged += QuantityTargetCurrencyTextBlock_TextChanged;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка", $"Помилка при завантаженні останньої конвертації: {ex.Message}", MessageBoxButton.OK);
            }
            finally
            {
                // Скинути прапорець блокування обробників подій
                isComboBoxEventDisabled = false;
            }
        }

        //Зміна типу курсора при наведенні його на відповідний об'єкт
        private void Image_MouseEnter(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Hand; // Зміна курсора на руку (подібно кнопці)
        }

        //Зміна типу курсора при виходу курсора за межі відповідного об'єкта
        private void Image_MouseLeave(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Arrow; // Повернення стандартного курсора
        }

        //Метод для переміщення вікна при одночасному натисканні і утримуванні лівої кнопки миші та зміні положенні курсора
        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        //Метод для звертання вікна при клікі на відповідному зображенні 
        private void minimize_MouseLeftButtonDown (object sender, MouseButtonEventArgs e)
        {
            // Зміна стану вікна на мінімізацію
            WindowState = WindowState.Minimized;
        }

       //Метод для отримання курсу валют із відповідного текстового поля
        private double GetExchangeRate(string rateString)
        {
            // Розділити рядок по рівню
            string[] parts = rateString.Split(' ');

            // Перевірити, чи є дві частини
            if (parts.Length == 5)
            {
                // Отримати другу частину, яка містить курс
                string ratePart = parts[3].Trim();
                // Спробувати перетворити курс у числовий формат
                if (double.TryParse(ratePart, out double exchangeRate))
                {
                    return exchangeRate;
                }
            }
            // Якщо не вдається знайти курс або перетворити його, повернути нуль або виконати інші дії за необхідності
            return 1;
        }

        //Метод для виклику ClearButton() при натисканні кнопки для очищення полів де відображаються кількості валют
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            ClearButton();
        }

        //Метод для очищення полів де відображаються кількості валют
        private void ClearButton()
        {
            QuantityBaseCurrencyTextBlock.TextChanged -= QuantityBaseCurrencyTextBlock_TextChanged;
            QuantityTargetCurrencyTextBlock.TextChanged -= QuantityTargetCurrencyTextBlock_TextChanged;
            QuantityBaseCurrencyTextBlock.Text="0,00000000";
            QuantityTargetCurrencyTextBlock.Text = "0,00000000";
            QuantityBaseCurrencyTextBlock.TextChanged += QuantityBaseCurrencyTextBlock_TextChanged;
            QuantityTargetCurrencyTextBlock.TextChanged += QuantityTargetCurrencyTextBlock_TextChanged;
        }

        //Метод для виклику TargetCurrencyComboBox() при натисканні копки розрахування даних конвертації
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            TargetCurrencyComboBox();
        }

        //Метод для розрахунку даних конвертації та заповнення відповідних полів
        private void CalculateButton()
        {
            // Вимкнути обробники подій
            QuantityBaseCurrencyTextBlock.TextChanged -= QuantityBaseCurrencyTextBlock_TextChanged;
            QuantityTargetCurrencyTextBlock.TextChanged -= QuantityTargetCurrencyTextBlock_TextChanged;
            try
            {
                // Отримання курсу конвертації між базовою та цільовою валютами
                double exchangeRate = GetExchangeRate(rateTextBox.Text);

                if (!string.IsNullOrEmpty(QuantityBaseCurrencyTextBlock.Text)) 
                {
                    // Отримати текст з TextBlock
                    string text = QuantityBaseCurrencyTextBlock.Text;

                    // Спробувати конвертувати текст в double
                    if (double.TryParse(text, out double quantityBaseCurrency))
                    {
                        // Тепер змінна quantityBaseCurrency містить значення введеного тексту у форматі double
                        double targetCurrencyCount = quantityBaseCurrency * exchangeRate;
                        // Розрахунок кількості вибраних валют у цільовій валюті
                        QuantityTargetCurrencyTextBlock.Text = targetCurrencyCount.ToString("F8");
                    }
                    else
                    {
                        // Якщо не вдається сконвертувати текст у double
                        MessageBox.Show("Помилка", "Переконайтеся, що ви правильно ввели число", MessageBoxButton.OK);
                    }

                }

                if (!string.IsNullOrEmpty(QuantityTargetCurrencyTextBlock.Text))
                {
                    // Отримати текст з TextBlock
                    string text = QuantityTargetCurrencyTextBlock.Text;

                    // Спробувати конвертувати текст в double
                    if (double.TryParse(text, out double quantityTargetCurrency))
                    {
                        // Тепер змінна quantityBaseCurrency містить значення введеного тексту у форматі double
                        double targetCurrencyCount = quantityTargetCurrency / exchangeRate;
                        // Розрахунок кількості вибраних валют у цільовій валюті
                        QuantityBaseCurrencyTextBlock.Text = targetCurrencyCount.ToString("F8");
                    }
                    else
                    {
                        // Якщо не вдається сконвертувати текст у double
                        MessageBox.Show("Помилка", "Переконайтеся, що Ви правильно ввели число!", MessageBoxButton.OK);
                    }

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Помилка", "Переконайтеся, що Ви правильно ввели число!", MessageBoxButton.OK);
            }
            // Увімкнути обробники подій
            QuantityBaseCurrencyTextBlock.TextChanged += QuantityBaseCurrencyTextBlock_TextChanged;
            QuantityTargetCurrencyTextBlock.TextChanged += QuantityTargetCurrencyTextBlock_TextChanged;
        }

        //Метод для очистки поля цільової валюти при внесенні змін в поле базової валюти
        private void QuantityBaseCurrencyTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Очистити поле QuantityTargetCurrencyTextBlock
            QuantityTargetCurrencyTextBlock.Text = "";
        }

        //Метод для очистки поля базової валюти при внесенні змін в поле цільової валюти
        private void QuantityTargetCurrencyTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Очистити поле QuantityBaseCurrencyTextBlock
            QuantityBaseCurrencyTextBlock.Text = "";
        }

    }
}

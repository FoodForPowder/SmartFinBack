using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SmartFin.Interfaces;
using SmartFin.Entities;
using SmartFin.Services;
using SmartFin.DTOs.Transaction;
using SmartFin.DTOs.Category;
using Smartfin.Extensions;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using SmartFin.DTOs.Expense;

namespace SmartFin.Parsers
{
    public class YandexBankStatementParser : IBankStatementParser
    {
        private readonly TransactionService _transactionService;
        private readonly CategoryService _categoryService;
        private readonly int _userId;

        // Для хранения категорий
        private Dictionary<string, int?> _categoryCache = new Dictionary<string, int?>();

        public YandexBankStatementParser(TransactionService transactionService, CategoryService categoryService, int userId)
        {
            _transactionService = transactionService;
            _categoryService = categoryService;
            _userId = userId;
        }

        public async Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream)
        {
            var importedTransactions = new List<TransactionDto>();
            await InitializeCategories();

            try
            {
                // Извлечение всего текста из PDF
                string fullText = ExtractTextFromPdf(fileStream);

                // Разбиваем текст на строки
                string[] lines = fullText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Находим строки с транзакциями
                // Для отладки сохраним первые 10 строк
                StringBuilder debugText = new StringBuilder();
                for (int i = 0; i < Math.Min(10, lines.Length); i++)
                {
                    debugText.AppendLine($"Строка {i}: {lines[i]}");
                }
                Console.WriteLine($"Первые строки выписки: {debugText}");

                // Ищем строки, содержащие описание операции, дату и сумму
                for (int i = 0; i < lines.Length; i++)
                {
                    // Пропускаем заголовки таблицы
                    if (lines[i].Contains("Описание операции") && lines[i].Contains("Дата и время"))
                        continue;

                    if (lines[i].Contains("Страница") || lines[i].Contains("Продолжение"))
                        continue;

                    if (lines[i].Contains("Исходящий остаток") || lines[i].Contains("Входящий остаток"))
                        continue;

                    if (lines[i].Contains("Всего расходных") || lines[i].Contains("Всего приходных"))
                        continue;

                    // Проверяем, начинается ли строка с ключевых слов, характерных для транзакций
                    if ((lines[i].StartsWith("Оплата") ||
                         lines[i].StartsWith("Входящий перевод") ||
                         lines[i].StartsWith("Исходящий перевод")) &&
                        i + 1 < lines.Length)
                    {
                        // Ищем строку с датой и суммой
                        string dateAndAmountLine = "";

                        // Проверяем следующую строку на наличие даты
                        if (Regex.IsMatch(lines[i + 1], @"\d{2}\.\d{2}\.\d{4}\s+в\s+\d{2}:\d{2}"))
                        {
                            dateAndAmountLine = lines[i + 1];

                            // Ищем строку с суммой - может быть дальше
                            string amountLine = "";
                            for (int j = i + 2; j < Math.Min(i + 5, lines.Length); j++)
                            {
                                if (lines[j].Contains("₽") &&
                                   (lines[j].Contains("+") || lines[j].Contains("–") || lines[j].Contains("-")))
                                {
                                    amountLine = lines[j];
                                    break;
                                }
                            }

                            // Если нашли описание, дату и сумму - парсим транзакцию
                            if (!string.IsNullOrEmpty(dateAndAmountLine) && !string.IsNullOrEmpty(amountLine))
                            {
                                var transaction = ParseTransactionFromLines(
                                    lines[i],
                                    dateAndAmountLine,
                                    amountLine);

                                if (transaction != null)
                                {
                                    try
                                    {
                                        var createdId = await _transactionService.CreateUserTransaction(transaction);
                                        if (createdId > 0)
                                        {
                                            var createdTransaction = await _transactionService.GetTransactionById(createdId);
                                            if (createdTransaction != null)
                                            {
                                                importedTransactions.Add(createdTransaction.asDto());
                                            }
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"Ошибка при создании транзакции: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }
                }

                // Если не нашли транзакции, попробуем альтернативный метод парсинга для табличного формата
                if (importedTransactions.Count == 0)
                {
                    Console.WriteLine("Применяем табличный метод парсинга");
                    importedTransactions = await ParseTableFormat(lines);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге выписки: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            return importedTransactions;
        }

        private async Task<List<TransactionDto>> ParseTableFormat(string[] lines)
        {
            var result = new List<TransactionDto>();

            // Ищем паттерн: Описание, Дата, Сумма в трех разных колонках в табличном формате
            for (int i = 0; i < lines.Length - 2; i++)
            {
                try
                {
                    // Проверяем форматы строк
                    string descriptionLine = lines[i].Trim();

                    // Ищем строки с описаниями транзакций
                    if (!descriptionLine.StartsWith("Оплата") &&
                        !descriptionLine.StartsWith("Входящий") &&
                        !descriptionLine.StartsWith("Исходящий"))
                        continue;

                    // Смотрим на следующую строку - она может содержать дату операции
                    string dateLine = lines[i + 1].Trim();
                    if (!Regex.IsMatch(dateLine, @"\d{2}\.\d{2}\.\d{4}\s+в\s+\d{2}:\d{2}"))
                        continue;

                    // Ищем строку с суммой в следующих нескольких строках
                    string amountLine = null;
                    for (int j = i + 2; j < Math.Min(lines.Length, i + 5); j++)
                    {
                        if ((lines[j].Contains("+") || lines[j].Contains("–") || lines[j].Contains("-")) &&
                            lines[j].Contains("₽"))
                        {
                            amountLine = lines[j].Trim();
                            break;
                        }
                    }

                    if (amountLine == null)
                        continue;

                    // Извлекаем данные для транзакции
                    var dateMatch = Regex.Match(dateLine, @"(\d{2})\.(\d{2})\.(\d{4})\s+в\s+(\d{2}):(\d{2})");
                    if (!dateMatch.Success)
                        continue;

                    var dateTime = new DateTime(
                        int.Parse(dateMatch.Groups[3].Value),
                        int.Parse(dateMatch.Groups[2].Value),
                        int.Parse(dateMatch.Groups[1].Value),
                        int.Parse(dateMatch.Groups[4].Value),
                        int.Parse(dateMatch.Groups[5].Value),
                        0
                    );

                    // Извлекаем сумму
                    var amountMatch = Regex.Match(amountLine, @"([\+\-–]\s*[\d\s]+,\d{2})\s*₽");
                    if (!amountMatch.Success)
                        continue;

                    var amountStr = amountMatch.Groups[1].Value
                        .Replace(" ", "")
                        .Replace("–", "-"); // заменяем тире на минус

                    if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        continue;

                    // Создаем транзакцию
                    var transaction = new CreateTransactionDto
                    {
                        Name = descriptionLine,
                        Date = dateTime,
                        sum = amount,
                        UserId = _userId,
                        CategoryId = DetermineCategory(descriptionLine)
                    };

                    // Создаем и сохраняем транзакцию
                    var createdId = await _transactionService.CreateUserTransaction(transaction);
                    if (createdId > 0)
                    {
                        var createdTransaction = await _transactionService.GetTransactionById(createdId);
                        if (createdTransaction != null)
                        {
                            result.Add(createdTransaction.asDto());
                            Console.WriteLine($"Создана транзакция: {descriptionLine}, {dateTime}, {amount}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при парсинге строки {i}: {ex.Message}");
                }
            }

            return result;
        }

        private string ExtractTextFromPdf(Stream pdfStream)
        {
            StringBuilder text = new StringBuilder();

            using (var reader = new PdfReader(pdfStream))
            using (var document = new PdfDocument(reader))
            {
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    ITextExtractionStrategy strategy = new SimpleTextExtractionStrategy();
                    string pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(i), strategy);
                    text.AppendLine(pageText);
                }
            }

            return text.ToString();
        }

        private CreateTransactionDto ParseTransactionFromLines(string descriptionLine, string dateLine, string amountLine)
        {
            try
            {
                // Извлекаем дату и время
                var dateMatch = Regex.Match(dateLine, @"(\d{2})\.(\d{2})\.(\d{4})\s+в\s+(\d{2}):(\d{2})");
                if (!dateMatch.Success)
                    return null;

                var dateTime = new DateTime(
                    int.Parse(dateMatch.Groups[3].Value),
                    int.Parse(dateMatch.Groups[2].Value),
                    int.Parse(dateMatch.Groups[1].Value),
                    int.Parse(dateMatch.Groups[4].Value),
                    int.Parse(dateMatch.Groups[5].Value),
                    0
                );

                // Извлекаем сумму
                var amountMatch = Regex.Match(amountLine, @"([\+\-–]\s*[\d\s]+,\d{2})\s*₽");
                if (!amountMatch.Success)
                    return null;

                var amountStr = amountMatch.Groups[1].Value
                    .Replace(" ", "")
                    .Replace("–", "-"); // заменяем тире на минус

                if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    return null;

                // Определяем категорию на основе описания
                int? categoryId = DetermineCategory(descriptionLine);

                // Создаем транзакцию
                return new CreateTransactionDto
                {
                    Name = descriptionLine,
                    Date = dateTime,
                    sum = amount,
                    UserId = _userId,
                    CategoryId = categoryId
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task InitializeCategories()
        {
            // Получаем существующие категории
            var categories = await _categoryService.GetUserCategoriesAsync(_userId);

            // Заполняем кэш категорий
            foreach (var category in categories)
            {
                _categoryCache[category.name.ToLower()] = category.id;
            }

            // Создаем базовые категории, если они отсутствуют
            var defaultCategories = new Dictionary<string, string>
            {
                { "продукты", "Продукты" },
                { "аптека", "Аптеки" },
                { "переводы", "Переводы" },
                { "такси", "Транспорт" },
                { "рестораны", "Кафе и рестораны" },
                { "магазины", "Покупки" }
            };

            foreach (var pair in defaultCategories)
            {
                if (!_categoryCache.ContainsKey(pair.Key))
                {
                    try
                    {
                        var categoryDto = new CategoryDTO { name = pair.Value };
                        var newCategory = await _categoryService.CreateCategoryAsync(categoryDto, _userId);
                        _categoryCache[pair.Key] = newCategory.id;
                        _categoryCache[pair.Value.ToLower()] = newCategory.id;
                    }
                    catch (Exception)
                    {
                        // Игнорируем ошибки создания категорий
                    }
                }
            }
        }

        private int? DetermineCategory(string description)
        {
            description = description.ToLower();

            // Маппинг ключевых слов на категории
            var mappings = new Dictionary<string, string>
            {
                { "pyaterochka", "продукты" },
                { "magnit", "продукты" },
                { "krasnoe&beloe", "продукты" },
                { "lenta", "продукты" },
                { "apteka", "аптека" },
                { "yandex.market", "магазины" },
                { "перевод сбп", "переводы" }
            };

            foreach (var mapping in mappings)
            {
                if (description.Contains(mapping.Key.ToLower()))
                {
                    if (_categoryCache.TryGetValue(mapping.Value, out int? categoryId))
                    {
                        return categoryId;
                    }
                }
            }

            // Категория по умолчанию
            return _categoryCache.TryGetValue("магазины", out int? defaultCategory) ? defaultCategory : null;
        }
    }
}
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
    public class VTBStatementParser : BankStatementParser
    {

        // Кэш для хранения категорий
        private Dictionary<string, int?> _categoryCache = new Dictionary<string, int?>();

        public VTBStatementParser(
        TransactionService transactionService, 
        CategoryService categoryService, 
        int userId) 
        : base(transactionService, categoryService, userId)
    {
    }


        public override async Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream)
        {
            var importedTransactions = new List<TransactionDto>();

            try
            {
                // Инициализация кэша категорий
                await InitializeCategories();

                // Извлечение текста из PDF
                string pdfText = ExtractTextFromPdf(fileStream);

                // Логирование для отладки
                Console.WriteLine($"Извлеченный текст из PDF (первые 500 символов): {pdfText.Substring(0, Math.Min(500, pdfText.Length))}");

                // Парсинг транзакций
                var transactions = ParseVTBFormat(pdfText);
                Console.WriteLine($"Найдено {transactions.Count} транзакций");

                // Импорт транзакций
                foreach (var transaction in transactions)
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
                                Console.WriteLine($"Успешно импортирована транзакция: {transaction.Name}, {transaction.sum}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при импорте транзакции: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Общая ошибка при парсинге: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
            }

            return importedTransactions;
        }
        private List<CreateTransactionDto> ParseVTBFormat(string pdfText)
        {
            var transactions = new List<CreateTransactionDto>();
            var lines = pdfText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length - 1; i++)
            {
                var currentLine = lines[i].Trim();
                var nextLine = lines[i + 1].Trim();

                // Пропускаем строки, не начинающиеся с даты
                if (!Regex.IsMatch(currentLine, @"^\d{2}\.\d{2}\.\d{4}"))
                    continue;

                try
                {
                    // Извлекаем дату
                    var dateMatch = Regex.Match(currentLine, @"^(\d{2}\.\d{2}\.\d{4})");
                    if (!dateMatch.Success) continue;

                    var dateStr = dateMatch.Groups[1].Value;
                    if (!DateTime.TryParseExact(dateStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                        continue;

                    // Извлекаем сумму
                    var amountMatch = Regex.Match(currentLine, @"(-?[\d.,]+)\s*RUB");
                    if (!amountMatch.Success) continue;

                    var amountStr = amountMatch.Groups[1].Value.Replace(",", ".");
                    if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        continue;

                    // Предполагаем, что это расход (если отрицательная сумма)
                    if (amount > 0)
                        amount = -amount;

                    // Пытаемся извлечь описание
                    string description = "";

                    // Убедимся, что следующая строка — это действительно описание
                    if (!Regex.IsMatch(nextLine, @"^\d{2}\.\d{2}\.\d{4}") &&
                        !nextLine.StartsWith("Спасибо") &&
                        !nextLine.StartsWith("Всегда ваш", StringComparison.OrdinalIgnoreCase) &&
                        !nextLine.StartsWith("Страница", StringComparison.OrdinalIgnoreCase))
                    {
                        description = nextLine;
                        i++; // Пропускаем следующую строку, т.к. она использована
                    }
                    else
                    {
                        // Альтернативно берём описание из текущей строки после "RUB"
                        int rubIndex = currentLine.LastIndexOf("RUB");
                        if (rubIndex > 0 && rubIndex + 3 < currentLine.Length)
                        {
                            description = currentLine.Substring(rubIndex + 3).Trim();
                        }
                    }

                    // Определяем категорию
                    int? categoryId = DetermineCategory(description);

                    // Создаём транзакцию
                    transactions.Add(new CreateTransactionDto
                    {
                        Date = date,
                        sum = amount,
                        Name = description,
                        UserId = _userId,
                        CategoryId = categoryId
                    });

                    Console.WriteLine($"Добавлена транзакция: {date}, {amount}, {description}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при парсинге строки {i}: {ex.Message}");
                }
            }

            return transactions;
        }

        private string ExtractTextFromPdf(Stream pdfStream)
        {
            var text = new StringBuilder();

            using (var reader = new PdfReader(pdfStream))
            using (var document = new PdfDocument(reader))
            {
                for (int i = 1; i <= document.GetNumberOfPages(); i++)
                {
                    var page = document.GetPage(i);
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(page, strategy);
                    text.AppendLine(pageText);
                }
            }

            return text.ToString();
        }

        private List<CreateTransactionDto> ParseTableData(string pdfText)
        {
            var transactions = new List<CreateTransactionDto>();

            try
            {
                // Находим строки с транзакциями по шаблону
                var transactionPattern = @"(\d{2}\.\d{2}\.\d{4})\s+(\d{2}:\d{2}:\d{2})\s+(\d{2}\.\d{2}\.\d{4})\s+([-\d,.]+\s*RUB)\s+(\d+\.\d{2})\s+([\d,.]+)\s+(\d+\.\d{2})\s+RUB\s+(.+)";

                // Альтернативный шаблон, если первый не сработает
                var altPattern = @"(\d{2}\.\d{2}\.\d{4})\s+(\d{2}:\d{2}:\d{2})\s+(\d{2}\.\d{2}\.\d{4})\s+([-\d,.]+\s*RUB)\s+(.+)";

                // Еще один альтернативный шаблон для данного конкретного формата
                var simplePattern = @"(\d{2}\.\d{2}\.\d{4})\s+\d{2}:\d{2}:\d{2}\s+\d{2}\.\d{2}\.\d{4}\s+(-?[\d,\.]+)\s+RUB\s+([\d\.]+)\s+([\d\.]+)\s+[\d\.]+\s+RUB\s+(.+)";

                // Использование прямого поиска строк с описанием транзакций
                string[] lines = pdfText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Ищем начало таблицы операций
                int tableStartIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Contains("Операции по счёту") || lines[i].Contains("Операции по счету"))
                    {
                        tableStartIndex = i;
                        break;
                    }
                }

                if (tableStartIndex == -1)
                {
                    Console.WriteLine("Не найдена таблица операций");
                    return transactions;
                }

                // Ищем заголовки колонок
                int headerIndex = -1;
                for (int i = tableStartIndex; i < Math.Min(tableStartIndex + 10, lines.Length); i++)
                {
                    if (lines[i].Contains("Дата и время") && lines[i].Contains("Описание операции"))
                    {
                        headerIndex = i;
                        break;
                    }
                }

                if (headerIndex == -1)
                {
                    Console.WriteLine("Не найдены заголовки колонок");
                    return transactions;
                }

                // Строка с заголовками определяет индексы колонок
                int dataStartIndex = headerIndex + 1;

                // Парсим строки
                for (int i = dataStartIndex; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();

                    // Пропускаем пустые строки и строки с итогами
                    if (string.IsNullOrWhiteSpace(line) ||
                        line.Contains("Спасибо") ||
                        line.Contains("Всегда Ваш") ||
                        line.Contains("Страница"))
                        continue;

                    // Проверяем, начинается ли строка с даты
                    if (Regex.IsMatch(line, @"^\d{2}\.\d{2}\.\d{4}"))
                    {
                        try
                        {
                            // Находим индексы важных частей строки
                            var dateMatch = Regex.Match(line, @"^(\d{2}\.\d{2}\.\d{4})");
                            if (!dateMatch.Success)
                                continue;

                            DateTime transactionDate;

                            // Парсим дату
                            if (!DateTime.TryParseExact(
                                dateMatch.Groups[1].Value,
                                "dd.MM.yyyy",
                                CultureInfo.InvariantCulture,
                                DateTimeStyles.None,
                                out transactionDate))
                                continue;

                            // Ищем сумму операции
                            decimal amount = 0;
                            bool isExpense = false;

                            // Сначала ищем формат с минусом
                            var minusMatch = Regex.Match(line, @"-[\d,.]+\s*RUB");
                            if (minusMatch.Success)
                            {
                                var amountStr = minusMatch.Value.Replace("RUB", "").Trim().Replace(",", ".");
                                if (decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out amount))
                                {
                                    isExpense = true;
                                }
                            }

                            // Если не нашли отрицательную сумму, ищем положительную
                            if (amount == 0)
                            {
                                var positiveMatch = Regex.Match(line, @"[\d,.]+\s*RUB");
                                if (positiveMatch.Success)
                                {
                                    var amountStr = positiveMatch.Value.Replace("RUB", "").Trim().Replace(",", ".");
                                    decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out amount);
                                }
                            }

                            // Определяем тип операции по словам "Приход" и "Расход"
                            if (amount > 0 && !isExpense)
                            {
                                // Проверяем, является ли это расходом
                                // Ищем значение в колонке "Расход"
                                if (line.Contains("0.00") && Regex.IsMatch(line, @"\d+\.\d{2}\s+0\.00"))
                                {
                                    // Это расход, сумма находится в другой колонке
                                    isExpense = true;

                                    // Находим сумму расхода
                                    var expenseMatch = Regex.Match(line, @"0\.00\s+([\d,.]+)");
                                    if (expenseMatch.Success && expenseMatch.Groups.Count > 1)
                                    {
                                        var expenseStr = expenseMatch.Groups[1].Value.Replace(",", ".");
                                        if (decimal.TryParse(expenseStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal expenseAmount))
                                        {
                                            amount = expenseAmount;
                                        }
                                    }
                                }
                            }

                            // Если определили сумму и тип операции
                            if (amount > 0)
                            {
                                if (isExpense)
                                {
                                    amount = -amount; // Делаем сумму отрицательной для расходов
                                }

                                // Извлекаем описание операции
                                string description = "";
                                int lastRubIndex = line.LastIndexOf("RUB");
                                if (lastRubIndex > 0 && lastRubIndex + 3 < line.Length)
                                {
                                    description = line.Substring(lastRubIndex + 3).Trim();
                                }

                                // Определяем категорию
                                int? categoryId = DetermineCategory(description);

                                // Создаем транзакцию
                                var transaction = new CreateTransactionDto
                                {
                                    Date = transactionDate,
                                    sum = amount,
                                    Name = description,
                                    UserId = _userId,
                                    CategoryId = categoryId
                                };

                                transactions.Add(transaction);
                                Console.WriteLine($"Добавлена транзакция: {transactionDate}, {amount}, {description}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Ошибка при парсинге строки {i}: {ex.Message}");
                        }
                    }
                }

                // Если не нашли транзакции, пробуем альтернативный метод
                if (transactions.Count == 0)
                {
                    transactions = ParseAlternative(pdfText);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при парсинге таблицы: {ex.Message}");
            }

            return transactions;
        }

        private List<CreateTransactionDto> ParseAlternative(string pdfText)
        {
            var transactions = new List<CreateTransactionDto>();
            Console.WriteLine("Применяем альтернативный метод парсинга");

            try
            {
                // Разделяем текст на строки
                string[] lines = pdfText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                // Обрабатываем каждую строку
                for (int i = 0; i < lines.Length; i++)
                {
                    // Ищем строку с датой и суммой
                    var dateMatch = Regex.Match(lines[i], @"(\d{2}\.\d{2}\.\d{4})");
                    if (!dateMatch.Success) continue;

                    DateTime transactionDate;
                    if (!DateTime.TryParseExact(
                        dateMatch.Groups[1].Value,
                        "dd.MM.yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out transactionDate))
                        continue;

                    // Ищем сумму
                    var amountMatch = Regex.Match(lines[i], @"(-?[\d,.]+)\s*RUB");
                    if (!amountMatch.Success) continue;

                    string amountStr = amountMatch.Groups[1].Value.Replace(",", ".");
                    if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                        continue;

                    // Ищем описание операции в этой же строке или следующей
                    string description = "";
                    int rubIndex = lines[i].LastIndexOf("RUB");
                    if (rubIndex > 0 && rubIndex + 3 < lines[i].Length)
                    {
                        description = lines[i].Substring(rubIndex + 3).Trim();
                    }
                    else if (i + 1 < lines.Length)
                    {
                        description = lines[i + 1].Trim();
                    }

                    // Если описание пустое или содержит только цифры/знаки пунктуации, пропускаем
                    if (string.IsNullOrWhiteSpace(description) || Regex.IsMatch(description, @"^[\d\s,.]+$"))
                        continue;

                    // Определяем категорию
                    int? categoryId = DetermineCategory(description);

                    // Создаем транзакцию
                    var transaction = new CreateTransactionDto
                    {
                        Date = transactionDate,
                        sum = amount,
                        Name = description,
                        UserId = _userId,
                        CategoryId = categoryId
                    };

                    transactions.Add(transaction);
                    Console.WriteLine($"Добавлена транзакция (альт.): {transactionDate}, {amount}, {description}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка в альтернативном методе: {ex.Message}");
            }

            return transactions;
        }

        private async Task InitializeCategories()
        {
            // Очистка кэша
            _categoryCache.Clear();

            // Получаем все категории пользователя
            var categories = await _categoryService.GetUserCategoriesAsync(_userId);

            // Заполняем кэш категорий
            foreach (var category in categories)
            {
                _categoryCache[category.name.ToLower()] = category.id;
            }

            // Создаем стандартные категории, если они отсутствуют
            var defaultCategories = new Dictionary<string, string>
            {
                { "оплата", "Покупки" },
                { "товары", "Покупки" },
                { "услуги", "Услуги" },
                { "перевод", "Переводы" },
                { "zapravki", "Транспорт" },
                { "fuel", "Транспорт" },
                { "wildberries", "Покупки" },
                { "yandex", "Транспорт" }
            };

            foreach (var mapping in defaultCategories)
            {
                if (!_categoryCache.ContainsKey(mapping.Value.ToLower()))
                {
                    try
                    {
                        var categoryDto = new CategoryDTO { name = mapping.Value };
                        var newCategory = await _categoryService.CreateCategoryAsync(categoryDto, _userId);
                        _categoryCache[mapping.Value.ToLower()] = newCategory.id;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при создании категории {mapping.Value}: {ex.Message}");
                    }
                }
            }
        }

        private int? DetermineCategory(string description)
        {
            description = description.ToLower();

            if (description.Contains("zapravki") || description.Contains("fuel") || description.Contains("бензин"))
                return GetCategoryId("транспорт");

            if (description.Contains("wildberries") || description.Contains("товар"))
                return GetCategoryId("покупки");

            if (description.Contains("перевод") || description.Contains("перечисление") || description.Contains("счет"))
                return GetCategoryId("переводы");

            if (description.Contains("оплата"))
                return GetCategoryId("покупки");

            // Категория по умолчанию
            return GetCategoryId("покупки");
        }

        private int? GetCategoryId(string categoryName)
        {
            if (_categoryCache.TryGetValue(categoryName.ToLower(), out int? categoryId))
                return categoryId;

            return null;
        }
    }
}
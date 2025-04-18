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
                string fullText = ExtractTextFromPdf(fileStream);
                var transactions = ParseYandexFormat(fullText);

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
                                Console.WriteLine($"✅ Импортирована транзакция: {transaction.Date:d}, {transaction.sum}, {transaction.Name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Ошибка при создании транзакции: {ex.Message}");
                    }
                }

                if (transactions.Count == 0)
                {
                    Console.WriteLine("⚠️ Транзакции не найдены.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка при парсинге выписки: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            return importedTransactions;
        }

        private List<CreateTransactionDto> ParseYandexFormat(string text)
        {
            var transactions = new List<CreateTransactionDto>();
            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            Console.WriteLine("🔎 Отладка: первые строки выписки:");
            for (int i = 0; i < Math.Min(lines.Length, 30); i++)
            {
                Console.WriteLine($"[{i}] {lines[i]}");
            }

            for (int i = 0; i < lines.Length - 3; i++)
            {
                string description = lines[i].Trim();
                string dateLine = lines[i + 1].Trim();
                string timeLine = lines[i + 2].Trim();
                string amountLine = lines[i + 3].Trim();

                Console.WriteLine($"\n🔍 Блок {i}:");
                Console.WriteLine($"Описание: {description}");
                Console.WriteLine($"Дата: {dateLine}");
                Console.WriteLine($"Время: {timeLine}");
                Console.WriteLine($"Сумма: {amountLine}");

                if (!Regex.IsMatch(dateLine, @"^\d{2}\.\d{2}\.\d{4}$") ||
                    !Regex.IsMatch(timeLine, @"^в\s+\d{2}:\d{2}$") ||
                    !Regex.IsMatch(amountLine, @"[+\-–−]?\s*\d[\d\s]*,\d{2}\s*₽"))
                {
                    Console.WriteLine("❌ Блок не соответствует ожидаемому формату");
                    continue;
                }

                try
                {
                    var dateTimeStr = $"{dateLine} {timeLine.Replace("в", "").Trim()}";
                    if (!DateTime.TryParseExact(dateTimeStr, "dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                    {
                        Console.WriteLine("❌ Не удалось распарсить дату и время");
                        continue;
                    }

                    var amountMatch = Regex.Match(amountLine, @"([+\-–−]?\s*\d[\d\s]*,\d{2})");
                    if (!amountMatch.Success)
                    {
                        Console.WriteLine("❌ Не удалось извлечь сумму");
                        continue;
                    }

                    var amountStr = amountMatch.Groups[1].Value
                        .Replace("–", "-")
                        .Replace("−", "-")
                        .Replace(" ", "")
                        .Replace(",", ".");

                    if (!decimal.TryParse(amountStr, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    {
                        Console.WriteLine($"❌ Не удалось преобразовать сумму: {amountStr}");
                        continue;
                    }

                    var transaction = new CreateTransactionDto
                    {
                        Name = description,
                        Date = dateTime,
                        sum = amount,
                        UserId = _userId,
                        CategoryId = DetermineCategory(description)
                    };

                    transactions.Add(transaction);
                    Console.WriteLine($"✅ Добавлена транзакция: {transaction.Date:d}, {transaction.sum}, {transaction.Name}");

                    i += 3; // Переход к следующему блоку
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка при обработке блока: {ex.Message}");
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
                    var strategy = new SimpleTextExtractionStrategy();
                    var pageText = PdfTextExtractor.GetTextFromPage(document.GetPage(i), strategy);
                    text.AppendLine(pageText);
                }
            }

            return text.ToString();
        }

        private async Task InitializeCategories()
        {
            var categories = await _categoryService.GetUserCategoriesAsync(_userId);

            foreach (var category in categories)
            {
                _categoryCache[category.name.ToLower()] = category.id;
            }

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
                    catch (Exception) { }
                }
            }
        }

        private int? DetermineCategory(string description)
        {
            description = description.ToLower();

            var mappings = new Dictionary<string, string>
            {
                { "pyaterochka", "продукты" },
                { "magnit", "продукты" },
                { "krasnoe", "продукты" },
                { "apteka", "аптека" },
                { "yandex", "магазины" },
                { "перевод", "переводы" },
                { "tinkoff", "переводы" },
                { "sbp", "переводы" }
            };

            foreach (var kv in mappings)
            {
                if (description.Contains(kv.Key))
                    return _categoryCache.TryGetValue(kv.Value, out int? catId) ? catId : null;
            }

            return _categoryCache.TryGetValue("магазины", out int? defCatId) ? defCatId : null;
        }
    }
}

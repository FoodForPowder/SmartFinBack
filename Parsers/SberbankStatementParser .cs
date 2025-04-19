using System.Text.RegularExpressions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SmartFin.Interfaces;
using SmartFin.Entities;
using SmartFin.Services;
using SmartFin.DTOs.Transaction;
using SmartFin.DTOs.Expense;
using Smartfin.Extensions;
using System.Collections.Generic;

namespace SmartFin.Parsers
{
    public class SberbankStatementParser : BankStatementParser
    {

        // Кэш для хранения категорий, чтобы не делать запрос в БД для каждой транзакции
        private Dictionary<string, int?> _categoryCache = new Dictionary<string, int?>();

        public SberbankStatementParser(
        TransactionService transactionService,
        CategoryService categoryService,
        int userId)
        : base(transactionService, categoryService, userId)
        {
        }

        public override async Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream)
        {
            // Заполняем кэш категорий при первом запуске
            await FillCategoryCacheAsync();

            var importedTransactions = new List<TransactionDto>();
            var reader = new PdfReader(fileStream);
            var document = new PdfDocument(reader);

            for (int i = 1; i <= document.GetNumberOfPages(); i++)
            {
                var page = document.GetPage(i);
                var listener = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, listener);

                // Разбиваем текст на строки и извлекаем блоки транзакций
                var lines = text.Split('\n');

                // Обрабатываем строки для извлечения транзакций
                for (int j = 0; j < lines.Length; j++)
                {
                    var transactionDto = await ParseTransactionFromLines(lines, j);
                    if (transactionDto != null)
                    {
                        var createdTransactionId = await _transactionService.CreateUserTransaction(transactionDto);
                        if (createdTransactionId > 0)
                        {
                            var transaction = await _transactionService.GetTransactionById(createdTransactionId);
                            if (transaction != null)
                            {
                                importedTransactions.Add(transaction.asDto());
                            }
                        }
                    }
                }
            }

            return importedTransactions;
        }

        private async Task<CreateTransactionDto> ParseTransactionFromLines(string[] lines, int currentIndex)
        {
            // Проверяем, что у нас есть хотя бы две строки для анализа
            if (currentIndex + 1 >= lines.Length) return null;

            var currentLine = lines[currentIndex].Trim();
            var nextLine = lines[currentIndex + 1].Trim();

            // Регулярное выражение для строки с датой операции, категорией и суммой
            var dateAmountPattern = @"(\d{2}\.\d{2}\.\d{4})\s+(\d{2}:\d{2})\s+([^\d]+?)\s+([-+]?\d+[,]\d{2})$";
            var match = Regex.Match(currentLine, dateAmountPattern);

            if (!match.Success) return null;

            // Проверяем, что следующая строка содержит описание операции
            var descriptionPattern = @"\d{2}\.\d{2}\.\d{4}\s+\d{6}\s+(.+)";
            var descriptionMatch = Regex.Match(nextLine, descriptionPattern);

            if (!descriptionMatch.Success) return null;

            try
            {
                // Извлекаем дату
                var dateStr = match.Groups[1].Value;
                var timeStr = match.Groups[2].Value;
                var dateTime = DateTime.ParseExact($"{dateStr} {timeStr}", "dd.MM.yyyy HH:mm", null);

                // Извлекаем категорию
                var categoryName = match.Groups[3].Value.Trim();

                // Извлекаем описание
                var description = descriptionMatch.Groups[1].Value.Trim();

                // Извлекаем сумму
                var amountStr = match.Groups[4].Value;

                // Определяем, является ли операция пополнением (доходом) по наличию знака "+"
                bool isIncome = amountStr.StartsWith("+");

                // Убираем знаки + и - для преобразования в decimal
                decimal amount = decimal.Parse(amountStr.Replace("+", "").Replace("-", ""));

                // Устанавливаем знак суммы в зависимости от типа операции
                if (!isIncome)
                {
                    // Если не доход - делаем сумму отрицательной
                    amount = -amount;
                }

                // Получаем ID категории (или создаем новую, если не существует)
                int? categoryId = await GetOrCreateCategoryId(categoryName);

                // Создаем и возвращаем объект транзакции
                return new CreateTransactionDto
                {
                    Date = dateTime,
                    sum = amount,
                    Name = description,
                    UserId = _userId,
                    CategoryId = categoryId
                };
            }
            catch (Exception)
            {
                // В случае ошибки разбора возвращаем null
                return null;
            }
        }

        private async Task FillCategoryCacheAsync()
        {
            // Получаем все существующие категории пользователя
            var categories = await _categoryService.GetUserCategoriesAsync(_userId);

            // Сопоставления категорий Сбербанка с нашими категориями
            var mappings = new Dictionary<string, string>
            {
                { "Супермаркеты", "Продукты" },
                { "Рестораны и кафе", "Кафе и рестораны" },
                { "Здоровье и красота", "Здоровье" },
                { "Одежда и аксессуары", "Одежда" },
                { "Коммунальные платежи, связь, интернет", "Коммунальные услуги" },
                { "Автомобиль", "Транспорт" },
                { "Все для дома", "Дом" },
                { "Прочие расходы", "Прочее" },
                { "Прочие операции", "Прочее" },
                { "Перевод на карту", "Переводы" },
                { "Перевод с карты", "Переводы" },
                { "Перевод СБП", "Переводы" },
                { "Внесение наличных", "Пополнение" },
                { "Возврат, отмена операции", "Возврат" }
            };

            // Проверяем существующие категории и добавляем в кэш
            foreach (var category in categories)
            {
                _categoryCache[category.name.ToLower()] = category.id;
            }

            // Добавляем маппинги в кэш
            foreach (var mapping in mappings)
            {
                if (!_categoryCache.ContainsKey(mapping.Key.ToLower()))
                {
                    // Если категория из маппинга существует в нашей системе
                    var matchedCategory = categories.FirstOrDefault(c => c.name.ToLower() == mapping.Value.ToLower());
                    if (matchedCategory != null)
                    {
                        _categoryCache[mapping.Key.ToLower()] = matchedCategory.id;
                    }
                }
            }
        }

        private async Task<int?> GetOrCreateCategoryId(string categoryName)
        {
            if (string.IsNullOrEmpty(categoryName))
                return null;

            var normalizedCategory = categoryName.ToLower();

            // Проверяем кэш
            if (_categoryCache.TryGetValue(normalizedCategory, out int? categoryId))
            {
                return categoryId;
            }

            // Если категории нет в кэше, создаем новую
            try
            {
                var categoryDto = new SmartFin.DTOs.Category.CategoryDTO { name = categoryName };
                var newCategory = await _categoryService.CreateCategoryAsync(categoryDto, _userId);

                // Добавляем новую категорию в кэш
                _categoryCache[normalizedCategory] = newCategory.id;

                return newCategory.id;
            }
            catch (Exception)
            {
                // Если произошла ошибка, возвращаем null
                return null;
            }
        }
    }
}
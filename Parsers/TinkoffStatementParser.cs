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

namespace SmartFin.Parsers
{
    public class TinkoffStatementParser : BankStatementParser
    {
        public TinkoffStatementParser(
        TransactionService transactionService,
        CategoryService categoryService,
        int userId)
        : base(transactionService, categoryService, userId)
        {
        }

        public override async Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream)
        {
            var importedTransactions = new List<TransactionDto>();
            var reader = new PdfReader(fileStream);
            var document = new PdfDocument(reader);

            for (int i = 1; i <= document.GetNumberOfPages(); i++)
            {
                var page = document.GetPage(i);
                var listener = new LocationTextExtractionStrategy();
                var text = PdfTextExtractor.GetTextFromPage(page, listener);

                var lines = text.Split('\n');

                foreach (var line in lines)
                {
                    var transactionDto = ParseTransactionLine(line);
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

        private CreateTransactionDto ParseTransactionLine(string line)
        {
            // Исправленное регулярное выражение для парсинга строки транзакции
            var pattern = @"(\d{2}\.\d{2}\.\d{4})\s+([+-]?\d+[\.,]\d{2})\s+(.+)";
            var match = Regex.Match(line, pattern);

            if (!match.Success) return null;

            // Попытка преобразовать строку в decimal, заменяя точку на запятую, если нужно
            if (!decimal.TryParse(match.Groups[2].Value.Replace('.', ','), out decimal sum))
            {
                return null;
            }

            return new CreateTransactionDto
            {
                Date = DateTime.ParseExact(match.Groups[1].Value, "dd.MM.yyyy", null),
                sum = sum,
                Name = match.Groups[3].Value.Trim(),
                UserId = _userId,
                CategoryId = null // Категорию можно определять отдельно или оставить null
            };
        }
    }
}
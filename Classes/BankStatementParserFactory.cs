using SmartFin.Interfaces;
using SmartFin.Parsers;
using SmartFin.Services;

namespace SmartFin.Classes
{
    public class BankStatementParserFactory
    {
        private readonly TransactionService _transactionService;
        private readonly CategoryService _categoryService;

        public BankStatementParserFactory(TransactionService transactionService, CategoryService categoryService)
        {
            _transactionService = transactionService;
            _categoryService = categoryService;
        }

        public BankStatementParser CreateParser(string bankName, int userId)
        {
            return bankName.ToLower() switch
            {
                "tinkoff" => new TinkoffStatementParser(_transactionService, _categoryService, userId),
                "sberbank" => new SberbankStatementParser(_transactionService, _categoryService, userId),
                "yandex" => new YandexBankStatementParser(_transactionService, _categoryService, userId),
                "vtb" => new VTBStatementParser(_transactionService, _categoryService, userId),
                // Добавьте другие банки здесь
                _ => throw new ArgumentException($"Парсер для банка {bankName} не реализован")
            };
        }
    }
}
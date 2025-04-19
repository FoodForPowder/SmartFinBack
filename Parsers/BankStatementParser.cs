using SmartFin.DTOs.Expense;
using SmartFin.Interfaces;
using SmartFin.Services;

namespace SmartFin.Parsers
{
    public abstract class BankStatementParser : IBankStatementParser
    {
        protected readonly TransactionService _transactionService;
        protected readonly CategoryService _categoryService;
        protected readonly int _userId;

        protected BankStatementParser(
            TransactionService transactionService,
            CategoryService categoryService,
            int userId)
        {
            _transactionService = transactionService;
            _categoryService = categoryService;
            _userId = userId;
        }

        public abstract Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream);
    }
}
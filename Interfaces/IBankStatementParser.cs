using SmartFin.DTOs.Expense;

namespace SmartFin.Interfaces
{
    public interface IBankStatementParser
    {
        Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream);
    }
}

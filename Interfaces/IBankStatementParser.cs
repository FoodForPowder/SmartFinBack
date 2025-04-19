using SmartFin.DTOs.Expense;
using SmartFin.Services;

namespace SmartFin.Interfaces
{

    public interface IBankStatementParser
    {
        Task<IEnumerable<TransactionDto>> ParseAndImportAsync(Stream fileStream);
    }
}

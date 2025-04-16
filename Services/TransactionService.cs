using Microsoft.EntityFrameworkCore;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Transaction;
using SmartFin.Entities;

namespace SmartFin.Services
{
    public class TransactionService(SmartFinDbContext context, UserService userService)
    {

        private readonly SmartFinDbContext _context = context;


        private readonly UserService _userService = userService;



        public async Task<List<Transaction>> GetUsersTransactions(int userid)
        {
            return await _context.Transactions.Where(x => x.UserId == userid).ToListAsync();
        }

        public async Task CreateUserTransaction(CreateTransactionDto expenseDto)
        {
            var Transaction = new Transaction
            {
                sum = expenseDto.sum,
                Date = expenseDto.Date.ToUniversalTime(),
                Name = expenseDto.Name,
                UserId = expenseDto.UserId,
                CategoryId = expenseDto.CategoryId
            };
            _context.Transactions.Add(Transaction);
            //await CheckExpenseLimitAndNotify(expenseDto.UserId);
            //await CheckGoalPaymentsPossibility(expenseDto.UserId);
            await _context.SaveChangesAsync();
        }
        public async Task<Transaction> GetTransactionById(int id)
        {
            return await _context.Transactions.FirstOrDefaultAsync(x => x.id == id);
        }

        public async Task<bool> DeleteTransaction(int id)
        {
            var expense = await _context.Transactions.FirstOrDefaultAsync(x => x.id == id);
            if (expense == null)
            {
                return false;
            }
            _context.Transactions.Remove(expense);
            await _context.SaveChangesAsync();
            //await CheckExpenseLimitAndNotify(expense.UserId);

            return true;

        }

        public async Task<bool> UpdateTransaction(int id, UpdateTransactionDto expenseDto)
        {
            var expense = await _context.Transactions.FirstOrDefaultAsync(x => x.id == id);
            if (expense == null)
            {
                return false;
            }
            expense.sum = expenseDto.sum;
            expense.Date = expenseDto.Date.ToUniversalTime();
            expense.Name = expenseDto.Name;
            expense.CategoryId = expenseDto.CategoryId;
            await context.SaveChangesAsync();
            //await CheckExpenseLimitAndNotify(expense.UserId);
            //await CheckGoalPaymentsPossibility(expense.UserId);
            return true;
        }





    }
}
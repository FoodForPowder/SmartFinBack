using Microsoft.EntityFrameworkCore;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Expense;
using SmartFin.Entities;

namespace SmartFin.Services
{
    public class ExpenseService(SmartFinDbContext context)
    {

        private readonly SmartFinDbContext _context = context;

        public async Task<List<Expense>> GetUsersExpenses(int userid)
        {
            return await _context.Expenses.Where(x => x.UserId == userid).ToListAsync();
        }

        public async Task CreateUserExpense(CreateExpenseDto expenseDto)
        {
            var Expense = new Expense
            {
                sum = expenseDto.sum,
                Date = expenseDto.Date,
                Name = expenseDto.Name,
                UserId = expenseDto.UserId,
                CategoryId = expenseDto.CategoryId
            };
            _context.Expenses.Add(Expense);
            await _context.SaveChangesAsync();
        }
        public async Task<Expense> GetExpenseById(int id)
        {
            return await _context.Expenses.FirstOrDefaultAsync(x => x.id == id);
        }

        public async Task<bool> DeleteExpense(int id)
        {
            var expense = await _context.Expenses.FirstOrDefaultAsync(x => x.id == id);
            if (expense == null)
            {
                return false;
            }
            _context.Expenses.Remove(expense);
            await _context.SaveChangesAsync();
            return true;

        }

        public async Task<bool> UpdateExpense(int id, UpdateExpenseDto expenseDto)
        {
            var expense = await _context.Expenses.FirstOrDefaultAsync(x => x.id == id);
            if (expense == null)
            {
                return false;
            }
            expense.sum = expenseDto.sum;
            expense.Date = expenseDto.Date;           
            expense.Name = expenseDto.Name;

            await context.SaveChangesAsync();
            return true;
        }


    }
}
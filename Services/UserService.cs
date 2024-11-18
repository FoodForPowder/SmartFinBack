using SmartFin.DbContexts;
using SmartFin.Entities;

public class UserService(SmartFinDbContext context)
{
    private readonly SmartFinDbContext _context = context;


    public async Task<decimal> CalculateAutomaticExpenseLimit(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Пользователь не найден");

        return user.MonthlyIncome * 0.8m;  // 80% от месячного дохода
    }

    public async Task UpdateExpenseLimit(int userId, decimal? newLimit)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Пользователь не найден");

        user.ExpenseLimit = newLimit ?? await CalculateAutomaticExpenseLimit(userId);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateMonthlyIncome(int userId, decimal newIncome)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            throw new Exception("Пользователь не найден");

        user.MonthlyIncome = newIncome;
        if (user.ExpenseLimit == null)
        {
            user.ExpenseLimit = newIncome * 0.8m;
        }
        await _context.SaveChangesAsync();
    }

    public async Task<User> GetUserById(int userId) => await _context.Users.FindAsync(userId);
    

        
    
}
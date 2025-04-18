using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Goal;
using SmartFin.DTOs.Transaction;
using SmartFin.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartFin.Services
{
    ///TO DO
    ///Сделать DTO для всего
    public class GoalService
    {
        private readonly SmartFinDbContext _context;
        private readonly UserService _userService;
        private readonly TransactionService _transactionService;

        public GoalService(SmartFinDbContext context, NotificationService notificationService, UserService userService, TransactionService transactionService)
        {
            _context = context;
            _userService = userService;
            _transactionService = transactionService;
        }

        public async Task<IEnumerable<Goal>> GetUserGoalsAsync(int userId)
        {
            var goals = await _context.Goals
                .Include(g => g.Users) // Загружаем пользователей для каждой цели
                .Where(g => g.Users.Any(u => u.Id == userId)) // Фильтруем цели по пользователю
                .ToListAsync();

            return goals;
        }

        public async Task<(bool isSuccess, string message)> CreateGoalAsync(CreateGoalDto goalDto)
        {
            try
            {
                var newGoal = new Goal
                {
                    dateOfStart = goalDto.dateOfStart.ToUniversalTime(),
                    dateOfEnd = goalDto.dateOfEnd.ToUniversalTime(),
                    payment = goalDto.payment,
                    name = goalDto.name,
                    description = goalDto.description,
                    plannedSum = goalDto.plannedSum,
                    status = "В процессе",
                };

                // Получаем пользователей по их ID
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => goalDto.UserId == u.Id);


                if (user == null)
                {
                    return (false, "Нет такого пользователя");
                }


                if (!await CanUserAffordGoal(user.Id, newGoal.payment))
                {
                    return (false, $"Пользователь {user.Name} не может позволить себе эту цель.");
                }


                newGoal.Users.Add(user); // Добавляем пользователей к цели

                await _context.Goals.AddAsync(newGoal);
                await _context.SaveChangesAsync();
                return (true, "Цель успешно создана");
            }
            catch (Exception ex)
            {
                return (false, ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task<(bool isAchievable, Goal? recalculatedGoal)> RecalculateGoal(GoalDto goalDto)
        {
            var newGoal = new Goal
            {
                dateOfStart = goalDto.dateOfStart,
                dateOfEnd = goalDto.dateOfEnd,
                name = goalDto.name,
                description = goalDto.description,
                plannedSum = goalDto.plannedSum,
                currentSum = goalDto.currentSum,
                status = goalDto.status,
                payment = goalDto.payment,
                Users = _context.Users.Where(u => goalDto.UserId.Contains(u.Id)).ToList(),
            };

            const int maxMonthsToAdd = 120; // Максимальное количество месяцев для добавления

            for (int i = 1; i <= maxMonthsToAdd; i++)
            {
                newGoal.dateOfEnd = goalDto.dateOfEnd.AddMonths(i);
                decimal individualPayment = CalculateMonthlyPayment(newGoal.plannedSum - newGoal.currentSum, DateTime.Now, newGoal.dateOfEnd) / newGoal.Users.Count;

                bool allUsersCanAfford = true;

                // Проверяем всех пользователей
                foreach (var user in newGoal.Users)
                {
                    if (!await CanUserAffordGoal(user.Id, individualPayment))
                    {
                        allUsersCanAfford = false;
                        break;
                    }
                }

                // Если все пользователи могут позволить себе цель с текущей датой окончания
                if (allUsersCanAfford)
                {
                    newGoal.payment = individualPayment;
                    return (true, newGoal);
                }
            }

            // Если мы дошли сюда, значит даже после увеличения срока на maxMonthsToAdd месяцев
            // не удалось найти подходящий вариант
            return (false, null);
        }


        public async Task<Goal?> GetGoalByIdAsync(int id)
        {
            var goal = await SearchGoalByIdAsync(id);
            return goal;
        }
        public async Task<bool> UpdateGoalAsync(Goal updateGoal)
        {
            var goal = await GetGoalByIdAsync(updateGoal.id);
            if (goal == null)
            {
                return false;
            }
            _context.Entry(goal).CurrentValues.SetValues(updateGoal);
            await _context.SaveChangesAsync();
            return true;
        }
        public async Task<bool> DeleteGoalAsync(int id, int userId)
        {
            var goalToDelete = await SearchGoalByIdAsync(id);
            if (goalToDelete == null)
            {
                return false;
            }

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            if (!goalToDelete.Users.Any(u => u.Id == userId))
            {
                return false;
            }

            goalToDelete.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<Goal?> SearchGoalByIdAsync(int id) => await _context.Goals
            .Include(g => g.Users)
            .FirstOrDefaultAsync(g => g.id == id);


        /// <summary>
        /// Получить суммарный месячный платеж у не оплаченных целей.
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task<decimal> GetTotalMonthlyGoalPayments(int userId)
        {
            var activeGoals = await _context.Goals
                .Include(g => g.Users)
                .Where(g => g.Users.Any(u => u.Id == userId) && g.status == "В процессе")
                .ToListAsync();

            return activeGoals.Sum(g => g.payment);
        }



        public async Task ContributeToGoalAsync(int goalId, decimal amount, int userId)
        {
            var goal = await GetGoalByIdAsync(goalId);
            if (goal == null)
                return;
            goal.lastMonthContributionAmount = amount;
            goal.lastContributionDate = DateTime.Now.ToUniversalTime();
            goal.currentSum += amount;

            if (goal.payment != amount)
            {
                goal.payment = CalculateMonthlyPayment(goal.plannedSum - goal.currentSum, goal.dateOfStart, goal.dateOfEnd);
            }
            CreateTransactionDto contributeTransaction = new()
            {
                Name = $"Пополнение цели: {goal.name}",
                sum = -amount,
                Date = DateTime.Now.ToUniversalTime(),
                UserId = userId,
                CategoryId = null
            };

            await _transactionService.CreateUserTransaction(contributeTransaction);
            await _context.SaveChangesAsync();
        }
        private decimal CalculateMonthlyPayment(decimal remainingSum, DateTime startDate, DateTime endDate)
        {
            int totalMonths = ((endDate.Year - startDate.Year) * 12) + endDate.Month - startDate.Month;
            return remainingSum / totalMonths;
        }
        private async Task<bool> CanUserAffordGoal(int userId, decimal newMonthlyPayment)
        {
            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            var currentGoals = await _context.Goals
                .Include(g => g.Users)
                .Where(g => g.Users.Any(u => u.Id == userId) && g.status == "В процессе")
                .ToListAsync();

            decimal totalMonthlyPayments = currentGoals.Sum(g => g.payment) + newMonthlyPayment;

            return totalMonthlyPayments <= (user.MonthlyIncome * 0.2m);
        }

        public async Task JoinGoalAsync(int goalId, int userId)
        {
            var goal = await GetGoalByIdAsync(goalId);
            if (goal == null)
            {
                throw new Exception($"Цель с ID {goalId} не найдена");
            }

            var user = await _userService.GetUserById(userId);
            if (user == null)
            {
                throw new Exception("Пользователь не найден");
            }

            if (goal.Users.Any(u => u.Id == userId))
            {
                throw new Exception("Вы уже являетесь участником этой цели");
            }

            if (!await CanUserAffordGoal(userId, goal.payment))
            {
                throw new Exception("Вы не можете присоединиться к цели, так как ежемесячный платеж превышает 20% от вашего дохода");
            }
            
            goal.Users.Add(user);
            (bool successful, Goal newGoal) = await RecalculateGoal(goal.AsDto());
            if (!successful)
            {
                throw new Exception("Не удалось пересчитать цель. Даже после перерасчета сумма ежемесячных отчислений превышает 20% дохода пользователя.");
            }

            // Копируем данные из пересчитанной цели
            goal.dateOfEnd = newGoal.dateOfEnd;
            goal.payment = newGoal.payment;
            
            await _context.SaveChangesAsync();
        }
    }
}
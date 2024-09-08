using Microsoft.EntityFrameworkCore;
using Smartfin.Extensions;
using SmartFin.DbContexts;
using SmartFin.DTOs.Goal;
using SmartFin.Entities;

namespace SmartFin.Services
{
    ///TO DO
    ///Сделать DTO для всего
    public class GoalService (SmartFinDbContext context)
    {

        private readonly SmartFinDbContext _context = context;
       
        public async Task<IEnumerable<Goal>> GetUserGoalsAsync(int userId)
        {
            var goals = await _context.Goals.Where(x => x.UserId == userId).ToListAsync();

            return goals;
        }

        public async Task CreateGoalAsync(CreateGoalDto goalDto)
        {
            var newGoal = new Goal
            {
                dateOfStart = goalDto.dateOfStart,
                dateOfEnd = goalDto.dateOfEnd,
                payment = goalDto.payment,
                name = goalDto.name,
                description = goalDto.description,
                plannedSum = goalDto.plannedSum,
                currentSum = goalDto.currentSum,
                status = goalDto.status,
                UserId = goalDto.UserId,
            };
            await _context.Goals.AddAsync(newGoal);
            await _context.SaveChangesAsync();
        }
        public async Task<Goal> GetGoalByIdAsync(int id, int userId)
        {
            var goal = await SearchGoalByIdAsync(id, userId);
            return goal;
        }
        public async Task<bool> UpdateGoalAsync(int id, int userId, UpdateGoalDto updateGoal)
        {
            var goalToUpdate = await SearchGoalByIdAsync(id, userId);
            if (goalToUpdate == null)
            {
                return false;
            }
            goalToUpdate.dateOfStart = updateGoal.dateOfStart;
            goalToUpdate.dateOfEnd = updateGoal.dateOfEnd;
            goalToUpdate.payment = updateGoal.payment;
            goalToUpdate.name = updateGoal.name;
            goalToUpdate.description = updateGoal.description;
            goalToUpdate.plannedSum = updateGoal.plannedSum;
            goalToUpdate.currentSum = updateGoal.currentSum;
            goalToUpdate.status = updateGoal.status;
            await _context.SaveChangesAsync();
            return true;

        }
        public async Task<bool> DeleteGoalAsync(int id, int userId)
        {
            var goalToDelete = await SearchGoalByIdAsync(id, userId);
            if (goalToDelete == null)
            {
                return false;
            }
            else
            {
                _context.Goals.Remove(goalToDelete);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        private async Task<Goal> SearchGoalByIdAsync(int id, int userId)
        {
            return await _context.Goals.FirstOrDefaultAsync(x => x.id == id && x.UserId == userId);
        }
    }
}
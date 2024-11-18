using Microsoft.EntityFrameworkCore;
using SmartFin.DbContexts;
using SmartFin.DTOs.Category;
using SmartFin.Entities;

namespace SmartFin.Services
{

    public class CategoryService(SmartFinDbContext context)
    {
        private readonly SmartFinDbContext _context = context;


        public async Task<IEnumerable<Category>> GetUserCategoriesAsync(int userId)
        {
            return await _context.Categories.Where(c => c.UserId == userId).ToListAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int id, int userId)
        {
            return await _context.Categories.FirstOrDefaultAsync(c => c.id == id && c.UserId == userId);
        }

        public async Task<Category> CreateCategoryAsync(CreateCategoryDTO categoryDto)
        {
            var category = new Category
            {
                name = categoryDto.name,
                UserId = categoryDto.UserId
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDTO categoryDto, int userId)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.id == id && c.UserId == userId);
            if (category == null)
            {
                return false;
            }
            category.name = categoryDto.name;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id, int userId)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.id == id && c.UserId == userId);
            if (category == null)
            {
                return false;
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
using webBackend.Models;

namespace webBackend.Services
{
    public interface ICategoryService
    {
        
    }

    namespace webBackend.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly AgoraDbContext _context;

        public CategoryService(AgoraDbContext context)
        {
            _context = context;
        }
    }
}

}

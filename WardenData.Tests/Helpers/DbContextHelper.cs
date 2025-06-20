using Microsoft.EntityFrameworkCore;
using WardenData.Models; // Ensure this using statement is present

namespace WardenData.Tests.Helpers
{
    public static class DbContextHelper
    {
        public static DbContextOptions<AppDbContext> GetInMemoryDbContextOptions(string dbName)
        {
            return new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: dbName)
                .Options;
        }
    }
}

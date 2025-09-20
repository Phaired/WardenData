using WardenData.Models;

namespace WardenData.Services;

public interface IDataConverter
{
    Task ProcessSessionDataAsync(string jsonData, AppDbContext context, int userId);
    Task ProcessOrderDataAsync(string jsonData, AppDbContext context, int userId);
    Task ProcessOrderEffectDataAsync(string jsonData, AppDbContext context, int userId);
    Task ProcessRuneHistoryDataAsync(string jsonData, AppDbContext context, int userId);
}
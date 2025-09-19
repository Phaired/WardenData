using WardenData.Models;

namespace WardenData.Services;

public interface IDataConverter
{
    Task ProcessSessionDataAsync(string jsonData, AppDbContext context);
    Task ProcessOrderDataAsync(string jsonData, AppDbContext context);
    Task ProcessOrderEffectDataAsync(string jsonData, AppDbContext context);
    Task ProcessRuneHistoryDataAsync(string jsonData, AppDbContext context);
}
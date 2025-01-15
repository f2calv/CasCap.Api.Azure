namespace CasCap.Abstractions;

public interface ILogAnalyticsQueryService
{
    //Task Query(string timespan = null);
    //Task GetCustomEvents(string timespan = null);
    Task<List<aiObject>> GetExceptions(int limit = 50);
}

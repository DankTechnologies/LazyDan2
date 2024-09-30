public interface IGameStreamProvider
{
    int Weight { get; }
    bool IsEnabled { get; }
    string Name { get;}
    Task<string> GetCfbStream(string team);
    Task<string> GetMlbStream(string team);
    Task<string> GetNbaStream(string team);
    Task<string> GetNflStream(string team);
    Task<string> GetNhlStream(string team);
    Task<string> GetWnbaStream(string team);
}
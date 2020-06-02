namespace iess_api.Interfaces
{
    public interface IDbConfiguration
    {
        string ConnectionString { get; }
        string Database { get; }
    }
}

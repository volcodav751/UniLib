using LiteDB;

namespace UniLibrary.Api.Services
{
    public class LiteDbService
    {
        private readonly string _connectionString;

        public LiteDbService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("LiteDb")
                ?? "Filename=UniLibrary.db;Connection=shared";
        }

        public LiteDatabase CreateDatabase()
        {
            return new LiteDatabase(_connectionString);
        }
    }
}
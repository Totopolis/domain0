using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class TokenRegistrationRepository : ITokenRegistrationRepository
    {
        private readonly string _connectionString;

        public const string TableName = "[dom].[TokenRegistration]";

        public TokenRegistrationRepository(string connectionString)
            => _connectionString = connectionString;

        public Task<TokenRegistration> FindById(int id)
            => SimpleCommand.ExecuteQueryAsync<TokenRegistration>(_connectionString,
                    $"select * from {TableName} where {nameof(TokenRegistration.Id)}=@p0", id)
                .FirstOrDefault();

        public Task<TokenRegistration> FindLastTokenByUserId(int userId)
            => SimpleCommand.ExecuteQueryAsync<TokenRegistration>(_connectionString,
                    $"select * from {TableName} where {nameof(TokenRegistration.Id)}=@p0", userId)
                .FirstOrDefault();

        public async Task Save(TokenRegistration registration)
        {
            if (registration.Id > 0)
            {
                await MappedCommand.UpdateAsync(_connectionString, TableName, registration,
                    nameof(TokenRegistration.Id));
            }
            else
            {
                registration.Id = (int) await MappedCommand.InsertAndGetIdAsync(_connectionString, TableName,
                    registration, nameof(TokenRegistration.Id));
            }
        }
    }
}

using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql;

namespace Domain0.FastSql
{
    public class SmsRequestRepository : ISmsRequestRepository
    {
        private readonly string _connectionString;

        public const string TableName = "[dom].[Caching]";

        public SmsRequestRepository(string connectionString)
            => _connectionString = connectionString;

        public Task<SmsRequest> Pick(decimal phone)
            => SimpleCommand.ExecuteQueryAsync<SmsRequest>(_connectionString,
                    $"select * from {TableName} where {nameof(SmsRequest.Phone)}=@p0", phone)
                .FirstOrDefault();

        public Task Remove(decimal phone)
            => SimpleCommand.ExecuteNonQueryAsync(_connectionString,
                $"delete from {TableName} where {nameof(SmsRequest.Phone)}=@p0", phone);

        public Task Save(SmsRequest smsRequest)
            => MappedCommand.InsertAsync(_connectionString, TableName, smsRequest);

    }
}

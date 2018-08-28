using System;
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

        public const string TableName = "[dom].[SmsRequest]";
         
        public SmsRequestRepository(string connectionString)
            => _connectionString = connectionString;

        public Task<SmsRequest> Pick(decimal phone)
            => SimpleCommand.ExecuteQueryAsync<SmsRequest>(_connectionString,
                    $"select * from {TableName} where {nameof(SmsRequest.Phone)}=@p0 and {nameof(SmsRequest.ExpiredAt)}>=@p1", phone, DateTime.UtcNow)
                .FirstOrDefault();

        public Task Save(SmsRequest smsRequest)
            => MappedCommand.InsertAsync(_connectionString, TableName, smsRequest);

        public async Task<bool> ConfirmRegister(decimal phone, string password)
        {
            var request = await Pick(phone);
            return request?.Password == password;
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;

namespace Domain0.FastSql
{
    public class SmsRequestRepository : ISmsRequestRepository
    {
        private readonly Func<DbContext> getContext;
        public const string TableName = "[dom].[SmsRequest]";

        public SmsRequestRepository(Func<DbContext> getContextFunc)
        {
            getContext = getContextFunc;
        }

        public Task<SmsRequest> Pick(decimal phone)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} " +
                    $"where {nameof(SmsRequest.Phone)}=@p0 " +
                    $"and {nameof(SmsRequest.ExpiredAt)}>=@p1 " +
                    $"order by id desc",  // get latest request
                    phone, DateTime.UtcNow)
                .ExecuteQueryAsync<SmsRequest>()
                .FirstOrDefault();

        public Task Save(SmsRequest smsRequest) => getContext().InsertAsync(TableName, smsRequest);

        public async Task<bool> ConfirmRegister(decimal phone, string password)
        {
            var request = await Pick(phone);
            return request?.Password == password;
        }

        public Task<SmsRequest> PickByUserId(int userId)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} " +
                    $"where {nameof(SmsRequest.UserId)}=@p0 " +
                    $"and {nameof(SmsRequest.ExpiredAt)}>=@p1 " +
                    $"order by id desc",  // get latest request
                    userId, DateTime.UtcNow)
                .ExecuteQueryAsync<SmsRequest>()
                .FirstOrDefault();
    }
}

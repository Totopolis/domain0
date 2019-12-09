using System;
using System.Linq;
using System.Threading.Tasks;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;

namespace Domain0.Repository.SqlServer
{
    public class SmsRequestRepository : ISmsRequestRepository
    {
        private readonly Func<DbContext> getContext;
        public const string TableName = "[dom].[SmsRequest]";
        public const string KeyName = "Id";

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

        public Task Save(SmsRequest smsRequest) => getContext().InsertAsync(TableName, smsRequest, KeyName);

        public async Task<SmsRequest> ConfirmRegister(decimal phone, string password)
        {
            var request = await Pick(phone);
            if (request?.Password == password)
            {
                await getContext().DeleteAsync(TableName, new { request.Id });
                return request;
            }

            return null;
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

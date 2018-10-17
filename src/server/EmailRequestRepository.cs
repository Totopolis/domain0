using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql.Common;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class EmailRequestRepository : RepositoryBase<int, EmailRequest>, IEmailRequestRepository
    {
        public EmailRequestRepository(Func<DbContext> getContextFunc)
            : base(getContextFunc)
        {
            TableName = "[dom].[EmailRequest]";
        }

        public Task<EmailRequest> Pick(string email)
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} " +
                    $"where {nameof(EmailRequest.Email)}=@p0 " +
                    $"and {nameof(EmailRequest.ExpiredAt)}>=@p1 " +
                    $"order by id desc",  // get latest request
                    email, DateTime.UtcNow)
                .ExecuteQueryAsync<EmailRequest>()
                .FirstOrDefault();

        public Task Save(EmailRequest emailRequest)
            => getContext().InsertAsync(TableName, emailRequest);

        public async Task<bool> ConfirmRegister(string email, string password)
        {
            var request = await Pick(email);
            return request?.Password == password;
        }

        public Task<EmailRequest> PickByUserId(int userId) 
            => getContext()
                .CreateSimple(
                    $"select * from {TableName} " +
                    $"where {nameof(EmailRequest.UserId)}=@p0 " +
                    $"and {nameof(EmailRequest.ExpiredAt)}>=@p1 " +
                    $"order by id desc",  // get latest request
                    userId, DateTime.UtcNow)
                .ExecuteQueryAsync<EmailRequest>()
                .FirstOrDefault();
    }
}

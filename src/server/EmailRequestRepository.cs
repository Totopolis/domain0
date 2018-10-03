using Domain0.Repository;
using Domain0.Repository.Model;
using Gerakul.FastSql;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Domain0.FastSql
{
    public class EmailRequestRepository : RepositoryBase<int, EmailRequest>, IEmailRequestRepository
    {
        public EmailRequestRepository(string connectionString)
            :base(connectionString)
        {
            TableName = "[dom].[EmailRequest]";
        }

        public Task<EmailRequest> Pick(string email)
            => SimpleCommand.ExecuteQueryAsync<EmailRequest>(connectionString,
                    $"select * from {TableName} " +
                    $"where {nameof(EmailRequest.Email)}=@p0 " +
                    $"and {nameof(EmailRequest.ExpiredAt)}>=@p1 " +
                    $"order by id desc",  // get latest request
                    email, DateTime.UtcNow)
                .FirstOrDefault();

        public Task Save(EmailRequest emailRequest)
            => MappedCommand.InsertAsync(connectionString, TableName, emailRequest);

        public async Task<bool> ConfirmRegister(string email, string password)
        {
            var request = await Pick(email);
            return request?.Password == password;
        }

        public Task<EmailRequest> PickByUserId(int userId) => 
            SimpleCommand.ExecuteQueryAsync<EmailRequest>(connectionString,
                $"select * from {TableName} " +
                $"where {nameof(EmailRequest.UserId)}=@p0 " +
                $"and {nameof(EmailRequest.ExpiredAt)}>=@p1 " +
                $"order by id desc",  // get latest request
                userId, DateTime.UtcNow)
            .FirstOrDefault();
    }
}

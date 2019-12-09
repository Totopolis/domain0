using System;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
{
    public class EmailRequestRepository : IEmailRequestRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public EmailRequestRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task Save(EmailRequest emailRequest)
        {
            const string query = @"
insert into dom.""EmailRequest""
(""Email"", ""Password"", ""ExpiredAt"", ""UserId"", ""EnvironmentId"")
values
(@Email, @Password, @ExpiredAt, @UserId, @EnvironmentId)
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, emailRequest);
            }
        }

        public async Task<EmailRequest> Pick(string email)
        {
            const string query = @"
select ""Id"", ""Email"", ""Password"", ""ExpiredAt"", ""UserId"", ""EnvironmentId""
from dom.""EmailRequest""
where ""Email"" = @Email and ""ExpiredAt"" >= @Now
order by ""Id"" desc
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<EmailRequest>(query,
                    new
                    {
                        Email = email,
                        Now = DateTime.UtcNow
                    });
            }
        }

        public async Task<EmailRequest> ConfirmRegister(string email, string password)
        {
            var request = await Pick(email);
            if (request == null || request.Password != password)
                return null;

            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"delete from dom.""EmailRequest"" where ""Id"" = @Id",
                    new { request.Id });
            }
            return request;

        }

        public async Task<EmailRequest> PickByUserId(int userId)
        {
            const string query = @"
select ""Id"", ""Email"", ""Password"", ""ExpiredAt"", ""UserId"", ""EnvironmentId""
from dom.""EmailRequest""
where ""UserId"" = @UserId and ""ExpiredAt"" >= @Now
order by ""Id"" desc
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<EmailRequest>(query,
                    new
                    {
                        UserId = userId,
                        Now = DateTime.UtcNow
                    });
            }
        }
    }
}
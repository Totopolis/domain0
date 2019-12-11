using System;
using System.Threading.Tasks;
using Dapper;
using Domain0.Repository.Model;

namespace Domain0.Repository.PostgreSql
{
    public class SmsRequestRepository : ISmsRequestRepository
    {
        private readonly IDbConnectionProvider _connectionProvider;

        public SmsRequestRepository(IDbConnectionProvider connectionProvider)
        {
            _connectionProvider = connectionProvider;
        }

        public async Task Save(SmsRequest emailRequest)
        {
            const string query = @"
insert into dom.""SmsRequest""
(""Phone"", ""Password"", ""ExpiredAt"", ""UserId"", ""EnvironmentId"")
values
(@Phone, @Password, @ExpiredAt, @UserId, @EnvironmentId)
";
            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(query, emailRequest);
            }
        }

        public async Task<SmsRequest> Pick(decimal phone)
        {
            const string query = @"
select ""Id"", ""Phone"", ""Password"", ""ExpiredAt"", ""UserId"", ""EnvironmentId""
from dom.""SmsRequest""
where ""Phone"" = @Phone and ""ExpiredAt"" >= @Now
order by ""Id"" desc
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<SmsRequest>(query,
                    new
                    {
                        Phone = phone,
                        Now = DateTime.UtcNow
                    });
            }
        }

        public async Task<SmsRequest> ConfirmRegister(decimal phone, string password)
        {
            var request = await Pick(phone);
            if (request == null || request.Password != password)
                return null;

            using (var con = _connectionProvider.Connection)
            {
                await con.ExecuteAsync(
                    @"delete from dom.""SmsRequest"" where ""Id"" = @Id",
                    new { request.Id });
            }

            return request;
        }

        public async Task<SmsRequest> PickByUserId(int userId)
        {
            const string query = @"
select ""Id"", ""Phone"", ""Password"", ""ExpiredAt"", ""UserId"", ""EnvironmentId""
from dom.""SmsRequest""
where ""UserId"" = @UserId and ""ExpiredAt"" >= @Now
order by ""Id"" desc
";
            using (var con = _connectionProvider.Connection)
            {
                return await con.QueryFirstOrDefaultAsync<SmsRequest>(query,
                    new
                    {
                        UserId = userId,
                        Now = DateTime.UtcNow
                    });
            }
        }
    }
}
using Domain0.Repository;
using System.Linq;
using System.Threading.Tasks;
using Gerakul.FastSql;
using Domain0.Repository.Model;

namespace Domain0.FastSql
{
    public class MessageTemplateRepository : IMessageTemplateRepository
    {
        private readonly string _connectionString;

        public const string TableName = "[dom].[Message]";

        public MessageTemplateRepository(string connectionString)
            => _connectionString = connectionString;

        public Task<string> GetWelcomeTemplate()
            => SimpleCommand.ExecuteQueryFirstColumnAsync<string>(_connectionString,
                    $"select {nameof(MessageTemplate.Template)} from {TableName} where {nameof(MessageTemplate.Name)}='WelcomeTemplate'")
                .FirstOrDefault();

        public Task<string> GetRegisterTemplate()
            => SimpleCommand.ExecuteQueryFirstColumnAsync<string>(_connectionString,
                    $"select {nameof(MessageTemplate.Template)} from {TableName} where {nameof(MessageTemplate.Name)}='RegisterTemplate'")
                .FirstOrDefault();

        public Task<string> GetRequestResetTemplate()
            => SimpleCommand.ExecuteQueryFirstColumnAsync<string>(_connectionString,
                    $"select {nameof(MessageTemplate.Template)} from {TableName} where {nameof(MessageTemplate.Name)}='RequestResetTemplate'")
                .FirstOrDefault();
    }
}

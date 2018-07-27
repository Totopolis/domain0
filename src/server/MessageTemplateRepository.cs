using Domain0.Repository;
using System.Linq;
using System.Threading.Tasks;
using Gerakul.FastSql;
using Domain0.Repository.Model;

namespace Domain0.FastSql
{
    public class MessageTemplateRepository : IMessageTemplateRepository
    {
        private readonly string connectionString;

        public const string TableName = "[dom].[Message]";

        public MessageTemplateRepository(string connectionString)
        {
            this.connectionString = connectionString;
        }

        public Task<string> GetWelcomeTemplate(
            MessageTemplateLocale locale,
            MessageTemplateType type)
        {
            return SimpleCommand.ExecuteQueryFirstColumnAsync<string>(
                    connectionString,
                    $"select {nameof(MessageTemplate.Template)} " +
                    $"from {TableName} " +
                    $"where {nameof(MessageTemplate.Name)}      ='WelcomeTemplate'" +
                    $"  and {nameof(MessageTemplate.Locale)}    =@p0" +
                    $"  and {nameof(MessageTemplate.Type)}      =@p1",
                    locale.ToString(), type.ToString())
                .FirstOrDefault();
        }

        public Task<string> GetRegisterTemplate(
            MessageTemplateLocale locale,
            MessageTemplateType type)
        {
            return SimpleCommand.ExecuteQueryFirstColumnAsync<string>(
                    connectionString,
                    $"select {nameof(MessageTemplate.Template)} " +
                    $"from {TableName} " +
                    $"where {nameof(MessageTemplate.Name)}      ='RegisterTemplate'" +
                    $"  and {nameof(MessageTemplate.Locale)}    =@p0" +
                    $"  and {nameof(MessageTemplate.Type)}      =@p1",
                    locale.ToString(), type.ToString())
                .FirstOrDefault();
        }

        public Task<string> GetRequestResetTemplate(
            MessageTemplateLocale locale,
            MessageTemplateType type)
        {
            return SimpleCommand.ExecuteQueryFirstColumnAsync<string>(
                    connectionString,
                    $"select {nameof(MessageTemplate.Template)} " +
                    $"from {TableName} " +
                    $"where {nameof(MessageTemplate.Name)}      ='RequestResetTemplate'" +
                    $"  and {nameof(MessageTemplate.Locale)}    =@p0" +
                    $"  and {nameof(MessageTemplate.Type)}      =@p1",
                    locale.ToString(), type.ToString())
                .FirstOrDefault();
        }
    }
}
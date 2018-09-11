using System.Text;
using System.Threading.Tasks;
using Domain0.Service;
using Gerakul.SqlQueue.InMemory;
using Newtonsoft.Json;

namespace Domain0.Nancy.Service
{
    public class SqlQueueSmsClient : ISmsClient
    {
        public SqlQueueSmsClient(
            SqlQueueSmsClientSettings settingsInstance)
        {
            settings = settingsInstance;

            var factory = new QueueFactory(settings.ConnectionString);
            if (!factory.IsQueueExsists(settings.QueueName))
                factory.CreateQueue(settings.QueueName);

            var client = QueueClient.Create(
                settings.ConnectionString,
                settings.QueueName);

            writer = client.CreateWriter();
        }

        public Task Send(decimal phone, string message)
        {
            var raw = Encoding.UTF8.GetBytes(
                JsonConvert.SerializeObject(new SqlQueueSms { Phone = phone, Message = message }));

            return Task.Run(() => writer.Write(raw));            
        }

        private readonly SqlQueueSmsClientSettings settings;

        private readonly Writer writer;

        private class SqlQueueSms
        {
            public decimal Phone { get; set; }
            public string Message { get; set; }
        }
    }
}

using Nito.AsyncEx;

namespace Domain0.Api.Client
{
    public interface ITokenStore
    {
        string Token { get; set; }
    }

    public interface IClientLockScope
    {
        AsyncReaderWriterLock RequestSetupLock { get; }
    }

    public interface IClientScope<out TClient> : ITokenStore, IClientLockScope
    {
        TClient Client { get; }
    }

    public class ClientLockScope<TClient> : IClientScope<TClient>
    {
        protected ClientLockScope()
        {
        }

        public ClientLockScope(TClient client)
        {
            Client = client;
        }

        protected string TokenValue;
        public virtual string Token
        {
            get
            {
                using (RequestSetupLock.ReaderLock())
                {
                    return TokenValue;
                }
            }
            set
            {
                using (RequestSetupLock.WriterLock())
                {
                    if (string.IsNullOrWhiteSpace(value))
                    {
                        TokenValue = null;
                    }
                    else
                    {
                        TokenValue = value;
                    }
                }
            }
        }

        public virtual TClient Client { get; }

        public AsyncReaderWriterLock RequestSetupLock { get; } = new AsyncReaderWriterLock();
    }
}
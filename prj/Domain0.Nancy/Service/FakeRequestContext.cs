using Domain0.Service;

namespace Domain0.Nancy.Service
{
    class FakeRequestContext : IRequestContext
    {
        public int UserId { get; }
    }
}

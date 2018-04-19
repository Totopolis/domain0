
namespace Domain0.Service
{
    public interface IPasswordGenerator
    {
        string Generate();
    }

    public class PasswordGenerator : IPasswordGenerator
    {
        public string Generate()
        {
            return "test password";
        }
    }
}

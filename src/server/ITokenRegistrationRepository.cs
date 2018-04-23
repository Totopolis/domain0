using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    /// <summary>
    /// 
    /// </summary>
    public interface ITokenRegistrationRepository
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TokenRegistration> FindById(int id);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        Task<int> Register(TokenRegistration registration);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TokenRegistration> FindLastTokenByUserId(int id);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        Task Save(TokenRegistration registration);
    }
}

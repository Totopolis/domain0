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
        /// Returns token registration by identifier
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TokenRegistration> FindById(int id);
        /// <summary>
        /// Updates or inserts new token registration
        /// NOTE: saves Id back to registration object
        /// </summary>
        /// <param name="registration"></param>
        /// <returns></returns>
        Task Save(TokenRegistration registration);
        /// <summary>
        /// Removes all tokens for user
        /// </summary>
        /// <param name="userId">User identifier</param>
        /// <returns></returns>
        Task RevokeByUserId(int userId);
    }
}

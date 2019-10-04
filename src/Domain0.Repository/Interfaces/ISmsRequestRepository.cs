
using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    /// <summary>
    /// Sms registration repository
    /// </summary>
    public interface ISmsRequestRepository
    {
        /// <summary>
        /// Save sms request
        /// </summary>
        /// <param name="smsRequest">Sms registration request</param>
        /// <returns></returns>
        Task Save(SmsRequest smsRequest);
        
        /// <summary>
        /// Get actual request by phone number
        /// </summary>
        /// <param name="phone">phone number</param>
        /// <returns></returns>
        Task<SmsRequest> Pick(decimal phone);
        
        /// <summary>
        /// Confirm request
        /// </summary>
        /// <param name="phone">phone number</param>
        /// <param name="password">sms password (pin)</param>
        /// <returns></returns>
        Task<SmsRequest> ConfirmRegister(decimal phone, string password);

        /// <summary>
        /// Get actual request by userId
        /// </summary>
        /// <param name="userId">user id</param>
        /// <returns></returns>
        Task<SmsRequest> PickByUserId(int userId);
    }
}

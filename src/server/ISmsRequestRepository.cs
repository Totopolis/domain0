
using Domain0.Repository.Model;
using System.Threading.Tasks;

namespace Domain0.Repository
{
    /// <summary>
    /// Репозиторий запросов регистрации.
    /// </summary>
    public interface ISmsRequestRepository
    {
        /// <summary>
        /// Сохранение запроса.
        /// </summary>
        /// <param name="smsRequest">запрос регистрации.</param>
        /// <returns></returns>
        Task Save(SmsRequest smsRequest);
        /// <summary>
        /// Получение запроса по номеру телефона.
        /// </summary>
        /// <param name="phone">номер телефона.</param>
        /// <returns></returns>
        Task<SmsRequest> Pick(decimal phone);
        /// <summary>
        /// Удаление запроса по номеру телефона.
        /// </summary>
        /// <param name="phone">номер телефона.</param>
        /// <returns></returns>
        Task Remove(decimal phone);
    }
}

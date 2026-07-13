using System.Threading.Tasks;

namespace ServiceApotheke.API.Services
{
    public interface INotificationDispatcher
    {
        Task DispatchToPharmacistAsync(int pharmacistId, string title, string body, object dataPayload = null);
    }
}

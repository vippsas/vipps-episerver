using System.Threading.Tasks;
using EPiServer.Commerce.Order;

namespace Vipps.Polling
{
    public interface IVippsPollingService
    {
        Task Run();
        void Start(string orderId, IOrderGroup orderGroup);
    }
}

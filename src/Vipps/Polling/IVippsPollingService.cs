using System.Threading.Tasks;

namespace Vipps.Polling
{
    public interface IVippsPollingService
    {
        Task Run();
        void Start(VippsPollingEntity pollingEntity);
    }
}

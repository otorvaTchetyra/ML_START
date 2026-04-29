using Client.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Client.Services
{
    public interface IHealthService
    {
        Task<HealthStatus> DropDBAsync();
        Task<HealthStatus> GetHealthStatusAsync();
        void ResetUrl(string url);
    }
}
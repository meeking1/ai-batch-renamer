using System.Threading.Tasks;
using AiBatchRenamer.Core.Models;

namespace AiBatchRenamer.Core.Services
{
    public interface IAiNamingService
    {
        bool IsConfigured { get; }

        Task<AiNamingResult> GenerateAsync(AiNamingRequest request);
    }
}

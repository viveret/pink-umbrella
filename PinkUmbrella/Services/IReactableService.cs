using System.Collections.Generic;
using System.Threading.Tasks;
using PinkUmbrella.Models;
using PinkUmbrella.Models.Public;
using Tides.Models;
using Tides.Models.Public;

namespace PinkUmbrella.Services
{
    public interface IReactableService
    {
        string ControllerName { get; }
        ReactionSubject Subject { get; }

        Task<int> GetCount(PublicId id, ReactionType type);

        Task RefreshStats(PublicId id);
        
        Task<List<int>> GetIds();
    }
}
using System.Threading.Tasks;
using PinkUmbrella.Models;
using Tides.Models.Auth;
using Tides.Models.Public;

namespace PinkUmbrella.Services
{
    public interface IRateLimitService
    {
        Task SetLimitForUser(PublicId userId, string property, int? limit);
        Task<int> GetLimitForUser(PublicId userId, string property);
        Task<ActorRateLimitModel> GetAllLimitsForUser(PublicId userId);
        Task<bool> TryActorAction(PublicId userId, string action);

        Task<int> GetRateForUser(PublicId userId, string property);
        Task<ActorRateLimitModel> GetAllRatesForUser(PublicId userId);
        Task IncrementRateForUser(PublicId userId, string property);


        Task SetLimitForGroup(string group, string property, int? limit);
        Task<int> GetLimitForGroup(string group, string property);
        Task<ActorRateLimitModel> GetAllLimitsForGroup(string group);
        

        Task SetLimitForIP(IPAddressModel ip, string property, int? limit);
        Task<int> GetLimitForIP(IPAddressModel ip, string property);
        Task<ActorRateLimitModel> GetAllLimitsForIP(IPAddressModel ip);
        Task<bool> TryIP(IPAddressModel ip, string property);

        Task<int> GetRateForIP(IPAddressModel ip, string property);
        Task<ActorRateLimitModel> GetAllRatesForIP(IPAddressModel ip);
        Task IncrementRateForIp(IPAddressModel ip, string property);
    }
}
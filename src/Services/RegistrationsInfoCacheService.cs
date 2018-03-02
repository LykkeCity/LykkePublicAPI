using System;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.Registration;
using Microsoft.Extensions.Caching.Distributed;
using Services.CacheModels;

namespace Services
{
    public class RegistrationsInfoCacheService : IRegistrationsInfoCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILykkeRegistrationClient _registrationClient;
        private readonly TimeSpan _cacheExpirationPeriod;
        private const string RegistrationsInfoPrefix = "RegistrationsInfo";

        public RegistrationsInfoCacheService(
            IDistributedCache cache,
            ILykkeRegistrationClient registrationClient,
            TimeSpan cacheExpirationPeriod
            )
        {
            _cache = cache;
            _registrationClient = registrationClient;
            _cacheExpirationPeriod = cacheExpirationPeriod;
        }
        
        public async Task<long> GetRegistrationsCountAsync()
        {
            var cachedValue = await _cache.TryGetFromCacheAsync(
                GetRegistrationsCountKey(),
                async () =>
                {
                    var count = await _registrationClient.GetRegistrationsCountAsync();
                    return new CachedRegisteredCount(count);
                },
                _cacheExpirationPeriod);

            return cachedValue.Count;
        }

        private static string GetRegistrationsCountKey() => $"{RegistrationsInfoPrefix}:RegistrationsCount";
    }
}

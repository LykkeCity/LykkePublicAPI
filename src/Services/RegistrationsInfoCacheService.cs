using System;
using System.Threading;
using System.Threading.Tasks;
using Core.Services;
using Lykke.Service.Registration;
using MessagePack;
using Microsoft.Extensions.Caching.Distributed;

namespace Services
{
    public class RegistrationsInfoCacheService : IRegistrationsInfoCacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILykkeRegistrationClient _registrationClient;
        private readonly DistributedCacheEntryOptions _cacheOptions;
        private const string RegistrationsInfoPrefix = "RegistrationsInfo";
        private readonly SemaphoreSlim _lockSlim = new SemaphoreSlim(1, 1);

        public RegistrationsInfoCacheService(
            IDistributedCache cache,
            ILykkeRegistrationClient registrationClient,
            TimeSpan cacheExpirationPeriod
            )
        {
            _cache = cache;
            _registrationClient = registrationClient;
            _cacheOptions = new DistributedCacheEntryOptions {AbsoluteExpirationRelativeToNow = cacheExpirationPeriod};
        }
        
        public async Task<long> GetRegistrationsCountAsync()
        {
            await _lockSlim.WaitAsync();
            
            try
            {
                var value = await _cache.GetAsync(GetRegistrationsCountKey());

                if (value != null) 
                    return MessagePackSerializer.Deserialize<long>(value);
                
                var count = await _registrationClient.GetRegistrationsCountAsync();
                await _cache.SetAsync(GetRegistrationsCountKey(), MessagePackSerializer.Serialize(count), _cacheOptions);
                return count;
            }
            finally
            {
                _lockSlim.Release();
            }
        }

        private static string GetRegistrationsCountKey() => $"{RegistrationsInfoPrefix}:RegistrationsCount";
    }
}

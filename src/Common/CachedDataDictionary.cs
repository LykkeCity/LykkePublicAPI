using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Common
{
    public class CachedDataDictionary<TKey, TValue> where TValue : class
    {
        private Dictionary<TKey, TValue> _cashe = new Dictionary<TKey, TValue>();
        private DateTime _lastRefreshDateTime;

        private readonly Func<Task<Dictionary<TKey, TValue>>> _getData;
        private readonly int _validDataInSeconds;

        public CachedDataDictionary(Func<Task<Dictionary<TKey, TValue>>> getData, int validDataInSeconds = 60 * 5)
        {
            _getData = getData;
            _validDataInSeconds = validDataInSeconds;
        }

        public bool HaveToRefreshCash()
        {
            if (_cashe == null)
                return true;

            return (DateTime.UtcNow - _lastRefreshDateTime).TotalSeconds > _validDataInSeconds;
        }

        public async Task<IDictionary<TKey, TValue>> GetDictionaryAsync()
        {
            if (HaveToRefreshCash())
            {
                _cashe = await _getData();
                _lastRefreshDateTime = DateTime.UtcNow;
            }

            return _cashe;
        }

        public async Task<TValue> GetItemAsync(TKey key)
        {
            if (HaveToRefreshCash())
            {
                _cashe = await _getData();
                _lastRefreshDateTime = DateTime.UtcNow;
            }

            var myCahce = _cashe;


            return myCahce.ContainsKey(key) ? myCahce[key] : null;
        }

        public async Task<IEnumerable<TValue>> Values()
        {
            if (HaveToRefreshCash())
            {
                _cashe = await _getData();
                _lastRefreshDateTime = DateTime.UtcNow;
            }

            var myCahce = _cashe;
            return myCahce.Values;
        }

        public TValue GetItem(TKey key)
        {
            var cache = _cashe;
            return cache.ContainsKey(key) ? cache[key] : default(TValue);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Storage
{
    // ReSharper disable once InconsistentNaming
    public interface INoSQLTableStorage<T> where T : new()
    {

        // Добавить новый элемент асинхронно (элемент вообще не должен существовать)
        Task InsertAsync(T item, params int[] notLogCodes);
        Task InsertAsync(IEnumerable<T> items);

        Task InsertOrMergeAsync(T item);

        Task<T> ReplaceAsync(string partitionKey, string rowKey, Func<T, T> item);

        Task<T> MergeAsync(string partitionKey, string rowKey, Func<T, T> item);

        Task InsertOrReplaceBatchAsync(IEnumerable<T> entites);

        Task InsertOrReplaceAsync(T item);

        Task DeleteAsync(T item);

        Task<T> DeleteAsync(string partitionKey, string rowKey);

        Task DeleteAsync(IEnumerable<T> items);

        Task<T> GetDataAsync(string partition, string row);


        /// <summary>
        /// Получить записи, предварительно отфильтровав их
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        Task<IList<T>> GetDataAsync(Func<T, bool> filter = null);

        /// <summary>
        /// Запрос по одной партиции и нескольким элементам
        /// </summary>
        /// <param name="partitionKey"></param>
        /// <param name="rowKeys"></param>
        /// <param name="pieceSize">На сколько частейделим запрос</param>
        /// <param name="filter">Фильтрация записей</param>
        /// <returns></returns>
        Task<IEnumerable<T>> GetDataAsync(string partitionKey, IEnumerable<string> rowKeys, int pieceSize = 100, Func<T, bool> filter = null);

        Task<IEnumerable<T>> GetDataAsync(IEnumerable<string> partitionKeys, int pieceSize = 100, Func<T, bool> filter = null);

        Task<IEnumerable<T>> GetDataAsync(IEnumerable<Tuple<string, string>> keys, int pieceSize = 100, Func<T, bool> filter = null);


        Task GetDataByChunksAsync(Func<IEnumerable<T>, Task> chunks);

        Task GetDataByChunksAsync(Action<IEnumerable<T>> chunks);
        Task GetDataByChunksAsync(string partitionKey, Action<IEnumerable<T>> chunks);

        Task ScanDataAsync(string partitionKey, Func<IEnumerable<T>, Task> chunk);

        /// <summary>
        /// Scan table by chinks and find an instane
        /// </summary>
        /// <param name="partitionKey">Partition we are going to scan</param>
        /// <param name="dataToSearch">CallBack, which we going to call when we have chunk of data to scan. </param>
        /// <returns>Null or instance</returns>
        Task<T> FirstOrNullViaScanAsync(string partitionKey, Func<IEnumerable<T>, T> dataToSearch);

        Task<IEnumerable<T>> GetDataAsync(string partition, Func<T, bool> filter = null);
        Task<T> GetTopRecordAsync(string partition);
        Task<IEnumerable<T>> GetTopRecordsAsync(string partition, int n);

        Task<IEnumerable<T>> GetDataRowKeysOnlyAsync(IEnumerable<string> rowKeys);
    }
}
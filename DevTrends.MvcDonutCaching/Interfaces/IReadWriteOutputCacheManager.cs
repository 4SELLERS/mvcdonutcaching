using System;
using System.Web.Mvc;

namespace DevTrends.MvcDonutCaching
{
    public interface IReadWriteOutputCacheManager : IOutputCacheManager
    {
        /// <summary>
        /// Implementations should add the given <see cref="cacheItem"/> in the cache.
        /// </summary>
        /// <param name="key">The cache key to add.</param>
        /// <param name="cacheItem">The cache item to add.</param>
        /// <param name="utcExpiry">The cache item UTC expiry date and time.</param>
        void AddItem(string key, CacheItem cacheItem, DateTime utcExpiry);

        /// <summary>
        /// Implementations should retrieve a cache item the given the <see cref="key"/>.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>A <see cref="CacheItem"/> instance on cache hit, null otherwise.</returns>
        CacheItem GetItem(string key);

        /// <summary>
        /// Ignore the result of current execution from cache
        /// </summary>
        /// <param name="context">The conteroller context</param>
        void IgnoreCurrentExecution(ControllerContext context);

        /// <summary>
        /// Check if the current execution should be ignore for caching
        /// </summary>
        /// <param name="context">The conteroller context</param>
        /// <returns>True if current execution should be ignored for caching</returns>
        bool GetIgnoreCurrentExecution(ControllerContext context);
    }
}

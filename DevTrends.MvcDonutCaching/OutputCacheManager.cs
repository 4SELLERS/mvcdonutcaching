using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.Routing;
using DevTrends.MvcDonutCaching.Annotations;

namespace DevTrends.MvcDonutCaching
{
    public class OutputCacheManager : IReadWriteOutputCacheManager
    {
        private const string KeyIgnoreCacheStore = "OutputCacheManager.KeyIgnoreCacheStore";

        private readonly OutputCacheProvider _outputCacheProvider;
        private readonly IKeyBuilder _keyBuilder;

        public OutputCacheManager()
            : this(OutputCache.Instance, new KeyBuilder())
        {
        }

        public OutputCacheManager(OutputCacheProvider outputCacheProvider, IKeyBuilder keyBuilder)
        {
            _outputCacheProvider = outputCacheProvider;
            _keyBuilder = keyBuilder;
        }

        public void AddItem(string key, CacheItem cacheItem, DateTime utcExpiry)
        {
            _outputCacheProvider.Add(key, cacheItem, utcExpiry);
        }

        public CacheItem GetItem(string key)
        {
            return _outputCacheProvider.Get(key) as CacheItem;
        }

        /// <summary>
        /// Removes a single output cache entry for the specified controller and action.
        /// </summary>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="actionName">The name of the controller action method.</param>
        public void RemoveItem([AspMvcController] string controllerName, [AspMvcAction] string actionName)
        {
            RemoveItem(controllerName, actionName, null);
        }

        /// <summary>
        /// Removes a single output cache entry for the specified controller, action and parameters.
        /// </summary>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="actionName">The name of the controller action method.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        public void RemoveItem([AspMvcController] string controllerName, [AspMvcAction] string actionName, object routeValues)
        {
            RemoveItem(controllerName, actionName, new RouteValueDictionary(routeValues));
        }

        /// <summary>
        /// Removes a single output cache entry for the specified controller, action and parameters.
        /// </summary>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="actionName">The name of the controller action method.</param>
        /// <param name="routeValues">A dictionary that contains the parameters for a route.</param>
        public void RemoveItem([AspMvcController] string controllerName, [AspMvcAction] string actionName, RouteValueDictionary routeValues)
        {
            var key = _keyBuilder.BuildKey(controllerName, actionName, routeValues);

            _outputCacheProvider.Remove(key);
        }

        /// <summary>
        /// Removes all output cache entries.
        /// </summary>
        public void RemoveItems()
        {
            RemoveItems(null, null, null);
        }

        /// <summary>
        /// Removes all output cache entries for the specified controller.
        /// </summary>
        /// <param name="controllerName">The name of the controller.</param>
        public void RemoveItems([AspMvcController] string controllerName)
        {
            RemoveItems(controllerName, null, null);
        }

        /// <summary>
        /// Removes all output cache entries for the specified controller and action.
        /// </summary>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="actionName">The name of the controller action method.</param>
        public void RemoveItems([AspMvcController] string controllerName, [AspMvcAction] string actionName)
        {
            RemoveItems(controllerName, actionName, null);
        }

        /// <summary>
        /// Removes all output cache entries for the specified controller, action and parameters.
        /// </summary>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="actionName">The name of the controller action method.</param>
        /// <param name="routeValues">An object that contains the parameters for a route.</param>
        public void RemoveItems([AspMvcController] string controllerName, [AspMvcAction] string actionName, object routeValues)
        {
            RemoveItems(controllerName, actionName, new RouteValueDictionary(routeValues));
        }

        /// <summary>
        /// Removes all output cache entries for the specified controller, action and parameters.
        /// </summary>
        /// <param name="controllerName">The name of the controller that contains the action method.</param>
        /// <param name="actionName">The name of the controller action method.</param>
        /// <param name="routeValues">A dictionary that contains the parameters for a route.</param>
        public void RemoveItems([AspMvcController] string controllerName, [AspMvcAction] string actionName, RouteValueDictionary routeValues)
        {
            var enumerableCache = _outputCacheProvider as IEnumerable<KeyValuePair<string, object>>;

            if (enumerableCache == null)
            {
                throw new NotSupportedException("Ensure that your custom OutputCacheProvider implements IEnumerable<KeyValuePair<string, object>>.");
            }

            var key = _keyBuilder.BuildKey(controllerName, actionName);

            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            var keysToDelete = enumerableCache
                .Where(_ => !string.IsNullOrEmpty(_.Key) && _.Key.StartsWith(key))
                .Select(_ => _.Key);

            if (routeValues != null)
            {
                foreach (var routeValue in routeValues)
                {
                    var keyFrag = _keyBuilder.BuildKeyFragment(routeValue);

                    if (string.IsNullOrEmpty(keyFrag))
                    {
                        continue;
                    }

                    keysToDelete = keysToDelete.Where(_ => !string.IsNullOrEmpty(_) && _.Contains(keyFrag));
                }
            }

            foreach (var keyToDelete in keysToDelete)
            {
                _outputCacheProvider.Remove(keyToDelete);
            }
        }

        /// <summary>
        /// Ignore the result of current execution from cache
        /// </summary>
        /// <param name="context">The conteroller context</param>
        public void IgnoreCurrentExecution(ControllerContext context) {
            context.HttpContext.Items[KeyIgnoreCacheStore] = true;
        }

        /// <summary>
        /// Check if the current execution should be ignore for caching
        /// </summary>
        /// <param name="context">The conteroller context</param>
        /// <returns>True if current execution should be ignored for caching</returns>
        public bool GetIgnoreCurrentExecution(ControllerContext context) {
            return context.HttpContext.Items[KeyIgnoreCacheStore] as bool? ?? false;
        }
    }
}

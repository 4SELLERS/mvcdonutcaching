﻿using DevTrends.MvcDonutCaching.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace DevTrends.MvcDonutCaching
{
    public class KeyGenerator : IKeyGenerator
    {
        private const string RouteDataKeyAction     = "action";
        private const string RouteDataKeyController = "controller";
        private const string DataTokensKeyArea      = "area";

        private readonly IKeyBuilder _keyBuilder;

        public KeyGenerator(IKeyBuilder keyBuilder)
        {
            if (keyBuilder == null)
            {
                throw new ArgumentNullException("keyBuilder");
            }

            _keyBuilder = keyBuilder;
        }

        [CanBeNull]
        public string GenerateKey(ControllerContext context, CacheSettings cacheSettings)
        {
            var routeData = context.RouteData;

            if (routeData == null)
            {
                return null;
            }

            string actionName = null,
                controllerName = null;

            if (
                routeData.Values.ContainsKey(RouteDataKeyAction) &&
                routeData.Values[RouteDataKeyAction] != null)
            {
                actionName = routeData.Values[RouteDataKeyAction].ToString();
            }

            if (
                routeData.Values.ContainsKey(RouteDataKeyController) && 
                routeData.Values[RouteDataKeyController] != null)
            {
                controllerName = routeData.Values[RouteDataKeyController].ToString();
            }

            if (string.IsNullOrEmpty(actionName) || string.IsNullOrEmpty(controllerName))
            {
                return null;
            }

            string areaName = null;

            if (routeData.DataTokens.ContainsKey(DataTokensKeyArea))
            {
                areaName = routeData.DataTokens[DataTokensKeyArea].ToString();
            }

            // remove controller, action and DictionaryValueProvider which is added by the framework for child actions
            var filteredRouteData = routeData.Values.Where(
                x => !string.Equals(x.Key, RouteDataKeyController, StringComparison.OrdinalIgnoreCase) && 
                     !string.Equals(x.Key, RouteDataKeyAction, StringComparison.OrdinalIgnoreCase) &&   
                     !string.Equals(x.Key, DataTokensKeyArea, StringComparison.OrdinalIgnoreCase) &&
                     !(x.Value is DictionaryValueProvider<object>)
            ).ToList();

            if (!string.IsNullOrWhiteSpace(areaName))
            {
                filteredRouteData.Add(new KeyValuePair<string, object>(DataTokensKeyArea, areaName));
            }

            var routeValues = new RouteValueDictionary(filteredRouteData.ToDictionary(x => x.Key, x => x.Value));

            if (!context.IsChildAction)
            {
                // note that route values take priority over form values and form values take priority over query string values

                if ((cacheSettings.Options & OutputCacheOptions.IgnoreFormData) != OutputCacheOptions.IgnoreFormData)
                {
                    foreach (var formKey in context.HttpContext.Request.Form.AllKeys)
                    {
                        if (routeValues.ContainsKey(formKey))
                        {
                            continue;
                        }

                        var item = context.HttpContext.Request.Form[formKey];
                        routeValues.Add(formKey, item ?? string.Empty);
                    }
                }

                if ((cacheSettings.Options & OutputCacheOptions.IgnoreQueryString) != OutputCacheOptions.IgnoreQueryString)
                {
                    foreach (var queryStringKey in context.HttpContext.Request.QueryString.AllKeys)
                    {
                        // queryStringKey is null if url has qs name without value. e.g. test.com?q
                        if (queryStringKey == null || routeValues.ContainsKey(queryStringKey))
                        {
                            continue;
                        }

                        var item = context.HttpContext.Request.QueryString[queryStringKey];
                        routeValues.Add(queryStringKey, item ?? string.Empty);
                    }
                }
            }

            if (!string.IsNullOrEmpty(cacheSettings.VaryByParam))
            {
                if (string.Equals(cacheSettings.VaryByParam, "none", StringComparison.OrdinalIgnoreCase))
                {
                    routeValues.Clear();
                }
                else if (cacheSettings.VaryByParam != "*")
                {
                    var parameters = SplitValues(cacheSettings.VaryByParam)
                        .Join(routeValues, p => p, rv => rv.Key, (p, rv) => rv, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(x => x.Key, x => x.Value);
                    
                    routeValues = new RouteValueDictionary(parameters);
                }
            }

            if (!string.IsNullOrEmpty(cacheSettings.VaryByCustom))
            {
                // If there is an existing route value with the same key as varybycustom, we should overwrite it
                routeValues[cacheSettings.VaryByCustom] = context.HttpContext.ApplicationInstance.GetVaryByCustomString(HttpContext.Current, cacheSettings.VaryByCustom);
            }

            if (!string.IsNullOrEmpty(cacheSettings.VaryByHeader) && !string.Equals(cacheSettings.VaryByHeader, "none", StringComparison.OrdinalIgnoreCase)) 
            {
                var headers = context.HttpContext.Request.Headers;
                var headersToVaryBy = cacheSettings.VaryByHeader == "*"
                    ? headers.AllKeys
                    : SplitValues(cacheSettings.VaryByHeader);

                var headersForCaching = headers.AllKeys
                    .Select(k => new KeyValuePair<string, object>(k, headers.Get(k)))
                    .Join(headersToVaryBy, kv => kv.Key, h => h, (kv, h) => kv, StringComparer.OrdinalIgnoreCase);
                
                foreach (var header in headersForCaching) 
                {
                    routeValues[header.Key] = header.Value ?? string.Empty;
                }                
            }

            var key = _keyBuilder.BuildKey(controllerName, actionName, routeValues);

            return key;
        }

        private static string[] SplitValues(string value) 
        {
            return value?.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
        }

    }
}

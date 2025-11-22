using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using WebApplicationBasic.Controllers;
using WebApplicationBasic.Services;
using Newtonsoft.Json;
using Serilog;

namespace WebApplicationBasic.Filtros
{
    /// <summary>
    /// Cache de saída em Redis para actions JSON.
    /// Serializa o Data do JsonResult como string e devolve ContentResult em hits.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RedisOutputCacheAttribute : ActionFilterAttribute
    {
        private const string CacheKeyItem = "__RedisOutputCache_Key";

        /// <summary>
        /// Prefixo lógico da chave
        /// </summary>
        public string KeyPrefix { get; set; }

        /// <summary>
        /// Duração do cache em segundos.
        /// </summary>
        public int Seconds { get; set; }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Seconds <= 0)
            {
                base.OnActionExecuting(filterContext);
                return;
            }

            try
            {
                var httpContext = filterContext.HttpContext;

                Guid org = Guid.Empty;
                if (filterContext.Controller is BaseController baseController)
                {
                    org = baseController.CurrentOrganizationId;
                }

                var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                var actionName = filterContext.ActionDescriptor.ActionName;

                var sb = new StringBuilder();
                sb.Append(string.IsNullOrEmpty(KeyPrefix) ? "redis:cache" : KeyPrefix);
                sb.Append(':').Append(controllerName);
                sb.Append(':').Append(actionName);

                if (org != Guid.Empty)
                {
                    sb.Append(":org=").Append(org);
                }

                var orderedParams = filterContext.ActionParameters
                    .OrderBy(p => p.Key, StringComparer.OrdinalIgnoreCase);

                foreach (var kvp in orderedParams)
                {
                    sb.Append(':')
                      .Append(kvp.Key)
                      .Append('=');

                    if (kvp.Value == null)
                    {
                        sb.Append("null");
                    }
                    else if (kvp.Value is DateTime dt)
                    {
                        sb.Append(dt.ToString("yyyyMMdd"));
                    }
                    else
                    {
                        sb.Append(kvp.Value.ToString());
                    }
                }

                var cacheKey = sb.ToString();
                httpContext.Items[CacheKeyItem] = cacheKey;

                // Usa versao de string pura do RedisService,
                // pois o valor gravado tambem e string (JSON bruto).
                var cachedJson = RedisService.Get(cacheKey);
                if (!string.IsNullOrEmpty(cachedJson))
                {
                    Log.Debug("CACHE_HIT: Cache encontrado para chave {CacheKey} - Controller: {Controller}, Action: {Action}",
                        cacheKey, controllerName, actionName);

                    filterContext.Result = new ContentResult
                    {
                        Content = cachedJson,
                        ContentType = "application/json"
                    };
                }
                else
                {
                    Log.Debug("CACHE_MISS: Cache não encontrado para chave {CacheKey} - Controller: {Controller}, Action: {Action}",
                        cacheKey, controllerName, actionName);
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "CACHE_ERROR: Erro ao buscar cache");
                // Em caso de erro no cache, apenas segue o fluxo normal.
            }

            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (Seconds <= 0 || filterContext.Exception != null)
            {
                base.OnActionExecuted(filterContext);
                return;
            }

            try
            {
                var httpContext = filterContext.HttpContext;
                var cacheKey = httpContext.Items[CacheKeyItem] as string;
                if (string.IsNullOrEmpty(cacheKey))
                {
                    base.OnActionExecuted(filterContext);
                    return;
                }

                string jsonToCache = null;

                if (filterContext.Result is JsonResult jsonResult)
                {
                    jsonToCache = JsonConvert.SerializeObject(
                        jsonResult.Data,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                }
                else if (filterContext.Result is ContentResult contentResult &&
                         string.Equals(contentResult.ContentType, "application/json", StringComparison.OrdinalIgnoreCase))
                {
                    jsonToCache = contentResult.Content;
                }

                if (!string.IsNullOrEmpty(jsonToCache))
                {
                    RedisService.Set(cacheKey, jsonToCache, TimeSpan.FromSeconds(Seconds));

                    var controllerName = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName;
                    var actionName = filterContext.ActionDescriptor.ActionName;

                    Log.Debug("CACHE_SET: Cache armazenado para chave {CacheKey} - Controller: {Controller}, Action: {Action}, TTL: {Seconds}s",
                        cacheKey, controllerName, actionName, Seconds);
                }
            }
            catch (Exception ex)
            {
                var controllerName = filterContext.ActionDescriptor?.ControllerDescriptor?.ControllerName;
                var actionName = filterContext.ActionDescriptor?.ActionName;

                Log.Warning(ex, "CACHE_SET_ERROR: Erro ao armazenar cache para {Controller}:{Action}", controllerName, actionName);
                // Qualquer falha no cache não pode quebrar a requisição.
            }

            base.OnActionExecuted(filterContext);
        }
    }
}

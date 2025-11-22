using Serilog;
using System;
using System.Diagnostics;
using System.Web.Mvc;

namespace WebApplicationBasic.Filtros
{
    public class RequestLoggingActionFilter : ActionFilterAttribute
    {
        private const string STOPWATCH_KEY = "ActionStopwatch";

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var stopwatch = Stopwatch.StartNew();
            filterContext.HttpContext.Items[STOPWATCH_KEY] = stopwatch;

            var request = filterContext.HttpContext.Request;
            var controller = filterContext.Controller.GetType().Name;
            var action = filterContext.ActionDescriptor.ActionName;

            Log.Information("ACTION_START: {Controller}.{Action} - {Method} {Url} from {RemoteIP} - User: {User}",
                controller,
                action,
                request.HttpMethod,
                request.Url.ToString(),
                GetClientIP(request),
                filterContext.HttpContext.User?.Identity?.Name ?? "Anonymous");

            base.OnActionExecuting(filterContext);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            if (filterContext.HttpContext.Items[STOPWATCH_KEY] is Stopwatch stopwatch)
            {
                stopwatch.Stop();

                var request = filterContext.HttpContext.Request;
                var controller = filterContext.Controller.GetType().Name;
                var action = filterContext.ActionDescriptor.ActionName;
                var hasException = filterContext.Exception != null;

                var logLevel = GetLogLevel(stopwatch.Elapsed, hasException);

                if (hasException)
                {
                    Log.Write(logLevel, filterContext.Exception,
                        "ACTION_ERROR: {Controller}.{Action} - Duration: {Duration}ms - User: {User}",
                        controller,
                        action,
                        stopwatch.ElapsedMilliseconds,
                        filterContext.HttpContext.User?.Identity?.Name ?? "Anonymous");
                }
                else
                {
                    Log.Write(logLevel,
                        "ACTION_END: {Controller}.{Action} - Duration: {Duration}ms - User: {User}",
                        controller,
                        action,
                        stopwatch.ElapsedMilliseconds,
                        filterContext.HttpContext.User?.Identity?.Name ?? "Anonymous");
                }
            }

            base.OnActionExecuted(filterContext);
        }

        private string GetClientIP(System.Web.HttpRequestBase request)
        {
            string ip = request.Headers["X-Forwarded-For"];

            if (string.IsNullOrEmpty(ip) || ip.ToLower() == "unknown")
                ip = request.Headers["X-Real-IP"];

            if (string.IsNullOrEmpty(ip) || ip.ToLower() == "unknown")
                ip = request.UserHostAddress;

            if (!string.IsNullOrEmpty(ip) && ip.Contains(","))
                ip = ip.Split(',')[0].Trim();

            return ip ?? "Unknown";
        }

        private Serilog.Events.LogEventLevel GetLogLevel(TimeSpan duration, bool hasException)
        {
            if (hasException)
                return Serilog.Events.LogEventLevel.Error;

            if (duration.TotalMilliseconds > 3000)
                return Serilog.Events.LogEventLevel.Warning;

            if (duration.TotalMilliseconds > 1000)
                return Serilog.Events.LogEventLevel.Information;

            return Serilog.Events.LogEventLevel.Debug;
        }
    }
}
//TODO: Watch Milan - (p77) - and implement and install and configure Serilog.AspNetCore, Serilog.Sinks.Seq
//TODO: Watch Milan - (p77) - REALLY Prof. Info and Guiding!
//namespace HrmSystem.Web.Middlewares;

//public class RequestLogContextMiddleware
//{
//    private readonly RequestDelegate _requestDelegate;

//    public RequestLogContextMiddleware(RequestDelegate requestDelegate)
//    {
//        _requestDelegate = requestDelegate;
//    }

//    public Task InvokeAsync(HttpContext httpContext)
//    {
//        using (LogContext.PushProperty("CorrelationId", httpContext.TraceIdentifier))
//        {
//            // the purpose is pushing the request correlation id into the log context
//            // to be included in the structured log of a life time of http request
//            return _next(httpContext);
//        }
//    }
//}

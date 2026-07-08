using HrmSystem.Application.Common.Interfaces.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;

namespace HrmSystem.Application.Common.Behaviors;

/*
    //!     I Just want to ONLY have run (Logging) for my Commands Pipeline; I don't care about Logging My Queries now
    //?         - I want my Queries to be as fast as possible, so I don't want the overhead of logging in the pipeline for them. I can always add logging directly in the handlers if needed.
    //?         - While for Commands; Their is a lot of value in adding some information of what command is being executed and with what data; So I want to have a logging behavior in the pipeline for my Commands.
    //*     [[TRequest request]] is the Command being executed.
    //*     [[RequestHandlerDelegate<TResponse> next]] is a (delegate) that represents the next [[behavior]] or the actual handler (CommandHandler) in the pipeline. When you call next(), it will execute the next step in the pipeline, which could be another behavior or the final handler that processes the command.
    //!     🚨🚨🚨 More Ideas to add: 🔺 Current User Id     🔺 Command's Id if we assign Ids to the Commands     🔺 What is the Correlation Id or Request Id and so on.
 */
internal sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        //! Using Reflection to get the (Type Name) of the (Command) being executed, so we can log the [[Command Name]].
        string commandTypeName = request.GetType().Name;
        bool isLoggingEnabled = _logger.IsEnabled(LogLevel.Information);

        try
        {
            if (isLoggingEnabled)
            {
                _logger.LogInformation("[START] Executing command: {CommandName}", commandTypeName);
            }

            TResponse? responseResult = await next(cancellationToken);

            if (isLoggingEnabled)
            {
                _logger.LogInformation(
                    "[END] Finished execution of command: {CommandName}",
                    commandTypeName
                );
            }
            return responseResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "✖ Error executing command: {CommandName}", commandTypeName);
            throw;
        }
    }
}

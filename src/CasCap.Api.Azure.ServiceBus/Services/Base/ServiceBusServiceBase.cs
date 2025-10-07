namespace CasCap.Services;

public abstract class ServiceBusServiceBase
{
    protected ILogger _logger;

    protected ServiceBusServiceBase(ILogger<ServiceBusServiceBase> logger) => _logger = logger;

    public event EventHandler<ProcessMessageEventArgs>? MessageReceivedEvent;
    protected virtual void OnRaiseMessageReceivedEvent(ProcessMessageEventArgs args) { MessageReceivedEvent?.Invoke(this, args); }

    public event EventHandler<ProcessErrorEventArgs>? ErrorReceivedEvent;
    protected virtual void OnRaiseErrorReceivedEvent(ProcessErrorEventArgs args) { ErrorReceivedEvent?.Invoke(this, args); }

    protected async Task MessageHandler(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        _logger.LogInformation("{ClassName} Received: {MessageBody}", nameof(ServiceBusServiceBase), messageBody);
        OnRaiseMessageReceivedEvent(args);
        await args.CompleteMessageAsync(args.Message);
    }

    protected Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "{ClassName} error args {@Args}", nameof(ServiceBusServiceBase), args);
        OnRaiseErrorReceivedEvent(args);
        return Task.CompletedTask;
    }
}

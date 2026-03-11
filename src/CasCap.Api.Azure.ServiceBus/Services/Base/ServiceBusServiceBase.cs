namespace CasCap.Services;

/// <summary>Base class providing common Service Bus message and error event handling.</summary>
public abstract class ServiceBusServiceBase
{
    /// <summary>Logger instance for this class.</summary>
    protected ILogger _logger;

    /// <summary>Initializes a new instance of <see cref="ServiceBusServiceBase" />.</summary>
    protected ServiceBusServiceBase(ILogger<ServiceBusServiceBase> logger) => _logger = logger;

    /// <summary>Raised when a message is received from the Service Bus.</summary>
    public event EventHandler<ProcessMessageEventArgs>? MessageReceivedEvent;

    /// <summary>Raises the <see cref="MessageReceivedEvent"/> event.</summary>
    protected virtual void OnRaiseMessageReceivedEvent(ProcessMessageEventArgs args)
        => MessageReceivedEvent?.Invoke(this, args);

    /// <summary>Raised when an error is received from the Service Bus.</summary>
    public event EventHandler<ProcessErrorEventArgs>? ErrorReceivedEvent;

    /// <summary>Raises the <see cref="ErrorReceivedEvent"/> event.</summary>
    protected virtual void OnRaiseErrorReceivedEvent(ProcessErrorEventArgs args)
        => ErrorReceivedEvent?.Invoke(this, args);

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

using Microsoft.Extensions.Logging;

namespace Handler;

class Program
{
  static void Main(string[] args)
  {
  }
}

interface IApplicationStatus
{
}

record SuccessStatus(string ApplicationId, string Status): IApplicationStatus;
record FailureStatus(DateTime? LastRequestTime, int RetriesCount): IApplicationStatus;

interface IResponse
{
}

record SuccessResponse(string Id, string Status): IResponse;
record FailureResponse(): IResponse;
record RetryResponse(TimeSpan Delay): IResponse;

interface IClient
{
  Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken);
}

interface IHandler
{
  Task<IApplicationStatus> GetApplicationStatus(string id);
}

class Handler : IHandler
{
  private readonly IClient _client1;
  private readonly IClient _client2;
  private readonly ILogger<Handler> _logger;

  public Handler(
    IClient client1,
    IClient client2,
    ILogger<Handler> logger)
  {
    _client1 = client1;
    _client2 = client2;
    _logger = logger;
  }

  public async Task<IApplicationStatus> GetApplicationStatus(string id)
  {
    if (id is null)
    {
      _logger.LogWarning("Null id provided");
      throw new ArgumentNullException(nameof(id), "Null id provided");
    }

    if (string.IsNullOrEmpty(id))
    {
      _logger.LogWarning("Empty id provided");
      throw new ArgumentException("Empty id provided", nameof(id));
    }

    int retryCount = 0;
    var lastRequestTime = DateTime.UtcNow;
    _logger.LogInformation("Starting to send requests to clients with id {clientId}", id);

    var cancellationSource = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    var cancellationToken = cancellationSource.Token;
    Task<IResponse> responseTask = GetClientsResponse(id, cancellationToken);

    try
    {
      while (!cancellationSource.IsCancellationRequested)
      {
        IResponse response = await responseTask;
        switch (response)
        {
          case FailureResponse:
            return new FailureStatus(lastRequestTime, retryCount);
          case RetryResponse retryResponse:
            retryCount++;
            lastRequestTime = DateTime.UtcNow;
            responseTask = GetClientsResponse(id, cancellationToken, retryResponse.Delay);
            break;
          case SuccessResponse successResponse:
            return new SuccessStatus(successResponse.Id, successResponse.Status);
          default:
            throw new ArgumentOutOfRangeException(nameof(response), $"Client with id {id} returned unknown type of response");
        }
      }
    }
    catch (TaskCanceledException e)
    {
      _logger.LogWarning("Finishing attempts to get application statuses because of cancellation requested");

      return new FailureStatus(DateTime.UtcNow, retryCount);
    }
    catch (Exception e)
    {
      _logger.LogError(
        "Client with id {clientId} threw an exception with message {exceptionMessage}",
        id,
        e.Message);

      throw;
    }
    finally
    {
      cancellationSource.Cancel();
    }

    return new FailureStatus(DateTime.UtcNow, retryCount);
  }
  
  private async Task<IResponse> GetClientsResponse(string id, CancellationToken cancellationToken, TimeSpan? delay = null)
  {
    if (delay.HasValue)
    {
      _logger.LogInformation("Waiting {delaySeconds} to retry getting application statuses", delay.Value.Seconds);
      await Task.Delay(delay.Value, cancellationToken);
    }
    
    List<Task<IResponse>> clientResponseTasks = new()
    {
      _client1.GetApplicationStatus(id, cancellationToken),
      _client2.GetApplicationStatus(id, cancellationToken)
    };

    Task<IResponse> responseTask = await Task.WhenAny(clientResponseTasks);

    IResponse response = await responseTask;

    return response;
  }
}

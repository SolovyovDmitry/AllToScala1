Описание задачи, C#

Имеется два, синхронизированных между собой источника данных (доступ к ним осуществляется с помощью интерфейса `IClient`), позволяющих получать статус заявки по ее идентификатору.

Необходимо реализовать интерфейс `IHandler`, метод `GetApplicationStatus` которого должен получать статус заявки по переданному в него идентификатору.

В методе необходимо выполнить обращение к обоим сервисам и вернуть ответ, как только будет получен ответ хотя бы от одного из них.




Технические детали

1. Максимальное время работы метода `GetApplicationStatus` не должно превышать 15 секунд (см. п.7).

2. `GetApplicationStatus` должен возвращать ответ клиенту как можно быстрее.

3. В теле `GetApplicationStatus` должны выполняться запросы к сервисам, а также обработка ответов сервисов и преобразование полученных данных в ответ нового метода.

4. В случае получения успешного результата (`SuccessResponse`)/ошибки (`FailureResponse`) хотя бы от одного сервиса необходимо сразу же вернуть его клиенту.

5. В случае получения сообщения о необходимости повторного вызова (`RetryResponse`) - необходимо организовать повторный вызов через указанный интервал времени (`RetryResponse.Delay`).

6. Для успешно выполненной операции вернуть объект `SuccessStatus`, где:

  * `Id` - идентификатор заявки (`SuccessResponse.Id`)

  * `Status` - статус заявки (`SuccessResponse.Status`)

7. В случае возникновения ошибок или таймаута нужно вернуть объект `FailureResponse`, где:

  * `LastRequestTime` - время последнего запроса, завершившегося ошибкой (опциональное);

  * `RetriesCount` - количество запросов к сервисам, которые закончились статусом `RetryResponse`.

8. Допустимо использовать сторонние библиотеки.




В качестве ответа пришлите полную реализацию интерфейса `IHandler` (со списком using'ов). В поле для ответа прикрепите ссылку на репозиторий с вашим решением.




Сниппеты кода

```csharp
interface IClient
{
  Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken);
}

interface IResponse
{
}

record SuccessResponse(string Id, string Status): IResponse;
record FailureResponse(): IResponse;
record RetryResponse(TimeSpan Delay): IResponse;

interface IHandler
{
  Task<IApplicationStatusResponse> GetApplicationStatus(string id);
}

class Handler: IHandler
{
  private readonly IClient _client1;
  private readonly IClient _client2;
  private readonly ILogger<Handler> _logger;
   
  public Handler(
    IClient service1,
    IClient service2,
    ILogger<Handler> logger)
  {
    _service1 = service1;
    _service2 = service2;
    _loggger = logger;
  }
   
  public Task<IApplicationStatus> GetApplicationStatus(string id)
  {
    //TODO: place code here
     
    return Task.CompletedTask;
  }
}

interface IApplicationStatus
{
}

record SuccessStatus(string ApplicationId, string Status): IApplicationStatus;
record FailureStatus(DateTime? LastRequestTime, int RetriesCount): IApplicationStatus;
```

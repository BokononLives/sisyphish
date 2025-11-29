namespace sisyphish.Controllers;

public interface IController : IBaseController
{
    Task Execute();
}

public interface IController<TResponse> : IBaseController
{
    Task<TResponse> Execute();
}

public interface IController<TRequest, TResponse> : IBaseController
{
    Task<TResponse> Execute(TRequest request);
}

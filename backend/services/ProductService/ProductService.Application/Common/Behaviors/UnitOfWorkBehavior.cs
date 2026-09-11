using MediatR;
using ProductService.Application.Common.Messaging;
using ProductService.Domain.Interfaces;

namespace ProductService.Application.Common.Behaviors;

/// <summary>Commits the EF Core unit of work (+ outbox flush) after a Command handler succeeds. Skipped for
/// Queries, which must never mutate state (architecture.md §3.3).</summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
        var response = await next();

        if (request is ICommand<TResponse>)
        {
            await unitOfWork.SaveChangesAsync(ct);
        }

        return response;
    }
}

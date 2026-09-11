using MediatR;

namespace ProductService.Application.Common.Messaging;

/// <summary>Marks a MediatR request as a write (CQRS Command). UnitOfWorkBehavior commits changes only for
/// requests implementing this marker — Queries never mutate state (architecture.md §3.3).</summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>Marks a MediatR request as a read (CQRS Query). Never passes through UnitOfWorkBehavior.</summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

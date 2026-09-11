namespace ProductService.Application.Common.Interfaces;

/// <summary>Resolves the authenticated caller's identity/roles from the current JWT (FR-012).
/// Implemented in ProductService.Api against HttpContext, registered per-request (Scoped).</summary>
public interface ICurrentUser
{
    string UserId { get; }
    bool IsInRole(string role);
}

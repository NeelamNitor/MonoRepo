using System.Net;
using System.Net.Http.Json;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.ExternalServices;

/// <summary>The narrow, synchronous call to Product Service used at order placement/cancellation
/// (architecture.md §1 / §4) — the only coupling between the two services besides the shared event contracts.</summary>
public sealed class ProductServiceHttpClient(HttpClient httpClient) : IProductServiceClient
{
    public async Task<ReserveStockResponse> ReserveStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"product-service/v1/products/{productId}/reserve-stock", new { orderId, quantity }, ct);

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            var conflict = await response.Content.ReadFromJsonAsync<ReserveStockConflictPayload>(cancellationToken: ct);
            return new ReserveStockResponse(false, null, null, conflict?.AvailableQuantity ?? 0);
        }

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ReserveStockSuccessPayload>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Empty response from Product Service reserve-stock.");

        return new ReserveStockResponse(true, payload.NameSnapshot, payload.UnitPriceSnapshot, payload.ReservedQuantity);
    }

    public async Task ReleaseStockAsync(Guid productId, Guid orderId, int quantity, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync(
            $"product-service/v1/products/{productId}/release-stock", new { orderId, quantity }, ct);
        response.EnsureSuccessStatusCode();
    }

    private sealed record ReserveStockSuccessPayload(Guid ProductId, string NameSnapshot, decimal UnitPriceSnapshot, int ReservedQuantity);
    private sealed record ReserveStockConflictPayload(Guid ProductId, int AvailableQuantity);
}

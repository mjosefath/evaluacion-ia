using Inventario.Contracts.Common;

namespace Inventario.Contracts.Products;

/// <summary>GET /api/products/lookup?search&amp;page&amp;pageSize&amp;sort&amp;dir</summary>
public sealed class ProductLookupQuery : PagedQuery;

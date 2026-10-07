using Central.Core.Models;

namespace MockOrderWise;

/// <summary>Shape of Central's POST /api/dispatch response.</summary>
public record DispatchResult(string Status, string OrderNumber);

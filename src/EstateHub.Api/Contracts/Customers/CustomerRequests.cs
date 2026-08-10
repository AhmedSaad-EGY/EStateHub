using System.Text.Json;

namespace EstateHub.Api.Contracts.Customers;

public sealed record UpdateCustomerProfileRequest(
    string? FullName,
    string? Persona,
    string? PhoneNumber);

public sealed record SaveSearchRequest(
    string? Name,
    JsonElement? Filters,
    bool AlertsEnabled);

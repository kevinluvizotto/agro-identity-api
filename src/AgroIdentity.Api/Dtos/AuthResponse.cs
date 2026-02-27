namespace AgroIdentity.Api.Dtos;
public record AuthResponse(string Token, DateTimeOffset ExpiresAt);
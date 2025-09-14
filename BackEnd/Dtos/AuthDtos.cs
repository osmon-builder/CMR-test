public record RegisterTenantDto(string TenantName, string AdminEmail, string Password);
public record LoginDto(string Email, string Password);
public record TokenResponse(string AccessToken);
namespace CmcTs.Core.Services;

public record AdUserInfo(
    string SamAccountName,
    string DisplayName,
    string? Email,
    string? Department);

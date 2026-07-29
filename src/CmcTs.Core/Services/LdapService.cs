using System.DirectoryServices.Protocols;
using System.Net;
using CmcTs.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CmcTs.Core.Services;

// Dùng System.DirectoryServices.Protocols (không phải System.DirectoryServices) vì đây là LDAP thuần
// theo giao thức, chạy được cả khi build/test trên máy không phải Windows/không join domain.
public class LdapService : ILdapService
{
    private readonly LdapOptions _options;
    private readonly ILogger<LdapService> _logger;
    private readonly string _domainSuffix;

    public LdapService(IOptions<LdapOptions> options, ILogger<LdapService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _domainSuffix = DomainSuffixFromBaseDn(_options.BaseDn);
    }

    public Task<AdUserInfo?> AuthenticateAsync(string samAccountName, string password, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(samAccountName) || string.IsNullOrWhiteSpace(password))
        {
            return Task.FromResult<AdUserInfo?>(null);
        }

        var upn = $"{samAccountName}@{_domainSuffix}";

        try
        {
            using var connection = CreateConnection();
            connection.AuthType = AuthType.Basic;
            connection.Credential = new NetworkCredential(upn, password);
            connection.Bind();
        }
        catch (LdapException ex)
        {
            _logger.LogWarning("LDAP bind thất bại cho {SamAccountName}: {Message}", samAccountName, ex.Message);
            return Task.FromResult<AdUserInfo?>(null);
        }

        // Mật khẩu đúng — dùng service account (đảm bảo quyền đọc ổn định) để lấy đầy đủ thuộc tính.
        var results = SearchUsersInternal($"(sAMAccountName={EscapeLdapFilterValue(samAccountName)})", 1);
        var found = results.FirstOrDefault();
        return Task.FromResult<AdUserInfo?>(found ?? new AdUserInfo(samAccountName, samAccountName, null, null));
    }

    public Task<IReadOnlyList<AdUserInfo>> SearchUsersAsync(string searchTerm, int maxResults = 20, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Task.FromResult<IReadOnlyList<AdUserInfo>>(Array.Empty<AdUserInfo>());
        }

        var escaped = EscapeLdapFilterValue(searchTerm);
        var filter = $"(|(sAMAccountName=*{escaped}*)(displayName=*{escaped}*)(cn=*{escaped}*))";
        var results = SearchUsersInternal(filter, maxResults);
        return Task.FromResult<IReadOnlyList<AdUserInfo>>(results);
    }

    private List<AdUserInfo> SearchUsersInternal(string filterInner, int maxResults)
    {
        var filter = $"(&(objectCategory=person)(objectClass=user){filterInner})";
        var list = new List<AdUserInfo>();

        try
        {
            using var connection = CreateServiceConnection();

            var request = new SearchRequest(
                _options.BaseDn,
                filter,
                SearchScope.Subtree,
                "sAMAccountName", "displayName", "mail", "department");
            request.SizeLimit = maxResults;

            if (connection.SendRequest(request) is not SearchResponse response)
            {
                return list;
            }

            foreach (SearchResultEntry entry in response.Entries)
            {
                var sam = GetAttributeValue(entry, "sAMAccountName");
                if (string.IsNullOrWhiteSpace(sam))
                {
                    continue;
                }

                list.Add(new AdUserInfo(
                    sam,
                    GetAttributeValue(entry, "displayName") ?? sam,
                    GetAttributeValue(entry, "mail"),
                    GetAttributeValue(entry, "department")));
            }
        }
        catch (LdapException ex)
        {
            _logger.LogError(ex, "Tìm kiếm AD thất bại cho filter {Filter}", filter);
        }

        return list;
    }

    private LdapConnection CreateConnection()
    {
        var identifier = new LdapDirectoryIdentifier(_options.Host, _options.Port);
        var connection = new LdapConnection(identifier)
        {
            AuthType = AuthType.Basic,
        };
        connection.SessionOptions.ProtocolVersion = 3;
        return connection;
    }

    private LdapConnection CreateServiceConnection()
    {
        var connection = CreateConnection();
        connection.Credential = new NetworkCredential(
            $"{_options.ServiceAccountUsername}@{_domainSuffix}",
            _options.ServiceAccountPassword);
        connection.Bind();
        return connection;
    }

    private static string? GetAttributeValue(SearchResultEntry entry, string attributeName)
    {
        if (!entry.Attributes.Contains(attributeName))
        {
            return null;
        }

        var value = entry.Attributes[attributeName][0];
        return value as string ?? value?.ToString();
    }

    // "DC=mdt,DC=local" -> "mdt.local", dùng để dựng UPN đăng nhập (user@domain).
    private static string DomainSuffixFromBaseDn(string baseDn)
    {
        return string.Join(".", baseDn
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.StartsWith("DC=", StringComparison.OrdinalIgnoreCase))
            .Select(p => p[3..]));
    }

    // Escape tối thiểu theo RFC 4515 để tránh LDAP filter injection từ input người dùng.
    private static string EscapeLdapFilterValue(string value)
    {
        return value
            .Replace("\\", "\\5c")
            .Replace("*", "\\2a")
            .Replace("(", "\\28")
            .Replace(")", "\\29")
            .Replace("\0", "\\00");
    }
}

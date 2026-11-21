namespace Modules.Media.Infrastructure.Config;

/// <summary>
/// Description: Vai trò của bản deploy, đọc từ appsettings "Media:Role" (Configuration/Media.json).
/// Provider = site ITS, Public = site WP, Both = dev/test. Không khai báo thì coi là Both.
/// Created date: 08/10/2026
/// </summary>
public static class MediaRole
{
    public static string Current
    {
        get
        {
            var role = App.Configuration?[MediaConst.RoleConfigKey];
            return string.IsNullOrWhiteSpace(role) ? MediaConst.Role.Both : role.Trim();
        }
    }

    /// <summary>Bản deploy hiện tại có bật vai trò <paramref name="role"/> không.</summary>
    public static bool Allows(string role)
    {
        var current = Current;
        return string.Equals(current, MediaConst.Role.Both, StringComparison.OrdinalIgnoreCase)
               || string.Equals(current, role, StringComparison.OrdinalIgnoreCase);
    }
}

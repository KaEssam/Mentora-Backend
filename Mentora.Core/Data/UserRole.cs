namespace Mentora.Core.Data;

public enum UserRole
{
    Mentee = 0,
    Mentor = 1,
    Admin = 2
}

public static class UserRoleExtensions
{
    public static string ToDisplayName(this UserRole role)
    {
        return role switch
        {
            UserRole.Mentee => "Mentee",
            UserRole.Mentor => "Mentor",
            UserRole.Admin => "Admin",
            _ => "Unknown"
        };
    }

    public static string ToDescription(this UserRole role)
    {
        return role switch
        {
            UserRole.Mentee => "User seeking mentorship and guidance",
            UserRole.Mentor => "User providing mentorship and expertise",
            UserRole.Admin => "System administrator with full access",
            _ => "Undefined role"
        };
    }
}

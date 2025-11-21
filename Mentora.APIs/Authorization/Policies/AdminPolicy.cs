using Microsoft.AspNetCore.Authorization;

namespace Mentora.APIs.Authorization.Policies;

public static class Policies
{
    public const string AdminPolicy = "AdminPolicy";
    public const string MentorPolicy = "MentorPolicy";
    public const string MenteePolicy = "MenteePolicy";
}

public class AdminRequirement : IAuthorizationRequirement
{
}

public class MentorRequirement : IAuthorizationRequirement
{
}

public class MenteeRequirement : IAuthorizationRequirement
{
}

// TODO: INTEGRATION - Advanced Authorization - Add resource-based policies and dynamic permissions when authorization system is enhanced

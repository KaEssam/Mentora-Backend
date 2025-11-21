using System.ComponentModel.DataAnnotations;
using Mentora.Core.Data;

namespace Mentora.APIs.DTOs;

public class RoleAssignmentRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }
}

public class RoleResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string RoleDescription { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
}

public class UserRoleDto
{
    public UserRole Role { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class RoleManagementRequest
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public UserRole NewRole { get; set; }

    public string? Reason { get; set; }
}

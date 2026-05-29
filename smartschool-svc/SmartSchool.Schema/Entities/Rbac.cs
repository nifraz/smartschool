using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartSchool.Schema.Entities;

[Table("role")]
public class Role : AbstractRecord
{
    [MaxLength(64)]
    public string Code { get; set; } = default!;

    [MaxLength(128)]
    public string Name { get; set; } = default!;

    [InverseProperty(nameof(RolePermission.Role))]
    public ICollection<RolePermission> RolePermissions { get; set; } = [];

    [InverseProperty(nameof(UserRoleLink.Role))]
    public ICollection<UserRoleLink> UserRoles { get; set; } = [];
}

[Table("permission")]
public class Permission
{
    [Key] public long Id { get; set; }
    [MaxLength(128)] public string Code { get; set; } = default!;
    [MaxLength(256)] public string? Description { get; set; }

    [InverseProperty(nameof(RolePermission.Permission))]
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}

[Table("role_permission")]
public class RolePermission
{
    public long RoleId { get; set; }
    public Role Role { get; set; } = default!;

    public long PermissionId { get; set; }
    public Permission Permission { get; set; } = default!;
}

[Table("user_role")]
public class UserRoleLink
{
    public long UserId { get; set; }
    public User User { get; set; } = default!;

    public long RoleId { get; set; }
    public Role Role { get; set; } = default!;
}

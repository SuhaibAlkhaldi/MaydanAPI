using Maydan.Domain.Common;
using Maydan.Domain.Enums;

namespace Maydan.Domain.Entities;

public class Role : SharedEntities
{
    public int RoleId { get; set; }
    public string RoleNameAr { get; set; } = string.Empty;
    public string RoleNameEn { get; set; } = string.Empty;
    // new field to  Distinguish this role belong to which of the 
    public EntityType? TargetEntityType { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();
}


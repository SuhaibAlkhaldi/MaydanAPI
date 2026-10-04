using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

// Many-to-many: a worker can provide more than one service, and a service can be provided by
// many workers. Same shape as UserGroup/RolePermission — a plain join entity, no extra columns.
public class WorkerServiceLink : SharedEntities
{
    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public int ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}

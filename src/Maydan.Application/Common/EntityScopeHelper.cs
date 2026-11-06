using Maydan.Domain.Entities;
using Maydan.Domain.Enums;

namespace Maydan.Application.Common;

public static class EntityScopeHelper
{
    public static bool CanViewOtherEntities(User currentUser) =>
        currentUser.EntityType is EntityType.BaytAlUrdon or EntityType.Aseza;
}

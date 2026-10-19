using Maydan.Domain.Common;

namespace Maydan.Domain.Entities;

// Deliberately untyped (no "VehicleLicense"/"DriverLicense" kind, no name) — the system only
// enforces a count (see Service.RequiresDocuments), not which specific documents were uploaded;
// that judgment call is left to whoever registers the worker.
public class WorkerDocument : SharedEntities
{
    public int WorkerId { get; set; }
    public Worker Worker { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;
}

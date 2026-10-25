namespace Maydan.API.Models.Workers
{
    public class CreateWorkerProjectLinkRequest
    {
        public Guid ProjectId { get; set; }
        public Guid WorkerId { get; set; }
        public decimal DailyWageAmount { get; set; }
    }

    // UpdateWorkerProjectLinkRequest.cs (for wage change / activation)
    public class UpdateWorkerProjectLinkRequest
    {
        public decimal? DailyWageAmount { get; set; }
        public bool? IsWorkerActive { get; set; }
    }
}

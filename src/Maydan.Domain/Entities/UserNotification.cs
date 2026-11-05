using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Entities
{
    public class UserNotification
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public string Type { get; set; } = "";
        public string TitleAr { get; set; } = ""; public string TitleEn { get; set; } = "";
        public string BodyAr { get; set; } = ""; public string BodyEn { get; set; } = "";
        public string? DataJson { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

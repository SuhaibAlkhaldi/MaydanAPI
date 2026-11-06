using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Maydan.Domain.Entities
{
    public class UserDevice
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Token { get; set; } = "";
        public string Platform { get; set; } = "web";
        public DateTime LastSeenAt { get; set; }
    }
}

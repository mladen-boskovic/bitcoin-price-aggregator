using System;
using System.Collections.Generic;
using System.Text;

namespace Domain
{
    public abstract class Entity
    {
        public int Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STak.TakHub.Core.Shared
{
    public abstract class BaseEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int      Id       { get; set; }
        public DateTime Created  { get; set; }
        public DateTime Modified { get; set; }
    }
}

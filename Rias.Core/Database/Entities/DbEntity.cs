using System;
using System.ComponentModel.DataAnnotations;

namespace Rias.Core.Database.Entities
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
        public DateTime? DateAdded { get; set; } = DateTime.UtcNow;
    }
}
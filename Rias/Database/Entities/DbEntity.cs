using System;
using System.ComponentModel.DataAnnotations;

namespace Rias.Database.Entities
{
    public class DbEntity
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
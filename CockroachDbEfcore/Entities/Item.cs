using System;
using System.ComponentModel.DataAnnotations;

namespace CockroachDbEfcore.Entities
{
    public class Item
    {
        [Required]
        [Key]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
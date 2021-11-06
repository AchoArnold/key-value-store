using System;
using System.ComponentModel.DataAnnotations;

namespace KeyValueStore.Entities
{
    public class Store
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
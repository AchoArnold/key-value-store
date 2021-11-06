using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KeyValueStore.Entities
{
    public class KeyValueEntry
    {
        [Required]
        [Key]
        [StringLength(44)]
        public string Key { get; set; } = default!;

        [Required] [Column(TypeName = "text")]
        public string Value { get; set; } = default!;

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
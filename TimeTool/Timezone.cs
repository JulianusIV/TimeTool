using System.ComponentModel.DataAnnotations.Schema;

namespace TimeTool
{
    public class Timezone
    {
        [Column("id")]
        public ulong Id { get; set; }
        public required string IANADescriptor { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;

namespace WebDev.Models
{
    public class Developer
    {
        [Key]
        public int ID { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public List<string> ImageSrcs { get; set; }
        public List<string> Skills { get; set; }
    }
}

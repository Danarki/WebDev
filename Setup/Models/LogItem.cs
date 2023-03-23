namespace WebDev.Models
{
    public class LogItem
    {
        public int ID { get; set; }
        public string Message { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public DateTime TimeOfOccurence { get; set; }
        public bool IsError { get; set; }
    }
}

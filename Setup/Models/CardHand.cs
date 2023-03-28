namespace WebDev.Models
{
    public class CardHand
    {
        public int ID { get; set; }
        public int GroupID { get; set; }
        public int OwnerID { get; set; }
        public int CardID { get; set; }
        public bool OwnerIsDealer { get; set; }
    }
}

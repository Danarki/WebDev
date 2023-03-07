namespace WebDev.Models.ViewModels
{
    public class LobbyViewModel
    {
        public List<User> UserList { get; set; }
        public int OwnerID { get; set; }
        public int RoomID { get; set; }
        public string? AdminToken { get; set; }
    }
}

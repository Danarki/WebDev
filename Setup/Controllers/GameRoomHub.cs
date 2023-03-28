using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class GameRoomHub : Hub
    {
        public async Task DeleteRoom(string token, string groupID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                IQueryable<GameRoom> roomBuilder = db.GameRooms.Where(x => x.OwnerToken == token);

                if (roomBuilder.Any())
                {
                    db.GameRooms.Remove(roomBuilder.First());

                    db.SaveChanges();

                    await Clients.Group(groupID).SendAsync("removeAll");
                }
                else
                {
                    LogItem logItem = new LogItem();

                    logItem.Message = "Access-control failed for Room Deletion";
                    logItem.TimeOfOccurence = DateTime.Now;
                    logItem.Source = "GameRoomHub";
                    logItem.Type = "CreateRoom";
                    logItem.IsError = true;

                    db.LogItem.Add(logItem);

                    db.SaveChanges();
                }
            }
        }

        public string EncodeString(string str)
        {
            return System.Web.HttpUtility.HtmlEncode(str);
        }

        public async Task GetGroupUsers(int groupID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                List<ConnectedUser>? connections =
                    db.ConnectedUsers.Where(x => x.RoomID.ToString() == groupID.ToString()).ToList();

                if (connections != null)
                {
                    List<string> userNames = new List<string>();

                    foreach (ConnectedUser connection in connections)
                    {
                        User? username = db.Users.Where(x => x.ID == connection.UserID).FirstOrDefault();

                        if (username != null)
                        {
                            userNames.Add(EncodeString(username.Username));
                        }
                    }

                    JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

                    serializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

                    string json = JsonConvert.SerializeObject(userNames, serializerSettings);

                    await Clients.Group(groupID.ToString()).SendAsync("playerList", json);
                }
            }
        }

        public async Task playerJoined(string playerName, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerJoined", EncodeString(playerName));
        }

        public async Task playerLeft(string playerName, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerLeft", EncodeString(playerName));
        }

        public void AddToRoom(int roomID)
        {
            HttpContext context = new HttpContextAccessor().HttpContext;

            using (WebAppContext db = new WebAppContext())
            {
                GameRoom? room = db.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

                if (room != null)
                {
                    int? userId = context.Session.GetInt32("UserID");

                    User user = db.Users.Where(x => x.ID == userId).FirstOrDefault();

                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        ConnectedUser? connectedCheck = db.ConnectedUsers
                            .Where(x => x.UserID == userId && x.RoomID == roomID)
                            .FirstOrDefault();

                        if (connectedCheck == null)
                        {
                            db.ConnectedUsers.Add(c);
                            db.SaveChanges();

                            Groups.AddToGroupAsync(Context.ConnectionId, roomID.ToString());

                            playerJoined(user.Username, roomID.ToString());

                            GetGroupUsers(roomID);
                        }
                    }
                }
            }
        }

        public async void RemoveFromRoom(int roomID)
        {
            HttpContext context = new HttpContextAccessor().HttpContext;

            using (WebAppContext db = new WebAppContext())
            {
                GameRoom? room = db.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

                if (room != null)
                {
                    int? userId = context.Session.GetInt32("UserID");

                    User user = db.Users.Where(x => x.ID == userId).FirstOrDefault();

                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        ConnectedUser? connectedCheck = db.ConnectedUsers
                            .Where(x => x.UserID == userId && x.RoomID == roomID)
                            .FirstOrDefault();

                        if (connectedCheck != null)
                        {
                            db.ConnectedUsers.RemoveRange(
                                db.ConnectedUsers.Where(
                                    x => x.UserID == userId && x.RoomID == roomID
                                )
                            );

                            db.SaveChanges();

                            Groups.RemoveFromGroupAsync(Context.ConnectionId, roomID.ToString());

                            playerLeft(user.Username, roomID.ToString());
                        }

                        await CheckEmptyRoom(roomID);
                    }
                }
            }
        }

        public async Task CheckEmptyRoom(int roomID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                IQueryable<ConnectedUser> connectionsBuilder = db.ConnectedUsers.Where(x => x.RoomID == roomID);

                if (!connectionsBuilder.Any())
                {
                    string roomToken = db.GameRooms.Where(x => x.ID == roomID).First().OwnerToken;
                    await DeleteRoom(roomToken, roomID.ToString());
                }
            }
        }

        public async Task StartGame(string token, string groupID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                GameRoom roomBuilder = db.GameRooms.Where(x => x.OwnerToken == token && x.ID == int.Parse(groupID)).FirstOrDefault();

                if (roomBuilder != null)
                {
                    roomBuilder.HasStarted = true;
                    
                    List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.RoomID == int.Parse(groupID)).ToList();

                    if (userList != null)
                    {
                        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                        foreach (ConnectedUser user in userList)
                        {
                            user.GameID = user.RoomID;
                            user.AuthToken = new string(Enumerable.Repeat(chars, 16)
                                .Select(s => s[new Random(BCrypt.Net.BCrypt.GenerateSalt().GetHashCode()).Next(s.Length)]).ToArray());
                        }
                    }

                    db.SaveChanges();

                    await Clients.Group(groupID).SendAsync("gameStarted", roomBuilder.ID);
                }
            }
        }
    }
}

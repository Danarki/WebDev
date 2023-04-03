using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;
using System.Text;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class GameRoomHub : Hub
    {
        // try to delete room with token as authenticator
        public async Task DeleteRoom(string token, string groupID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                IQueryable<GameRoom> roomBuilder = db.GameRooms.Where(x => x.OwnerToken == token);

                // any rooms with this token?
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
                    logItem.Type = "DeleteRoom";
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

        // Get all users in group with ID
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

                    // ready response JSON
                    JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

                    serializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

                    string json = JsonConvert.SerializeObject(userNames, serializerSettings);

                    await Clients.Group(groupID.ToString()).SendAsync("playerList", json);
                }
            }
        }

        // Add current user to the room they connected to
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

                            // send notification to other users that this user has joined
                            playerJoined(user.Username, roomID.ToString());

                            // get users for current group
                            GetGroupUsers(roomID);
                        }
                    }
                }
            }
        }

        // remove self from room with room ID
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

                    // user was found?
                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        ConnectedUser? connectedCheck = db.ConnectedUsers
                            .Where(x => x.UserID == userId && x.RoomID == roomID)
                            .FirstOrDefault();

                        // connected user is found?
                        if (connectedCheck != null)
                        {
                            // remove user from every connection, this prevents duplicates as well
                            db.ConnectedUsers.RemoveRange(
                                db.ConnectedUsers.Where(
                                    x => x.UserID == userId && x.RoomID == roomID
                                )
                            );

                            db.SaveChanges();

                            Groups.RemoveFromGroupAsync(Context.ConnectionId, roomID.ToString());

                            // send notification to other users
                            playerLeft(user.Username, roomID.ToString());
                        }

                        // check if room has to be removed because empty
                        await CheckEmptyRoom(roomID);
                    }
                }
            }
        }

        // checks if room with ID has to be deleted
        public async Task CheckEmptyRoom(int roomID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                IQueryable<ConnectedUser> connectionsBuilder = db.ConnectedUsers.Where(x => x.RoomID == roomID);

                // checks for no remaining connections
                if (!connectionsBuilder.Any())
                {
                    string roomToken = db.GameRooms.Where(x => x.ID == roomID).First().OwnerToken;
                    await DeleteRoom(roomToken, roomID.ToString());
                }
            }
        }

        // starts game with token as authenticator, only owner with token may start
        public async Task StartGame(string token, string groupID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                GameRoom room = db.GameRooms.Where(x => x.OwnerToken == token && x.ID == int.Parse(groupID)).FirstOrDefault();

                // check if room was found
                if (room != null)
                {
                    room.HasStarted = true; // set room has started flag, room no longer gets shown in the overview
                    
                    List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.RoomID == int.Parse(groupID)).ToList();

                    // check if there are any users
                    if (userList != null)
                    {
                        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

                        // give every user an authtoken and assign the game ID
                        foreach (ConnectedUser user in userList)
                        {
                            user.GameID = user.RoomID;
                            user.AuthToken = new string(Enumerable.Repeat(chars, 16)
                                .Select(s => s[new Random(BCrypt.Net.BCrypt.GenerateSalt().GetHashCode()).Next(s.Length)]).ToArray());
                        }
                    }

                    db.SaveChanges();

                    await Clients.Group(groupID).SendAsync("gameStarted", room.ID);
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
    }
}

using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using WebDev.Models;

namespace WebDev.Controllers
{
    public class GameHub : Hub
    {
        public List<Card> CardHand { get; set; }
        public bool Initialized { get; set; }
        public string RoomID { get; set; }

        public void AddToRoom(int roomID)
        {
            HttpContext context = new HttpContextAccessor().HttpContext;

            using (var db = new WebAppContext())
            {
                var room = db.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

                if (room != null)
                {
                    var userId = context.Session.GetInt32("UserID");

                    User user = db.Users.Where(x => x.ID == userId).FirstOrDefault();

                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        var connectedCheck = db.ConnectedUsers.Where(x => x.UserID == userId && x.RoomID == roomID)
                            .FirstOrDefault();

                        if (connectedCheck == null)
                        {
                            db.ConnectedUsers.Add(c);
                            db.SaveChanges();

                            Groups.AddToGroupAsync(Context.ConnectionId, roomID.ToString());

                            playerJoined(user.Username, roomID.ToString());
                        }
                    }
                }
            }
        }

        public void CreateGameDealer(string GameID)
        {
            using (var db = new WebAppContext())
            {
                Dealer dealer = new Dealer();
                dealer.GameID = int.Parse(GameID);

                db.Dealers.Add(dealer);
                db.SaveChanges();
            }
        }

        public void GivePlayerCard(string gameID, int playerID)
        {
            using (var db = new WebAppContext())
            {
                ConnectedUser user = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID) && x.UserID == playerID).FirstOrDefault();

                if (user != null && user.IsDisabled)
                {
                    return;
                }

                DeckCards c = db.DeckCards.Where(x => x.GameID == int.Parse(gameID) && !x.InUse).FirstOrDefault();

                if (c == null)
                {
                    DeckCards d = new DeckCards();
                    d.InsertDeck(int.Parse(gameID));

                    c = db.DeckCards.Where(x => x.GameID == int.Parse(gameID) && !x.InUse).FirstOrDefault();
                }

                c.InUse = true;
                db.SaveChanges();

                CardHand h = new CardHand
                {
                    CardID = c.ID,
                    OwnerID = playerID,
                    OwnerIsDealer = false
                };

                db.CardHands.Add(h);

                db.SaveChanges();

                var dh = db.CardHands.Where(x => x.OwnerID == playerID && !x.OwnerIsDealer).ToList();
                List<DeckCards> cards = new List<DeckCards>();
                foreach (CardHand hand in dh)
                {
                    DeckCards card = db.DeckCards.Where(x => x.ID == hand.CardID).FirstOrDefault();

                    if (card != null)
                    {
                        cards.Add(card);
                    }
                }

                SendPlayerCard(c, gameID);
                SendPlayerScore(countScore(cards, playerID, gameID), gameID);
            }
        }

        public void GiveDealerCard(string GameID)
        {
            using (var db = new WebAppContext())
            {
                DeckCards c = db.DeckCards.Where(x => x.GameID == int.Parse(GameID) && !x.InUse).FirstOrDefault();
                c.InUse = true;
                db.SaveChanges();

                Dealer d = db.Dealers.Where(x => x.GameID == int.Parse(GameID)).FirstOrDefault();

                CardHand h = new CardHand
                {
                    CardID = c.ID,
                    OwnerID = d.ID,
                    OwnerIsDealer = true
                };

                db.CardHands.Add(h);

                db.SaveChanges();

                var dh = db.CardHands.Where(x => x.OwnerID == d.ID && x.OwnerIsDealer).ToList();
                List<DeckCards> cards = new List<DeckCards>();
                foreach (CardHand hand in dh)
                {
                    DeckCards card = db.DeckCards.Where(x => x.ID == hand.CardID).FirstOrDefault();

                    if (card != null)
                    {
                        cards.Add(card);
                    }
                }

                SendDealerCard(c, GameID);
                SendDealerScore(countScore(cards), GameID);
            }
        }

        public void DisablePlayer(int playerID, string gameID)
        {
            if (playerID != null && gameID != null)
            {
                using (var db = new WebAppContext())
                {
                    ConnectedUser connectedUser = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID) && x.UserID == playerID).FirstOrDefault();

                    if (connectedUser != null)
                    {
                        connectedUser.IsDisabled = true;

                        db.SaveChanges();
                    }

                    List<ConnectedUser> allConnectedUsers = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID)).ToList();

                    bool userNotDisabled = false;

                    foreach (ConnectedUser user in allConnectedUsers)
                    {
                        if (!user.IsDisabled)
                        {
                            userNotDisabled = true;
                        }
                    }

                    if (!userNotDisabled)
                    {
                        GiveDealerCard(gameID);
                    }
                }
            }
        }

        public string countScore(List<DeckCards> cards, int? playerID = null, string? gameID = null)
        {
            int score = 0;
            int score2 = 0;
            bool AceFound = false;
            bool SymbolFound = false;
            foreach (var card in cards)
            {
                if (card.Rank == "A")
                {
                    score += 11;
                    score2 += 1;
                    AceFound = true;
                }
                else if (card.Rank == "K" || card.Rank == "Q" || card.Rank == "J")
                {
                    score += 10;
                    score2 += 10;
                    SymbolFound = true;
                }
                else
                {
                    score += int.Parse(card.Rank);
                    score2 += int.Parse(card.Rank);
                }
            }

            // No Aces, score over 21
            if (score > 21 && !AceFound)
            {
                if (playerID != null && gameID != null)
                {
                    DisablePlayer((int)playerID, gameID);
                }

                return "Bust";
            }

            // Symbol and Ace was found in 2 cards
            if (SymbolFound && AceFound && cards.Count == 2)
            {
                if (playerID != null && gameID != null)
                {
                    DisablePlayer((int)playerID, gameID);
                }

                return "Blackjack";
            }

            // Ace is found
            if (AceFound)
            {
                // Both scores are over 21
                if (score2 > 21 && score > 21)
                {
                    if (playerID != null && gameID != null)
                    {
                        using (var db = new WebAppContext())
                        {
                            ConnectedUser connectedUser = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID) && x.UserID == playerID).FirstOrDefault();
                            connectedUser.IsDisabled = true;

                            db.SaveChanges();
                        }
                    }

                    return "Bust";
                }

                // Both scores are 21 and under 
                if (score < 22)
                {
                    return score2 + "/" + score;
                }

                return score2.ToString();
            }

            if (score == 21)
            {
                DisablePlayer((int)playerID, gameID);
            }

            return score.ToString();
        }

        public void StartRound(string GameID)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, GameID);

            EndRound(int.Parse(GameID));
            DeckCards d = new DeckCards();
            d.InsertDeck(int.Parse(GameID));

            CreateGameDealer(GameID);

            GiveDealerCard(GameID);
            
            GivePlayerCard(GameID, 2);
            GivePlayerCard(GameID, 2);
        }

        public async Task playerHit(string LobbyID, string PlayerID, string AuthToken)
        {
            using (var db = new WebAppContext())
            {
                ConnectedUser connectedUser = db.ConnectedUsers.Where(x => 
                    x.GameID == int.Parse(LobbyID) &&
                    x.UserID == int.Parse(PlayerID) && 
                    x.AuthToken == AuthToken && 
                    !x.IsDisabled).FirstOrDefault();
                if (connectedUser == null)
                {
                    return;
                }
            }

            GivePlayerCard(LobbyID, int.Parse(PlayerID));
        }

        public void EndRound(int GameID)
        {
            using (var db = new WebAppContext())
            {
                Dealer dealer = db.Dealers.Where(x => x.GameID == GameID).FirstOrDefault();

                db.DeckCards.RemoveRange(db.DeckCards.Where(x => x.GameID == GameID));
                db.Dealers.RemoveRange(dealer);
                db.CardHands.RemoveRange(db.CardHands.Where(x => x.OwnerID == dealer.ID));

                db.SaveChanges();
            }
        }

        public async Task SendPlayerCard(DeckCards card, string groupID)
        {
            string json = JsonConvert.SerializeObject(card);

            await Clients.Group(groupID).SendAsync("playerCardReceived", json);
        }

        public async Task SendPlayerScore(string score, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerScoreReceived", score);
        }

        public async Task SendDealerCard(DeckCards card, string groupID)
        {
            string json = JsonConvert.SerializeObject(card);

            await Clients.Group(groupID).SendAsync("dealerCardReceived", json);
        }

        public async Task SendDealerScore(string score, string groupID)
        {
            await Clients.Group(groupID).SendAsync("dealerScoreReceived", score);
        }

        public async Task playerJoined(string playerName, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerJoined", EncodeString(playerName));
        }

        public string EncodeString(string str)
        {
            return System.Web.HttpUtility.HtmlEncode(str);
        }
    }
}



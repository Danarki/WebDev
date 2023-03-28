using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using WebDev.Models;
using WebDev.Models.ViewModels;

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

            using (WebAppContext db = new WebAppContext())
            {
                GameRoom room = db.GameRooms.Where(x => x.ID == roomID).FirstOrDefault();

                if (room != null)
                {
                    int? userId = context.Session.GetInt32("UserID");

                    User user = db.Users.Where(x => x.ID == userId).FirstOrDefault();

                    if (user != null)
                    {
                        ConnectedUser c = new ConnectedUser();
                        c.RoomID = roomID;
                        c.UserID = (int)userId;

                        ConnectedUser? connectedCheck = db.ConnectedUsers.Where(x => x.UserID == userId && x.RoomID == roomID)
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

        public void StartRound(string GameID, string UserID)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, GameID);

            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser leadUser = db.ConnectedUsers.Where(x => x.GameID == int.Parse(GameID)).FirstOrDefault();

                if (leadUser != null && leadUser.UserID == int.Parse(UserID))
                {
                    EndRound(int.Parse(GameID), true);
                    DeckCards d = new DeckCards();
                    d.InsertDeck(int.Parse(GameID));

                    CreateGameDealer(GameID);

                    GiveDealerCard(GameID);

                    GiveAllPlayersBeginningCards(GameID);

                    sendPlayerScores(GameID);
                }
            }
        }

        public void NewRound(string GameID)
        {
            EndRound(int.Parse(GameID), false);
            newRoundStart(GameID);

            GiveDealerCard(GameID);

            GiveAllPlayersBeginningCards(GameID);

            sendPlayerScores(GameID);
        }

        public void GiveAllPlayersBeginningCards(string GameID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.GameID == int.Parse(GameID)).ToList();

                foreach (ConnectedUser user in userList)
                {
                    GivePlayerCard(GameID, user.UserID);
                    GivePlayerCard(GameID, user.UserID);
                }

                //db.ConnectedUsers.Where(x => x.UserID == 1).FirstOrDefault().IsDisabled = true;

                db.SaveChanges();
            }
        }

        public void CreateGameDealer(string GameID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                Dealer dealer = new Dealer();
                dealer.GameID = int.Parse(GameID);
                dealer.HandScore = 0;
                dealer.HasAce = false;

                db.Dealers.Add(dealer);
                db.SaveChanges();
            }
        }

        public void GivePlayerCard(string gameID, int playerID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser user = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID) && x.UserID == playerID).FirstOrDefault();

                if (user != null && user.IsDisabled != null && (bool)user.IsDisabled)
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
                    OwnerIsDealer = false,
                    GroupID = int.Parse(gameID)
                };

                db.CardHands.Add(h);

                db.SaveChanges();

                List<CardHand> dh = db.CardHands.Where(x => x.OwnerID == playerID && !x.OwnerIsDealer).ToList();
                List<DeckCards> cards = new List<DeckCards>();
                foreach (CardHand hand in dh)
                {
                    DeckCards card = db.DeckCards.Where(x => x.ID == hand.CardID && x.GameID == int.Parse(gameID)).FirstOrDefault();

                    if (card != null)
                    {
                        cards.Add(card);
                    }
                }

                SendPlayerCard(playerID, c, gameID, cards.Count);
                countScore(cards, playerID, gameID);
                sendPlayerScores(gameID);
            }
        }

        public void GiveDealerCard(string GameID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                DeckCards c = db.DeckCards.Where(x => x.GameID == int.Parse(GameID) && !x.InUse).FirstOrDefault();

                if (c == null)
                {
                    DeckCards dc = new DeckCards();
                    dc.InsertDeck(int.Parse(GameID));

                    c = db.DeckCards.Where(x => x.GameID == int.Parse(GameID) && !x.InUse).FirstOrDefault();
                }

                c.InUse = true;

                db.SaveChanges();

                Dealer d = db.Dealers.Where(x => x.GameID == int.Parse(GameID)).FirstOrDefault();

                CardHand h = new CardHand
                {
                    CardID = c.ID,
                    OwnerID = d.ID,
                    OwnerIsDealer = true,
                    GroupID = int.Parse(GameID)
                };

                db.CardHands.Add(h);

                db.SaveChanges();

                List<CardHand> dh = db.CardHands.Where(x => x.OwnerID == d.ID && x.OwnerIsDealer).ToList();
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
                countScore(cards, d.ID, GameID, true);
            }
        }

        public bool CheckHasBlackjack(bool HasAce, int cardCount, int score)
        {
            return (HasAce && cardCount == 2 && score == 21);
        }

        public void AssessPoints(string gameID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                Dealer dealer = db.Dealers.Where(x => x.GameID == int.Parse(gameID)).FirstOrDefault();
                List<CardHand> dealerCards = db.CardHands.Where(x => x.OwnerID == dealer.ID && x.OwnerIsDealer).ToList();
                if (dealer != null && dealerCards != null)
                {
                    if (CheckHasBlackjack((bool)dealer.HasAce, dealerCards.Count, (int)dealer.HandScore))
                    {
                        List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID)).ToList(); // players lose except BJ hands

                        foreach (ConnectedUser user in userList)
                        {
                            List<CardHand> userCards = db.CardHands.Where(x => x.OwnerID == user.UserID && !x.OwnerIsDealer).ToList();
                            if (!CheckHasBlackjack((bool)user.HasAce, userCards.Count, (int)user.HandScore)) // quite monstrous?
                            {
                                user.GameScore--;
                                db.SaveChanges();
                            }
                        }
                    }
                    else
                    {
                        int dealerScore = (int)dealer.HandScore;

                        if ((bool)dealer.HasAce && dealerScore >= 22 && dealerScore <= 31) // dealer uses gotten ace as a 1
                        {
                            dealerScore -= 10;
                        }

                        if (dealerScore > 21) // Dealer busted
                        {
                            List<ConnectedUser> winningUserList = db.ConnectedUsers.Where(x => x.HandScore <= 21 || (bool)x.HasAce && x.HandScore <= 31).ToList();

                            List<ConnectedUser> losingUserList = db.ConnectedUsers.Where(x => x.HandScore >= 22 && (bool)!x.HasAce || (bool)x.HasAce && x.HandScore >= 32).ToList();

                            foreach (ConnectedUser loser in losingUserList)
                            {
                                loser.GameScore--;
                            }

                            foreach (ConnectedUser winner in winningUserList)
                            {
                                List<CardHand> userCards = db.CardHands.Where(x => x.OwnerID == winner.UserID && !x.OwnerIsDealer).ToList();
                                if (CheckHasBlackjack((bool)winner.HasAce, userCards.Count, (int)winner.HandScore))
                                {
                                    winner.GameScore += 3;
                                }
                                else
                                {
                                    winner.GameScore++;
                                }
                            }

                            db.SaveChanges();
                            // all players get points when not busted
                        }
                        else // dealer has greater than 16 but not bust
                        {
                            List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID)).ToList();

                            foreach (ConnectedUser user in userList)
                            {
                                List<CardHand> userCards = db.CardHands.Where(x => x.OwnerID == user.UserID && !x.OwnerIsDealer && x.GroupID == int.Parse(gameID)).ToList();
                                if (CheckHasBlackjack((bool)user.HasAce, userCards.Count, (int)user.HandScore))
                                {
                                    user.GameScore += 3;
                                }
                                else
                                {
                                    int userScore = (int)user.HandScore;

                                    if ((bool)user.HasAce && userScore >= 22 && userScore <= 31) // dealer uses gotten ace as a 1
                                    {
                                        userScore -= 10;
                                    }

                                    if (userScore > dealerScore && userScore <= 21)
                                    {
                                        user.GameScore++;
                                    }

                                    if (userScore < dealerScore || userScore >= 22)
                                    {
                                        user.GameScore--;
                                    }
                                }
                            }

                            db.SaveChanges();
                            // all players check :(
                        }
                    }
                }
            }
        }

        public void DisablePlayer(int playerID, string gameID)
        {
            if (playerID != null && gameID != null)
            {
                using (WebAppContext db = new WebAppContext())
                {
                    ConnectedUser connectedUser = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID) && x.UserID == playerID).FirstOrDefault();

                    if (connectedUser != null)
                    {
                        connectedUser.IsDisabled = true;

                        db.SaveChanges();
                    }

                    List<ConnectedUser> allConnectedUsers = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID)).ToList();

                    bool userNotDisabled = true;

                    foreach (ConnectedUser user in allConnectedUsers)
                    {
                        if (user.IsDisabled != null && !(bool)user.IsDisabled)
                        {
                            userNotDisabled = false;
                        }
                    }

                    if (userNotDisabled)
                    {
                        sendPlayerScores(gameID);
                        GiveDealerCard(gameID);
                        AssessPoints(gameID);
                        Thread.Sleep(2500);
                        NewRound(gameID);
                    }
                }
            }
        }

        public void countScore(List<DeckCards> cards, int? playerID = null, string? gameID = null, bool isDealer = false)
        {
            int score = 0;
            int score2 = 0;
            bool AceFound = false;
            bool SymbolFound = false;

            foreach (DeckCards card in cards)
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

            if (isDealer)
            {
                using (WebAppContext db = new WebAppContext())
                {
                    Dealer dealer = db.Dealers.Where(x => x.ID == playerID).FirstOrDefault();


                    if (SymbolFound && AceFound && cards.Count == 2)
                    {
                        SendDealerScore("Blackjack", gameID);
                        dealer.HasAce = true;
                        dealer.HandScore = 21;

                        db.SaveChanges();

                        return;
                    }

                    if (score <= 21)
                    {
                        SendDealerScore(score.ToString(), gameID);
                        dealer.HandScore = score;
                    }
                    else if (AceFound)
                    {
                        dealer.HasAce = true;
                        if (score <= 21)
                        {
                            SendDealerScore(score2 + "/" + score, gameID);
                            dealer.HandScore = score;
                        }
                        else if (score2 <= 21)
                        {
                            SendDealerScore(score2.ToString(), gameID);
                            dealer.HandScore = score;
                        }
                        else
                        {
                            SendDealerScore("Bust", gameID);
                            dealer.HandScore = 22;
                        }
                    }
                    else
                    {
                        SendDealerScore("Bust", gameID);
                        dealer.HandScore = 22;
                    }

                    db.SaveChanges();

                    int totalScore = score;

                    if (score > 21 && AceFound)
                    {
                        totalScore -= 10;
                    }

                    if (cards.Count > 1)
                    {
                        if (totalScore < 16)
                        {
                            Thread.Sleep(1000); //small delay for realism
                            GiveDealerCard(gameID);
                        }
                    }
                }
            }
            else
            {
                using (WebAppContext db = new WebAppContext())
                {
                    ConnectedUser user = db.ConnectedUsers.Where(x => x.UserID == playerID && x.GameID == int.Parse(gameID)).FirstOrDefault();

                    // No Aces, score over 21
                    if (score > 21 && !AceFound)
                    {
                        if (playerID != null && gameID != null)
                        {
                            user.HandScore = 22;
                            db.SaveChanges();

                            SendPlayerScore(user.UserID, "Bust", gameID);
                            DisablePlayer((int)playerID, gameID);
                        }
                        return;
                    }

                    // Symbol and Ace was found in 2 cards
                    if (SymbolFound && AceFound && cards.Count == 2)
                    {
                        user.HasAce = true;
                        if (playerID != null && gameID != null)
                        {
                            user.HandScore = 21;
                            db.SaveChanges();

                            SendPlayerScore(user.UserID, "Blackjack", gameID);
                            DisablePlayer((int)playerID, gameID);
                        }

                        return;
                    }

                    // Ace is found
                    if (AceFound)
                    {
                        user.HasAce = true;
                        // Both scores are over 21
                        if (score2 > 21 && score > 21)
                        {
                            user.HandScore = 32;
                            db.SaveChanges();

                            SendPlayerScore(user.UserID, "Bust", gameID);
                            DisablePlayer((int)playerID, gameID);

                            return;
                        }

                        if (score == 21 || score2 == 21)
                        {
                            user.HandScore = 21;
                            db.SaveChanges();

                            SendPlayerScore(user.UserID, "21", gameID);
                            DisablePlayer((int)playerID, gameID);

                            return;
                        }

                        // Both scores are 21 and under 
                        if (score <= 21)
                        {
                            user.HandScore = score;
                            db.SaveChanges();

                            SendPlayerScore(user.UserID, score2 + "/" + score, gameID);

                            return;
                        }
                        else
                        {
                            user.HandScore = score2;
                            db.SaveChanges();

                            SendPlayerScore(user.UserID, score2.ToString(), gameID);

                            return;
                        }
                    }

                    user.HandScore = score;
                    db.SaveChanges();

                    SendPlayerScore(user.UserID, score.ToString(), gameID);
                }
            }
        }

        public async Task playerLeave(string LobbyID, string PlayerID, string AuthToken)
        {
            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser connectedUser = db.ConnectedUsers.Where(x =>
                    x.GameID == int.Parse(LobbyID) &&
                    x.UserID == int.Parse(PlayerID) &&
                    x.AuthToken == AuthToken).FirstOrDefault();

                db.ConnectedUsers.Remove(connectedUser);

                db.SaveChanges();

                sendPlayerScores(LobbyID);
            }
        }

        public async Task playerStand(string LobbyID, string PlayerID, string AuthToken)
        {
            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser connectedUser = db.ConnectedUsers.Where(x =>
                    x.GameID == int.Parse(LobbyID) &&
                    x.UserID == int.Parse(PlayerID) &&
                    x.AuthToken == AuthToken &&
                    !(bool)x.IsDisabled).FirstOrDefault();
                if (connectedUser == null)
                {
                    return;
                }

                DisablePlayer(connectedUser.UserID, connectedUser.GameID.ToString());
            }
        }

        public async Task playerHit(string LobbyID, string PlayerID, string AuthToken)
        {
            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser connectedUser = db.ConnectedUsers.Where(x =>
                    x.GameID == int.Parse(LobbyID) &&
                    x.UserID == int.Parse(PlayerID) &&
                    x.AuthToken == AuthToken &&
                    !(bool)x.IsDisabled).FirstOrDefault();
                if (connectedUser == null)
                {
                    return;
                }
            }

            GivePlayerCard(LobbyID, int.Parse(PlayerID));
        }

        public void EndRound(int GameID, bool firstClear)
        {
            using (WebAppContext db = new WebAppContext())
            {
                if (firstClear)
                {
                    db.DeckCards.RemoveRange(db.DeckCards.Where(x => x.GameID == GameID));
                    db.Dealers.RemoveRange(db.Dealers.Where(x => x.GameID == GameID));
                }
                db.CardHands.RemoveRange(db.CardHands.Where(x => x.GroupID == GameID));

                List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.GameID == GameID).ToList();

                foreach (ConnectedUser user in userList)
                {
                    user.HandScore = 0;
                    user.HasAce = false;
                    user.IsDisabled = false;

                    if (firstClear)
                        user.GameScore = 0;
                }

                db.SaveChanges();
            }
        }

        public async Task SendPlayerCard(int playerID, DeckCards card, string groupID, int amount)
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            serializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

            CardDataViewModel data = new CardDataViewModel();

            data.Rank = card.Rank;
            data.Symbol = card.Symbol;
            data.UserID = playerID;
            data.Amount = amount;

            string json = JsonConvert.SerializeObject(data, serializerSettings);

            await Clients.Group(groupID).SendAsync("playerCardReceived", json);
        }

        public async Task SendPlayerScore(int playerID, string score, string groupID)
        {
            await Clients.Group(groupID).SendAsync("playerScoreReceived", playerID, score);
        }

        public async Task SendDealerCard(DeckCards card, string groupID)
        {
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            serializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

            string json = JsonConvert.SerializeObject(card, serializerSettings);

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

        public async Task newRoundStart(string groupID)
        {
            await Clients.Group(groupID).SendAsync("newRound");
        }

        public async Task sendPlayerScores(string groupID)
        {
            List<PlayerScoreDataViewModel> userList = new List<PlayerScoreDataViewModel>();
            using (WebAppContext db = new WebAppContext())
            {
                List<ConnectedUser> users = db.ConnectedUsers.Where(x => x.GameID == int.Parse(groupID)).ToList();

                foreach (ConnectedUser user in users)
                {
                    User userFound = db.Users.Where(x => x.ID == user.UserID).FirstOrDefault();

                    if (userFound != null)
                    {
                        List<CardHand> cards = db.CardHands.Where(x => x.OwnerID == user.UserID && !x.OwnerIsDealer && x.GroupID == int.Parse(groupID)).ToList();
                        
                        if (cards != null)
                        {
                            List<DeckCards> cardList = new List<DeckCards>();

                            foreach (CardHand cardHand in cards)
                            {
                                DeckCards foundCard = db.DeckCards.Where(x => x.ID == cardHand.CardID)
                                    .FirstOrDefault();
                                cardList.Add(new DeckCards()
                                {
                                    Symbol = foundCard.Symbol,
                                    Rank = foundCard.Rank,
                                });
                            }
                            userList.Add(new PlayerScoreDataViewModel()
                            {
                                Name = userFound.Username,
                                UserID = userFound.ID,
                                Cards = cardList,
                                GameScore = (int)user.GameScore,
                                HandScore = (int)user.HandScore,
                            }
                            );
                        }
                    }
                }
            }

            List<PlayerScoreDataViewModel> sortedUserList = userList.OrderByDescending(x => x.GameScore).ToList();
            
            JsonSerializerSettings serializerSettings = new JsonSerializerSettings();

            serializerSettings.StringEscapeHandling = StringEscapeHandling.EscapeHtml;

            string json = JsonConvert.SerializeObject(sortedUserList, serializerSettings);

            await Clients.Group(groupID).SendAsync("playerPointsList", json);
        }


        public string EncodeString(string str)
        {
            return System.Web.HttpUtility.HtmlEncode(str);
        }
    }
}



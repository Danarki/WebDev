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
        // init for the game, gets called by every user and adds them to the group, only the call from the original owner hands out cards etc
        public void StartRound(string GameID, string UserID)
        {
            Groups.AddToGroupAsync(Context.ConnectionId, GameID);

            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser leadUser = db.ConnectedUsers.Where(x => x.GameID == int.Parse(GameID)).FirstOrDefault();

                // lead user was found and is user that made the call.
                if (leadUser != null && leadUser.UserID == int.Parse(UserID))
                {
                    // end round serves as a cleaner and enabler for the players
                    EndRound(int.Parse(GameID), true);
                 
                    // insert a new deck
                    DeckCards d = new DeckCards();
                    d.InsertDeck(int.Parse(GameID));

                    CreateGameDealer(GameID);

                    GiveDealerCard(GameID);

                    // gives all players 2 cards
                    GiveAllPlayersBeginningCards(GameID);

                    // sends all current scores and names to all players
                    sendPlayerScores(GameID);
                }
            }
        }

        // used every new round, get called internally
        public void NewRound(string GameID)
        {
            // end round serves as a cleaner and enabler for the players
            EndRound(int.Parse(GameID), false);

            // send new round signal
            newRoundStart(GameID);

            GiveDealerCard(GameID);

            GiveAllPlayersBeginningCards(GameID);

            sendPlayerScores(GameID);
        }

        // gets all players and gives them 2 cards of the top of the deck
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

        // gives a player a card 
        public void GivePlayerCard(string gameID, int playerID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser user = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID) && x.UserID == playerID).FirstOrDefault();

                // checks if user has been found, if disabled flag is filled and not true
                if (user != null && user.IsDisabled != null && (bool)user.IsDisabled)
                {
                    return;
                }

                // get first card from the deck that is not in use
                DeckCards c = db.DeckCards.Where(x => x.GameID == int.Parse(gameID) && !x.InUse).FirstOrDefault();

                // checks if card is found
                if (c == null)
                {
                    // inserts new deck
                    DeckCards d = new DeckCards();
                    d.InsertDeck(int.Parse(gameID));

                    // set new card
                    c = db.DeckCards.Where(x => x.GameID == int.Parse(gameID) && !x.InUse).FirstOrDefault();
                }

                c.InUse = true;
                db.SaveChanges();

                // add card to hand of player
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

                // get all cards in hand currently
                foreach (CardHand hand in dh)
                {
                    DeckCards card = db.DeckCards.Where(x => x.ID == hand.CardID && x.GameID == int.Parse(gameID)).FirstOrDefault();

                    if (card != null)
                    {
                        cards.Add(card);
                    }
                }

                // send the newly acquired card to the player
                SendPlayerCard(playerID, c, gameID, cards.Count);

                // count hand score
                countScore(cards, playerID, gameID);

                sendPlayerScores(gameID);
            }
        }

        // same process as function above, but then for the dealer
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

        // function for checking if card hand is blackjack
        public bool CheckHasBlackjack(bool HasAce, int cardCount, int score)
        {
            return (HasAce && cardCount == 2 && score == 21);
        }

        // assess points for players against the dealer which has three possibilities
        // Dealer has Blackjack
        // Dealer busted
        // Dealer has score, but not over 21
        public void AssessPoints(string gameID)
        {
            using (WebAppContext db = new WebAppContext())
            {
                Dealer dealer = db.Dealers.Where(x => x.GameID == int.Parse(gameID)).FirstOrDefault();
                List<CardHand> dealerCards = db.CardHands.Where(x => x.OwnerID == dealer.ID && x.OwnerIsDealer).ToList();
                if (dealer != null && dealerCards != null)
                {
                    // dealer has blackjack, all players without blackjack lose
                    if (CheckHasBlackjack((bool)dealer.HasAce, dealerCards.Count, (int)dealer.HandScore))
                    {
                        List<ConnectedUser> userList = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID)).ToList(); 

                        foreach (ConnectedUser user in userList)
                        {
                            List<CardHand> userCards = db.CardHands.Where(x => x.OwnerID == user.UserID && !x.OwnerIsDealer).ToList();
                            if (!CheckHasBlackjack((bool)user.HasAce, userCards.Count, (int)user.HandScore))
                            {
                                user.GameScore--;
                                db.SaveChanges();
                            }
                        }
                    }
                    // dealer does not have blackjack
                    else
                    {
                        int dealerScore = (int)dealer.HandScore;

                        // dealer has ace and score above 21. Ace now gets used as a 1 instead of 11
                        if ((bool)dealer.HasAce && dealerScore >= 22 && dealerScore <= 31) 
                        {
                            dealerScore -= 10;
                        }

                        // Dealer busted, all players get points when not busted
                        if (dealerScore > 21) 
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

                                // winning user has blackjack
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
                        }
                        // dealer has greater than 16 but not bust
                        else 
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

                                    // player uses ace as 1 if score is greater than 21
                                    if ((bool)user.HasAce && userScore >= 22 && userScore <= 31) 
                                    {
                                        userScore -= 10;
                                    }

                                    // user scored higher than dealer
                                    if (userScore > dealerScore && userScore <= 21)
                                    {
                                        user.GameScore++;
                                    }

                                    // user scored lower then dealer or went bust
                                    if (userScore < dealerScore || userScore >= 22)
                                    {
                                        user.GameScore--;
                                    }
                                }
                            }

                            db.SaveChanges();
                        }
                    }
                }
            }
        }

        // disable a player within a group
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

                    // check if all users are disabled
                    foreach (ConnectedUser user in allConnectedUsers)
                    {
                        if (user.IsDisabled != null && !(bool)user.IsDisabled)
                        {
                            userNotDisabled = false;
                        }
                    }

                    // all users are disabled, end the round
                    if (userNotDisabled)
                    {
                        // send final scores
                        sendPlayerScores(gameID);

                        // give give dealer cards until hand is finished
                        GiveDealerCard(gameID);

                        // assess player scores
                        AssessPoints(gameID);

                        // wait 2.5 seconds, so next round does not start immediately
                        Thread.Sleep(2500);

                        // start new round
                        NewRound(gameID);
                    }
                }
            }
        }

        // count the score of the player with cards
        public void countScore(List<DeckCards> cards, int? playerID = null, string? gameID = null, bool isDealer = false)
        {
            int score = 0;
            int score2 = 0; // only used when player has an Ace
            bool AceFound = false;
            bool SymbolFound = false;

            // go through all cards and set flags accordingly
            foreach (DeckCards card in cards)
            {
                // Ace found
                if (card.Rank == "A")
                {
                    score += 11;
                    score2 += 1;
                    AceFound = true;
                }
                // Picture found
                else if (card.Rank == "K" || card.Rank == "Q" || card.Rank == "J")
                {
                    score += 10;
                    score2 += 10;
                    SymbolFound = true;
                }
                // Normal card found
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

                    // check if both flags are set with 2 cards, blackjack
                    if (SymbolFound && AceFound && cards.Count == 2)
                    {
                        SendDealerScore("Blackjack", gameID);

                        dealer.HasAce = true;
                        dealer.HandScore = 21;

                        db.SaveChanges();

                        return;
                    }

                    // score is below 22, not bust
                    if (score <= 21)
                    {
                        SendDealerScore(score.ToString(), gameID);
                        dealer.HandScore = score;
                    }
                    // ace is found
                    else if (AceFound)
                    {
                        dealer.HasAce = true;

                        // score is below 22
                        if (score <= 21)
                        {
                            SendDealerScore(score2 + "/" + score, gameID);
                            dealer.HandScore = score;
                        }
                        // only score where ace counts as 1 is below 22
                        else if (score2 <= 21)
                        {
                            SendDealerScore(score2.ToString(), gameID);
                            dealer.HandScore = score;
                        }
                        // both scores are above 21, bust
                        else
                        {
                            SendDealerScore("Bust", gameID);
                            dealer.HandScore = 22;
                        }
                    }
                    // no Ace found, score too high, bust
                    else
                    {
                        SendDealerScore("Bust", gameID);
                        dealer.HandScore = 22;
                    }

                    db.SaveChanges();

                    // used for getting highest score
                    int totalScore = score;

                    // score is too high along with Ace in hand
                    if (score > 21 && AceFound)
                    {
                        totalScore -= 10;
                    }

                    // More than 1 card is in hand
                    if (cards.Count > 1)
                    {
                        // total score is not high enough, must draw
                        if (totalScore < 16)
                        {
                            Thread.Sleep(1000); //small delay for realism

                            GiveDealerCard(gameID); // give dealer another card repeating the process until score is high enough
                        }
                    }
                }
            }
            // Same process as above, but then for user
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

                        // either score is 21, also disable the player, turn is finished
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

                    // player has score of 21, turn is over
                    if (score == 21)
                    {
                        user.HandScore = 21;
                        db.SaveChanges();

                        SendPlayerScore(user.UserID, "21", gameID);
                        DisablePlayer((int)playerID, gameID);

                        return;
                    }

                    user.HandScore = score;
                    db.SaveChanges();

                    SendPlayerScore(user.UserID, score.ToString(), gameID);
                }
            }
        }

        // player leaves, uses AuthToken as authenticator
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

        // player ends own turn, uses AuthToken as authenticator
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

        // player takes another card, uses AuthToken as authenticator
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

        // ends current round, also used for initial cleaner
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

        // admin or moderator ends the round by force, uses AuthToken as authenticator
        public void ForceEnd(string gameID, string userID, string AuthToken)
        {
            using (WebAppContext db = new WebAppContext())
            {
                ConnectedUser connectedUser = db.ConnectedUsers.Where(x =>
                    x.GameID == int.Parse(gameID) &&
                    x.UserID == int.Parse(userID) &&
                    x.AuthToken == AuthToken &&
                    !(bool)x.IsDisabled).FirstOrDefault();

                if (connectedUser == null)
                {
                    return;
                }

                List<ConnectedUser> users = db.ConnectedUsers.Where(x => x.GameID == int.Parse(gameID)).ToList();

                foreach (ConnectedUser user in users)
                {
                    user.IsDisabled = true;
                }

                db.SaveChanges();

                sendPlayerScores(gameID);
                GiveDealerCard(gameID);
                AssessPoints(gameID);
                Thread.Sleep(2500);
                NewRound(gameID);
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

        // gather all scores of players and send it to every user in group in correct format
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



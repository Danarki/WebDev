namespace WebDev.Models
{
    public class DeckCards
    {
        public int ID { get; set; }
        public int GameID { get; set; }
        public string Symbol { get; set; }
        public string Rank { get; set; }
        public bool InUse { get; set; }

        public void InsertDeck(int GameID)
        {
            List<Card> deck = GenerateDeck();
            deck = ShuffleDeck(deck);

            foreach (Card card in deck)
            {
                DeckCards deckCard = new DeckCards
                {
                    GameID = GameID,
                    Symbol = card.Symbol,
                    Rank = card.Rank,
                    InUse = false
                };

                deckCard.Insert();
            }
        }

        public void Insert()
        {
            using (var db = new WebAppContext())
            {
                db.DeckCards.Add(this);
                db.SaveChanges();
            }
        }

        public List<Card> ShuffleDeck(List<Card> deck)
        {
            Random rand = new Random();
            int length = deck.Count;
            while (length > 1)
            {
                length--;
                int first = rand.Next(length + 1);
                (deck[first], deck[length]) = (deck[length], deck[first]);
            }

            return deck;
        }

        public List<Card> GenerateDeck()
        {
            List<Card> deck = new List<Card>();
            foreach (var symbol in (Symbol[])Enum.GetValues(typeof(Symbol)))
            {
                foreach (var rank in (Rank[])Enum.GetValues(typeof(Rank)))
                {
                    Card card = new Card();

                    card.Rank = rank.ToString().Substring(1);
                    card.Symbol = symbol.ToString();
                    card.Value = (int)rank;
                    deck.Add(card);
                }
            }

            return deck;
        }
    }
}

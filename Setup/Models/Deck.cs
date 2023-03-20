namespace WebDev.Models
{
    public class Deck
    {
        public List<Card> DeckList { get; set; }

        public Deck()
        {
            GenerateDeck();
            ShuffleDeck();
        }

        public void ShuffleDeck()
        {
            Random rand = new Random();
            int length = DeckList.Count;
            while (length > 1)
            {
                length--;
                int first = rand.Next(length + 1);
                (DeckList[first], DeckList[length]) = (DeckList[length], DeckList[first]);
            }
        }
        public void GenerateDeck()
        {
            DeckList = new List<Card>();
            foreach (var symbol in (Symbol[])Enum.GetValues(typeof(Symbol)))
            {
                foreach (var rank in (Rank[])Enum.GetValues(typeof(Rank)))
                {
                    Card card = new Card();

                    card.Rank = rank.ToString().Substring(1);
                    card.Symbol = symbol.ToString();
                    card.Value = (int)rank;
                    DeckList.Add(card);
                }
            }
        }
    }
}

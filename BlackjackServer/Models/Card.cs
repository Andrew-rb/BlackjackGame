namespace BlackjackServer.Models
{
    public class Card
    {
        public string Suit { get; private set; }
        public string Rank { get; private set; }

        public Card(string suit, string rank)
        {
            Suit = suit;
            Rank = rank;
        }

        public int GetValue()
        {
            if (Rank == "A") return 11;
            if (Rank == "K" || Rank == "Q" || Rank == "J") return 10;
            return int.Parse(Rank);
        }

        public override string ToString() => $"{Rank}{Suit[0]}";
    }
}
namespace BlackjackServer.Models
{
    public class Hand
    {
        public List<Card> Cards { get; private set; } = new List<Card>();
        public int Bet { get; set; }
        public bool IsStand { get; set; }
        public bool IsBusted => GetValue() > 21;

        public void AddCard(Card card) => Cards.Add(card);

        public int GetValue()
        {
            int sum = Cards.Sum(c => c.GetValue());
            int aceCount = Cards.Count(c => c.Rank == "A");
            while (sum > 21 && aceCount > 0)
            {
                sum -= 10;
                aceCount--;
            }
            return sum;
        }

        public bool IsBlackjack() => Cards.Count == 2 && GetValue() == 21;

        public void Clear()
        {
            Cards.Clear();
            IsStand = false;
            Bet = 0;
        }
    }
}
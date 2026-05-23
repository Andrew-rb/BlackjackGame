using BlackjackServer.Models;

namespace BlackjackServer.GameLogic
{
    public class Deck
    {
        private List<Card> cards = new List<Card>();
        private Random rng = new Random();

        public Deck()
        {
            string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K", "A" };
            foreach (var suit in suits)
                foreach (var rank in ranks)
                    cards.Add(new Card(suit, rank));
        }

        public void Shuffle()
        {
            int n = cards.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Card temp = cards[k];
                cards[k] = cards[n];
                cards[n] = temp;
            }
        }

        public Card DrawCard()
        {
            if (cards.Count == 0) throw new Exception("Колода пуста");
            Card top = cards[0];
            cards.RemoveAt(0);
            return top;
        }
    }
}
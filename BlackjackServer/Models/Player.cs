namespace BlackjackServer.Models
{
    public class Player
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string IpAddress { get; set; }
        public int Balance { get; set; }
        public List<Hand> Hands { get; set; }
        public int CurrentHandIndex { get; set; }

        public Player(string id, string name, string ipAddress = "")
        {
            Id = id;
            Name = name;
            IpAddress = ipAddress;
            Balance = 1000;
            Hands = new List<Hand> { new Hand() };
            CurrentHandIndex = 0;
        }

        public Hand CurrentHand => Hands[CurrentHandIndex];

        public void ResetForNewRound()
        {
            Hands.Clear();
            Hands.Add(new Hand());
            CurrentHandIndex = 0;
        }
    }
}
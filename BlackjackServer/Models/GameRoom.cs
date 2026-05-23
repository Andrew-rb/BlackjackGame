using BlackjackServer.GameLogic;
using BlackjackServer.Rooms;
using BlackjackServer.Networking;

namespace BlackjackServer.Models
{
    public enum RoomState { Waiting, Betting, Dealing, PlayerTurn, DealerTurn, RoundEnd }

    public class GameRoom
    {
        public string Id { get; private set; }
        public List<Player> Players { get; private set; } = new List<Player>();
        public RoomState State { get; private set; } = RoomState.Waiting;
        public Deck Deck { get; private set; }
        public Hand DealerHand { get; private set; } = new Hand();

        private int currentPlayerIdx = 0;
        private readonly object roomLock = new object();

        public GameRoom(string id)
        {
            Id = id;
            Deck = new Deck();
            DealerHand.Clear();
        }

        public bool AddPlayer(Player player)
        {
            lock (roomLock)
            {
                if (Players.Count >= 6) return false;
                if (Players.Any(p => p.Name == player.Name)) return false;
                Players.Add(player);
                Logger.Log($"Игрок {player.Name} вошёл в комнату {Id}");
                BroadcastState();
                return true;
            }
        }

        public void RemovePlayer(string playerId)
        {
            lock (roomLock)
            {
                var player = Players.FirstOrDefault(p => p.Id == playerId);
                if (player != null)
                {
                    Players.Remove(player);
                    Logger.Log($"Игрок {player.Name} покинул комнату {Id}");

                    if (State == RoomState.Betting && Players.Count > 0 && Players.All(p => p.CurrentHand.Bet > 0))
                    {
                        DealCards();
                    }
                    else if (State == RoomState.PlayerTurn && currentPlayerIdx > 0 && currentPlayerIdx <= Players.Count)
                    {
                        int removedIndex = Players.FindIndex(p => p.Id == playerId);
                        if (removedIndex <= currentPlayerIdx && currentPlayerIdx > 0)
                            currentPlayerIdx--;
                        NextPlayerTurn();
                    }

                    BroadcastState();
                }
                if (Players.Count == 0 && RoomManager.RoomExists(Id))
                    RoomManager.RemoveRoom(Id);
            }
        }

        public void StartGame()
        {
            lock (roomLock)
            {
                if (State != RoomState.Waiting && State != RoomState.RoundEnd)
                {
                    Broadcast("ERROR|Недостаточно игроков для старта (нужно хотя бы 1)");
                    return;
                }
                if (State == RoomState.RoundEnd)
                {
                    ResetRoom();
                }
                State = RoomState.Betting;
                Logger.Log($"Комната {Id}: начало раунда, запрос ставок");
                BroadcastChat("Игра начинается! Делайте ставки.");
                foreach (var p in Players)
                {
                    p.ResetForNewRound();
                    SendToPlayer(p, "BET_REQUEST|Сделайте вашу ставку");
                }
                BroadcastState();
            }
        }

        public void PlaceBet(Player player, int amount)
        {
            lock (roomLock)
            {
                if (State != RoomState.Betting) { SendError(player, "Ставки не принимаются сейчас"); return; }
                if (amount < 10 || amount > player.Balance) { SendError(player, "Неверная сумма ставки"); return; }

                player.CurrentHand.Bet = amount;
                player.Balance -= amount;
                SendToPlayer(player, $"BET_ACCEPTED|{amount}");
                SendToPlayer(player, $"NEW_BALANCE|{player.Balance}");
                BroadcastChat($"Игрок {player.Name} сделал ставку {amount}.");

                if (Players.All(p => p.CurrentHand.Bet > 0))
                {
                    DealCards();
                }
            }
        }

        private void DealCards()
        {
            State = RoomState.Dealing;
            Deck = new Deck();
            Deck.Shuffle();

            foreach (var p in Players)
            {
                p.CurrentHand.AddCard(Deck.DrawCard());
                p.CurrentHand.AddCard(Deck.DrawCard());
                SendToPlayer(p, $"YOUR_CARDS|{SerializeHand(p.CurrentHand)}");
            }

            DealerHand.Clear();
            DealerHand.AddCard(Deck.DrawCard());
            DealerHand.AddCard(Deck.DrawCard());
            BroadcastDealerCard(DealerHand.Cards[0]);

            BroadcastState();

            State = RoomState.PlayerTurn;
            currentPlayerIdx = 0;
            NextPlayerTurn();
        }

        private void NextPlayerTurn()
        {
            while (currentPlayerIdx < Players.Count)
            {
                var player = Players[currentPlayerIdx];
                if (player == null)
                {
                    currentPlayerIdx++;
                    continue;
                }

                while (player.CurrentHandIndex < player.Hands.Count)
                {
                    if (player.CurrentHand.IsStand || player.CurrentHand.IsBusted)
                    {
                        player.CurrentHandIndex++;
                        continue;
                    }

                    var hand = player.CurrentHand;
                    bool canDouble = hand.Cards.Count == 2 && (player.Balance >= hand.Bet);
                    bool canSplit = hand.Cards.Count == 2 &&
                                  hand.Cards[0].GetValue() == hand.Cards[1].GetValue() &&
                                  player.Balance >= hand.Bet;

                    BroadcastChat($"Ход игрока {player.Name}.");

                    SendToPlayer(player, $"YOUR_TURN|{SerializeHand(hand)}|{canDouble}|{canSplit}");
                    return;
                }
                currentPlayerIdx++;
            }
            DealerTurn();
        }

        public void PlayerHit(Player player)
        {
            lock (roomLock)
            {
                if (State != RoomState.PlayerTurn || player == null || player != Players[currentPlayerIdx])
                    return;
                if (State != RoomState.PlayerTurn || player != Players[currentPlayerIdx]) return;
                var hand = player.CurrentHand;
                if (hand.IsStand || hand.IsBusted) return;

                hand.AddCard(Deck.DrawCard());
                SendToPlayer(player, $"CARD_ADDED|{SerializeHand(hand)}");
                if (hand.IsBusted)
                {
                    SendToPlayer(player, "BUST|Вы перебрали!");
                    player.CurrentHandIndex++;
                }
                else
                {
                    bool canDouble = false;
                    bool canSplit = false;

                    SendToPlayer(player,
                        $"YOUR_TURN|{SerializeHand(hand)}|{canDouble}|{canSplit}");
                    return;
                }

                if (player.CurrentHandIndex >= player.Hands.Count)
                    currentPlayerIdx++;
                NextPlayerTurn();
            }
        }

        public void PlayerStand(Player player)
        {
            lock (roomLock)
            {
                if (State != RoomState.PlayerTurn || player == null || player != Players[currentPlayerIdx])
                    return;
                if (State != RoomState.PlayerTurn || player != Players[currentPlayerIdx]) return;
                var hand = player.CurrentHand;
                hand.IsStand = true;
                SendToPlayer(player, "STAND_ACCEPTED");
                player.CurrentHandIndex++;
                if (player.CurrentHandIndex >= player.Hands.Count)
                    currentPlayerIdx++;
                NextPlayerTurn();
            }
        }

        public void PlayerDouble(Player player)
        {
            lock (roomLock)
            {
                if (State != RoomState.PlayerTurn || player == null || player != Players[currentPlayerIdx])
                    return;
                if (State != RoomState.PlayerTurn || player != Players[currentPlayerIdx]) return;
                var hand = player.CurrentHand;
                if (hand.Cards.Count != 2) { SendError(player, "Double доступен только с двумя картами"); return; }
                int doubleBet = hand.Bet * 2;
                if (doubleBet > player.Balance + hand.Bet) { SendError(player, "Недостаточно средств"); return; }

                player.Balance -= hand.Bet;
                hand.Bet = doubleBet;
                hand.AddCard(Deck.DrawCard());
                SendToPlayer(player, $"DOUBLE_CARD|{SerializeHand(hand)}");
                hand.IsStand = true;
                player.CurrentHandIndex++;
                if (player.CurrentHandIndex >= player.Hands.Count)
                    currentPlayerIdx++;
                NextPlayerTurn();
            }
        }

        public void PlayerSplit(Player player)
        {
            lock (roomLock)
            {
                if (State != RoomState.PlayerTurn || player == null || player != Players[currentPlayerIdx])
                    return;
                if (State != RoomState.PlayerTurn || player != Players[currentPlayerIdx]) return;
                var hand = player.CurrentHand;
                if (hand.Cards.Count != 2 || hand.Cards[0].GetValue() != hand.Cards[1].GetValue())
                { SendError(player, "Сплит возможен только для пары одинаковых карт"); return; }
                if (player.Balance < hand.Bet) { SendError(player, "Недостаточно средств для сплита"); return; }

                player.Balance -= hand.Bet;
                Card card1 = hand.Cards[0];
                Card card2 = hand.Cards[1];
                Hand hand2 = new Hand { Bet = hand.Bet };
                hand.Clear();
                hand.AddCard(card1);
                hand2.AddCard(card2);
                hand.AddCard(Deck.DrawCard());
                hand2.AddCard(Deck.DrawCard());
                player.Hands = new List<Hand> { hand, hand2 };
                player.CurrentHandIndex = 0;
                SendToPlayer(player, $"SPLIT_DONE|{SerializeHand(hand)}|{SerializeHand(hand2)}");
                SendToPlayer(player, "YOUR_TURN|" + SerializeHand(hand));
            }
        }

        private void DealerTurn()
        {
            State = RoomState.DealerTurn;
            BroadcastChat("Ход дилера.");
            Broadcast("DEALER_TURN_START");
            BroadcastDealerCard(DealerHand.Cards[1]);

            while (DealerHand.GetValue() < 17)
            {
                DealerHand.AddCard(Deck.DrawCard());
                BroadcastDealerCard(DealerHand.Cards.Last());
            }
            int dealerVal = DealerHand.GetValue();
            bool dealerBlackjack = DealerHand.IsBlackjack();

            Dictionary<Player, int> oldBalances = new Dictionary<Player, int>();
            foreach (var p in Players)
                oldBalances[p] = p.Balance;

            foreach (var p in Players)
            {
                foreach (var hand in p.Hands)
                {
                    int playerVal = hand.GetValue();
                    bool playerBlackjack = hand.IsBlackjack();
                    int payout = 0;

                    if (hand.IsBusted)
                    {
                        payout = 0;
                    }
                    else if (playerBlackjack && !dealerBlackjack)
                    {
                        payout = (int)(hand.Bet * 2.5);
                    }
                    else if (dealerBlackjack && !playerBlackjack)
                    {
                        payout = 0;
                    }
                    else if (dealerVal > 21)
                    {
                        payout = hand.Bet * 2;
                    }
                    else if (playerVal > dealerVal)
                    {
                        payout = hand.Bet * 2;
                    }
                    else if (playerVal == dealerVal)
                    {
                        payout = hand.Bet;
                    }
                    else
                    {
                        payout = 0;
                    }

                    p.Balance += payout;
                    SendToPlayer(p, $"ROUND_RESULT|{playerVal}|{dealerVal}|{payout}|{(playerBlackjack ? "BJ" : "")}");
                }
                SendToPlayer(p, $"NEW_BALANCE|{p.Balance}");
            }

            foreach (var p in Players)
            {
                int oldBalance = oldBalances[p];
                int change = p.Balance - oldBalance;
                string resultMsg;
                if (change > 0)
                    resultMsg = $"выиграл {change} фишек";
                else if (change < 0)
                    resultMsg = $"проиграл {-change} фишек";
                else
                    resultMsg = "сыграл в ничью";

                BroadcastChat($"Игрок {p.Name} {resultMsg}. Баланс: {p.Balance}");
            }

            State = RoomState.RoundEnd;
            BroadcastState();       
        }

        private void ResetRoom()
        {
            lock (roomLock)
            {
                foreach (var p in Players) p.ResetForNewRound();
                DealerHand.Clear();
                State = RoomState.Waiting;
                Logger.Log($"Комната {Id} готова к новой игре");
                Broadcast("ROOM_RESET|Можно начинать новую игру (/start)");
                BroadcastState();
            }
        }

        public void SendChat(Player sender, string message)
        {
            string chatMsg = $"[{sender.Name}] {message}";
            Broadcast($"CHAT|{chatMsg}");
            Logger.Log($"Чат в {Id}: {chatMsg}");
        }

        private void Broadcast(string data) => BroadcastToAll(data);
        private void BroadcastState() => Broadcast($"STATE|{SerializeRoomState()}");
        private void BroadcastDealerCard(Card card) => Broadcast($"DEALER_CARD|{card}");
        private void SendToPlayer(Player p, string data) => TcpServer.SendToPlayer(p.Id, data);
        private void SendError(Player p, string msg) => SendToPlayer(p, $"ERROR|{msg}");

        private string SerializeHand(Hand h) => string.Join(",", h.Cards);
        private string SerializePlayerHands(Player player)
        {
            var handsInfo = new List<string>();

            foreach (var hand in player.Hands)
            {
                string cards = string.Join(",", hand.Cards);
                int value = hand.GetValue();

                handsInfo.Add($"{cards}:{value}");
            }

            return string.Join("~", handsInfo);
        }

        private string SerializeRoomState()
        {
            var plist = new List<string>();
            foreach (var p in Players)
            {
                string handsStr = SerializePlayerHands(p);
                plist.Add($"{p.Name}:{p.Balance}:{handsStr}");
            }
            string dealerCards = "";

            if (DealerHand.Cards.Count > 0)
            {
                dealerCards = string.Join(",", DealerHand.Cards.Select(c => c.ToString()));
            }
            string currentPlayerName = "";
            if (State == RoomState.PlayerTurn && currentPlayerIdx < Players.Count)
                currentPlayerName = Players[currentPlayerIdx].Name;

            return $"room={Id}|state={State}|players={string.Join(";", plist)}|dealer={dealerCards}|currentPlayer={currentPlayerName}";
        }

        private void BroadcastToAll(string data)
        {
            foreach (var p in Players)
                TcpServer.SendToPlayer(p.Id, data);
        }

        private void BroadcastChat(string message)
        {
            Broadcast($"CHAT|{message}");
        }
    }
}
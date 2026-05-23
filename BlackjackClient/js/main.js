document.addEventListener('DOMContentLoaded', async () => {
    const WS_URL = `ws://${window.location.hostname}:8888`;
    let playerId = null;
    let playerName = null;
    let currentBalance = 0;

    Chat.init((msg) => WS.send(msg));

    WS.setHandler(async (rawMessage) => {
        const parts = rawMessage.split('|');
        const cmd = parts[0];
        const args = parts.slice(1);

        switch (cmd) {
            case 'JOIN_OK':
                playerId = args[0];
                playerName = args[1];
                sessionStorage.setItem('playerName', playerName);
                Chat.systemMessage(`Добро пожаловать, ${playerName}! Баланс: 1000 фишек. Нажмите START для игры.`);
                document.getElementById('playerName').innerText = playerName;
                StateMachine.setGameState('WAITING');
                break;

            case 'BET_REQUEST':
                Chat.systemMessage('Сделайте ставку (минимальная 10)');
                StateMachine.setGameState('BETTING');
                break;

            case 'BET_ACCEPTED':

                currentBet = parseInt(args[0]);

                Chat.systemMessage(
                    `Ставка ${currentBet} принята. Ожидаем раздачу...`
                );

                break;

            case 'YOUR_CARDS':

                Renderer.updatePlayerHandsFromSerialized(
                    `${args[0]}:${calculateHandValue(args[0])}`
                );

                break;

            case 'YOUR_TURN':
                StateMachine.setGameState('PLAYERTURN');
                const canDouble = args[1] === 'True' || args[1] === 'true';
                const canSplit = args[2] === 'True' || args[2] === 'true';
                StateMachine.setTurn(true, canSplit, canDouble);
                Chat.systemMessage('Ваш ход!' +
                    (canDouble ? ' (доступен Double)' : '') +
                    (canSplit ? ' (доступен Split)' : ''));
                break;

            case 'CARD_ADDED':

                Chat.systemMessage('Вы взяли карту');

                Renderer.updatePlayerHandsFromSerialized(
                    `${args[0]}:${calculateHandValue(args[0])}`
                );

                break;

            case 'STAND_ACCEPTED':
                Chat.systemMessage('Вы остановились');
                break;

            case 'DOUBLE_CARD':

                Chat.systemMessage('Удвоение! Вы получили одну карту.');

                Renderer.updatePlayerHandsFromSerialized(
                    `${args[0]}:${calculateHandValue(args[0])}`
                );

                break;

            case 'SPLIT_DONE':

                Chat.systemMessage('Сплит выполнен! Играйте две руки.');

                const hand1 = `${args[0]}:${calculateHandValue(args[0])}`;
                const hand2 = `${args[1]}:${calculateHandValue(args[1])}`;

                Renderer.updatePlayerHandsFromSerialized(
                    `${hand1}~${hand2}`
                );

                break;

            case 'BUST':
                Chat.systemMessage(`Перебор! ${args[0]}`);
                break;

            case 'DEALER_CARD':
                if (Renderer.getDealerCardsCount() === 0) {
                    Renderer.initDealerCards(args[0]);
                } else {
                    Renderer.addDealerCard(args[0]);
                }
                break;

            case 'DEALER_TURN_START':
                Chat.systemMessage('Дилер начинает свой ход...');
                break;

            case 'ROUND_RESULT':
                const playerVal = args[0];
                const dealerVal = args[1];
                const payout = parseInt(args[2]);
                let resultMsg = `Ваши очки: ${playerVal}, у дилера: ${dealerVal}. `;
                if (payout === 0) {

                    resultMsg += 'Вы проиграли.';
                }
                else if (payout === currentBet) {

                    resultMsg += 'Ничья, ставка возвращена.';
                }
                else if (payout > currentBet) {

                    resultMsg += `Вы выиграли ${payout - currentBet} фишек!`;
                }
                if (args[3] === 'BJ') resultMsg += ' (Блэкджек!)';
                Chat.systemMessage(resultMsg);
                Renderer.updateDealerScore(dealerVal);
                break;

            case 'NEW_BALANCE':
                currentBalance = parseInt(args[0]);
                Renderer.updateBalance(currentBalance);
                break;

            case 'STATE':
                console.log('RAW STATE', rawMessage);
                try {
                    const stateRaw = args.join('|');
                    const stateData = parseStateMessage(stateRaw);
                    console.log('PARSED STATE', stateData);
                    Renderer.updateFullState(stateData, playerName);
                    if (stateData.state === 'PlayerTurn') {
                        StateMachine.setGameState('PLAYERTURN');
                    } else if (stateData.state === 'Waiting') {
                        StateMachine.setGameState('WAITING');
                        Renderer.resetDealerCards();
                        Renderer.updateDealerScore(null);
                    } else if (stateData.state === 'Betting') {
                        StateMachine.setGameState('BETTING');
                    } else if (stateData.state === 'RoundEnd') {
                        StateMachine.setGameState('ROUNDEND');
                    }
                } catch (e) {
                    console.error('STATE PARSE ERROR', e);
                }
                break;

            case 'ROOM_RESET':
                Chat.systemMessage('Комната готова к новой игре. Нажмите START');
                StateMachine.resetForNewRound();
                Renderer.resetDealerCards();
                Renderer.updateDealerScore(null);
                break;

            case 'CHAT':
                Chat.addMessage(args[0], false);
                break;

            case 'ERROR':
                Chat.systemMessage(`Ошибка: ${args.join('|')}`);
                break;

            default:
                console.log('Неизвестная команда:', cmd, args);
        }
    });

    function calculateHandValue(cardsStr) {

        if (!cardsStr) return 0;

        const cards = cardsStr.split(',');

        let sum = 0;
        let aces = 0;

        cards.forEach(card => {

            const rank = card.slice(0, -1);

            if (rank === 'A') {
                sum += 11;
                aces++;
            }
            else if (['K', 'Q', 'J'].includes(rank)) {
                sum += 10;
            }
            else {
                sum += parseInt(rank);
            }
        });

        while (sum > 21 && aces > 0) {
            sum -= 10;
            aces--;
        }

        return sum;
    }
    function parseStateMessage(stateStr) {

        const obj = {
            players: [],
            currentPlayer: ''
        };

        const parts = stateStr.split('|');

        for (let p of parts) {

            if (p.startsWith('room=')) {

                obj.room = p.substring(5);
            }

            else if (p.startsWith('state=')) {

                obj.state = p.substring(6);
            }

            else if (p.startsWith('dealer=')) {

                obj.dealer = p.substring(7);
            }

            else if (p.startsWith('currentPlayer=')) {

                obj.currentPlayer = p.substring(14);
            }

            else if (p.startsWith('players=')) {

                const playersRaw = p.substring(8);

                if (!playersRaw)
                    continue;

                const playersList =
                    playersRaw.split(';');

                for (let pl of playersList) {

                    const parts = pl.split(':');

                    if (parts.length < 4)
                        continue;

                    const name = parts[0];
                    const balance = parseInt(parts[1]);

                    const handsStr = parts.slice(2).join(':');

                    obj.players.push({
                        name,
                        balance,
                        handsStr
                    });
                }
            }
        }

        return obj;
    }

    const playerNameInput = prompt('Введите ваше имя:', 'Игрок');
    if (!playerNameInput) {
        alert('Имя обязательно');
        return;
    }
    try {
        await WS.connect(WS_URL);
        WS.send(`JOIN|${playerNameInput}`);
        document.getElementById('connStatus').innerText = 'Подключено';
        document.getElementById('connStatus').style.background = '#2e8b57';
    } catch (e) {
        document.getElementById('connStatus').innerText = 'Ошибка подключения';
        Chat.systemMessage('Не удалось подключиться к серверу. Проверьте, запущен ли сервер на ws://localhost:8888');
        return;
    }

    document.getElementById('btnStart').onclick = () => {
        if (WS.isConnected()) WS.send('START');
    };
    document.getElementById('btnBet').onclick = () => {
        const amount = parseInt(document.getElementById('betAmount').value);
        if (isNaN(amount) || amount < 10) {
            Chat.systemMessage('Ставка должна быть не менее 10');
            return;
        }
        WS.send(`BET|${amount}`);
    };
    document.getElementById('btnHit').onclick = () => WS.send('HIT');
    document.getElementById('btnStand').onclick = () => WS.send('STAND');
    document.getElementById('btnDouble').onclick = () => WS.send('DOUBLE');
    document.getElementById('btnSplit').onclick = () => WS.send('SPLIT');
    document.getElementById('btnLeave').onclick = () => {
        WS.send('LEAVE');
        setTimeout(() => location.reload(), 500);
    };
    const btnNewRound = document.getElementById('btnNewRound');
    if (btnNewRound) {
        btnNewRound.onclick = () => {
            console.log('Нажата кнопка Новый раунд');
            if (WS.isConnected()) {
                WS.send('START');
                console.log('Отправлена команда START для нового раунда');
            } else {
                console.warn('Нет соединения с сервером');
            }
        };
    } else {
        console.warn('Кнопка btnNewRound не найдена в DOM');
    }
});
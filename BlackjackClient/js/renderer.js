const Renderer = (function () {
    let dealerCards = [];

    function suitToSymbol(suit) {
        const map = {
            'Hearts': '♥',
            'Diamonds': '♦',
            'Clubs': '♣',
            'Spades': '♠'
        };
        return map[suit] || '?';
    }

    function updatePlayerHandsFromSerialized(serializedHands) {
        const handsArray = [];
        const splitHands = serializedHands.split('~');

        splitHands.forEach(handPart => {
            const lastColon = handPart.lastIndexOf(':');
            if (lastColon === -1) return;

            const cardsStr = handPart.substring(0, lastColon);
            const valueStr = handPart.substring(lastColon + 1);
            const cards = cardsStr ? cardsStr.split(',') : [];
            const value = parseInt(valueStr) || 0;

            handsArray.push({ cards, value });
        });

        const handsContainer = document.getElementById('playerHands');
        if (handsContainer) {
            handsContainer.innerHTML = renderPlayerHands(handsArray);
        }
    }

    function renderAllPlayers(
        players,
        currentPlayerName,
        currentTurnPlayerName,
        roomState
    ) {
        console.log('=== renderAllPlayers ===');
        console.log(players);
        console.log('currentPlayerName', currentPlayerName);
        console.log('currentTurnPlayerName', currentTurnPlayerName);
        console.log('roomState', roomState);

        const container = document.getElementById('otherPlayersList');
        if (!container) return;

        let html = '';
        const isRoundEnd = (roomState === 'RoundEnd');

        for (let p of players) {
            if (p.name === currentPlayerName) continue;

            const isTurn = p.name === currentTurnPlayerName;
            const playerClass = isTurn ? 'player-card active-turn' : 'player-card';
            let handsHtml = '';
            const handsArray = parseHandsFromString(p.handsStr);

            if (handsArray.length === 0) {
                handsHtml += `
                <div class="hand">
                    <div class="hand-label">Ожидание</div>
                    <div class="hand-cards"></div>
                </div>`;
            }

            for (let hand of handsArray) {
                let cardsHtml = '';
                for (let i = 0; i < hand.cards.length; i++) {
                    if (isRoundEnd) {
                        // В конце раунда – лицом вверх
                        cardsHtml += renderCard(hand.cards[i], false);
                    } else {
                        // Рубашка
                        cardsHtml += '<div class="card back"></div>';
                    }
                }
                handsHtml += `
                <div class="hand">
                    <div class="hand-label">${handsArray.length > 1 ? 'Рука' : 'Карты'}</div>
                    <div class="hand-cards">${cardsHtml}</div>
                </div>`;
            }

            html += `
            <div class="${playerClass}">
                <div class="player-name">${p.name} ${isTurn ? '🎲' : ''}</div>
                <div class="player-balance">💰 ${p.balance}</div>
                <div class="player-hands">${handsHtml}</div>
            </div>`;
        }

        container.innerHTML = html || '<div class="player-card">Нет других игроков</div>';
        console.log('renderAllPlayers: количество других игроков =', players.filter(p => p.name !== currentPlayerName).length);
    }

    function parseHandsFromString(handsStr) {
        console.log('parseHandsFromString:', handsStr);
        const result = [];
        if (!handsStr) return result;
        const parts = handsStr.split('~');
        for (let part of parts) {
            const lastColon = part.lastIndexOf(':');
            if (lastColon === -1) continue;
            const cardsStr = part.substring(0, lastColon);
            const value = parseInt(part.substring(lastColon + 1)) || 0;
            const cards = cardsStr ? cardsStr.split(',') : [];
            result.push({ cards, value });
        }
        return result;
    }

    function renderCard(cardStr, hidden = false) {
        if (hidden || !cardStr) {
            return '<div class="card back"></div>';
        }

        let rank = cardStr.slice(0, -1);
        let suitLetter = cardStr.slice(-1);
        let suitFull = '';
        if (suitLetter === 'H') suitFull = 'Hearts';
        else if (suitLetter === 'D') suitFull = 'Diamonds';
        else if (suitLetter === 'C') suitFull = 'Clubs';
        else if (suitLetter === 'S') suitFull = 'Spades';

        const symbol = suitToSymbol(suitFull);
        return `<div class="card" data-suit="${symbol}">
                    <div class="rank">${rank}</div>
                    <div class="suit">${symbol}</div>
                </div>`;
    }

    function initDealerCards(firstCard) {
        dealerCards = [];
        if (firstCard) {
            dealerCards.push({ card: firstCard, isOpen: true });
        }
        dealerCards.push({ card: null, isOpen: false });
        renderDealer();
        console.log('initDealerCards: первая карта', firstCard);
    }

    function addDealerCard(cardStr) {
        let index = dealerCards.findIndex(c => !c.isOpen);
        if (index !== -1) {
            dealerCards[index] = { card: cardStr, isOpen: true };
        } else {
            dealerCards.push({ card: cardStr, isOpen: true });
        }
        renderDealer();
        const dealerValue = calculateDealerTotal();
        updateDealerScore(dealerValue);
        console.log('addDealerCard:', cardStr, 'новый счёт дилера:', dealerValue);
    }

    function getDealerCardsCount() {
        return dealerCards.length;
    }

    function resetDealerCards() {
        dealerCards = [];
        renderDealer();
        updateDealerScore(null);
        console.log('resetDealerCards: сброшены');
    }

    function renderDealer() {
        console.log('RENDER DEALER', dealerCards);
        const container = document.getElementById('dealerCards');
        if (!container) return;

        if (dealerCards.length === 0) {
            container.innerHTML = '';
            return;
        }

        let html = '';
        for (let item of dealerCards) {
            if (item.isOpen && item.card) {
                html += renderCard(item.card, false);
            } else {
                html += '<div class="card back"></div>';
            }
        }
        container.innerHTML = html;
    }

    function calculateDealerTotal() {
        let total = 0;
        let aces = 0;
        for (let item of dealerCards) {
            if (item.isOpen && item.card) {
                let rank = item.card.slice(0, -1);
                if (rank === 'A') {
                    total += 11;
                    aces++;
                } else if (['K', 'Q', 'J'].includes(rank)) {
                    total += 10;
                } else {
                    total += parseInt(rank);
                }
            }
        }
        while (total > 21 && aces > 0) {
            total -= 10;
            aces--;
        }
        return total;
    }

    function updateDealerScore(score) {
        const scoreEl = document.getElementById('dealerScore');
        if (scoreEl) {
            scoreEl.innerText = `Очки: ${score !== null ? score : '?'}`;
        }
    }

    function renderPlayerHands(hands) {
        if (!hands || hands.length === 0) return '<div class="hand">Нет рук</div>';
        let html = '';
        hands.forEach((hand, idx) => {
            let cards = hand.cards || [];
            let value = hand.value || 0;
            html += `<div class="hand">
                        <div class="hand-label">${hands.length > 1 ? `Рука ${idx + 1}` : 'Ваша рука'}</div>
                        <div class="hand-cards">${cards.map(c => renderCard(c, false)).join('')}</div>
                        <div class="hand-value">Очки: ${value}</div>
                    </div>`;
        });
        return html;
    }

    function updateBalance(balance) {
        const el = document.getElementById('playerBalance');
        if (el) el.innerText = `Баланс: ${balance}`;
    }

    function updateFullState(state, currentPlayerName) {
        console.log('UPDATE FULL STATE', state);
        console.log('CURRENT PLAYER', currentPlayerName);
        console.log('STATE UPDATE', state);
        if (!state.players) return;

        const currentTurnPlayer = state.currentPlayer || '';
        renderAllPlayers(state.players, currentPlayerName, currentTurnPlayer, state.state);

        const myData = state.players.find(p => p.name === currentPlayerName);
        if (myData) {
            updateBalance(myData.balance);
            const handsArray = parseHandsFromString(myData.handsStr);
            const handsContainer = document.getElementById('playerHands');
            if (handsContainer) {
                handsContainer.innerHTML = renderPlayerHands(handsArray);
            }
        }

        if (state.state === 'Waiting' || state.state === 'Betting') {
            resetDealerCards();
            updateDealerScore(null);
            return;
        }

        if (state.dealer && state.dealer !== '?') {
            const dealerCardsRaw = state.dealer.split(',');
            dealerCards = [];
            dealerCardsRaw.forEach((card, index) => {
                if (index === 0) {
                    dealerCards.push({ card: card, isOpen: true });
                } else {
                    if (state.state === 'DealerTurn' || state.state === 'RoundEnd') {
                        dealerCards.push({ card: card, isOpen: true });
                    } else {
                        dealerCards.push({ card: null, isOpen: false });
                    }
                }
            });
            renderDealer();

            if (state.state === 'DealerTurn' || state.state === 'RoundEnd') {
                updateDealerScore(calculateDealerTotal());
            } else {
                updateDealerScore('?');
            }
        }
    }

    return {
        initDealerCards,
        addDealerCard,
        resetDealerCards,
        updateDealerScore,
        updateBalance,
        updateFullState,
        renderPlayerHands,
        updatePlayerHandsFromSerialized,
        calculateDealerTotal,
        getDealerCardsCount
    };
})();
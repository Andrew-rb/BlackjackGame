const StateMachine = (function() {
    let currentGameState = 'WAITING'; 
    let myTurn = false;
    let canSplit = false;
    let canDouble = false;

    const btnBet = document.getElementById('btnBet');
    const btnStart = document.getElementById('btnStart');
    const btnNewRound = document.getElementById('btnNewRound');
    const btnHit = document.getElementById('btnHit');
    const btnStand = document.getElementById('btnStand');
    const btnDouble = document.getElementById('btnDouble');
    const btnSplit = document.getElementById('btnSplit');
    const betInput = document.getElementById('betAmount');

    function disableAllActions() {
        btnHit.disabled = true;
        btnStand.disabled = true;
        btnDouble.disabled = true;
        btnSplit.disabled = true;
    }

    function setTurn(turn, splitAvail = false, doubleAvail = false) {
        myTurn = turn;
        canSplit = splitAvail;
        canDouble = doubleAvail;
        if (turn && currentGameState === 'PLAYERTURN') {
            btnHit.disabled = false;
            btnStand.disabled = false;
            btnDouble.disabled = !doubleAvail;
            btnSplit.disabled = !splitAvail;
        } else {
            btnHit.disabled = true;
            btnStand.disabled = true;
            btnDouble.disabled = true;
            btnSplit.disabled = true;
        }
    }

    function enableBetting(enable) {
        btnBet.disabled = !enable;
        betInput.disabled = !enable;
        if (enable) btnStart.disabled = true;  
    }

    function enableStart(enable) {
        btnStart.disabled = !enable;
        if (enable) enableBetting(false);
    }

    function setGameState(state) {
        console.log('StateMachine: новое состояние ' + state);
        currentGameState = state;
        switch(state) {
            case 'WAITING':
                disableAllActions();
                enableStart(true);
                btnStart.style.display = 'inline-block';  
                btnNewRound.style.display = 'none';    
                enableBetting(false);
                myTurn = false;
                break;
            case 'BETTING':
                disableAllActions();
                enableBetting(true);
                btnStart.style.display = 'none';           
                btnNewRound.style.display = 'none';
                enableStart(false);                     
                myTurn = false;
                break;
            case 'PLAYERTURN':
                enableBetting(false);
                enableStart(false);
                btnStart.style.display = 'none';
                btnNewRound.style.display = 'none';
                break;
            case 'ROUNDEND':
                disableAllActions();
                enableBetting(false);
                enableStart(false);
                btnStart.style.display = 'none';         
                btnNewRound.style.display = 'inline-block';
                myTurn = false;
                break;
            default:
                break;
        }
    }

    function resetForNewRound() {
        setGameState('WAITING');
        setTurn(false);
    }

    return { setGameState, setTurn, resetForNewRound, disableAllActions };
})();
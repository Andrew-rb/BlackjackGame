const WS = (function() {
    let socket = null;
    let messageHandler = null;
    let connected = false;
    let reconnectAttempts = 0;
    const MAX_RECONNECT = 5;

    function log(level, msg, data) {
        const timestamp = new Date().toISOString();
        const consoleMsg = `[${timestamp}] [${level}] ${msg}` + (data ? ` ${JSON.stringify(data)}` : '');
        if (level === 'ERROR') console.error(consoleMsg);
        else console.log(consoleMsg);
    }

    function connect(serverUrl) {
        return new Promise((resolve, reject) => {
            if (socket && socket.readyState === WebSocket.OPEN) {
                log('INFO', 'Уже подключены');
                resolve();
                return;
            }
            log('INFO', `Подключение к ${serverUrl}`);
            socket = new WebSocket(serverUrl);
            socket.onopen = () => {
                connected = true;
                reconnectAttempts = 0;
                log('INFO', 'WebSocket соединение установлено');
                resolve();
            };
            socket.onerror = (err) => {
                log('ERROR', 'Ошибка WebSocket', err);
                reject(err);
            };
            socket.onclose = (ev) => {
                connected = false;
                log('WARN', `Соединение закрыто: ${ev.code} ${ev.reason}`);
                if (reconnectAttempts < MAX_RECONNECT) {
                    reconnectAttempts++;
                    log('INFO', `Попытка переподключения ${reconnectAttempts}/${MAX_RECONNECT} через 3с...`);
                    setTimeout(() => connect(serverUrl).catch(e => log('ERROR', 'Переподключение не удалось', e)), 3000);
                } else {
                    log('ERROR', 'Не удалось переподключиться, обновите страницу');
                }
            };
            socket.onmessage = (event) => {
                log('DEBUG', 'Получено сообщение', event.data);
                if (messageHandler) messageHandler(event.data);
            };
        });
    }

    function send(message) {
        if (socket && socket.readyState === WebSocket.OPEN) {
            socket.send(message);
            log('DEBUG', 'Отправлено', message);
        } else {
            log('ERROR', 'Невозможно отправить: соединение закрыто');
        }
    }

    function setHandler(handler) {
        messageHandler = handler;
    }

    function isConnected() { return connected; }

    return { connect, send, setHandler, isConnected };
})();
const Chat = (function() {
    const chatContainer = document.getElementById('chatMessages');
    const chatInput = document.getElementById('chatInput');
    const btnSend = document.getElementById('btnSendChat');

    function addMessage(text, isSystem = false) {
        const div = document.createElement('div');
        div.innerText = text;
        if (isSystem) div.classList.add('system-msg');
        chatContainer.appendChild(div);
        chatContainer.scrollTop = chatContainer.scrollHeight;
        while (chatContainer.children.length > 200) chatContainer.removeChild(chatContainer.firstChild);
    }

    function systemMessage(msg) {
        addMessage(`🔹 ${msg}`, true);
    }

    function playerMessage(name, msg) {
        addMessage(`${name}: ${msg}`, false);
    }

    function init(wsSendCallback) {
        btnSend.onclick = () => {
            const text = chatInput.value.trim();
            if (text) {
                wsSendCallback(`CHAT|${text}`);
                chatInput.value = '';
            }
        };
        chatInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter') btnSend.click();
        });
    }

    return { addMessage, systemMessage, playerMessage, init };
})();
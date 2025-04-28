let messages = [
    { role: "system", content: "You are a helpful assistant." }
];

function linkify(text) {
    const urlPattern = /(https?:\/\/[^\s]+)/g;
    return text.replace(urlPattern, `<a href="$1" target="_blank">$1</a>`);
}

async function sendMessage() {
    const input = document.getElementById("user-input");
    const chatBox = document.getElementById("chat-box");
    const userMessage = input.value.trim();
    if (!userMessage) return;

    messages.push({ role: "user", content: userMessage });
    chatBox.innerHTML += `<div class="message user"><strong>You:</strong> ${userMessage}</div>`;
    input.value = "";
    chatBox.scrollTop = chatBox.scrollHeight;

    const response = await fetch("/chat", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ messages }),
    });

    const data = await response.json();
    const formattedReply = linkify(data.reply);

    chatBox.innerHTML += `<div class="message Chatty"><strong>Chatty:</strong> ${formattedReply}</div>`;
    messages.push({ role: "assistant", content: data.reply });
    chatBox.scrollTop = chatBox.scrollHeight;
}

async function uploadFile() {
    const dropZone = document.getElementById('drop-zone');
    const fileInput = document.getElementById('fileInput');
    
    dropZone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropZone.classList.add('hover');
    });
    
    dropZone.addEventListener('dragleave', () => {
        dropZone.classList.remove('hover');
    });
    
    dropZone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropZone.classList.remove('hover');
        const file = e.dataTransfer.files[0];
        if (file) uploadFile(file);
    });
    
    fileInput.addEventListener('change', () => {
        const file = fileInput.files[0];
        if (file) uploadFile(file);
    });
    
    async function uploadFile(file) {
        const formData = new FormData();
        formData.append("file", file);
    
        const response = await fetch("/upload-read", {  // New endpoint
            method: "POST",
            body: formData
        });
    
        const result = await response.json();
        document.getElementById("chat-box").innerHTML += `<div class="message Chatty"><strong>Chatty:</strong> ${result.summary}</div>`;
    }
    

    
}
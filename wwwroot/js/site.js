const messages = [
  {
    role: "system",
    content:
      "You are a helpful assistant that explains documents, solves user issues, and provides guidance from screenshots.",
  },
];

function linkify(text) {
  const urlPattern = /(https?:\/\/[^\s]+)/g;
  return text.replace(urlPattern, '<a href="$1" target="_blank">$1</a>');
}

async function sendMessage() {
  const input = document.getElementById("user-input");
  const chatBox = document.getElementById("chat-box");
  const userMessage = input.value.trim();
  if (!userMessage) return;

  messages.push({role: "user", content: userMessage});
  chatBox.innerHTML += `<div class="message user"><strong>You:</strong> ${userMessage}</div>`;
  input.value = "";
  chatBox.scrollTop = chatBox.scrollHeight;

  const response = await fetch("/chat", {
    method: "POST",
    headers: {"Content-Type": "application/json"},
    body: JSON.stringify({messages}),
  });

  const data = await response.json();
  const formattedReply = linkify(data.reply);
  chatBox.innerHTML += `<div class="message Chatty"><strong>Chatty:</strong> ${formattedReply}</div>`;
  messages.push({role: "assistant", content: data.reply});
  chatBox.scrollTop = chatBox.scrollHeight;
}

function setupFileHandling() {
  const dropZone = document.getElementById("drop-zone");
  const fileInput = document.getElementById("fileInput");

  dropZone.addEventListener("dragover", e => {
    e.preventDefault();
    dropZone.classList.add("hover");
  });
  dropZone.addEventListener("dragleave", () =>
    dropZone.classList.remove("hover")
  );
  dropZone.addEventListener("drop", e => {
    e.preventDefault();
    dropZone.classList.remove("hover");
    const file = e.dataTransfer.files[0];
    if (file) uploadFile(file);
  });
  fileInput.addEventListener("change", () => {
    const file = fileInput.files[0];
    if (file) uploadFile(file);
  });

  document.addEventListener("paste", async e => {
    const items = e.clipboardData?.items;
    if (!items) return;

    for (const item of items) {
      if (item.type.startsWith("image/")) {
        const file = item.getAsFile();

        // ✅ Preview pasted image
        const reader = new FileReader();
        reader.onload = async function (event) {
          const imgHtml = `
            <div class="message user">
              <strong>You (pasted image):</strong><br>
              <img src="${event.target.result}" style="max-width: 300px; border-radius: 6px;" />
            </div>`;
          const chatBox = document.getElementById("chat-box");
          chatBox.innerHTML += imgHtml;
          chatBox.scrollTop = chatBox.scrollHeight;
        };
        reader.readAsDataURL(file);

        // ✅ Upload to /upload-image
        const formData = new FormData();
        formData.append("file", file);

        const res = await fetch("/upload-image", {
          method: "POST",
          body: formData,
        });

        const data = await res.json();

        document.getElementById("chat-box").innerHTML += `
          <div class="message Chatty">
            <strong>Chatty:</strong><br>${linkify(data.text)}
          </div>`;
        messages.push({
          role: "user",
          content: `This was pasted from an image. What do you think?\n\n${data.text}`,
        });
      }
    }
  });
}

async function uploadFile(file) {
  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch("/upload-read", {method: "POST", body: formData});
  const result = await res.json();

  document.getElementById("chat-box").innerHTML += `
      <div class="message Chatty"><strong>📄 Chatty (File Insight):</strong> ${result.summary}</div>`;
  messages.push({
    role: "user",
    content: `Can you explain this file to me?\n\n${result.summary}`,
  });
}

async function uploadScreenshot(file) {
  const formData = new FormData();
  formData.append("file", file);

  const res = await fetch("/upload-image", {method: "POST", body: formData});
  const data = await res.json();

  document.getElementById("chat-box").innerHTML += `
      <div class="message Chatty"><strong>📸 Chatty (OCR Result):</strong> ${data.text}</div>`;
  messages.push({
    role: "user",
    content: `I pasted a screenshot. Please help:\n\n${data.text}`,
  });
}

document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("send-button").addEventListener("click", sendMessage);
  setupFileHandling();

  document.getElementById("gpt-button").addEventListener("click", () => {
    window.open("https://chat.openai.com/", "_blank"); // Open in a new tab
  });
});



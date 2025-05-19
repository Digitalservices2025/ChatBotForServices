let attachedFile = null;
let attachedFileName = "";

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

  const userDisplay = attachedFile
    ? `You (with file "${attachedFileName}")`
    : "You";

  chatBox.innerHTML += `<div class="message user"><strong>${userDisplay}:</strong> ${userMessage}</div>`;
  input.value = "";
  chatBox.scrollTop = chatBox.scrollHeight;

  const thinkingId = `thinking-${Date.now()}`;
  const thinkingDiv = document.createElement("div");
  thinkingDiv.className = "message Chatty";
  thinkingDiv.id = thinkingId;
  thinkingDiv.innerHTML = `<strong>Chatty:</strong> <em>🤔 Chatty is thinking...</em>`;
  chatBox.appendChild(thinkingDiv);
  chatBox.scrollTop = chatBox.scrollHeight;

  const finalMessages = [...messages];

  if (attachedFile) {
    const formData = new FormData();
    formData.append("file", attachedFile);

    const res = await fetch("/upload-read", {
      method: "POST",
      body: formData,
    });

    const result = await res.json();
    finalMessages.push({
      role: "user",
      content: `This file was uploaded:\n\n${result.summary}`,
    });
  }

  finalMessages.push({role: "user", content: userMessage});

  const response = await fetch("/chat", {
    method: "POST",
    headers: {"Content-Type": "application/json"},
    body: JSON.stringify({messages: finalMessages}),
  });

  const data = await response.json();

  const isHtml = data.reply.includes("<a ");
  const formattedReply = isHtml ? data.reply : linkify(data.reply);

  const thinkingElement = document.getElementById(thinkingId);
  if (thinkingElement) {
    const replyDiv = document.createElement("div");
    replyDiv.className = "message Chatty";
    replyDiv.innerHTML = `<strong>Chatty:</strong><br>${formattedReply}`;
    thinkingElement.replaceWith(replyDiv);
  }

  messages.push({role: "user", content: userMessage});
  messages.push({role: "assistant", content: data.reply});

  // Reset file attachment
  attachedFile = null;
  attachedFileName = "";
  document.getElementById("file-preview").innerHTML = "";
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
        await uploadScreenshot(file);
      }
    }
  });
}

let lastUploadedFileContent = "";

async function uploadFile(file) {
  attachedFile = file;
  attachedFileName = file.name;

  // Only if auto-processing (not deferred upload)
  if (file.type === "application/pdf") {
    const chatBox = document.getElementById("chat-box");
    const thinkingId = `thinking-${Date.now()}`;
    chatBox.innerHTML += `<div id="${thinkingId}" class="message Chatty"><strong>Chatty:</strong> <em>🤔 Chatty is thinking (document)...</em></div>`;
    chatBox.scrollTop = chatBox.scrollHeight;

    const formData = new FormData();
    formData.append("file", file);

    const res = await fetch("/upload-read", {
      method: "POST",
      body: formData,
    });

    const result = await res.json();

    const thinkingDiv = document.getElementById(thinkingId);
    if (thinkingDiv) {
      thinkingDiv.innerHTML = `<strong>📄 Chatty (File Insight):</strong> ${result.summary}`;
    }

    messages.push({
      role: "user",
      content: `Please analyze this file:\n\n${result.summary}`,
    });
  } else {
    const filePreviewBox = document.getElementById("file-preview");
    filePreviewBox.innerHTML = `
        <div style="background: #2a2a40; padding: 10px 14px; border-radius: 8px; color: #d6eaff; margin-top: 10px; display: flex; align-items: center; justify-content: space-between;">
          <span>📎 <strong>Attached:</strong> ${file.name}</span>
          <button onclick="removeAttachedFile()" style="background: none; border: none; color: #ff6f6f; font-size: 1.2rem;">❌</button>
        </div>`;
  }
}

async function uploadScreenshot(file) {
  const chatBox = document.getElementById("chat-box");

  const thinkingId = `thinking-${Date.now()}`;
  chatBox.innerHTML += `
      <div id="${thinkingId}" class="message Chatty">
        <strong>Chatty:</strong> <em>🤔 Chatty is thinking (from screenshot)...</em>
      </div>`;
  chatBox.scrollTop = chatBox.scrollHeight;

  const formData = new FormData();
  formData.append("file", file);

  try {
    const res = await fetch("/upload-image", {
      method: "POST",
      body: formData,
    });

    if (!res.ok) throw new Error(`Upload failed: ${res.status}`);

    const data = await res.json();
    const thinkingDiv = document.getElementById(thinkingId);
    if (thinkingDiv) {
      thinkingDiv.innerHTML = `
          <strong>📸 Chatty (OCR Result):</strong><br>${linkify(data.text)}`;
    }

    messages.push({
      role: "user",
      content: `I pasted a screenshot. Please help:\n\n${data.text}`,
    });
  } catch (error) {
    console.error("OCR upload failed:", error);
    const thinkingDiv = document.getElementById(thinkingId);
    if (thinkingDiv) {
      thinkingDiv.innerHTML = `
          <strong>Chatty:</strong> ❌ Error analyzing screenshot. Please try again.`;
    }
  }
}

document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("send-button").addEventListener("click", sendMessage);
  setupFileHandling();

  document.getElementById("gpt-button").addEventListener("click", () => {
    window.open("https://chat.openai.com/", "_blank"); // Open in a new tab
  });
});

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

function removeAttachedFile() {
  attachedFile = null;
  attachedFileName = "";
  document.getElementById("file-preview").innerHTML = "";
}

document.addEventListener("DOMContentLoaded", () => {
  document.getElementById("send-button").addEventListener("click", sendMessage);
  setupFileHandling();

  document.getElementById("gpt-button").addEventListener("click", () => {
    window.open("https://chat.openai.com/", "_blank");
  });
});

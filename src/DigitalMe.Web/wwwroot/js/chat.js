// Chat functionality for DigitalMe Blazor Web App

window.scrollToBottom = (element) => {
    if (element) {
        element.scrollTop = element.scrollHeight;
    }
};

window.initializeTextarea = (textarea) => {
    if (textarea) {
        // Auto-focus on page load
        textarea.focus();
        
        // Set initial height
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
    }
};

window.autoResizeTextarea = (textarea) => {
    if (textarea) {
        textarea.style.height = 'auto';
        textarea.style.height = Math.min(textarea.scrollHeight, 120) + 'px';
    }
};

window.resetTextareaHeight = (textarea) => {
    if (textarea) {
        textarea.style.height = 'auto';
    }
};

window.copyToClipboard = async (text) => {
    try {
        if (navigator.clipboard && window.isSecureContext) {
            await navigator.clipboard.writeText(text);
        } else {
            // Fallback for older browsers or non-HTTPS
            const textArea = document.createElement('textarea');
            textArea.value = text;
            textArea.style.position = 'fixed';
            textArea.style.left = '-999999px';
            textArea.style.top = '-999999px';
            document.body.appendChild(textArea);
            textArea.focus();
            textArea.select();
            document.execCommand('copy');
            textArea.remove();
        }
        return true;
    } catch (err) {
        console.error('Failed to copy text:', err);
        return false;
    }
};

// Keyboard shortcuts
document.addEventListener('keydown', (e) => {
    // Ctrl+/ or Cmd+/ for focus to input
    if ((e.ctrlKey || e.metaKey) && e.key === '/') {
        e.preventDefault();
        const textarea = document.querySelector('.message-textarea');
        if (textarea) {
            textarea.focus();
        }
    }
});

// Auto-scroll on new messages
const observeNewMessages = () => {
    const chatMessages = document.querySelector('.chat-messages');
    if (!chatMessages) return;
    
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.type === 'childList' && mutation.addedNodes.length > 0) {
                // New message added, scroll to bottom
                setTimeout(() => {
                    chatMessages.scrollTop = chatMessages.scrollHeight;
                }, 100);
            }
        });
    });
    
    observer.observe(chatMessages, {
        childList: true,
        subtree: true
    });
};

// Initialize when page loads
document.addEventListener('DOMContentLoaded', () => {
    observeNewMessages();
});

// Reinitialize when Blazor updates the DOM
document.addEventListener('DOMContentLoaded', () => {
    // Blazor-specific: Reinitialize after server-side renders
    new MutationObserver(() => {
        observeNewMessages();
    }).observe(document.body, {
        childList: true,
        subtree: true
    });
});
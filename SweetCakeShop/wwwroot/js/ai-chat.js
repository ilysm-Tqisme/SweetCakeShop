/**
 * SweetCakeShop — Chat video style: GetChatHistory + SendMessage, DB + Gemini
 */
window.SweetCakeAiChat = function (config) {
    const fab = document.getElementById(config.fabId);
    const panel = document.getElementById(config.panelId);
    const closeBtn = document.getElementById(config.closeId);
    const input = document.getElementById(config.inputId);
    const sendBtn = document.getElementById(config.sendId);
    const messages = document.getElementById(config.messagesId);
    const quickRepliesEl = config.quickRepliesId ? document.getElementById(config.quickRepliesId) : null;

    if (!fab || !panel) return;

    let historyLoaded = false;

    function getClientContext() {
        const productIdRaw = panel.dataset.productId;
        const productId = productIdRaw && productIdRaw !== '' ? parseInt(productIdRaw, 10) : null;
        return {
            pageUrl: window.location.pathname + window.location.search,
            productId: Number.isFinite(productId) ? productId : null
        };
    }

    fab.addEventListener('click', async () => {
        panel.classList.toggle('open');
        if (panel.classList.contains('open') && !historyLoaded && config.historyUrl) {
            await loadHistory();
        }
    });
    closeBtn?.addEventListener('click', () => panel.classList.remove('open'));

    function escapeHtml(s) {
        const d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }

    function renderMarkdown(text) {
        let html = escapeHtml(text);
        html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
        html = html.replace(/^[-•] (.+)$/gm, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>\n?)+/gs, m => '<ul class="mb-0 ps-3">' + m + '</ul>');
        html = html.replace(/\n/g, '<br>');
        return html;
    }

    function renderProductCards(products) {
        if (!products || !products.length) return '';
        const cards = products.map(p => {
            const img = p.imageUrl || p.ImageUrl;
            const url = p.detailUrl || p.DetailUrl || '/Products/IndexPro';
            const name = p.name || p.Name;
            const price = p.price ?? p.Price;
            const imgHtml = img
                ? `<img src="${escapeHtml(img)}" alt="" class="ai-product-thumb" />`
                : '<span class="ai-product-thumb ai-product-thumb--empty">🍰</span>';
            return `<a class="ai-product-card" href="${escapeHtml(url)}">
                ${imgHtml}
                <span class="ai-product-card-body">
                    <span class="ai-product-name">${escapeHtml(name)}</span>
                    <span class="ai-product-price">${Number(price).toLocaleString('vi-VN')} VND</span>
                </span>
            </a>`;
        }).join('');
        return `<div class="ai-product-cards">${cards}</div>`;
    }

    function appendMsg(sender, text, products, scroll = true) {
        const role = sender === 'user' ? 'user' : 'bot';
        const wrap = document.createElement('div');
        wrap.className = 'ai-msg ' + role;
        const label = role === 'user' ? 'Bạn' : 'Trợ lý';
        const bubble = document.createElement('div');
        bubble.className = 'bubble';
        if (role === 'bot') {
            bubble.innerHTML = renderMarkdown(text) + renderProductCards(products);
        } else {
            bubble.textContent = text;
        }
        wrap.innerHTML = '<div class="small text-muted mb-1">' + label + '</div>';
        wrap.appendChild(bubble);
        messages.appendChild(wrap);
        if (scroll) messages.scrollTop = messages.scrollHeight;
    }

    function clearMessages() {
        messages.innerHTML = '';
    }

    function showTyping() {
        const wrap = document.createElement('div');
        wrap.className = 'ai-msg bot ai-typing-indicator';
        wrap.id = config.typingId || 'aiTypingIndicator';
        wrap.innerHTML = '<div class="small text-muted mb-1">Trợ lý</div><div class="bubble"><span class="typing-dots"><span></span><span></span><span></span></span></div>';
        messages.appendChild(wrap);
        messages.scrollTop = messages.scrollHeight;
    }

    function removeTyping() {
        document.getElementById(config.typingId || 'aiTypingIndicator')?.remove();
    }

    function renderQuickReplies(items) {
        if (!quickRepliesEl || !items?.length) return;
        quickRepliesEl.innerHTML = '';
        items.forEach(text => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'ai-quick-chip';
            btn.textContent = text;
            btn.addEventListener('click', () => {
                input.value = text;
                send();
            });
            quickRepliesEl.appendChild(btn);
        });
    }

    async function loadHistory() {
        try {
            const res = await fetch(config.historyUrl, { credentials: 'include' });
            const data = await res.json();
            clearMessages();
            const list = data.messages || data.Messages || [];
            list.forEach(m => {
                const sender = m.sender || m.Sender;
                appendMsg(sender, m.content || m.Content, m.products || m.Products, false);
            });
            messages.scrollTop = messages.scrollHeight;
            renderQuickReplies(data.quickReplies || data.QuickReplies || []);
            historyLoaded = true;
        } catch {
            appendMsg('model', 'Chào anh/chị! 🍰 Em tư vấn bánh SweetCakeShop — hỏi em bất cứ điều gì về menu nhé!');
        }
    }

    async function send() {
        const msg = input.value.trim();
        if (!msg) return;

        appendMsg('user', msg);
        input.value = '';
        sendBtn.disabled = true;
        input.disabled = true;
        showTyping();

        try {
            const ctx = getClientContext();
            const body = config.useAdminPayload
                ? { message: msg }
                : { userMessage: msg, pageUrl: ctx.pageUrl, productId: ctx.productId };
            const res = await fetch(config.sendUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(body)
            });
            const data = await res.json();
            removeTyping();
            const text = data.reply ?? data.Reply ?? 'Không có phản hồi.';
            const products = data.products ?? data.Products;
            appendMsg('model', text, products);
            const qr = data.quickReplies ?? data.QuickReplies;
            if (qr?.length) renderQuickReplies(qr);
            if (config.onCartAction && text.toLowerCase().includes('giỏ')) {
                try { config.onCartAction(); } catch (_) { }
            }
        } catch {
            removeTyping();
            appendMsg('model', 'Không thể kết nối trợ lý AI. Vui lòng thử lại sau giây lát.');
        } finally {
            sendBtn.disabled = false;
            input.disabled = false;
            input.focus();
        }
    }

    sendBtn?.addEventListener('click', send);
    input?.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            send();
        }
    });
};

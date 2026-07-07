window.api = {
    createRequest(dto) {
        return sendJson("/api/requests", "POST", dto);
    },

    startPipeline(options) {
        return sendJson("/api/pipeline/start", "POST", options);
    },

    stopPipeline() {
        return fetch("/api/pipeline/stop", { method: "POST" }).then(readResponse);
    },

    generate(count) {
        return fetch(`/api/pipeline/generate?count=${encodeURIComponent(count)}`, { method: "POST" }).then(readResponse);
    },

    getSnapshot() {
        return fetch("/api/pipeline/snapshot").then(readResponse);
    }
};

function sendJson(url, method, body) {
    return fetch(url, {
        method,
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(body)
    }).then(readResponse);
}

async function readResponse(response) {
    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (!response.ok) {
        const message = payload?.message || payload?.title || `HTTP ${response.status}`;
        throw new Error(message);
    }

    return payload;
}

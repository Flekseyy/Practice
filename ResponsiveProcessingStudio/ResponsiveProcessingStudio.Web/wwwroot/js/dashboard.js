const state = {
    snapshot: null,
    events: [],
    knownStatuses: new Map()
};

const statusLabels = {
    Created: "Создана",
    Waiting: "Waiting",
    Classifying: "Classifying",
    Validating: "Validating",
    Assigning: "Assigning",
    Processing: "Processing",
    Completed: "Completed",
    Failed: "Failed",
    Cancelled: "Cancelled"
};

const serviceLabels = {
    Unknown: "Не определено",
    Credit: "Кредитование",
    DebitCard: "Блокировка карты",
    Deposit: "Вклад",
    Mortgage: "Ипотека",
    MoneyTransfer: "Перевод средств"
};

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("todayDate").textContent = new Intl.DateTimeFormat("ru-RU", {
        day: "2-digit",
        month: "long",
        year: "numeric"
    }).format(new Date());

    bindActions();
    connectRealtime();
    refreshSnapshot();
});

function bindActions() {
    document.getElementById("btnStart").addEventListener("click", () => runAction(() => api.startPipeline(readSettings()), "Pipeline запущен"));
    document.getElementById("btnApplySettings").addEventListener("click", () => runAction(() => api.startPipeline(readSettings()), "Настройки применены"));
    document.getElementById("btnStop").addEventListener("click", () => runAction(() => api.stopPipeline(), "Pipeline остановлен"));
    document.getElementById("btnGenerate100").addEventListener("click", () => runAction(() => api.generate(100), "Тестовые заявки сгенерированы"));

    document.getElementById("requestForm").addEventListener("submit", async (event) => {
        event.preventDefault();

        const serviceType = document.getElementById("serviceType").value;
        const dto = {
            clientName: document.getElementById("clientName").value,
            serviceType: serviceType || null,
            message: document.getElementById("message").value
        };

        await runAction(async () => {
            const result = await api.createRequest(dto);
            document.getElementById("requestForm").reset();
            return result;
        }, "Заявка создана");
    });
}

async function runAction(action, successMessage) {
    const formMessage = document.getElementById("formMessage");
    formMessage.textContent = "";
    formMessage.className = "";

    try {
        await action();
        formMessage.textContent = successMessage;
        formMessage.classList.add("ok");
        await refreshSnapshot();
    } catch (error) {
        formMessage.textContent = error.message;
        formMessage.classList.add("error");
    }
}

function readSettings() {
    return {
        workersCount: readNumber("workersCount"),
        queueSize: readNumber("queueSize"),
        retriesCount: readNumber("retriesCount"),
        retryDelayMs: readNumber("retryDelayMs"),
        errorPercent: readNumber("errorPercent")
    };
}

function readNumber(id) {
    return Number(document.getElementById(id).value);
}

async function refreshSnapshot() {
    try {
        renderSnapshot(await api.getSnapshot());
    } catch (error) {
        setConnection("Ошибка API", false);
    }
}

function connectRealtime() {
    if (!window.signalR) {
        setConnection("Polling fallback", false);
        setInterval(refreshSnapshot, 1200);
        return;
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/pipeline")
        .withAutomaticReconnect()
        .build();

    connection.on("pipelineSnapshot", renderSnapshot);
    connection.onreconnecting(() => setConnection("Переподключение", false));
    connection.onreconnected(() => setConnection("Подключено", true));
    connection.onclose(() => {
        setConnection("Отключено", false);
        setInterval(refreshSnapshot, 1500);
    });

    connection.start()
        .then(() => {
            document.getElementById("signalrMode").textContent = "SignalR";
            setConnection("Подключено", true);
        })
        .catch(() => {
            setConnection("Polling fallback", false);
            setInterval(refreshSnapshot, 1200);
        });
}

function renderSnapshot(snapshot) {
    state.snapshot = snapshot;
    trackEvents(snapshot.recentRequests || []);

    setText("waitingCount", snapshot.waitingCount);
    setText("classifyingCount", snapshot.classifyingCount);
    setText("validatingCount", snapshot.validatingCount);
    setText("assigningCount", snapshot.assigningCount);
    setText("processingCount", snapshot.processingCount);
    setText("completedCount", snapshot.completedCount);
    setText("failedCount", snapshot.failedCount);
    setText("cancelledCount", snapshot.cancelledCount);
    setText("retryCount", snapshot.retryCount);
    setText("workersCountView", snapshot.workersCount);

    setText("stageWaiting", snapshot.waitingCount);
    setText("stageClassifying", snapshot.classifyingCount);
    setText("stageValidating", snapshot.validatingCount);
    setText("stageAssigning", snapshot.assigningCount);
    setText("stageProcessing", snapshot.processingCount);
    setText("stageCompleted", snapshot.completedCount);

    const processed = snapshot.completedCount + snapshot.failedCount + snapshot.cancelledCount;
    const successRate = processed > 0 ? Math.round((snapshot.completedCount / processed) * 100) : 0;
    const errorRate = processed > 0 ? Math.round((snapshot.failedCount / processed) * 100) : 0;

    setText("totalProcessed", processed);
    setText("successRate", `${successRate}%`);
    setText("errorRate", `${errorRate}%`);
    setText("pipelineStateText", stateText(snapshot.pipelineStatus));

    renderRequests(snapshot.recentRequests || []);
    renderEvents();
    setConnection(document.getElementById("connectionDetails").textContent || "Подключено", true);
    setText("lastMessageTime", new Date().toLocaleTimeString("ru-RU"));
}

function renderRequests(requests) {
    const table = document.getElementById("requestsTable");

    if (!requests.length) {
        table.innerHTML = `<tr><td colspan="8" class="empty-row">Заявок пока нет</td></tr>`;
        return;
    }

    table.innerHTML = requests.map((request) => `
        <tr>
            <td>#${shortId(request.id)}</td>
            <td>${escapeHtml(request.clientName)}</td>
            <td>${serviceLabels[request.serviceType] || request.serviceType}</td>
            <td><span class="status-badge ${String(request.status).toLowerCase()}">${statusLabels[request.status] || request.status}</span></td>
            <td>${escapeHtml(request.assignedDepartment || "-")}</td>
            <td>${escapeHtml(request.assignedHandler || "-")}</td>
            <td>${request.retryCount}</td>
            <td>${escapeHtml(request.lastError || "-")}</td>
        </tr>
    `).join("");
}

function trackEvents(requests) {
    for (const request of requests) {
        const previous = state.knownStatuses.get(request.id);
        if (previous && previous !== request.status) {
            state.events.unshift({
                time: new Date().toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit", second: "2-digit" }),
                text: `Заявка #${shortId(request.id)}: ${previous} → ${request.status}`,
                status: request.status
            });
        } else if (!previous) {
            state.events.unshift({
                time: new Date().toLocaleTimeString("ru-RU", { hour: "2-digit", minute: "2-digit", second: "2-digit" }),
                text: `Заявка #${shortId(request.id)} добавлена в систему`,
                status: request.status
            });
        }

        state.knownStatuses.set(request.id, request.status);
    }

    state.events = state.events.slice(0, 7);
}

function renderEvents() {
    const list = document.getElementById("eventList");
    if (!state.events.length) {
        list.innerHTML = `<li class="muted">Событий пока нет</li>`;
        return;
    }

    list.innerHTML = state.events.map((event) => `
        <li>
            <span>${event.time}</span>
            <p>${escapeHtml(event.text)}</p>
        </li>
    `).join("");
}

function setConnection(text, online) {
    const pill = document.getElementById("connectionStatus");
    pill.textContent = text;
    pill.classList.toggle("online", online);
    document.getElementById("connectionDetails").textContent = text;
}

function stateText(status) {
    if (status === "Running") return "Система работает стабильно";
    if (status === "Stopping") return "Pipeline останавливается";
    return "Pipeline остановлен";
}

function setText(id, value) {
    document.getElementById(id).textContent = value;
}

function shortId(id) {
    return String(id).replaceAll("-", "").slice(0, 6);
}

function escapeHtml(value) {
    return String(value)
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

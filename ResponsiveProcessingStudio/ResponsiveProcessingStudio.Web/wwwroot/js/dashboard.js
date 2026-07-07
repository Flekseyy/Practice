const state = {
    snapshot: null,
    pollingTimerId: null,
    stageModal: {
        stage: null,
        timerId: null,
        refreshVersion: 0,
        isRefreshing: false
    }
};

const statusLabels = {
    0: "Создана",
    1: "Ожидает",
    2: "Классификация",
    3: "Проверка",
    4: "Назначение",
    5: "В обработке",
    6: "Успешно",
    7: "Ошибка",
    8: "Отменено",
    Created: "Создана",
    Waiting: "Ожидает",
    Classifying: "Классификация",
    Validating: "Проверка",
    Assigning: "Назначение",
    Processing: "В обработке",
    Completed: "Успешно",
    Failed: "Ошибка",
    Cancelled: "Отменено"
};

const serviceLabels = {
    0: "Не определено",
    1: "Кредитование",
    2: "Банковская карта",
    3: "Вклад",
    4: "Ипотека",
    5: "Перевод средств",
    Unknown: "Не определено",
    Credit: "Кредитование",
    DebitCard: "Банковская карта",
    Deposit: "Вклад",
    Mortgage: "Ипотека",
    MoneyTransfer: "Перевод средств"
};

const statusKeys = {
    0: "created",
    1: "waiting",
    2: "classifying",
    3: "validating",
    4: "assigning",
    5: "processing",
    6: "completed",
    7: "failed",
    8: "cancelled",
    Created: "created",
    Waiting: "waiting",
    Classifying: "classifying",
    Validating: "validating",
    Assigning: "assigning",
    Processing: "processing",
    Completed: "completed",
    Failed: "failed",
    Cancelled: "cancelled"
};

const pipelineStages = [
    { status: "Waiting", title: "Очередь" },
    { status: "Classifying", title: "Классификация" },
    { status: "Validating", title: "Проверка" },
    { status: "Assigning", title: "Назначение" },
    { status: "Processing", title: "Обработка" },
    { status: "Completed", title: "Завершение" }
];

document.addEventListener("DOMContentLoaded", () => {
    document.getElementById("todayDate").textContent = new Intl.DateTimeFormat("ru-RU", {
        day: "2-digit",
        month: "long",
        year: "numeric"
    }).format(new Date());

    bindActions();
    bindStageCards();
    bindStageModal();
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

        if (!isPipelineRunning(state.snapshot?.pipelineStatus)) {
            const formMessage = document.getElementById("formMessage");
            formMessage.textContent = "Сначала запустите обработку";
            formMessage.className = "error";
            return;
        }

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

function bindStageCards() {
    const cards = document.querySelectorAll(".pipeline-track .stage-card");

    cards.forEach((card, index) => {
        const stage = pipelineStages[index];
        if (!stage) {
            return;
        }

        card.setAttribute("role", "button");
        card.setAttribute("tabindex", "0");
        card.setAttribute("aria-label", `Показать заявки этапа ${stage.title}`);

        card.addEventListener("click", () => openStageModal(stage));
        card.addEventListener("keydown", (event) => {
            if (event.key !== "Enter" && event.key !== " ") {
                return;
            }

            event.preventDefault();
            openStageModal(stage);
        });
    });
}

function bindStageModal() {
    const modal = document.getElementById("stageModal");
    const closeButton = document.getElementById("stageModalClose");

    closeButton?.addEventListener("click", closeStageModal);
    modal?.addEventListener("click", (event) => {
        if (event.target === modal) {
            closeStageModal();
        }
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && modal && !modal.hidden) {
            closeStageModal();
        }
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
        startPollingFallback();
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
        startPollingFallback();
    });

    connection.start()
        .then(() => {
            setConnection("Подключено", true);
        })
        .catch(() => {
            startPollingFallback();
        });
}

function startPollingFallback() {
    setConnection("Polling fallback", false);

    if (state.pollingTimerId !== null) {
        return;
    }

    state.pollingTimerId = setInterval(refreshSnapshot, 1200);
}

function renderSnapshot(snapshot) {
    state.snapshot = snapshot;
    syncPipelineControls(snapshot.pipelineStatus);

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

    setConnection("Подключено", true);
}

function syncPipelineControls(pipelineStatus) {
    const isRunning = isPipelineRunning(pipelineStatus);
    const generateButton = document.getElementById("btnGenerate100");
    const createButton = document.getElementById("btnCreateRequest");
    const formMessage = document.getElementById("formMessage");

    if (generateButton) {
        generateButton.disabled = !isRunning;
    }

    if (createButton) {
        createButton.disabled = !isRunning;
    }

    if (!formMessage) {
        return;
    }

    if (!isRunning) {
        formMessage.textContent = "Сначала запустите обработку";
        formMessage.className = "error";
    } else if (formMessage.textContent === "Сначала запустите обработку") {
        formMessage.textContent = "";
        formMessage.className = "";
    }
}

function isPipelineRunning(pipelineStatus) {
    return pipelineStatus === "Running" || pipelineStatus === 1;
}

async function openStageModal(stage) {
    const modal = document.getElementById("stageModal");
    const title = document.getElementById("stageModalTitle");
    const subtitle = document.getElementById("stageModalSubtitle");
    const table = document.getElementById("stageRequestsTable");

    if (!modal || !title || !subtitle || !table) {
        return;
    }

    stopStageModalRefresh();
    state.stageModal.stage = stage;
    title.textContent = stage.title;
    subtitle.textContent = "Загрузка заявок...";
    table.innerHTML = `<tr><td colspan="8" class="empty-row">Загрузка...</td></tr>`;
    modal.hidden = false;
    document.body.classList.add("modal-open");

    await refreshStageModal(true);
    startStageModalRefresh();
}

function startStageModalRefresh() {
    if (state.stageModal.timerId !== null) {
        return;
    }

    state.stageModal.timerId = setInterval(() => refreshStageModal(false), 500);
}

function stopStageModalRefresh() {
    if (state.stageModal.timerId === null) {
        return;
    }

    clearInterval(state.stageModal.timerId);
    state.stageModal.timerId = null;
}

async function refreshStageModal(isInitialLoad) {
    const stage = state.stageModal.stage;
    const subtitle = document.getElementById("stageModalSubtitle");
    const table = document.getElementById("stageRequestsTable");

    if (!stage || !subtitle || !table || state.stageModal.isRefreshing) {
        return;
    }

    const refreshVersion = ++state.stageModal.refreshVersion;
    state.stageModal.isRefreshing = true;

    try {
        const requests = await getRequestsByStatus(stage.status);

        if (refreshVersion !== state.stageModal.refreshVersion || state.stageModal.stage !== stage) {
            return;
        }

        subtitle.textContent = `${requests.length} ${pluralizeRequests(requests.length)} на этом этапе`;
        renderStageRequests(requests);
    } catch (error) {
        subtitle.textContent = isInitialLoad
            ? "Не удалось загрузить заявки"
            : "Не удалось обновить заявки";

        if (isInitialLoad) {
            table.innerHTML = `<tr><td colspan="8" class="empty-row">${escapeHtml(error.message)}</td></tr>`;
        }
    } finally {
        state.stageModal.isRefreshing = false;
    }
}

async function getRequestsByStatus(status) {
    if (window.api && typeof window.api.getRequestsByStatus === "function") {
        return window.api.getRequestsByStatus(status);
    }

    const response = await fetch(`/api/requests?status=${encodeURIComponent(status)}`);
    const text = await response.text();
    const payload = text ? JSON.parse(text) : null;

    if (!response.ok) {
        const message = payload?.message || payload?.title || `HTTP ${response.status}`;
        throw new Error(message);
    }

    return payload;
}

function closeStageModal() {
    const modal = document.getElementById("stageModal");
    if (!modal) {
        return;
    }

    stopStageModalRefresh();
    state.stageModal.stage = null;
    state.stageModal.refreshVersion++;
    modal.hidden = true;
    document.body.classList.remove("modal-open");
}

function renderStageRequests(requests) {
    const table = document.getElementById("stageRequestsTable");
    if (!table) {
        return;
    }

    if (!requests.length) {
        table.innerHTML = `<tr><td colspan="8" class="empty-row">На этом этапе сейчас нет заявок</td></tr>`;
        return;
    }

    table.innerHTML = renderRequestRows(requests);
}

function renderRequestRows(requests) {
    return requests.map((request) => `
        <tr>
            <td>#${shortId(request.id)}</td>
            <td>${escapeHtml(request.clientName)}</td>
            <td>${serviceText(request.serviceType)}</td>
            <td><span class="status-badge ${statusClass(request.status)}">${statusText(request.status)}</span></td>
            <td>${escapeHtml(request.assignedDepartment || "-")}</td>
            <td>${escapeHtml(request.assignedHandler || "-")}</td>
            <td>${request.retryCount}</td>
            <td>${escapeHtml(request.lastError || "-")}</td>
        </tr>
    `).join("");
}

function pluralizeRequests(count) {
    const lastTwo = count % 100;
    const last = count % 10;

    if (lastTwo >= 11 && lastTwo <= 14) {
        return "заявок";
    }

    if (last === 1) {
        return "заявка";
    }

    if (last >= 2 && last <= 4) {
        return "заявки";
    }

    return "заявок";
}

function setConnection(text, online) {
    const pill = document.getElementById("connectionStatus");
    if (!pill) {
        return;
    }

    pill.textContent = text;
    pill.classList.toggle("online", online);
}

function stateText(status) {
    if (status === "Running") return "Система работает стабильно";
    if (status === "Stopping") return "Pipeline останавливается";
    return "Pipeline остановлен";
}

function setText(id, value) {
    const element = document.getElementById(id);
    if (element) {
        element.textContent = value;
    }
}

function serviceText(value) {
    return serviceLabels[value] || String(value || "-");
}

function statusText(value) {
    return statusLabels[value] || String(value || "-");
}

function statusClass(value) {
    return statusKeys[value] || String(value || "").toLowerCase();
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

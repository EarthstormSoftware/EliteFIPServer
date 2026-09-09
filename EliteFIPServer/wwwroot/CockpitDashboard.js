"use strict";

const connection = window.panelConnection;
const byId = id => document.getElementById(id);
const text = (id, value) => { const element = byId(id); if (element) element.textContent = value ?? "--"; };
const fixed = (value, places = 1) => typeof value === "number" ? value.toFixed(places) : "--";
const money = value => typeof value === "number" ? value.toLocaleString() : "--";
const joined = values => values.filter(value => value !== null && value !== undefined && value !== "").join(" / ") || "--";
const hasTimestamp = data => data?.LastUpdate && new Date(data.LastUpdate).getUTCFullYear() > 1;
const query = new URLSearchParams(window.location.search);
const cleanProfileName = value => (value || "default").trim().replace(/[^a-z0-9 _-]/gi, "").slice(0, 32) || "default";
const layoutProfile = cleanProfileName(query.get("layout"));
const embeddableWidgets = ["ship", "location", "target", "activity", "route"];
const requestedWidget = (query.get("widget") || "").toLowerCase();
const embeddedWidget = embeddableWidgets.includes(requestedWidget) ? requestedWidget : null;
const fullscreenPreferenceKey = `elite-dashboard-fullscreen:${layoutProfile.toLowerCase()}`;
const settingsKey = `elite-dashboard-settings-v7:${layoutProfile.toLowerCase()}`;
const viewDefinitions = {
    cockpit: "Complete Cockpit",
    navigation: "Navigation Dashboard",
    combat: "Combat Dashboard"
};
let currentView = window.location.hash.slice(1).toLowerCase();
if (!viewDefinitions[currentView]) currentView = "cockpit";

function loadDashboardSettings() {
    try {
        const stored = JSON.parse(localStorage.getItem(settingsKey) || "{}");
        return stored && typeof stored === "object" ? stored : {};
    } catch (error) {
        console.warn("Ignoring invalid cockpit settings", error);
        return {};
    }
}

const dashboardSettings = loadDashboardSettings(),
    settingsButton = byId("dashboard-settings"),
    configurationToolbar = byId("configuration-toolbar"),
    profileInput = byId("layout-profile"),
    profileButton = byId("apply-layout-profile"),
    presentationSelect = byId("presentation-mode"),
    activeLayoutLabel = byId("active-layout-label"),
    grid = document.querySelector(".grid"),
    panels = () => Array.from(document.querySelectorAll("[data-widget-panel]"));

const widgetLabels = {
    ship: "Ship", location: "Location", target: "Target", activity: "Activity", route: "Route"
};
// Free-form grid: every widget owns an explicit {x, y, w, h} cell rectangle so panels can be
// placed anywhere without relying on source order or auto-flow wrapping.
const GRID_COLUMNS = 12;
const MIN_GRID_ROWS = 4;
const defaultWidgetSize = {
    ship: { w: 6, h: 2 }, location: { w: 6, h: 2 }, target: { w: 6, h: 2 },
    activity: { w: 6, h: 2 }, route: { w: 12, h: 2 }
};
let configurationMode = false;
let presentationMode;

function resolvePresentationMode() {
    if (embeddedWidget) return "display";
    if (dashboardSettings.mode === "display" || dashboardSettings.mode === "tablet") return dashboardSettings.mode;
    return window.matchMedia("(hover: none) and (pointer: coarse)").matches || window.innerWidth <= 760 ? "tablet" : "display";
}

function applyPresentationMode() {
    presentationMode = resolvePresentationMode();
    document.body.classList.toggle("mode-display", presentationMode === "display");
    document.body.classList.toggle("mode-tablet", presentationMode === "tablet");
    document.body.dataset.presentationMode = presentationMode;
    if (presentationSelect) presentationSelect.value = dashboardSettings.mode || "auto";
    document.querySelectorAll("details").forEach(details => {
        if (presentationMode === "display") details.open = true;
    });
}

function setConfigurationMode(enabled) {
    if (embeddedWidget) return;
    configurationMode = enabled;
    document.body.classList.toggle("configuration-mode", enabled);
    if (configurationToolbar) configurationToolbar.hidden = !enabled;
    if (!enabled) panels().forEach(panel => panel.classList.remove("dragging", "resizing", "invalid-drop"));
    renderConfigurationControls();
}

function getViewSettings() {
    dashboardSettings.views ??= {};
    dashboardSettings.views[currentView] ??= {};
    return dashboardSettings.views[currentView];
}

function getViewLayout() {
    const settings = getViewSettings();
    settings.layout ??= {};
    return settings.layout;
}

function isWidgetEnabled(widget) {
    if (embeddedWidget) return widget === embeddedWidget;
    return getViewSettings().widgets?.[widget] !== false;
}

function setWidgetEnabled(widget, enabled) {
    const settings = getViewSettings();
    settings.widgets ??= {};
    settings.widgets[widget] = enabled;
    saveDashboardSettings();
}

function getTabletOrder() {
    const settings = getViewSettings();
    const available = panels().filter(panel => panel.dataset.views.split(" ").includes(currentView)).map(panel => panel.dataset.widgetPanel);
    settings.tabletOrder = [...(settings.tabletOrder || []).filter(widget => available.includes(widget)), ...available.filter(widget => !settings.tabletOrder?.includes(widget))];
    return settings.tabletOrder;
}

function moveTabletWidget(widget, offset) {
    const order = getTabletOrder();
    const from = order.indexOf(widget);
    const to = Math.max(0, Math.min(order.length - 1, from + offset));
    if (from === to) return;
    [order[from], order[to]] = [order[to], order[from]];
    saveDashboardSettings();
    applyGridLayout();
    renderConfigurationControls();
}

function cyclePanelDetail(widget) {
    const settings = getViewSettings();
    settings.detail ??= {};
    const levels = ["compact", "standard", "detailed"];
    settings.detail[widget] = levels[(levels.indexOf(settings.detail[widget] || "standard") + 1) % levels.length];
    saveDashboardSettings();
    applyGridLayout();
    renderConfigurationControls();
}

function rectsOverlap(a, b) {
    return a.x < b.x + b.w && a.x + a.w > b.x && a.y < b.y + b.h && a.y + a.h > b.y;
}

function findFreeRect(w, h, occupied) {
    for (let y = 0; ; y++) {
        for (let x = 0; x <= GRID_COLUMNS - w; x++) {
            const candidate = { x, y, w, h };
            if (!occupied.some(rect => rectsOverlap(candidate, rect))) return candidate;
        }
    }
}

function visibleWidgetsForView() {
    if (embeddedWidget) return panels().filter(panel => panel.dataset.widgetPanel === embeddedWidget);
    return panels().filter(panel => panel.dataset.views.split(" ").includes(currentView) && isWidgetEnabled(panel.dataset.widgetPanel));
}

function widgetRect(widget, layout) {
    const size = defaultWidgetSize[widget] || { w: 3, h: 2 };
    const rect = layout[widget];
    return { x: rect?.x ?? 0, y: rect?.y ?? 0, w: rect?.w ?? size.w, h: rect?.h ?? size.h };
}

// Assigns a cell rectangle to any visible widget that doesn't already have one.
function ensureLayout() {
    const layout = getViewLayout();
    const occupied = [];
    visibleWidgetsForView().forEach(panel => {
        const widget = panel.dataset.widgetPanel;
        const size = defaultWidgetSize[widget] || { w: 3, h: 2 };
        let rect = layout[widget];
        if (!rect || typeof rect.x !== "number" || typeof rect.y !== "number") {
            rect = findFreeRect(rect?.w || size.w, rect?.h || size.h, occupied);
            layout[widget] = rect;
        }
        occupied.push(widgetRect(widget, layout));
    });
}

function currentRowCount() {
    const layout = getViewLayout();
    const bottoms = visibleWidgetsForView().map(panel => {
        const rect = widgetRect(panel.dataset.widgetPanel, layout);
        return rect.y + rect.h;
    });
    return Math.max(MIN_GRID_ROWS, ...bottoms, 0);
}

function setPanelRect(panel, rect) {
    panel.style.gridColumn = `${rect.x + 1} / span ${rect.w}`;
    panel.style.gridRow = `${rect.y + 1} / span ${rect.h}`;
}

function applyGridLayout() {
    if (!grid) return;
    if (embeddedWidget) {
        grid.style.setProperty("--grid-rows", 1);
        visibleWidgetsForView().forEach(panel => setPanelRect(panel, { x: 0, y: 0, w: GRID_COLUMNS, h: 1 }));
        return;
    }
    ensureLayout();
    const layout = getViewLayout();
    grid.style.setProperty("--grid-rows", currentRowCount());
    const tabletOrder = getTabletOrder();
    visibleWidgetsForView().forEach(panel => {
        const widget = panel.dataset.widgetPanel;
        setPanelRect(panel, widgetRect(widget, layout));
        panel.style.order = presentationMode === "tablet" ? tabletOrder.indexOf(widget) : "";
        panel.dataset.detailLevel = getViewSettings().detail?.[widget] || "standard";
    });
}

// Shared drag/resize logic. Panels use explicit grid placement (no auto-flow), so moving one
// panel never reflows the others - the dragged rectangle just has to avoid overlapping them.
function beginPointerLayout(panel, event, mode) {
    if (!configurationMode || presentationMode === "tablet") return;
    event.preventDefault();
    event.stopPropagation();
    const widget = panel.dataset.widgetPanel;
    const layout = getViewLayout();
    const startRect = widgetRect(widget, layout);
    const gridBounds = grid.getBoundingClientRect();
    const cellWidth = gridBounds.width / GRID_COLUMNS;
    const cellHeight = gridBounds.height / currentRowCount();
    const startX = event.clientX;
    const startY = event.clientY;
    const others = visibleWidgetsForView()
        .filter(other => other !== panel)
        .map(other => widgetRect(other.dataset.widgetPanel, layout));

    panel.classList.add(mode === "move" ? "dragging" : "resizing");
    document.body.classList.add(mode === "move" ? "moving-widget" : "resizing-widget");
    panel.style.zIndex = "5";
    let currentRect = startRect;

    const onMove = moveEvent => {
        const dxCells = Math.round((moveEvent.clientX - startX) / cellWidth);
        const dyCells = Math.round((moveEvent.clientY - startY) / cellHeight);
        currentRect = mode === "move"
            ? {
                x: Math.max(0, Math.min(GRID_COLUMNS - startRect.w, startRect.x + dxCells)),
                y: Math.max(0, startRect.y + dyCells),
                w: startRect.w, h: startRect.h
            }
            : {
                x: startRect.x, y: startRect.y,
                w: Math.max(2, Math.min(GRID_COLUMNS - startRect.x, startRect.w + dxCells)),
                h: Math.max(1, startRect.h + dyCells)
            };
        panel.classList.toggle("invalid-drop", others.some(other => rectsOverlap(currentRect, other)));
        setPanelRect(panel, currentRect);
    };

    const finish = () => {
        document.removeEventListener("pointermove", onMove);
        document.removeEventListener("pointerup", finish);
        document.removeEventListener("pointercancel", finish);
        panel.classList.remove("dragging", "resizing", "invalid-drop");
        document.body.classList.remove("moving-widget", "resizing-widget");
        panel.style.zIndex = "";
        const invalid = others.some(other => rectsOverlap(currentRect, other));
        layout[widget] = invalid ? startRect : currentRect;
        saveDashboardSettings();
        applyGridLayout();
    };

    document.addEventListener("pointermove", onMove);
    document.addEventListener("pointerup", finish, { once: true });
    document.addEventListener("pointercancel", finish, { once: true });
}

function addResizeHandle(panel) {
    const handle = document.createElement("div");
    handle.className = "widget-resize-handle";
    handle.title = "Drag to resize";
    handle.addEventListener("pointerdown", event => beginPointerLayout(panel, event, "resize"));
    panel.appendChild(handle);
}

function renderConfigurationControls() {
    panels().forEach(panel => {
        panel.querySelector(".widget-config")?.remove();
        panel.querySelector(".widget-resize-handle")?.remove();
        if (!configurationMode || panel.hidden) return;
        const controls = document.createElement("div");
        controls.className = "widget-config";
        const remove = document.createElement("button");
        remove.type = "button";
        remove.className = "widget-remove";
        remove.textContent = "-";
        remove.title = "Remove widget";
        remove.setAttribute("aria-label", `Remove ${widgetLabels[panel.dataset.widgetPanel]}`);
        remove.addEventListener("click", () => {
            setWidgetEnabled(panel.dataset.widgetPanel, false);
            applyDashboardSettings();
        });
        controls.append(remove);
        if (presentationMode === "tablet") {
            const up = document.createElement("button");
            up.type = "button";
            up.textContent = "\u2191";
            up.title = "Move panel up";
            up.setAttribute("aria-label", `Move ${widgetLabels[panel.dataset.widgetPanel]} up`);
            up.addEventListener("click", () => moveTabletWidget(panel.dataset.widgetPanel, -1));
            const down = document.createElement("button");
            down.type = "button";
            down.textContent = "\u2193";
            down.title = "Move panel down";
            down.setAttribute("aria-label", `Move ${widgetLabels[panel.dataset.widgetPanel]} down`);
            down.addEventListener("click", () => moveTabletWidget(panel.dataset.widgetPanel, 1));
            const detail = document.createElement("button");
            detail.type = "button";
            detail.textContent = "D";
            detail.title = `Detail: ${panel.dataset.detailLevel || "standard"}`;
            detail.setAttribute("aria-label", `Change ${widgetLabels[panel.dataset.widgetPanel]} detail level`);
            detail.addEventListener("click", () => cyclePanelDetail(panel.dataset.widgetPanel));
            controls.prepend(up, down, detail);
        }
        panel.appendChild(controls);
        addResizeHandle(panel);
    });

    document.querySelectorAll(".config-add-slot").forEach(slot => slot.remove());
    if (!configurationMode) return;
    const inactive = panels().filter(panel => panel.dataset.views.split(" ").includes(currentView) && !isWidgetEnabled(panel.dataset.widgetPanel));
    if (!inactive.length) return;
    const layout = getViewLayout();
    const occupied = visibleWidgetsForView().map(panel => widgetRect(panel.dataset.widgetPanel, layout));
    const slotRect = findFreeRect(3, 2, occupied);
    grid.style.setProperty("--grid-rows", Math.max(currentRowCount(), slotRect.y + slotRect.h));
    const slot = document.createElement("div");
    slot.className = "config-add-slot";
    setPanelRect(slot, slotRect);
    const add = document.createElement("button");
    add.type = "button";
    add.className = "config-add-button";
    add.textContent = "+";
    add.title = "Add widget";
    add.addEventListener("click", () => {
        slot.classList.toggle("picker-open");
        picker.hidden = !picker.hidden;
    });
    const picker = document.createElement("div");
    picker.className = "widget-picker";
    picker.hidden = true;
    inactive.forEach(panel => {
        const choice = document.createElement("button");
        choice.type = "button";
        choice.textContent = widgetLabels[panel.dataset.widgetPanel];
        choice.addEventListener("click", () => {
            setWidgetEnabled(panel.dataset.widgetPanel, true);
            applyDashboardSettings();
        });
        picker.appendChild(choice);
    });
    slot.append(add, picker);
    grid.appendChild(slot);
}

function configurePanelDrag(panel) {
    panel.addEventListener("pointerdown", event => {
        if (!configurationMode || event.target.closest("button, .widget-resize-handle")) return;
        beginPointerLayout(panel, event, "move");
    });
}

panels().forEach(configurePanelDrag);

function saveDashboardSettings() {
    localStorage.setItem(settingsKey, JSON.stringify(dashboardSettings));
}

function applyDashboardSettings() {
    document.querySelectorAll("[data-widget-panel]").forEach(panel => {
        panel.hidden = embeddedWidget
            ? panel.dataset.widgetPanel !== embeddedWidget
            : !isWidgetEnabled(panel.dataset.widgetPanel) || !panel.dataset.views.split(" ").includes(currentView);
    });
    applyGridLayout();
    renderConfigurationControls();
}

function applyView(view, updateUrl = true) {
    currentView = viewDefinitions[view] ? view : "cockpit";
    document.title = embeddedWidget ? `Elite ${widgetLabels[embeddedWidget]}` : `Elite ${viewDefinitions[currentView]}`;
    if (activeLayoutLabel) activeLayoutLabel.textContent = `Current: ${layoutProfile} / ${currentView}`;
    document.querySelectorAll("[data-view-link]").forEach(link => {
        link.classList.toggle("active", link.dataset.viewLink === currentView);
    });
    if (updateUrl && window.location.hash !== `#${currentView}`) {
        history.replaceState(null, "", `#${currentView}`);
    }
    applyDashboardSettings();
}

if (embeddedWidget) {
    document.body.classList.add("widget-embed");
    document.body.dataset.embeddedWidget = embeddedWidget;
}
if (profileInput) profileInput.value = layoutProfile;
if (profileButton) profileButton.addEventListener("click", () => {
    const url = new URL(window.location.href);
    url.searchParams.set("layout", cleanProfileName(profileInput.value));
    window.location.assign(url);
});
if (presentationSelect) presentationSelect.addEventListener("change", () => {
    dashboardSettings.mode = presentationSelect.value;
    saveDashboardSettings();
    applyPresentationMode();
    applyDashboardSettings();
});
if (settingsButton) settingsButton.addEventListener("click", () => setConfigurationMode(!configurationMode));
document.querySelectorAll("[data-view-link]").forEach(link => link.addEventListener("click", event => {
    event.preventDefault();
    applyView(link.dataset.viewLink);
}));
window.addEventListener("hashchange", () => applyView(window.location.hash.slice(1).toLowerCase(), false));
window.addEventListener("resize", () => {
    if (!dashboardSettings.mode || dashboardSettings.mode === "auto") {
        applyPresentationMode();
        applyDashboardSettings();
    }
});
document.querySelectorAll("[data-view-link]").forEach(link => {
    const url = new URL(link.href, window.location.href);
    url.search = window.location.search;
    link.href = url.pathname + url.search + url.hash;
});
applyPresentationMode();
applyDashboardSettings();
applyView(currentView, false);

const fullscreenButton = byId("fullscreen");
let fullscreenRestoreButton;
function hideFullscreenRestore() {
    if (fullscreenRestoreButton) fullscreenRestoreButton.hidden = true;
}
function showFullscreenRestore() {
    if (!fullscreenRestoreButton) {
        fullscreenRestoreButton = document.createElement("button");
        fullscreenRestoreButton.className = "fullscreen-restore";
        fullscreenRestoreButton.type = "button";
        fullscreenRestoreButton.textContent = "Restore fullscreen";
        fullscreenRestoreButton.addEventListener("click", async () => {
            localStorage.setItem(fullscreenPreferenceKey, "true");
            try {
                await document.documentElement.requestFullscreen();
                hideFullscreenRestore();
                updateFullscreenButton();
            } catch (error) {
                console.warn("Fullscreen restore was blocked", error);
            }
        });
        document.body.appendChild(fullscreenRestoreButton);
    }
    fullscreenRestoreButton.hidden = false;
}
const updateFullscreenButton = () => {
    if (!fullscreenButton) return;
    const isFullscreen = Boolean(document.fullscreenElement);
    fullscreenButton.textContent = isFullscreen ? "\u00d7" : "\u26f6";
    fullscreenButton.setAttribute("aria-label", isFullscreen ? "Exit fullscreen" : "Toggle fullscreen");
    fullscreenButton.title = isFullscreen ? "Exit fullscreen" : "Toggle fullscreen";
    if (isFullscreen) hideFullscreenRestore();
};

if (fullscreenButton) fullscreenButton.addEventListener("click", async () => {
    if (document.fullscreenElement) {
        localStorage.setItem(fullscreenPreferenceKey, "false");
        await document.exitFullscreen();
    } else {
        localStorage.setItem(fullscreenPreferenceKey, "true");
        await document.documentElement.requestFullscreen();
    }
    updateFullscreenButton();
});

document.addEventListener("fullscreenchange", updateFullscreenButton);
updateFullscreenButton();
if (localStorage.getItem(fullscreenPreferenceKey) === "true" && !document.fullscreenElement) {
    window.setTimeout(() => document.documentElement.requestFullscreen().catch(() => showFullscreenRestore()), 0);
}

function renderList(id, items, formatter, emptyText) {
    const list = byId(id);
    if (!list) return;
    list.replaceChildren();
    const values = Array.isArray(items) ? items : [];
    if (!values.length) {
        const item = document.createElement("li");
        item.textContent = emptyText;
        list.append(item);
        return;
    }
    values.slice(0, 12).forEach(value => {
        const item = document.createElement("li");
        item.textContent = formatter(value);
        list.append(item);
    });
    if (values.length > 12) {
        const item = document.createElement("li");
        item.textContent = `+ ${values.length - 12} more`;
        list.append(item);
    }
}

const activityEntries = [];
function addActivity(label, detail) {
    if (!detail) return;
    activityEntries.unshift(`${label}: ${detail}`);
    activityEntries.splice(6);
    renderList("activity-feed", activityEntries, value => value, "No recent events");
}

connection.on("StatusData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("ship-fuel", `${fixed(data.FuelMain, 1)} t main / ${fixed(data.FuelReservoir, 2)} t reserve`);
    text("ship-cargo", `${fixed(data.Cargo, 0)} t`);
    text("ship-condition", `${fixed(data.Health * 100, 0)}% hull / ${fixed(data.Temperature, 0)} C`);
    text("ship-pips", `SYS ${data.SystemPips ?? "--"} / ENG ${data.EnginePips ?? "--"} / WEP ${data.WeaponPips ?? "--"}`);
    const alerts = [
        data.LowFuel && "LOW FUEL", data.Overheating && "OVERHEATING", data.InDanger && "DANGER",
        data.BeingInterdicted && "INTERDICTION", data.FsdMassLocked && "MASS LOCKED",
        data.FsdCharging && "FSD CHARGING", data.SilentRunning && "SILENT RUNNING"
    ].filter(Boolean);
    text("ship-alerts", alerts.length ? alerts.join(" / ") : joined([data.LegalState, "Nominal"]));
});

connection.on("LocationData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("location-system", data.SystemName || "Unknown system");
    text("status-system", data.SystemName || "Unknown system");
    text("location-body", data.BodyName || "Deep space");
    text("location-station", data.StationName || "In space");
    text("location-context", data.IsDocked ? "Docked" : data.IsOnFoot ? "On foot" : data.IsInSrv ? "SRV" : data.IsInTaxi ? "Taxi" : "In flight");
    text("location-coordinates", data.Latitude || data.Longitude ? `${fixed(data.Latitude, 4)}, ${fixed(data.Longitude, 4)}` : "--");
});

connection.on("TargetData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("target-ship", data.TargetLocked ? data.Ship : "No target");
    text("target-pilot", data.TargetLocked ? joined([data.PilotName, data.PilotRank]) : "--");
    text("target-condition", data.TargetLocked ? `${fixed(data.HullHealth * 100, 0)}% / ${fixed(data.ShieldHealth * 100, 0)}%` : "--");
    text("target-legal", data.TargetLocked ? joined([data.LegalStatus, data.Bounty ? `${money(data.Bounty)} CR` : ""]) : "--");
    text("target-scan", data.TargetLocked ? `${data.ScanStage ?? 0} / 3` : "--");
    text("target-affiliation", data.TargetLocked ? joined([data.Faction, data.Power]) : "--");
    text("target-squadron", data.TargetLocked ? data.SquadronId : "--");
    text("target-subsystem", data.TargetLocked && data.SubSystem ? `${data.SubSystem} / ${fixed(data.SubSystemHealth * 100, 0)}%` : "--");
});

connection.on("StationData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("location-station", data.StationName || "In space");
    text("status-station", data.StationName || "In space");
    text("location-station-profile", joined([data.StationType, data.StationState, data.Economy, data.HasActiveFine ? "Active fine" : ""]));
    text("location-pads", data.SmallLandingPads || data.MediumLandingPads || data.LargeLandingPads
        ? `S ${data.SmallLandingPads} / M ${data.MediumLandingPads} / L ${data.LargeLandingPads}` : "--");
    text("location-services", data.Services?.length ? data.Services.join(", ") : "--");
});

connection.on("ExplorationData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("location-body", data.BodyName || "Deep space");
    text("location-body-class", data.PlanetClass || data.StarType || data.BodyType);
    text("location-geology", joined([data.AtmosphereType || data.Atmosphere, data.Volcanism, data.IsLandable ? "Landable" : ""]));
    text("location-environment", joined([data.SurfaceGravity ? `${fixed(data.SurfaceGravity, 2)} m/s2` : "", data.SurfaceTemperature ? `${fixed(data.SurfaceTemperature, 0)} K` : ""]));
    text("location-survey", data.BodyType === "Station" ? "--" : `${data.WasDiscovered ? "Discovered" : "Unresolved"} / ${data.WasMapped ? "Mapped" : "Unmapped"}`);
    renderList("location-material-list", data.Materials, material => `${material.Name} ${fixed(material.Percent, 1)}%`, "No body material data");
});

connection.on("LoadoutData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    const shipName = data.ShipName || data.Ship || "Ship";
    text("ship-title", `${shipName} - Ship Information System`);
    text("status-ship", shipName);
    text("ship-jump-range", data.MaxJumpRange ? `${fixed(data.MaxJumpRange, 2)} ly` : "--");
    text("ship-rebuy", data.Rebuy ? `${money(data.Rebuy)} CR` : "--");
    renderList("ship-module-list", data.Modules, module => {
        const engineered = module.Engineering?.BlueprintName ? ` / G${module.Engineering.Level} ${module.Engineering.BlueprintName}` : "";
        return `${module.Slot}: ${module.Item}${engineered}`;
    }, "No loadout data");
});

connection.on("MissionData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("activity-mission", data.LocalisedName || data.Name);
    text("activity-destination", joined([data.DestinationSystem, data.DestinationStation]));
    text("activity-mission-meta", joined([data.Reward ? `${money(data.Reward)} CR` : "", data.Expiry ? new Date(data.Expiry).toLocaleString() : ""]));
    addActivity("Mission", joined([data.TargetType, data.Target, data.Count ? `x${data.Count}` : ""]));
});

connection.on("ReceivedTextData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("activity-message", joined([data.Source, data.Message]));
    addActivity(data.Channel || "Message", joined([data.Source, data.Message]));
});

connection.on("CargoData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("ship-cargo", `${fixed(data.Total, 0)} / ${fixed(data.Capacity, 0)} t`);
    renderList("ship-cargo-list", data.Items, item => `${item.Name} x${item.Count}${item.IsStolen ? " / stolen" : ""}`, "Cargo hold empty");
});

connection.on("MaterialsData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("ship-materials", `Raw ${data.Raw?.length ?? 0} / Manufactured ${data.Manufactured?.length ?? 0} / Encoded ${data.Encoded?.length ?? 0}`);
});

connection.on("CombatData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    addActivity(data.Operation || "Combat", joined([data.Target || data.Faction, data.Reward ? `${money(data.Reward)} CR` : ""]));
});

connection.on("SystemData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("location-system", data.SystemName || "Unknown system");
    text("location-economy", joined([data.Economy, data.SecondaryEconomy]));
    text("location-security", data.Security);
    text("location-population", data.Population ? money(data.Population) : "--");
    text("location-powerplay", joined([data.PowerplayState, ...(data.Powers || [])]));
    text("location-faction", data.ControllingFaction);
    renderList("location-faction-list", data.Factions, faction => joined([
        faction.Name,
        faction.Influence ? `${fixed(faction.Influence * 100, 1)}%` : "",
        faction.State,
        faction.Happiness,
        typeof faction.Reputation === "number" ? `Reputation ${fixed(faction.Reputation, 0)}` : ""
    ]), "No faction data");
});

connection.on("MissionLifecycleData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    addActivity("Mission", joined([data.Operation, data.LocalisedName || data.Name, data.Reward ? `${money(data.Reward)} CR` : "", data.Fine ? `${money(data.Fine)} CR fine` : ""]));
});

connection.on("MissionCollectionData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    const active = data.Active || [];
    text("activity-mission", active.length ? `${active.length} active` : "No active missions");
    active.slice(0, 4).forEach(mission => addActivity("Active mission", joined([mission.Name, mission.IsPassengerMission ? "Passenger" : ""])));
});

connection.on("DockingData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data) || !data.Status) return;
    if (data.StationName) text("location-station", data.StationName);
    const guidance = data.Status === "Granted" && data.LandingPad
        ? `Docking granted / pad ${data.LandingPad}`
        : joined([data.Status, data.Reason]);
    text("location-context", guidance);
    if (data.Status && data.Status !== "Undocked") addActivity("Docking", joined([data.Status, data.StationName, data.LandingPad ? `pad ${data.LandingPad}` : "", data.Reason]));
});

connection.on("RouteTargetData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data) || !data.SystemName) return;
    text("route-next", joined([data.SystemName, data.StarClass]));
    text("route-remaining", typeof data.RemainingJumps === "number" ? `${data.RemainingJumps} jumps` : "--");
});

connection.on("JumpData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data) || !data.DestinationSystemName) return;
    text("route-last-jump", data.JumpComplete ? joined([data.DestinationSystemName, `${fixed(data.JumpDistance, 2)} ly`, `${fixed(data.FuelUsed, 2)} t used`]) : joined(["Jumping", data.DestinationSystemName]));
});

connection.on("NavRouteData", function () {});
connection.on("PreviousNavRoute", function () {});

startPanelConnection();

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
const embeddableWidgets = ["ship", "location", "target", "activity", "route", "exploration", "exobiology", "missions", "commander", "trade", "carrier"];
const requestedWidget = (query.get("widget") || "").toLowerCase();
const embeddedWidget = embeddableWidgets.includes(requestedWidget) ? requestedWidget : null;
const fullscreenPreferenceKey = `elite-dashboard-fullscreen:${layoutProfile.toLowerCase()}`;
const settingsKey = `elite-dashboard-settings-v8:${layoutProfile.toLowerCase()}`;
// v7 layouts had rows a quarter of the screen high; v8 rows are three times finer. v7 is left in place.
const legacySettingsKey = `elite-dashboard-settings-v7:${layoutProfile.toLowerCase()}`;
const LEGACY_ROW_SCALE = 3;
const viewDefinitions = {
    cockpit: "Complete Cockpit",
    navigation: "Navigation Dashboard",
    combat: "Combat Dashboard"
};
let currentView = window.location.hash.slice(1).toLowerCase();
if (!viewDefinitions[currentView]) currentView = "cockpit";

// Colour theme is a per-device choice shared by every layout; ?theme= overrides it so an
// embedded widget or display browser can be themed without opening the configuration toolbar.
const themeNames = ["teal", "orange", "blue", "green", "night", "contrast"];
const themeKey = "elite-dashboard-theme";
function storedTheme() {
    try { return localStorage.getItem(themeKey); } catch { return null; }
}
let currentTheme = [(query.get("theme") || "").toLowerCase(), storedTheme()].find(theme => themeNames.includes(theme)) || "teal";

function applyTheme(theme) {
    currentTheme = theme;
    document.documentElement.dataset.theme = theme;
    const frameRoot = byId("route-frame")?.contentDocument?.documentElement;
    if (frameRoot) frameRoot.dataset.theme = theme;
    const background = getComputedStyle(document.documentElement).getPropertyValue("--background").trim();
    if (background) document.querySelector('meta[name="theme-color"]')?.setAttribute("content", background);
    const select = byId("dashboard-theme");
    if (select) select.value = theme;
}
applyTheme(currentTheme);
byId("route-frame")?.addEventListener("load", () => applyTheme(currentTheme));
byId("dashboard-theme")?.addEventListener("change", event => {
    const theme = themeNames.includes(event.target.value) ? event.target.value : "teal";
    try { localStorage.setItem(themeKey, theme); } catch { /* the choice still applies to this page */ }
    if (query.has("theme")) {
        query.set("theme", theme);
        history.replaceState(null, "", `${window.location.pathname}?${query}${window.location.hash}`);
    }
    applyTheme(theme);
});

function loadDashboardSettings() {
    try {
        const current = localStorage.getItem(settingsKey);
        const stored = JSON.parse(current ?? localStorage.getItem(legacySettingsKey) ?? "{}");
        if (!stored || typeof stored !== "object") return {};
        if (current === null) {
            Object.values(stored.views || {}).forEach(view => Object.values(view?.layout || {}).forEach(rect => {
                if (typeof rect?.y === "number") rect.y *= LEGACY_ROW_SCALE;
                if (typeof rect?.h === "number") rect.h *= LEGACY_ROW_SCALE;
            }));
        }
        return stored;
    } catch (error) {
        console.warn("Ignoring invalid cockpit settings", error);
        return {};
    }
}

const dashboardSettings = loadDashboardSettings(),
    settingsButton = byId("dashboard-settings"),
    cancelButton = byId("dashboard-cancel"),
    configurationToolbar = byId("configuration-toolbar"),
    profileInput = byId("layout-profile"),
    profileButton = byId("apply-layout-profile"),
    presentationSelect = byId("presentation-mode"),
    activeLayoutLabel = byId("active-layout-label"),
    grid = document.querySelector(".grid"),
    panels = () => Array.from(document.querySelectorAll("[data-widget-panel]"));

const widgetLabels = {
    ship: "Ship", location: "Location", target: "Target", activity: "Activity", route: "Route",
    exploration: "Exploration", exobiology: "Exobiology", missions: "Missions",
    commander: "Commander", trade: "Mining and Trade", carrier: "Fleet Carrier"
};
// Free-form grid: every widget owns an explicit {x, y, w, h} cell rectangle so panels can be
// placed anywhere without relying on source order or auto-flow wrapping. The rows stretch so the
// layout always fits the screen: at least MIN_GRID_ROWS, or more if widgets reach further down.
const GRID_COLUMNS = 12;
const MIN_GRID_ROWS = 12;
const defaultWidgetSize = {
    ship: { w: 6, h: 6 }, location: { w: 6, h: 6 }, target: { w: 6, h: 6 },
    activity: { w: 6, h: 6 }, route: { w: 12, h: 6 }, exploration: { w: 6, h: 6 }, exobiology: { w: 6, h: 6 }, missions: { w: 6, h: 6 },
    commander: { w: 6, h: 6 }, trade: { w: 6, h: 6 }, carrier: { w: 6, h: 6 }
};
// The smallest each widget can be resized to, so its essential readings stay legible: a width in columns,
// and a height in pixels, because rows get shorter as a layout grows taller.
const minimumWidgetSize = {
    ship: { w: 3, px: 170 }, location: { w: 3, px: 170 }, target: { w: 3, px: 150 }, activity: { w: 3, px: 150 },
    route: { w: 4, px: 150 }, exploration: { w: 3, px: 150 }, exobiology: { w: 3, px: 150 }, missions: { w: 3, px: 150 },
    commander: { w: 3, px: 150 }, trade: { w: 3, px: 150 }, carrier: { w: 3, px: 150 }
};
// rowPitch is a row plus one gap; a widget h rows tall is h * rowPitch - gap pixels.
const minimumSize = (widget, rowPitch, gap) => {
    const minimum = minimumWidgetSize[widget] || { w: 3, px: 150 };
    return { w: minimum.w, h: Math.max(2, Math.ceil((minimum.px + gap) / rowPitch)) };
};
const gridGap = () => parseFloat(getComputedStyle(grid).rowGap) || 0;
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

// The settings as they were when editing started, for Cancel.
let settingsBeforeEditing = null;

function setConfigurationMode(enabled) {
    if (embeddedWidget) return;
    configurationMode = enabled;
    settingsBeforeEditing = enabled ? JSON.stringify(dashboardSettings) : null;
    document.body.classList.toggle("configuration-mode", enabled);
    if (configurationToolbar) configurationToolbar.hidden = !enabled;
    if (!enabled) panels().forEach(panel => panel.classList.remove("dragging", "resizing"));
    // While editing, the gear becomes Done and Cancel appears beside it.
    if (settingsButton) {
        settingsButton.textContent = enabled ? "✓" : "⚙";
        settingsButton.title = enabled ? "Done" : "Configure dashboard";
        settingsButton.setAttribute("aria-label", enabled ? "Finish editing the dashboard" : "Configure dashboard");
        settingsButton.setAttribute("aria-pressed", String(enabled));
    }
    if (cancelButton) cancelButton.hidden = !enabled;
    const settingsIcon = byId("layout-settings-button");
    if (settingsIcon) settingsIcon.hidden = !enabled;
    if (!enabled) byId("layout-settings-dialog")?.close();
    applyGridLayout();
    renderConfigurationControls();
}

// Puts back the layout, widgets, detail levels and presentation mode from before editing started.
function cancelEditing() {
    if (!configurationMode || settingsBeforeEditing === null) return;
    const before = JSON.parse(settingsBeforeEditing);
    Object.keys(dashboardSettings).forEach(key => delete dashboardSettings[key]);
    Object.assign(dashboardSettings, before);
    saveDashboardSettings();
    setConfigurationMode(false);
    applyPresentationMode();
    applyDashboardSettings();
}

function getViewSettings() {
    dashboardSettings.views ??= {};
    dashboardSettings.views[currentView] ??= {};
    return dashboardSettings.views[currentView];
}

// Landscape and portrait screens keep separate positions for each view; which panels a view shows, and their
// detail levels, are shared. "layout" (landscape) is the original key, so older settings need no migration.
const screenOrientation = () => window.innerWidth >= window.innerHeight ? "landscape" : "portrait";
let layoutOrientation = screenOrientation();
const layoutKeyFor = orientation => orientation === "portrait" ? "portraitLayout" : "layout";

function getViewLayout(orientation = layoutOrientation) {
    const settings = getViewSettings();
    const key = layoutKeyFor(orientation);
    // A first portrait layout stacks the landscape panels full width, top to bottom, keeping their heights.
    settings[key] ??= orientation === "portrait" ? stackedLayout(settings.layout || {}) : {};
    return settings[key];
}

function stackedLayout(source) {
    let y = 0;
    return Object.fromEntries(Object.entries(source)
        .filter(([, rect]) => typeof rect?.x === "number" && typeof rect?.y === "number")
        .sort(([, a], [, b]) => a.y - b.y || a.x - b.x)
        .map(([widget, rect]) => {
            const stacked = { x: 0, y, w: GRID_COLUMNS, h: rect.h || 6 };
            y += stacked.h;
            return [widget, stacked];
        }));
}

function isWidgetEnabled(widget) {
    if (embeddedWidget) return widget === embeddedWidget;
    const chosen = getViewSettings().widgets?.[widget];
    if (typeof chosen === "boolean") return chosen;
    // Widgets added in later versions only appear by themselves in their default views, so existing
    // layouts don't suddenly gain extra panels; the other views offer them under "+".
    const defaultViews = document.querySelector(`[data-widget-panel="${widget}"]`)?.dataset.defaultViews;
    return defaultViews === undefined || defaultViews.split(" ").includes(currentView);
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
    const size = defaultWidgetSize[widget] || { w: 3, h: 6 };
    const rect = layout[widget];
    return { x: rect?.x ?? 0, y: rect?.y ?? 0, w: rect?.w ?? size.w, h: rect?.h ?? size.h };
}

// Assigns a cell rectangle to any visible widget that doesn't already have one, around every saved one.
function ensureLayout() {
    const layout = getViewLayout();
    const placed = rect => rect && typeof rect.x === "number" && typeof rect.y === "number";
    const widgets = visibleWidgetsForView().map(panel => panel.dataset.widgetPanel);
    const occupied = widgets.filter(widget => placed(layout[widget])).map(widget => widgetRect(widget, layout));
    widgets.filter(widget => !placed(layout[widget])).forEach(widget => {
        // New panels default to full width in portrait, where half-width columns are narrow.
        const size = { ...(defaultWidgetSize[widget] || { w: 3, h: 6 }), ...(layoutOrientation === "portrait" ? { w: GRID_COLUMNS } : {}) };
        layout[widget] = findFreeRect(layout[widget]?.w || size.w, layout[widget]?.h || size.h, occupied);
        occupied.push(layout[widget]);
    });
}

// Rows shorter than this mean the minimums can't fit on this screen.
const MIN_ROW_PIXELS = 8;
const rowCountOf = rects => Math.max(MIN_GRID_ROWS, ...Object.values(rects).map(rect => rect.y + rect.h), 0);

// The saved layout as drawn on this screen. In display mode, a widget shorter than its pixel minimum (because
// the view has grown taller, or the screen is smaller) is drawn taller, pushing what's below it down. A few
// passes settle it, as extra rows make every row shorter; the saved layout only changes when the user edits.
function effectiveLayout() {
    const layout = getViewLayout();
    const saved = Object.fromEntries(visibleWidgetsForView().map(panel => [panel.dataset.widgetPanel, widgetRect(panel.dataset.widgetPanel, layout)]));
    // While editing, show the saved layout exactly, so what is arranged is what is stored; resizing still
    // respects the minimums.
    if (presentationMode !== "display" || embeddedWidget || configurationMode || !grid || !grid.clientHeight) return saved;
    let rects = saved;
    const gap = gridGap();
    // Each pass grows every short widget for the current row height; the extra rows shorten every row, so
    // repeat until nothing is short. A layout that can't fit at any height stops after the last pass.
    for (let pass = 0; pass < 12; pass++) {
        const rowPitch = (grid.clientHeight + gap) / rowCountOf(rects);
        const short = Object.keys(rects)
            .sort((a, b) => rects[a].y - rects[b].y)
            .filter(widget => rects[widget].h < minimumSize(widget, rowPitch, gap).h);
        if (!short.length) break;
        short.forEach(widget => {
            rects = pushLayout(rects, widget, { ...rects[widget], h: Math.max(rects[widget].h, minimumSize(widget, rowPitch, gap).h) });
        });
    }
    // On a screen too short for the minimums (widgets stacked taller than the grid), growing rows never
    // settles, as each row adds a gap. Then draw the saved layout as it is rather than one that overflows.
    const rows = rowCountOf(rects);
    const rowPitch = (grid.clientHeight + gap) / rows;
    const settled = rowPitch - gap >= MIN_ROW_PIXELS
        && Object.keys(rects).every(widget => rects[widget].h >= minimumSize(widget, rowPitch, gap).h);
    return settled ? rects : saved;
}

function currentRowCount() {
    return rowCountOf(effectiveLayout());
}

// Faint cell outlines behind the panels while editing, so the snapping is visible.
function renderGridGuides(rows) {
    let guides = grid.querySelector(".grid-guides");
    if (!configurationMode || presentationMode === "tablet" || embeddedWidget) {
        guides?.remove();
        return;
    }
    if (!guides) {
        guides = document.createElement("div");
        guides.className = "grid-guides";
        guides.setAttribute("aria-hidden", "true");
        grid.prepend(guides);
    }
    if (guides.childElementCount !== rows * GRID_COLUMNS) {
        guides.replaceChildren(...Array.from({ length: rows * GRID_COLUMNS }, () => document.createElement("span")));
    }
}

// Views this tall have rows too short to read comfortably; say so while editing.
const TALL_VIEW_ROWS = 24;
function updateLayoutWarning(rows) {
    const warning = byId("layout-warning");
    if (!warning) return;
    warning.hidden = !configurationMode || presentationMode === "tablet" || rows <= TALL_VIEW_ROWS;
    warning.textContent = `This view is ${rows} rows tall, so panels are small. Remove or narrow some to make room.`;
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
    const rects = effectiveLayout();
    const rows = rowCountOf(rects);
    grid.style.setProperty("--grid-rows", rows);
    renderGridGuides(rows);
    updateLayoutWarning(rows);
    const tabletOrder = getTabletOrder();
    visibleWidgetsForView().forEach(panel => {
        const widget = panel.dataset.widgetPanel;
        setPanelRect(panel, rects[widget]);
        panel.style.order = presentationMode === "tablet" ? tabletOrder.indexOf(widget) : "";
        panel.dataset.detailLevel = getViewSettings().detail?.[widget] || "standard";
    });
}

// Puts `moved` at `rect` and pushes every widget it now overlaps straight down, cascading to anything those
// land on. Widgets clear of the change stay exactly where they are; nothing is pulled up to fill gaps.
function pushLayout(rects, moved, rect) {
    const result = { [moved]: rect };
    const placed = [rect];
    Object.keys(rects)
        .filter(widget => widget !== moved)
        .sort((a, b) => rects[a].y - rects[b].y || rects[a].x - rects[b].x)
        .forEach(widget => {
            const next = { ...rects[widget] };
            let blocker;
            while ((blocker = placed.find(other => rectsOverlap(next, other)))) next.y = blocker.y + blocker.h;
            result[widget] = next;
            placed.push(next);
        });
    return result;
}

// Shared drag/resize logic. Panels use explicit grid placement (no auto-flow); while dragging, widgets
// in the way are shown pushed down, and the drop keeps that arrangement.
function beginPointerLayout(panel, event, mode) {
    if (!configurationMode || presentationMode === "tablet") return;
    event.preventDefault();
    event.stopPropagation();
    const widget = panel.dataset.widgetPanel;
    const layout = getViewLayout();
    const startRects = effectiveLayout();
    const startRect = startRects[widget];
    const gridBounds = grid.getBoundingClientRect();
    const cellWidth = gridBounds.width / GRID_COLUMNS;
    const gap = gridGap();
    const cellHeight = (gridBounds.height + gap) / currentRowCount();
    const minimum = minimumSize(widget, cellHeight, gap);
    const startX = event.clientX;
    const startY = event.clientY;
    const visible = visibleWidgetsForView();

    panel.classList.add(mode === "move" ? "dragging" : "resizing");
    document.body.classList.add(mode === "move" ? "moving-widget" : "resizing-widget");
    panel.style.zIndex = "5";
    let arrangement = startRects;

    const onMove = moveEvent => {
        const dxCells = Math.round((moveEvent.clientX - startX) / cellWidth);
        const dyCells = Math.round((moveEvent.clientY - startY) / cellHeight);
        const rect = mode === "move"
            ? {
                x: Math.max(0, Math.min(GRID_COLUMNS - startRect.w, startRect.x + dxCells)),
                y: Math.max(0, startRect.y + dyCells),
                w: startRect.w, h: startRect.h
            }
            : {
                x: startRect.x, y: startRect.y,
                w: Math.max(Math.min(minimum.w, GRID_COLUMNS - startRect.x), Math.min(GRID_COLUMNS - startRect.x, startRect.w + dxCells)),
                h: Math.max(minimum.h, startRect.h + dyCells)
            };
        arrangement = pushLayout(startRects, widget, rect);
        visible.forEach(other => setPanelRect(other, arrangement[other.dataset.widgetPanel]));
        grid.style.setProperty("--grid-rows", rowCountOf(arrangement));
        renderGridGuides(rowCountOf(arrangement));
    };

    const finish = () => {
        document.removeEventListener("pointermove", onMove);
        document.removeEventListener("pointerup", finish);
        document.removeEventListener("pointercancel", finish);
        panel.classList.remove("dragging", "resizing");
        document.body.classList.remove("moving-widget", "resizing-widget");
        panel.style.zIndex = "";
        Object.assign(layout, arrangement);
        saveDashboardSettings();
        applyGridLayout();
        renderConfigurationControls();
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
    // In display mode the slot goes in free space within the rows the layout already has, around the layout as
    // drawn; adding rows would make every panel shorter. With no room, the "+" sits in the toolbar instead.
    const occupied = Object.values(effectiveLayout());
    // The rows actually drawn, which applyGridLayout set for the grid's current height.
    const rows = Number(grid.style.getPropertyValue("--grid-rows")) || currentRowCount();
    const slotRect = presentationMode === "tablet" ? findFreeRect(3, 6, occupied)
        : [6, 5, 4, 3, 2].map(h => findFreeRect(3, h, occupied)).find(rect => rect.y + rect.h <= rows);
    const slot = document.createElement("div");
    slot.className = "config-add-slot";
    if (slotRect) {
        setPanelRect(slot, slotRect);
    } else {
        slot.classList.add("in-toolbar");
    }
    // Tablet mode stacks panels by order; keep the add slot last.
    slot.style.order = presentationMode === "tablet" ? "9999" : "";
    const add = document.createElement("button");
    add.type = "button";
    add.className = "config-add-button";
    add.textContent = "+";
    add.title = "Add widget";
    add.addEventListener("click", () => {
        slot.classList.toggle("picker-open");
        picker.hidden = !picker.hidden;
        // Open upwards from the lower half of the screen so the list stays visible.
        const bounds = add.getBoundingClientRect();
        picker.classList.toggle("opens-up", bounds.top + bounds.height / 2 > window.innerHeight / 2);
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
    (slotRect || !configurationToolbar ? grid : configurationToolbar).appendChild(slot);
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
    updateEditingLabel();
}

// Which layout, view and (in display mode, where positions apply) orientation is being edited.
function updateEditingLabel() {
    if (!activeLayoutLabel) return;
    const orientation = presentationMode === "display" ? ` (${layoutOrientation})` : "";
    activeLayoutLabel.textContent = `Editing ${layoutProfile} / ${viewDefinitions[currentView]}${orientation}`;
}

function applyView(view, updateUrl = true) {
    currentView = viewDefinitions[view] ? view : "cockpit";
    document.title = embeddedWidget ? `Elite ${widgetLabels[embeddedWidget]}` : `Elite ${viewDefinitions[currentView]}`;
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
// ---- Layout settings dialog (edit mode only) ----

const layoutDialog = byId("layout-settings-dialog");
const layoutSettingsButton = byId("layout-settings-button");
const layoutNameMessage = byId("layout-name-message");
const layoutStorageKeys = name => [`elite-dashboard-settings-v8:${name.toLowerCase()}`, `elite-dashboard-settings-v7:${name.toLowerCase()}`, `elite-dashboard-fullscreen:${name.toLowerCase()}`];

// Layout names saved in this browser, from their settings keys.
function knownLayouts() {
    const names = new Set([layoutProfile.toLowerCase()]);
    try {
        for (let index = 0; index < localStorage.length; index++) {
            const match = /^elite-dashboard-settings-v[78]:(.+)$/.exec(localStorage.key(index) || "");
            if (match) names.add(match[1]);
        }
    } catch { /* storage unavailable: only the current layout is known */ }
    return [...names].sort();
}
const layoutExists = name => knownLayouts().includes(name.toLowerCase());

function openLayout(name) {
    const url = new URL(window.location.href);
    url.searchParams.set("layout", name);
    window.location.assign(url);
}

// The name typed, cleaned as the URL will have it; null (with a message) when it can't be used for a copy or rename.
function newLayoutName() {
    const name = cleanProfileName(profileInput.value);
    profileInput.value = name;
    if (name.toLowerCase() === layoutProfile.toLowerCase()) {
        layoutNameMessage.textContent = "Type a different name first.";
        return null;
    }
    if (layoutExists(name) && !window.confirm(`A layout called "${name}" already exists in this browser. Replace it?`)) return null;
    return name;
}

// Copies this layout, including changes made in this edit session, under another name.
function copyLayoutTo(name) {
    const [, , fullscreenKey] = layoutStorageKeys(layoutProfile);
    const [settingsTarget, legacyTarget, fullscreenTarget] = layoutStorageKeys(name);
    localStorage.setItem(settingsTarget, JSON.stringify(dashboardSettings));
    localStorage.removeItem(legacyTarget);
    const fullscreen = localStorage.getItem(fullscreenKey);
    if (fullscreen === null) localStorage.removeItem(fullscreenTarget); else localStorage.setItem(fullscreenTarget, fullscreen);
}

function openLayoutDialog() {
    if (!layoutDialog) return;
    profileInput.value = layoutProfile;
    text("current-layout-name", layoutProfile);
    text("reset-view-name", viewDefinitions[currentView]);
    // Positions apply to passive display; touch mode stacks panels in a list instead.
    const other = layoutOrientation === "portrait" ? "landscape" : "portrait";
    const otherLayout = getViewSettings()[layoutKeyFor(other)];
    text("orientation-note", presentationMode === "display"
        ? `You are arranging this view for ${layoutOrientation} screens. ${other[0].toUpperCase()}${other.slice(1)} screens keep their own positions.`
        : "Positions apply to passive display; in touch mode panels are listed in order.");
    text("other-orientation-name", other);
    const copyOrientation = byId("copy-orientation-layout");
    if (copyOrientation) copyOrientation.hidden = presentationMode !== "display" || !otherLayout || !Object.keys(otherLayout).length;
    layoutNameMessage.textContent = "";
    byId("known-layouts")?.replaceChildren(...knownLayouts().map(name => Object.assign(document.createElement("option"), { value: name })));
    layoutDialog.showModal();
}

if (layoutSettingsButton) layoutSettingsButton.addEventListener("click", openLayoutDialog);
if (profileButton) profileButton.addEventListener("click", () => {
    const name = cleanProfileName(profileInput.value);
    if (name.toLowerCase() === layoutProfile.toLowerCase()) {
        layoutNameMessage.textContent = "That is the layout already open.";
        return;
    }
    openLayout(name);
});
// Like "Save as": the copy gets this session's changes, and this layout goes back to how it was before editing.
// (Switch to keeps them here, like Done; Rename moves them with the layout.)
byId("copy-layout-profile")?.addEventListener("click", () => {
    const name = newLayoutName();
    if (!name) return;
    copyLayoutTo(name);
    if (settingsBeforeEditing !== null) localStorage.setItem(settingsKey, settingsBeforeEditing);
    openLayout(name);
});
byId("rename-layout-profile")?.addEventListener("click", () => {
    const name = newLayoutName();
    if (!name) return;
    copyLayoutTo(name);
    layoutStorageKeys(layoutProfile).forEach(key => localStorage.removeItem(key));
    openLayout(name);
});
// Replaces this orientation's positions with the other's; part of the edit session, so Cancel undoes it.
byId("copy-orientation-layout")?.addEventListener("click", () => {
    const other = layoutOrientation === "portrait" ? "landscape" : "portrait";
    const source = getViewSettings()[layoutKeyFor(other)];
    if (!source) return;
    getViewSettings()[layoutKeyFor(layoutOrientation)] = JSON.parse(JSON.stringify(source));
    saveDashboardSettings();
    applyDashboardSettings();
    layoutDialog?.close();
});
// Back to the default panels and positions for this view; part of the edit session, so Cancel undoes it.
byId("reset-view-layout")?.addEventListener("click", () => {
    delete dashboardSettings.views?.[currentView];
    saveDashboardSettings();
    applyDashboardSettings();
    layoutDialog?.close();
});
if (presentationSelect) presentationSelect.addEventListener("change", () => {
    dashboardSettings.mode = presentationSelect.value;
    saveDashboardSettings();
    applyPresentationMode();
    applyDashboardSettings();
});
if (settingsButton) settingsButton.addEventListener("click", () => setConfigurationMode(!configurationMode));
if (cancelButton) cancelButton.addEventListener("click", cancelEditing);
// Escape finishes editing and keeps the changes, like Done; Cancel is the explicit way to discard them.
document.addEventListener("keydown", event => {
    if (event.key === "Escape" && configurationMode && !document.querySelector("dialog[open]")) setConfigurationMode(false);
});
document.querySelectorAll("[data-view-link]").forEach(link => link.addEventListener("click", event => {
    event.preventDefault();
    applyView(link.dataset.viewLink);
}));
window.addEventListener("hashchange", () => applyView(window.location.hash.slice(1).toLowerCase(), false));
window.addEventListener("resize", () => {
    const orientation = screenOrientation();
    const turned = orientation !== layoutOrientation;
    layoutOrientation = orientation;
    if (!dashboardSettings.mode || dashboardSettings.mode === "auto") {
        applyPresentationMode();
        applyDashboardSettings();
    } else if (turned) {
        applyDashboardSettings();
    }
});
document.querySelectorAll("[data-view-link]").forEach(link => {
    const url = new URL(link.href, window.location.href);
    url.search = window.location.search;
    link.href = url.pathname + url.search + url.hash;
});
// The grid's height decides how many rows each widget needs to stay legible, and it changes when the window
// resizes or the edit toolbar opens or wraps, so lay out again whenever it does.
let laidOutGridHeight = 0;
if (grid && "ResizeObserver" in window) {
    new ResizeObserver(() => {
        if (embeddedWidget || presentationMode !== "display" || grid.clientHeight === laidOutGridHeight) return;
        laidOutGridHeight = grid.clientHeight;
        requestAnimationFrame(() => {
            applyGridLayout();
            renderConfigurationControls();
        });
    }).observe(grid);
}
applyPresentationMode();
applyDashboardSettings();
applyView(currentView, false);

const fullscreenButton = byId("fullscreen");
// The toggle is a pressed button while the dashboard is fullscreen.
const updateFullscreenButton = () => {
    if (!fullscreenButton) return;
    const isFullscreen = Boolean(document.fullscreenElement);
    fullscreenButton.setAttribute("aria-pressed", String(isFullscreen));
    fullscreenButton.title = isFullscreen ? "Exit fullscreen" : "Fullscreen";
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
// Browsers only allow fullscreen after a user gesture, so if it was on last time and the automatic attempt is
// refused, the first tap or click anywhere (other than the toggle itself) restores it.
if (localStorage.getItem(fullscreenPreferenceKey) === "true" && !document.fullscreenElement) {
    window.setTimeout(() => document.documentElement.requestFullscreen().catch(() => {
        const restore = event => {
            if (event.target.closest?.("#fullscreen") || document.fullscreenElement) return;
            document.documentElement.requestFullscreen().catch(() => {});
        };
        document.addEventListener("pointerdown", restore, { once: true, capture: true });
    }), 0);
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

// Status.json's Health/Oxygen/Temperature describe the on-foot suit, not the ship. Ship hull comes
// from Loadout and HullDamage, whichever is newer.
let latestStatus = null;
let shipLoadout = null;
let cargoCapacity = 0;
let shipHull = { value: null, time: 0 };
const percent = value => typeof value === "number" ? `${Math.round(value * 100)}%` : "--";
const legalLabel = value => value ? value.replace(/([a-z])([A-Z])/g, (match, lower, upper) => `${lower} ${upper.toLowerCase()}`) : "--";
const legalWarnings = ["IllegalCargo", "Speeding", "Wanted", "Hostile", "PassengerWanted", "Warrant"];
const altitude = metres => metres >= 10000 ? `${fixed(metres / 1000, 1)} km` : `${money(Math.round(metres))} m`;

function updateShipHull(value, timestamp) {
    const time = new Date(timestamp).getTime();
    if (typeof value !== "number" || time < shipHull.time) return;
    shipHull = { value, time };
    renderShipStatus();
}

function renderShipStatus() {
    const status = latestStatus;
    if (status?.OnFoot) {
        text("ship-condition-label", "Suit");
        text("ship-condition", joined([`Health ${percent(status.Health)}`, `O2 ${percent(status.Oxygen)}`, status.Temperature ? `${fixed(status.Temperature, 0)} K` : ""]));
        return;
    }
    text("ship-condition-label", "Hull");
    text("ship-condition", joined([
        shipHull.value === null ? "" : percent(shipHull.value),
        status?.InMainShip ? (status.ShieldsUp ? "Shields up" : "Shields down") : ""
    ]));
    if (!status) return;
    const fuelCapacity = shipLoadout?.MainFuelCapacity;
    text("ship-fuel", fuelCapacity
        ? `${fixed(status.FuelMain, 1)} / ${fixed(fuelCapacity, 0)} t (${Math.round(status.FuelMain / fuelCapacity * 100)}%) / ${fixed(status.FuelReservoir, 2)} t res`
        : `${fixed(status.FuelMain, 1)} t main / ${fixed(status.FuelReservoir, 2)} t reserve`);
    text("ship-cargo", cargoCapacity ? `${fixed(status.Cargo, 0)} / ${fixed(cargoCapacity, 0)} t` : `${fixed(status.Cargo, 0)} t`);
}

connection.on("StatusData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    latestStatus = data;
    renderExobiologySpacing();
    renderShipStatus();
    text("ship-credits", data.Balance ? `${money(data.Balance)} CR` : "--");
    text("status-credits", credits(data.Balance));
    text("ship-firegroup", data.OnFoot
        ? data.SelectedWeapon || "--"
        : joined([typeof data.FireGroup === "number" ? `Group ${String.fromCharCode(65 + data.FireGroup)}` : "", data.SelectedWeapon]));
    text("location-legal", legalLabel(data.LegalState));
    text("location-destination", data.DestinationName && !data.DestinationName.startsWith("$") ? data.DestinationName : "--");
    text("location-approach", data.HasLatLong && !data.Docked ? `${altitude(data.Altitude)} / ${data.Heading}°` : "--");
    if (data.HasLatLong) text("location-coordinates", `${fixed(data.Latitude, 4)}, ${fixed(data.Longitude, 4)}`);
    const alerts = [
        data.LowFuel && "LOW FUEL", data.Overheating && "OVERHEATING", data.InDanger && "DANGER",
        data.BeingInterdicted && "INTERDICTION", data.FsdMassLocked && "MASS LOCKED",
        data.FsdCharging && "FSD CHARGING", data.SilentRunning && "SILENT RUNNING",
        data.LowHealth && "LOW HEALTH", data.LowOxygen && "LOW OXYGEN",
        data.VeryCold && "VERY COLD", data.VeryHot && "VERY HOT",
        legalWarnings.includes(data.LegalState) && legalLabel(data.LegalState).toUpperCase()
    ].filter(Boolean);
    text("ship-alerts", alerts.length ? alerts.join(" / ") : "Nominal");
    // On foot the ship flags and pips are all zero; keep showing the ship's last state.
    if (data.OnFoot) return;
    text("ship-pips", `SYS ${data.SystemPips ?? "--"} / ENG ${data.EnginePips ?? "--"} / WEP ${data.WeaponPips ?? "--"}`);
    document.querySelectorAll("#ship-flags [data-flag]").forEach(flag => flag.classList.toggle("on", Boolean(data[flag.dataset.flag])));
});

connection.on("LocationData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("location-system", data.SystemName || "Unknown system");
    text("status-system", data.SystemName || "Unknown system");
    if (data.SystemName) {
        routeSystem = data.SystemName;
        renderRouteRemaining();
    }
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
    text("location-station-faction", joined([data.Faction, data.Government, data.Allegiance]));
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
    // Ring classes such as "eRingClass_Icy" read as "Icy"; the reserve level applies to the rings and belts.
    const rings = (data.Rings || []).map(ring => (ring.RingClass || "").replace(/^eRingClass_/, "").replace(/([a-z])([A-Z])/g, "$1 $2"));
    text("location-rings", rings.length ? joined([`${rings.length} ring${rings.length === 1 ? "" : "s"}`, [...new Set(rings)].join(", "),
        data.ReserveLevel ? `${data.ReserveLevel.replace(/Resources$/, "")} reserves` : ""]) : "--");
});

connection.on("LoadoutData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    const shipName = data.ShipName || data.Ship || "Ship";
    text("ship-title", `${shipName} - Ship Information System`);
    text("status-ship", shipName);
    text("ship-jump-range", data.MaxJumpRange ? `${fixed(data.MaxJumpRange, 2)} ly` : "--");
    text("ship-rebuy", data.Rebuy ? `${money(data.Rebuy)} CR` : "--");
    shipLoadout = data;
    if (data.CargoCapacity) cargoCapacity = data.CargoCapacity;
    renderShipStatus();
    updateShipHull(data.HullHealth, data.LastUpdate);
    renderModules(data.Modules || []);
});

// Module health comes from the Loadout event, so it is as of the last outfitting, repair or login, not live.
function renderModules(modules) {
    const damaged = modules.filter(module => module.Health < 1).sort((a, b) => a.Health - b.Health);
    const off = modules.filter(module => !module.IsOn);
    text("ship-modules", modules.length ? joined([
        damaged.length ? `${damaged.length} damaged / lowest ${damaged[0].Item} ${percent(damaged[0].Health)}` : "All intact",
        off.length ? `${off.length} off` : ""
    ]) : "--");
    // Damaged modules first, so they stay visible when the list is cut short.
    renderList("ship-module-list", [...damaged, ...modules.filter(module => !(module.Health < 1))], module => joined([
        `${module.Slot}: ${module.Item}`,
        module.Health < 1 ? percent(module.Health) : "",
        module.IsOn ? "" : "OFF",
        // The journal counts power priority from 0; show only the non-default ones, as the game numbers them.
        module.Priority > 0 ? `Priority ${module.Priority + 1}` : "",
        module.AmmoInClip || module.AmmoInHopper ? `Ammo ${module.AmmoInClip} + ${module.AmmoInHopper}` : "",
        module.Engineering?.BlueprintName ? `G${module.Engineering.Level} ${module.Engineering.BlueprintName}` : "",
        module.Engineering?.ExperimentalEffect && module.Engineering.ExperimentalEffect !== "--" ? module.Engineering.ExperimentalEffect : ""
    ]), "No loadout data");
}

connection.on("MissionData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("activity-mission", data.LocalisedName || data.Name);
    latestMissionShown = true;
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
    if (data.Capacity) cargoCapacity = data.Capacity;
    text("ship-cargo", `${fixed(data.Total, 0)} / ${fixed(data.Capacity || cargoCapacity, 0)} t`);
    renderList("ship-cargo-list", data.Items, item => `${item.Name} x${item.Count}${item.IsStolen ? " / stolen" : ""}`, "Cargo hold empty");
});

connection.on("MaterialsData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    const materials = [...(data.Raw || []), ...(data.Manufactured || []), ...(data.Encoded || [])];
    // Materials at 90% or more of their grade's storage limit; collecting more of those is wasted.
    const fill = material => material.Maximum ? material.Count / material.Maximum : 0;
    const nearLimit = materials.filter(material => fill(material) >= 0.9).length;
    text("ship-materials", joined([
        `Raw ${data.Raw?.length ?? 0} / Manufactured ${data.Manufactured?.length ?? 0} / Encoded ${data.Encoded?.length ?? 0}`,
        nearLimit ? `${nearLimit} near limit` : ""
    ]));
    renderList("ship-material-list", [...materials].sort((a, b) => fill(b) - fill(a) || b.Count - a.Count), material => joined([
        material.Name,
        material.Maximum ? `${material.Count} / ${material.Maximum}` : `${material.Count}`,
        material.Grade ? `G${material.Grade}` : "",
        material.Category
    ]), "No materials data");
});

connection.on("CombatData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    if (data.Operation === "HullDamage" && !data.IsFighter) updateShipHull(data.HullHealth, data.LastUpdate);
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
    text("location-government", joined([data.Government, data.SystemAllegiance]));
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

let activeMissions = [];
let latestMissionShown = false;

connection.on("MissionCollectionData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    activeMissions = data.Active || [];
    // The Activity panel shows the latest accepted mission once there is one; until then, the count.
    if (!latestMissionShown) text("activity-mission", activeMissions.length ? `${activeMissions.length} active` : "No active missions");
    renderMissions();
    const results = [
        ...(data.Complete || []).map(mission => ({ mission, result: "Completed" })),
        ...(data.Failed || []).map(mission => ({ mission, result: "Failed" }))
    ];
    renderList("missions-results", results, item => joined([item.result, item.mission.Name, item.mission.Reward ? `${money(item.mission.Reward)} CR` : ""]),
        "No completed or failed missions");
});

const knownDate = value => value && new Date(value).getUTCFullYear() > 1 ? new Date(value) : null;

function timeLeft(expiry) {
    const minutes = Math.floor((expiry - Date.now()) / 60000);
    if (minutes <= 0) return "expired";
    const days = Math.floor(minutes / 1440), hours = Math.floor(minutes % 1440 / 60), mins = minutes % 60;
    return days ? `${days}d ${hours}h` : hours ? `${hours}h ${mins}m` : `${mins}m`;
}

function renderMissions() {
    const missions = activeMissions;
    text("missions-count", missions.length ? `${missions.length} mission${missions.length === 1 ? "" : "s"}` : "None");
    const next = missions.find(mission => knownDate(mission.Expiry));
    text("missions-next", next ? `${next.Name} / ${timeLeft(knownDate(next.Expiry))}` : "--");
    const reward = missions.reduce((total, mission) => total + (mission.Reward || 0), 0);
    text("missions-reward", reward ? `${money(reward)} CR` : "--");
    const destinations = new Map();
    missions.forEach(mission => mission.DestinationSystem && destinations.set(mission.DestinationSystem, (destinations.get(mission.DestinationSystem) || 0) + 1));
    text("missions-destinations", destinations.size
        ? [...destinations].sort((a, b) => b[1] - a[1]).map(([system, count]) => count > 1 ? `${system} (${count})` : system).join(", ")
        : "--");
    renderList("missions-list", missions, mission => {
        const expiry = knownDate(mission.Expiry);
        return joined([
            mission.Name,
            mission.DestinationStation ? `${mission.DestinationSystem} / ${mission.DestinationStation}` : mission.DestinationSystem,
            mission.Count && mission.Commodity ? `${mission.Count} x ${mission.Commodity}` : "",
            mission.Reward ? `${money(mission.Reward)} CR` : "",
            expiry ? timeLeft(expiry) : ""
        ]);
    }, "No active missions");
}
window.setInterval(() => { if (activeMissions.length) renderMissions(); }, 60000);

const credits = value => {
    if (typeof value !== "number" || !value) return "--";
    return value >= 1e9 ? `${(value / 1e9).toFixed(2)}B CR` : value >= 1e6 ? `${(value / 1e6).toFixed(2)}M CR` : value >= 1e4 ? `${Math.round(value / 1e3)}k CR` : `${money(value)} CR`;
};

connection.on("SystemExplorationData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    latestSystemExploration = data;
    renderExobiology();
    const bodies = data.Bodies || [];
    const planets = bodies.filter(body => body.BodyType === "Planet");
    const scannedBodies = bodies.filter(body => body.BodyType);
    const shortName = name => data.SystemName && name?.startsWith(`${data.SystemName} `) ? name.slice(data.SystemName.length + 1) : name || "--";

    text("exploration-discovery", data.AllBodiesFound
        ? `All ${data.BodyCount} scanned`
        : data.BodyCount ? `${scannedBodies.length} of ${data.BodyCount} scanned / ${fixed(data.DiscoveryProgress * 100, 0)}%`
        : scannedBodies.length ? `${scannedBodies.length} scanned` : "No discovery scan yet");
    text("exploration-value", credits(data.EstimatedValue));
    const best = planets.filter(body => !body.IsMapped).sort((a, b) => b.EstimatedMappedValue - a.EstimatedMappedValue)[0];
    text("exploration-best", best && best.EstimatedMappedValue >= 300000
        ? `${shortName(best.BodyName)} / ${credits(best.EstimatedMappedValue)}`
        : planets.length ? "Nothing high value" : "--");
    text("exploration-mapped", planets.length ? `${planets.filter(body => body.IsMapped).length} of ${planets.length} planets` : "--");
    const signals = new Map();
    bodies.forEach(body => (body.Signals || []).forEach(signal => signals.set(signal.Type, (signals.get(signal.Type) || 0) + signal.Count)));
    text("exploration-signals", signals.size ? [...signals].map(([type, count]) => `${type} ${count}`).join(" / ") : "--");

    const worth = body => Math.max(body.EstimatedValue, body.EstimatedMappedValue);
    renderList("exploration-body-list", [...scannedBodies].sort((a, b) => worth(b) - worth(a)), body => joined([
        shortName(body.BodyName),
        (body.PlanetClass || (body.StarType ? `${body.StarType} star` : "")).replace("Sudarsky class", "Class"),
        body.TerraformState === "Terraformable" ? "Terraformable" : "",
        body.DistanceFromArrivalLs ? `${money(Math.round(body.DistanceFromArrivalLs))} Ls` : "",
        credits(body.IsMapped || body.BodyType === "Star" ? body.EstimatedValue : body.EstimatedMappedValue) + (body.IsMapped || body.BodyType === "Star" ? "" : " mapped"),
        body.WasDiscovered ? "" : "First discovery",
        body.IsMapped ? "Mapped" : "",
        ...(body.Signals || []).map(signal => `${signal.Type} ${signal.Count}`)
    ]), "No bodies scanned in this system");
});

// Organic sampling. Spacing is measured live from the commander to each earlier sample of the species,
// using Status positions; samples replayed at startup have no position, so only the rule is shown.
let exobiology = null;
let latestSystemExploration = null;

function surfaceDistance(from, to, radius) {
    const rad = Math.PI / 180;
    const dLat = (to.Latitude - from.Latitude) * rad, dLon = (to.Longitude - from.Longitude) * rad;
    const a = Math.sin(dLat / 2) ** 2 + Math.cos(from.Latitude * rad) * Math.cos(to.Latitude * rad) * Math.sin(dLon / 2) ** 2;
    return 2 * radius * Math.asin(Math.min(1, Math.sqrt(a)));
}

function renderExobiologySpacing() {
    const current = exobiology?.Current;
    const status = latestStatus;
    if (!current) {
        text("exobiology-spacing", "--");
        return;
    }
    const samples = current.Samples || [];
    if (status?.BodyName && current.BodyName && status.BodyName !== current.BodyName) {
        text("exobiology-spacing", `Sampling on ${current.BodyName}`);
    } else if (!samples.length || !status?.HasLatLong || !(status.PlanetRadius > 0)) {
        text("exobiology-spacing", `${money(current.SampleDistance)} m apart`);
    } else {
        const nearest = Math.round(Math.min(...samples.map(sample => surfaceDistance(sample, status, status.PlanetRadius))));
        text("exobiology-spacing", nearest >= current.SampleDistance
            ? `Clear / ${money(nearest)} m from nearest`
            : `Too close / ${money(nearest)} of ${money(current.SampleDistance)} m`);
    }
}

function renderExobiology() {
    if (!exobiology) return;
    const current = exobiology.Current;
    const unsold = exobiology.Unsold || [];
    text("exobiology-species", current ? current.Variant || current.Species : "No sample in progress");
    text("exobiology-progress", current ? joined([`${current.SamplesTaken} of 3`, credits(current.EstimatedValue)]) : "--");
    renderExobiologySpacing();
    text("exobiology-unsold", unsold.length ? `${unsold.length} species / ${credits(exobiology.UnsoldEstimatedValue)}` : "None");
    text("exobiology-last-sale", credits(exobiology.LastSaleValue));

    // Biological signals on the body being sampled (or stood on), against species analysed there.
    const bodyName = current?.BodyName || latestStatus?.BodyName;
    const body = (latestSystemExploration?.Bodies || []).find(item => item.BodyName === bodyName);
    const signals = (body?.Signals || []).find(signal => signal.Type === "Biological")?.Count;
    const analysed = unsold.filter(scan => scan.BodyName === bodyName).length;
    text("exobiology-body", bodyName ? joined([bodyName, signals ? `${analysed} of ${signals} analysed` : analysed ? `${analysed} analysed` : ""]) : "--");

    renderList("exobiology-unsold-list", unsold, scan => joined([scan.Species, scan.BodyName, credits(scan.EstimatedValue)]), "No unsold organic data");
}

connection.on("ExobiologyData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    exobiology = data;
    renderExobiology();
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

// The game reports the jumps left; the light years left add up the plotted jumps after the current system.
let routeStops = [];
let routeJumpsLeft = null;
let routeSystem = null;

function renderRouteRemaining() {
    const index = routeStops.findIndex(stop => stop.SystemName === routeSystem);
    const distance = index < 0 ? 0 : routeStops.slice(index + 1).reduce((total, stop) => total + (stop.JumpDistance || 0), 0);
    text("route-remaining", joined([
        routeJumpsLeft === null ? "" : `${routeJumpsLeft} jumps`,
        distance ? `${fixed(distance, 1)} ly` : ""
    ]));
}

connection.on("RouteTargetData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data) || !data.SystemName) return;
    text("route-next", joined([data.SystemName, data.StarClass]));
    routeJumpsLeft = typeof data.RemainingJumps === "number" ? data.RemainingJumps : null;
    renderRouteRemaining();
});

connection.on("JumpData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data) || !data.DestinationSystemName) return;
    text("route-last-jump", data.JumpComplete ? joined([data.DestinationSystemName, `${fixed(data.JumpDistance, 2)} ly`, `${fixed(data.FuelUsed, 2)} t used`]) : joined(["Jumping", data.DestinationSystemName]));
    if (data.JumpComplete) {
        routeSystem = data.DestinationSystemName;
        renderRouteRemaining();
    }
});

connection.on("NavRouteData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    routeStops = data.NavRouteActive && Array.isArray(data.Stops) ? data.Stops : [];
    renderRouteRemaining();
});
connection.on("PreviousNavRoute", function () {});

// ---- Commander: ranks, reputation, Powerplay, engineers and unredeemed combat earnings ----

const eliteRanks = ["Elite", "Elite I", "Elite II", "Elite III", "Elite IV", "Elite V"];
const rankTitles = {
    Combat: ["Harmless", "Mostly Harmless", "Novice", "Competent", "Expert", "Master", "Dangerous", "Deadly", ...eliteRanks],
    Trade: ["Penniless", "Mostly Penniless", "Peddler", "Dealer", "Merchant", "Broker", "Entrepreneur", "Tycoon", ...eliteRanks],
    Explore: ["Aimless", "Mostly Aimless", "Scout", "Surveyor", "Trailblazer", "Pathfinder", "Ranger", "Pioneer", ...eliteRanks],
    Soldier: ["Defenceless", "Mostly Defenceless", "Rookie", "Soldier", "Gunslinger", "Warrior", "Gladiator", "Deadeye", ...eliteRanks],
    Exobiologist: ["Directionless", "Mostly Directionless", "Compiler", "Collector", "Cataloguer", "Taxonomist", "Ecologist", "Geneticist", ...eliteRanks],
    CQC: ["Helpless", "Mostly Helpless", "Amateur", "Semi Professional", "Professional", "Champion", "Hero", "Legend", ...eliteRanks],
    Federation: ["None", "Recruit", "Cadet", "Midshipman", "Petty Officer", "Chief Petty Officer", "Warrant Officer", "Ensign", "Lieutenant",
        "Lieutenant Commander", "Post Commander", "Post Captain", "Rear Admiral", "Vice Admiral", "Admiral"],
    Empire: ["None", "Outsider", "Serf", "Master", "Squire", "Knight", "Lord", "Baron", "Viscount", "Count", "Earl", "Marquis", "Duke", "Prince", "King"]
};
const rankLabels = { Explore: "Exploration", Soldier: "Mercenary", Exobiologist: "Exobiology" };
const rankTitle = rank => rankTitles[rank.Name]?.[rank.Rank] ?? `Rank ${rank.Rank}`;
const rankText = rank => joined([rankTitle(rank), rank.Progress ? `${rank.Progress}%` : ""]);
// The game's reputation bands, from -100 to 100.
const reputationLabel = value => value <= -90 ? "Hostile" : value < -35 ? "Unfriendly" : value < 4 ? "Neutral" : value < 35 ? "Cordial" : value < 90 ? "Friendly" : "Allied";

connection.on("CommanderData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    const ranks = data.Ranks || [];
    const combat = ranks.find(rank => rank.Name === "Combat");
    text("commander-combat", combat ? rankText(combat) : "--");
    const pledged = Math.floor((data.TimePledgedSeconds || 0) / 86400);
    text("commander-powerplay", data.Power ? joined([data.Power, `Rank ${data.PowerplayRank}`, `${money(data.Merits)} merits`, pledged ? `${pledged} days` : ""]) : "Not pledged");
    text("commander-reputation", (data.Reputation || []).length
        ? data.Reputation.map(item => `${item.Superpower.slice(0, 3)} ${reputationLabel(item.Reputation)}`).join(" / ") : "--");
    const engineers = data.Engineers || [];
    const unlocked = engineers.filter(engineer => engineer.Progress === "Unlocked");
    const invited = engineers.filter(engineer => engineer.Progress === "Invited" || engineer.Progress === "Acquainted");
    text("commander-engineers", engineers.length ? joined([`${unlocked.length} unlocked`, invited.length ? `${invited.length} invited` : ""]) : "--");
    renderList("commander-rank-list", ranks, rank => `${rankLabels[rank.Name] || rank.Name}: ${rankText(rank)}`, "No rank data");
    renderList("commander-engineer-list", [...unlocked].sort((a, b) => b.Rank - a.Rank).concat(invited), engineer => joined([
        engineer.Name,
        engineer.Progress === "Unlocked" ? `Grade ${engineer.Rank}` : engineer.Progress,
        engineer.Progress === "Unlocked" && engineer.Rank < 5 && engineer.RankProgress ? `${engineer.RankProgress}% to next` : ""
    ]), "No engineer data");
});

connection.on("CombatEarningsData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("commander-bounties", data.UnredeemedBounties ? `${credits(data.UnredeemedBounties)} / ${data.BountyCount} kill${data.BountyCount === 1 ? "" : "s"}` : "None");
    text("commander-bonds", data.UnredeemedBonds ? `${credits(data.UnredeemedBonds)} / ${data.BondCount} bond${data.BondCount === 1 ? "" : "s"}` : "None");
});

// ---- Mining and trade ----

let latestMining = null;
function renderMining() {
    const data = latestMining;
    if (!data) return;
    const prospect = data.LastProspect;
    text("trade-prospect", prospect ? joined([
        prospect.Content,
        prospect.Materials?.[0] ? `${prospect.Materials[0].Name} ${fixed(prospect.Materials[0].Percent, 1)}%` : "",
        prospect.Motherlode ? `Motherlode: ${prospect.Motherlode}` : ""
    ]) : "--");
    // Tons per hour since the first ton refined this session, once there is enough to measure.
    const hours = knownDate(data.FirstRefined) ? (Date.now() - knownDate(data.FirstRefined).getTime()) / 3600000 : 0;
    text("trade-refined", data.TotalRefined ? joined([`${data.TotalRefined} t`, hours > 0.05 ? `${fixed(data.TotalRefined / hours, 0)} t/h` : ""]) : "--");
    renderList("trade-refined-list", data.Refined, item => `${item.Name} ${item.Count} t`, "Nothing refined this session");
    renderList("trade-prospect-list", prospect?.Materials, material => `${material.Name} ${fixed(material.Percent, 1)}%`, "No asteroid prospected");
}
window.setInterval(renderMining, 60000);

connection.on("MiningData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    latestMining = data;
    renderMining();
});

connection.on("TradeData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("trade-profit", data.SessionSales ? joined([credits(data.SessionProfit), `${credits(data.SessionSales)} sales`]) : "--");
    text("trade-last-sale", data.LastSale);
    const prices = data.CargoPrices || [];
    const best = prices[0];
    text("trade-best-price", best ? joined([best.Name, `${money(best.SellPrice)} CR/t`, best.Demand ? "" : "no demand"]) : "--");
    text("trade-market", joined([data.MarketStation, data.MarketSystem]));
    renderList("trade-price-list", prices, price => joined([
        `${price.Name} x${price.Count}`,
        `${money(price.SellPrice)} CR/t`,
        price.MeanPrice ? `${price.SellPrice >= price.MeanPrice ? "+" : ""}${fixed((price.SellPrice / price.MeanPrice - 1) * 100, 0)}% vs average` : "",
        price.Demand ? `demand ${money(price.Demand)}` : "no demand"
    ]), "No market data for your cargo");
});

// ---- Fleet carrier ----

let latestCarrier = null;
function renderCarrierJump() {
    const data = latestCarrier;
    if (!data) return;
    const departure = knownDate(data.JumpDeparture);
    if (!data.JumpDestinationSystem || !departure) {
        text("carrier-jump", "None scheduled");
        return;
    }
    const seconds = Math.round((departure.getTime() - Date.now()) / 1000);
    text("carrier-jump", seconds > 0
        ? `${data.JumpDestinationSystem} in ${Math.floor(seconds / 60)}:${String(seconds % 60).padStart(2, "0")}`
        : `Jumped to ${data.JumpDestinationSystem}`);
}
window.setInterval(renderCarrierJump, 1000);

connection.on("CarrierData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data) || !data.Name && !data.JumpDestinationSystem) return;
    latestCarrier = data;
    text("carrier-name", joined([data.Name, data.Callsign, data.IsPendingDecommission ? "DECOMMISSIONING" : ""]));
    text("carrier-fuel", data.Name ? `${money(data.FuelLevel)} / 1,000 t` : "--");
    text("carrier-balance", credits(data.Balance));
    text("carrier-space", data.TotalCapacity ? `${money(data.FreeSpace)} of ${money(data.TotalCapacity)} t` : "--");
    // The game reports a negative available balance when the reserve is more than the carrier holds.
    text("carrier-reserve", data.Balance ? joined([
        `${credits(data.ReserveBalance)} reserved`,
        data.AvailableBalance < 0 ? `${credits(-data.AvailableBalance)} short` : `${credits(data.AvailableBalance)} free`
    ]) : "--");
    text("carrier-access", data.DockingAccess);
    text("carrier-updated", new Date(data.LastUpdate).toLocaleString());
    renderCarrierJump();
});

// ---- On foot: suit loadout and ship locker, in the Ship panel ----

connection.on("OnFootData", envelope => {
    const data = envelope.Data;
    if (!hasTimestamp(data)) return;
    text("ship-suit", data.SuitName ? joined([data.SuitName, data.LoadoutName, ...(data.Weapons || [])]) : "--");
    renderList("ship-locker-list", data.ShipLocker, item => `${item.Name} x${item.Count}`, "No ship locker data");
});

startPanelConnection();

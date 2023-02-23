"use strict";

const connection = new signalR.HubConnectionBuilder()
	.withUrl("/gamedataupdatehub", { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
	.withAutomaticReconnect()
	.build();
	console.log(connection);

function createElement(parentEl = document.body, tagName = 'div') {
	const el = document.createElement(tagName);
	parentEl.appendChild(el);
	return el;
}

// The class representing the jump destination system (or better, the main star of it)
class SystemClass {
	el;
	labelEl;
	starClass;

	constructor(parentEl, name, starClass, ID) {
		// Set the main properties
		this.el                  = createElement(parentEl, 'li');
		this.labelEl             = createElement(this.el, 'span');
		this.labelEl.innerHTML   = name;
		this.starClass           = starClass;
		this.ID                  = ID;

		// Determine additional properties
		if (/\d/.test(name))         { this.el.classList.add('compact')      ; }
		if (this.isScoopable())      { this.el.classList.add('scoopable')    ; }
		if (this.isSupercharge())    { this.el.classList.add('supercharge')  ; }
		if (this.isWhiteDwarf())     { this.el.classList.add('white-dwarf')  ; }
		if (this.isBlackHole())      { this.el.classList.add('black-hole')   ; }

		// Add CSS class properties for styling the star item in the HTML page.
		this.el.classList.add('Class_' + this.starClass);
	}

	isScoopable() {return [ 'O', 'B', 'A', 'F', 'G', 'K', 'M' ].includes(this.starClass); }
	isSupercharge() {return /^(N|D.*)$/.test(this.starClass); }
	isWhiteDwarf() {return /^D/.test(this.starClass); }
	isBlackHole() {return this.starClass === 'H'; }
	getPointSize() {return parseInt(window.getComputedStyle(this.el,'::after').width, 10); }
	getDomElementPosition() {
		return {
			left: this.el.offsetLeft + this.el.offsetWidth  - (this.getPointSize() / 2),
			top:  this.el.offsetTop  + this.el.offsetHeight - (this.getPointSize() / 2),
		};
	}
}

class RouteClass {
	el;
	routeEl;
	systemList;
	currentSystemName;
	currentLocation;
	centerViewTimeout;
	onArrival;

	constructor(parentEl) {
		this.el = createElement(parentEl);
		this.el.classList.add('route-container');

		this.routeEl = createElement(this.el, 'ul');
		this.systemList = {};
		this.steps;
		this.currentSystemName;
	}

	setSteps(steps) {
		// Builds the list of systems in the planned route
		this.routeEl.innerHTML = '';
		this.systemList = {};
		this.steps = steps;		
		steps.forEach((step, index) => {
			const system = new SystemClass(this.routeEl, step.SystemName, step.Class, step.SystemId);
			this.systemList[step.SystemName] = system;
			if (this.currentLocation) {
				if (this.currentLocation == step.SystemName) {
					this.currentSystemName = this.currentLocation;
					system.el.classList.add('current');
					this.centerView();
					this.checkArrival();
				}
			}
		});
	}

	clearRoute() {
		this.routeEl.innerHTML = '';
		this.systemList = {};
	}

	setCurrentSystem(systemName) {		
		// Find the previous current system and remove the "current" class from it
		if (this.currentSystemName && this.systemList[this.currentSystemName]) {
			this.systemList[this.currentSystemName].el.classList.remove('current')
		}
		this.currentLocation = systemName;
		// If theres a system in the list with the systemName, set it as "current"
		// and update the view.
		if (systemName && this.systemList[systemName]) {
			const system = this.systemList[systemName];
			system.el.classList.remove('jumping');
			system.el.classList.add('current');
			this.centerView();
			this.checkArrival();
			this.currentSystemName = systemName;
		}
	}

	getRemainingJump() {
		return this.steps.length -1 - this.steps.findIndex(step => step.SystemName === this.currentSystemName);
	}

	checkArrival() {
		if (this.isArrival()) {
			if (typeof this.onArrival === 'function') {
				this.onArrival(this.currentSystemName);
			}
		}
	}

	isArrival() {
		return this.getRemainingJump() === 0;
	}

	jump(systemName) {
		if (this.currentSystemName && this.systemList[this.currentSystemName]) {
			this.systemList[this.currentSystemName].el.classList.remove('current')
		}

		if (systemName && this.systemList[systemName]) {
			this.systemList[systemName].el.classList.add('jumping');
			this.currentSystemName = systemName;
			this.centerView(550);
		}
	}

	centerView(delay = 0) {
		clearTimeout(this.centerViewTimeout);

		this.centerViewTimeout = setTimeout(() => {
			if (this.currentSystemName && this.systemList[this.currentSystemName]) {
				const currentSystemElementPosition = this.systemList[this.currentSystemName].getDomElementPosition();
				const newScrollPosition = {
					left: currentSystemElementPosition.left - (this.el.offsetWidth / 2),
					top:	currentSystemElementPosition.top - (this.el.offsetHeight / 2),
				};
				this.el.scrollTo({left: newScrollPosition.left, top: newScrollPosition.top, behavior: "smooth"});
			}
		}, delay);
	}
}
const route	 = new RouteClass();

//---import GuiClass		 from './modules/Gui.js';
class GuiClass {
	config = {};
	autohideTimeout;

	constructor(config = {}) {
		this.setState(config);
	}

	setState(config) {
		this.config = config;

		typeof config.guiScale   !== 'undefined' && this.setScale(config.guiScale);
		typeof config.autohide   !== 'undefined' && this.setAutohide(config.autohide.enabled);
		typeof config.hide       !== 'undefined' && this.setHide(config.hide);
		typeof config.compact    !== 'undefined' && this.setCompact(config.compact);
		typeof config.themeColor !== 'undefined' && this.setColor(config.themeColor);
		typeof config.shadow     !== 'undefined' && this.setShadow(config.shadow);
		typeof config.fullColor  !== 'undefined' && this.setFullColor(config.fullColor);
	}

	setScale(scale) {
		document.documentElement.style.setProperty('--gui-scale', scale);
		this.config.guiScale = scale;
	}

	setCompact(compact) {
		document.documentElement.style.setProperty('--compact', compact ? 'var(--on)' : 'var(--off)');
		this.config.compact = compact;
	}

	setAutohide(autohide) {
		document.documentElement.classList.toggle('gui-autohide', autohide);
		this.config.autohide.enabled = autohide;
	}

	setHide(hide) {
		document.documentElement.classList.toggle('gui-hidden', hide);
		this.config.hide = hide;
	}

	setColor(color) {
		document.documentElement.style.setProperty('--color-theme', color);
		this.config.themeColor = color;
	}

	setShadow(isShadowOn) {
		document.documentElement.style.setProperty('--color-background', isShadowOn ? '#000C' : 'transparent');
		this.config.shadow = isShadowOn;
	}

	setFullColor(isFullColored) {
		document.documentElement.style.setProperty('--color-off', isFullColored ? 'var(--color-theme)' : 'var(--color-on)');
		document.documentElement.style.setProperty('--color-off-alpha', isFullColored ? '.5' : '.4');
		this.config.fullColor = isFullColored;
	}

	resetAutohideTimeout() {
		this.clearAutohideTimeout();
		if (this.config.autohide.delay) {
			this.autohideTimeout = setTimeout(() => document.documentElement.classList.add('gui-autohide--timeout'), this.config.autohide.delay * 1000);
		}
	}

	clearAutohideTimeout() {
		clearTimeout(this.autohideTimeout);
		document.documentElement.classList.remove('gui-autohide--timeout');
	}
}
const gui		 = new GuiClass();

////const info		= new Info(createElement());
window.route	= route;
window.gui		= gui;
window.addEventListener('resize', () => route.centerView() );

//[route.el, info.el].forEach(el => el.classList.add('gui'));
[route.el].forEach(el => el.classList.add('gui'));


connection.start().catch(function (err) {
		return console.error(err.toString());
});

connection.on("NavRouteData", function (NavRouteData) {
		var data = JSON.parse(NavRouteData);
		if (data != null) {
			console.log(data);
			if (data.NavRouteActive = true && data.Stops.length > 0) {

				console.log("Processing " + data.Stops.length + " stops");
				route.setSteps(data.Stops);
				console.log("called SET STEPS");
				console.log(route);

			} else {
				route.clearRoute();
			}
		}
});

connection.on("LocationData", function (LocationData) {
		var data = JSON.parse(LocationData);
		if (data != null) {
				console.log(data);
				if (data.SystemName != null) {
					route.setCurrentSystem(data.SystemName);
				};			 
		}
});

connection.on("JumpData", function (JumpData) {

	var data = JSON.parse(JumpData);
	if (data != null) {
		console.log(data);		
		if (data.DestinationSystemName != null) { route.jump(data.DestinationSystemName); };
	}
});


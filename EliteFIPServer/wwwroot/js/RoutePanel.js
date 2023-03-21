"use strict";

const connection = new signalR.HubConnectionBuilder()
	.withUrl("/gamedataupdatehub", { skipNegotiation: true, transport: signalR.HttpTransportType.WebSockets })
	.withAutomaticReconnect()
	.build();

const roundAccurately = (number, decimalPlaces) => Number(Math.round(number + "e" + decimalPlaces) + "e-" + decimalPlaces);

connection.on("LocationData", function (LocationData) {

	var data = JSON.parse(LocationData);
	if (data != null) {
		console.log(data);
		if (data.SystemName != null) {			
			route.setCurrentSystem(data.SystemName);
		};		
	}
});

connection.on("NavRouteData", function (NavRouteData) {
	var data = JSON.parse(NavRouteData);
	if (data != null) {
		console.log("New Nav Route");
		console.log(data);
		if (data.NavRouteActive = true && data.Stops.length > 0) {
			route.setSteps(data.Stops);
		} else {
			route.clearRoute();
		}
	}
});

connection.on("PreviousNavRoute", function (PrevRouteData) {
	var data = JSON.parse(PrevRouteData);
	if (data != null) {
		console.log("Previous Nav Route");
		console.log(data);
		if (data.Stops.length > 0) {
			prevroute.setSteps(data.Stops);
			prevroute.setCurrentSystem(data.LastSystemReached);
		} 
	}
});

connection.on("JumpData", function (JumpData) {

	var data = JSON.parse(JumpData);
	if (data != null) {
		console.log(data);		
		if (data.DestinationSystemName != null) {
			if (data.JumpComplete != null && data.JumpComplete == false) {
				route.jump(data.DestinationSystemName);
			}

		};
	}
});

connection.start().catch(function (err) {
	return console.error(err.toString());
});

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
		this.el = createElement(parentEl, 'li');
		this.labelEl = createElement(this.el, 'span');
		this.labelEl.innerHTML = name;
		this.starClass = starClass;
		this.ID = ID;

		// Determine additional properties
		if (/\d/.test(name)) { this.el.classList.add('compact'); }
		if (this.isScoopable()) { this.el.classList.add('scoopable'); }
		if (this.isSupercharge()) { this.el.classList.add('supercharge'); }
		if (this.isWhiteDwarf()) { this.el.classList.add('white-dwarf'); }
		if (this.isBlackHole()) { this.el.classList.add('black-hole'); }

		// Add CSS class properties for styling the star item in the HTML page.
		this.el.classList.add('Class_' + this.starClass);
	}

	isScoopable() { return ['O', 'B', 'A', 'F', 'G', 'K', 'M'].includes(this.starClass); }
	isSupercharge() { return /^(N|D.*)$/.test(this.starClass); }
	isWhiteDwarf() { return /^D/.test(this.starClass); }
	isBlackHole() { return this.starClass === 'H'; }
	getPointSize() { return parseInt(window.getComputedStyle(this.el, '::after').width, 10); }
	getDomElementPosition() {
		return {
			left: this.el.offsetLeft + this.el.offsetWidth - (this.getPointSize() / 2),
			top: this.el.offsetTop + this.el.offsetHeight - (this.getPointSize() / 2),
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
		return this.steps.length - 1 - this.steps.findIndex(step => step.SystemName === this.currentSystemName);
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
					top: currentSystemElementPosition.top - (this.el.offsetHeight / 2),
				};
				this.el.scrollTo({ left: newScrollPosition.left, top: newScrollPosition.top, behavior: "smooth" });
			}
		}, delay);
	}
}


const route = new RouteClass(document.getElementById("NavRoute"));
const prevroute = new RouteClass(document.getElementById("PrevNavRoute"));

window.route = route;
window.addEventListener('resize', () => route.centerView());





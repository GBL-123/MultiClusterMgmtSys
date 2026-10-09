//#region \0rolldown/runtime.js
var e = Object.defineProperty, t = (t, n) => {
	let r = {};
	for (var i in t) e(r, i, {
		get: t[i],
		enumerable: !0
	});
	return n || e(r, Symbol.toStringTag, { value: "Module" }), r;
}, n = function(e, t) {
	return n = Object.setPrototypeOf || { __proto__: [] } instanceof Array && function(e, t) {
		e.__proto__ = t;
	} || function(e, t) {
		for (var n in t) Object.prototype.hasOwnProperty.call(t, n) && (e[n] = t[n]);
	}, n(e, t);
};
function r(e, t) {
	if (typeof t != "function" && t !== null) throw TypeError("Class extends value " + String(t) + " is not a constructor or null");
	n(e, t);
	function r() {
		this.constructor = e;
	}
	e.prototype = t === null ? Object.create(t) : (r.prototype = t.prototype, new r());
}
//#endregion
//#region node_modules/zrender/lib/core/env.js
var i = function() {
	function e() {
		this.firefox = !1, this.ie = !1, this.edge = !1, this.newEdge = !1, this.weChat = !1;
	}
	return e;
}(), a = new (function() {
	function e() {
		this.browser = new i(), this.node = !1, this.wxa = !1, this.worker = !1, this.svgSupported = !1, this.touchEventsSupported = !1, this.pointerEventsSupported = !1, this.domSupported = !1, this.transformSupported = !1, this.transform3dSupported = !1, this.hasGlobalWindow = typeof window < "u";
	}
	return e;
}())();
typeof wx == "object" && typeof wx.getSystemInfoSync == "function" ? (a.wxa = !0, a.touchEventsSupported = !0) : typeof document > "u" && typeof self < "u" ? a.worker = !0 : !a.hasGlobalWindow || "Deno" in window || typeof navigator < "u" && typeof navigator.userAgent == "string" && navigator.userAgent.indexOf("Node.js") > -1 ? (a.node = !0, a.svgSupported = !0) : o(navigator.userAgent, a);
function o(e, t) {
	var n = t.browser, r = e.match(/Firefox\/([\d.]+)/), i = e.match(/MSIE\s([\d.]+)/) || e.match(/Trident\/.+?rv:(([\d.]+))/), a = e.match(/Edge?\/([\d.]+)/), o = /micromessenger/i.test(e);
	if (r && (n.firefox = !0, n.version = r[1]), i && (n.ie = !0, n.version = i[1]), a && (n.edge = !0, n.version = a[1], n.newEdge = +a[1].split(".")[0] > 18), o && (n.weChat = !0), t.svgSupported = typeof SVGRect < "u", t.touchEventsSupported = "ontouchstart" in window && !n.ie && !n.edge, t.pointerEventsSupported = "onpointerdown" in window && (n.edge || n.ie && +n.version >= 11), t.domSupported = typeof document < "u") {
		var s = document.documentElement.style;
		t.transform3dSupported = (n.ie && "transition" in s || n.edge || "WebKitCSSMatrix" in window && "m11" in new WebKitCSSMatrix() || "MozPerspective" in s) && !("OTransition" in s), t.transformSupported = t.transform3dSupported || n.ie && +n.version >= 9;
	}
}
var s = "12px sans-serif", c = 20, l = 100, u = "007LLmW'55;N0500LLLLLLLLLL00NNNLzWW\\\\WQb\\0FWLg\\bWb\\WQ\\WrWWQ000CL5LLFLL0LL**F*gLLLL5F0LF\\FFF5.5N";
function d(e) {
	var t = {};
	if (typeof JSON > "u") return t;
	for (var n = 0; n < e.length; n++) {
		var r = String.fromCharCode(n + 32);
		t[r] = (e.charCodeAt(n) - c) / l;
	}
	return t;
}
var f = d(u), p = {
	createCanvas: function() {
		return typeof document < "u" && document.createElement("canvas");
	},
	measureText: (function() {
		var e, t;
		return function(n, r) {
			if (!e) {
				var i = p.createCanvas();
				e = i && i.getContext("2d");
			}
			if (e) return t !== r && (t = e.font = r || "12px sans-serif"), e.measureText(n);
			n ||= "", r ||= "12px sans-serif";
			var a = /((?:\d+)?\.?\d*)px/.exec(r), o = a && +a[1] || 12, s = 0;
			if (r.indexOf("mono") >= 0) s = o * n.length;
			else for (var c = 0; c < n.length; c++) {
				var l = f[n[c]];
				s += l == null ? o : l * o;
			}
			return { width: s };
		};
	})(),
	loadImage: function(e, t, n) {
		var r = new Image();
		return r.onload = t, r.onerror = n, r.src = e, r;
	},
	getTime: function() {
		return Date.now ? Date.now() : +/* @__PURE__ */ new Date();
	}
}, m = re([
	"Function",
	"RegExp",
	"Date",
	"Error",
	"CanvasGradient",
	"CanvasPattern",
	"Image",
	"Canvas"
], function(e, t) {
	return e["[object " + t + "]"] = !0, e;
}, {}), h = re([
	"Int8",
	"Uint8",
	"Uint8Clamped",
	"Int16",
	"Uint16",
	"Int32",
	"Uint32",
	"Float32",
	"Float64"
], function(e, t) {
	return e["[object " + t + "Array]"] = !0, e;
}, {}), g = Object.prototype.toString, _ = Array.prototype, v = _.forEach, y = _.filter, b = _.slice, x = _.map, S = function() {}.constructor, C = S ? S.prototype : null, w = "__proto__", T = 2311, E = 2 ** 53 - 1;
function D() {
	return T >= E && (T = 0), T++;
}
function O() {
	var e = [...arguments];
	typeof console < "u" && console.error.apply(console, e);
}
function k(e) {
	if (typeof e != "object" || !e) return e;
	var t = e, n = g.call(e);
	if (n === "[object Array]") {
		if (!xe(e)) {
			t = [];
			for (var r = 0, i = e.length; r < i; r++) t[r] = k(e[r]);
		}
	} else if (h[n]) {
		if (!xe(e)) {
			var a = e.constructor;
			if (a.from) t = a.from(e);
			else {
				t = new a(e.length);
				for (var r = 0, i = e.length; r < i; r++) t[r] = e[r];
			}
		}
	} else if (!m[n] && !xe(e) && !le(e)) for (var o in t = {}, e) e.hasOwnProperty(o) && o !== w && (t[o] = k(e[o]));
	return t;
}
function A(e, t, n) {
	if (!W(t) || !W(e)) return n ? k(t) : e;
	for (var r in t) if (t.hasOwnProperty(r) && r !== w) {
		var i = e[r], a = t[r];
		W(a) && W(i) && !B(a) && !B(i) && !le(a) && !le(i) && !se(a) && !se(i) && !xe(a) && !xe(i) ? A(i, a, n) : (n || !(r in e)) && (e[r] = k(t[r]));
	}
	return e;
}
function j(e, t) {
	if (Object.assign) Object.assign(e, t);
	else for (var n in t) t.hasOwnProperty(n) && n !== w && (e[n] = t[n]);
	return e;
}
function ee(e, t, n) {
	e ||= {};
	for (var r = 0; r < n.length; r++) {
		var i = n[r];
		e[i] = t[i];
	}
	return e;
}
function M(e, t, n) {
	for (var r = L(t), i = 0, a = r.length; i < a; i++) {
		var o = r[i];
		(n ? t[o] != null : e[o] == null) && (e[o] = t[o]);
	}
	return e;
}
p.createCanvas;
function N(e, t) {
	if (e) {
		if (e.indexOf) return e.indexOf(t);
		for (var n = 0, r = e.length; n < r; n++) if (e[n] === t) return n;
	}
	return -1;
}
function te(e, t) {
	var n = e.prototype;
	function r() {}
	for (var i in r.prototype = t.prototype, e.prototype = new r(), n) n.hasOwnProperty(i) && (e.prototype[i] = n[i]);
	e.prototype.constructor = e, e.superClass = t;
}
function ne(e, t, n) {
	if (e = "prototype" in e ? e.prototype : e, t = "prototype" in t ? t.prototype : t, Object.getOwnPropertyNames) for (var r = Object.getOwnPropertyNames(t), i = 0; i < r.length; i++) {
		var a = r[i];
		a !== "constructor" && (n ? t[a] != null : e[a] == null) && (e[a] = t[a]);
	}
	else M(e, t, n);
}
function P(e) {
	return !e || typeof e == "string" ? !1 : typeof e.length == "number";
}
function F(e, t, n) {
	if (e && t) {
		if (e.forEach && e.forEach === v) e.forEach(t, n);
		else if (e.length === +e.length) for (var r = 0, i = e.length; r < i; r++) t.call(n, e[r], r, e);
		else for (var a in e) e.hasOwnProperty(a) && t.call(n, e[a], a, e);
	}
}
function I(e, t, n) {
	if (!e) return [];
	if (!t) return he(e);
	if (e.map && e.map === x) return e.map(t, n);
	for (var r = [], i = 0, a = e.length; i < a; i++) r.push(t.call(n, e[i], i, e));
	return r;
}
function re(e, t, n, r) {
	if (e && t) {
		for (var i = 0, a = e.length; i < a; i++) n = t.call(r, n, e[i], i, e);
		return n;
	}
}
function ie(e, t, n) {
	if (!e) return [];
	if (!t) return he(e);
	if (e.filter && e.filter === y) return e.filter(t, n);
	for (var r = [], i = 0, a = e.length; i < a; i++) t.call(n, e[i], i, e) && r.push(e[i]);
	return r;
}
function L(e) {
	if (!e) return [];
	if (Object.keys) return Object.keys(e);
	var t = [];
	for (var n in e) e.hasOwnProperty(n) && t.push(n);
	return t;
}
function ae(e, t) {
	var n = [...arguments].slice(2);
	return function() {
		return e.apply(t, n.concat(b.call(arguments)));
	};
}
var R = C && V(C.bind) ? C.call.bind(C.bind) : ae;
function z(e) {
	var t = [...arguments].slice(1);
	return function() {
		return e.apply(this, t.concat(b.call(arguments)));
	};
}
function B(e) {
	return Array.isArray ? Array.isArray(e) : g.call(e) === "[object Array]";
}
function V(e) {
	return typeof e == "function";
}
function H(e) {
	return typeof e == "string";
}
function oe(e) {
	return g.call(e) === "[object String]";
}
function U(e) {
	return typeof e == "number";
}
function W(e) {
	var t = typeof e;
	return t === "function" || !!e && t === "object";
}
function se(e) {
	return !!m[g.call(e)];
}
function ce(e) {
	return !!h[g.call(e)];
}
function le(e) {
	return typeof e == "object" && typeof e.nodeType == "number" && typeof e.ownerDocument == "object";
}
function ue(e) {
	return e.colorStops != null;
}
function de(e) {
	return e.image != null;
}
function fe(e) {
	return e !== e;
}
function pe() {
	for (var e = [...arguments], t = 0, n = e.length; t < n; t++) if (e[t] != null) return e[t];
}
function G(e, t) {
	return e ?? t;
}
function me(e, t, n) {
	return e ?? t ?? n;
}
function he(e) {
	var t = [...arguments].slice(1);
	return b.apply(e, t);
}
function ge(e) {
	if (typeof e == "number") return [
		e,
		e,
		e,
		e
	];
	var t = e.length;
	return t === 2 ? [
		e[0],
		e[1],
		e[0],
		e[1]
	] : t === 3 ? [
		e[0],
		e[1],
		e[2],
		e[1]
	] : e;
}
function _e(e, t) {
	if (!e) throw Error(t);
}
function ve(e) {
	return e == null ? null : typeof e.trim == "function" ? e.trim() : e.replace(/^[\s\uFEFF\xA0]+|[\s\uFEFF\xA0]+$/g, "");
}
var ye = "__ec_primitive__";
function be(e) {
	e[ye] = !0;
}
function xe(e) {
	return e[ye];
}
var Se = function() {
	function e() {
		this.data = {};
	}
	return e.prototype.delete = function(e) {
		var t = this.has(e);
		return t && delete this.data[e], t;
	}, e.prototype.has = function(e) {
		return this.data.hasOwnProperty(e);
	}, e.prototype.get = function(e) {
		return this.data[e];
	}, e.prototype.set = function(e, t) {
		return this.data[e] = t, this;
	}, e.prototype.keys = function() {
		return L(this.data);
	}, e.prototype.forEach = function(e) {
		var t = this.data;
		for (var n in t) t.hasOwnProperty(n) && e(t[n], n);
	}, e;
}(), Ce = typeof Map == "function";
function we() {
	return Ce ? /* @__PURE__ */ new Map() : new Se();
}
var Te = function() {
	function e(t) {
		var n = B(t);
		this.data = we();
		var r = this;
		t instanceof e ? t.each(i) : t && F(t, i);
		function i(e, t) {
			n ? r.set(e, t) : r.set(t, e);
		}
	}
	return e.prototype.hasKey = function(e) {
		return this.data.has(e);
	}, e.prototype.get = function(e) {
		return this.data.get(e);
	}, e.prototype.set = function(e, t) {
		return this.data.set(e, t), t;
	}, e.prototype.each = function(e, t) {
		this.data.forEach(function(n, r) {
			e.call(t, n, r);
		});
	}, e.prototype.keys = function() {
		var e = this.data.keys();
		return Ce ? Array.from(e) : e;
	}, e.prototype.removeKey = function(e) {
		this.data.delete(e);
	}, e;
}();
function K(e) {
	return new Te(e);
}
function Ee(e, t) {
	for (var n = new e.constructor(e.length + t.length), r = 0; r < e.length; r++) n[r] = e[r];
	for (var i = e.length, r = 0; r < t.length; r++) n[r + i] = t[r];
	return n;
}
function De(e, t) {
	var n;
	if (Object.create) n = Object.create(e);
	else {
		var r = function() {};
		r.prototype = e, n = new r();
	}
	return t && j(n, t), n;
}
function Oe(e) {
	var t = e.style;
	t.webkitUserSelect = "none", t.userSelect = "none", t.webkitTapHighlightColor = "rgba(0,0,0,0)", t["-webkit-touch-callout"] = "none";
}
function ke(e, t) {
	return e.hasOwnProperty(t);
}
function Ae() {}
var je = 180 / Math.PI;
//#endregion
//#region node_modules/zrender/lib/core/vector.js
function Me(e, t) {
	return e ??= 0, t ??= 0, [e, t];
}
function Ne(e, t) {
	return e[0] = t[0], e[1] = t[1], e;
}
function Pe(e) {
	return [e[0], e[1]];
}
function Fe(e, t, n) {
	return e[0] = t, e[1] = n, e;
}
function Ie(e, t, n) {
	return e[0] = t[0] + n[0], e[1] = t[1] + n[1], e;
}
function Le(e, t, n, r) {
	return e[0] = t[0] + n[0] * r, e[1] = t[1] + n[1] * r, e;
}
function Re(e, t, n) {
	return e[0] = t[0] - n[0], e[1] = t[1] - n[1], e;
}
function ze(e) {
	return Math.sqrt(Be(e));
}
function Be(e) {
	return e[0] * e[0] + e[1] * e[1];
}
function Ve(e, t, n) {
	return e[0] = t[0] * n, e[1] = t[1] * n, e;
}
function He(e, t) {
	var n = ze(t);
	return n === 0 ? (e[0] = 0, e[1] = 0) : (e[0] = t[0] / n, e[1] = t[1] / n), e;
}
function Ue(e, t) {
	return Math.sqrt((e[0] - t[0]) * (e[0] - t[0]) + (e[1] - t[1]) * (e[1] - t[1]));
}
var We = Ue;
function Ge(e, t) {
	return (e[0] - t[0]) * (e[0] - t[0]) + (e[1] - t[1]) * (e[1] - t[1]);
}
var Ke = Ge;
function qe(e, t, n) {
	var r = t[0], i = t[1];
	return e[0] = n[0] * r + n[2] * i + n[4], e[1] = n[1] * r + n[3] * i + n[5], e;
}
function Je(e, t, n) {
	return e[0] = Math.min(t[0], n[0]), e[1] = Math.min(t[1], n[1]), e;
}
function Ye(e, t, n) {
	return e[0] = Math.max(t[0], n[0]), e[1] = Math.max(t[1], n[1]), e;
}
//#endregion
//#region node_modules/zrender/lib/mixin/Draggable.js
var Xe = function() {
	function e(e, t) {
		this.target = e, this.topTarget = t && t.topTarget;
	}
	return e;
}(), Ze = function() {
	function e(e) {
		this.handler = e, e.on("mousedown", this._dragStart, this), e.on("mousemove", this._drag, this), e.on("mouseup", this._dragEnd, this);
	}
	return e.prototype._dragStart = function(e) {
		for (var t = e.target; t && !t.draggable;) t = t.parent || t.__hostTarget;
		t && (this._draggingTarget = t, t.dragging = !0, this._x = e.offsetX, this._y = e.offsetY, this.handler.dispatchToElement(new Xe(t, e), "dragstart", e.event));
	}, e.prototype._drag = function(e) {
		var t = this._draggingTarget;
		if (t) {
			var n = e.offsetX, r = e.offsetY, i = n - this._x, a = r - this._y;
			this._x = n, this._y = r, t.drift(i, a, e), this.handler.dispatchToElement(new Xe(t, e), "drag", e.event);
			var o = this.handler.findHover(n, r, t).target, s = this._dropTarget;
			this._dropTarget = o, t !== o && (s && o !== s && this.handler.dispatchToElement(new Xe(s, e), "dragleave", e.event), o && o !== s && this.handler.dispatchToElement(new Xe(o, e), "dragenter", e.event));
		}
	}, e.prototype._dragEnd = function(e) {
		var t = this._draggingTarget;
		t && (t.dragging = !1), this.handler.dispatchToElement(new Xe(t, e), "dragend", e.event), this._dropTarget && this.handler.dispatchToElement(new Xe(this._dropTarget, e), "drop", e.event), this._draggingTarget = null, this._dropTarget = null;
	}, e;
}(), Qe = function() {
	function e(e) {
		e && (this._$eventProcessor = e);
	}
	return e.prototype.on = function(e, t, n, r) {
		this._$handlers ||= {};
		var i = this._$handlers;
		if (typeof t == "function" && (r = n, n = t, t = null), !n || !e) return this;
		var a = this._$eventProcessor;
		t != null && a && a.normalizeQuery && (t = a.normalizeQuery(t)), i[e] || (i[e] = []);
		for (var o = 0; o < i[e].length; o++) if (i[e][o].h === n) return this;
		var s = {
			h: n,
			query: t,
			ctx: r || this,
			callAtLast: n.zrEventfulCallAtLast
		}, c = i[e].length - 1, l = i[e][c];
		return l && l.callAtLast ? i[e].splice(c, 0, s) : i[e].push(s), this;
	}, e.prototype.isSilent = function(e) {
		var t = this._$handlers;
		return !t || !t[e] || !t[e].length;
	}, e.prototype.off = function(e, t) {
		var n = this._$handlers;
		if (!n) return this;
		if (!e) return this._$handlers = {}, this;
		if (t) {
			if (n[e]) {
				for (var r = [], i = 0, a = n[e].length; i < a; i++) n[e][i].h !== t && r.push(n[e][i]);
				n[e] = r;
			}
			n[e] && n[e].length === 0 && delete n[e];
		} else delete n[e];
		return this;
	}, e.prototype.trigger = function(e) {
		var t = [...arguments].slice(1);
		if (!this._$handlers) return this;
		var n = this._$handlers[e], r = this._$eventProcessor;
		if (n) for (var i = t.length, a = n.length, o = 0; o < a; o++) {
			var s = n[o];
			if (!(r && r.filter && s.query != null && !r.filter(e, s.query))) switch (i) {
				case 0:
					s.h.call(s.ctx);
					break;
				case 1:
					s.h.call(s.ctx, t[0]);
					break;
				case 2:
					s.h.call(s.ctx, t[0], t[1]);
					break;
				default: s.h.apply(s.ctx, t);
			}
		}
		return r && r.afterTrigger && r.afterTrigger(e), this;
	}, e.prototype.triggerWithContext = function(e) {
		var t = [...arguments].slice(1);
		if (!this._$handlers) return this;
		var n = this._$handlers[e], r = this._$eventProcessor;
		if (n) for (var i = t.length, a = t[i - 1], o = n.length, s = 0; s < o; s++) {
			var c = n[s];
			if (!(r && r.filter && c.query != null && !r.filter(e, c.query))) switch (i) {
				case 0:
					c.h.call(a);
					break;
				case 1:
					c.h.call(a, t[0]);
					break;
				case 2:
					c.h.call(a, t[0], t[1]);
					break;
				default: c.h.apply(a, t.slice(1, i - 1));
			}
		}
		return r && r.afterTrigger && r.afterTrigger(e), this;
	}, e;
}(), $e = Math.log(2);
function et(e, t, n, r, i, a) {
	var o = r + "-" + i, s = e.length;
	if (a.hasOwnProperty(o)) return a[o];
	if (t === 1) {
		var c = Math.round(Math.log((1 << s) - 1 & ~i) / $e);
		return e[n][c];
	}
	for (var l = r | 1 << n, u = n + 1; r & 1 << u;) u++;
	for (var d = 0, f = 0, p = 0; f < s; f++) {
		var m = 1 << f;
		m & i || (d += (p % 2 ? -1 : 1) * e[n][f] * et(e, t - 1, u, l, i | m, a), p++);
	}
	return a[o] = d, d;
}
function tt(e, t) {
	var n = [
		[
			e[0],
			e[1],
			1,
			0,
			0,
			0,
			-t[0] * e[0],
			-t[0] * e[1]
		],
		[
			0,
			0,
			0,
			e[0],
			e[1],
			1,
			-t[1] * e[0],
			-t[1] * e[1]
		],
		[
			e[2],
			e[3],
			1,
			0,
			0,
			0,
			-t[2] * e[2],
			-t[2] * e[3]
		],
		[
			0,
			0,
			0,
			e[2],
			e[3],
			1,
			-t[3] * e[2],
			-t[3] * e[3]
		],
		[
			e[4],
			e[5],
			1,
			0,
			0,
			0,
			-t[4] * e[4],
			-t[4] * e[5]
		],
		[
			0,
			0,
			0,
			e[4],
			e[5],
			1,
			-t[5] * e[4],
			-t[5] * e[5]
		],
		[
			e[6],
			e[7],
			1,
			0,
			0,
			0,
			-t[6] * e[6],
			-t[6] * e[7]
		],
		[
			0,
			0,
			0,
			e[6],
			e[7],
			1,
			-t[7] * e[6],
			-t[7] * e[7]
		]
	], r = {}, i = et(n, 8, 0, 0, 0, r);
	if (i !== 0) {
		for (var a = [], o = 0; o < 8; o++) for (var s = 0; s < 8; s++) a[s] ?? (a[s] = 0), a[s] += ((o + s) % 2 ? -1 : 1) * et(n, 7, +(o === 0), 1 << o, 1 << s, r) / i * t[o];
		return function(e, t, n) {
			var r = t * a[6] + n * a[7] + 1;
			e[0] = (t * a[0] + n * a[1] + a[2]) / r, e[1] = (t * a[3] + n * a[4] + a[5]) / r;
		};
	}
}
//#endregion
//#region node_modules/zrender/lib/core/dom.js
var nt = "___zrEVENTSAVED", rt = [];
function it(e, t, n, r, i) {
	return ot(rt, t, r, i, !0) && ot(e, n, rt[0], rt[1]);
}
function at(e, t) {
	e && n(e), t && n(t);
	function n(e) {
		var t = e[nt];
		t && (t.clearMarkers && t.clearMarkers(), delete e[nt]);
	}
}
function ot(e, t, n, r, i) {
	if (t.getBoundingClientRect && a.domSupported && !lt(t)) {
		var o = t[nt] || (t[nt] = {}), s = ct(st(t, o), o, i);
		if (s) return s(e, n, r), !0;
	}
	return !1;
}
function st(e, t) {
	var n = t.markers;
	if (n) return n;
	n = t.markers = [];
	for (var r = ["left", "right"], i = ["top", "bottom"], a = 0; a < 4; a++) {
		var o = document.createElement("div"), s = o.style, c = a % 2, l = (a >> 1) % 2;
		s.cssText = [
			"position: absolute",
			"visibility: hidden",
			"padding: 0",
			"margin: 0",
			"border-width: 0",
			"user-select: none",
			"width:0",
			"height:0",
			r[c] + ":0",
			i[l] + ":0",
			r[1 - c] + ":auto",
			i[1 - l] + ":auto",
			""
		].join("!important;"), e.appendChild(o), n.push(o);
	}
	return t.clearMarkers = function() {
		F(n, function(e) {
			e.parentNode && e.parentNode.removeChild(e);
		});
	}, n;
}
function ct(e, t, n) {
	for (var r = n ? "invTrans" : "trans", i = t[r], a = t.srcCoords, o = [], s = [], c = !0, l = 0; l < 4; l++) {
		var u = e[l].getBoundingClientRect(), d = 2 * l, f = u.left, p = u.top;
		o.push(f, p), c = c && a && f === a[d] && p === a[d + 1], s.push(e[l].offsetLeft, e[l].offsetTop);
	}
	return c && i ? i : (t.srcCoords = o, t[r] = n ? tt(s, o) : tt(o, s));
}
function lt(e) {
	return e.nodeName.toUpperCase() === "CANVAS";
}
var ut = /([&<>"'])/g, dt = {
	"&": "&amp;",
	"<": "&lt;",
	">": "&gt;",
	"\"": "&quot;",
	"'": "&#39;"
};
function ft(e) {
	return e == null ? "" : (e + "").replace(ut, function(e, t) {
		return dt[t];
	});
}
//#endregion
//#region node_modules/zrender/lib/core/event.js
var pt = /^(?:mouse|pointer|contextmenu|drag|drop)|click/, mt = [], ht = a.browser.firefox && +a.browser.version.split(".")[0] < 39;
function gt(e, t, n, r) {
	return n ||= {}, r ? _t(e, t, n) : ht && t.layerX != null && t.layerX !== t.offsetX ? (n.zrX = t.layerX, n.zrY = t.layerY) : t.offsetX == null ? _t(e, t, n) : (n.zrX = t.offsetX, n.zrY = t.offsetY), n;
}
function _t(e, t, n) {
	if (a.domSupported && e.getBoundingClientRect) {
		var r = t.clientX, i = t.clientY;
		if (lt(e)) {
			var o = e.getBoundingClientRect();
			n.zrX = r - o.left, n.zrY = i - o.top;
			return;
		}
		if (ot(mt, e, r, i)) {
			n.zrX = mt[0], n.zrY = mt[1];
			return;
		}
	}
	n.zrX = n.zrY = 0;
}
function vt(e) {
	return e || window.event;
}
function yt(e, t, n) {
	if (t = vt(t), t.zrX != null) return t;
	var r = t.type;
	if (r && r.indexOf("touch") >= 0) {
		var i = r === "touchend" ? t.changedTouches[0] : t.targetTouches[0];
		i && gt(e, i, t, n);
	} else {
		gt(e, t, t, n);
		var a = bt(t);
		t.zrDelta = a ? a / 120 : -(t.detail || 0) / 3;
	}
	var o = t.button;
	return t.which == null && o !== void 0 && pt.test(t.type) && (t.which = o & 1 ? 1 : o & 2 ? 3 : o & 4 ? 2 : 0), t;
}
function bt(e) {
	var t = e.wheelDelta;
	if (t) return t;
	var n = e.deltaX, r = e.deltaY;
	if (n == null || r == null) return t;
	var i = Math.abs(r === 0 ? n : r), a = r > 0 ? -1 : r < 0 ? 1 : n > 0 ? -1 : 1;
	return 3 * i * a;
}
function xt(e, t, n, r) {
	e.addEventListener(t, n, r);
}
function St(e, t, n, r) {
	e.removeEventListener(t, n, r);
}
var Ct = function(e) {
	e.preventDefault(), e.stopPropagation(), e.cancelBubble = !0;
};
function wt(e) {
	return e.which === 2 || e.which === 3;
}
//#endregion
//#region node_modules/zrender/lib/core/GestureMgr.js
var Tt = function() {
	function e() {
		this._track = [];
	}
	return e.prototype.recognize = function(e, t, n) {
		return this._doTrack(e, t, n), this._recognize(e);
	}, e.prototype.clear = function() {
		return this._track.length = 0, this;
	}, e.prototype._doTrack = function(e, t, n) {
		var r = e.touches;
		if (r) {
			for (var i = {
				points: [],
				touches: [],
				target: t,
				event: e
			}, a = 0, o = r.length; a < o; a++) {
				var s = r[a], c = gt(n, s, {});
				i.points.push([c.zrX, c.zrY]), i.touches.push(s);
			}
			this._track.push(i);
		}
	}, e.prototype._recognize = function(e) {
		for (var t in Ot) if (Ot.hasOwnProperty(t)) {
			var n = Ot[t](this._track, e);
			if (n) return n;
		}
	}, e;
}();
function Et(e) {
	var t = e[1][0] - e[0][0], n = e[1][1] - e[0][1];
	return Math.sqrt(t * t + n * n);
}
function Dt(e) {
	return [(e[0][0] + e[1][0]) / 2, (e[0][1] + e[1][1]) / 2];
}
var Ot = { pinch: function(e, t) {
	var n = e.length;
	if (n) {
		var r = (e[n - 1] || {}).points, i = (e[n - 2] || {}).points || r;
		if (i && i.length > 1 && r && r.length > 1) {
			var a = Et(r) / Et(i);
			!isFinite(a) && (a = 1), t.pinchScale = a;
			var o = Dt(r);
			return t.pinchX = o[0], t.pinchY = o[1], {
				type: "pinch",
				target: e[0].target,
				event: t
			};
		}
	}
} };
//#endregion
//#region node_modules/zrender/lib/core/matrix.js
function kt() {
	return [
		1,
		0,
		0,
		1,
		0,
		0
	];
}
function At(e) {
	return e[0] = 1, e[1] = 0, e[2] = 0, e[3] = 1, e[4] = 0, e[5] = 0, e;
}
function jt(e, t) {
	return e[0] = t[0], e[1] = t[1], e[2] = t[2], e[3] = t[3], e[4] = t[4], e[5] = t[5], e;
}
function Mt(e, t, n) {
	var r = t[0] * n[0] + t[2] * n[1], i = t[1] * n[0] + t[3] * n[1], a = t[0] * n[2] + t[2] * n[3], o = t[1] * n[2] + t[3] * n[3], s = t[0] * n[4] + t[2] * n[5] + t[4], c = t[1] * n[4] + t[3] * n[5] + t[5];
	return e[0] = r, e[1] = i, e[2] = a, e[3] = o, e[4] = s, e[5] = c, e;
}
function Nt(e, t, n) {
	return e[0] = t[0], e[1] = t[1], e[2] = t[2], e[3] = t[3], e[4] = t[4] + n[0], e[5] = t[5] + n[1], e;
}
function Pt(e, t, n, r) {
	r === void 0 && (r = [0, 0]);
	var i = t[0], a = t[2], o = t[4], s = t[1], c = t[3], l = t[5], u = Math.sin(n), d = Math.cos(n);
	return e[0] = i * d + s * u, e[1] = -i * u + s * d, e[2] = a * d + c * u, e[3] = -a * u + d * c, e[4] = d * (o - r[0]) + u * (l - r[1]) + r[0], e[5] = d * (l - r[1]) - u * (o - r[0]) + r[1], e;
}
function Ft(e, t, n) {
	var r = n[0], i = n[1];
	return e[0] = t[0] * r, e[1] = t[1] * i, e[2] = t[2] * r, e[3] = t[3] * i, e[4] = t[4] * r, e[5] = t[5] * i, e;
}
function It(e, t) {
	var n = t[0], r = t[2], i = t[4], a = t[1], o = t[3], s = t[5], c = n * o - a * r;
	return c ? (c = 1 / c, e[0] = o * c, e[1] = -a * c, e[2] = -r * c, e[3] = n * c, e[4] = (r * s - o * i) * c, e[5] = (a * i - n * s) * c, e) : null;
}
//#endregion
//#region node_modules/zrender/lib/core/Point.js
var q = function() {
	function e(e, t) {
		this.x = e || 0, this.y = t || 0;
	}
	return e.prototype.copy = function(e) {
		return this.x = e.x, this.y = e.y, this;
	}, e.prototype.clone = function() {
		return new e(this.x, this.y);
	}, e.prototype.set = function(e, t) {
		return this.x = e, this.y = t, this;
	}, e.prototype.equal = function(e) {
		return e.x === this.x && e.y === this.y;
	}, e.prototype.add = function(e) {
		return this.x += e.x, this.y += e.y, this;
	}, e.prototype.scale = function(e) {
		this.x *= e, this.y *= e;
	}, e.prototype.scaleAndAdd = function(e, t) {
		this.x += e.x * t, this.y += e.y * t;
	}, e.prototype.sub = function(e) {
		return this.x -= e.x, this.y -= e.y, this;
	}, e.prototype.dot = function(e) {
		return this.x * e.x + this.y * e.y;
	}, e.prototype.len = function() {
		return Math.sqrt(this.x * this.x + this.y * this.y);
	}, e.prototype.lenSquare = function() {
		return this.x * this.x + this.y * this.y;
	}, e.prototype.normalize = function() {
		var e = this.len();
		return this.x /= e, this.y /= e, this;
	}, e.prototype.distance = function(e) {
		var t = this.x - e.x, n = this.y - e.y;
		return Math.sqrt(t * t + n * n);
	}, e.prototype.distanceSquare = function(e) {
		var t = this.x - e.x, n = this.y - e.y;
		return t * t + n * n;
	}, e.prototype.negate = function() {
		return this.x = -this.x, this.y = -this.y, this;
	}, e.prototype.transform = function(e) {
		if (e) {
			var t = this.x, n = this.y;
			return this.x = e[0] * t + e[2] * n + e[4], this.y = e[1] * t + e[3] * n + e[5], this;
		}
	}, e.prototype.toArray = function(e) {
		return e[0] = this.x, e[1] = this.y, e;
	}, e.prototype.fromArray = function(e) {
		this.x = e[0], this.y = e[1];
	}, e.set = function(e, t, n) {
		e.x = t, e.y = n;
	}, e.copy = function(e, t) {
		e.x = t.x, e.y = t.y;
	}, e.len = function(e) {
		return Math.sqrt(e.x * e.x + e.y * e.y);
	}, e.lenSquare = function(e) {
		return e.x * e.x + e.y * e.y;
	}, e.dot = function(e, t) {
		return e.x * t.x + e.y * t.y;
	}, e.add = function(e, t, n) {
		e.x = t.x + n.x, e.y = t.y + n.y;
	}, e.sub = function(e, t, n) {
		e.x = t.x - n.x, e.y = t.y - n.y;
	}, e.scale = function(e, t, n) {
		e.x = t.x * n, e.y = t.y * n;
	}, e.scaleAndAdd = function(e, t, n, r) {
		e.x = t.x + n.x * r, e.y = t.y + n.y * r;
	}, e.lerp = function(e, t, n, r) {
		var i = 1 - r;
		e.x = i * t.x + r * n.x, e.y = i * t.y + r * n.y;
	}, e;
}(), Lt = Math.min, Rt = Math.max, zt = Math.abs, Bt = ["x", "y"], Vt = ["width", "height"], Ht = new q(), Ut = new q(), Wt = new q(), Gt = new q(), Kt = sn(), qt = Kt.minTv, Jt = Kt.maxTv, Yt = [0, 0], J = function() {
	function e(e, t, n, r) {
		Zt(this, e, t, n, r);
	}
	return e.set = function(e, t, n, r, i) {
		return r < 0 && (t += r, r = -r), i < 0 && (n += i, i = -i), e.x = t, e.y = n, e.width = r, e.height = i, e;
	}, e.prototype.union = function(e) {
		var t = Lt(e.x, this.x), n = Lt(e.y, this.y);
		this.width = isFinite(this.x) && isFinite(this.width) ? Rt(e.x + e.width, this.x + this.width) - t : e.width, this.height = isFinite(this.y) && isFinite(this.height) ? Rt(e.y + e.height, this.y + this.height) - n : e.height, this.x = t, this.y = n;
	}, e.prototype.applyTransform = function(t) {
		e.applyTransform(this, this, t);
	}, e.prototype.calculateTransform = function(e) {
		return $t(kt(), this, e);
	}, e.prototype.intersect = function(t, n, r) {
		return e.intersect(this, t, n, r);
	}, e.intersect = function(t, n, r, i) {
		r && q.set(r, 0, 0);
		var a = i && i.outIntersectRect || null, o = i && i.clamp;
		if (a && (a.x = a.y = a.width = a.height = NaN), !t || !n) return !1;
		t instanceof e || (t = Zt(nn, t.x, t.y, t.width, t.height)), n instanceof e || (n = Zt(rn, n.x, n.y, n.width, n.height));
		var s = !!r;
		Kt.reset(i, s);
		var c = Kt.touchThreshold, l = t.x + c, u = t.x + t.width - c, d = t.y + c, f = t.y + t.height - c, p = n.x + c, m = n.x + n.width - c, h = n.y + c, g = n.y + n.height - c;
		if (l > u || d > f || p > m || h > g) return !1;
		var _ = !(u < p || m < l || f < h || g < d);
		return (s || a) && (Yt[0] = Infinity, Yt[1] = 0, on(l, u, p, m, 0, s, a, o), on(d, f, h, g, 1, s, a, o), s && q.copy(r, _ ? Kt.useDir ? Kt.dirMinTv : qt : Jt)), _;
	}, e.contain = function(e, t, n) {
		return t >= e.x && t <= e.x + e.width && n >= e.y && n <= e.y + e.height;
	}, e.prototype.contain = function(t, n) {
		return e.contain(this, t, n);
	}, e.prototype.clone = function() {
		return new e(this.x, this.y, this.width, this.height);
	}, e.prototype.copy = function(e) {
		Qt(this, e);
	}, e.prototype.plain = function() {
		return {
			x: this.x,
			y: this.y,
			width: this.width,
			height: this.height
		};
	}, e.prototype.isFinite = function() {
		return isFinite(this.x) && isFinite(this.y) && isFinite(this.width) && isFinite(this.height);
	}, e.prototype.isZero = function() {
		return this.width === 0 || this.height === 0;
	}, e.create = function(t) {
		return new e(t ? t.x : 0, t ? t.y : 0, t ? t.width : 0, t ? t.height : 0);
	}, e.copy = function(e, t) {
		return e.x = t.x, e.y = t.y, e.width = t.width, e.height = t.height, e;
	}, e.applyTransform = function(e, t, n) {
		if (!n) e !== t && Qt(e, t);
		else if (n[1] < 1e-5 && n[1] > -1e-5 && n[2] < 1e-5 && n[2] > -1e-5) {
			var r = n[0], i = n[3], a = n[4], o = n[5];
			e.x = t.x * r + a, e.y = t.y * i + o, e.width = t.width * r, e.height = t.height * i, e.width < 0 && (e.x += e.width, e.width = -e.width), e.height < 0 && (e.y += e.height, e.height = -e.height);
		} else {
			Ht.x = Wt.x = t.x, Ht.y = Gt.y = t.y, Ut.x = Gt.x = t.x + t.width, Ut.y = Wt.y = t.y + t.height, Ht.transform(n), Gt.transform(n), Ut.transform(n), Wt.transform(n), e.x = Lt(Ht.x, Ut.x, Wt.x, Gt.x), e.y = Lt(Ht.y, Ut.y, Wt.y, Gt.y);
			var s = Rt(Ht.x, Ut.x, Wt.x, Gt.x), c = Rt(Ht.y, Ut.y, Wt.y, Gt.y);
			e.width = s - e.x, e.height = c - e.y;
		}
	}, e.calculateTransform = function(e, t, n) {
		var r = n.width / t.width, i = n.height / t.height;
		return e = At(e || []), Nt(e, e, Fe(an, -t.x, -t.y)), Ft(e, e, Fe(an, r, i)), Nt(e, e, Fe(an, n.x, n.y)), e;
	}, e;
}(), Xt = J.create, Zt = J.set, Qt = J.copy, $t = J.calculateTransform, en = J.applyTransform, tn = J.contain, nn = new J(0, 0, 0, 0), rn = new J(0, 0, 0, 0), an = [];
function on(e, t, n, r, i, a, o, s) {
	var c = zt(t - n), l = zt(r - e), u = Lt(c, l), d = Bt[i], f = Bt[1 - i], p = Vt[i];
	t < n || r < e ? c < l ? (a && (Jt[d] = -c), s && (o[d] = t, o[p] = 0)) : (a && (Jt[d] = l), s && (o[d] = e, o[p] = 0)) : (o && (o[d] = Rt(e, n), o[p] = Lt(t, r) - o[d]), a && (u < Yt[0] || Kt.useDir) && (Yt[0] = Lt(u, Yt[0]), (c < l || !Kt.bidirectional) && (qt[d] = c, qt[f] = 0, Kt.useDir && Kt.calcDirMTV()), (c >= l || !Kt.bidirectional) && (qt[d] = -l, qt[f] = 0, Kt.useDir && Kt.calcDirMTV())));
}
function sn() {
	var e = 0, t = new q(), n = new q(), r = {
		minTv: new q(),
		maxTv: new q(),
		useDir: !1,
		dirMinTv: new q(),
		touchThreshold: 0,
		bidirectional: !0,
		negativeSize: !1,
		reset: function(i, a) {
			r.touchThreshold = 0, i && i.touchThreshold != null && (r.touchThreshold = Rt(0, i.touchThreshold)), r.negativeSize = !1, a && (r.minTv.set(Infinity, Infinity), r.maxTv.set(0, 0), r.useDir = !1, i && i.direction != null && (r.useDir = !0, r.dirMinTv.copy(r.minTv), n.copy(r.minTv), e = i.direction, r.bidirectional = i.bidirectional == null || !!i.bidirectional, r.bidirectional || t.set(Math.cos(e), Math.sin(e))));
		},
		calcDirMTV: function() {
			var a = r.minTv, o = r.dirMinTv, s = a.y * a.y + a.x * a.x, c = Math.sin(e), l = Math.cos(e), u = c * a.y + l * a.x;
			i(u) ? i(a.x) && i(a.y) && o.set(0, 0) : (n.x = s * l / u, n.y = s * c / u, i(n.x) && i(n.y) ? o.set(0, 0) : (r.bidirectional || t.dot(n) > 0) && n.len() < o.len() && o.copy(n));
		}
	};
	function i(e) {
		return zt(e) < 1e-10;
	}
	return r;
}
//#endregion
//#region node_modules/zrender/lib/Handler.js
var cn = "silent";
function ln(e, t, n) {
	return {
		type: e,
		event: n,
		target: t.target,
		topTarget: t.topTarget,
		cancelBubble: !1,
		offsetX: n.zrX,
		offsetY: n.zrY,
		gestureEvent: n.gestureEvent,
		pinchX: n.pinchX,
		pinchY: n.pinchY,
		pinchScale: n.pinchScale,
		wheelDelta: n.zrDelta,
		zrByTouch: n.zrByTouch,
		which: n.which,
		stop: un
	};
}
function un() {
	Ct(this.event);
}
var dn = function(e) {
	r(t, e);
	function t() {
		var t = e !== null && e.apply(this, arguments) || this;
		return t.handler = null, t;
	}
	return t.prototype.dispose = function() {}, t.prototype.setCursor = function() {}, t;
}(Qe), fn = function() {
	function e(e, t) {
		this.x = e, this.y = t;
	}
	return e;
}(), pn = [
	"click",
	"dblclick",
	"mousewheel",
	"mouseout",
	"mouseup",
	"mousedown",
	"mousemove",
	"contextmenu"
], mn = new J(0, 0, 0, 0), hn = function(e) {
	r(t, e);
	function t(t, n, r, i, a) {
		var o = e.call(this) || this;
		return o._hovered = new fn(0, 0), o.storage = t, o.painter = n, o.painterRoot = i, o._pointerSize = a, r ||= new dn(), o.proxy = null, o.setHandlerProxy(r), o._draggingMgr = new Ze(o), o;
	}
	return t.prototype.setHandlerProxy = function(e) {
		this.proxy && this.proxy.dispose(), e && (F(pn, function(t) {
			e.on && e.on(t, this[t], this);
		}, this), e.handler = this), this.proxy = e;
	}, t.prototype.mousemove = function(e) {
		var t = e.zrX, n = e.zrY, r = vn(this, t, n), i = this._hovered, a = i.target;
		a && !a.__zr && (i = this.findHover(i.x, i.y), a = i.target);
		var o = this._hovered = r ? new fn(t, n) : this.findHover(t, n), s = o.target, c = this.proxy;
		c.setCursor && c.setCursor(s ? s.cursor : "default"), a && s !== a && this.dispatchToElement(i, "mouseout", e), this.dispatchToElement(o, "mousemove", e), s && s !== a && this.dispatchToElement(o, "mouseover", e);
	}, t.prototype.mouseout = function(e) {
		var t = e.zrEventControl;
		t !== "only_globalout" && this.dispatchToElement(this._hovered, "mouseout", e), t !== "no_globalout" && this.trigger("globalout", {
			type: "globalout",
			event: e
		});
	}, t.prototype.resize = function() {
		this._hovered = new fn(0, 0);
	}, t.prototype.dispatch = function(e, t) {
		var n = this[e];
		n && n.call(this, t);
	}, t.prototype.dispose = function() {
		this.proxy.dispose(), this.storage = null, this.proxy = null, this.painter = null;
	}, t.prototype.setCursorStyle = function(e) {
		var t = this.proxy;
		t.setCursor && t.setCursor(e);
	}, t.prototype.dispatchToElement = function(e, t, n) {
		e ||= {};
		var r = e.target;
		if (!(r && r.silent)) {
			for (var i = "on" + t, a = ln(t, e, n); r && (r[i] && (a.cancelBubble = !!r[i].call(r, a)), r.trigger(t, a), r = r.__hostTarget ? r.__hostTarget : r.parent, !a.cancelBubble););
			a.cancelBubble || (this.trigger(t, a), this.painter && this.painter.eachOtherLayer && this.painter.eachOtherLayer(function(e) {
				typeof e[i] == "function" && e[i].call(e, a), e.trigger && e.trigger(t, a);
			}));
		}
	}, t.prototype.findHover = function(e, t, n) {
		var r = this.storage.getDisplayList(), i = new fn(e, t);
		if (_n(r, i, e, t, n), this._pointerSize && !i.target) {
			for (var a = [], o = this._pointerSize, s = o / 2, c = new J(e - s, t - s, o, o), l = r.length - 1; l >= 0; l--) {
				var u = r[l];
				u !== n && !u.ignore && !u.ignoreCoarsePointer && (!u.parent || !u.parent.ignoreCoarsePointer) && (mn.copy(u.getBoundingRect()), u.transform && mn.applyTransform(u.transform), mn.intersect(c) && a.push(u));
			}
			if (a.length) {
				for (var d = 4, f = Math.PI / 12, p = Math.PI * 2, m = 0; m < s; m += d) for (var h = 0; h < p; h += f) if (_n(a, i, e + m * Math.cos(h), t + m * Math.sin(h), n), i.target) return i;
			}
		}
		return i;
	}, t.prototype.processGesture = function(e, t) {
		this._gestureMgr ||= new Tt();
		var n = this._gestureMgr;
		t === "start" && n.clear();
		var r = n.recognize(e, this.findHover(e.zrX, e.zrY, null).target, this.proxy.dom);
		if (t === "end" && n.clear(), r) {
			var i = r.type;
			e.gestureEvent = i;
			var a = new fn();
			a.target = r.target, this.dispatchToElement(a, i, r.event);
		}
	}, t;
}(Qe);
F([
	"click",
	"mousedown",
	"mouseup",
	"mousewheel",
	"dblclick",
	"contextmenu"
], function(e) {
	hn.prototype[e] = function(t) {
		var n = t.zrX, r = t.zrY, i = vn(this, n, r), a, o;
		if ((e !== "mouseup" || !i) && (a = this.findHover(n, r), o = a.target), e === "mousedown") this._downEl = o, this._downPoint = [t.zrX, t.zrY], this._upEl = o;
		else if (e === "mouseup") this._upEl = o;
		else if (e === "click") {
			if (this._downEl !== this._upEl || !this._downPoint || We(this._downPoint, [t.zrX, t.zrY]) > 4) return;
			this._downPoint = null;
		}
		this.dispatchToElement(a, e, t);
	};
});
function gn(e, t, n) {
	if (e[e.rectHover ? "rectContain" : "contain"](t, n)) {
		for (var r = e, i = void 0, a = !1; r;) {
			if (r.ignoreClip && (a = !0), !a) {
				var o = r.getClipPath();
				if (o && !o.contain(t, n)) return !1;
			}
			r.silent && (i = !0);
			var s = r.__hostTarget;
			r = s ? r.ignoreHostSilent ? null : s : r.parent;
		}
		return !i || cn;
	}
	return !1;
}
function _n(e, t, n, r, i) {
	for (var a = e.length - 1; a >= 0; a--) {
		var o = e[a], s = void 0;
		if (o !== i && !o.ignore && (s = gn(o, n, r)) && (!t.topTarget && (t.topTarget = o), s !== cn)) {
			t.target = o;
			break;
		}
	}
}
function vn(e, t, n) {
	var r = e.painter;
	return t < 0 || t > r.getWidth() || n < 0 || n > r.getHeight();
}
//#endregion
//#region node_modules/zrender/lib/core/timsort.js
var yn = 32, bn = 7;
function xn(e) {
	for (var t = 0; e >= yn;) t |= e & 1, e >>= 1;
	return e + t;
}
function Sn(e, t, n, r) {
	var i = t + 1;
	if (i === n) return 1;
	if (r(e[i++], e[t]) < 0) {
		for (; i < n && r(e[i], e[i - 1]) < 0;) i++;
		Cn(e, t, i);
	} else for (; i < n && r(e[i], e[i - 1]) >= 0;) i++;
	return i - t;
}
function Cn(e, t, n) {
	for (n--; t < n;) {
		var r = e[t];
		e[t++] = e[n], e[n--] = r;
	}
}
function wn(e, t, n, r, i) {
	for (r === t && r++; r < n; r++) {
		for (var a = e[r], o = t, s = r, c; o < s;) c = o + s >>> 1, i(a, e[c]) < 0 ? s = c : o = c + 1;
		var l = r - o;
		switch (l) {
			case 3: e[o + 3] = e[o + 2];
			case 2: e[o + 2] = e[o + 1];
			case 1:
				e[o + 1] = e[o];
				break;
			default: for (; l > 0;) e[o + l] = e[o + l - 1], l--;
		}
		e[o] = a;
	}
}
function Tn(e, t, n, r, i, a) {
	var o = 0, s = 0, c = 1;
	if (a(e, t[n + i]) > 0) {
		for (s = r - i; c < s && a(e, t[n + i + c]) > 0;) o = c, c = (c << 1) + 1, c <= 0 && (c = s);
		c > s && (c = s), o += i, c += i;
	} else {
		for (s = i + 1; c < s && a(e, t[n + i - c]) <= 0;) o = c, c = (c << 1) + 1, c <= 0 && (c = s);
		c > s && (c = s);
		var l = o;
		o = i - c, c = i - l;
	}
	for (o++; o < c;) {
		var u = o + (c - o >>> 1);
		a(e, t[n + u]) > 0 ? o = u + 1 : c = u;
	}
	return c;
}
function En(e, t, n, r, i, a) {
	var o = 0, s = 0, c = 1;
	if (a(e, t[n + i]) < 0) {
		for (s = i + 1; c < s && a(e, t[n + i - c]) < 0;) o = c, c = (c << 1) + 1, c <= 0 && (c = s);
		c > s && (c = s);
		var l = o;
		o = i - c, c = i - l;
	} else {
		for (s = r - i; c < s && a(e, t[n + i + c]) >= 0;) o = c, c = (c << 1) + 1, c <= 0 && (c = s);
		c > s && (c = s), o += i, c += i;
	}
	for (o++; o < c;) {
		var u = o + (c - o >>> 1);
		a(e, t[n + u]) < 0 ? c = u : o = u + 1;
	}
	return c;
}
function Dn(e, t) {
	var n = bn, r, i, a = 0, o = [];
	r = [], i = [];
	function s(e, t) {
		r[a] = e, i[a] = t, a += 1;
	}
	function c() {
		for (; a > 1;) {
			var e = a - 2;
			if (e >= 1 && i[e - 1] <= i[e] + i[e + 1] || e >= 2 && i[e - 2] <= i[e] + i[e - 1]) i[e - 1] < i[e + 1] && e--;
			else if (i[e] > i[e + 1]) break;
			u(e);
		}
	}
	function l() {
		for (; a > 1;) {
			var e = a - 2;
			e > 0 && i[e - 1] < i[e + 1] && e--, u(e);
		}
	}
	function u(n) {
		var o = r[n], s = i[n], c = r[n + 1], l = i[n + 1];
		i[n] = s + l, n === a - 3 && (r[n + 1] = r[n + 2], i[n + 1] = i[n + 2]), a--;
		var u = En(e[c], e, o, s, 0, t);
		o += u, s -= u, s !== 0 && (l = Tn(e[o + s - 1], e, c, l, l - 1, t), l !== 0 && (s <= l ? d(o, s, c, l) : f(o, s, c, l)));
	}
	function d(r, i, a, s) {
		var c = 0;
		for (c = 0; c < i; c++) o[c] = e[r + c];
		var l = 0, u = a, d = r;
		if (e[d++] = e[u++], --s === 0) for (c = 0; c < i; c++) e[d + c] = o[l + c];
		else if (i === 1) {
			for (c = 0; c < s; c++) e[d + c] = e[u + c];
			e[d + s] = o[l];
		} else {
			for (var f = n, p, m, h;;) {
				p = 0, m = 0, h = !1;
				do
					if (t(e[u], o[l]) < 0) {
						if (e[d++] = e[u++], m++, p = 0, --s === 0) {
							h = !0;
							break;
						}
					} else if (e[d++] = o[l++], p++, m = 0, --i === 1) {
						h = !0;
						break;
					}
				while ((p | m) < f);
				if (h) break;
				do {
					if (p = En(e[u], o, l, i, 0, t), p !== 0) {
						for (c = 0; c < p; c++) e[d + c] = o[l + c];
						if (d += p, l += p, i -= p, i <= 1) {
							h = !0;
							break;
						}
					}
					if (e[d++] = e[u++], --s === 0) {
						h = !0;
						break;
					}
					if (m = Tn(o[l], e, u, s, 0, t), m !== 0) {
						for (c = 0; c < m; c++) e[d + c] = e[u + c];
						if (d += m, u += m, s -= m, s === 0) {
							h = !0;
							break;
						}
					}
					if (e[d++] = o[l++], --i === 1) {
						h = !0;
						break;
					}
					f--;
				} while (p >= bn || m >= bn);
				if (h) break;
				f < 0 && (f = 0), f += 2;
			}
			if (n = f, n < 1 && (n = 1), i === 1) {
				for (c = 0; c < s; c++) e[d + c] = e[u + c];
				e[d + s] = o[l];
			} else if (i === 0) throw Error();
			else for (c = 0; c < i; c++) e[d + c] = o[l + c];
		}
	}
	function f(r, i, a, s) {
		var c = 0;
		for (c = 0; c < s; c++) o[c] = e[a + c];
		var l = r + i - 1, u = s - 1, d = a + s - 1, f = 0, p = 0;
		if (e[d--] = e[l--], --i === 0) for (f = d - (s - 1), c = 0; c < s; c++) e[f + c] = o[c];
		else if (s === 1) {
			for (d -= i, l -= i, p = d + 1, f = l + 1, c = i - 1; c >= 0; c--) e[p + c] = e[f + c];
			e[d] = o[u];
		} else {
			for (var m = n;;) {
				var h = 0, g = 0, _ = !1;
				do
					if (t(o[u], e[l]) < 0) {
						if (e[d--] = e[l--], h++, g = 0, --i === 0) {
							_ = !0;
							break;
						}
					} else if (e[d--] = o[u--], g++, h = 0, --s === 1) {
						_ = !0;
						break;
					}
				while ((h | g) < m);
				if (_) break;
				do {
					if (h = i - En(o[u], e, r, i, i - 1, t), h !== 0) {
						for (d -= h, l -= h, i -= h, p = d + 1, f = l + 1, c = h - 1; c >= 0; c--) e[p + c] = e[f + c];
						if (i === 0) {
							_ = !0;
							break;
						}
					}
					if (e[d--] = o[u--], --s === 1) {
						_ = !0;
						break;
					}
					if (g = s - Tn(e[l], o, 0, s, s - 1, t), g !== 0) {
						for (d -= g, u -= g, s -= g, p = d + 1, f = u + 1, c = 0; c < g; c++) e[p + c] = o[f + c];
						if (s <= 1) {
							_ = !0;
							break;
						}
					}
					if (e[d--] = e[l--], --i === 0) {
						_ = !0;
						break;
					}
					m--;
				} while (h >= bn || g >= bn);
				if (_) break;
				m < 0 && (m = 0), m += 2;
			}
			if (n = m, n < 1 && (n = 1), s === 1) {
				for (d -= i, l -= i, p = d + 1, f = l + 1, c = i - 1; c >= 0; c--) e[p + c] = e[f + c];
				e[d] = o[u];
			} else if (s === 0) throw Error();
			else for (f = d - (s - 1), c = 0; c < s; c++) e[f + c] = o[c];
		}
	}
	return {
		mergeRuns: c,
		forceMergeRuns: l,
		pushRun: s
	};
}
function On(e, t, n, r) {
	n ||= 0, r ||= e.length;
	var i = r - n;
	if (!(i < 2)) {
		var a = 0;
		if (i < yn) a = Sn(e, n, r, t), wn(e, n, r, n + a, t);
		else {
			var o = Dn(e, t), s = xn(i);
			do {
				if (a = Sn(e, n, r, t), a < s) {
					var c = i;
					c > s && (c = s), wn(e, n, n + c, n + a, t), a = c;
				}
				o.pushRun(n, a), o.mergeRuns(), i -= a, n += a;
			} while (i !== 0);
			o.forceMergeRuns();
		}
	}
}
//#endregion
//#region node_modules/zrender/lib/Storage.js
var kn = !1;
function An() {
	kn || (kn = !0, console.warn("z / z2 / zlevel of displayable is invalid, which may cause unexpected errors"));
}
function jn(e, t) {
	return e.zlevel === t.zlevel ? e.z === t.z ? e.z2 - t.z2 : e.z - t.z : e.zlevel - t.zlevel;
}
var Mn = function() {
	function e() {
		this._roots = [], this._displayList = [], this._displayListLen = 0, this.displayableSortFunc = jn;
	}
	return e.prototype.traverse = function(e, t) {
		for (var n = 0; n < this._roots.length; n++) this._roots[n].traverse(e, t);
	}, e.prototype.getDisplayList = function(e, t) {
		t ||= !1;
		var n = this._displayList;
		return (e || !n.length) && this.updateDisplayList(t), n;
	}, e.prototype.updateDisplayList = function(e) {
		this._displayListLen = 0;
		for (var t = this._roots, n = this._displayList, r = 0, i = t.length; r < i; r++) this._updateAndAddDisplayable(t[r], null, e);
		n.length = this._displayListLen, On(n, jn);
	}, e.prototype._updateAndAddDisplayable = function(e, t, n) {
		if (!e.ignore || n) {
			e.beforeUpdate(), e.update(), e.afterUpdate();
			var r = e.getClipPath(), i = t && t.length, a = 0, o = e.__clipPaths;
			if (!e.ignoreClip && (i || r)) {
				if (o ||= e.__clipPaths = [], i) for (var s = 0; s < t.length; s++) o[a++] = t[s];
				for (var c = r, l = e; c;) c.parent = l, c.updateTransform(), o[a++] = c, l = c, c = c.getClipPath();
			}
			if (o && (o.length = a), e.childrenRef) {
				for (var u = e.childrenRef(), d = 0; d < u.length; d++) {
					var f = u[d];
					e.__dirty && (f.__dirty |= 1), this._updateAndAddDisplayable(f, o, n);
				}
				e.__dirty = 0;
			} else {
				var p = e;
				isNaN(p.z) && (An(), p.z = 0), isNaN(p.z2) && (An(), p.z2 = 0), isNaN(p.zlevel) && (An(), p.zlevel = 0), this._displayList[this._displayListLen++] = p;
			}
			var m = e.getDecalElement && e.getDecalElement();
			m && this._updateAndAddDisplayable(m, o, n);
			var h = e.getTextGuideLine();
			h && this._updateAndAddDisplayable(h, o, n);
			var g = e.getTextContent();
			g && this._updateAndAddDisplayable(g, o, n);
		}
	}, e.prototype.addRoot = function(e) {
		e.__zr && e.__zr.storage === this || this._roots.push(e);
	}, e.prototype.delRoot = function(e) {
		if (e instanceof Array) for (var t = 0, n = e.length; t < n; t++) this.delRoot(e[t]);
		else {
			var r = N(this._roots, e);
			r >= 0 && this._roots.splice(r, 1);
		}
	}, e.prototype.delAllRoots = function() {
		this._roots = [], this._displayList = [], this._displayListLen = 0;
	}, e.prototype.getRoots = function() {
		return this._roots;
	}, e.prototype.dispose = function() {
		this._displayList = null, this._roots = null;
	}, e;
}(), Nn = a.hasGlobalWindow && (window.requestAnimationFrame && window.requestAnimationFrame.bind(window) || window.msRequestAnimationFrame && window.msRequestAnimationFrame.bind(window) || window.mozRequestAnimationFrame || window.webkitRequestAnimationFrame) || function(e) {
	return setTimeout(e, 16);
}, Pn = {
	linear: function(e) {
		return e;
	},
	quadraticIn: function(e) {
		return e * e;
	},
	quadraticOut: function(e) {
		return e * (2 - e);
	},
	quadraticInOut: function(e) {
		return (e *= 2) < 1 ? .5 * e * e : -.5 * (--e * (e - 2) - 1);
	},
	cubicIn: function(e) {
		return e * e * e;
	},
	cubicOut: function(e) {
		return --e * e * e + 1;
	},
	cubicInOut: function(e) {
		return (e *= 2) < 1 ? .5 * e * e * e : .5 * ((e -= 2) * e * e + 2);
	},
	quarticIn: function(e) {
		return e * e * e * e;
	},
	quarticOut: function(e) {
		return 1 - --e * e * e * e;
	},
	quarticInOut: function(e) {
		return (e *= 2) < 1 ? .5 * e * e * e * e : -.5 * ((e -= 2) * e * e * e - 2);
	},
	quinticIn: function(e) {
		return e * e * e * e * e;
	},
	quinticOut: function(e) {
		return --e * e * e * e * e + 1;
	},
	quinticInOut: function(e) {
		return (e *= 2) < 1 ? .5 * e * e * e * e * e : .5 * ((e -= 2) * e * e * e * e + 2);
	},
	sinusoidalIn: function(e) {
		return 1 - Math.cos(e * Math.PI / 2);
	},
	sinusoidalOut: function(e) {
		return Math.sin(e * Math.PI / 2);
	},
	sinusoidalInOut: function(e) {
		return .5 * (1 - Math.cos(Math.PI * e));
	},
	exponentialIn: function(e) {
		return e === 0 ? 0 : 1024 ** (e - 1);
	},
	exponentialOut: function(e) {
		return e === 1 ? 1 : 1 - 2 ** (-10 * e);
	},
	exponentialInOut: function(e) {
		return e === 0 ? 0 : e === 1 ? 1 : (e *= 2) < 1 ? .5 * 1024 ** (e - 1) : .5 * (-(2 ** (-10 * (e - 1))) + 2);
	},
	circularIn: function(e) {
		return 1 - Math.sqrt(1 - e * e);
	},
	circularOut: function(e) {
		return Math.sqrt(1 - --e * e);
	},
	circularInOut: function(e) {
		return (e *= 2) < 1 ? -.5 * (Math.sqrt(1 - e * e) - 1) : .5 * (Math.sqrt(1 - (e -= 2) * e) + 1);
	},
	elasticIn: function(e) {
		var t, n = .1, r = .4;
		return e === 0 ? 0 : e === 1 ? 1 : (!n || n < 1 ? (n = 1, t = r / 4) : t = r * Math.asin(1 / n) / (2 * Math.PI), -(n * 2 ** (10 * --e) * Math.sin((e - t) * (2 * Math.PI) / r)));
	},
	elasticOut: function(e) {
		var t, n = .1, r = .4;
		return e === 0 ? 0 : e === 1 ? 1 : (!n || n < 1 ? (n = 1, t = r / 4) : t = r * Math.asin(1 / n) / (2 * Math.PI), n * 2 ** (-10 * e) * Math.sin((e - t) * (2 * Math.PI) / r) + 1);
	},
	elasticInOut: function(e) {
		var t, n = .1, r = .4;
		return e === 0 ? 0 : e === 1 ? 1 : (!n || n < 1 ? (n = 1, t = r / 4) : t = r * Math.asin(1 / n) / (2 * Math.PI), (e *= 2) < 1 ? -.5 * (n * 2 ** (10 * --e) * Math.sin((e - t) * (2 * Math.PI) / r)) : n * 2 ** (-10 * --e) * Math.sin((e - t) * (2 * Math.PI) / r) * .5 + 1);
	},
	backIn: function(e) {
		var t = 1.70158;
		return e * e * ((t + 1) * e - t);
	},
	backOut: function(e) {
		var t = 1.70158;
		return --e * e * ((t + 1) * e + t) + 1;
	},
	backInOut: function(e) {
		var t = 2.5949095;
		return (e *= 2) < 1 ? .5 * (e * e * ((t + 1) * e - t)) : .5 * ((e -= 2) * e * ((t + 1) * e + t) + 2);
	},
	bounceIn: function(e) {
		return 1 - Pn.bounceOut(1 - e);
	},
	bounceOut: function(e) {
		return e < 1 / 2.75 ? 7.5625 * e * e : e < 2 / 2.75 ? 7.5625 * (e -= 1.5 / 2.75) * e + .75 : e < 2.5 / 2.75 ? 7.5625 * (e -= 2.25 / 2.75) * e + .9375 : 7.5625 * (e -= 2.625 / 2.75) * e + .984375;
	},
	bounceInOut: function(e) {
		return e < .5 ? Pn.bounceIn(e * 2) * .5 : Pn.bounceOut(e * 2 - 1) * .5 + .5;
	}
}, Fn = Math.pow, In = Math.sqrt, Ln = 1e-8, Rn = 1e-4, zn = In(3), Bn = 1 / 3, Vn = Me(), Hn = Me(), Un = Me();
function Wn(e) {
	return e > -Ln && e < Ln;
}
function Gn(e) {
	return e > Ln || e < -Ln;
}
function Kn(e, t, n, r, i) {
	var a = 1 - i;
	return a * a * (a * e + 3 * i * t) + i * i * (i * r + 3 * a * n);
}
function qn(e, t, n, r, i) {
	var a = 1 - i;
	return 3 * (((t - e) * a + 2 * (n - t) * i) * a + (r - n) * i * i);
}
function Jn(e, t, n, r, i, a) {
	var o = r + 3 * (t - n) - e, s = 3 * (n - t * 2 + e), c = 3 * (t - e), l = e - i, u = s * s - 3 * o * c, d = s * c - 9 * o * l, f = c * c - 3 * s * l, p = 0;
	if (Wn(u) && Wn(d)) {
		if (Wn(s)) a[0] = 0;
		else {
			var m = -c / s;
			m >= 0 && m <= 1 && (a[p++] = m);
		}
	} else {
		var h = d * d - 4 * u * f;
		if (Wn(h)) {
			var g = d / u, m = -s / o + g, _ = -g / 2;
			m >= 0 && m <= 1 && (a[p++] = m), _ >= 0 && _ <= 1 && (a[p++] = _);
		} else if (h > 0) {
			var v = In(h), y = u * s + 1.5 * o * (-d + v), b = u * s + 1.5 * o * (-d - v);
			y = y < 0 ? -Fn(-y, Bn) : Fn(y, Bn), b = b < 0 ? -Fn(-b, Bn) : Fn(b, Bn);
			var m = (-s - (y + b)) / (3 * o);
			m >= 0 && m <= 1 && (a[p++] = m);
		} else {
			var x = (2 * u * s - 3 * o * d) / (2 * In(u * u * u)), S = Math.acos(x) / 3, C = In(u), w = Math.cos(S), m = (-s - 2 * C * w) / (3 * o), _ = (-s + C * (w + zn * Math.sin(S))) / (3 * o), T = (-s + C * (w - zn * Math.sin(S))) / (3 * o);
			m >= 0 && m <= 1 && (a[p++] = m), _ >= 0 && _ <= 1 && (a[p++] = _), T >= 0 && T <= 1 && (a[p++] = T);
		}
	}
	return p;
}
function Yn(e, t, n, r, i) {
	var a = 6 * n - 12 * t + 6 * e, o = 9 * t + 3 * r - 3 * e - 9 * n, s = 3 * t - 3 * e, c = 0;
	if (Wn(o)) {
		if (Gn(a)) {
			var l = -s / a;
			l >= 0 && l <= 1 && (i[c++] = l);
		}
	} else {
		var u = a * a - 4 * o * s;
		if (Wn(u)) i[0] = -a / (2 * o);
		else if (u > 0) {
			var d = In(u), l = (-a + d) / (2 * o), f = (-a - d) / (2 * o);
			l >= 0 && l <= 1 && (i[c++] = l), f >= 0 && f <= 1 && (i[c++] = f);
		}
	}
	return c;
}
function Xn(e, t, n, r, i, a) {
	var o = (t - e) * i + e, s = (n - t) * i + t, c = (r - n) * i + n, l = (s - o) * i + o, u = (c - s) * i + s, d = (u - l) * i + l;
	a[0] = e, a[1] = o, a[2] = l, a[3] = d, a[4] = d, a[5] = u, a[6] = c, a[7] = r;
}
function Zn(e, t, n, r, i, a, o, s, c, l, u) {
	var d, f = .005, p = Infinity, m, h, g, _;
	Vn[0] = c, Vn[1] = l;
	for (var v = 0; v < 1; v += .05) Hn[0] = Kn(e, n, i, o, v), Hn[1] = Kn(t, r, a, s, v), g = Ke(Vn, Hn), g < p && (d = v, p = g);
	p = Infinity;
	for (var y = 0; y < 32 && !(f < Rn); y++) m = d - f, h = d + f, Hn[0] = Kn(e, n, i, o, m), Hn[1] = Kn(t, r, a, s, m), g = Ke(Hn, Vn), m >= 0 && g < p ? (d = m, p = g) : (Un[0] = Kn(e, n, i, o, h), Un[1] = Kn(t, r, a, s, h), _ = Ke(Un, Vn), h <= 1 && _ < p ? (d = h, p = _) : f *= .5);
	return u && (u[0] = Kn(e, n, i, o, d), u[1] = Kn(t, r, a, s, d)), In(p);
}
function Qn(e, t, n, r, i, a, o, s, c) {
	for (var l = e, u = t, d = 0, f = 1 / c, p = 1; p <= c; p++) {
		var m = p * f, h = Kn(e, n, i, o, m), g = Kn(t, r, a, s, m), _ = h - l, v = g - u;
		d += Math.sqrt(_ * _ + v * v), l = h, u = g;
	}
	return d;
}
function $n(e, t, n, r) {
	var i = 1 - r;
	return i * (i * e + 2 * r * t) + r * r * n;
}
function er(e, t, n, r) {
	return 2 * ((1 - r) * (t - e) + r * (n - t));
}
function tr(e, t, n, r, i) {
	var a = e - 2 * t + n, o = 2 * (t - e), s = e - r, c = 0;
	if (Wn(a)) {
		if (Gn(o)) {
			var l = -s / o;
			l >= 0 && l <= 1 && (i[c++] = l);
		}
	} else {
		var u = o * o - 4 * a * s;
		if (Wn(u)) {
			var l = -o / (2 * a);
			l >= 0 && l <= 1 && (i[c++] = l);
		} else if (u > 0) {
			var d = In(u), l = (-o + d) / (2 * a), f = (-o - d) / (2 * a);
			l >= 0 && l <= 1 && (i[c++] = l), f >= 0 && f <= 1 && (i[c++] = f);
		}
	}
	return c;
}
function nr(e, t, n) {
	var r = e + n - 2 * t;
	return r === 0 ? .5 : (e - t) / r;
}
function rr(e, t, n, r, i) {
	var a = (t - e) * r + e, o = (n - t) * r + t, s = (o - a) * r + a;
	i[0] = e, i[1] = a, i[2] = s, i[3] = s, i[4] = o, i[5] = n;
}
function ir(e, t, n, r, i, a, o, s, c) {
	var l, u = .005, d = Infinity;
	Vn[0] = o, Vn[1] = s;
	for (var f = 0; f < 1; f += .05) {
		Hn[0] = $n(e, n, i, f), Hn[1] = $n(t, r, a, f);
		var p = Ke(Vn, Hn);
		p < d && (l = f, d = p);
	}
	d = Infinity;
	for (var m = 0; m < 32 && !(u < Rn); m++) {
		var h = l - u, g = l + u;
		Hn[0] = $n(e, n, i, h), Hn[1] = $n(t, r, a, h);
		var p = Ke(Hn, Vn);
		if (h >= 0 && p < d) l = h, d = p;
		else {
			Un[0] = $n(e, n, i, g), Un[1] = $n(t, r, a, g);
			var _ = Ke(Un, Vn);
			g <= 1 && _ < d ? (l = g, d = _) : u *= .5;
		}
	}
	return c && (c[0] = $n(e, n, i, l), c[1] = $n(t, r, a, l)), In(d);
}
function ar(e, t, n, r, i, a, o) {
	for (var s = e, c = t, l = 0, u = 1 / o, d = 1; d <= o; d++) {
		var f = d * u, p = $n(e, n, i, f), m = $n(t, r, a, f), h = p - s, g = m - c;
		l += Math.sqrt(h * h + g * g), s = p, c = m;
	}
	return l;
}
//#endregion
//#region node_modules/zrender/lib/animation/cubicEasing.js
var or = /cubic-bezier\(([0-9,\.e ]+)\)/;
function sr(e) {
	var t = e && or.exec(e);
	if (t) {
		var n = t[1].split(","), r = +ve(n[0]), i = +ve(n[1]), a = +ve(n[2]), o = +ve(n[3]);
		if (isNaN(r + i + a + o)) return;
		var s = [];
		return function(e) {
			return e <= 0 ? 0 : e >= 1 ? 1 : Jn(0, r, a, 1, e, s) && Kn(0, i, o, 1, s[0]);
		};
	}
}
//#endregion
//#region node_modules/zrender/lib/animation/Clip.js
var cr = function() {
	function e(e) {
		this._inited = !1, this._startTime = 0, this._pausedTime = 0, this._paused = !1, this._life = e.life || 1e3, this._delay = e.delay || 0, this.loop = e.loop || !1, this.onframe = e.onframe || Ae, this.ondestroy = e.ondestroy || Ae, this.onrestart = e.onrestart || Ae, e.easing && this.setEasing(e.easing);
	}
	return e.prototype.step = function(e, t) {
		if (this._inited ||= (this._startTime = e + this._delay, !0), this._paused) this._pausedTime += t;
		else {
			var n = this._life, r = e - this._startTime - this._pausedTime, i = r / n;
			i < 0 && (i = 0), i = Math.min(i, 1);
			var a = this.easingFunc, o = a ? a(i) : i;
			if (this.onframe(o), i === 1) {
				if (this.loop) {
					var s = r % n;
					this._startTime = e - s, this._pausedTime = 0, this.onrestart();
				} else return !0;
			}
			return !1;
		}
	}, e.prototype.pause = function() {
		this._paused = !0;
	}, e.prototype.resume = function() {
		this._paused = !1;
	}, e.prototype.setEasing = function(e) {
		this.easing = e, this.easingFunc = V(e) ? e : Pn[e] || sr(e);
	}, e;
}(), lr = function() {
	function e(e) {
		this.value = e;
	}
	return e;
}(), ur = function() {
	function e() {
		this._len = 0;
	}
	return e.prototype.insert = function(e) {
		var t = new lr(e);
		return this.insertEntry(t), t;
	}, e.prototype.insertEntry = function(e) {
		this.head ? (this.tail.next = e, e.prev = this.tail, e.next = null, this.tail = e) : this.head = this.tail = e, this._len++;
	}, e.prototype.remove = function(e) {
		var t = e.prev, n = e.next;
		t ? t.next = n : this.head = n, n ? n.prev = t : this.tail = t, e.next = e.prev = null, this._len--;
	}, e.prototype.len = function() {
		return this._len;
	}, e.prototype.clear = function() {
		this.head = this.tail = null, this._len = 0;
	}, e;
}(), dr = function() {
	function e(e) {
		this._list = new ur(), this._maxSize = 10, this._map = {}, this._maxSize = e;
	}
	return e.prototype.put = function(e, t) {
		var n = this._list, r = this._map, i = null;
		if (r[e] == null) {
			var a = n.len(), o = this._lastRemovedEntry;
			if (a >= this._maxSize && a > 0) {
				var s = n.head;
				n.remove(s), delete r[s.key], i = s.value, this._lastRemovedEntry = s;
			}
			o ? o.value = t : o = new lr(t), o.key = e, n.insertEntry(o), r[e] = o;
		}
		return i;
	}, e.prototype.get = function(e) {
		var t = this._map[e], n = this._list;
		if (t != null) return t !== n.tail && (n.remove(t), n.insertEntry(t)), t.value;
	}, e.prototype.clear = function() {
		this._list.clear(), this._map = {};
	}, e.prototype.len = function() {
		return this._list.len();
	}, e;
}(), fr = {
	transparent: [
		0,
		0,
		0,
		0
	],
	aliceblue: [
		240,
		248,
		255,
		1
	],
	antiquewhite: [
		250,
		235,
		215,
		1
	],
	aqua: [
		0,
		255,
		255,
		1
	],
	aquamarine: [
		127,
		255,
		212,
		1
	],
	azure: [
		240,
		255,
		255,
		1
	],
	beige: [
		245,
		245,
		220,
		1
	],
	bisque: [
		255,
		228,
		196,
		1
	],
	black: [
		0,
		0,
		0,
		1
	],
	blanchedalmond: [
		255,
		235,
		205,
		1
	],
	blue: [
		0,
		0,
		255,
		1
	],
	blueviolet: [
		138,
		43,
		226,
		1
	],
	brown: [
		165,
		42,
		42,
		1
	],
	burlywood: [
		222,
		184,
		135,
		1
	],
	cadetblue: [
		95,
		158,
		160,
		1
	],
	chartreuse: [
		127,
		255,
		0,
		1
	],
	chocolate: [
		210,
		105,
		30,
		1
	],
	coral: [
		255,
		127,
		80,
		1
	],
	cornflowerblue: [
		100,
		149,
		237,
		1
	],
	cornsilk: [
		255,
		248,
		220,
		1
	],
	crimson: [
		220,
		20,
		60,
		1
	],
	cyan: [
		0,
		255,
		255,
		1
	],
	darkblue: [
		0,
		0,
		139,
		1
	],
	darkcyan: [
		0,
		139,
		139,
		1
	],
	darkgoldenrod: [
		184,
		134,
		11,
		1
	],
	darkgray: [
		169,
		169,
		169,
		1
	],
	darkgreen: [
		0,
		100,
		0,
		1
	],
	darkgrey: [
		169,
		169,
		169,
		1
	],
	darkkhaki: [
		189,
		183,
		107,
		1
	],
	darkmagenta: [
		139,
		0,
		139,
		1
	],
	darkolivegreen: [
		85,
		107,
		47,
		1
	],
	darkorange: [
		255,
		140,
		0,
		1
	],
	darkorchid: [
		153,
		50,
		204,
		1
	],
	darkred: [
		139,
		0,
		0,
		1
	],
	darksalmon: [
		233,
		150,
		122,
		1
	],
	darkseagreen: [
		143,
		188,
		143,
		1
	],
	darkslateblue: [
		72,
		61,
		139,
		1
	],
	darkslategray: [
		47,
		79,
		79,
		1
	],
	darkslategrey: [
		47,
		79,
		79,
		1
	],
	darkturquoise: [
		0,
		206,
		209,
		1
	],
	darkviolet: [
		148,
		0,
		211,
		1
	],
	deeppink: [
		255,
		20,
		147,
		1
	],
	deepskyblue: [
		0,
		191,
		255,
		1
	],
	dimgray: [
		105,
		105,
		105,
		1
	],
	dimgrey: [
		105,
		105,
		105,
		1
	],
	dodgerblue: [
		30,
		144,
		255,
		1
	],
	firebrick: [
		178,
		34,
		34,
		1
	],
	floralwhite: [
		255,
		250,
		240,
		1
	],
	forestgreen: [
		34,
		139,
		34,
		1
	],
	fuchsia: [
		255,
		0,
		255,
		1
	],
	gainsboro: [
		220,
		220,
		220,
		1
	],
	ghostwhite: [
		248,
		248,
		255,
		1
	],
	gold: [
		255,
		215,
		0,
		1
	],
	goldenrod: [
		218,
		165,
		32,
		1
	],
	gray: [
		128,
		128,
		128,
		1
	],
	green: [
		0,
		128,
		0,
		1
	],
	greenyellow: [
		173,
		255,
		47,
		1
	],
	grey: [
		128,
		128,
		128,
		1
	],
	honeydew: [
		240,
		255,
		240,
		1
	],
	hotpink: [
		255,
		105,
		180,
		1
	],
	indianred: [
		205,
		92,
		92,
		1
	],
	indigo: [
		75,
		0,
		130,
		1
	],
	ivory: [
		255,
		255,
		240,
		1
	],
	khaki: [
		240,
		230,
		140,
		1
	],
	lavender: [
		230,
		230,
		250,
		1
	],
	lavenderblush: [
		255,
		240,
		245,
		1
	],
	lawngreen: [
		124,
		252,
		0,
		1
	],
	lemonchiffon: [
		255,
		250,
		205,
		1
	],
	lightblue: [
		173,
		216,
		230,
		1
	],
	lightcoral: [
		240,
		128,
		128,
		1
	],
	lightcyan: [
		224,
		255,
		255,
		1
	],
	lightgoldenrodyellow: [
		250,
		250,
		210,
		1
	],
	lightgray: [
		211,
		211,
		211,
		1
	],
	lightgreen: [
		144,
		238,
		144,
		1
	],
	lightgrey: [
		211,
		211,
		211,
		1
	],
	lightpink: [
		255,
		182,
		193,
		1
	],
	lightsalmon: [
		255,
		160,
		122,
		1
	],
	lightseagreen: [
		32,
		178,
		170,
		1
	],
	lightskyblue: [
		135,
		206,
		250,
		1
	],
	lightslategray: [
		119,
		136,
		153,
		1
	],
	lightslategrey: [
		119,
		136,
		153,
		1
	],
	lightsteelblue: [
		176,
		196,
		222,
		1
	],
	lightyellow: [
		255,
		255,
		224,
		1
	],
	lime: [
		0,
		255,
		0,
		1
	],
	limegreen: [
		50,
		205,
		50,
		1
	],
	linen: [
		250,
		240,
		230,
		1
	],
	magenta: [
		255,
		0,
		255,
		1
	],
	maroon: [
		128,
		0,
		0,
		1
	],
	mediumaquamarine: [
		102,
		205,
		170,
		1
	],
	mediumblue: [
		0,
		0,
		205,
		1
	],
	mediumorchid: [
		186,
		85,
		211,
		1
	],
	mediumpurple: [
		147,
		112,
		219,
		1
	],
	mediumseagreen: [
		60,
		179,
		113,
		1
	],
	mediumslateblue: [
		123,
		104,
		238,
		1
	],
	mediumspringgreen: [
		0,
		250,
		154,
		1
	],
	mediumturquoise: [
		72,
		209,
		204,
		1
	],
	mediumvioletred: [
		199,
		21,
		133,
		1
	],
	midnightblue: [
		25,
		25,
		112,
		1
	],
	mintcream: [
		245,
		255,
		250,
		1
	],
	mistyrose: [
		255,
		228,
		225,
		1
	],
	moccasin: [
		255,
		228,
		181,
		1
	],
	navajowhite: [
		255,
		222,
		173,
		1
	],
	navy: [
		0,
		0,
		128,
		1
	],
	oldlace: [
		253,
		245,
		230,
		1
	],
	olive: [
		128,
		128,
		0,
		1
	],
	olivedrab: [
		107,
		142,
		35,
		1
	],
	orange: [
		255,
		165,
		0,
		1
	],
	orangered: [
		255,
		69,
		0,
		1
	],
	orchid: [
		218,
		112,
		214,
		1
	],
	palegoldenrod: [
		238,
		232,
		170,
		1
	],
	palegreen: [
		152,
		251,
		152,
		1
	],
	paleturquoise: [
		175,
		238,
		238,
		1
	],
	palevioletred: [
		219,
		112,
		147,
		1
	],
	papayawhip: [
		255,
		239,
		213,
		1
	],
	peachpuff: [
		255,
		218,
		185,
		1
	],
	peru: [
		205,
		133,
		63,
		1
	],
	pink: [
		255,
		192,
		203,
		1
	],
	plum: [
		221,
		160,
		221,
		1
	],
	powderblue: [
		176,
		224,
		230,
		1
	],
	purple: [
		128,
		0,
		128,
		1
	],
	red: [
		255,
		0,
		0,
		1
	],
	rosybrown: [
		188,
		143,
		143,
		1
	],
	royalblue: [
		65,
		105,
		225,
		1
	],
	saddlebrown: [
		139,
		69,
		19,
		1
	],
	salmon: [
		250,
		128,
		114,
		1
	],
	sandybrown: [
		244,
		164,
		96,
		1
	],
	seagreen: [
		46,
		139,
		87,
		1
	],
	seashell: [
		255,
		245,
		238,
		1
	],
	sienna: [
		160,
		82,
		45,
		1
	],
	silver: [
		192,
		192,
		192,
		1
	],
	skyblue: [
		135,
		206,
		235,
		1
	],
	slateblue: [
		106,
		90,
		205,
		1
	],
	slategray: [
		112,
		128,
		144,
		1
	],
	slategrey: [
		112,
		128,
		144,
		1
	],
	snow: [
		255,
		250,
		250,
		1
	],
	springgreen: [
		0,
		255,
		127,
		1
	],
	steelblue: [
		70,
		130,
		180,
		1
	],
	tan: [
		210,
		180,
		140,
		1
	],
	teal: [
		0,
		128,
		128,
		1
	],
	thistle: [
		216,
		191,
		216,
		1
	],
	tomato: [
		255,
		99,
		71,
		1
	],
	turquoise: [
		64,
		224,
		208,
		1
	],
	violet: [
		238,
		130,
		238,
		1
	],
	wheat: [
		245,
		222,
		179,
		1
	],
	white: [
		255,
		255,
		255,
		1
	],
	whitesmoke: [
		245,
		245,
		245,
		1
	],
	yellow: [
		255,
		255,
		0,
		1
	],
	yellowgreen: [
		154,
		205,
		50,
		1
	]
};
function pr(e) {
	return e = Math.round(e), e < 0 ? 0 : e > 255 ? 255 : e;
}
function mr(e) {
	return e = Math.round(e), e < 0 ? 0 : e > 360 ? 360 : e;
}
function hr(e) {
	return e < 0 ? 0 : e > 1 ? 1 : e;
}
function gr(e) {
	var t = e;
	return t.length && t.charAt(t.length - 1) === "%" ? pr(parseFloat(t) / 100 * 255) : pr(parseInt(t, 10));
}
function _r(e) {
	var t = e;
	return t.length && t.charAt(t.length - 1) === "%" ? hr(parseFloat(t) / 100) : hr(parseFloat(t));
}
function vr(e, t, n) {
	return n < 0 ? n += 1 : n > 1 && --n, n * 6 < 1 ? e + (t - e) * n * 6 : n * 2 < 1 ? t : n * 3 < 2 ? e + (t - e) * (2 / 3 - n) * 6 : e;
}
function yr(e, t, n, r, i) {
	return e[0] = t, e[1] = n, e[2] = r, e[3] = i, e;
}
function br(e, t) {
	return e[0] = t[0], e[1] = t[1], e[2] = t[2], e[3] = t[3], e;
}
var xr = new dr(20), Sr = null;
function Cr(e, t) {
	Sr && br(Sr, t), Sr = xr.put(e, Sr || t.slice());
}
function wr(e, t) {
	if (e) {
		t ||= [];
		var n = xr.get(e);
		if (n) return br(t, n);
		e += "";
		var r = e.replace(/ /g, "").toLowerCase();
		if (r in fr) return br(t, fr[r]), Cr(e, t), t;
		var i = r.length;
		if (r.charAt(0) === "#") {
			if (i === 4 || i === 5) {
				var a = parseInt(r.slice(1, 4), 16);
				if (!(a >= 0 && a <= 4095)) {
					yr(t, 0, 0, 0, 1);
					return;
				}
				return yr(t, (a & 3840) >> 4 | (a & 3840) >> 8, a & 240 | (a & 240) >> 4, a & 15 | (a & 15) << 4, i === 5 ? parseInt(r.slice(4), 16) / 15 : 1), Cr(e, t), t;
			}
			if (i === 7 || i === 9) {
				var a = parseInt(r.slice(1, 7), 16);
				if (!(a >= 0 && a <= 16777215)) {
					yr(t, 0, 0, 0, 1);
					return;
				}
				return yr(t, (a & 16711680) >> 16, (a & 65280) >> 8, a & 255, i === 9 ? parseInt(r.slice(7), 16) / 255 : 1), Cr(e, t), t;
			}
		} else {
			var o = r.indexOf("("), s = r.indexOf(")");
			if (o !== -1 && s + 1 === i) {
				var c = r.substr(0, o), l = r.substr(o + 1, s - (o + 1)).split(","), u = 1;
				switch (c) {
					case "rgba":
						if (l.length !== 4) return l.length === 3 ? yr(t, +l[0], +l[1], +l[2], 1) : yr(t, 0, 0, 0, 1);
						u = _r(l.pop());
					case "rgb":
						if (l.length >= 3) return yr(t, gr(l[0]), gr(l[1]), gr(l[2]), l.length === 3 ? u : _r(l[3])), Cr(e, t), t;
						yr(t, 0, 0, 0, 1);
						return;
					case "hsla":
						if (l.length !== 4) {
							yr(t, 0, 0, 0, 1);
							return;
						}
						return l[3] = _r(l[3]), Tr(l, t), Cr(e, t), t;
					case "hsl":
						if (l.length !== 3) {
							yr(t, 0, 0, 0, 1);
							return;
						}
						return Tr(l, t), Cr(e, t), t;
					default: return;
				}
			}
			yr(t, 0, 0, 0, 1);
		}
	}
}
function Tr(e, t) {
	var n = (parseFloat(e[0]) % 360 + 360) % 360 / 360, r = _r(e[1]), i = _r(e[2]), a = i <= .5 ? i * (r + 1) : i + r - i * r, o = i * 2 - a;
	return t ||= [], yr(t, pr(vr(o, a, n + 1 / 3) * 255), pr(vr(o, a, n) * 255), pr(vr(o, a, n - 1 / 3) * 255), 1), e.length === 4 && (t[3] = e[3]), t;
}
function Er(e) {
	if (e) {
		var t = e[0] / 255, n = e[1] / 255, r = e[2] / 255, i = Math.min(t, n, r), a = Math.max(t, n, r), o = a - i, s = (a + i) / 2, c, l;
		if (o === 0) c = 0, l = 0;
		else {
			l = s < .5 ? o / (a + i) : o / (2 - a - i);
			var u = ((a - t) / 6 + o / 2) / o, d = ((a - n) / 6 + o / 2) / o, f = ((a - r) / 6 + o / 2) / o;
			t === a ? c = f - d : n === a ? c = 1 / 3 + u - f : r === a && (c = 2 / 3 + d - u), c < 0 && (c += 1), c > 1 && --c;
		}
		var p = [
			c * 360,
			l,
			s
		];
		return e[3] != null && p.push(e[3]), p;
	}
}
function Dr(e, t) {
	var n = wr(e);
	if (n) {
		for (var r = 0; r < 3; r++) t < 0 ? n[r] = n[r] * (1 - t) | 0 : n[r] = (255 - n[r]) * t + n[r] | 0, n[r] > 255 ? n[r] = 255 : n[r] < 0 && (n[r] = 0);
		return kr(n, n.length === 4 ? "rgba" : "rgb");
	}
}
function Or(e, t, n, r) {
	var i = wr(e);
	if (e) return i = Er(i), t != null && (i[0] = mr(V(t) ? t(i[0]) : t)), n != null && (i[1] = _r(V(n) ? n(i[1]) : n)), r != null && (i[2] = _r(V(r) ? r(i[2]) : r)), kr(Tr(i), "rgba");
}
function kr(e, t) {
	if (e && e.length) {
		var n = e[0] + "," + e[1] + "," + e[2];
		return (t === "rgba" || t === "hsva" || t === "hsla") && (n += "," + e[3]), t + "(" + n + ")";
	}
}
function Ar(e, t) {
	var n = wr(e);
	return n ? (.299 * n[0] + .587 * n[1] + .114 * n[2]) * n[3] / 255 + (1 - n[3]) * t : 0;
}
var jr = new dr(100);
function Mr(e) {
	if (H(e)) {
		var t = jr.get(e);
		return t || (t = Dr(e, -.1), jr.put(e, t)), t;
	}
	if (ue(e)) {
		var n = j({}, e);
		return n.colorStops = I(e.colorStops, function(e) {
			return {
				offset: e.offset,
				color: Dr(e.color, -.1)
			};
		}), n;
	}
	return e;
}
//#endregion
//#region node_modules/zrender/lib/svg/helper.js
function Nr(e) {
	return e.type === "linear";
}
function Pr(e) {
	return e.type === "radial";
}
(function() {
	return typeof Buffer < "u" && typeof Buffer.from == "function" ? function(e) {
		return Buffer.from(e).toString("base64");
	} : typeof btoa == "function" && typeof unescape == "function" && typeof encodeURIComponent == "function" ? function(e) {
		return btoa(unescape(encodeURIComponent(e)));
	} : function(e) {
		return null;
	};
})();
//#endregion
//#region node_modules/zrender/lib/animation/Animator.js
var Fr = Array.prototype.slice;
function Ir(e, t, n) {
	return (t - e) * n + e;
}
function Lr(e, t, n, r) {
	for (var i = t.length, a = 0; a < i; a++) e[a] = Ir(t[a], n[a], r);
	return e;
}
function Rr(e, t, n, r) {
	for (var i = t.length, a = i && t[0].length, o = 0; o < i; o++) {
		e[o] || (e[o] = []);
		for (var s = 0; s < a; s++) e[o][s] = Ir(t[o][s], n[o][s], r);
	}
	return e;
}
function zr(e, t, n, r) {
	for (var i = t.length, a = 0; a < i; a++) e[a] = t[a] + n[a] * r;
	return e;
}
function Br(e, t, n, r) {
	for (var i = t.length, a = i && t[0].length, o = 0; o < i; o++) {
		e[o] || (e[o] = []);
		for (var s = 0; s < a; s++) e[o][s] = t[o][s] + n[o][s] * r;
	}
	return e;
}
function Vr(e, t) {
	for (var n = e.length, r = t.length, i = n > r ? t : e, a = Math.min(n, r), o = i[a - 1] || {
		color: [
			0,
			0,
			0,
			0
		],
		offset: 0
	}, s = a; s < Math.max(n, r); s++) i.push({
		offset: o.offset,
		color: o.color.slice()
	});
}
function Hr(e, t, n) {
	var r = e, i = t;
	if (r.push && i.push) {
		var a = r.length, o = i.length;
		if (a !== o) {
			if (a > o) r.length = o;
			else for (var s = a; s < o; s++) r.push(n === 1 ? i[s] : Fr.call(i[s]));
		}
		for (var c = r[0] && r[0].length, s = 0; s < r.length; s++) if (n === 1) isNaN(r[s]) && (r[s] = i[s]);
		else for (var l = 0; l < c; l++) isNaN(r[s][l]) && (r[s][l] = i[s][l]);
	}
}
function Ur(e) {
	if (P(e)) {
		var t = e.length;
		if (P(e[0])) {
			for (var n = [], r = 0; r < t; r++) n.push(Fr.call(e[r]));
			return n;
		}
		return Fr.call(e);
	}
	return e;
}
function Wr(e) {
	return e[0] = Math.floor(e[0]) || 0, e[1] = Math.floor(e[1]) || 0, e[2] = Math.floor(e[2]) || 0, e[3] = e[3] == null ? 1 : e[3], "rgba(" + e.join(",") + ")";
}
function Gr(e) {
	return P(e && e[0]) ? 2 : 1;
}
var Kr = 0, qr = 1, Jr = 2, Yr = 3, Xr = 4, Zr = 5, Qr = 6;
function $r(e) {
	return e === Xr || e === Zr;
}
function ei(e) {
	return e === qr || e === Jr;
}
var ti = [
	0,
	0,
	0,
	0
], ni = function() {
	function e(e) {
		this.keyframes = [], this.discrete = !1, this._invalid = !1, this._needsSort = !1, this._lastFr = 0, this._lastFrP = 0, this.propName = e;
	}
	return e.prototype.isFinished = function() {
		return this._finished;
	}, e.prototype.setFinished = function() {
		this._finished = !0, this._additiveTrack && this._additiveTrack.setFinished();
	}, e.prototype.needsAnimate = function() {
		return this.keyframes.length >= 1;
	}, e.prototype.getAdditiveTrack = function() {
		return this._additiveTrack;
	}, e.prototype.addKeyframe = function(e, t, n) {
		this._needsSort = !0;
		var r = this.keyframes, i = r.length, a = !1, o = Qr, s = t;
		if (P(t)) {
			var c = Gr(t);
			o = c, (c === 1 && !U(t[0]) || c === 2 && !U(t[0][0])) && (a = !0);
		} else if (U(t) && !fe(t)) o = Kr;
		else if (H(t)) {
			if (!isNaN(+t)) o = Kr;
			else {
				var l = wr(t);
				l && (s = l, o = Yr);
			}
		} else if (ue(t)) {
			var u = j({}, s);
			u.colorStops = I(t.colorStops, function(e) {
				return {
					offset: e.offset,
					color: wr(e.color)
				};
			}), Nr(t) ? o = Xr : Pr(t) && (o = Zr), s = u;
		}
		i === 0 ? this.valType = o : (o !== this.valType || o === Qr) && (a = !0), this.discrete = this.discrete || a;
		var d = {
			time: e,
			value: s,
			rawValue: t,
			percent: 0
		};
		return n && (d.easing = n, d.easingFunc = V(n) ? n : Pn[n] || sr(n)), r.push(d), d;
	}, e.prototype.prepare = function(e, t) {
		var n = this.keyframes;
		this._needsSort && n.sort(function(e, t) {
			return e.time - t.time;
		});
		for (var r = this.valType, i = n.length, a = n[i - 1], o = this.discrete, s = ei(r), c = $r(r), l = 0; l < i; l++) {
			var u = n[l], d = u.value, f = a.value;
			u.percent = u.time / e, o || (s && l !== i - 1 ? Hr(d, f, r) : c && Vr(d.colorStops, f.colorStops));
		}
		if (!o && r !== Zr && t && this.needsAnimate() && t.needsAnimate() && r === t.valType && !t._finished) {
			this._additiveTrack = t;
			for (var p = n[0].value, l = 0; l < i; l++) r === Kr ? n[l].additiveValue = n[l].value - p : r === Yr ? n[l].additiveValue = zr([], n[l].value, p, -1) : ei(r) && (n[l].additiveValue = r === qr ? zr([], n[l].value, p, -1) : Br([], n[l].value, p, -1));
		}
	}, e.prototype.step = function(e, t) {
		if (!this._finished) {
			this._additiveTrack && this._additiveTrack._finished && (this._additiveTrack = null);
			var n = this._additiveTrack != null, r = n ? "additiveValue" : "value", i = this.valType, a = this.keyframes, o = a.length, s = this.propName, c = i === Yr, l, u = this._lastFr, d = Math.min, f, p;
			if (o === 1) f = p = a[0];
			else {
				if (t < 0) l = 0;
				else if (t < this._lastFrP) {
					for (l = d(u + 1, o - 1); l >= 0 && !(a[l].percent <= t); l--);
					l = d(l, o - 2);
				} else {
					for (l = u; l < o && !(a[l].percent > t); l++);
					l = d(l - 1, o - 2);
				}
				p = a[l + 1], f = a[l];
			}
			if (f && p) {
				this._lastFr = l, this._lastFrP = t;
				var m = p.percent - f.percent, h = m === 0 ? 1 : d((t - f.percent) / m, 1);
				p.easingFunc && (h = p.easingFunc(h));
				var g = n ? this._additiveValue : c ? ti : e[s];
				if ((ei(i) || c) && !g && (g = this._additiveValue = []), this.discrete) e[s] = h < 1 ? f.rawValue : p.rawValue;
				else if (ei(i)) i === qr ? Lr(g, f[r], p[r], h) : Rr(g, f[r], p[r], h);
				else if ($r(i)) {
					var _ = f[r], v = p[r], y = i === Xr;
					e[s] = {
						type: y ? "linear" : "radial",
						x: Ir(_.x, v.x, h),
						y: Ir(_.y, v.y, h),
						colorStops: I(_.colorStops, function(e, t) {
							var n = v.colorStops[t];
							return {
								offset: Ir(e.offset, n.offset, h),
								color: Wr(Lr([], e.color, n.color, h))
							};
						}),
						global: v.global
					}, y ? (e[s].x2 = Ir(_.x2, v.x2, h), e[s].y2 = Ir(_.y2, v.y2, h)) : e[s].r = Ir(_.r, v.r, h);
				} else if (c) Lr(g, f[r], p[r], h), n || (e[s] = Wr(g));
				else {
					var b = Ir(f[r], p[r], h);
					n ? this._additiveValue = b : e[s] = b;
				}
				n && this._addToTarget(e);
			}
		}
	}, e.prototype._addToTarget = function(e) {
		var t = this.valType, n = this.propName, r = this._additiveValue;
		t === Kr ? e[n] = e[n] + r : t === Yr ? (wr(e[n], ti), zr(ti, ti, r, 1), e[n] = Wr(ti)) : t === qr ? zr(e[n], e[n], r, 1) : t === Jr && Br(e[n], e[n], r, 1);
	}, e;
}(), ri = function() {
	function e(e, t, n, r) {
		this._tracks = {}, this._trackKeys = [], this._maxTime = 0, this._started = 0, this._clip = null, this._target = e, this._loop = t, t && r ? O("Can' use additive animation on looped animation.") : (this._additiveAnimators = r, this._allowDiscrete = n);
	}
	return e.prototype.getMaxTime = function() {
		return this._maxTime;
	}, e.prototype.getDelay = function() {
		return this._delay;
	}, e.prototype.getLoop = function() {
		return this._loop;
	}, e.prototype.getTarget = function() {
		return this._target;
	}, e.prototype.changeTarget = function(e) {
		this._target = e;
	}, e.prototype.when = function(e, t, n) {
		return this.whenWithKeys(e, t, L(t), n);
	}, e.prototype.whenWithKeys = function(e, t, n, r) {
		for (var i = this._tracks, a = 0; a < n.length; a++) {
			var o = n[a], s = i[o];
			if (!s) {
				s = i[o] = new ni(o);
				var c = void 0, l = this._getAdditiveTrack(o);
				if (l) {
					var u = l.keyframes, d = u[u.length - 1];
					c = d && d.value, l.valType === Yr && c && (c = Wr(c));
				} else c = this._target[o];
				if (c == null) continue;
				e > 0 && s.addKeyframe(0, Ur(c), r), this._trackKeys.push(o);
			}
			s.addKeyframe(e, Ur(t[o]), r);
		}
		return this._maxTime = Math.max(this._maxTime, e), this;
	}, e.prototype.pause = function() {
		this._clip.pause(), this._paused = !0;
	}, e.prototype.resume = function() {
		this._clip.resume(), this._paused = !1;
	}, e.prototype.isPaused = function() {
		return !!this._paused;
	}, e.prototype.duration = function(e) {
		return this._maxTime = e, this._force = !0, this;
	}, e.prototype._doneCallback = function() {
		this._setTracksFinished(), this._clip = null;
		var e = this._doneCbs;
		if (e) for (var t = e.length, n = 0; n < t; n++) e[n].call(this);
	}, e.prototype._abortedCallback = function() {
		this._setTracksFinished();
		var e = this.animation, t = this._abortedCbs;
		if (e && e.removeClip(this._clip), this._clip = null, t) for (var n = 0; n < t.length; n++) t[n].call(this);
	}, e.prototype._setTracksFinished = function() {
		for (var e = this._tracks, t = this._trackKeys, n = 0; n < t.length; n++) e[t[n]].setFinished();
	}, e.prototype._getAdditiveTrack = function(e) {
		var t, n = this._additiveAnimators;
		if (n) for (var r = 0; r < n.length; r++) {
			var i = n[r].getTrack(e);
			i && (t = i);
		}
		return t;
	}, e.prototype.start = function(e) {
		if (!(this._started > 0)) {
			this._started = 1;
			for (var t = this, n = [], r = this._maxTime || 0, i = 0; i < this._trackKeys.length; i++) {
				var a = this._trackKeys[i], o = this._tracks[a], s = this._getAdditiveTrack(a), c = o.keyframes, l = c.length;
				if (o.prepare(r, s), o.needsAnimate()) {
					if (!this._allowDiscrete && o.discrete) {
						var u = c[l - 1];
						u && (t._target[o.propName] = u.rawValue), o.setFinished();
					} else n.push(o);
				}
			}
			if (n.length || this._force) {
				var d = new cr({
					life: r,
					loop: this._loop,
					delay: this._delay || 0,
					onframe: function(e) {
						t._started = 2;
						var r = t._additiveAnimators;
						if (r) {
							for (var i = !1, a = 0; a < r.length; a++) if (r[a]._clip) {
								i = !0;
								break;
							}
							i || (t._additiveAnimators = null);
						}
						for (var a = 0; a < n.length; a++) n[a].step(t._target, e);
						var o = t._onframeCbs;
						if (o) for (var a = 0; a < o.length; a++) o[a](t._target, e);
					},
					ondestroy: function() {
						t._doneCallback();
					}
				});
				this._clip = d, this.animation && this.animation.addClip(d), e && d.setEasing(e);
			} else this._doneCallback();
			return this;
		}
	}, e.prototype.stop = function(e) {
		if (this._clip) {
			var t = this._clip;
			e && t.onframe(1), this._abortedCallback();
		}
	}, e.prototype.delay = function(e) {
		return this._delay = e, this;
	}, e.prototype.during = function(e) {
		return e && (this._onframeCbs ||= [], this._onframeCbs.push(e)), this;
	}, e.prototype.done = function(e) {
		return e && (this._doneCbs ||= [], this._doneCbs.push(e)), this;
	}, e.prototype.aborted = function(e) {
		return e && (this._abortedCbs ||= [], this._abortedCbs.push(e)), this;
	}, e.prototype.getClip = function() {
		return this._clip;
	}, e.prototype.getTrack = function(e) {
		return this._tracks[e];
	}, e.prototype.getTracks = function() {
		var e = this;
		return I(this._trackKeys, function(t) {
			return e._tracks[t];
		});
	}, e.prototype.stopTracks = function(e, t) {
		if (!e.length || !this._clip) return !0;
		for (var n = this._tracks, r = this._trackKeys, i = 0; i < e.length; i++) {
			var a = n[e[i]];
			a && !a.isFinished() && (t ? a.step(this._target, 1) : this._started === 1 && a.step(this._target, 0), a.setFinished());
		}
		for (var o = !0, i = 0; i < r.length; i++) if (!n[r[i]].isFinished()) {
			o = !1;
			break;
		}
		return o && this._abortedCallback(), o;
	}, e.prototype.saveTo = function(e, t, n) {
		if (e) {
			t ||= this._trackKeys;
			for (var r = 0; r < t.length; r++) {
				var i = t[r], a = this._tracks[i];
				if (a && !a.isFinished()) {
					var o = a.keyframes, s = o[n ? 0 : o.length - 1];
					s && (e[i] = Ur(s.rawValue));
				}
			}
		}
	}, e.prototype.__changeFinalValue = function(e, t) {
		t ||= L(e);
		for (var n = 0; n < t.length; n++) {
			var r = t[n], i = this._tracks[r];
			if (i) {
				var a = i.keyframes;
				if (a.length > 1) {
					var o = a.pop();
					i.addKeyframe(o.time, e[r]), i.prepare(this._maxTime, i.getAdditiveTrack());
				}
			}
		}
	}, e;
}();
//#endregion
//#region node_modules/zrender/lib/animation/Animation.js
function ii() {
	return (/* @__PURE__ */ new Date()).getTime();
}
var ai = function(e) {
	r(t, e);
	function t(t) {
		var n = e.call(this) || this;
		return n._running = !1, n._time = 0, n._pausedTime = 0, n._pauseStart = 0, n._paused = !1, t ||= {}, n.stage = t.stage || {}, n;
	}
	return t.prototype.addClip = function(e) {
		e.animation && this.removeClip(e), this._head ? (this._tail.next = e, e.prev = this._tail, e.next = null, this._tail = e) : this._head = this._tail = e, e.animation = this;
	}, t.prototype.addAnimator = function(e) {
		e.animation = this;
		var t = e.getClip();
		t && this.addClip(t);
	}, t.prototype.removeClip = function(e) {
		if (e.animation) {
			var t = e.prev, n = e.next;
			t ? t.next = n : this._head = n, n ? n.prev = t : this._tail = t, e.next = e.prev = e.animation = null;
		}
	}, t.prototype.removeAnimator = function(e) {
		var t = e.getClip();
		t && this.removeClip(t), e.animation = null;
	}, t.prototype.update = function(e) {
		for (var t = ii() - this._pausedTime, n = t - this._time, r = this._head; r;) {
			var i = r.next;
			r.step(t, n) && (r.ondestroy(), this.removeClip(r)), r = i;
		}
		this._time = t, e || (this.trigger("frame", n), this.stage.update && this.stage.update());
	}, t.prototype._startLoop = function() {
		var e = this;
		this._running = !0;
		function t() {
			e._running && (Nn(t), !e._paused && e.update());
		}
		Nn(t);
	}, t.prototype.start = function() {
		this._running || (this._time = ii(), this._pausedTime = 0, this._startLoop());
	}, t.prototype.stop = function() {
		this._running = !1;
	}, t.prototype.pause = function() {
		this._paused ||= (this._pauseStart = ii(), !0);
	}, t.prototype.resume = function() {
		this._paused &&= (this._pausedTime += ii() - this._pauseStart, !1);
	}, t.prototype.clear = function() {
		for (var e = this._head; e;) {
			var t = e.next;
			e.prev = e.next = e.animation = null, e = t;
		}
		this._head = this._tail = null;
	}, t.prototype.isFinished = function() {
		return this._head == null;
	}, t.prototype.animate = function(e, t) {
		t ||= {}, this.start();
		var n = new ri(e, t.loop);
		return this.addAnimator(n), n;
	}, t;
}(Qe), oi = 300, si = a.domSupported, ci = (function() {
	var e = [
		"click",
		"dblclick",
		"mousewheel",
		"wheel",
		"mouseout",
		"mouseup",
		"mousedown",
		"mousemove",
		"contextmenu"
	], t = [
		"touchstart",
		"touchend",
		"touchmove"
	], n = {
		pointerdown: 1,
		pointerup: 1,
		pointermove: 1,
		pointerout: 1
	};
	return {
		mouse: e,
		touch: t,
		pointer: I(e, function(e) {
			var t = e.replace("mouse", "pointer");
			return n.hasOwnProperty(t) ? t : e;
		})
	};
})(), li = {
	mouse: ["mousemove", "mouseup"],
	pointer: ["pointermove", "pointerup"]
}, ui = !1;
function di(e) {
	var t = e.pointerType;
	return t === "pen" || t === "touch";
}
function fi(e) {
	e.touching = !0, e.touchTimer != null && (clearTimeout(e.touchTimer), e.touchTimer = null), e.touchTimer = setTimeout(function() {
		e.touching = !1, e.touchTimer = null;
	}, 700);
}
function pi(e) {
	e && (e.zrByTouch = !0);
}
function mi(e, t) {
	return yt(e.dom, new gi(e, t), !0);
}
function hi(e, t) {
	for (var n = t, r = !1; n && n.nodeType !== 9 && !(r = n.domBelongToZr || n !== t && n === e.painterRoot);) n = n.parentNode;
	return r;
}
var gi = function() {
	function e(e, t) {
		this.stopPropagation = Ae, this.stopImmediatePropagation = Ae, this.preventDefault = Ae, this.type = t.type, this.target = this.currentTarget = e.dom, this.pointerType = t.pointerType, this.clientX = t.clientX, this.clientY = t.clientY;
	}
	return e;
}(), _i = {
	mousedown: function(e) {
		e = yt(this.dom, e), this.__mayPointerCapture = [e.zrX, e.zrY], this.trigger("mousedown", e);
	},
	mousemove: function(e) {
		e = yt(this.dom, e);
		var t = this.__mayPointerCapture;
		t && (e.zrX !== t[0] || e.zrY !== t[1]) && this.__togglePointerCapture(!0), this.trigger("mousemove", e);
	},
	mouseup: function(e) {
		e = yt(this.dom, e), this.__togglePointerCapture(!1), this.trigger("mouseup", e);
	},
	mouseout: function(e) {
		e = yt(this.dom, e);
		var t = e.toElement || e.relatedTarget;
		hi(this, t) || (this.__pointerCapturing && (e.zrEventControl = "no_globalout"), this.trigger("mouseout", e));
	},
	wheel: function(e) {
		ui = !0, e = yt(this.dom, e), this.trigger("mousewheel", e);
	},
	mousewheel: function(e) {
		ui || (e = yt(this.dom, e), this.trigger("mousewheel", e));
	},
	touchstart: function(e) {
		e = yt(this.dom, e), pi(e), this.__lastTouchMoment = /* @__PURE__ */ new Date(), this.handler.processGesture(e, "start"), _i.mousemove.call(this, e), _i.mousedown.call(this, e);
	},
	touchmove: function(e) {
		e = yt(this.dom, e), pi(e), this.handler.processGesture(e, "change"), _i.mousemove.call(this, e);
	},
	touchend: function(e) {
		e = yt(this.dom, e), pi(e), this.handler.processGesture(e, "end"), _i.mouseup.call(this, e), +/* @__PURE__ */ new Date() - this.__lastTouchMoment < oi && _i.click.call(this, e);
	},
	pointerdown: function(e) {
		_i.mousedown.call(this, e);
	},
	pointermove: function(e) {
		di(e) || _i.mousemove.call(this, e);
	},
	pointerup: function(e) {
		_i.mouseup.call(this, e);
	},
	pointerout: function(e) {
		di(e) || _i.mouseout.call(this, e);
	}
};
F([
	"click",
	"dblclick",
	"contextmenu"
], function(e) {
	_i[e] = function(t) {
		t = yt(this.dom, t), this.trigger(e, t);
	};
});
var vi = {
	pointermove: function(e) {
		di(e) || vi.mousemove.call(this, e);
	},
	pointerup: function(e) {
		vi.mouseup.call(this, e);
	},
	mousemove: function(e) {
		this.trigger("mousemove", e);
	},
	mouseup: function(e) {
		var t = this.__pointerCapturing;
		this.__togglePointerCapture(!1), this.trigger("mouseup", e), t && (e.zrEventControl = "only_globalout", this.trigger("mouseout", e));
	}
};
function yi(e, t) {
	var n = t.domHandlers;
	a.pointerEventsSupported ? F(ci.pointer, function(r) {
		xi(t, r, function(t) {
			n[r].call(e, t);
		});
	}) : (a.touchEventsSupported && F(ci.touch, function(r) {
		xi(t, r, function(i) {
			n[r].call(e, i), fi(t);
		});
	}), F(ci.mouse, function(r) {
		xi(t, r, function(i) {
			i = vt(i), t.touching || n[r].call(e, i);
		});
	}));
}
function bi(e, t) {
	a.pointerEventsSupported ? F(li.pointer, n) : a.touchEventsSupported || F(li.mouse, n);
	function n(n) {
		function r(r) {
			r = vt(r), hi(e, r.target) || (r = mi(e, r), t.domHandlers[n].call(e, r));
		}
		xi(t, n, r, { capture: !0 });
	}
}
function xi(e, t, n, r) {
	e.mounted[t] = n, e.listenerOpts[t] = r, xt(e.domTarget, t, n, r);
}
function Si(e) {
	var t = e.mounted;
	for (var n in t) t.hasOwnProperty(n) && St(e.domTarget, n, t[n], e.listenerOpts[n]);
	e.mounted = {};
}
var Ci = function() {
	function e(e, t) {
		this.mounted = {}, this.listenerOpts = {}, this.touching = !1, this.domTarget = e, this.domHandlers = t;
	}
	return e;
}(), wi = function(e) {
	r(t, e);
	function t(t, n) {
		var r = e.call(this) || this;
		return r.__pointerCapturing = !1, r.dom = t, r.painterRoot = n, r._localHandlerScope = new Ci(t, _i), si && (r._globalHandlerScope = new Ci(document, vi)), yi(r, r._localHandlerScope), r;
	}
	return t.prototype.dispose = function() {
		Si(this._localHandlerScope), si && Si(this._globalHandlerScope);
	}, t.prototype.setCursor = function(e) {
		this.dom.style && (this.dom.style.cursor = e || "default");
	}, t.prototype.__togglePointerCapture = function(e) {
		if (this.__mayPointerCapture = null, si && +this.__pointerCapturing ^ e) {
			this.__pointerCapturing = e;
			var t = this._globalHandlerScope;
			e ? bi(this, t) : Si(t);
		}
	}, t;
}(Qe), Ti = 1;
a.hasGlobalWindow && (Ti = Math.max(window.devicePixelRatio || window.screen && window.screen.deviceXDPI / window.screen.logicalXDPI || 1, 1));
var Ei = Ti, Di = .4, Oi = "#333", ki = "#ccc", Ai = "#eee", ji = At, Mi = 5e-5;
function Ni(e) {
	return e > Mi || e < -Mi;
}
var Pi = [], Fi = [], Ii = kt(), Li = Math.abs, Ri = function() {
	function e() {}
	return e.prototype.getLocalTransform = function(e) {
		return zi(this, e);
	}, e.prototype.setPosition = function(e) {
		this.x = e[0], this.y = e[1];
	}, e.prototype.setScale = function(e) {
		this.scaleX = e[0], this.scaleY = e[1];
	}, e.prototype.setSkew = function(e) {
		this.skewX = e[0], this.skewY = e[1];
	}, e.prototype.setOrigin = function(e) {
		this.originX = e[0], this.originY = e[1];
	}, e.prototype.needLocalTransform = function() {
		return Ni(this.rotation) || Ni(this.x) || Ni(this.y) || Ni(this.scaleX - 1) || Ni(this.scaleY - 1) || Ni(this.skewX) || Ni(this.skewY);
	}, e.prototype.updateTransform = function() {
		var e = this.parent && this.parent.transform, t = this.needLocalTransform(), n = this.transform;
		t || e ? (n ||= kt(), t ? this.getLocalTransform(n) : ji(n), e && (t ? Mt(n, e, n) : jt(n, e)), this.transform = n, this._resolveGlobalScaleRatio(n), this.invTransform = this.invTransform || kt(), It(this.invTransform, n)) : n && (ji(n), this.invTransform = null);
	}, e.prototype._resolveGlobalScaleRatio = function(e) {
		var t = this.globalScaleRatio;
		if (t != null && t !== 1) {
			this.getGlobalScale(Pi);
			var n = Pi[0] < 0 ? -1 : 1, r = Pi[1] < 0 ? -1 : 1, i = ((Pi[0] - n) * t + n) / Pi[0] || 0, a = ((Pi[1] - r) * t + r) / Pi[1] || 0;
			e[0] *= i, e[1] *= i, e[2] *= a, e[3] *= a;
		}
	}, e.prototype.getComputedTransform = function() {
		for (var e = this, t = []; e;) t.push(e), e = e.parent;
		for (; e = t.pop();) e.updateTransform();
		return this.transform;
	}, e.prototype.setLocalTransform = function(e) {
		if (e) {
			var t = e[0] * e[0] + e[1] * e[1], n = e[2] * e[2] + e[3] * e[3], r = Math.atan2(e[1], e[0]), i = Math.PI / 2 + r - Math.atan2(e[3], e[2]);
			n = Math.sqrt(n) * Math.cos(i), t = Math.sqrt(t), this.skewX = i, this.skewY = 0, this.rotation = -r, this.x = +e[4], this.y = +e[5], this.scaleX = t, this.scaleY = n, this.originX = 0, this.originY = 0;
		}
	}, e.prototype.decomposeTransform = function() {
		if (this.transform) {
			var e = this.parent, t = this.transform;
			e && e.transform && (e.invTransform = e.invTransform || kt(), Mt(Fi, e.invTransform, t), t = Fi);
			var n = this.originX, r = this.originY;
			(n || r) && (Ii[4] = n, Ii[5] = r, Mt(Fi, t, Ii), Fi[4] -= n, Fi[5] -= r, t = Fi), this.setLocalTransform(t);
		}
	}, e.prototype.getGlobalScale = function(e) {
		var t = this.transform;
		return e ||= [], t ? (e[0] = Math.sqrt(t[0] * t[0] + t[1] * t[1]), e[1] = Math.sqrt(t[2] * t[2] + t[3] * t[3]), t[0] < 0 && (e[0] = -e[0]), t[3] < 0 && (e[1] = -e[1]), e) : (e[0] = 1, e[1] = 1, e);
	}, e.prototype.transformCoordToLocal = function(e, t) {
		var n = [e, t], r = this.invTransform;
		return r && qe(n, n, r), n;
	}, e.prototype.transformCoordToGlobal = function(e, t) {
		var n = [e, t], r = this.transform;
		return r && qe(n, n, r), n;
	}, e.prototype.getLineScale = function() {
		var e = this.transform;
		return e && Li(e[0] - 1) > 1e-10 && Li(e[3] - 1) > 1e-10 ? Math.sqrt(Li(e[0] * e[3] - e[2] * e[1])) : 1;
	}, e.prototype.copyTransform = function(e) {
		Hi(this, e);
	}, e.getLocalTransform = function(e, t) {
		t ||= [];
		var n = e.originX || 0, r = e.originY || 0, i = e.scaleX, a = e.scaleY, o = e.anchorX, s = e.anchorY, c = e.rotation || 0, l = e.x, u = e.y, d = e.skewX ? Math.tan(e.skewX) : 0, f = e.skewY ? Math.tan(-e.skewY) : 0;
		if (n || r || o || s) {
			var p = n + o, m = r + s;
			t[4] = -p * i - d * m * a, t[5] = -m * a - f * p * i;
		} else t[4] = t[5] = 0;
		return t[0] = i, t[3] = a, t[1] = f * i, t[2] = d * a, c && Pt(t, t, c), t[4] += n + l, t[5] += r + u, t;
	}, e.initDefaultProps = (function() {
		var t = e.prototype;
		t.scaleX = t.scaleY = t.globalScaleRatio = 1, t.x = t.y = t.originX = t.originY = t.skewX = t.skewY = t.rotation = t.anchorX = t.anchorY = 0;
	})(), e;
}(), zi = Ri.getLocalTransform;
function Bi() {
	return new Ri();
}
var Vi = [
	"x",
	"y",
	"originX",
	"originY",
	"anchorX",
	"anchorY",
	"rotation",
	"scaleX",
	"scaleY",
	"skewX",
	"skewY"
];
function Hi(e, t) {
	return ee(e, t, Vi);
}
//#endregion
//#region node_modules/zrender/lib/contain/text.js
function Ui(e) {
	Wi ||= new dr(100), e ||= "12px sans-serif";
	var t = Wi.get(e);
	return t || (t = {
		font: e,
		strWidthCache: new dr(500),
		asciiWidthMap: null,
		asciiWidthMapTried: !1,
		stWideCharWidth: p.measureText("国", e).width,
		asciiCharWidth: p.measureText("a", e).width
	}, Wi.put(e, t)), t;
}
var Wi;
function Gi(e) {
	if (!(Ki >= qi)) {
		e ||= "12px sans-serif";
		for (var t = [], n = +/* @__PURE__ */ new Date(), r = 0; r <= 127; r++) t[r] = p.measureText(String.fromCharCode(r), e).width;
		var i = +/* @__PURE__ */ new Date() - n;
		return i > 16 ? Ki = qi : i > 2 && Ki++, t;
	}
}
var Ki = 0, qi = 5;
function Ji(e, t) {
	return e.asciiWidthMapTried ||= (e.asciiWidthMap = Gi(e.font), !0), 0 <= t && t <= 127 ? e.asciiWidthMap == null ? e.asciiCharWidth : e.asciiWidthMap[t] : e.stWideCharWidth;
}
function Yi(e, t) {
	var n = e.strWidthCache, r = n.get(t);
	return r ?? (r = p.measureText(t, e.font).width, n.put(t, r)), r;
}
function Xi(e, t, n, r) {
	var i = Yi(Ui(t), e), a = ea(t);
	return new J(Qi(0, i, n), $i(0, a, r), i, a);
}
function Zi(e, t, n, r) {
	var i = ((e || "") + "").split("\n");
	if (i.length === 1) return Xi(i[0], t, n, r);
	for (var a = new J(0, 0, 0, 0), o = 0; o < i.length; o++) {
		var s = Xi(i[o], t, n, r);
		o === 0 ? a.copy(s) : a.union(s);
	}
	return a;
}
function Qi(e, t, n, r) {
	return n === "right" ? r ? e += t : e -= t : n === "center" && (r ? e += t / 2 : e -= t / 2), e;
}
function $i(e, t, n, r) {
	return n === "middle" ? r ? e += t / 2 : e -= t / 2 : n === "bottom" && (r ? e += t : e -= t), e;
}
function ea(e) {
	return Ui(e).stWideCharWidth;
}
function ta(e, t) {
	return typeof e == "string" ? e.lastIndexOf("%") >= 0 ? parseFloat(e) / 100 * t : parseFloat(e) : e;
}
function na(e, t, n) {
	var r = t.position || "inside", i = t.distance == null ? 5 : t.distance, a = n.height, o = n.width, s = a / 2, c = n.x, l = n.y, u = "left", d = "top";
	if (r instanceof Array) c += ta(r[0], n.width), l += ta(r[1], n.height), u = null, d = null;
	else switch (r) {
		case "left":
			c -= i, l += s, u = "right", d = "middle";
			break;
		case "right":
			c += i + o, l += s, d = "middle";
			break;
		case "top":
			c += o / 2, l -= i, u = "center", d = "bottom";
			break;
		case "bottom":
			c += o / 2, l += a + i, u = "center";
			break;
		case "inside":
			c += o / 2, l += s, u = "center", d = "middle";
			break;
		case "insideLeft":
			c += i, l += s, d = "middle";
			break;
		case "insideRight":
			c += o - i, l += s, u = "right", d = "middle";
			break;
		case "insideTop":
			c += o / 2, l += i, u = "center";
			break;
		case "insideBottom":
			c += o / 2, l += a - i, u = "center", d = "bottom";
			break;
		case "insideTopLeft":
			c += i, l += i;
			break;
		case "insideTopRight":
			c += o - i, l += i, u = "right";
			break;
		case "insideBottomLeft":
			c += i, l += a - i, d = "bottom";
			break;
		case "insideBottomRight": c += o - i, l += a - i, u = "right", d = "bottom";
	}
	return e ||= {}, e.x = c, e.y = l, e.align = u, e.verticalAlign = d, e;
}
//#endregion
//#region node_modules/zrender/lib/Element.js
var ra = "__zr_normal__", ia = Vi.concat(["ignore"]), aa = re(Vi, function(e, t) {
	return e[t] = !0, e;
}, { ignore: !1 }), oa = {}, sa = new J(0, 0, 0, 0), ca = [], la = function() {
	function e(e) {
		this.id = D(), this.animators = [], this.currentStates = [], this.states = {}, this._init(e);
	}
	return e.prototype._init = function(e) {
		this.attr(e);
	}, e.prototype.drift = function(e, t, n) {
		switch (this.draggable) {
			case "horizontal":
				t = 0;
				break;
			case "vertical": e = 0;
		}
		var r = this.transform;
		r ||= this.transform = [
			1,
			0,
			0,
			1,
			0,
			0
		], r[4] += e, r[5] += t, this.decomposeTransform(), this.markRedraw();
	}, e.prototype.beforeUpdate = function() {}, e.prototype.afterUpdate = function() {}, e.prototype.update = function() {
		this.updateTransform(), this.__dirty && this.updateInnerText();
	}, e.prototype.updateInnerText = function(e) {
		var t = this._textContent;
		if (t && (!t.ignore || e)) {
			this.textConfig ||= {};
			var n = this.textConfig, r = n.local, i = t.innerTransformable, a = void 0, o = void 0, s = !1;
			i.parent = r ? this : null;
			var c = !1;
			i.copyTransform(t);
			var l = n.position != null, u = n.autoOverflowArea, d = void 0;
			if ((u || l) && (d = sa, n.layoutRect ? d.copy(n.layoutRect) : d.copy(this.getBoundingRect()), r || d.applyTransform(this.transform)), l) {
				this.calculateTextPosition ? this.calculateTextPosition(oa, n, d) : na(oa, n, d), i.x = oa.x, i.y = oa.y, a = oa.align, o = oa.verticalAlign;
				var f = n.origin;
				if (f && n.rotation != null) {
					var p = void 0, m = void 0;
					f === "center" ? (p = d.width * .5, m = d.height * .5) : (p = ta(f[0], d.width), m = ta(f[1], d.height)), c = !0, i.originX = -i.x + p + (r ? 0 : d.x), i.originY = -i.y + m + (r ? 0 : d.y);
				}
			}
			n.rotation != null && (i.rotation = n.rotation);
			var h = n.offset;
			h && (i.x += h[0], i.y += h[1], c || (i.originX = -h[0], i.originY = -h[1]));
			var g = this._innerTextDefaultStyle ||= {};
			if (u) {
				var _ = g.overflowRect = g.overflowRect || new J(0, 0, 0, 0);
				i.getLocalTransform(ca), It(ca, ca), J.copy(_, d), _.applyTransform(ca);
			} else g.overflowRect = null;
			var v = n.inside == null ? typeof n.position == "string" && n.position.indexOf("inside") >= 0 : n.inside, y = void 0, b = void 0, x = void 0;
			v && this.canBeInsideText() ? (y = n.insideFill, b = n.insideStroke, (y == null || y === "auto") && (y = this.getInsideTextFill()), (b == null || b === "auto") && (b = this.getInsideTextStroke(y), x = !0)) : (y = n.outsideFill, b = n.outsideStroke, (y == null || y === "auto") && (y = this.getOutsideFill()), (b == null || b === "auto") && (b = this.getOutsideStroke(y), x = !0)), y ||= "#000", (y !== g.fill || b !== g.stroke || x !== g.autoStroke || a !== g.align || o !== g.verticalAlign) && (s = !0, g.fill = y, g.stroke = b, g.autoStroke = x, g.align = a, g.verticalAlign = o, t.setDefaultTextStyle(g)), t.__dirty |= 1, s && t.dirtyStyle(!0);
		}
	}, e.prototype.canBeInsideText = function() {
		return !0;
	}, e.prototype.getInsideTextFill = function() {
		return "#fff";
	}, e.prototype.getInsideTextStroke = function(e) {
		return "#000";
	}, e.prototype.getOutsideFill = function() {
		return this.__zr && this.__zr.isDarkMode() ? ki : Oi;
	}, e.prototype.getOutsideStroke = function(e) {
		var t = this.__zr && this.__zr.getBackgroundColor(), n = typeof t == "string" && wr(t);
		n ||= [
			255,
			255,
			255,
			1
		];
		for (var r = n[3], i = this.__zr.isDarkMode(), a = 0; a < 3; a++) n[a] = n[a] * r + (i ? 0 : 255) * (1 - r);
		return n[3] = 1, kr(n, "rgba");
	}, e.prototype.traverse = function(e, t) {}, e.prototype.attrKV = function(e, t) {
		e === "textConfig" ? this.setTextConfig(t) : e === "textContent" ? this.setTextContent(t) : e === "clipPath" ? this.setClipPath(t) : e === "extra" ? (this.extra = this.extra || {}, j(this.extra, t)) : this[e] = t;
	}, e.prototype.hide = function() {
		this.ignore = !0, this.markRedraw();
	}, e.prototype.show = function() {
		this.ignore = !1, this.markRedraw();
	}, e.prototype.attr = function(e, t) {
		if (typeof e == "string") this.attrKV(e, t);
		else if (W(e)) for (var n = L(e), r = 0; r < n.length; r++) {
			var i = n[r];
			this.attrKV(i, e[i]);
		}
		return this.markRedraw(), this;
	}, e.prototype.saveCurrentToNormalState = function(e) {
		this._innerSaveToNormal(e);
		for (var t = this._normalState, n = 0; n < this.animators.length; n++) {
			var r = this.animators[n], i = r.__fromStateTransition;
			if (!(r.getLoop() || i && i !== "__zr_normal__")) {
				var a = r.targetName, o = a ? t[a] : t;
				r.saveTo(o);
			}
		}
	}, e.prototype._innerSaveToNormal = function(e) {
		var t = this._normalState;
		t ||= this._normalState = {}, e.textConfig && !t.textConfig && (t.textConfig = this.textConfig), this._savePrimaryToNormal(e, t, ia);
	}, e.prototype._savePrimaryToNormal = function(e, t, n) {
		for (var r = 0; r < n.length; r++) {
			var i = n[r];
			e[i] != null && !(i in t) && (t[i] = this[i]);
		}
	}, e.prototype.hasState = function() {
		return this.currentStates.length > 0;
	}, e.prototype.getState = function(e) {
		return this.states[e];
	}, e.prototype.ensureState = function(e) {
		var t = this.states;
		return t[e] || (t[e] = {}), t[e];
	}, e.prototype.clearStates = function(e) {
		this.useState(ra, !1, e);
	}, e.prototype.useState = function(e, t, n, r) {
		var i = e === ra;
		if (this.hasState() || !i) {
			var a = this.currentStates, o = this.stateTransition;
			if (!(N(a, e) >= 0 && (t || a.length === 1))) {
				var s;
				if (this.stateProxy && !i && (s = this.stateProxy(e)), s ||= this.states && this.states[e], !s && !i) O("State " + e + " not exists.");
				else {
					i || this.saveCurrentToNormalState(s);
					var c = this._textContent, l = _a(this, c, s, r);
					l && !this.__inHover && (this.__inHover = l), this._applyStateObj(e, s, this._normalState, t, ya(this, n, o), o);
					var u = this._textGuide;
					return c && c.useState(e, t, n, !!l), u && u.useState(e, t, n, !!l), i ? (this.currentStates = [], this._normalState = {}) : t ? this.currentStates.push(e) : this.currentStates = [e], this._updateAnimationTargets(), this.markRedraw(), !l && this.__inHover && (this.__inHover = 0, this.__dirty &= -2), s;
				}
			}
		}
	}, e.prototype.useStates = function(e, t, n) {
		if (!e.length) this.clearStates();
		else {
			var r = [], i = this.currentStates, a = e.length, o = a === i.length;
			if (o) {
				for (var s = 0; s < a; s++) if (e[s] !== i[s]) {
					o = !1;
					break;
				}
			}
			if (o) return;
			for (var s = 0; s < a; s++) {
				var c = e[s], l = void 0;
				this.stateProxy && (l = this.stateProxy(c, e)), l ||= this.states[c], l && r.push(l);
			}
			var u = r[a - 1], d = this._textContent, f = _a(this, d, u, n);
			f && !this.__inHover && (this.__inHover = f);
			var p = this._mergeStates(r), m = this.stateTransition;
			this.saveCurrentToNormalState(p), this._applyStateObj(e.join(","), p, this._normalState, !1, ya(this, t, m), m);
			var h = this._textGuide;
			d && d.useStates(e, t, !!f), h && h.useStates(e, t, !!f), this._updateAnimationTargets(), this.currentStates = e.slice(), this.markRedraw(), !f && this.__inHover && (this.__inHover = 0, this.__dirty &= -2);
		}
	}, e.prototype.isSilent = function() {
		for (var e = this; e;) {
			if (e.silent) return !0;
			var t = e.__hostTarget;
			e = t ? e.ignoreHostSilent ? null : t : e.parent;
		}
		return !1;
	}, e.prototype._updateAnimationTargets = function() {
		for (var e = 0; e < this.animators.length; e++) {
			var t = this.animators[e];
			t.targetName && t.changeTarget(this[t.targetName]);
		}
	}, e.prototype.removeState = function(e) {
		var t = N(this.currentStates, e);
		if (t >= 0) {
			var n = this.currentStates.slice();
			n.splice(t, 1), this.useStates(n);
		}
	}, e.prototype.replaceState = function(e, t, n) {
		var r = this.currentStates.slice(), i = N(r, e), a = N(r, t) >= 0;
		i >= 0 ? a ? r.splice(i, 1) : r[i] = t : n && !a && r.push(t), this.useStates(r);
	}, e.prototype.toggleState = function(e, t) {
		t ? this.useState(e, !0) : this.removeState(e);
	}, e.prototype._mergeStates = function(e) {
		for (var t = {}, n, r = 0; r < e.length; r++) {
			var i = e[r];
			j(t, i), i.textConfig && (n ||= {}, j(n, i.textConfig));
		}
		return n && (t.textConfig = n), t;
	}, e.prototype._applyStateObj = function(e, t, n, r, i, a) {
		if (this.__inHover !== 1) {
			var o = !(t && r);
			t && t.textConfig ? (this.textConfig = j({}, r ? this.textConfig : n.textConfig), j(this.textConfig, t.textConfig)) : o && n.textConfig && (this.textConfig = n.textConfig);
			for (var s = {}, c = !1, l = 0; l < ia.length; l++) {
				var u = ia[l], d = i && aa[u];
				t && t[u] != null ? d ? (c = !0, s[u] = t[u]) : this[u] = t[u] : o && n[u] != null && (d ? (c = !0, s[u] = n[u]) : this[u] = n[u]);
			}
			if (!i) for (var l = 0; l < this.animators.length; l++) {
				var f = this.animators[l], p = f.targetName;
				f.getLoop() || f.__changeFinalValue(p ? (t || n)[p] : t || n);
			}
			c && this._transitionState(e, s, a);
		}
	}, e.prototype._attachComponent = function(e) {
		if ((!e.__zr || e.__hostTarget) && e !== this) {
			var t = this.__zr;
			t && e.addSelfToZr(t), e.__zr = t, e.__hostTarget = this;
		}
	}, e.prototype._detachComponent = function(e) {
		e.__zr && e.removeSelfFromZr(e.__zr), e.__zr = null, e.__hostTarget = null;
	}, e.prototype.getClipPath = function() {
		return this._clipPath;
	}, e.prototype.setClipPath = function(e) {
		this._clipPath && this._clipPath !== e && this.removeClipPath(), this._attachComponent(e), this._clipPath = e, this.markRedraw();
	}, e.prototype.removeClipPath = function() {
		var e = this._clipPath;
		e && (this._detachComponent(e), this._clipPath = null, this.markRedraw());
	}, e.prototype.getTextContent = function() {
		return this._textContent;
	}, e.prototype.setTextContent = function(e) {
		var t = this._textContent;
		t !== e && (t && t !== e && this.removeTextContent(), e.innerTransformable = new Ri(), this._attachComponent(e), this._textContent = e, this.markRedraw());
	}, e.prototype.setTextConfig = function(e) {
		this.textConfig ||= {}, j(this.textConfig, e), this.markRedraw();
	}, e.prototype.removeTextConfig = function() {
		this.textConfig = null, this.markRedraw();
	}, e.prototype.removeTextContent = function() {
		var e = this._textContent;
		e && (e.innerTransformable = null, this._detachComponent(e), this._textContent = null, this._innerTextDefaultStyle = null, this.markRedraw());
	}, e.prototype.getTextGuideLine = function() {
		return this._textGuide;
	}, e.prototype.setTextGuideLine = function(e) {
		this._textGuide && this._textGuide !== e && this.removeTextGuideLine(), this._attachComponent(e), this._textGuide = e, this.markRedraw();
	}, e.prototype.removeTextGuideLine = function() {
		var e = this._textGuide;
		e && (this._detachComponent(e), this._textGuide = null, this.markRedraw());
	}, e.prototype.markRedraw = function() {
		this.__dirty |= 1;
		var e = this.__zr;
		e && (this.__inHover ? e.refreshHover() : e.refresh()), this.__hostTarget && this.__hostTarget.markRedraw();
	}, e.prototype.dirty = function() {
		this.markRedraw();
	}, e.prototype.addSelfToZr = function(e) {
		if (this.__zr !== e) {
			this.__zr = e;
			var t = this.animators;
			if (t) for (var n = 0; n < t.length; n++) e.animation.addAnimator(t[n]);
			this._clipPath && this._clipPath.addSelfToZr(e), this._textContent && this._textContent.addSelfToZr(e), this._textGuide && this._textGuide.addSelfToZr(e);
		}
	}, e.prototype.removeSelfFromZr = function(e) {
		if (this.__zr) {
			this.__zr = null;
			var t = this.animators;
			if (t) for (var n = 0; n < t.length; n++) e.animation.removeAnimator(t[n]);
			this._clipPath && this._clipPath.removeSelfFromZr(e), this._textContent && this._textContent.removeSelfFromZr(e), this._textGuide && this._textGuide.removeSelfFromZr(e);
		}
	}, e.prototype.animate = function(e, t, n) {
		var r = new ri(e ? this[e] : this, t, n);
		return e && (r.targetName = e), this.addAnimator(r, e), r;
	}, e.prototype.addAnimator = function(e, t) {
		var n = this.__zr, r = this;
		e.during(function() {
			r.updateDuringAnimation(t);
		}).done(function() {
			var t = r.animators, n = N(t, e);
			n >= 0 && t.splice(n, 1);
		}), this.animators.push(e), n && n.animation.addAnimator(e), n && n.wakeUp();
	}, e.prototype.updateDuringAnimation = function(e) {
		this.markRedraw();
	}, e.prototype.stopAnimation = function(e, t) {
		for (var n = this.animators, r = n.length, i = [], a = 0; a < r; a++) {
			var o = n[a];
			!e || e === o.scope ? o.stop(t) : i.push(o);
		}
		return this.animators = i, this;
	}, e.prototype.animateTo = function(e, t, n) {
		ua(this, e, t, n);
	}, e.prototype.animateFrom = function(e, t, n) {
		ua(this, e, t, n, !0);
	}, e.prototype._transitionState = function(e, t, n, r) {
		for (var i = ua(this, t, n, r), a = 0; a < i.length; a++) i[a].__fromStateTransition = e;
	}, e.prototype.getBoundingRect = function() {
		return null;
	}, e.prototype.getPaintRect = function() {
		return null;
	}, e.initDefaultProps = (function() {
		var t = e.prototype;
		t.type = "element", t.name = "", t.ignore = t.silent = t.ignoreHostSilent = t.isGroup = t.draggable = t.dragging = t.ignoreClip = !1, t.__inHover = 0, t.__dirty = 1;
		function n(e, n, r, i) {
			Object.defineProperty(t, e, {
				get: function() {
					if (!this[n]) {
						var e = this[n] = [];
						a(this, e);
					}
					return this[n];
				},
				set: function(e) {
					this[r] = e[0], this[i] = e[1], this[n] = e, a(this, e);
				}
			});
			function a(e, t) {
				Object.defineProperty(t, 0, {
					get: function() {
						return e[r];
					},
					set: function(t) {
						e[r] = t;
					}
				}), Object.defineProperty(t, 1, {
					get: function() {
						return e[i];
					},
					set: function(t) {
						e[i] = t;
					}
				});
			}
		}
		Object.defineProperty && (n("position", "_legacyPos", "x", "y"), n("scale", "_legacyScale", "scaleX", "scaleY"), n("origin", "_legacyOrigin", "originX", "originY"));
	})(), e;
}();
ne(la, Qe), ne(la, Ri);
function ua(e, t, n, r, i) {
	n ||= {};
	var a = [];
	ga(e, "", e, t, n, r, a, i);
	var o = a.length, s = !1, c = n.done, l = n.aborted, u = function() {
		s = !0, o--, o <= 0 && (s ? c && c() : l && l());
	}, d = function() {
		o--, o <= 0 && (s ? c && c() : l && l());
	};
	o || c && c(), a.length > 0 && n.during && a[0].during(function(e, t) {
		n.during(t);
	});
	for (var f = 0; f < a.length; f++) {
		var p = a[f];
		u && p.done(u), d && p.aborted(d), n.force && p.duration(n.duration), p.start(n.easing);
	}
	return a;
}
function da(e, t, n) {
	for (var r = 0; r < n; r++) e[r] = t[r];
}
function fa(e) {
	return P(e[0]);
}
function pa(e, t, n) {
	if (P(t[n])) {
		if (P(e[n]) || (e[n] = []), ce(t[n])) {
			var r = t[n].length;
			e[n].length !== r && (e[n] = new t[n].constructor(r), da(e[n], t[n], r));
		} else {
			var i = t[n], a = e[n], o = i.length;
			if (fa(i)) for (var s = i[0].length, c = 0; c < o; c++) a[c] ? da(a[c], i[c], s) : a[c] = Array.prototype.slice.call(i[c]);
			else da(a, i, o);
			a.length = i.length;
		}
	} else e[n] = t[n];
}
function ma(e, t) {
	return e === t || P(e) && P(t) && ha(e, t);
}
function ha(e, t) {
	var n = e.length;
	if (n !== t.length) return !1;
	for (var r = 0; r < n; r++) if (e[r] !== t[r]) return !1;
	return !0;
}
function ga(e, t, n, r, i, a, o, s) {
	for (var c = L(r), l = i.duration, u = i.delay, d = i.additive, f = i.setToFinal, p = !W(a), m = e.animators, h = [], g = 0; g < c.length; g++) {
		var _ = c[g], v = r[_];
		if (v != null && n[_] != null && (p || a[_])) {
			if (W(v) && !P(v) && !ue(v)) {
				if (t) {
					s || (n[_] = v, e.updateDuringAnimation(t));
					continue;
				}
				ga(e, _, n[_], v, i, a && a[_], o, s);
			} else h.push(_);
		} else s || (n[_] = v, e.updateDuringAnimation(t), h.push(_));
	}
	var y = h.length;
	if (!d && y) for (var b = 0; b < m.length; b++) {
		var x = m[b];
		if (x.targetName === t && x.stopTracks(h)) {
			var S = N(m, x);
			m.splice(S, 1);
		}
	}
	if (i.force || (h = ie(h, function(e) {
		return !ma(r[e], n[e]);
	}), y = h.length), y > 0 || i.force && !o.length) {
		var C = void 0, w = void 0, T = void 0;
		if (s) {
			w = {}, f && (C = {});
			for (var b = 0; b < y; b++) {
				var _ = h[b];
				w[_] = n[_], f ? C[_] = r[_] : n[_] = r[_];
			}
		} else if (f) {
			T = {};
			for (var b = 0; b < y; b++) {
				var _ = h[b];
				T[_] = Ur(n[_]), pa(n, r, _);
			}
		}
		var x = new ri(n, !1, !1, d ? ie(m, function(e) {
			return e.targetName === t;
		}) : null);
		x.targetName = t, i.scope && (x.scope = i.scope), f && C && x.whenWithKeys(0, C, h), T && x.whenWithKeys(0, T, h), x.whenWithKeys(l ?? 500, s ? w : r, h).delay(u || 0), e.addAnimator(x, t), o.push(x);
	}
}
function _a(e, t, n, r) {
	return !(n && n.hoverLayer || r) || va(e) || t && va(t) ? 0 : 1;
}
function va(e) {
	return e.type === "text" || e.type === "tspan";
}
function ya(e, t, n) {
	return !t && !e.__inHover && n && n.duration > 0;
}
//#endregion
//#region node_modules/zrender/lib/graphic/Group.js
var ba = function(e) {
	r(t, e);
	function t(t) {
		var n = e.call(this) || this;
		return n.isGroup = !0, n._children = [], n.attr(t), n;
	}
	return t.prototype.childrenRef = function() {
		return this._children;
	}, t.prototype.children = function() {
		return this._children.slice();
	}, t.prototype.childAt = function(e) {
		return this._children[e];
	}, t.prototype.childOfName = function(e) {
		for (var t = this._children, n = 0; n < t.length; n++) if (t[n].name === e) return t[n];
	}, t.prototype.childCount = function() {
		return this._children.length;
	}, t.prototype.add = function(e) {
		return e && e !== this && e.parent !== this && (this._children.push(e), this._doAdd(e)), this;
	}, t.prototype.addBefore = function(e, t) {
		if (e && e !== this && e.parent !== this && t && t.parent === this) {
			var n = this._children, r = n.indexOf(t);
			r >= 0 && (n.splice(r, 0, e), this._doAdd(e));
		}
		return this;
	}, t.prototype.replace = function(e, t) {
		var n = N(this._children, e);
		return n >= 0 && this.replaceAt(t, n), this;
	}, t.prototype.replaceAt = function(e, t) {
		var n = this._children, r = n[t];
		if (e && e !== this && e.parent !== this && e !== r) {
			n[t] = e, r.parent = null;
			var i = this.__zr;
			i && r.removeSelfFromZr(i), this._doAdd(e);
		}
		return this;
	}, t.prototype._doAdd = function(e) {
		e.parent && e.parent.remove(e), e.parent = this;
		var t = this.__zr;
		t && t !== e.__zr && e.addSelfToZr(t), t && t.refresh();
	}, t.prototype.remove = function(e) {
		var t = this.__zr, n = this._children, r = N(n, e);
		return r < 0 || (n.splice(r, 1), e.parent = null, t && e.removeSelfFromZr(t), t && t.refresh()), this;
	}, t.prototype.removeAll = function() {
		for (var e = this._children, t = this.__zr, n = 0; n < e.length; n++) {
			var r = e[n];
			t && r.removeSelfFromZr(t), r.parent = null;
		}
		return e.length = 0, this;
	}, t.prototype.eachChild = function(e, t) {
		for (var n = this._children, r = 0; r < n.length; r++) {
			var i = n[r];
			e.call(t, i, r);
		}
		return this;
	}, t.prototype.traverse = function(e, t) {
		for (var n = 0; n < this._children.length; n++) {
			var r = this._children[n], i = e.call(t, r);
			r.isGroup && !i && r.traverse(e, t);
		}
		return this;
	}, t.prototype.addSelfToZr = function(t) {
		e.prototype.addSelfToZr.call(this, t);
		for (var n = 0; n < this._children.length; n++) this._children[n].addSelfToZr(t);
	}, t.prototype.removeSelfFromZr = function(t) {
		e.prototype.removeSelfFromZr.call(this, t);
		for (var n = 0; n < this._children.length; n++) this._children[n].removeSelfFromZr(t);
	}, t.prototype.getBoundingRect = function(e) {
		for (var t = new J(0, 0, 0, 0), n = e || this._children, r = [], i = null, a = 0; a < n.length; a++) {
			var o = n[a];
			if (!(o.ignore || o.invisible)) {
				var s = o.getBoundingRect(), c = o.getLocalTransform(r);
				c ? (J.applyTransform(t, s, c), i ||= t.clone(), i.union(t)) : (i ||= s.clone(), i.union(s));
			}
		}
		return i || t;
	}, t;
}(la);
ba.prototype.type = "group";
//#endregion
//#region node_modules/zrender/lib/zrender.js
var xa = {}, Sa = {};
function Ca(e) {
	delete Sa[e];
}
function wa(e) {
	if (!e) return !1;
	if (typeof e == "string") return Ar(e, 1) < Di;
	if (e.colorStops) {
		for (var t = e.colorStops, n = 0, r = t.length, i = 0; i < r; i++) n += Ar(t[i].color, 1);
		return n /= r, n < Di;
	}
	return !1;
}
var Ta = function() {
	function e(e, t, n) {
		var r = this;
		this._sleepAfterStill = 10, this._stillFrameAccum = 0, this._needsRefresh = !0, this._needsRefreshHover = !1, this._darkMode = !1, n ||= {}, this.dom = t, this.id = e;
		var i = new Mn(), o = n.renderer || "canvas";
		xa[o] || (o = L(xa)[0]), n.useDirtyRect = n.useDirtyRect != null && n.useDirtyRect;
		var s = new xa[o](t, i, n, e), c = n.ssr || s.ssrOnly;
		this.storage = i, this.painter = s;
		var l = !a.node && !a.worker && !c ? new wi(s.getViewportRoot(), s.root) : null, u = n.useCoarsePointer, d = u == null || u === "auto" ? a.touchEventsSupported : !!u, f = 44, p;
		d && (p = G(n.pointerSize, f)), this.handler = new hn(i, s, l, s.root, p), this.animation = new ai({ stage: { update: c ? null : function() {
			return r._flush(!1);
		} } }), c || this.animation.start();
	}
	return e.prototype.add = function(e) {
		!this._disposed && e && (this.storage.addRoot(e), e.addSelfToZr(this), this.refresh());
	}, e.prototype.remove = function(e) {
		!this._disposed && e && (this.storage.delRoot(e), e.removeSelfFromZr(this), this.refresh());
	}, e.prototype.configLayer = function(e, t) {
		this._disposed || (this.painter.configLayer && this.painter.configLayer(e, t), this.refresh());
	}, e.prototype.setBackgroundColor = function(e) {
		this._disposed || (this.painter.setBackgroundColor && this.painter.setBackgroundColor(e), this.refresh(), this._backgroundColor = e, this._darkMode = wa(e));
	}, e.prototype.getBackgroundColor = function() {
		return this._backgroundColor;
	}, e.prototype.setDarkMode = function(e) {
		this._darkMode = e;
	}, e.prototype.isDarkMode = function() {
		return this._darkMode;
	}, e.prototype.refreshImmediately = function(e) {
		this._disposed || this._refresh({
			animUpdate: !e,
			refresh: !0,
			refreshHover: !1
		});
	}, e.prototype._refresh = function(e) {
		e.animUpdate && this.animation.update(!0), this._needsRefresh = this._needsRefreshHover = !1, this.painter.refresh({
			refresh: e.refresh,
			refreshHover: e.refreshHover
		}), this._needsRefresh = this._needsRefreshHover = !1;
	}, e.prototype.refresh = function() {
		this._disposed || (this._needsRefresh = !0, this.animation.start());
	}, e.prototype.flush = function() {
		this._disposed || this._flush(!0);
	}, e.prototype._flush = function(e) {
		var t, n = ii(), r = this._needsRefresh, i = this._needsRefreshHover;
		(r || i) && (t = !0, this._refresh({
			animUpdate: e,
			refresh: r,
			refreshHover: i
		}));
		var a = ii();
		t ? (this._stillFrameAccum = 0, this.trigger("rendered", { elapsedTime: a - n })) : this._sleepAfterStill > 0 && (this._stillFrameAccum++, this._stillFrameAccum > this._sleepAfterStill && this.animation.stop());
	}, e.prototype.setSleepAfterStill = function(e) {
		this._sleepAfterStill = e;
	}, e.prototype.wakeUp = function() {
		this._disposed || (this.animation.start(), this._stillFrameAccum = 0);
	}, e.prototype.refreshHover = function() {
		this._needsRefreshHover = !0;
	}, e.prototype.refreshHoverImmediately = function() {
		this._disposed || this._refresh({
			animUpdate: !1,
			refresh: !1,
			refreshHover: !0
		});
	}, e.prototype.resize = function(e) {
		this._disposed || (e ||= {}, this.painter.resize(e.width, e.height), this.handler.resize());
	}, e.prototype.clearAnimation = function() {
		this._disposed || this.animation.clear();
	}, e.prototype.getWidth = function() {
		if (!this._disposed) return this.painter.getWidth();
	}, e.prototype.getHeight = function() {
		if (!this._disposed) return this.painter.getHeight();
	}, e.prototype.setCursorStyle = function(e) {
		this._disposed || this.handler.setCursorStyle(e);
	}, e.prototype.findHover = function(e, t) {
		if (!this._disposed) return this.handler.findHover(e, t);
	}, e.prototype.on = function(e, t, n) {
		return this._disposed || this.handler.on(e, t, n), this;
	}, e.prototype.off = function(e, t) {
		this._disposed || this.handler.off(e, t);
	}, e.prototype.trigger = function(e, t) {
		this._disposed || this.handler.trigger(e, t);
	}, e.prototype.clear = function() {
		if (!this._disposed) {
			for (var e = this.storage.getRoots(), t = 0; t < e.length; t++) e[t] instanceof ba && e[t].removeSelfFromZr(this);
			this.storage.delAllRoots(), this.painter.clear();
		}
	}, e.prototype.dispose = function() {
		this._disposed || (this.animation.stop(), this.clear(), this.storage.dispose(), this.painter.dispose(), this.handler.dispose(), this.animation = this.storage = this.painter = this.handler = null, this._disposed = !0, Ca(this.id));
	}, e;
}();
function Ea(e, t) {
	var n = new Ta(D(), e, t);
	return Sa[n.id] = n, n;
}
function Da(e, t) {
	xa[e] = t;
}
//#endregion
//#region node_modules/echarts/lib/util/number.js
var Oa = 1e-4, ka = 20;
function Aa(e) {
	return e.replace(/^\s+|\s+$/g, "");
}
var ja = Math.min, Ma = Math.max, Na = Math.abs, Pa = Math.round, Fa = Math.pow, Ia = Math.PI, La = Math.random;
function Ra(e, t, n, r) {
	var i = t[0], a = t[1], o = n[0], s = n[1], c = a - i, l = s - o;
	if (c === 0) return l === 0 ? o : (o + s) / 2;
	if (r) {
		if (c > 0) {
			if (e <= i) return o;
			if (e >= a) return s;
		} else if (e >= i) return o;
		else if (e <= a) return s;
	} else {
		if (e === i) return o;
		if (e === a) return s;
	}
	return (e - i) / c * l + o;
}
var za = Ba;
function Ba(e, t, n) {
	switch (e) {
		case "center":
		case "middle":
			e = "50%";
			break;
		case "left":
		case "top":
			e = "0%";
			break;
		case "right":
		case "bottom": e = "100%";
	}
	return Va(e, t, n);
}
function Va(e, t, n) {
	return H(e) ? Ua(e) ? parseFloat(e) / 100 * t + (n || 0) : parseFloat(e) : e == null ? NaN : +e;
}
function Ha(e) {
	return H(e) && Ua(e);
}
function Ua(e) {
	return !!Aa(e).match(/%$/);
}
function Wa(e, t, n) {
	return isNaN(t) ? n ? "" + e : +e : (t = ja(Ma(0, t), ka), e = (+e).toFixed(t), n ? e : +e);
}
function Ga(e) {
	return e.sort(function(e, t) {
		return e - t;
	}), e;
}
function Ka(e) {
	if (e = +e, isNaN(e)) return 0;
	if (e > 1e-14) {
		for (var t = 1, n = 0; n < 15; n++, t *= 10) if (Pa(e * t) / t === e) return n;
	}
	return qa(e);
}
function qa(e) {
	var t = e.toString().toLowerCase(), n = t.indexOf("e"), r = n > 0 ? +t.slice(n + 1) : 0, i = n > 0 ? n : t.length, a = t.indexOf(".");
	return Ma(0, (a < 0 ? 0 : i - 1 - a) - r);
}
function Ja(e, t) {
	var n = Ma(Ka(e), Ka(t)), r = e + t;
	return n > ka ? r : Wa(r, n);
}
Fa(2, 53) - 1;
function Ya(e) {
	var t = Ia * 2;
	return (e % t + t) % t;
}
function Xa(e) {
	return e > -Oa && e < Oa;
}
var Za = /^(?:(\d{4})(?:[-\/](\d{1,2})(?:[-\/](\d{1,2})(?:[T ](\d{1,2})(?::(\d{1,2})(?::(\d{1,2})(?:[.,](\d+))?)?)?(Z|[\+\-]\d\d:?\d\d)?)?)?)?)?$/;
function Qa(e) {
	if (e instanceof Date) return e;
	if (H(e)) {
		var t = Za.exec(e);
		if (!t) return /* @__PURE__ */ new Date(NaN);
		if (t[8]) {
			var n = +t[4] || 0;
			return t[8].toUpperCase() !== "Z" && (n -= +t[8].slice(0, 3)), new Date(Date.UTC(+t[1], (t[2] || 1) - 1, +t[3] || 1, n, +(t[5] || 0), +t[6] || 0, t[7] ? +t[7].substring(0, 3) : 0));
		}
		return new Date(+t[1], (t[2] || 1) - 1, +t[3] || 1, +t[4] || 0, +(t[5] || 0), +t[6] || 0, t[7] ? +t[7].substring(0, 3) : 0);
	}
	return e == null ? /* @__PURE__ */ new Date(NaN) : new Date(Pa(e));
}
function $a(e) {
	var t = parseFloat(e);
	return t == e && (t !== 0 || !H(e) || e.indexOf("x") <= 0) ? t : NaN;
}
function eo(e) {
	return !isNaN($a(e));
}
function to() {
	return Pa(La() * 9);
}
function no(e, t) {
	return t === 0 ? e : no(t, e % t);
}
function ro(e, t) {
	return e == null ? t : t == null ? e : e * t / no(e, t);
}
function io(e) {
	return e != null && isFinite(e);
}
//#endregion
//#region node_modules/echarts/lib/util/log.js
var ao = "[ECharts] ", oo = {}, so = typeof console < "u" && console.warn && console.log;
function co(e, t, n) {
	if (so) {
		if (n) {
			if (oo[t]) return;
			oo[t] = !0;
		}
		console[e](ao + t);
	}
}
function lo(e, t) {
	co("error", e, t);
}
function uo(e) {
	throw Error(e);
}
//#endregion
//#region node_modules/echarts/lib/util/model.js
var fo = "series\0", po = "\0_ec_\0";
function mo(e) {
	return e instanceof Array ? e : e == null ? [] : [e];
}
function ho(e, t, n) {
	if (e) {
		e[t] = e[t] || {}, e.emphasis = e.emphasis || {}, e.emphasis[t] = e.emphasis[t] || {};
		for (var r = 0, i = n.length; r < i; r++) {
			var a = n[r];
			!e.emphasis[t].hasOwnProperty(a) && e[t].hasOwnProperty(a) && (e.emphasis[t][a] = e[t][a]);
		}
	}
}
var go = /* @__PURE__ */ "fontStyle.fontWeight.fontSize.fontFamily.rich.tag.color.textBorderColor.textBorderWidth.width.height.lineHeight.align.verticalAlign.baseline.shadowColor.shadowBlur.shadowOffsetX.shadowOffsetY.textShadowColor.textShadowBlur.textShadowOffsetX.textShadowOffsetY.backgroundColor.borderColor.borderWidth.borderRadius.padding".split(".");
function _o(e) {
	return W(e) && !B(e) && !(e instanceof Date) ? e.value : e;
}
function vo(e) {
	return W(e) && !(e instanceof Array);
}
function yo(e, t, n) {
	var r = n === "normalMerge", i = n === "replaceMerge", a = n === "replaceAll";
	e ||= [], t = (t || []).slice();
	var o = K();
	F(t, function(e, n) {
		W(e) || (t[n] = null);
	});
	var s = bo(e, o, n);
	return (r || i) && xo(s, e, o, t), r && So(s, t), r || i ? Co(s, t, i) : a && wo(s, t), To(s), s;
}
function bo(e, t, n) {
	var r = [];
	if (n === "replaceAll") return r;
	for (var i = 0; i < e.length; i++) {
		var a = e[i];
		a && a.id != null && t.set(a.id, i), r.push({
			existing: n === "replaceMerge" || Ao(a) ? null : a,
			newOption: null,
			keyInfo: null,
			brandNew: null
		});
	}
	return r;
}
function xo(e, t, n, r) {
	F(r, function(i, a) {
		if (i && i.id != null) {
			var o = Do(i.id), s = n.get(o);
			if (s != null) {
				var c = e[s];
				_e(!c.newOption, "Duplicated option on id \"" + o + "\"."), c.newOption = i, c.existing = t[s], r[a] = null;
			}
		}
	});
}
function So(e, t) {
	F(t, function(n, r) {
		if (n && n.name != null) for (var i = 0; i < e.length; i++) {
			var a = e[i].existing;
			if (!e[i].newOption && a && (a.id == null || n.id == null) && !Ao(n) && !Ao(a) && Eo("name", a, n)) {
				e[i].newOption = n, t[r] = null;
				return;
			}
		}
	});
}
function Co(e, t, n) {
	F(t, function(t) {
		if (t) {
			for (var r, i = 0; (r = e[i]) && (r.newOption || Ao(r.existing) || r.existing && t.id != null && !Eo("id", t, r.existing));) i++;
			r ? (r.newOption = t, r.brandNew = n) : e.push({
				newOption: t,
				brandNew: n,
				existing: null,
				keyInfo: null
			}), i++;
		}
	});
}
function wo(e, t) {
	F(t, function(t) {
		e.push({
			newOption: t,
			brandNew: !0,
			existing: null,
			keyInfo: null
		});
	});
}
function To(e) {
	var t = K();
	F(e, function(e) {
		var n = e.existing;
		n && t.set(n.id, e);
	}), F(e, function(e) {
		var n = e.newOption;
		_e(!n || n.id == null || !t.get(n.id) || t.get(n.id) === e, "id duplicates: " + (n && n.id)), n && n.id != null && t.set(n.id, e), !e.keyInfo && (e.keyInfo = {});
	}), F(e, function(e, n) {
		var r = e.existing, i = e.newOption, a = e.keyInfo;
		if (W(i)) {
			if (a.name = i.name == null ? r ? r.name : fo + n : Do(i.name), r) a.id = Do(r.id);
			else if (i.id != null) a.id = Do(i.id);
			else {
				var o = 0;
				do
					a.id = "\0" + a.name + "\0" + o++;
				while (t.get(a.id));
			}
			t.set(a.id, e);
		}
	});
}
function Eo(e, t, n) {
	var r = Oo(t[e], null), i = Oo(n[e], null);
	return r != null && i != null && r === i;
}
function Do(e) {
	return Oo(e, "");
}
function Oo(e, t) {
	return e == null ? t : H(e) ? e : U(e) || oe(e) ? e + "" : t;
}
function ko(e) {
	var t = e.name;
	return !!(t && t.indexOf(fo));
}
function Ao(e) {
	return e && e.id != null && Do(e.id).indexOf(po) === 0;
}
function jo(e, t, n) {
	F(e, function(e) {
		var r = e.newOption;
		W(r) && (e.keyInfo.mainType = t, e.keyInfo.subType = Mo(t, r, e.existing, n));
	});
}
function Mo(e, t, n, r) {
	return t.type ? t.type : n ? n.subType : r.determineSubType(e, t);
}
function No(e, t) {
	if (t.dataIndexInside != null) return t.dataIndexInside;
	if (t.dataIndex != null) return B(t.dataIndex) ? I(t.dataIndex, function(t) {
		return e.indexOfRawIndex(t);
	}) : e.indexOfRawIndex(t.dataIndex);
	if (t.name != null) return B(t.name) ? I(t.name, function(t) {
		return e.indexOfName(t);
	}) : e.indexOfName(t.name);
}
function Y() {
	var e = "__ec_inner_" + Po++;
	return function(t) {
		return t[e] || (t[e] = {});
	};
}
var Po = to();
function Fo(e, t, n) {
	var r = Io(t, n), i = r.mainTypeSpecified, a = r.queryOptionMap, o = r.others, s = n ? n.defaultMainType : null;
	return !i && s && a.set(s, {}), a.each(function(t, r) {
		var i = Ro(e, r, t, {
			useDefault: s === r,
			enableAll: n && n.enableAll != null ? n.enableAll : !0,
			enableNone: n && n.enableNone != null ? n.enableNone : !0
		});
		o[r + "Models"] = i.models, o[r + "Model"] = i.models[0];
	}), o;
}
function Io(e, t) {
	var n;
	if (H(e)) {
		var r = {};
		r[e + "Index"] = 0, n = r;
	} else n = e;
	var i = K(), a = {}, o = !1;
	return F(n, function(e, n) {
		if (n === "dataIndex" || n === "dataIndexInside") a[n] = e;
		else {
			var r = n.match(/^(\w+)(Index|Id|Name)$/) || [], s = r[1], c = (r[2] || "").toLowerCase();
			if (!(!s || !c || t && t.includeMainTypes && N(t.includeMainTypes, s) < 0)) {
				o ||= !!s;
				var l = i.get(s) || i.set(s, {});
				l[c] = e;
			}
		}
	}), {
		mainTypeSpecified: o,
		queryOptionMap: i,
		others: a
	};
}
var Lo = {
	useDefault: !0,
	enableAll: !1,
	enableNone: !1
};
function Ro(e, t, n, r) {
	r ||= Lo;
	var i = n.index, a = n.id, o = n.name, s = {
		models: null,
		specified: i != null || a != null || o != null
	};
	if (!s.specified) {
		var c = void 0;
		return s.models = r.useDefault && (c = e.getComponent(t)) ? [c] : [], s;
	}
	if (i === "none" || i === !1) {
		if (r.enableNone) return s.models = [], s;
		i = -1;
	}
	return i === "all" && (i = r.enableAll ? a = o = null : -1), s.models = e.queryComponents({
		mainType: t,
		index: i,
		id: a,
		name: o
	}), s;
}
function zo(e, t, n) {
	var r = {};
	r[t + "Id"] = e[t + "Id"], r[t + "Index"] = e[t + "Index"], r[t + "Name"] = e[t + "Name"];
	var i = {
		mainType: t,
		query: r
	};
	return n && (i.subType = n), i;
}
function Bo(e, t, n) {
	e.setAttribute ? e.setAttribute(t, n) : e[t] = n;
}
function Vo(e, t) {
	return e.getAttribute ? e.getAttribute(t) : e[t];
}
function Ho(e) {
	return e === "auto" ? a.domSupported ? "html" : "richText" : e || "html";
}
(function() {
	function e() {}
	return e.prototype.reset = function(e, t, n, r) {
		return this._list = e, this._step = r ||= 1, this._idx = t, this._end = n ?? (r > 0 ? e.length : 0), this.item = null, this.key = NaN, this;
	}, e.prototype.next = function() {
		return (this._step > 0 ? this._idx < this._end : this._idx >= this._end) && (this.item = this._list[this._idx], this.key = this._idx += this._step, !0);
	}, e;
})();
function Uo() {
	return [Infinity, -Infinity];
}
function Wo() {
	var e = "__ec_once_" + Go++;
	return function(t, n) {
		ke(t, e) || (t[e] = 1, n());
	};
}
var Go = to();
function Ko(e, t, n) {
	var r = K(), i = 0;
	F(e, function(a) {
		var o = t(a), s = r.get(o) || 0;
		n && n(a, s), !s && !n && (e[i++] = a), r.set(o, s + 1);
	}), n || (e.length = i);
}
function qo(e, t, n) {
	var r = e.getData().count();
	return {
		progressiveRender: n.progressiveEnabled && t.incrementalPrepareRender && r >= n.threshold,
		large: e.get("large") && r >= e.get("largeThreshold"),
		modDataCount: e.get("progressiveChunkMode") === "mod" ? e.getData().count() : null
	};
}
function Jo(e, t) {
	return {
		seriesType: e,
		overallReset: t
	};
}
function Yo(e) {
	return { overallReset: e };
}
//#endregion
//#region node_modules/echarts/lib/util/clazz.js
var Xo = ".", Zo = "___EC__COMPONENT__CONTAINER___", Qo = "___EC__EXTENDED_CLASS___";
function $o(e) {
	var t = {
		main: "",
		sub: ""
	};
	if (e) {
		var n = e.split(Xo);
		t.main = n[0] || "", t.sub = n[1] || "";
	}
	return t;
}
function es(e) {
	_e(/^[a-zA-Z0-9_]+([.][a-zA-Z0-9_]+)?$/.test(e), "componentType \"" + e + "\" illegal");
}
function ts(e) {
	return !!(e && e[Qo]);
}
function ns(e, t) {
	e.$constructor = e, e.extend = function(e) {
		var t = this, n;
		return rs(t) ? n = function(e) {
			r(t, e);
			function t() {
				return e.apply(this, arguments) || this;
			}
			return t;
		}(t) : (n = function() {
			(e.$constructor || t).apply(this, arguments);
		}, te(n, this)), j(n.prototype, e), n[Qo] = !0, n.extend = this.extend, n.superCall = ss, n.superApply = cs, n.superClass = t, n;
	};
}
function rs(e) {
	return V(e) && /^class\s/.test(Function.prototype.toString.call(e));
}
function is(e, t) {
	e.extend = t.extend;
}
var as = Math.round(Math.random() * 10);
function os(e) {
	var t = ["__\0is_clz", as++].join("_");
	e.prototype[t] = !0, e.isInstance = function(e) {
		return !!(e && e[t]);
	};
}
function ss(e, t) {
	var n = [...arguments].slice(2);
	return this.superClass.prototype[t].apply(e, n);
}
function cs(e, t, n) {
	return this.superClass.prototype[t].apply(e, n);
}
function ls(e) {
	var t = {};
	e.registerClass = function(e) {
		var r = e.type || e.prototype.type;
		if (r) {
			es(r), e.prototype.type = r;
			var i = $o(r);
			if (!i.sub) t[i.main] = e;
			else if (i.sub !== Zo) {
				var a = n(i);
				a[i.sub] = e;
			}
		}
		return e;
	}, e.getClass = function(e, n, r) {
		var i = t[e];
		if (i && i[Zo] && (i = n ? i[n] : null), r && !i) throw Error(n ? "Component " + e + "." + (n || "") + " is used but not imported." : e + ".type should be specified.");
		return i;
	}, e.getClassesByMainType = function(e) {
		var n = $o(e), r = [], i = t[n.main];
		return i && i[Zo] ? F(i, function(e, t) {
			t !== Zo && r.push(e);
		}) : r.push(i), r;
	}, e.hasClass = function(e) {
		return !!t[$o(e).main];
	}, e.getAllClassMainTypes = function() {
		var e = [];
		return F(t, function(t, n) {
			e.push(n);
		}), e;
	}, e.hasSubTypes = function(e) {
		var n = t[$o(e).main];
		return n && n[Zo];
	};
	function n(e) {
		var n = t[e.main];
		return (!n || !n[Zo]) && (n = t[e.main] = {}, n[Zo] = !0), n;
	}
}
//#endregion
//#region node_modules/echarts/lib/model/mixin/makeStyleMapper.js
function us(e, t) {
	for (var n = 0; n < e.length; n++) e[n][1] || (e[n][1] = e[n][0]);
	return t ||= !1, function(n, r, i) {
		for (var a = {}, o = 0; o < e.length; o++) {
			var s = e[o][1];
			if (!(r && N(r, s) >= 0 || i && N(i, s) < 0)) {
				var c = n.getShallow(s, t);
				c != null && (a[e[o][0]] = c);
			}
		}
		return a;
	};
}
var ds = us([
	["fill", "color"],
	["shadowBlur"],
	["shadowOffsetX"],
	["shadowOffsetY"],
	["opacity"],
	["shadowColor"]
]), fs = function() {
	function e() {}
	return e.prototype.getAreaStyle = function(e, t) {
		return ds(this, e, t);
	}, e;
}(), ps = new dr(50);
function ms(e) {
	if (typeof e == "string") {
		var t = ps.get(e);
		return t && t.image;
	}
	return e;
}
function hs(e, t, n, r, i) {
	if (!e) return t;
	if (typeof e == "string") {
		if (t && t.__zrImageSrc === e || !n) return t;
		var a = ps.get(e), o = {
			hostEl: n,
			cb: r,
			cbPayload: i
		};
		return a ? (t = a.image, !_s(t) && a.pending.push(o)) : (t = p.loadImage(e, gs, gs), t.__zrImageSrc = e, ps.put(e, t.__cachedImgObj = {
			image: t,
			pending: [o]
		})), t;
	}
	return e;
}
function gs() {
	var e = this.__cachedImgObj;
	this.onload = this.onerror = this.__cachedImgObj = null;
	for (var t = 0; t < e.pending.length; t++) {
		var n = e.pending[t], r = n.cb;
		r && r(this, n.cbPayload), n.hostEl.dirty();
	}
	e.pending.length = 0;
}
function _s(e) {
	return e && e.width && e.height;
}
//#endregion
//#region node_modules/zrender/lib/graphic/helper/parseText.js
var vs = /\{([a-zA-Z0-9_]+)\|([^}]*)\}/g;
function ys(e, t, n, r, i, a) {
	if (!n) e.text = "", e.isTruncated = !1;
	else {
		var o = (t + "").split("\n");
		a = bs(n, r, i, a);
		for (var s = !1, c = {}, l = 0, u = o.length; l < u; l++) xs(c, o[l], a), o[l] = c.textLine, s ||= c.isTruncated;
		e.text = o.join("\n"), e.isTruncated = s;
	}
}
function bs(e, t, n, r) {
	r ||= {};
	var i = j({}, r);
	n = G(n, "..."), i.maxIterations = G(r.maxIterations, 2);
	var a = i.minChar = G(r.minChar, 0), o = i.fontMeasureInfo = Ui(t), s = o.asciiCharWidth;
	i.placeholder = G(r.placeholder, "");
	for (var c = e = Math.max(0, e - 1), l = 0; l < a && c >= s; l++) c -= s;
	var u = Yi(o, n);
	return u > c && (n = "", u = 0), c = e - u, i.ellipsis = n, i.ellipsisWidth = u, i.contentWidth = c, i.containerWidth = e, i;
}
function xs(e, t, n) {
	var r = n.containerWidth, i = n.contentWidth, a = n.fontMeasureInfo;
	if (!r) e.textLine = "", e.isTruncated = !1;
	else {
		var o = Yi(a, t);
		if (o <= r) e.textLine = t, e.isTruncated = !1;
		else {
			for (var s = 0;; s++) {
				if (o <= i || s >= n.maxIterations) {
					t += n.ellipsis;
					break;
				}
				var c = s === 0 ? Ss(t, i, a) : o > 0 ? Math.floor(t.length * i / o) : 0;
				t = t.substr(0, c), o = Yi(a, t);
			}
			t === "" && (t = n.placeholder), e.textLine = t, e.isTruncated = !0;
		}
	}
}
function Ss(e, t, n) {
	for (var r = 0, i = 0, a = e.length; i < a && r < t; i++) r += Ji(n, e.charCodeAt(i));
	return i;
}
function Cs(e, t, n, r) {
	var i = Is(e), a = t.overflow, o = t.padding, s = o ? o[1] + o[3] : 0, c = o ? o[0] + o[2] : 0, l = t.font, u = a === "truncate", d = ea(l), f = G(t.lineHeight, d), p = t.lineOverflow === "truncate", m = !1, h = t.width;
	h == null && n != null && (h = n - s);
	var g = t.height;
	g == null && r != null && (g = r - c);
	var _ = h != null && (a === "break" || a === "breakAll") ? i ? Ms(i, t.font, h, a === "breakAll", 0).lines : [] : i ? i.split("\n") : [], v = _.length * f;
	if (g ??= v, v > g && p) {
		var y = Math.floor(g / f);
		m ||= _.length > y, _ = _.slice(0, y), v = _.length * f;
	}
	if (i && u && h != null) for (var b = bs(h, l, t.ellipsis, {
		minChar: t.truncateMinChar,
		placeholder: t.placeholder
	}), x = {}, S = 0; S < _.length; S++) xs(x, _[S], b), _[S] = x.textLine, m ||= x.isTruncated;
	for (var C = g, w = 0, T = Ui(l), S = 0; S < _.length; S++) w = Math.max(Yi(T, _[S]), w);
	h ??= w;
	var E = h;
	return C += c, E += s, {
		lines: _,
		height: g,
		outerWidth: E,
		outerHeight: C,
		lineHeight: f,
		calculatedLineHeight: d,
		contentWidth: w,
		contentHeight: v,
		width: h,
		isTruncated: m
	};
}
var ws = function() {
	function e() {}
	return e;
}(), Ts = function() {
	function e(e) {
		this.tokens = [], e && (this.tokens = e);
	}
	return e;
}(), Es = function() {
	function e() {
		this.width = 0, this.height = 0, this.contentWidth = 0, this.contentHeight = 0, this.outerWidth = 0, this.outerHeight = 0, this.lines = [], this.isTruncated = !1;
	}
	return e;
}();
function Ds(e, t, n, r, i) {
	var a = new Es(), o = Is(e);
	if (!o) return a;
	var s = t.padding, c = s ? s[1] + s[3] : 0, l = s ? s[0] + s[2] : 0, u = t.width;
	u == null && n != null && (u = n - c);
	var d = t.height;
	d == null && r != null && (d = r - l);
	for (var f = t.overflow, p = (f === "break" || f === "breakAll") && u != null ? {
		width: u,
		accumWidth: 0,
		breakAll: f === "breakAll"
	} : null, m = vs.lastIndex = 0, h; (h = vs.exec(o)) != null;) {
		var g = h.index;
		g > m && Os(a, o.substring(m, g), t, p), Os(a, h[2], t, p, h[1]), m = vs.lastIndex;
	}
	m < o.length && Os(a, o.substring(m, o.length), t, p);
	var _ = [], v = 0, y = 0, b = f === "truncate", x = t.lineOverflow === "truncate", S = {};
	function C(e, t, n) {
		e.width = t, e.lineHeight = n, v += n, y = Math.max(y, t);
	}
	outer: for (var w = 0; w < a.lines.length; w++) {
		for (var T = a.lines[w], E = 0, D = 0, O = 0; O < T.tokens.length; O++) {
			var k = T.tokens[O], A = k.styleName && t.rich[k.styleName] || {}, j = k.textPadding = A.padding, ee = j ? j[1] + j[3] : 0, M = k.font = A.font || t.font;
			k.contentHeight = ea(M);
			var N = G(A.height, k.contentHeight);
			if (k.innerHeight = N, j && (N += j[0] + j[2]), k.height = N, k.lineHeight = me(A.lineHeight, t.lineHeight, N), k.align = A && A.align || i, k.verticalAlign = A && A.verticalAlign || "middle", x && d != null && v + k.lineHeight > d) {
				var te = a.lines.length;
				O > 0 ? (T.tokens = T.tokens.slice(0, O), C(T, D, E), a.lines = a.lines.slice(0, w + 1)) : a.lines = a.lines.slice(0, w), a.isTruncated = a.isTruncated || a.lines.length < te;
				break outer;
			}
			var ne = A.width, P = ne == null || ne === "auto";
			if (typeof ne == "string" && ne.charAt(ne.length - 1) === "%") k.percentWidth = ne, _.push(k), k.contentWidth = Yi(Ui(M), k.text);
			else {
				if (P) {
					var F = A.backgroundColor, I = F && F.image;
					I && (I = ms(I), _s(I) && (k.width = Math.max(k.width, I.width * N / I.height)));
				}
				var re = b && u != null ? u - D : null;
				re != null && re < k.width ? !P || re < ee ? (k.text = "", k.width = k.contentWidth = 0) : (ys(S, k.text, re - ee, M, t.ellipsis, { minChar: t.truncateMinChar }), k.text = S.text, a.isTruncated = a.isTruncated || S.isTruncated, k.width = k.contentWidth = Yi(Ui(M), k.text)) : k.contentWidth = Yi(Ui(M), k.text);
			}
			k.width += ee, D += k.width, A && (E = Math.max(E, k.lineHeight));
		}
		C(T, D, E);
	}
	a.outerWidth = a.width = G(u, y), a.outerHeight = a.height = G(d, v), a.contentHeight = v, a.contentWidth = y, a.outerWidth += c, a.outerHeight += l;
	for (var w = 0; w < _.length; w++) {
		var k = _[w], ie = k.percentWidth;
		k.width = parseInt(ie, 10) / 100 * a.width;
	}
	return a;
}
function Os(e, t, n, r, i) {
	var a = t === "", o = i && n.rich[i] || {}, s = e.lines, c = o.font || n.font, l = !1, u, d;
	if (r) {
		var f = o.padding, p = f ? f[1] + f[3] : 0;
		if (o.width != null && o.width !== "auto") {
			var m = ta(o.width, r.width) + p;
			s.length > 0 && m + r.accumWidth > r.width && (u = t.split("\n"), l = !0), r.accumWidth = m;
		} else {
			var h = Ms(t, c, r.width, r.breakAll, r.accumWidth);
			r.accumWidth = h.accumWidth + p, d = h.linesWidths, u = h.lines;
		}
	}
	u ||= t.split("\n");
	for (var g = Ui(c), _ = 0; _ < u.length; _++) {
		var v = u[_], y = new ws();
		if (y.styleName = i, y.text = v, y.isLineHolder = !v && !a, y.width = typeof o.width == "number" ? o.width : d ? d[_] : Yi(g, v), !_ && !l) {
			var b = (s[s.length - 1] || (s[0] = new Ts())).tokens, x = b.length;
			x === 1 && b[0].isLineHolder ? b[0] = y : (v || !x || a) && b.push(y);
		} else s.push(new Ts([y]));
	}
}
function ks(e) {
	var t = e.charCodeAt(0);
	return t >= 32 && t <= 591 || t >= 880 && t <= 4351 || t >= 4608 && t <= 5119 || t >= 7680 && t <= 8303;
}
var As = re(",&?/;] ".split(""), function(e, t) {
	return e[t] = !0, e;
}, {});
function js(e) {
	return !ks(e) || !!As[e];
}
function Ms(e, t, n, r, i) {
	for (var a = [], o = [], s = "", c = "", l = 0, u = 0, d = Ui(t), f = 0; f < e.length; f++) {
		var p = e.charAt(f);
		if (p === "\n") c && (s += c, u += l), a.push(s), o.push(u), s = "", c = "", l = 0, u = 0;
		else {
			var m = Ji(d, p.charCodeAt(0)), h = !r && !js(p);
			(a.length ? u + m > n : i + u + m > n) ? u ? (s || c) && (h ? (s || (s = c, c = "", l = 0, u = l), a.push(s), o.push(u - l), c += p, l += m, s = "", u = l) : (c && (s += c, c = "", l = 0), a.push(s), o.push(u), s = p, u = m)) : h ? (a.push(c), o.push(l), c = p, l = m) : (a.push(p), o.push(m)) : (u += m, h ? (c += p, l += m) : (c && (s += c, c = "", l = 0), s += p));
		}
	}
	return c && (s += c), s && (a.push(s), o.push(u)), a.length === 1 && (u += i), {
		accumWidth: u,
		lines: a,
		linesWidths: o
	};
}
function Ns(e, t, n, r, i, a) {
	if (e.baseX = n, e.baseY = r, e.outerWidth = e.outerHeight = null, t) {
		var o = t.width * 2, s = t.height * 2;
		J.set(Ps, Qi(n, o, i), $i(r, s, a), o, s), J.intersect(t, Ps, null, Fs);
		var c = Fs.outIntersectRect;
		e.outerWidth = c.width, e.outerHeight = c.height, e.baseX = Qi(c.x, c.width, i, !0), e.baseY = $i(c.y, c.height, a, !0);
	}
}
var Ps = new J(0, 0, 0, 0), Fs = {
	outIntersectRect: {},
	clamp: !0
};
function Is(e) {
	return e == null ? e = "" : e += "";
}
function Ls(e) {
	var t = Is(e.text), n = e.font;
	return Rs(e, Yi(Ui(n), t), ea(n), null);
}
function Rs(e, t, n, r) {
	var i = new J(Qi(e.x || 0, t, e.textAlign), $i(e.y || 0, n, e.textBaseline), t, n), a = r ?? (zs(e) ? e.lineWidth : 0);
	return a > 0 && (i.x -= a / 2, i.y -= a / 2, i.width += a, i.height += a), i;
}
function zs(e) {
	var t = e.stroke;
	return t != null && t !== "none" && e.lineWidth > 0;
}
//#endregion
//#region node_modules/zrender/lib/graphic/Displayable.js
var Bs = "__zr_style_" + Math.round(Math.random() * 10), Vs = {
	shadowBlur: 0,
	shadowOffsetX: 0,
	shadowOffsetY: 0,
	shadowColor: "#000",
	opacity: 1,
	blend: "source-over"
}, Hs = { style: {
	shadowBlur: !0,
	shadowOffsetX: !0,
	shadowOffsetY: !0,
	shadowColor: !0,
	opacity: !0
} };
Vs[Bs] = !0;
var Us = [
	"z",
	"z2",
	"invisible"
], Ws = ["invisible"], Gs = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype._init = function(t) {
		for (var n = L(t), r = 0; r < n.length; r++) {
			var i = n[r];
			i === "style" ? this.useStyle(t[i]) : e.prototype.attrKV.call(this, i, t[i]);
		}
		this.style || this.useStyle({});
	}, t.prototype.beforeBrush = function(e) {}, t.prototype.afterBrush = function() {}, t.prototype.innerBeforeBrush = function() {}, t.prototype.innerAfterBrush = function() {}, t.prototype.shouldBePainted = function(e, t, n, r) {
		var i = this.transform;
		if (this.ignore || this.invisible || this.style.opacity === 0 || this.culling && Js(this, e, t) || i && !i[0] && !i[3]) return !1;
		if (n && this.__clipPaths && this.__clipPaths.length) {
			for (var a = 0; a < this.__clipPaths.length; ++a) if (this.__clipPaths[a].isZeroArea()) return !1;
		}
		if (r && this.parent) for (var o = this.parent; o;) {
			if (o.ignore) return !1;
			o = o.parent;
		}
		return !0;
	}, t.prototype.contain = function(e, t) {
		return this.rectContain(e, t);
	}, t.prototype.traverse = function(e, t) {
		e.call(t, this);
	}, t.prototype.rectContain = function(e, t) {
		var n = this.transformCoordToLocal(e, t);
		return this.getBoundingRect().contain(n[0], n[1]);
	}, t.prototype.getPaintRect = function() {
		var e = this._paintRect;
		if (!this._paintRect || this.__dirty) {
			var t = this.transform, n = this.getBoundingRect(), r = this.style, i = r.shadowBlur || 0, a = r.shadowOffsetX || 0, o = r.shadowOffsetY || 0;
			e = this._paintRect ||= new J(0, 0, 0, 0), t ? J.applyTransform(e, n, t) : e.copy(n), (i || a || o) && (e.width += i * 2 + Math.abs(a), e.height += i * 2 + Math.abs(o), e.x = Math.min(e.x, e.x + a - i), e.y = Math.min(e.y, e.y + o - i));
			var s = this.dirtyRectTolerance;
			e.isZero() || (e.x = Math.floor(e.x - s), e.y = Math.floor(e.y - s), e.width = Math.ceil(e.width + 1 + s * 2), e.height = Math.ceil(e.height + 1 + s * 2));
		}
		return e;
	}, t.prototype.setPrevPaintRect = function(e) {
		e ? (this._prevPaintRect = this._prevPaintRect || new J(0, 0, 0, 0), this._prevPaintRect.copy(e)) : this._prevPaintRect = null;
	}, t.prototype.getPrevPaintRect = function() {
		return this._prevPaintRect;
	}, t.prototype.animateStyle = function(e) {
		return this.animate("style", e);
	}, t.prototype.updateDuringAnimation = function(e) {
		e === "style" ? this.dirtyStyle() : this.markRedraw();
	}, t.prototype.attrKV = function(t, n) {
		t === "style" ? this.style ? this.setStyle(n) : this.useStyle(n) : e.prototype.attrKV.call(this, t, n);
	}, t.prototype.setStyle = function(e, t) {
		return typeof e == "string" ? this.style[e] = t : j(this.style, e), this.dirtyStyle(), this;
	}, t.prototype.dirtyStyle = function(e) {
		e || this.markRedraw(), this.__dirty |= 2, this._rect &&= null;
	}, t.prototype.dirty = function() {
		this.dirtyStyle();
	}, t.prototype.styleChanged = function() {
		return !!(this.__dirty & 2);
	}, t.prototype.styleUpdated = function() {
		this.__dirty &= -3;
	}, t.prototype.createStyle = function(e) {
		return De(Vs, e);
	}, t.prototype.useStyle = function(e) {
		e[Bs] || (e = this.createStyle(e)), this.style = e, this.dirtyStyle();
	}, t.prototype._useHoverStyle = function(e) {
		this.__hoverStyle = e;
	}, t.prototype.isStyleObject = function(e) {
		return e[Bs];
	}, t.prototype._innerSaveToNormal = function(t) {
		e.prototype._innerSaveToNormal.call(this, t);
		var n = this._normalState;
		t.style && !n.style && (n.style = this._mergeStyle(this.createStyle(), this.style)), this._savePrimaryToNormal(t, n, Us);
	}, t.prototype._applyStateObj = function(t, n, r, i, a, o) {
		e.prototype._applyStateObj.call(this, t, n, r, i, a, o);
		var s = !(n && i), c = this.__inHover === 1, l;
		if (n && n.style ? a ? i ? l = n.style : (l = this._mergeStyle(this.createStyle(), r.style), this._mergeStyle(l, n.style)) : (l = this._mergeStyle(this.createStyle(), i ? this.style : r.style), this._mergeStyle(l, n.style)) : s && (l = r.style), l) {
			if (a) {
				var u = this.style;
				if (this.style = this.createStyle(s ? {} : u), s) for (var d = L(u), f = 0; f < d.length; f++) {
					var p = d[f];
					p in l && (l[p] = l[p], this.style[p] = u[p]);
				}
				for (var m = L(l), f = 0; f < m.length; f++) {
					var p = m[f];
					this.style[p] = this.style[p];
				}
				this._transitionState(t, { style: l }, o, this.getAnimationStyleProps());
			} else c ? this._useHoverStyle(l) : this.useStyle(l);
		}
		if (!c) for (var h = this.__inHover ? Ws : Us, f = 0; f < h.length; f++) {
			var p = h[f];
			n && n[p] != null ? this[p] = n[p] : s && r[p] != null && (this[p] = r[p]);
		}
	}, t.prototype._mergeStates = function(t) {
		for (var n = e.prototype._mergeStates.call(this, t), r, i = 0; i < t.length; i++) {
			var a = t[i];
			a.style && (r ||= {}, this._mergeStyle(r, a.style));
		}
		return r && (n.style = r), n;
	}, t.prototype._mergeStyle = function(e, t) {
		return j(e, t), e;
	}, t.prototype.getAnimationStyleProps = function() {
		return Hs;
	}, t.initDefaultProps = (function() {
		var e = t.prototype;
		e.type = "displayable", e.invisible = !1, e.z = 0, e.z2 = 0, e.zlevel = 0, e.culling = !1, e.cursor = "pointer", e.rectHover = !1, e.incremental = 0, e._rect = null, e.dirtyRectTolerance = 0, e.__dirty = 3;
	})(), t;
}(la), Ks = new J(0, 0, 0, 0), qs = new J(0, 0, 0, 0);
function Js(e, t, n) {
	return Ks.copy(e.getBoundingRect()), e.transform && Ks.applyTransform(e.transform), qs.width = t, qs.height = n, !Ks.intersect(qs);
}
//#endregion
//#region node_modules/zrender/lib/core/bbox.js
var Ys = Math.min, Xs = Math.max, Zs = Math.sin, Qs = Math.cos, $s = Math.PI * 2, ec = Me(), tc = Me(), nc = Me();
function rc(e, t, n) {
	if (e.length !== 0) {
		for (var r = e[0], i = r[0], a = r[0], o = r[1], s = r[1], c = 1; c < e.length; c++) r = e[c], i = Ys(i, r[0]), a = Xs(a, r[0]), o = Ys(o, r[1]), s = Xs(s, r[1]);
		t[0] = i, t[1] = o, n[0] = a, n[1] = s;
	}
}
function ic(e, t, n, r, i, a) {
	i[0] = Ys(e, n), i[1] = Ys(t, r), a[0] = Xs(e, n), a[1] = Xs(t, r);
}
var ac = [], oc = [];
function sc(e, t, n, r, i, a, o, s, c, l) {
	var u = Yn, d = Kn, f = u(e, n, i, o, ac);
	c[0] = Infinity, c[1] = Infinity, l[0] = -Infinity, l[1] = -Infinity;
	for (var p = 0; p < f; p++) {
		var m = d(e, n, i, o, ac[p]);
		c[0] = Ys(m, c[0]), l[0] = Xs(m, l[0]);
	}
	f = u(t, r, a, s, oc);
	for (var p = 0; p < f; p++) {
		var h = d(t, r, a, s, oc[p]);
		c[1] = Ys(h, c[1]), l[1] = Xs(h, l[1]);
	}
	c[0] = Ys(e, c[0]), l[0] = Xs(e, l[0]), c[0] = Ys(o, c[0]), l[0] = Xs(o, l[0]), c[1] = Ys(t, c[1]), l[1] = Xs(t, l[1]), c[1] = Ys(s, c[1]), l[1] = Xs(s, l[1]);
}
function cc(e, t, n, r, i, a, o, s) {
	var c = nr, l = $n, u = Xs(Ys(c(e, n, i), 1), 0), d = Xs(Ys(c(t, r, a), 1), 0), f = l(e, n, i, u), p = l(t, r, a, d);
	o[0] = Ys(e, i, f), o[1] = Ys(t, a, p), s[0] = Xs(e, i, f), s[1] = Xs(t, a, p);
}
function lc(e, t, n, r, i, a, o, s, c) {
	var l = Je, u = Ye, d = Math.abs(i - a);
	if (d % $s < 1e-4 && d > 1e-4) s[0] = e - n, s[1] = t - r, c[0] = e + n, c[1] = t + r;
	else {
		if (ec[0] = Qs(i) * n + e, ec[1] = Zs(i) * r + t, tc[0] = Qs(a) * n + e, tc[1] = Zs(a) * r + t, l(s, ec, tc), u(c, ec, tc), i %= $s, i < 0 && (i += $s), a %= $s, a < 0 && (a += $s), i > a && !o ? a += $s : i < a && o && (i += $s), o) {
			var f = a;
			a = i, i = f;
		}
		for (var p = 0; p < a; p += Math.PI / 2) p > i && (nc[0] = Qs(p) * n + e, nc[1] = Zs(p) * r + t, l(s, nc, s), u(c, nc, c));
	}
}
//#endregion
//#region node_modules/zrender/lib/core/PathProxy.js
var X = {
	M: 1,
	L: 2,
	C: 3,
	Q: 4,
	A: 5,
	Z: 6,
	R: 7
}, uc = [], dc = [], fc = [], pc = [], mc = [], hc = [], gc = Math.min, _c = Math.max, vc = Math.cos, yc = Math.sin, bc = Math.abs, xc = Math.PI, Sc = xc * 2, Cc = typeof Float32Array < "u", wc = [];
function Tc(e) {
	return Math.round(e / xc * 1e8) / 1e8 % 2 * xc;
}
function Ec(e, t) {
	var n = Tc(e[0]);
	n < 0 && (n += Sc);
	var r = n - e[0], i = e[1];
	i += r, !t && i - n >= Sc ? i = n + Sc : t && n - i >= Sc ? i = n - Sc : !t && n > i ? i = n + (Sc - Tc(n - i)) : t && n < i && (i = n - (Sc - Tc(i - n))), e[0] = n, e[1] = i;
}
var Dc = function() {
	function e(e) {
		this.dpr = 1, this._xi = 0, this._yi = 0, this._x0 = 0, this._y0 = 0, this._len = 0, e && (this._saveData = !1), this._saveData && (this.data = []);
	}
	return e.prototype.increaseVersion = function() {
		this._version++;
	}, e.prototype.getVersion = function() {
		return this._version;
	}, e.prototype.setScale = function(e, t, n) {
		n ||= 0, n > 0 && (this._ux = bc(n / Ei / e) || 0, this._uy = bc(n / Ei / t) || 0);
	}, e.prototype.setDPR = function(e) {
		this.dpr = e;
	}, e.prototype.setContext = function(e) {
		this._ctx = e;
	}, e.prototype.getContext = function() {
		return this._ctx;
	}, e.prototype.beginPath = function() {
		return this._ctx && this._ctx.beginPath(), this.reset(), this;
	}, e.prototype.reset = function() {
		this._saveData && (this._len = 0), this._pathSegLen && (this._pathSegLen = null, this._pathLen = 0), this._version++;
	}, e.prototype.moveTo = function(e, t) {
		return this._drawPendingPt(), this.addData(X.M, e, t), this._ctx && this._ctx.moveTo(e, t), this._x0 = e, this._y0 = t, this._xi = e, this._yi = t, this;
	}, e.prototype.lineTo = function(e, t) {
		var n = bc(e - this._xi), r = bc(t - this._yi), i = n > this._ux || r > this._uy;
		if (this.addData(X.L, e, t), this._ctx && i && this._ctx.lineTo(e, t), i) this._xi = e, this._yi = t, this._pendingPtDist = 0;
		else {
			var a = n * n + r * r;
			a > this._pendingPtDist && (this._pendingPtX = e, this._pendingPtY = t, this._pendingPtDist = a);
		}
		return this;
	}, e.prototype.bezierCurveTo = function(e, t, n, r, i, a) {
		return this._drawPendingPt(), this.addData(X.C, e, t, n, r, i, a), this._ctx && this._ctx.bezierCurveTo(e, t, n, r, i, a), this._xi = i, this._yi = a, this;
	}, e.prototype.quadraticCurveTo = function(e, t, n, r) {
		return this._drawPendingPt(), this.addData(X.Q, e, t, n, r), this._ctx && this._ctx.quadraticCurveTo(e, t, n, r), this._xi = n, this._yi = r, this;
	}, e.prototype.arc = function(e, t, n, r, i, a) {
		this._drawPendingPt(), wc[0] = r, wc[1] = i, Ec(wc, a), r = wc[0], i = wc[1];
		var o = i - r;
		return this.addData(X.A, e, t, n, n, r, o, 0, +!a), this._ctx && this._ctx.arc(e, t, n, r, i, a), this._xi = vc(i) * n + e, this._yi = yc(i) * n + t, this;
	}, e.prototype.arcTo = function(e, t, n, r, i) {
		return this._drawPendingPt(), this._ctx && this._ctx.arcTo(e, t, n, r, i), this;
	}, e.prototype.rect = function(e, t, n, r) {
		return this._drawPendingPt(), this._ctx && this._ctx.rect(e, t, n, r), this.addData(X.R, e, t, n, r), this;
	}, e.prototype.closePath = function() {
		this._drawPendingPt(), this.addData(X.Z);
		var e = this._ctx, t = this._x0, n = this._y0;
		return e && e.closePath(), this._xi = t, this._yi = n, this;
	}, e.prototype.fill = function(e) {
		e && e.fill(), this.toStatic();
	}, e.prototype.stroke = function(e) {
		e && e.stroke(), this.toStatic();
	}, e.prototype.len = function() {
		return this._len;
	}, e.prototype.setData = function(e) {
		if (this._saveData) {
			var t = e.length;
			!(this.data && this.data.length === t) && Cc && (this.data = new Float32Array(t));
			for (var n = 0; n < t; n++) this.data[n] = e[n];
			this._len = t;
		}
	}, e.prototype.appendPath = function(e) {
		if (this._saveData) {
			e instanceof Array || (e = [e]);
			for (var t = e.length, n = 0, r = this._len, i = 0; i < t; i++) n += e[i].len();
			var a = this.data;
			if (Cc && (a instanceof Float32Array || !a) && (this.data = new Float32Array(r + n), r > 0 && a)) for (var o = 0; o < r; o++) this.data[o] = a[o];
			for (var i = 0; i < t; i++) for (var s = e[i].data, o = 0; o < s.length; o++) this.data[r++] = s[o];
			this._len = r;
		}
	}, e.prototype.addData = function(e, t, n, r, i, a, o, s, c) {
		if (this._saveData) {
			var l = this.data;
			this._len + arguments.length > l.length && (this._expandData(), l = this.data);
			for (var u = 0; u < arguments.length; u++) l[this._len++] = arguments[u];
		}
	}, e.prototype._drawPendingPt = function() {
		this._pendingPtDist > 0 && (this._ctx && this._ctx.lineTo(this._pendingPtX, this._pendingPtY), this._pendingPtDist = 0);
	}, e.prototype._expandData = function() {
		if (!(this.data instanceof Array)) {
			for (var e = [], t = 0; t < this._len; t++) e[t] = this.data[t];
			this.data = e;
		}
	}, e.prototype.toStatic = function() {
		if (this._saveData) {
			this._drawPendingPt();
			var e = this.data;
			e instanceof Array && (e.length = this._len, Cc && this._len > 11 && (this.data = new Float32Array(e)));
		}
	}, e.prototype.getBoundingRect = function() {
		fc[0] = fc[1] = mc[0] = mc[1] = Number.MAX_VALUE, pc[0] = pc[1] = hc[0] = hc[1] = -Number.MAX_VALUE;
		for (var e = this.data, t = 0, n = 0, r = 0, i = 0, a = 0; a < this._len;) {
			var o = e[a++], s = a === 1;
			switch (s && (t = e[a], n = e[a + 1], r = t, i = n), o) {
				case X.M:
					t = r = e[a++], n = i = e[a++], mc[0] = r, mc[1] = i, hc[0] = r, hc[1] = i;
					break;
				case X.L:
					ic(t, n, e[a], e[a + 1], mc, hc), t = e[a++], n = e[a++];
					break;
				case X.C:
					sc(t, n, e[a++], e[a++], e[a++], e[a++], e[a], e[a + 1], mc, hc), t = e[a++], n = e[a++];
					break;
				case X.Q:
					cc(t, n, e[a++], e[a++], e[a], e[a + 1], mc, hc), t = e[a++], n = e[a++];
					break;
				case X.A:
					var c = e[a++], l = e[a++], u = e[a++], d = e[a++], f = e[a++], p = e[a++] + f;
					a += 1;
					var m = !e[a++];
					s && (r = vc(f) * u + c, i = yc(f) * d + l), lc(c, l, u, d, f, p, m, mc, hc), t = vc(p) * u + c, n = yc(p) * d + l;
					break;
				case X.R:
					r = t = e[a++], i = n = e[a++];
					var h = e[a++], g = e[a++];
					ic(r, i, r + h, i + g, mc, hc);
					break;
				case X.Z: t = r, n = i;
			}
			Je(fc, fc, mc), Ye(pc, pc, hc);
		}
		return a === 0 && (fc[0] = fc[1] = pc[0] = pc[1] = 0), new J(fc[0], fc[1], pc[0] - fc[0], pc[1] - fc[1]);
	}, e.prototype._calculateLength = function() {
		var e = this.data, t = this._len, n = this._ux, r = this._uy, i = 0, a = 0, o = 0, s = 0;
		this._pathSegLen ||= [];
		for (var c = this._pathSegLen, l = 0, u = 0, d = 0; d < t;) {
			var f = e[d++], p = d === 1;
			p && (i = e[d], a = e[d + 1], o = i, s = a);
			var m = -1;
			switch (f) {
				case X.M:
					i = o = e[d++], a = s = e[d++];
					break;
				case X.L:
					var h = e[d++], g = e[d++], _ = h - i, v = g - a;
					(bc(_) > n || bc(v) > r || d === t - 1) && (m = Math.sqrt(_ * _ + v * v), i = h, a = g);
					break;
				case X.C:
					var y = e[d++], b = e[d++], h = e[d++], g = e[d++], x = e[d++], S = e[d++];
					m = Qn(i, a, y, b, h, g, x, S, 10), i = x, a = S;
					break;
				case X.Q:
					var y = e[d++], b = e[d++], h = e[d++], g = e[d++];
					m = ar(i, a, y, b, h, g, 10), i = h, a = g;
					break;
				case X.A:
					var C = e[d++], w = e[d++], T = e[d++], E = e[d++], D = e[d++], O = e[d++], k = O + D;
					d += 1, p && (o = vc(D) * T + C, s = yc(D) * E + w), m = _c(T, E) * gc(Sc, Math.abs(O)), i = vc(k) * T + C, a = yc(k) * E + w;
					break;
				case X.R:
					o = i = e[d++], s = a = e[d++];
					var A = e[d++], j = e[d++];
					m = A * 2 + j * 2;
					break;
				case X.Z:
					var _ = o - i, v = s - a;
					m = Math.sqrt(_ * _ + v * v), i = o, a = s;
			}
			m >= 0 && (c[u++] = m, l += m);
		}
		return this._pathLen = l, l;
	}, e.prototype.rebuildPath = function(e, t) {
		var n = this.data, r = this._ux, i = this._uy, a = this._len, o, s, c, l, u, d, f = t < 1, p, m, h = 0, g = 0, _, v = 0, y, b;
		if (!(f && (this._pathSegLen || this._calculateLength(), p = this._pathSegLen, m = this._pathLen, _ = t * m, !_))) lo: for (var x = 0; x < a;) {
			var S = n[x++], C = x === 1;
			switch (C && (c = n[x], l = n[x + 1], o = c, s = l), S !== X.L && v > 0 && (e.lineTo(y, b), v = 0), S) {
				case X.M:
					o = c = n[x++], s = l = n[x++], e.moveTo(c, l);
					break;
				case X.L:
					u = n[x++], d = n[x++];
					var w = bc(u - c), T = bc(d - l);
					if (w > r || T > i) {
						if (f) {
							var E = p[g++];
							if (h + E > _) {
								var D = (_ - h) / E;
								e.lineTo(c * (1 - D) + u * D, l * (1 - D) + d * D);
								break lo;
							}
							h += E;
						}
						e.lineTo(u, d), c = u, l = d, v = 0;
					} else {
						var O = w * w + T * T;
						O > v && (y = u, b = d, v = O);
					}
					break;
				case X.C:
					var k = n[x++], A = n[x++], j = n[x++], ee = n[x++], M = n[x++], N = n[x++];
					if (f) {
						var E = p[g++];
						if (h + E > _) {
							var D = (_ - h) / E;
							Xn(c, k, j, M, D, uc), Xn(l, A, ee, N, D, dc), e.bezierCurveTo(uc[1], dc[1], uc[2], dc[2], uc[3], dc[3]);
							break lo;
						}
						h += E;
					}
					e.bezierCurveTo(k, A, j, ee, M, N), c = M, l = N;
					break;
				case X.Q:
					var k = n[x++], A = n[x++], j = n[x++], ee = n[x++];
					if (f) {
						var E = p[g++];
						if (h + E > _) {
							var D = (_ - h) / E;
							rr(c, k, j, D, uc), rr(l, A, ee, D, dc), e.quadraticCurveTo(uc[1], dc[1], uc[2], dc[2]);
							break lo;
						}
						h += E;
					}
					e.quadraticCurveTo(k, A, j, ee), c = j, l = ee;
					break;
				case X.A:
					var te = n[x++], ne = n[x++], P = n[x++], F = n[x++], I = n[x++], re = n[x++], ie = n[x++], L = !n[x++], ae = P > F ? P : F, R = bc(P - F) > .001, z = I + re, B = !1;
					if (f) {
						var E = p[g++];
						h + E > _ && (z = I + re * (_ - h) / E, B = !0), h += E;
					}
					if (R && e.ellipse ? e.ellipse(te, ne, P, F, ie, I, z, L) : e.arc(te, ne, ae, I, z, L), B) break lo;
					C && (o = vc(I) * P + te, s = yc(I) * F + ne), c = vc(z) * P + te, l = yc(z) * F + ne;
					break;
				case X.R:
					o = c = n[x], s = l = n[x + 1], u = n[x++], d = n[x++];
					var V = n[x++], H = n[x++];
					if (f) {
						var E = p[g++];
						if (h + E > _) {
							var oe = _ - h;
							e.moveTo(u, d), e.lineTo(u + gc(oe, V), d), oe -= V, oe > 0 && e.lineTo(u + V, d + gc(oe, H)), oe -= H, oe > 0 && e.lineTo(u + _c(V - oe, 0), d + H), oe -= V, oe > 0 && e.lineTo(u, d + _c(H - oe, 0));
							break lo;
						}
						h += E;
					}
					e.rect(u, d, V, H);
					break;
				case X.Z:
					if (f) {
						var E = p[g++];
						if (h + E > _) {
							var D = (_ - h) / E;
							e.lineTo(c * (1 - D) + o * D, l * (1 - D) + s * D);
							break lo;
						}
						h += E;
					}
					e.closePath(), c = o, l = s;
			}
		}
	}, e.prototype.clone = function() {
		var t = new e(), n = this.data;
		return t.data = n.slice ? n.slice() : Array.prototype.slice.call(n), t._len = this._len, t;
	}, e.prototype.canSave = function() {
		return !!this._saveData;
	}, e.CMD = X, e.initDefaultProps = (function() {
		var t = e.prototype;
		t._saveData = !0, t._ux = 0, t._uy = 0, t._pendingPtDist = 0, t._version = 0;
	})(), e;
}();
//#endregion
//#region node_modules/zrender/lib/contain/line.js
function Oc(e, t, n, r, i, a, o) {
	if (i === 0) return !1;
	var s = i, c = 0, l = e;
	if (o > t + s && o > r + s || o < t - s && o < r - s || a > e + s && a > n + s || a < e - s && a < n - s) return !1;
	if (e !== n) c = (t - r) / (e - n), l = (e * r - n * t) / (e - n);
	else return Math.abs(a - e) <= s / 2;
	var u = c * a - o + l;
	return u * u / (c * c + 1) <= s / 2 * s / 2;
}
//#endregion
//#region node_modules/zrender/lib/contain/cubic.js
function kc(e, t, n, r, i, a, o, s, c, l, u) {
	if (c === 0) return !1;
	var d = c;
	return u > t + d && u > r + d && u > a + d && u > s + d || u < t - d && u < r - d && u < a - d && u < s - d || l > e + d && l > n + d && l > i + d && l > o + d || l < e - d && l < n - d && l < i - d && l < o - d ? !1 : Zn(e, t, n, r, i, a, o, s, l, u, null) <= d / 2;
}
//#endregion
//#region node_modules/zrender/lib/contain/quadratic.js
function Ac(e, t, n, r, i, a, o, s, c) {
	if (o === 0) return !1;
	var l = o;
	return c > t + l && c > r + l && c > a + l || c < t - l && c < r - l && c < a - l || s > e + l && s > n + l && s > i + l || s < e - l && s < n - l && s < i - l ? !1 : ir(e, t, n, r, i, a, s, c, null) <= l / 2;
}
//#endregion
//#region node_modules/zrender/lib/contain/util.js
var jc = Math.PI * 2;
function Mc(e) {
	return e %= jc, e < 0 && (e += jc), e;
}
//#endregion
//#region node_modules/zrender/lib/contain/arc.js
var Nc = Math.PI * 2;
function Pc(e, t, n, r, i, a, o, s, c) {
	if (o === 0) return !1;
	var l = o;
	s -= e, c -= t;
	var u = Math.sqrt(s * s + c * c);
	if (u - l > n || u + l < n) return !1;
	if (Math.abs(r - i) % Nc < 1e-4) return !0;
	if (a) {
		var d = r;
		r = Mc(i), i = Mc(d);
	} else r = Mc(r), i = Mc(i);
	r > i && (i += Nc);
	var f = Math.atan2(c, s);
	return f < 0 && (f += Nc), f >= r && f <= i || f + Nc >= r && f + Nc <= i;
}
//#endregion
//#region node_modules/zrender/lib/contain/windingLine.js
function Fc(e, t, n, r, i, a) {
	if (a > t && a > r || a < t && a < r || r === t) return 0;
	var o = (a - t) / (r - t), s = r < t ? 1 : -1;
	(o === 1 || o === 0) && (s = r < t ? .5 : -.5);
	var c = o * (n - e) + e;
	return c === i ? Infinity : c > i ? s : 0;
}
//#endregion
//#region node_modules/zrender/lib/contain/path.js
var Ic = Dc.CMD, Lc = Math.PI * 2, Rc = 1e-4;
function zc(e, t) {
	return Math.abs(e - t) < Rc;
}
var Bc = [
	-1,
	-1,
	-1
], Vc = [-1, -1];
function Hc() {
	var e = Vc[0];
	Vc[0] = Vc[1], Vc[1] = e;
}
function Uc(e, t, n, r, i, a, o, s, c, l) {
	if (l > t && l > r && l > a && l > s || l < t && l < r && l < a && l < s) return 0;
	var u = Jn(t, r, a, s, l, Bc);
	if (u === 0) return 0;
	for (var d = 0, f = -1, p = void 0, m = void 0, h = 0; h < u; h++) {
		var g = Bc[h], _ = g === 0 || g === 1 ? .5 : 1;
		Kn(e, n, i, o, g) < c || (f < 0 && (f = Yn(t, r, a, s, Vc), Vc[1] < Vc[0] && f > 1 && Hc(), p = Kn(t, r, a, s, Vc[0]), f > 1 && (m = Kn(t, r, a, s, Vc[1]))), f === 2 ? g < Vc[0] ? d += p < t ? _ : -_ : g < Vc[1] ? d += m < p ? _ : -_ : d += s < m ? _ : -_ : g < Vc[0] ? d += p < t ? _ : -_ : d += s < p ? _ : -_);
	}
	return d;
}
function Wc(e, t, n, r, i, a, o, s) {
	if (s > t && s > r && s > a || s < t && s < r && s < a) return 0;
	var c = tr(t, r, a, s, Bc);
	if (c === 0) return 0;
	var l = nr(t, r, a);
	if (l >= 0 && l <= 1) {
		for (var u = 0, d = $n(t, r, a, l), f = 0; f < c; f++) {
			var p = Bc[f] === 0 || Bc[f] === 1 ? .5 : 1, m = $n(e, n, i, Bc[f]);
			m < o || (Bc[f] < l ? u += d < t ? p : -p : u += a < d ? p : -p);
		}
		return u;
	}
	var p = Bc[0] === 0 || Bc[0] === 1 ? .5 : 1, m = $n(e, n, i, Bc[0]);
	return m < o ? 0 : a < t ? p : -p;
}
function Gc(e, t, n, r, i, a, o, s) {
	if (s -= t, s > n || s < -n) return 0;
	var c = Math.sqrt(n * n - s * s);
	Bc[0] = -c, Bc[1] = c;
	var l = Math.abs(r - i);
	if (l < 1e-4) return 0;
	if (l >= Lc - 1e-4) {
		r = 0, i = Lc;
		var u = a ? 1 : -1;
		return o >= Bc[0] + e && o <= Bc[1] + e ? u : 0;
	}
	if (r > i) {
		var d = r;
		r = i, i = d;
	}
	r < 0 && (r += Lc, i += Lc);
	for (var f = 0, p = 0; p < 2; p++) {
		var m = Bc[p];
		if (m + e > o) {
			var h = Math.atan2(s, m), u = a ? 1 : -1;
			h < 0 && (h = Lc + h), (h >= r && h <= i || h + Lc >= r && h + Lc <= i) && (h > Math.PI / 2 && h < Math.PI * 1.5 && (u = -u), f += u);
		}
	}
	return f;
}
function Kc(e, t, n, r, i) {
	for (var a = e.data, o = e.len(), s = 0, c = 0, l = 0, u = 0, d = 0, f, p, m = 0; m < o;) {
		var h = a[m++], g = m === 1;
		switch (h === Ic.M && m > 1 && (n || (s += Fc(c, l, u, d, r, i))), g && (c = a[m], l = a[m + 1], u = c, d = l), h) {
			case Ic.M:
				u = a[m++], d = a[m++], c = u, l = d;
				break;
			case Ic.L:
				if (n) {
					if (Oc(c, l, a[m], a[m + 1], t, r, i)) return !0;
				} else s += Fc(c, l, a[m], a[m + 1], r, i) || 0;
				c = a[m++], l = a[m++];
				break;
			case Ic.C:
				if (n) {
					if (kc(c, l, a[m++], a[m++], a[m++], a[m++], a[m], a[m + 1], t, r, i)) return !0;
				} else s += Uc(c, l, a[m++], a[m++], a[m++], a[m++], a[m], a[m + 1], r, i) || 0;
				c = a[m++], l = a[m++];
				break;
			case Ic.Q:
				if (n) {
					if (Ac(c, l, a[m++], a[m++], a[m], a[m + 1], t, r, i)) return !0;
				} else s += Wc(c, l, a[m++], a[m++], a[m], a[m + 1], r, i) || 0;
				c = a[m++], l = a[m++];
				break;
			case Ic.A:
				var _ = a[m++], v = a[m++], y = a[m++], b = a[m++], x = a[m++], S = a[m++];
				m += 1;
				var C = !!(1 - a[m++]);
				f = Math.cos(x) * y + _, p = Math.sin(x) * b + v, g ? (u = f, d = p) : s += Fc(c, l, f, p, r, i);
				var w = (r - _) * b / y + _;
				if (n) {
					if (Pc(_, v, b, x, x + S, C, t, w, i)) return !0;
				} else s += Gc(_, v, b, x, x + S, C, w, i);
				c = Math.cos(x + S) * y + _, l = Math.sin(x + S) * b + v;
				break;
			case Ic.R:
				u = c = a[m++], d = l = a[m++];
				var T = a[m++], E = a[m++];
				if (f = u + T, p = d + E, n) {
					if (Oc(u, d, f, d, t, r, i) || Oc(f, d, f, p, t, r, i) || Oc(f, p, u, p, t, r, i) || Oc(u, p, u, d, t, r, i)) return !0;
				} else s += Fc(f, d, f, p, r, i), s += Fc(u, p, u, d, r, i);
				break;
			case Ic.Z:
				if (n) {
					if (Oc(c, l, u, d, t, r, i)) return !0;
				} else s += Fc(c, l, u, d, r, i);
				c = u, l = d;
		}
	}
	return !n && !zc(l, d) && (s += Fc(c, l, u, d, r, i) || 0), s !== 0;
}
function qc(e, t, n) {
	return Kc(e, 0, !1, t, n);
}
function Jc(e, t, n, r) {
	return Kc(e, t, !0, n, r);
}
//#endregion
//#region node_modules/zrender/lib/graphic/Path.js
var Yc = M({
	fill: "#000",
	stroke: null,
	strokePercent: 1,
	fillOpacity: 1,
	strokeOpacity: 1,
	lineDashOffset: 0,
	lineWidth: 1,
	lineCap: "butt",
	miterLimit: 10,
	strokeNoScale: !1,
	strokeFirst: !1
}, Vs), Xc = { style: M({
	fill: !0,
	stroke: !0,
	strokePercent: !0,
	fillOpacity: !0,
	strokeOpacity: !0,
	lineDashOffset: !0,
	lineWidth: !0,
	miterLimit: !0
}, Hs.style) }, Zc = Vi.concat([
	"invisible",
	"culling",
	"z",
	"z2",
	"zlevel",
	"parent"
]), Z = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.update = function() {
		var n = this;
		e.prototype.update.call(this);
		var r = this.style;
		if (r.decal) {
			var i = this._decalEl = this._decalEl || new t();
			i.buildPath === t.prototype.buildPath && (i.buildPath = function(e) {
				n.buildPath(e, n.shape);
			}), i.silent = !0;
			var a = i.style;
			for (var o in r) a[o] !== r[o] && (a[o] = r[o]);
			a.fill = r.fill ? r.decal : null, a.decal = null, a.shadowColor = null, r.strokeFirst && (a.stroke = null);
			for (var s = 0; s < Zc.length; ++s) i[Zc[s]] = this[Zc[s]];
			i.__dirty |= 1;
		} else this._decalEl &&= null;
	}, t.prototype.getDecalElement = function() {
		return this._decalEl;
	}, t.prototype._init = function(t) {
		var n = L(t);
		this.shape = this.getDefaultShape();
		var r = this.getDefaultStyle();
		r && this.useStyle(r);
		for (var i = 0; i < n.length; i++) {
			var a = n[i], o = t[a];
			a === "style" ? this.style ? j(this.style, o) : this.useStyle(o) : a === "shape" ? j(this.shape, o) : e.prototype.attrKV.call(this, a, o);
		}
		this.style || this.useStyle({});
	}, t.prototype.getDefaultStyle = function() {
		return null;
	}, t.prototype.getDefaultShape = function() {
		return {};
	}, t.prototype.canBeInsideText = function() {
		return this.hasFill();
	}, t.prototype.getInsideTextFill = function() {
		var e = this.style.fill;
		if (e !== "none") {
			if (H(e)) {
				var t = Ar(e, 0);
				return t > .5 ? Oi : t > .2 ? Ai : ki;
			}
			if (e) return ki;
		}
		return Oi;
	}, t.prototype.getInsideTextStroke = function(e) {
		var t = this.style.fill;
		if (H(t)) {
			var n = this.__zr;
			if (!!(n && n.isDarkMode()) == Ar(e, 0) < .4) return t;
		}
	}, t.prototype.buildPath = function(e, t, n) {}, t.prototype.pathUpdated = function() {
		this.__dirty &= -5;
	}, t.prototype.getUpdatedPathProxy = function(e) {
		return !this.path && this.createPathProxy(), this.path.beginPath(), this.buildPath(this.path, this.shape, e), this.path;
	}, t.prototype.createPathProxy = function() {
		this.path = new Dc(!1);
	}, t.prototype.hasStroke = function() {
		var e = this.style, t = e.stroke;
		return !(t == null || t === "none" || !(e.lineWidth > 0));
	}, t.prototype.hasFill = function() {
		var e = this.style.fill;
		return e != null && e !== "none";
	}, t.prototype.getBoundingRect = function() {
		var e = this._rect, t = this.style, n = !e;
		if (n) {
			var r = !1;
			this.path || (r = !0, this.createPathProxy());
			var i = this.path;
			(r || this.__dirty & 4) && (i.beginPath(), this.buildPath(i, this.shape, !1), this.pathUpdated()), e = i.getBoundingRect();
		}
		if (this._rect = e, this.hasStroke() && this.path && this.path.len() > 0) {
			var a = this._rectStroke ||= e.clone();
			if (this.__dirty || n) {
				a.copy(e);
				var o = t.strokeNoScale ? this.getLineScale() : 1, s = t.lineWidth;
				if (!this.hasFill()) {
					var c = this.strokeContainThreshold;
					s = Math.max(s, c ?? 4);
				}
				o > 1e-10 && (a.width += s / o, a.height += s / o, a.x -= s / o / 2, a.y -= s / o / 2);
			}
			return a;
		}
		return e;
	}, t.prototype.contain = function(e, t) {
		var n = this.transformCoordToLocal(e, t), r = this.getBoundingRect(), i = this.style;
		if (e = n[0], t = n[1], r.contain(e, t)) {
			var a = this.path;
			if (this.hasStroke()) {
				var o = i.lineWidth, s = i.strokeNoScale ? this.getLineScale() : 1;
				if (s > 1e-10 && (this.hasFill() || (o = Math.max(o, this.strokeContainThreshold)), Jc(a, o / s, e, t))) return !0;
			}
			if (this.hasFill()) return qc(a, e, t);
		}
		return !1;
	}, t.prototype.dirtyShape = function() {
		this.__dirty |= 4, this._rect &&= null, this._decalEl && this._decalEl.dirtyShape(), this.markRedraw();
	}, t.prototype.dirty = function() {
		this.dirtyStyle(), this.dirtyShape();
	}, t.prototype.animateShape = function(e) {
		return this.animate("shape", e);
	}, t.prototype.updateDuringAnimation = function(e) {
		e === "style" ? this.dirtyStyle() : e === "shape" ? this.dirtyShape() : this.markRedraw();
	}, t.prototype.attrKV = function(t, n) {
		t === "shape" ? this.setShape(n) : e.prototype.attrKV.call(this, t, n);
	}, t.prototype.setShape = function(e, t) {
		var n = this.shape;
		return n ||= this.shape = {}, typeof e == "string" ? n[e] = t : j(n, e), this.dirtyShape(), this;
	}, t.prototype.shapeChanged = function() {
		return !!(this.__dirty & 4);
	}, t.prototype.createStyle = function(e) {
		return De(Yc, e);
	}, t.prototype._innerSaveToNormal = function(t) {
		e.prototype._innerSaveToNormal.call(this, t);
		var n = this._normalState;
		t.shape && !n.shape && (n.shape = j({}, this.shape));
	}, t.prototype._applyStateObj = function(t, n, r, i, a, o) {
		if (e.prototype._applyStateObj.call(this, t, n, r, i, a, o), this.__inHover !== 1) {
			var s = !(n && i), c;
			if (n && n.shape ? a ? i ? c = n.shape : (c = j({}, r.shape), j(c, n.shape)) : (c = j({}, i ? this.shape : r.shape), j(c, n.shape)) : s && (c = r.shape), c) {
				if (a) {
					this.shape = j({}, this.shape);
					for (var l = {}, u = L(c), d = 0; d < u.length; d++) {
						var f = u[d];
						typeof c[f] == "object" ? this.shape[f] = c[f] : l[f] = c[f];
					}
					this._transitionState(t, { shape: l }, o);
				} else this.shape = c, this.dirtyShape();
			}
		}
	}, t.prototype._mergeStates = function(t) {
		for (var n = e.prototype._mergeStates.call(this, t), r, i = 0; i < t.length; i++) {
			var a = t[i];
			a.shape && (r ||= {}, this._mergeStyle(r, a.shape));
		}
		return r && (n.shape = r), n;
	}, t.prototype.getAnimationStyleProps = function() {
		return Xc;
	}, t.prototype.isZeroArea = function() {
		return !1;
	}, t.extend = function(e) {
		var n = function(t) {
			r(n, t);
			function n(n) {
				var r = t.call(this, n) || this;
				return e.init && e.init.call(r, n), r;
			}
			return n.prototype.getDefaultStyle = function() {
				return k(e.style);
			}, n.prototype.getDefaultShape = function() {
				return k(e.shape);
			}, n;
		}(t);
		for (var i in e) typeof e[i] == "function" && (n.prototype[i] = e[i]);
		return n;
	}, t.initDefaultProps = (function() {
		var e = t.prototype;
		e.type = "path", e.strokeContainThreshold = 5, e.segmentIgnoreThreshold = 0, e.subPixelOptimize = !1, e.autoBatch = !1, e.__dirty = 7;
	})(), t;
}(Gs), Qc = M({
	strokeFirst: !0,
	font: s,
	x: 0,
	y: 0,
	textAlign: "left",
	textBaseline: "top",
	miterLimit: 2
}, Yc), $c = function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t.prototype.hasStroke = function() {
		return zs(this.style);
	}, t.prototype.hasFill = function() {
		var e = this.style.fill;
		return e != null && e !== "none";
	}, t.prototype.createStyle = function(e) {
		return De(Qc, e);
	}, t.prototype.setBoundingRect = function(e) {
		this._rect = e;
	}, t.prototype.getBoundingRect = function() {
		return this._rect ||= Ls(this.style), this._rect;
	}, t.initDefaultProps = (function() {
		var e = t.prototype;
		e.dirtyRectTolerance = 10;
	})(), t;
}(Gs);
$c.prototype.type = "tspan";
//#endregion
//#region node_modules/zrender/lib/graphic/Image.js
var el = M({
	x: 0,
	y: 0
}, Vs), tl = { style: M({
	x: !0,
	y: !0,
	width: !0,
	height: !0,
	sx: !0,
	sy: !0,
	sWidth: !0,
	sHeight: !0
}, Hs.style) };
function nl(e) {
	return !!(e && typeof e != "string" && e.width && e.height);
}
var rl = function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t.prototype.createStyle = function(e) {
		return De(el, e);
	}, t.prototype._getSize = function(e) {
		var t = this.style, n = t[e];
		if (n != null) return n;
		var r = nl(t.image) ? t.image : this.__image;
		if (!r) return 0;
		var i = e === "width" ? "height" : "width", a = t[i];
		return a == null ? r[e] : r[e] / r[i] * a;
	}, t.prototype.getWidth = function() {
		return this._getSize("width");
	}, t.prototype.getHeight = function() {
		return this._getSize("height");
	}, t.prototype.getAnimationStyleProps = function() {
		return tl;
	}, t.prototype.getBoundingRect = function() {
		var e = this.style;
		return this._rect ||= new J(e.x || 0, e.y || 0, this.getWidth(), this.getHeight()), this._rect;
	}, t;
}(Gs);
rl.prototype.type = "image";
//#endregion
//#region node_modules/zrender/lib/graphic/helper/roundRect.js
function il(e, t) {
	var n = t.x, r = t.y, i = t.width, a = t.height, o = t.r, s, c, l, u;
	i < 0 && (n += i, i = -i), a < 0 && (r += a, a = -a), typeof o == "number" ? s = c = l = u = o : o instanceof Array ? o.length === 1 ? s = c = l = u = o[0] : o.length === 2 ? (s = l = o[0], c = u = o[1]) : o.length === 3 ? (s = o[0], c = u = o[1], l = o[2]) : (s = o[0], c = o[1], l = o[2], u = o[3]) : s = c = l = u = 0;
	var d;
	s + c > i && (d = s + c, s *= i / d, c *= i / d), l + u > i && (d = l + u, l *= i / d, u *= i / d), c + l > a && (d = c + l, c *= a / d, l *= a / d), s + u > a && (d = s + u, s *= a / d, u *= a / d), e.moveTo(n + s, r), e.lineTo(n + i - c, r), c !== 0 && e.arc(n + i - c, r + c, c, -Math.PI / 2, 0), e.lineTo(n + i, r + a - l), l !== 0 && e.arc(n + i - l, r + a - l, l, 0, Math.PI / 2), e.lineTo(n + u, r + a), u !== 0 && e.arc(n + u, r + a - u, u, Math.PI / 2, Math.PI), e.lineTo(n, r + s), s !== 0 && e.arc(n + s, r + s, s, Math.PI, Math.PI * 1.5), e.closePath();
}
//#endregion
//#region node_modules/zrender/lib/graphic/helper/subPixelOptimize.js
var al = Math.round;
function ol(e, t, n) {
	if (t) {
		var r = t.x1, i = t.x2, a = t.y1, o = t.y2;
		e.x1 = r, e.x2 = i, e.y1 = a, e.y2 = o;
		var s = n && n.lineWidth;
		return s && (al(r * 2) === al(i * 2) && (e.x1 = e.x2 = cl(r, s, !0)), al(a * 2) === al(o * 2) && (e.y1 = e.y2 = cl(a, s, !0))), e;
	}
}
function sl(e, t, n) {
	if (t) {
		var r = t.x, i = t.y, a = t.width, o = t.height;
		e.x = r, e.y = i, e.width = a, e.height = o;
		var s = n && n.lineWidth;
		return s && (e.x = cl(r, s, !0), e.y = cl(i, s, !0), e.width = Math.max(cl(r + a, s, !1) - e.x, a === 0 ? 0 : 1), e.height = Math.max(cl(i + o, s, !1) - e.y, o === 0 ? 0 : 1)), e;
	}
}
function cl(e, t, n) {
	if (!t) return e;
	var r = al(e * 2);
	return (r + al(t)) % 2 == 0 ? r / 2 : (r + (n ? 1 : -1)) / 2;
}
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Rect.js
var ll = function() {
	function e() {
		this.x = 0, this.y = 0, this.width = 0, this.height = 0;
	}
	return e;
}(), ul = {}, dl = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultShape = function() {
		return new ll();
	}, t.prototype.buildPath = function(e, t) {
		var n, r, i, a;
		if (this.subPixelOptimize) {
			var o = sl(ul, t, this.style);
			n = o.x, r = o.y, i = o.width, a = o.height, o.r = t.r, t = o;
		} else n = t.x, r = t.y, i = t.width, a = t.height;
		t.r ? il(e, t) : e.rect(n, r, i, a);
	}, t.prototype.isZeroArea = function() {
		return !this.shape.width || !this.shape.height;
	}, t;
}(Z);
dl.prototype.type = "rect";
//#endregion
//#region node_modules/zrender/lib/graphic/Text.js
var fl = { fill: "#000" }, pl = 2, ml = {}, hl = { style: M({
	fill: !0,
	stroke: !0,
	fillOpacity: !0,
	strokeOpacity: !0,
	lineWidth: !0,
	fontSize: !0,
	lineHeight: !0,
	width: !0,
	height: !0,
	textShadowColor: !0,
	textShadowBlur: !0,
	textShadowOffsetX: !0,
	textShadowOffsetY: !0,
	backgroundColor: !0,
	padding: !0,
	borderColor: !0,
	borderWidth: !0,
	borderRadius: !0
}, Hs.style) }, gl = function(e) {
	r(t, e);
	function t(t) {
		var n = e.call(this) || this;
		return n.type = "text", n._children = [], n._defaultStyle = fl, n.attr(t), n;
	}
	return t.prototype.childrenRef = function() {
		return this._children;
	}, t.prototype.update = function() {
		e.prototype.update.call(this), this.styleChanged() && this._updateSubTexts();
		for (var t = 0; t < this._children.length; t++) {
			var n = this._children[t];
			n.zlevel = this.zlevel, n.z = this.z, n.z2 = this.z2, n.culling = this.culling, n.cursor = this.cursor, n.invisible = this.invisible;
		}
	}, t.prototype.updateTransform = function() {
		var t = this.innerTransformable;
		t ? (t.updateTransform(), t.transform && (this.transform = t.transform)) : e.prototype.updateTransform.call(this);
	}, t.prototype.getLocalTransform = function(t) {
		var n = this.innerTransformable;
		return n ? n.getLocalTransform(t) : e.prototype.getLocalTransform.call(this, t);
	}, t.prototype.getComputedTransform = function() {
		return this.__hostTarget && (this.__hostTarget.getComputedTransform(), this.__hostTarget.updateInnerText(!0)), e.prototype.getComputedTransform.call(this);
	}, t.prototype._updateSubTexts = function() {
		this._childCursor = 0, Cl(this.style), this.style.rich ? this._updateRichTexts() : this._updatePlainTexts(), this._children.length = this._childCursor, this.styleUpdated();
	}, t.prototype.addSelfToZr = function(t) {
		e.prototype.addSelfToZr.call(this, t);
		for (var n = 0; n < this._children.length; n++) this._children[n].__zr = t;
	}, t.prototype.removeSelfFromZr = function(t) {
		e.prototype.removeSelfFromZr.call(this, t);
		for (var n = 0; n < this._children.length; n++) this._children[n].__zr = null;
	}, t.prototype.getBoundingRect = function() {
		if (this.styleChanged() && this._updateSubTexts(), !this._rect) {
			for (var e = new J(0, 0, 0, 0), t = this._children, n = [], r = null, i = 0; i < t.length; i++) {
				var a = t[i], o = a.getBoundingRect(), s = a.getLocalTransform(n);
				s ? (e.copy(o), e.applyTransform(s), r ||= e.clone(), r.union(e)) : (r ||= o.clone(), r.union(o));
			}
			this._rect = r || e;
		}
		return this._rect;
	}, t.prototype.setDefaultTextStyle = function(e) {
		this._defaultStyle = e || fl;
	}, t.prototype.setTextContent = function(e) {}, t.prototype._mergeStyle = function(e, t) {
		if (!t) return e;
		var n = t.rich, r = e.rich || n && {};
		return j(e, t), n && r ? (this._mergeRich(r, n), e.rich = r) : r && (e.rich = r), e;
	}, t.prototype._mergeRich = function(e, t) {
		for (var n = L(t), r = 0; r < n.length; r++) {
			var i = n[r];
			e[i] = e[i] || {}, j(e[i], t[i]);
		}
	}, t.prototype.getAnimationStyleProps = function() {
		return hl;
	}, t.prototype._getOrCreateChild = function(e) {
		var t = this._children[this._childCursor];
		return (!t || !(t instanceof e)) && (t = new e()), this._children[this._childCursor++] = t, t.__zr = this.__zr, t.parent = this, t;
	}, t.prototype._updatePlainTexts = function() {
		var e = this.style, t = e.font || "12px sans-serif", n = e.padding, r = this._defaultStyle, i = e.x || 0, a = e.y || 0, o = e.align || r.align || "left", s = e.verticalAlign || r.verticalAlign || "top";
		Ns(ml, r.overflowRect, i, a, o, s), i = ml.baseX, a = ml.baseY;
		var c = Cs(Ol(e), e, ml.outerWidth, ml.outerHeight), l = kl(e), u = !!e.backgroundColor, d = c.outerHeight, f = c.outerWidth, p = c.lines, m = c.lineHeight;
		this.isTruncated = !!c.isTruncated;
		var h = i, g = $i(a, c.contentHeight, s);
		if (l || n) {
			var _ = Qi(i, f, o), v = $i(a, d, s);
			l && this._renderBackground(e, e, _, v, f, d);
		}
		g += m / 2, n && (h = Dl(i, o, n), s === "top" ? g += n[0] : s === "bottom" && (g -= n[2]));
		for (var y = 0, b = !1, x = !1, S = El("fill" in e ? e.fill : (x = !0, r.fill)), C = Tl("stroke" in e ? e.stroke : !u && (!r.autoStroke || x) ? (y = pl, b = !0, r.stroke) : null), w = e.textShadowBlur > 0, T = 0; T < p.length; T++) {
			var E = this._getOrCreateChild($c), D = E.createStyle();
			E.useStyle(D), D.text = p[T], D.x = h, D.y = g, o && (D.textAlign = o), D.textBaseline = "middle", D.opacity = e.opacity, D.strokeFirst = !0, w && (D.shadowBlur = e.textShadowBlur || 0, D.shadowColor = e.textShadowColor || "transparent", D.shadowOffsetX = e.textShadowOffsetX || 0, D.shadowOffsetY = e.textShadowOffsetY || 0), D.stroke = C, D.fill = S, C && (D.lineWidth = e.lineWidth || y, D.lineDash = e.lineDash, D.lineDashOffset = e.lineDashOffset || 0), D.font = t, xl(D, e), g += m, E.setBoundingRect(Rs(D, c.contentWidth, c.calculatedLineHeight, b ? 0 : null));
		}
	}, t.prototype._updateRichTexts = function() {
		var e = this.style, t = this._defaultStyle, n = e.align || t.align, r = e.verticalAlign || t.verticalAlign, i = e.x || 0, a = e.y || 0;
		Ns(ml, t.overflowRect, i, a, n, r), i = ml.baseX, a = ml.baseY;
		var o = Ds(Ol(e), e, ml.outerWidth, ml.outerHeight, n), s = o.width, c = o.outerWidth, l = o.outerHeight, u = e.padding;
		this.isTruncated = !!o.isTruncated;
		var d = Qi(i, c, n), f = $i(a, l, r), p = d, m = f;
		u && (p += u[3], m += u[0]);
		var h = p + s;
		kl(e) && this._renderBackground(e, e, d, f, c, l);
		for (var g = !!e.backgroundColor, _ = 0; _ < o.lines.length; _++) {
			for (var v = o.lines[_], y = v.tokens, b = y.length, x = v.lineHeight, S = v.width, C = 0, w = p, T = h, E = b - 1, D = void 0; C < b && (D = y[C], !D.align || D.align === "left");) this._placeToken(D, e, x, m, w, "left", g), S -= D.width, w += D.width, C++;
			for (; E >= 0 && (D = y[E], D.align === "right");) this._placeToken(D, e, x, m, T, "right", g), S -= D.width, T -= D.width, E--;
			for (w += (s - (w - p) - (h - T) - S) / 2; C <= E;) D = y[C], this._placeToken(D, e, x, m, w + D.width / 2, "center", g), w += D.width, C++;
			m += x;
		}
	}, t.prototype._placeToken = function(e, t, n, r, i, a, o) {
		var s = t.rich[e.styleName] || {};
		s.text = e.text;
		var c = e.verticalAlign, l = r + n / 2;
		c === "top" ? l = r + e.height / 2 : c === "bottom" && (l = r + n - e.height / 2), !e.isLineHolder && kl(s) && this._renderBackground(s, t, a === "right" ? i - e.width : a === "center" ? i - e.width / 2 : i, l - e.height / 2, e.width, e.height);
		var u = !!s.backgroundColor, d = e.textPadding;
		d && (i = Dl(i, a, d), l -= e.height / 2 - d[0] - e.innerHeight / 2);
		var f = this._getOrCreateChild($c), p = f.createStyle();
		f.useStyle(p);
		var m = this._defaultStyle, h = !1, g = 0, _ = !1, v = El("fill" in s ? s.fill : "fill" in t ? t.fill : (h = !0, m.fill)), y = Tl("stroke" in s ? s.stroke : "stroke" in t ? t.stroke : !u && !o && (!m.autoStroke || h) ? (g = pl, _ = !0, m.stroke) : null), b = s.textShadowBlur > 0 || t.textShadowBlur > 0;
		p.text = e.text, p.x = i, p.y = l, b && (p.shadowBlur = s.textShadowBlur || t.textShadowBlur || 0, p.shadowColor = s.textShadowColor || t.textShadowColor || "transparent", p.shadowOffsetX = s.textShadowOffsetX || t.textShadowOffsetX || 0, p.shadowOffsetY = s.textShadowOffsetY || t.textShadowOffsetY || 0), p.textAlign = a, p.textBaseline = "middle", p.font = e.font || "12px sans-serif", p.opacity = me(s.opacity, t.opacity, 1), xl(p, s), y && (p.lineWidth = me(s.lineWidth, t.lineWidth, g), p.lineDash = G(s.lineDash, t.lineDash), p.lineDashOffset = t.lineDashOffset || 0, p.stroke = y), v && (p.fill = v), f.setBoundingRect(Rs(p, e.contentWidth, e.contentHeight, _ ? 0 : null));
	}, t.prototype._renderBackground = function(e, t, n, r, i, a) {
		var o = e.backgroundColor, s = e.borderWidth, c = e.borderColor, l = o && o.image, u = o && !l, d = e.borderRadius, f = this, p, m;
		if (u || e.lineHeight || s && c) {
			p = this._getOrCreateChild(dl), p.useStyle(p.createStyle()), p.style.fill = null;
			var h = p.shape;
			h.x = n, h.y = r, h.width = i, h.height = a, h.r = d, p.dirtyShape();
		}
		if (u) {
			var g = p.style;
			g.fill = o || null, g.fillOpacity = G(e.fillOpacity, 1);
		} else if (l) {
			m = this._getOrCreateChild(rl), m.onload = function() {
				f.dirtyStyle();
			};
			var _ = m.style;
			_.image = o.image, _.x = n, _.y = r, _.width = i, _.height = a;
		}
		if (s && c) {
			var g = p.style;
			g.lineWidth = s, g.stroke = c, g.strokeOpacity = G(e.strokeOpacity, 1), g.lineDash = e.borderDash, g.lineDashOffset = e.borderDashOffset || 0, p.strokeContainThreshold = 0, p.hasFill() && p.hasStroke() && (g.strokeFirst = !0, g.lineWidth *= 2);
		}
		var v = (p || m).style;
		v.shadowBlur = e.shadowBlur || 0, v.shadowColor = e.shadowColor || "transparent", v.shadowOffsetX = e.shadowOffsetX || 0, v.shadowOffsetY = e.shadowOffsetY || 0, v.opacity = me(e.opacity, t.opacity, 1);
	}, t.makeFont = function(e) {
		var t = "";
		return Sl(e) && (t = [
			e.fontStyle,
			e.fontWeight,
			bl(e.fontSize),
			e.fontFamily || "sans-serif"
		].join(" ")), t && ve(t) || e.textFont || e.font;
	}, t;
}(Gs), _l = {
	left: !0,
	right: 1,
	center: 1
}, vl = {
	top: 1,
	bottom: 1,
	middle: 1
}, yl = [
	"fontStyle",
	"fontWeight",
	"fontSize",
	"fontFamily"
];
function bl(e) {
	return typeof e == "string" && (e.indexOf("px") !== -1 || e.indexOf("rem") !== -1 || e.indexOf("em") !== -1) ? e : isNaN(+e) ? "12px" : e + "px";
}
function xl(e, t) {
	for (var n = 0; n < yl.length; n++) {
		var r = yl[n], i = t[r];
		i != null && (e[r] = i);
	}
}
function Sl(e) {
	return e.fontSize != null || e.fontFamily || e.fontWeight;
}
function Cl(e) {
	return wl(e), F(e.rich, wl), e;
}
function wl(e) {
	if (e) {
		e.font = gl.makeFont(e);
		var t = e.align;
		t === "middle" && (t = "center"), e.align = t == null || _l[t] ? t : "left";
		var n = e.verticalAlign;
		n === "center" && (n = "middle"), e.verticalAlign = n == null || vl[n] ? n : "top", e.padding &&= ge(e.padding);
	}
}
function Tl(e, t) {
	return e == null || t <= 0 || e === "transparent" || e === "none" ? null : e.image || e.colorStops ? "#000" : e;
}
function El(e) {
	return e == null || e === "none" ? null : e.image || e.colorStops ? "#000" : e;
}
function Dl(e, t, n) {
	return t === "right" ? e - n[1] : t === "center" ? e + n[3] / 2 - n[1] / 2 : e + n[3];
}
function Ol(e) {
	var t = e.text;
	return t != null && (t += ""), t;
}
function kl(e) {
	return !!(e.backgroundColor || e.lineHeight || e.borderWidth && e.borderColor);
}
//#endregion
//#region node_modules/echarts/lib/util/innerStore.js
var Al = Y(), jl = function(e, t, n, r) {
	if (r) {
		var i = Al(r);
		i.dataIndex = n, i.dataType = t, i.seriesIndex = e, i.ssrType = "chart", r.type === "group" && r.traverse(function(r) {
			var i = Al(r);
			i.seriesIndex = e, i.dataIndex = n, i.dataType = t, i.ssrType = "chart";
		});
	}
}, Ml = "series", Nl = K([
	"tooltip",
	"label",
	"itemName",
	"itemId",
	"itemGroupId",
	"itemChildGroupId",
	"seriesName"
]), Pl = "original", Fl = "arrayRows", Il = "objectRows", Ll = "keyedColumns", Rl = "typedArray", zl = "unknown", Bl = "column", Vl = "Roam", Hl = [
	"getDom",
	"getZr",
	"getWidth",
	"getHeight",
	"getDevicePixelRatio",
	"dispatchAction",
	"isSSR",
	"isDisposed",
	"on",
	"off",
	"getDataURL",
	"getConnectedDataURL",
	"getOption",
	"getId",
	"updateLabelLayout"
], Ul = function() {
	function e(e) {
		F(Hl, function(t) {
			this[t] = R(e[t], e);
		}, this);
	}
	return e;
}();
function Wl(e, t) {
	return t.mainType === "series" ? e.getViewOfSeriesModel(t) : e.getViewOfComponentModel(t);
}
//#endregion
//#region node_modules/echarts/lib/util/states.js
var Gl = 1, Kl = {}, ql = Y(), Jl = Y(), Yl = [
	"emphasis",
	"blur",
	"select"
], Xl = [
	"normal",
	"emphasis",
	"blur",
	"select"
], Zl = "highlight", Ql = "downplay", $l = "select", eu = "unselect", tu = "toggleSelect", nu = "selectchanged";
function ru(e) {
	return e != null && e !== "none";
}
function iu(e, t, n) {
	e.onHoverStateChange && (e.hoverState || 0) !== n && e.onHoverStateChange(t), e.hoverState = n;
}
function au(e) {
	iu(e, "emphasis", 2);
}
function ou(e) {
	e.hoverState === 2 && iu(e, "normal", 0);
}
function su(e) {
	iu(e, "blur", 1);
}
function cu(e) {
	e.hoverState === 1 && iu(e, "normal", 0);
}
function lu(e) {
	e.selected = !0;
}
function uu(e) {
	e.selected = !1;
}
function du(e, t, n) {
	t(e, n);
}
function fu(e, t, n) {
	du(e, t, n), e.isGroup && e.traverse(function(e) {
		du(e, t, n);
	});
}
function pu(e, t, n, r) {
	for (var i = e.style, a = {}, o = 0; o < t.length; o++) {
		var s = t[o];
		a[s] = i[s] ?? (r && r[s]);
	}
	for (var o = 0; o < e.animators.length; o++) {
		var c = e.animators[o];
		c.__fromStateTransition && c.__fromStateTransition.indexOf(n) < 0 && c.targetName === "style" && c.saveTo(a, t);
	}
	return a;
}
function mu(e, t, n, r) {
	var i = n && N(n, "select") >= 0, a = !1;
	if (e instanceof Z) {
		var o = ql(e), s = i && o.selectFill || o.normalFill, c = i && o.selectStroke || o.normalStroke;
		if (ru(s) || ru(c)) {
			r ||= {};
			var l = r.style || {};
			l.fill === "inherit" ? (a = !0, r = j({}, r), l = j({}, l), l.fill = s) : !ru(l.fill) && ru(s) ? (a = !0, r = j({}, r), l = j({}, l), l.fill = Mr(s)) : !ru(l.stroke) && ru(c) && (a || (r = j({}, r), l = j({}, l)), l.stroke = Mr(c)), r.style = l;
		}
	}
	if (r && r.z2 == null) {
		a || (r = j({}, r));
		var u = e.z2EmphasisLift;
		r.z2 = e.z2 + (u ?? 10);
	}
	return r;
}
function hu(e, t, n) {
	if (n && n.z2 == null) {
		n = j({}, n);
		var r = e.z2SelectLift;
		n.z2 = e.z2 + (r ?? 9);
	}
	return n;
}
function gu(e, t, n) {
	var r = N(e.currentStates, t) >= 0, i = e.style.opacity, a = r ? null : pu(e, ["opacity"], t, { opacity: 1 });
	n ||= {};
	var o = n.style || {};
	return o.opacity ?? (n = j({}, n), o = j({ opacity: r ? i : a.opacity * .1 }, o), n.style = o), n;
}
function _u(e, t) {
	var n = this.states[e];
	if (this.style) {
		if (e === "emphasis") return mu(this, e, t, n);
		if (e === "blur") return gu(this, e, n);
		if (e === "select") return hu(this, e, n);
	}
	return n;
}
function vu(e) {
	e.stateProxy = _u;
	var t = e.getTextContent(), n = e.getTextGuideLine();
	t && (t.stateProxy = _u), n && (n.stateProxy = _u);
}
function yu(e, t) {
	!Du(e, t) && !e.__highByOuter && fu(e, au);
}
function bu(e, t) {
	!Du(e, t) && !e.__highByOuter && fu(e, ou);
}
function xu(e, t) {
	e.__highByOuter |= 1 << (t || 0), fu(e, au);
}
function Su(e, t) {
	!(e.__highByOuter &= ~(1 << (t || 0))) && fu(e, ou);
}
function Cu(e) {
	fu(e, su);
}
function wu(e) {
	fu(e, cu);
}
function Tu(e) {
	fu(e, lu);
}
function Eu(e) {
	fu(e, uu);
}
function Du(e, t) {
	return e.__highDownSilentOnTouch && t.zrByTouch;
}
function Ou(e) {
	var t = e.getModel(), n = [], r = [];
	t.eachComponent(function(t, i) {
		var a = Jl(i), o = Wl(e, i), s = t === "series";
		!s && r.push(o), a.isBlured && (o.group.traverse(function(e) {
			cu(e);
		}), s && n.push(i)), a.isBlured = !1;
	}), F(r, function(e) {
		e && e.toggleBlurSeries && e.toggleBlurSeries(n, !1, t);
	});
}
function ku(e, t, n, r) {
	var i = r.getModel();
	n ||= "coordinateSystem";
	function a(e, t) {
		for (var n = 0; n < t.length; n++) {
			var r = e.getItemGraphicEl(t[n]);
			r && wu(r);
		}
	}
	if (e != null && t && t !== "none") {
		var o = i.getSeriesByIndex(e), s = o.coordinateSystem;
		s && s.master && (s = s.master);
		var c = [];
		i.eachSeries(function(e) {
			var i = o === e, l = e.coordinateSystem;
			if (l && l.master && (l = l.master), !(n === "series" && !i || n === "coordinateSystem" && !(l && s ? l === s : i) || t === "series" && i)) {
				if (r.getViewOfSeriesModel(e).group.traverse(function(e) {
					e.__highByOuter && i && t === "self" || su(e);
				}), P(t)) a(e.getData(), t);
				else if (W(t)) for (var u = L(t), d = 0; d < u.length; d++) a(e.getData(u[d]), t[u[d]]);
				c.push(e), Jl(e).isBlured = !0;
			}
		}), i.eachComponent(function(e, t) {
			if (e !== "series") {
				var n = r.getViewOfComponentModel(t);
				n && n.toggleBlurSeries && n.toggleBlurSeries(c, !0, i);
			}
		});
	}
}
function Au(e, t, n) {
	if (e != null && t != null) {
		var r = n.getModel().getComponent(e, t);
		if (r) {
			Jl(r).isBlured = !0;
			var i = n.getViewOfComponentModel(r);
			i && i.focusBlurEnabled && i.group.traverse(function(e) {
				su(e);
			});
		}
	}
}
function ju(e, t, n) {
	var r = e.seriesIndex, i = e.getData(t.dataType);
	if (i) {
		var a = No(i, t);
		a = (B(a) ? a[0] : a) || 0;
		var o = i.getItemGraphicEl(a);
		if (!o) for (var s = i.count(), c = 0; !o && c < s;) o = i.getItemGraphicEl(c++);
		if (o) {
			var l = Al(o);
			ku(r, l.focus, l.blurScope, n);
		} else {
			var u = e.get(["emphasis", "focus"]), d = e.get(["emphasis", "blurScope"]);
			u != null && ku(r, u, d, n);
		}
	}
}
function Mu(e, t, n, r) {
	var i = {
		focusSelf: !1,
		dispatchers: null
	};
	if (e == null || e === "series" || t == null || n == null) return i;
	var a = r.getModel().getComponent(e, t);
	if (!a) return i;
	var o = r.getViewOfComponentModel(a);
	if (!o || !o.findHighDownDispatchers) return i;
	for (var s = o.findHighDownDispatchers(n), c, l = 0; l < s.length; l++) if (Al(s[l]).focus === "self") {
		c = !0;
		break;
	}
	return {
		focusSelf: c,
		dispatchers: s
	};
}
function Nu(e, t, n) {
	var r = Al(e), i = Mu(r.componentMainType, r.componentIndex, r.componentHighDownName, n), a = i.dispatchers, o = i.focusSelf;
	a ? (o && Au(r.componentMainType, r.componentIndex, n), F(a, function(e) {
		return yu(e, t);
	})) : (ku(r.seriesIndex, r.focus, r.blurScope, n), r.focus === "self" && Au(r.componentMainType, r.componentIndex, n), yu(e, t));
}
function Pu(e, t, n) {
	Ou(n);
	var r = Al(e), i = Mu(r.componentMainType, r.componentIndex, r.componentHighDownName, n).dispatchers;
	i ? F(i, function(e) {
		return bu(e, t);
	}) : bu(e, t);
}
function Fu(e, t, n) {
	if (Gu(t)) {
		var r = t.dataType, i = No(e.getData(r), t);
		B(i) || (i = [i]), e[t.type === "toggleSelect" ? "toggleSelect" : t.type === "select" ? "select" : "unselect"](i, r);
	}
}
function Iu(e) {
	F(e.getAllData(), function(t) {
		var n = t.data, r = t.type;
		n.eachItemGraphicEl(function(t, n) {
			e.isSelected(n, r) ? Tu(t) : Eu(t);
		});
	});
}
function Lu(e) {
	var t = [];
	return e.eachSeries(function(e) {
		F(e.getAllData(), function(n) {
			n.data;
			var r = n.type, i = e.getSelectedDataIndices();
			if (i.length > 0) {
				var a = {
					dataIndex: i,
					seriesIndex: e.seriesIndex
				};
				r != null && (a.dataType = r), t.push(a);
			}
		});
	}), t;
}
function Ru(e, t, n) {
	Hu(e, !0), fu(e, vu), Vu(e, t, n);
}
function zu(e) {
	Hu(e, !1);
}
function Bu(e, t, n, r) {
	r ? zu(e) : Ru(e, t, n);
}
function Vu(e, t, n) {
	var r = Al(e);
	t == null ? r.focus &&= null : (r.focus = t, r.blurScope = n);
}
function Hu(e, t) {
	var n = t === !1, r = e;
	e.highDownSilentOnTouch && (r.__highDownSilentOnTouch = e.highDownSilentOnTouch), (!n || r.__highDownDispatcher) && (r.__highByOuter = r.__highByOuter || 0, r.__highDownDispatcher = !n);
}
function Uu(e) {
	return !!(e && e.__highDownDispatcher);
}
function Wu(e) {
	var t = Kl[e];
	return t == null && Gl <= 32 && (t = Kl[e] = Gl++), t;
}
function Gu(e) {
	var t = e.type;
	return t === "select" || t === "unselect" || t === "toggleSelect";
}
function Ku(e) {
	var t = e.type;
	return t === "highlight" || t === "downplay";
}
function qu(e) {
	var t = ql(e);
	t.normalFill = e.style.fill, t.normalStroke = e.style.stroke;
	var n = e.states.select || {};
	t.selectFill = n.style && n.style.fill || null, t.selectStroke = n.style && n.style.stroke || null;
}
//#endregion
//#region node_modules/zrender/lib/tool/transformPath.js
var Ju = Dc.CMD, Yu = [
	[],
	[],
	[]
], Xu = Math.sqrt, Zu = Math.atan2;
function Qu(e, t) {
	if (t) {
		var n = e.data, r = e.len(), i, a, o, s, c, l, u = Ju.M, d = Ju.C, f = Ju.L, p = Ju.R, m = Ju.A, h = Ju.Q;
		for (o = 0, s = 0; o < r;) {
			switch (i = n[o++], s = o, a = 0, i) {
				case u:
					a = 1;
					break;
				case f:
					a = 1;
					break;
				case d:
					a = 3;
					break;
				case h:
					a = 2;
					break;
				case m:
					var g = t[4], _ = t[5], v = Xu(t[0] * t[0] + t[1] * t[1]), y = Xu(t[2] * t[2] + t[3] * t[3]), b = Zu(-t[1] / y, t[0] / v);
					n[o] *= v, n[o++] += g, n[o] *= y, n[o++] += _, n[o++] *= v, n[o++] *= y, n[o++] += b, n[o++] += b, o += 2, s = o;
					break;
				case p: l[0] = n[o++], l[1] = n[o++], qe(l, l, t), n[s++] = l[0], n[s++] = l[1], l[0] += n[o++], l[1] += n[o++], qe(l, l, t), n[s++] = l[0], n[s++] = l[1];
			}
			for (c = 0; c < a; c++) {
				var x = Yu[c];
				x[0] = n[o++], x[1] = n[o++], qe(x, x, t), n[s++] = x[0], n[s++] = x[1];
			}
		}
		e.increaseVersion();
	}
}
//#endregion
//#region node_modules/zrender/lib/tool/path.js
var $u = Math.sqrt, ed = Math.sin, td = Math.cos, nd = Math.PI;
function rd(e) {
	return Math.sqrt(e[0] * e[0] + e[1] * e[1]);
}
function id(e, t) {
	return (e[0] * t[0] + e[1] * t[1]) / (rd(e) * rd(t));
}
function ad(e, t) {
	return (e[0] * t[1] < e[1] * t[0] ? -1 : 1) * Math.acos(id(e, t));
}
function od(e, t, n, r, i, a, o, s, c, l, u) {
	var d = nd / 180 * c, f = td(d) * (e - n) / 2 + ed(d) * (t - r) / 2, p = -1 * ed(d) * (e - n) / 2 + td(d) * (t - r) / 2, m = f * f / (o * o) + p * p / (s * s);
	m > 1 && (o *= $u(m), s *= $u(m));
	var h = (i === a ? -1 : 1) * $u((o * o * (s * s) - o * o * (p * p) - s * s * (f * f)) / (o * o * (p * p) + s * s * (f * f))) || 0, g = h * o * p / s, _ = h * -s * f / o, v = (e + n) / 2 + td(d) * g - ed(d) * _, y = (t + r) / 2 + ed(d) * g + td(d) * _, b = ad([1, 0], [(f - g) / o, (p - _) / s]), x = [(f - g) / o, (p - _) / s], S = [(-1 * f - g) / o, (-1 * p - _) / s], C = ad(x, S);
	if (id(x, S) <= -1 && (C = nd), id(x, S) >= 1 && (C = 0), C < 0) {
		var w = Math.round(C / nd * 1e6) / 1e6;
		C = nd * 2 + w % 2 * nd;
	}
	u.addData(l, v, y, o, s, b, C, d, a);
}
var sd = /([mlvhzcqtsa])([^mlvhzcqtsa]*)/gi, cd = /-?([0-9]*\.)?[0-9]+([eE]-?[0-9]+)?/g;
function ld(e) {
	var t = new Dc();
	if (!e) return t;
	var n = 0, r = 0, i = n, a = r, o, s = Dc.CMD, c = e.match(sd);
	if (!c) return t;
	for (var l = 0; l < c.length; l++) {
		for (var u = c[l], d = u.charAt(0), f = void 0, p = u.match(cd) || [], m = p.length, h = 0; h < m; h++) p[h] = parseFloat(p[h]);
		for (var g = 0; g < m;) {
			var _ = void 0, v = void 0, y = void 0, b = void 0, x = void 0, S = void 0, C = void 0, w = n, T = r, E = void 0, D = void 0;
			switch (d) {
				case "l":
					n += p[g++], r += p[g++], f = s.L, t.addData(f, n, r);
					break;
				case "L":
					n = p[g++], r = p[g++], f = s.L, t.addData(f, n, r);
					break;
				case "m":
					n += p[g++], r += p[g++], f = s.M, t.addData(f, n, r), i = n, a = r, d = "l";
					break;
				case "M":
					n = p[g++], r = p[g++], f = s.M, t.addData(f, n, r), i = n, a = r, d = "L";
					break;
				case "h":
					n += p[g++], f = s.L, t.addData(f, n, r);
					break;
				case "H":
					n = p[g++], f = s.L, t.addData(f, n, r);
					break;
				case "v":
					r += p[g++], f = s.L, t.addData(f, n, r);
					break;
				case "V":
					r = p[g++], f = s.L, t.addData(f, n, r);
					break;
				case "C":
					f = s.C, t.addData(f, p[g++], p[g++], p[g++], p[g++], p[g++], p[g++]), n = p[g - 2], r = p[g - 1];
					break;
				case "c":
					f = s.C, t.addData(f, p[g++] + n, p[g++] + r, p[g++] + n, p[g++] + r, p[g++] + n, p[g++] + r), n += p[g - 2], r += p[g - 1];
					break;
				case "S":
					_ = n, v = r, E = t.len(), D = t.data, o === s.C && (_ += n - D[E - 4], v += r - D[E - 3]), f = s.C, w = p[g++], T = p[g++], n = p[g++], r = p[g++], t.addData(f, _, v, w, T, n, r);
					break;
				case "s":
					_ = n, v = r, E = t.len(), D = t.data, o === s.C && (_ += n - D[E - 4], v += r - D[E - 3]), f = s.C, w = n + p[g++], T = r + p[g++], n += p[g++], r += p[g++], t.addData(f, _, v, w, T, n, r);
					break;
				case "Q":
					w = p[g++], T = p[g++], n = p[g++], r = p[g++], f = s.Q, t.addData(f, w, T, n, r);
					break;
				case "q":
					w = p[g++] + n, T = p[g++] + r, n += p[g++], r += p[g++], f = s.Q, t.addData(f, w, T, n, r);
					break;
				case "T":
					_ = n, v = r, E = t.len(), D = t.data, o === s.Q && (_ += n - D[E - 4], v += r - D[E - 3]), n = p[g++], r = p[g++], f = s.Q, t.addData(f, _, v, n, r);
					break;
				case "t":
					_ = n, v = r, E = t.len(), D = t.data, o === s.Q && (_ += n - D[E - 4], v += r - D[E - 3]), n += p[g++], r += p[g++], f = s.Q, t.addData(f, _, v, n, r);
					break;
				case "A":
					y = p[g++], b = p[g++], x = p[g++], S = p[g++], C = p[g++], w = n, T = r, n = p[g++], r = p[g++], f = s.A, od(w, T, n, r, S, C, y, b, x, f, t);
					break;
				case "a": y = p[g++], b = p[g++], x = p[g++], S = p[g++], C = p[g++], w = n, T = r, n += p[g++], r += p[g++], f = s.A, od(w, T, n, r, S, C, y, b, x, f, t);
			}
		}
		(d === "z" || d === "Z") && (f = s.Z, t.addData(f), n = i, r = a), o = f;
	}
	return t.toStatic(), t;
}
var ud = function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t.prototype.applyTransform = function(e) {}, t;
}(Z);
function dd(e) {
	return e.setData != null;
}
function fd(e, t) {
	var n = ld(e), r = j({}, t);
	return r.buildPath = function(e) {
		var t = dd(e);
		if (t && e.canSave()) {
			e.appendPath(n);
			var r = e.getContext();
			r && e.rebuildPath(r, 1);
		} else {
			var r = t ? e.getContext() : e;
			r && n.rebuildPath(r, 1);
		}
	}, r.applyTransform = function(e) {
		Qu(n, e), this.dirtyShape();
	}, r;
}
function pd(e, t) {
	return new ud(fd(e, t));
}
function md(e, t) {
	var n = fd(e, t);
	return function(e) {
		r(t, e);
		function t(t) {
			var r = e.call(this, t) || this;
			return r.applyTransform = n.applyTransform, r.buildPath = n.buildPath, r;
		}
		return t;
	}(ud);
}
function hd(e, t) {
	for (var n = [], r = e.length, i = 0; i < r; i++) {
		var a = e[i];
		n.push(a.getUpdatedPathProxy(!0));
	}
	var o = new Z(t);
	return o.createPathProxy(), o.buildPath = function(e) {
		if (dd(e)) {
			e.appendPath(n);
			var t = e.getContext();
			t && e.rebuildPath(t, 1);
		}
	}, o;
}
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Circle.js
var gd = function() {
	function e() {
		this.cx = 0, this.cy = 0, this.r = 0;
	}
	return e;
}(), _d = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultShape = function() {
		return new gd();
	}, t.prototype.buildPath = function(e, t) {
		e.moveTo(t.cx + t.r, t.cy), e.arc(t.cx, t.cy, t.r, 0, Math.PI * 2);
	}, t;
}(Z);
_d.prototype.type = "circle";
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Ellipse.js
var vd = function() {
	function e() {
		this.cx = 0, this.cy = 0, this.rx = 0, this.ry = 0;
	}
	return e;
}(), yd = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultShape = function() {
		return new vd();
	}, t.prototype.buildPath = function(e, t) {
		var n = .5522848, r = t.cx, i = t.cy, a = t.rx, o = t.ry, s = a * n, c = o * n;
		e.moveTo(r - a, i), e.bezierCurveTo(r - a, i - c, r - s, i - o, r, i - o), e.bezierCurveTo(r + s, i - o, r + a, i - c, r + a, i), e.bezierCurveTo(r + a, i + c, r + s, i + o, r, i + o), e.bezierCurveTo(r - s, i + o, r - a, i + c, r - a, i), e.closePath();
	}, t;
}(Z);
yd.prototype.type = "ellipse";
//#endregion
//#region node_modules/zrender/lib/graphic/helper/roundSector.js
var bd = Math.PI, xd = bd * 2, Sd = Math.sin, Cd = Math.cos, wd = Math.acos, Td = Math.atan2, Ed = Math.abs, Dd = Math.sqrt, Od = Math.max, kd = Math.min, Ad = 1e-4;
function jd(e, t, n, r, i, a, o, s) {
	var c = n - e, l = r - t, u = o - i, d = s - a, f = d * c - u * l;
	if (!(f * f < Ad)) return f = (u * (t - a) - d * (e - i)) / f, [e + f * c, t + f * l];
}
function Md(e, t, n, r, i, a, o) {
	var s = e - n, c = t - r, l = (o ? a : -a) / Dd(s * s + c * c), u = l * c, d = -l * s, f = e + u, p = t + d, m = n + u, h = r + d, g = (f + m) / 2, _ = (p + h) / 2, v = m - f, y = h - p, b = v * v + y * y, x = i - a, S = f * h - m * p, C = (y < 0 ? -1 : 1) * Dd(Od(0, x * x * b - S * S)), w = (S * y - v * C) / b, T = (-S * v - y * C) / b, E = (S * y + v * C) / b, D = (-S * v + y * C) / b, O = w - g, k = T - _, A = E - g, j = D - _;
	return O * O + k * k > A * A + j * j && (w = E, T = D), {
		cx: w,
		cy: T,
		x0: -u,
		y0: -d,
		x1: w * (i / x - 1),
		y1: T * (i / x - 1)
	};
}
function Nd(e) {
	var t;
	if (B(e)) {
		var n = e.length;
		if (!n) return e;
		t = n === 1 ? [
			e[0],
			e[0],
			0,
			0
		] : n === 2 ? [
			e[0],
			e[0],
			e[1],
			e[1]
		] : n === 3 ? e.concat(e[2]) : e;
	} else t = [
		e,
		e,
		e,
		e
	];
	return t;
}
function Pd(e, t) {
	var n, r = Od(t.r, 0), i = Od(t.r0 || 0, 0), a = r > 0;
	if (a || i > 0) {
		if (a || (r = i, i = 0), i > r) {
			var o = r;
			r = i, i = o;
		}
		var s = t.startAngle, c = t.endAngle;
		if (!(isNaN(s) || isNaN(c))) {
			var l = t.cx, u = t.cy, d = !!t.clockwise, f = Ed(c - s), p = f > xd && f % xd;
			if (p > Ad && (f = p), !(r > Ad)) e.moveTo(l, u);
			else if (f > xd - Ad) e.moveTo(l + r * Cd(s), u + r * Sd(s)), e.arc(l, u, r, s, c, !d), i > Ad && (e.moveTo(l + i * Cd(c), u + i * Sd(c)), e.arc(l, u, i, c, s, d));
			else {
				var m = void 0, h = void 0, g = void 0, _ = void 0, v = void 0, y = void 0, b = void 0, x = void 0, S = void 0, C = void 0, w = void 0, T = void 0, E = void 0, D = void 0, O = void 0, k = void 0, A = r * Cd(s), j = r * Sd(s), ee = i * Cd(c), M = i * Sd(c), N = f > Ad;
				if (N) {
					var te = t.cornerRadius;
					te && (n = Nd(te), m = n[0], h = n[1], g = n[2], _ = n[3]);
					var ne = Ed(r - i) / 2;
					if (v = kd(ne, g), y = kd(ne, _), b = kd(ne, m), x = kd(ne, h), w = S = Od(v, y), T = C = Od(b, x), (S > Ad || C > Ad) && (E = r * Cd(c), D = r * Sd(c), O = i * Cd(s), k = i * Sd(s), f < bd)) {
						var P = jd(A, j, O, k, E, D, ee, M);
						if (P) {
							var F = A - P[0], I = j - P[1], re = E - P[0], ie = D - P[1], L = 1 / Sd(wd((F * re + I * ie) / (Dd(F * F + I * I) * Dd(re * re + ie * ie))) / 2), ae = Dd(P[0] * P[0] + P[1] * P[1]);
							w = kd(S, (r - ae) / (L + 1)), T = kd(C, (i - ae) / (L - 1));
						}
					}
				}
				if (!N) e.moveTo(l + A, u + j);
				else if (w > Ad) {
					var R = kd(g, w), z = kd(_, w), B = Md(O, k, A, j, r, R, d), V = Md(E, D, ee, M, r, z, d);
					e.moveTo(l + B.cx + B.x0, u + B.cy + B.y0), w < S && R === z ? e.arc(l + B.cx, u + B.cy, w, Td(B.y0, B.x0), Td(V.y0, V.x0), !d) : (R > 0 && e.arc(l + B.cx, u + B.cy, R, Td(B.y0, B.x0), Td(B.y1, B.x1), !d), e.arc(l, u, r, Td(B.cy + B.y1, B.cx + B.x1), Td(V.cy + V.y1, V.cx + V.x1), !d), z > 0 && e.arc(l + V.cx, u + V.cy, z, Td(V.y1, V.x1), Td(V.y0, V.x0), !d));
				} else e.moveTo(l + A, u + j), e.arc(l, u, r, s, c, !d);
				if (!(i > Ad) || !N) e.lineTo(l + ee, u + M);
				else if (T > Ad) {
					var R = kd(m, T), z = kd(h, T), B = Md(ee, M, E, D, i, -z, d), V = Md(A, j, O, k, i, -R, d);
					e.lineTo(l + B.cx + B.x0, u + B.cy + B.y0), T < C && R === z ? e.arc(l + B.cx, u + B.cy, T, Td(B.y0, B.x0), Td(V.y0, V.x0), !d) : (z > 0 && e.arc(l + B.cx, u + B.cy, z, Td(B.y0, B.x0), Td(B.y1, B.x1), !d), e.arc(l, u, i, Td(B.cy + B.y1, B.cx + B.x1), Td(V.cy + V.y1, V.cx + V.x1), d), R > 0 && e.arc(l + V.cx, u + V.cy, R, Td(V.y1, V.x1), Td(V.y0, V.x0), !d));
				} else e.lineTo(l + ee, u + M), e.arc(l, u, i, c, s, d);
			}
			e.closePath();
		}
	}
}
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Sector.js
var Fd = function() {
	function e() {
		this.cx = 0, this.cy = 0, this.r0 = 0, this.r = 0, this.startAngle = 0, this.endAngle = Math.PI * 2, this.clockwise = !0, this.cornerRadius = 0;
	}
	return e;
}(), Id = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultShape = function() {
		return new Fd();
	}, t.prototype.buildPath = function(e, t) {
		Pd(e, t);
	}, t.prototype.isZeroArea = function() {
		return this.shape.startAngle === this.shape.endAngle || this.shape.r === this.shape.r0;
	}, t;
}(Z);
Id.prototype.type = "sector";
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Ring.js
var Ld = function() {
	function e() {
		this.cx = 0, this.cy = 0, this.r = 0, this.r0 = 0;
	}
	return e;
}(), Rd = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultShape = function() {
		return new Ld();
	}, t.prototype.buildPath = function(e, t) {
		var n = t.cx, r = t.cy, i = Math.PI * 2;
		e.moveTo(n + t.r, r), e.arc(n, r, t.r, 0, i, !1), e.moveTo(n + t.r0, r), e.arc(n, r, t.r0, 0, i, !0);
	}, t;
}(Z);
Rd.prototype.type = "ring";
//#endregion
//#region node_modules/zrender/lib/graphic/helper/smoothBezier.js
function zd(e, t, n, r) {
	var i = [], a = [], o = [], s = [], c, l, u, d;
	if (r) {
		u = [Infinity, Infinity], d = [-Infinity, -Infinity];
		for (var f = 0, p = e.length; f < p; f++) Je(u, u, e[f]), Ye(d, d, e[f]);
		Je(u, u, r[0]), Ye(d, d, r[1]);
	}
	for (var f = 0, p = e.length; f < p; f++) {
		var m = e[f];
		if (n) c = e[f ? f - 1 : p - 1], l = e[(f + 1) % p];
		else if (f === 0 || f === p - 1) {
			i.push(Pe(e[f]));
			continue;
		} else c = e[f - 1], l = e[f + 1];
		Re(a, l, c), Ve(a, a, t);
		var h = Ue(m, c), g = Ue(m, l), _ = h + g;
		_ !== 0 && (h /= _, g /= _), Ve(o, a, -h), Ve(s, a, g);
		var v = Ie([], m, o), y = Ie([], m, s);
		r && (Ye(v, v, u), Je(v, v, d), Ye(y, y, u), Je(y, y, d)), i.push(v), i.push(y);
	}
	return n && i.push(i.shift()), i;
}
//#endregion
//#region node_modules/zrender/lib/graphic/helper/poly.js
function Bd(e, t, n) {
	var r = t.smooth, i = t.points;
	if (i && i.length >= 2) {
		if (r) {
			var a = zd(i, r, n, t.smoothConstraint);
			e.moveTo(i[0][0], i[0][1]);
			for (var o = i.length, s = 0; s < (n ? o : o - 1); s++) {
				var c = a[s * 2], l = a[s * 2 + 1], u = i[(s + 1) % o];
				e.bezierCurveTo(c[0], c[1], l[0], l[1], u[0], u[1]);
			}
		} else {
			e.moveTo(i[0][0], i[0][1]);
			for (var s = 1, d = i.length; s < d; s++) e.lineTo(i[s][0], i[s][1]);
		}
		n && e.closePath();
	}
}
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Polygon.js
var Vd = function() {
	function e() {
		this.points = null, this.smooth = 0, this.smoothConstraint = null;
	}
	return e;
}(), Hd = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultShape = function() {
		return new Vd();
	}, t.prototype.buildPath = function(e, t) {
		Bd(e, t, !0);
	}, t;
}(Z);
Hd.prototype.type = "polygon";
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Polyline.js
var Ud = function() {
	function e() {
		this.points = null, this.percent = 1, this.smooth = 0, this.smoothConstraint = null;
	}
	return e;
}(), Wd = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultStyle = function() {
		return {
			stroke: "#000",
			fill: null
		};
	}, t.prototype.getDefaultShape = function() {
		return new Ud();
	}, t.prototype.buildPath = function(e, t) {
		Bd(e, t, !1);
	}, t;
}(Z);
Wd.prototype.type = "polyline";
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Line.js
var Gd = {}, Kd = function() {
	function e() {
		this.x1 = 0, this.y1 = 0, this.x2 = 0, this.y2 = 0, this.percent = 1;
	}
	return e;
}(), qd = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultStyle = function() {
		return {
			stroke: "#000",
			fill: null
		};
	}, t.prototype.getDefaultShape = function() {
		return new Kd();
	}, t.prototype.buildPath = function(e, t) {
		var n, r, i, a;
		if (this.subPixelOptimize) {
			var o = ol(Gd, t, this.style);
			n = o.x1, r = o.y1, i = o.x2, a = o.y2;
		} else n = t.x1, r = t.y1, i = t.x2, a = t.y2;
		var s = t.percent;
		s !== 0 && (e.moveTo(n, r), s < 1 && (i = n * (1 - s) + i * s, a = r * (1 - s) + a * s), e.lineTo(i, a));
	}, t.prototype.pointAt = function(e) {
		var t = this.shape;
		return [t.x1 * (1 - e) + t.x2 * e, t.y1 * (1 - e) + t.y2 * e];
	}, t;
}(Z);
qd.prototype.type = "line";
//#endregion
//#region node_modules/zrender/lib/graphic/shape/BezierCurve.js
var Jd = [], Yd = function() {
	function e() {
		this.x1 = 0, this.y1 = 0, this.x2 = 0, this.y2 = 0, this.cpx1 = 0, this.cpy1 = 0, this.percent = 1;
	}
	return e;
}();
function Xd(e, t, n) {
	var r = e.cpx2, i = e.cpy2;
	return r != null || i != null ? [(n ? qn : Kn)(e.x1, e.cpx1, e.cpx2, e.x2, t), (n ? qn : Kn)(e.y1, e.cpy1, e.cpy2, e.y2, t)] : [(n ? er : $n)(e.x1, e.cpx1, e.x2, t), (n ? er : $n)(e.y1, e.cpy1, e.y2, t)];
}
var Zd = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultStyle = function() {
		return {
			stroke: "#000",
			fill: null
		};
	}, t.prototype.getDefaultShape = function() {
		return new Yd();
	}, t.prototype.buildPath = function(e, t) {
		var n = t.x1, r = t.y1, i = t.x2, a = t.y2, o = t.cpx1, s = t.cpy1, c = t.cpx2, l = t.cpy2, u = t.percent;
		u !== 0 && (e.moveTo(n, r), c == null || l == null ? (u < 1 && (rr(n, o, i, u, Jd), o = Jd[1], i = Jd[2], rr(r, s, a, u, Jd), s = Jd[1], a = Jd[2]), e.quadraticCurveTo(o, s, i, a)) : (u < 1 && (Xn(n, o, c, i, u, Jd), o = Jd[1], c = Jd[2], i = Jd[3], Xn(r, s, l, a, u, Jd), s = Jd[1], l = Jd[2], a = Jd[3]), e.bezierCurveTo(o, s, c, l, i, a)));
	}, t.prototype.pointAt = function(e) {
		return Xd(this.shape, e, !1);
	}, t.prototype.tangentAt = function(e) {
		var t = Xd(this.shape, e, !0);
		return He(t, t);
	}, t;
}(Z);
Zd.prototype.type = "bezier-curve";
//#endregion
//#region node_modules/zrender/lib/graphic/shape/Arc.js
var Qd = function() {
	function e() {
		this.cx = 0, this.cy = 0, this.r = 0, this.startAngle = 0, this.endAngle = Math.PI * 2, this.clockwise = !0;
	}
	return e;
}(), $d = function(e) {
	r(t, e);
	function t(t) {
		return e.call(this, t) || this;
	}
	return t.prototype.getDefaultStyle = function() {
		return {
			stroke: "#000",
			fill: null
		};
	}, t.prototype.getDefaultShape = function() {
		return new Qd();
	}, t.prototype.buildPath = function(e, t) {
		var n = t.cx, r = t.cy, i = Math.max(t.r, 0), a = t.startAngle, o = t.endAngle, s = t.clockwise, c = Math.cos(a), l = Math.sin(a);
		e.moveTo(c * i + n, l * i + r), e.arc(n, r, i, a, o, !s);
	}, t;
}(Z);
$d.prototype.type = "arc";
//#endregion
//#region node_modules/zrender/lib/graphic/CompoundPath.js
var ef = function(e) {
	r(t, e);
	function t() {
		var t = e !== null && e.apply(this, arguments) || this;
		return t.type = "compound", t;
	}
	return t.prototype._updatePathDirty = function() {
		for (var e = this.shape.paths, t = this.shapeChanged(), n = 0; n < e.length; n++) t ||= e[n].shapeChanged();
		t && this.dirtyShape();
	}, t.prototype.beforeBrush = function() {
		this._updatePathDirty();
		for (var e = this.shape.paths || [], t = this.getGlobalScale(), n = 0; n < e.length; n++) e[n].path || e[n].createPathProxy(), e[n].path.setScale(t[0], t[1], e[n].segmentIgnoreThreshold);
	}, t.prototype.buildPath = function(e, t) {
		for (var n = t.paths || [], r = 0; r < n.length; r++) n[r].buildPath(e, n[r].shape, !0);
	}, t.prototype.afterBrush = function() {
		for (var e = this.shape.paths || [], t = 0; t < e.length; t++) e[t].pathUpdated();
	}, t.prototype.getBoundingRect = function() {
		return this._updatePathDirty.call(this), Z.prototype.getBoundingRect.call(this);
	}, t;
}(Z), tf = function() {
	function e(e) {
		this.colorStops = e || [];
	}
	return e.prototype.addColorStop = function(e, t) {
		this.colorStops.push({
			offset: e,
			color: t
		});
	}, e;
}(), nf = function(e) {
	r(t, e);
	function t(t, n, r, i, a, o) {
		var s = e.call(this, a) || this;
		return s.x = t ?? 0, s.y = n ?? 0, s.x2 = r ?? 1, s.y2 = i ?? 0, s.type = "linear", s.global = o || !1, s;
	}
	return t;
}(tf), rf = function(e) {
	r(t, e);
	function t(t, n, r, i, a) {
		var o = e.call(this, i) || this;
		return o.x = t ?? .5, o.y = n ?? .5, o.r = r ?? .5, o.type = "radial", o.global = a || !1, o;
	}
	return t;
}(tf), af = Math.min, of = Math.max, sf = Math.abs, cf = [0, 0], lf = [0, 0], uf = sn(), df = uf.minTv, ff = uf.maxTv, pf = function() {
	function e(e, t) {
		this._corners = [], this._axes = [], this._origin = [0, 0];
		for (var n = 0; n < 4; n++) this._corners[n] = new q();
		for (var n = 0; n < 2; n++) this._axes[n] = new q();
		e && this.fromBoundingRect(e, t);
	}
	return e.prototype.fromBoundingRect = function(e, t) {
		var n = this._corners, r = this._axes, i = e.x, a = e.y, o = i + e.width, s = a + e.height;
		if (n[0].set(i, a), n[1].set(o, a), n[2].set(o, s), n[3].set(i, s), t) for (var c = 0; c < 4; c++) n[c].transform(t);
		q.sub(r[0], n[1], n[0]), q.sub(r[1], n[3], n[0]), r[0].normalize(), r[1].normalize();
		for (var c = 0; c < 2; c++) this._origin[c] = r[c].dot(n[0]);
	}, e.prototype.intersect = function(e, t, n) {
		var r = !0, i = !t;
		return t && q.set(t, 0, 0), uf.reset(n, !i), !this._intersectCheckOneSide(this, e, i, 1) && (r = !1, i) || !this._intersectCheckOneSide(e, this, i, -1) && (r = !1, i) || !i && !uf.negativeSize && q.copy(t, r ? uf.useDir ? uf.dirMinTv : df : ff), r;
	}, e.prototype._intersectCheckOneSide = function(e, t, n, r) {
		for (var i = !0, a = 0; a < 2; a++) {
			var o = e._axes[a];
			if (e._getProjMinMaxOnAxis(a, e._corners, cf), e._getProjMinMaxOnAxis(a, t._corners, lf), uf.negativeSize || cf[1] < lf[0] || cf[0] > lf[1]) {
				if (i = !1, uf.negativeSize || n) return i;
				var s = sf(lf[0] - cf[1]), c = sf(cf[0] - lf[1]);
				af(s, c) > ff.len() && (s < c ? q.scale(ff, o, -s * r) : q.scale(ff, o, c * r));
			} else if (!n) {
				var s = sf(lf[0] - cf[1]), c = sf(cf[0] - lf[1]);
				(uf.useDir || af(s, c) < df.len()) && ((s < c || !uf.bidirectional) && (q.scale(df, o, s * r), uf.useDir && uf.calcDirMTV()), (s >= c || !uf.bidirectional) && (q.scale(df, o, -c * r), uf.useDir && uf.calcDirMTV()));
			}
		}
		return i;
	}, e.prototype._getProjMinMaxOnAxis = function(e, t, n) {
		for (var r = this._axes[e], i = this._origin, a = t[0].dot(r) + i[e], o = a, s = a, c = 1; c < t.length; c++) {
			var l = t[c].dot(r) + i[e];
			o = af(l, o), s = of(l, s);
		}
		n[0] = o + uf.touchThreshold, n[1] = s - uf.touchThreshold, uf.negativeSize = n[1] < n[0];
	}, e;
}(), mf = [], hf = function(e) {
	r(t, e);
	function t() {
		var t = e !== null && e.apply(this, arguments) || this;
		return t.notClear = !0, t.incremental = 1, t._displayables = [], t._temporaryDisplayables = [], t._cursor = 0, t;
	}
	return t.prototype.traverse = function(e, t) {
		e.call(t, this);
	}, t.prototype.useStyle = function() {
		this.style = {};
	}, t.prototype._useHoverStyle = function() {
		this.__hoverStyle = null;
	}, t.prototype.getCursor = function() {
		return this._cursor;
	}, t.prototype.innerAfterBrush = function() {
		this._cursor = this._displayables.length;
	}, t.prototype.clearDisplaybles = function() {
		this._displayables = [], this._temporaryDisplayables = [], this._cursor = 0, this.markRedraw(), this.notClear = !1;
	}, t.prototype.clearTemporalDisplayables = function() {
		this._temporaryDisplayables = [];
	}, t.prototype.addDisplayable = function(e, t) {
		t ? this._temporaryDisplayables.push(e) : this._displayables.push(e), this.markRedraw();
	}, t.prototype.addDisplayables = function(e, t) {
		t ||= !1;
		for (var n = 0; n < e.length; n++) this.addDisplayable(e[n], t);
	}, t.prototype.getDisplayables = function() {
		return this._displayables;
	}, t.prototype.getTemporalDisplayables = function() {
		return this._temporaryDisplayables;
	}, t.prototype.eachPendingDisplayable = function(e) {
		for (var t = this._cursor; t < this._displayables.length; t++) e && e(this._displayables[t]);
		for (var t = 0; t < this._temporaryDisplayables.length; t++) e && e(this._temporaryDisplayables[t]);
	}, t.prototype.update = function() {
		this.updateTransform();
		for (var e = this._cursor; e < this._displayables.length; e++) {
			var t = this._displayables[e];
			t.parent = this, t.update(), t.parent = null;
		}
		for (var e = 0; e < this._temporaryDisplayables.length; e++) {
			var t = this._temporaryDisplayables[e];
			t.parent = this, t.update(), t.parent = null;
		}
	}, t.prototype.getBoundingRect = function() {
		if (!this._rect) {
			for (var e = new J(Infinity, Infinity, -Infinity, -Infinity), t = 0; t < this._displayables.length; t++) {
				var n = this._displayables[t], r = n.getBoundingRect().clone();
				n.needLocalTransform() && r.applyTransform(n.getLocalTransform(mf)), e.union(r);
			}
			this._rect = e;
		}
		return this._rect;
	}, t.prototype.contain = function(e, t) {
		var n = this.transformCoordToLocal(e, t);
		if (this.getBoundingRect().contain(n[0], n[1])) {
			for (var r = 0; r < this._displayables.length; r++) if (this._displayables[r].contain(e, t)) return !0;
		}
		return !1;
	}, t;
}(Gs), gf = Y();
function _f(e, t, n, r, i) {
	var a;
	if (t && t.ecModel) {
		var o = t.ecModel.getUpdatePayload();
		a = o && o.animation;
	}
	var s = t && t.isAnimationEnabled(), c = e === "update";
	if (s) {
		var l = void 0, u = void 0, d = void 0;
		return r ? (l = G(r.duration, 200), u = G(r.easing, "cubicOut"), d = 0) : (l = t.getShallow(c ? "animationDurationUpdate" : "animationDuration"), u = t.getShallow(c ? "animationEasingUpdate" : "animationEasing"), d = t.getShallow(c ? "animationDelayUpdate" : "animationDelay")), a && (a.duration != null && (l = a.duration), a.easing != null && (u = a.easing), a.delay != null && (d = a.delay)), V(d) && (d = d(n, i)), V(l) && (l = l(n)), {
			duration: l || 0,
			delay: d,
			easing: u
		};
	}
	return null;
}
function vf(e, t, n, r, i, a, o) {
	var s = !1, c;
	V(i) ? (o = a, a = i, i = null) : W(i) && (a = i.cb, o = i.during, s = i.isFrom, c = i.removeOpt, i = i.dataIndex);
	var l = e === "leave";
	l || t.stopAnimation("leave");
	var u = _f(e, r, i, l ? c || {} : null, r && r.getAnimationDelayParams ? r.getAnimationDelayParams(t, i) : null);
	if (u && u.duration > 0) {
		var d = u.duration, f = u.delay, p = u.easing, m = {
			duration: d,
			delay: f || 0,
			easing: p,
			done: a,
			force: !!a || !!o,
			setToFinal: !l,
			scope: e,
			during: o
		};
		s ? t.animateFrom(n, m) : t.animateTo(n, m);
	} else t.stopAnimation(), !s && t.attr(n), o && o(1), a && a();
}
function yf(e, t, n, r, i, a) {
	vf("update", e, t, n, r, i, a);
}
function bf(e, t, n, r, i, a) {
	vf("enter", e, t, n, r, i, a);
}
function xf(e) {
	if (!e.__zr) return !0;
	for (var t = 0; t < e.animators.length; t++) if (e.animators[t].scope === "leave") return !0;
	return !1;
}
function Sf(e, t, n, r, i, a) {
	xf(e) || vf("leave", e, t, n, r, i, a);
}
function Cf(e, t, n, r) {
	e.removeTextContent(), e.removeTextGuideLine(), Sf(e, { style: { opacity: 0 } }, t, n, r);
}
function wf(e, t, n) {
	function r() {
		e.parent && e.parent.remove(e);
	}
	e.isGroup ? e.traverse(function(e) {
		e.isGroup || Cf(e, t, n, r);
	}) : Cf(e, t, n, r);
}
function Tf(e) {
	gf(e).oldStyle = e.style;
}
//#endregion
//#region node_modules/echarts/lib/util/graphic.js
var Ef = /* @__PURE__ */ t({
	Arc: () => $d,
	BezierCurve: () => Zd,
	BoundingRect: () => J,
	Circle: () => _d,
	CompoundPath: () => ef,
	Ellipse: () => yd,
	Group: () => ba,
	HOVER_LAYER_FOR_INCREMENTAL: () => 2,
	HOVER_LAYER_FROM_THRESHOLD: () => 1,
	HOVER_LAYER_NO: () => 0,
	Image: () => rl,
	IncrementalDisplayable: () => hf,
	Line: () => qd,
	LinearGradient: () => nf,
	OrientedBoundingRect: () => pf,
	Path: () => Z,
	Point: () => q,
	Polygon: () => Hd,
	Polyline: () => Wd,
	RadialGradient: () => rf,
	Rect: () => dl,
	Ring: () => Rd,
	Sector: () => Id,
	Text: () => gl,
	WH: () => kf,
	XY: () => Of,
	applyTransform: () => Wf,
	calcZ2Range: () => pp,
	clipPointsByRect: () => Yf,
	clipRectByRect: () => Xf,
	createIcon: () => Zf,
	decomposeTransform: () => _p,
	ensureCopyRect: () => up,
	ensureCopyTransform: () => dp,
	expandOrShrinkRect: () => np,
	extendPath: () => Mf,
	extendShape: () => Af,
	getCurrentCanvasPainter: () => yp,
	getShapeClass: () => Pf,
	getTransform: () => Uf,
	groupTransition: () => Jf,
	initProps: () => bf,
	isBoundingRectAxisAligned: () => cp,
	isElementRemoved: () => xf,
	lineLineIntersect: () => $f,
	linePolygonIntersect: () => Qf,
	makeImage: () => If,
	makePath: () => Ff,
	mergePath: () => Rf,
	payloadDisableAnimation: () => gp,
	registerShape: () => Nf,
	removeElement: () => Sf,
	removeElementWithFadeOut: () => wf,
	resizePath: () => zf,
	retrieveZInfo: () => fp,
	setTooltipConfig: () => ap,
	subPixelOptimize: () => Hf,
	subPixelOptimizeLine: () => Bf,
	subPixelOptimizeRect: () => Vf,
	transformDirection: () => Gf,
	traverseElements: () => sp,
	traverseUpdateZ: () => mp,
	updateProps: () => yf
}), Df = {}, Of = ["x", "y"], kf = ["width", "height"];
function Af(e) {
	return Z.extend(e);
}
var jf = md;
function Mf(e, t) {
	return jf(e, t);
}
function Nf(e, t) {
	Df[e] = t;
}
function Pf(e) {
	if (Df.hasOwnProperty(e)) return Df[e];
}
function Ff(e, t, n, r) {
	var i = pd(e, t);
	return n && (r === "center" && (n = Lf(n, i.getBoundingRect())), zf(i, n)), i;
}
function If(e, t, n) {
	var r = new rl({
		style: {
			image: e,
			x: t.x,
			y: t.y,
			width: t.width,
			height: t.height
		},
		onload: function(e) {
			if (n === "center") {
				var i = {
					width: e.width,
					height: e.height
				};
				r.setStyle(Lf(t, i));
			}
		}
	});
	return r;
}
function Lf(e, t) {
	var n = t.width / t.height, r = e.height * n, i;
	r <= e.width ? i = e.height : (r = e.width, i = r / n);
	var a = e.x + e.width / 2, o = e.y + e.height / 2;
	return {
		x: a - r / 2,
		y: o - i / 2,
		width: r,
		height: i
	};
}
var Rf = hd;
function zf(e, t) {
	if (e.applyTransform) {
		var n = e.getBoundingRect().calculateTransform(t);
		e.applyTransform(n);
	}
}
function Bf(e, t) {
	return ol(e, e, { lineWidth: t }), e;
}
function Vf(e, t) {
	return sl(e, e, t), e;
}
var Hf = cl;
function Uf(e, t) {
	for (var n = At([]); e && e !== t;) Mt(n, e.getLocalTransform(), n), e = e.parent;
	return n;
}
function Wf(e, t, n) {
	return t && !P(t) && (t = Ri.getLocalTransform(t)), n && (t = It([], t)), qe([], e, t);
}
function Gf(e, t, n) {
	var r = t[4] === 0 || t[5] === 0 || t[0] === 0 ? 1 : Na(2 * t[4] / t[0]), i = t[4] === 0 || t[5] === 0 || t[2] === 0 ? 1 : Na(2 * t[4] / t[2]), a = [e === "left" ? -r : e === "right" ? r : 0, e === "top" ? -i : e === "bottom" ? i : 0];
	return a = Wf(a, t, n), Na(a[0]) > Na(a[1]) ? a[0] > 0 ? "right" : "left" : a[1] > 0 ? "bottom" : "top";
}
function Kf(e) {
	return !e.isGroup;
}
function qf(e) {
	return e.shape != null;
}
function Jf(e, t, n) {
	if (!e || !t) return;
	function r(e) {
		var t = {};
		return e.traverse(function(e) {
			Kf(e) && e.anid && (t[e.anid] = e);
		}), t;
	}
	function i(e) {
		var t = {
			x: e.x,
			y: e.y,
			rotation: e.rotation
		};
		return qf(e) && (t.shape = k(e.shape)), t;
	}
	var a = r(e);
	t.traverse(function(e) {
		if (Kf(e) && e.anid) {
			var t = a[e.anid];
			if (t) {
				var r = i(e);
				e.attr(i(t)), yf(e, r, n, Al(e).dataIndex);
			}
		}
	});
}
function Yf(e, t) {
	return I(e, function(e) {
		var n = e[0];
		n = Ma(n, t.x), n = ja(n, t.x + t.width);
		var r = e[1];
		return r = Ma(r, t.y), r = ja(r, t.y + t.height), [n, r];
	});
}
function Xf(e, t) {
	var n = Ma(e.x, t.x), r = ja(e.x + e.width, t.x + t.width), i = Ma(e.y, t.y), a = ja(e.y + e.height, t.y + t.height);
	if (r >= n && a >= i) return {
		x: n,
		y: i,
		width: r - n,
		height: a - i
	};
}
function Zf(e, t, n) {
	var r = j({ rectHover: !0 }, t), i = r.style = { strokeNoScale: !0 };
	if (n ||= {
		x: -1,
		y: -1,
		width: 2,
		height: 2
	}, e) return e.indexOf("image://") === 0 ? (i.image = e.slice(8), M(i, n), new rl(r)) : Ff(e.replace("path://", ""), r, n, "center");
}
function Qf(e, t, n, r, i) {
	for (var a = 0, o = i[i.length - 1]; a < i.length; a++) {
		var s = i[a];
		if ($f(e, t, n, r, s[0], s[1], o[0], o[1])) return !0;
		o = s;
	}
}
function $f(e, t, n, r, i, a, o, s) {
	var c = n - e, l = r - t, u = o - i, d = s - a, f = ep(u, d, c, l);
	if (tp(f)) return !1;
	var p = e - i, m = t - a, h = ep(p, m, c, l) / f;
	if (h < 0 || h > 1) return !1;
	var g = ep(p, m, u, d) / f;
	return !(g < 0 || g > 1);
}
function ep(e, t, n, r) {
	return e * r - n * t;
}
function tp(e) {
	return e <= 1e-6 && e >= -1e-6;
}
function np(e, t, n, r, i) {
	return t == null || (U(t) ? rp[0] = rp[1] = rp[2] = rp[3] = t : (rp[0] = t[0], rp[1] = t[1], rp[2] = t[2], rp[3] = t[3]), r && (rp[0] = Ma(0, rp[0]), rp[1] = Ma(0, rp[1]), rp[2] = Ma(0, rp[2]), rp[3] = Ma(0, rp[3])), n && (rp[0] = -rp[0], rp[1] = -rp[1], rp[2] = -rp[2], rp[3] = -rp[3]), ip(e, rp, "x", "width", 3, 1, i && i[0] || 0), ip(e, rp, "y", "height", 0, 2, i && i[1] || 0)), e;
}
var rp = [
	0,
	0,
	0,
	0
];
function ip(e, t, n, r, i, a, o) {
	var s = t[a] + t[i], c = e[r];
	e[r] += s, o = Ma(0, ja(o, c)), e[r] < o ? (e[r] = o, e[n] += t[i] >= 0 ? -t[i] : t[a] >= 0 ? c + t[a] : Na(s) > 1e-8 ? (c - o) * t[i] / s : 0) : e[n] -= t[i];
}
function ap(e) {
	var t = e.itemTooltipOption, n = e.componentModel, r = e.itemName, i = H(t) ? { formatter: t } : t, a = n.mainType, o = n.componentIndex, s = {
		componentType: a,
		name: r,
		$vars: ["name"]
	};
	s[a + "Index"] = o;
	var c = e.formatterParamsExtra;
	c && F(L(c), function(e) {
		ke(s, e) || (s[e] = c[e], s.$vars.push(e));
	});
	var l = Al(e.el);
	l.componentMainType = a, l.componentIndex = o, l.tooltipConfig = {
		name: r,
		option: M({
			content: r,
			encodeHTMLContent: !0,
			formatterParams: s
		}, i)
	};
}
function op(e, t) {
	var n;
	e.isGroup && (n = t(e)), n || e.traverse(t);
}
function sp(e, t) {
	if (e) {
		if (B(e)) for (var n = 0; n < e.length; n++) op(e[n], t);
		else op(e, t);
	}
}
function cp(e) {
	return !e || Na(e[1]) < lp && Na(e[2]) < lp || Na(e[0]) < lp && Na(e[3]) < lp;
}
var lp = 1e-5;
function up(e, t) {
	return e ? J.copy(e, t) : t.clone();
}
function dp(e, t) {
	return t ? jt(e || kt(), t) : void 0;
}
function fp(e) {
	return {
		z: e.get("z") || 0,
		zlevel: e.get("zlevel") || 0
	};
}
function pp(e) {
	var t = -Infinity, n = Infinity;
	op(e, function(e) {
		r(e), r(e.getTextContent()), r(e.getTextGuideLine());
	});
	function r(e) {
		if (e && !e.isGroup) {
			var t = e.currentStates;
			if (t.length) for (var n = 0; n < t.length; n++) i(e.states[t[n]]);
			i(e);
		}
	}
	function i(e) {
		if (e) {
			var r = e.z2;
			r > t && (t = r), r < n && (n = r);
		}
	}
	return n > t && (n = t = 0), {
		min: n,
		max: t
	};
}
function mp(e, t, n) {
	hp(e, t, n, -Infinity);
}
function hp(e, t, n, r) {
	if (e.ignoreModelZ) return r;
	var i = e.getTextContent(), a = e.getTextGuideLine();
	if (e.isGroup) for (var o = e.childrenRef(), s = 0; s < o.length; s++) r = Ma(hp(o[s], t, n, r), r);
	else e.z = t, e.zlevel = n, r = Ma(e.z2 || 0, r);
	if (i && (i.z = t, i.zlevel = n, isFinite(r) && (i.z2 = r + 2)), a) {
		var c = e.textGuideLineConfig;
		a.z = t, a.zlevel = n, isFinite(r) && (a.z2 = r + (c && c.showAbove ? 1 : -1));
	}
	return r;
}
function gp(e) {
	return e.animation = { duration: 0 }, e;
}
function _p(e, t) {
	return t ? jt(vp.transform, t) : At(vp.transform), vp.decomposeTransform(), Hi(e, vp), e;
}
var vp = new Ri();
vp.transform = kt();
function yp(e) {
	var t = e.getZr().painter;
	return t.getType() === "canvas" ? t : null;
}
Nf("circle", _d), Nf("ellipse", yd), Nf("sector", Id), Nf("ring", Rd), Nf("polygon", Hd), Nf("polyline", Wd), Nf("rect", dl), Nf("line", qd), Nf("bezierCurve", Zd), Nf("arc", $d);
//#endregion
//#region node_modules/echarts/lib/label/labelStyle.js
var bp = {};
function xp(e, t) {
	for (var n = 0; n < Yl.length; n++) {
		var r = Yl[n], i = t[r], a = e.ensureState(r);
		a.style = a.style || {}, a.style.text = i;
	}
	var o = e.currentStates.slice();
	e.clearStates(!0), e.setStyle({ text: t.normal }), e.useStates(o, !0);
}
function Sp(e, t, n) {
	var r = e.labelFetcher, i = e.labelDataIndex, a = e.labelDimIndex, o = t.normal, s;
	r && (s = r.getFormattedLabel(i, "normal", null, a, o && o.get("formatter"), n == null ? null : { interpolatedValue: n })), s ??= V(e.defaultText) ? e.defaultText(i, e, n) : e.defaultText;
	for (var c = { normal: s }, l = 0; l < Yl.length; l++) {
		var u = Yl[l], d = t[u];
		c[u] = G(r ? r.getFormattedLabel(i, u, null, a, d && d.get("formatter")) : null, s);
	}
	return c;
}
function Cp(e, t, n, r) {
	n ||= bp;
	for (var i = e instanceof gl, a = !1, o = 0; o < Xl.length; o++) {
		var s = t[Xl[o]];
		if (s && s.getShallow("show")) {
			a = !0;
			break;
		}
	}
	var c = i ? e : e.getTextContent();
	if (a) {
		i || (c || (c = new gl(), e.setTextContent(c)), e.stateProxy && (c.stateProxy = e.stateProxy));
		var l = Sp(n, t), u = t.normal, d = !!u.getShallow("show"), f = Tp(u, r && r.normal, n, !1, !i);
		f.text = l.normal, i || e.setTextConfig(Ep(u, n, !1));
		for (var o = 0; o < Yl.length; o++) {
			var p = Yl[o], s = t[p];
			if (s) {
				var m = c.ensureState(p), h = !!G(s.getShallow("show"), d);
				if (h !== d && (m.ignore = !h), m.style = Tp(s, r && r[p], n, !0, !i), m.style.text = l[p], !i) {
					var g = e.ensureState(p);
					g.textConfig = Ep(s, n, !0);
				}
			}
		}
		c.silent = !!u.getShallow("silent"), c.style.x != null && (f.x = c.style.x), c.style.y != null && (f.y = c.style.y), c.ignore = !d, c.useStyle(f), c.dirty(), n.enableTextSetter && (Pp(c).setLabelText = function(e) {
			var r = Sp(n, t, e);
			xp(c, r);
		});
	} else c && (c.ignore = !0);
	e.dirty();
}
function wp(e, t) {
	t ||= "label";
	for (var n = { normal: e.getModel(t) }, r = 0; r < Yl.length; r++) {
		var i = Yl[r];
		n[i] = e.getModel([i, t]);
	}
	return n;
}
function Tp(e, t, n, r, i) {
	var a = {};
	return Dp(a, e, n, r, i), t && j(a, t), a;
}
function Ep(e, t, n) {
	t ||= {};
	var r = {}, i, a = e.getShallow("rotate"), o = G(e.getShallow("distance"), n ? null : 5), s = e.getShallow("offset");
	return i = e.getShallow("position") || (n ? null : "inside"), i === "outside" && (i = t.defaultOutsidePosition || "top"), i != null && (r.position = i), s != null && (r.offset = s), a != null && (a *= Math.PI / 180, r.rotation = a), o != null && (r.distance = o), r.outsideFill = e.get("color") === "inherit" ? t.inheritColor || null : "auto", t.autoOverflowArea != null && (r.autoOverflowArea = t.autoOverflowArea), t.layoutRect != null && (r.layoutRect = t.layoutRect), r;
}
function Dp(e, t, n, r, i) {
	n ||= bp;
	var a = t.ecModel, o = a && a.option.textStyle, s = Op(t), c;
	if (s) {
		c = {};
		var l = "richInheritPlainLabel", u = G(t.get(l), a ? a.get(l) : void 0);
		for (var d in s) if (s.hasOwnProperty(d)) {
			var f = t.getModel(["rich", d]);
			Mp(c[d] = {}, f, o, t, u, n, r, i, !1, !0);
		}
	}
	c && (e.rich = c);
	var p = t.get("overflow");
	p && (e.overflow = p);
	var m = t.get("lineOverflow");
	m && (e.lineOverflow = m);
	var h = e, g = t.get("minMargin");
	if (g != null) g = U(g) ? g / 2 : 0, h.margin = [
		g,
		g,
		g,
		g
	], h.__marginType = Fp.minMargin;
	else {
		var _ = t.get("textMargin");
		_ != null && (h.margin = ge(_), h.__marginType = Fp.textMargin);
	}
	Mp(e, t, o, null, null, n, r, i, !0, !1);
}
function Op(e) {
	for (var t; e && e !== e.ecModel;) {
		var n = (e.option || bp).rich;
		if (n) {
			t ||= {};
			for (var r = L(n), i = 0; i < r.length; i++) {
				var a = r[i];
				t[a] = 1;
			}
		}
		e = e.parentModel;
	}
	return t;
}
var kp = [
	"fontStyle",
	"fontWeight",
	"fontSize",
	"fontFamily",
	"textShadowColor",
	"textShadowBlur",
	"textShadowOffsetX",
	"textShadowOffsetY"
], Ap = [
	"align",
	"lineHeight",
	"width",
	"height",
	"tag",
	"verticalAlign",
	"ellipsis"
], jp = [
	"padding",
	"borderWidth",
	"borderRadius",
	"borderDashOffset",
	"backgroundColor",
	"borderColor",
	"shadowColor",
	"shadowBlur",
	"shadowOffsetX",
	"shadowOffsetY"
];
function Mp(e, t, n, r, i, a, o, s, c, l) {
	n = !o && n || bp;
	var u = a && a.inheritColor, d = t.getShallow("color"), f = t.getShallow("textBorderColor"), p = G(t.getShallow("opacity"), n.opacity);
	(d === "inherit" || d === "auto") && (d = u || null), (f === "inherit" || f === "auto") && (f = u || null), s || (d ||= n.color, f ||= n.textBorderColor), d != null && (e.fill = d), f != null && (e.stroke = f);
	var m = G(t.getShallow("textBorderWidth"), n.textBorderWidth);
	m != null && (e.lineWidth = m);
	var h = G(t.getShallow("textBorderType"), n.textBorderType);
	h != null && (e.lineDash = h);
	var g = G(t.getShallow("textBorderDashOffset"), n.textBorderDashOffset);
	g != null && (e.lineDashOffset = g), !o && p == null && !l && (p = a && a.defaultOpacity), p != null && (e.opacity = p), !o && !s && e.fill == null && a.inheritColor && (e.fill = a.inheritColor);
	for (var _ = 0; _ < kp.length; _++) {
		var v = kp[_], y = i !== !1 && r ? me(t.getShallow(v), r.getShallow(v), n[v]) : G(t.getShallow(v), n[v]);
		y != null && (e[v] = y);
	}
	for (var _ = 0; _ < Ap.length; _++) {
		var v = Ap[_], y = t.getShallow(v);
		y != null && (e[v] = y);
	}
	if (e.verticalAlign == null) {
		var b = t.getShallow("baseline");
		b != null && (e.verticalAlign = b);
	}
	if (!c || !a.disableBox) {
		for (var _ = 0; _ < jp.length; _++) {
			var v = jp[_], y = t.getShallow(v);
			y != null && (e[v] = y);
		}
		var x = t.getShallow("borderType");
		x != null && (e.borderDash = x), (e.backgroundColor === "auto" || e.backgroundColor === "inherit") && u && (e.backgroundColor = u), (e.borderColor === "auto" || e.borderColor === "inherit") && u && (e.borderColor = u);
	}
}
function Np(e, t) {
	var n = t && t.getModel("textStyle");
	return ve([
		e.fontStyle || n && n.getShallow("fontStyle") || "",
		e.fontWeight || n && n.getShallow("fontWeight") || "",
		(e.fontSize || n && n.getShallow("fontSize") || 12) + "px",
		e.fontFamily || n && n.getShallow("fontFamily") || "sans-serif"
	].join(" "));
}
var Pp = Y(), Fp = {
	minMargin: 1,
	textMargin: 2
}, Ip = ["textStyle", "color"], Lp = [
	"fontStyle",
	"fontWeight",
	"fontSize",
	"fontFamily",
	"padding",
	"lineHeight",
	"rich",
	"width",
	"height",
	"overflow"
], Rp = new gl(), zp = function() {
	function e() {}
	return e.prototype.getTextColor = function(e) {
		var t = this.ecModel;
		return this.getShallow("color") || (!e && t ? t.get(Ip) : null);
	}, e.prototype.getFont = function() {
		return Np({
			fontStyle: this.getShallow("fontStyle"),
			fontWeight: this.getShallow("fontWeight"),
			fontSize: this.getShallow("fontSize"),
			fontFamily: this.getShallow("fontFamily")
		}, this.ecModel);
	}, e.prototype.getTextRect = function(e) {
		for (var t = {
			text: e,
			verticalAlign: this.getShallow("verticalAlign") || this.getShallow("baseline")
		}, n = 0; n < Lp.length; n++) t[Lp[n]] = this.getShallow(Lp[n]);
		return Rp.useStyle(t), Rp.update(), Rp.getBoundingRect();
	}, e;
}(), Bp = [
	["lineWidth", "width"],
	["stroke", "color"],
	["opacity"],
	["shadowBlur"],
	["shadowOffsetX"],
	["shadowOffsetY"],
	["shadowColor"],
	["lineDash", "type"],
	["lineDashOffset", "dashOffset"],
	["lineCap", "cap"],
	["lineJoin", "join"],
	["miterLimit"]
], Vp = us(Bp), Hp = function() {
	function e() {}
	return e.prototype.getLineStyle = function(e) {
		return Vp(this, e);
	}, e;
}(), Up = [
	["fill", "color"],
	["stroke", "borderColor"],
	["lineWidth", "borderWidth"],
	["opacity"],
	["shadowBlur"],
	["shadowOffsetX"],
	["shadowOffsetY"],
	["shadowColor"],
	["lineDash", "borderType"],
	["lineDashOffset", "borderDashOffset"],
	["lineCap", "borderCap"],
	["lineJoin", "borderJoin"],
	["miterLimit", "borderMiterLimit"]
], Wp = us(Up), Gp = function() {
	function e() {}
	return e.prototype.getItemStyle = function(e, t) {
		return Wp(this, e, t);
	}, e;
}(), Kp = function() {
	function e(e, t, n) {
		this.parentModel = t, this.ecModel = n, this.option = e;
	}
	return e.prototype.init = function(e, t, n) {}, e.prototype.mergeOption = function(e, t) {
		A(this.option, e, !0);
	}, e.prototype.get = function(e, t) {
		return e == null ? this.option : this._doGet(this.parsePath(e), !t && this.parentModel);
	}, e.prototype.getShallow = function(e, t) {
		var n = this.option, r = n == null ? n : n[e];
		if (r == null && !t) {
			var i = this.parentModel;
			i && (r = i.getShallow(e));
		}
		return r;
	}, e.prototype.getModel = function(t, n) {
		var r = t != null, i = r ? this.parsePath(t) : null, a = r ? this._doGet(i) : this.option;
		return n ||= this.parentModel && this.parentModel.getModel(this.resolveParentPath(i)), new e(a, n, this.ecModel);
	}, e.prototype.isEmpty = function() {
		return this.option == null;
	}, e.prototype.restoreData = function() {}, e.prototype.clone = function() {
		var e = this.constructor;
		return new e(k(this.option));
	}, e.prototype.parsePath = function(e) {
		return typeof e == "string" ? e.split(".") : e;
	}, e.prototype.resolveParentPath = function(e) {
		return e;
	}, e.prototype.isAnimationEnabled = function() {
		if (!a.node && this.option) {
			if (this.option.animation != null) return !!this.option.animation;
			if (this.parentModel) return this.parentModel.isAnimationEnabled();
		}
	}, e.prototype._doGet = function(e, t) {
		var n = this.option;
		if (!e) return n;
		for (var r = 0; r < e.length && !(e[r] && (n = n && typeof n == "object" ? n[e[r]] : null, n == null)); r++);
		return n == null && t && (n = t._doGet(this.resolveParentPath(e), t.parentModel)), n;
	}, e;
}();
ns(Kp), os(Kp), ne(Kp, Hp), ne(Kp, Gp), ne(Kp, fs), ne(Kp, zp);
//#endregion
//#region node_modules/echarts/lib/util/component.js
var qp = Math.round(Math.random() * 10);
function Jp(e) {
	return [e || "", qp++].join("_");
}
function Yp(e) {
	var t = {};
	e.registerSubTypeDefaulter = function(e, n) {
		var r = $o(e);
		t[r.main] = n;
	}, e.determineSubType = function(n, r) {
		var i = r.type;
		if (!i) {
			var a = $o(n).main;
			e.hasSubTypes(n) && t[a] && (i = t[a](r));
		}
		return i;
	};
}
function Xp(e, t) {
	e.topologicalTravel = function(e, t, r, i) {
		if (!e.length) return;
		var a = n(t), o = a.graph, s = a.noEntryList, c = {};
		for (F(e, function(e) {
			c[e] = !0;
		}); s.length;) {
			var l = s.pop(), u = o[l], d = !!c[l];
			d && (r.call(i, l, u.originalDeps.slice()), delete c[l]), F(u.successor, d ? p : f);
		}
		F(c, function() {
			throw Error("");
		});
		function f(e) {
			o[e].entryCount--, o[e].entryCount === 0 && s.push(e);
		}
		function p(e) {
			c[e] = !0, f(e);
		}
	};
	function n(e) {
		var n = {}, a = [];
		return F(e, function(o) {
			var s = r(n, o), c = i(s.originalDeps = t(o), e);
			s.entryCount = c.length, s.entryCount === 0 && a.push(o), F(c, function(e) {
				N(s.predecessor, e) < 0 && s.predecessor.push(e);
				var t = r(n, e);
				N(t.successor, e) < 0 && t.successor.push(o);
			});
		}), {
			graph: n,
			noEntryList: a
		};
	}
	function r(e, t) {
		return e[t] || (e[t] = {
			predecessor: [],
			successor: []
		}), e[t];
	}
	function i(e, t) {
		var n = [];
		return F(e, function(e) {
			N(t, e) >= 0 && n.push(e);
		}), n;
	}
}
//#endregion
//#region node_modules/echarts/lib/i18n/langEN.js
var Zp = {
	time: {
		month: [
			"January",
			"February",
			"March",
			"April",
			"May",
			"June",
			"July",
			"August",
			"September",
			"October",
			"November",
			"December"
		],
		monthAbbr: [
			"Jan",
			"Feb",
			"Mar",
			"Apr",
			"May",
			"Jun",
			"Jul",
			"Aug",
			"Sep",
			"Oct",
			"Nov",
			"Dec"
		],
		dayOfWeek: [
			"Sunday",
			"Monday",
			"Tuesday",
			"Wednesday",
			"Thursday",
			"Friday",
			"Saturday"
		],
		dayOfWeekAbbr: [
			"Sun",
			"Mon",
			"Tue",
			"Wed",
			"Thu",
			"Fri",
			"Sat"
		]
	},
	legend: { selector: {
		all: "All",
		inverse: "Inv"
	} },
	toolbox: {
		brush: { title: {
			rect: "Box Select",
			polygon: "Lasso Select",
			lineX: "Horizontally Select",
			lineY: "Vertically Select",
			keep: "Keep Selections",
			clear: "Clear Selections"
		} },
		dataView: {
			title: "Data View",
			lang: [
				"Data View",
				"Close",
				"Refresh"
			]
		},
		dataZoom: { title: {
			zoom: "Zoom",
			back: "Zoom Reset"
		} },
		magicType: { title: {
			line: "Switch to Line Chart",
			bar: "Switch to Bar Chart",
			stack: "Stack",
			tiled: "Tile"
		} },
		restore: { title: "Restore" },
		saveAsImage: {
			title: "Save as Image",
			lang: ["Right Click to Save Image"]
		}
	},
	series: { typeNames: {
		pie: "Pie chart",
		bar: "Bar chart",
		line: "Line chart",
		scatter: "Scatter plot",
		effectScatter: "Ripple scatter plot",
		radar: "Radar chart",
		tree: "Tree",
		treemap: "Treemap",
		boxplot: "Boxplot",
		candlestick: "Candlestick",
		k: "K line chart",
		heatmap: "Heat map",
		map: "Map",
		parallel: "Parallel coordinate map",
		lines: "Line graph",
		graph: "Relationship graph",
		sankey: "Sankey diagram",
		funnel: "Funnel chart",
		gauge: "Gauge",
		pictorialBar: "Pictorial bar",
		themeRiver: "Theme River Map",
		sunburst: "Sunburst",
		custom: "Custom chart",
		chart: "Chart"
	} },
	aria: {
		general: {
			withTitle: "This is a chart about \"{title}\"",
			withoutTitle: "This is a chart"
		},
		series: {
			single: {
				prefix: "",
				withName: " with type {seriesType} named {seriesName}.",
				withoutName: " with type {seriesType}."
			},
			multiple: {
				prefix: ". It consists of {seriesCount} series count.",
				withName: " The {seriesId} series is a {seriesType} representing {seriesName}.",
				withoutName: " The {seriesId} series is a {seriesType}.",
				separator: {
					middle: "",
					end: ""
				}
			}
		},
		data: {
			allData: "The data is as follows: ",
			partialData: "The first {displayCnt} items are: ",
			withName: "the data for {name} is {value}",
			withoutName: "{value}",
			separator: {
				middle: ", ",
				end: ". "
			}
		}
	}
}, Qp = {
	time: {
		month: [
			"一月",
			"二月",
			"三月",
			"四月",
			"五月",
			"六月",
			"七月",
			"八月",
			"九月",
			"十月",
			"十一月",
			"十二月"
		],
		monthAbbr: [
			"1月",
			"2月",
			"3月",
			"4月",
			"5月",
			"6月",
			"7月",
			"8月",
			"9月",
			"10月",
			"11月",
			"12月"
		],
		dayOfWeek: [
			"星期日",
			"星期一",
			"星期二",
			"星期三",
			"星期四",
			"星期五",
			"星期六"
		],
		dayOfWeekAbbr: [
			"日",
			"一",
			"二",
			"三",
			"四",
			"五",
			"六"
		]
	},
	legend: { selector: {
		all: "全选",
		inverse: "反选"
	} },
	toolbox: {
		brush: { title: {
			rect: "矩形选择",
			polygon: "圈选",
			lineX: "横向选择",
			lineY: "纵向选择",
			keep: "保持选择",
			clear: "清除选择"
		} },
		dataView: {
			title: "数据视图",
			lang: [
				"数据视图",
				"关闭",
				"刷新"
			]
		},
		dataZoom: { title: {
			zoom: "区域缩放",
			back: "区域缩放还原"
		} },
		magicType: { title: {
			line: "切换为折线图",
			bar: "切换为柱状图",
			stack: "切换为堆叠",
			tiled: "切换为平铺"
		} },
		restore: { title: "还原" },
		saveAsImage: {
			title: "保存为图片",
			lang: ["右键另存为图片"]
		}
	},
	series: { typeNames: {
		pie: "饼图",
		bar: "柱状图",
		line: "折线图",
		scatter: "散点图",
		effectScatter: "涟漪散点图",
		radar: "雷达图",
		tree: "树图",
		treemap: "矩形树图",
		boxplot: "箱型图",
		candlestick: "K线图",
		k: "K线图",
		heatmap: "热力图",
		map: "地图",
		parallel: "平行坐标图",
		lines: "线图",
		graph: "关系图",
		sankey: "桑基图",
		funnel: "漏斗图",
		gauge: "仪表盘图",
		pictorialBar: "象形柱图",
		themeRiver: "主题河流图",
		sunburst: "旭日图",
		custom: "自定义图表",
		chart: "图表"
	} },
	aria: {
		general: {
			withTitle: "这是一个关于“{title}”的图表。",
			withoutTitle: "这是一个图表，"
		},
		series: {
			single: {
				prefix: "",
				withName: "图表类型是{seriesType}，表示{seriesName}。",
				withoutName: "图表类型是{seriesType}。"
			},
			multiple: {
				prefix: "它由{seriesCount}个图表系列组成。",
				withName: "第{seriesId}个系列是一个表示{seriesName}的{seriesType}，",
				withoutName: "第{seriesId}个系列是一个{seriesType}，",
				separator: {
					middle: "；",
					end: "。"
				}
			}
		},
		data: {
			allData: "其数据是——",
			partialData: "其中，前{displayCnt}项是——",
			withName: "{name}的数据是{value}",
			withoutName: "{value}",
			separator: {
				middle: "，",
				end: ""
			}
		}
	}
}, $p = "ZH", em = "EN", tm = em, nm = {}, rm = {}, im = a.domSupported ? function() {
	return (document.documentElement.lang || navigator.language || navigator.browserLanguage || tm).toUpperCase().indexOf($p) > -1 ? $p : tm;
}() : tm;
function am(e, t) {
	e = e.toUpperCase(), rm[e] = new Kp(t), nm[e] = t;
}
function om(e) {
	if (H(e)) {
		var t = nm[e.toUpperCase()] || {};
		return e === $p || e === em ? k(t) : A(k(t), k(nm[tm]), !1);
	}
	return A(k(e), k(nm[tm]), !1);
}
function sm(e) {
	return rm[e];
}
function cm() {
	return rm[tm];
}
am(em, Zp), am($p, Qp);
//#endregion
//#region node_modules/echarts/lib/scale/break.js
var lm = null;
function um() {
	return lm;
}
function dm(e) {
	var t = e.brk;
	return t ? t.hasBreaks() : !1;
}
var fm = {
	year: "{yyyy}",
	month: "{MMM}",
	day: "{d}",
	hour: "{HH}:{mm}",
	minute: "{HH}:{mm}",
	second: "{HH}:{mm}:{ss}",
	millisecond: "{HH}:{mm}:{ss} {SSS}"
}, pm = "{yyyy}-{MM}-{dd}";
pm + "" + fm.hour, pm + "" + fm.minute, pm + "" + fm.second;
function mm(e, t) {
	return e += "", "0000".substr(0, t - e.length) + e;
}
function hm(e, t, n, r) {
	var i = Qa(e), a = i[gm(n)](), o = i[_m(n)]() + 1, s = Math.floor((o - 1) / 3) + 1, c = i[vm(n)](), l = i["get" + (n ? "UTC" : "") + "Day"](), u = i[ym(n)](), d = (u - 1) % 12 + 1, f = i[bm(n)](), p = i[xm(n)](), m = i[Sm(n)](), h = u >= 12 ? "pm" : "am", g = h.toUpperCase(), _ = (r instanceof Kp ? r : sm(r || im) || cm()).getModel("time"), v = _.get("month"), y = _.get("monthAbbr"), b = _.get("dayOfWeek"), x = _.get("dayOfWeekAbbr");
	return (t || "").replace(/{a}/g, h + "").replace(/{A}/g, g + "").replace(/{yyyy}/g, a + "").replace(/{yy}/g, mm(a % 100 + "", 2)).replace(/{Q}/g, s + "").replace(/{MMMM}/g, v[o - 1]).replace(/{MMM}/g, y[o - 1]).replace(/{MM}/g, mm(o, 2)).replace(/{M}/g, o + "").replace(/{dd}/g, mm(c, 2)).replace(/{d}/g, c + "").replace(/{eeee}/g, b[l]).replace(/{ee}/g, x[l]).replace(/{e}/g, l + "").replace(/{HH}/g, mm(u, 2)).replace(/{H}/g, u + "").replace(/{hh}/g, mm(d + "", 2)).replace(/{h}/g, d + "").replace(/{mm}/g, mm(f, 2)).replace(/{m}/g, f + "").replace(/{ss}/g, mm(p, 2)).replace(/{s}/g, p + "").replace(/{SSS}/g, mm(m, 3)).replace(/{S}/g, m + "");
}
function gm(e) {
	return e ? "getUTCFullYear" : "getFullYear";
}
function _m(e) {
	return e ? "getUTCMonth" : "getMonth";
}
function vm(e) {
	return e ? "getUTCDate" : "getDate";
}
function ym(e) {
	return e ? "getUTCHours" : "getHours";
}
function bm(e) {
	return e ? "getUTCMinutes" : "getMinutes";
}
function xm(e) {
	return e ? "getUTCSeconds" : "getSeconds";
}
function Sm(e) {
	return e ? "getUTCMilliseconds" : "getMilliseconds";
}
//#endregion
//#region node_modules/echarts/lib/util/format.js
function Cm(e) {
	if (!eo(e)) return H(e) ? e : "-";
	var t = (e + "").split(".");
	return t[0].replace(/(\d{1,3})(?=(?:\d{3})+(?!\d))/g, "$1,") + (t.length > 1 ? "." + t[1] : "");
}
function wm(e, t) {
	return e = (e || "").toLowerCase().replace(/-(.)/g, function(e, t) {
		return t.toUpperCase();
	}), t && e && (e = e.charAt(0).toUpperCase() + e.slice(1)), e;
}
var Tm = ge;
function Em(e, t, n) {
	var r = "{yyyy}-{MM}-{dd} {HH}:{mm}:{ss}";
	function i(e) {
		return e && ve(e) ? e : "-";
	}
	function a(e) {
		return io(e);
	}
	var o = t === "time", s = e instanceof Date;
	if (o || s) {
		var c = o ? Qa(e) : e;
		if (!isNaN(+c)) return hm(c, r, n);
		if (s) return "-";
	}
	if (t === "ordinal") return oe(e) ? i(e) : U(e) && a(e) ? e + "" : "-";
	var l = $a(e);
	return a(l) ? Cm(l) : oe(e) ? i(e) : typeof e == "boolean" ? e + "" : "-";
}
var Dm = [
	"a",
	"b",
	"c",
	"d",
	"e",
	"f",
	"g"
], Om = function(e, t) {
	return "{" + e + (t ?? "") + "}";
};
function km(e, t, n) {
	B(t) || (t = [t]);
	var r = t.length;
	if (!r) return "";
	for (var i = t[0].$vars || [], a = 0; a < i.length; a++) {
		var o = Dm[a];
		e = e.replace(Om(o), Om(o, 0));
	}
	for (var s = 0; s < r; s++) for (var c = 0; c < i.length; c++) {
		var l = t[s][i[c]];
		e = e.replace(Om(Dm[c], s), n ? ft(l) : l);
	}
	return e;
}
function Am(e, t) {
	var n = H(e) ? {
		color: e,
		extraCssText: t
	} : e || {}, r = n.color, i = n.type;
	t = n.extraCssText;
	var a = n.renderMode || "html";
	return r ? a === "html" ? i === "subItem" ? "<span style=\"display:inline-block;vertical-align:middle;margin-right:8px;margin-left:3px;border-radius:4px;width:4px;height:4px;background-color:" + ft(r) + ";" + (t || "") + "\"></span>" : "<span style=\"display:inline-block;margin-right:4px;border-radius:10px;width:10px;height:10px;background-color:" + ft(r) + ";" + (t || "") + "\"></span>" : {
		renderMode: a,
		content: "{" + (n.markerId || "markerX") + "|}  ",
		style: i === "subItem" ? {
			width: 4,
			height: 4,
			borderRadius: 2,
			backgroundColor: r
		} : {
			width: 10,
			height: 10,
			borderRadius: 5,
			backgroundColor: r
		}
	} : "";
}
function jm(e, t) {
	return t ||= "transparent", H(e) ? e : W(e) && e.colorStops && (e.colorStops[0] || {}).color || t;
}
//#endregion
//#region node_modules/echarts/lib/core/CoordinateSystem.js
var Mm = {}, Nm = {}, Pm = function() {
	function e() {
		this._normalMasterList = [], this._nonSeriesBoxMasterList = [];
	}
	return e.prototype.create = function(e, t) {
		this._nonSeriesBoxMasterList = n(Mm, !0), this._normalMasterList = n(Nm, !1);
		function n(n, r) {
			var i = [];
			return F(n, function(n, r) {
				var a = n.create(e, t);
				i = i.concat(a || []);
			}), i;
		}
	}, e.prototype.update = function(e, t) {
		F(this._normalMasterList, function(n) {
			n.update && n.update(e, t);
		});
	}, e.prototype.getCoordinateSystems = function() {
		return this._normalMasterList.concat(this._nonSeriesBoxMasterList);
	}, e.register = function(e, t) {
		e === "matrix" || e === "calendar" ? Mm[e] = t : Nm[e] = t;
	}, e.get = function(e) {
		return Nm[e] || Mm[e];
	}, e;
}();
function Fm(e) {
	return !!Mm[e];
}
var Im = K();
function Lm(e) {
	var t = e.getShallow("coord", !0), n = 1;
	if (t == null) {
		var r = Im.get(e.type);
		r && r.getCoord2 && (n = 2, t = r.getCoord2(e));
	}
	return {
		coord: t,
		from: n
	};
}
function Rm(e, t) {
	var n = e.getShallow("coordinateSystem"), r = e.getShallow("coordinateSystemUsage", !0), i = 0;
	if (n) {
		var a = e.mainType === "series";
		r ??= a ? "data" : "box", r === "data" ? (i = 1, a || (i = 0)) : r === "box" && (i = 2, !a && !Fm(n) && (i = 0));
	}
	return {
		coordSysType: n,
		kind: i
	};
}
function zm(e) {
	var t = e.targetModel, n = e.coordSysType, r = e.coordSysProvider, i = e.isDefaultDataCoordSys;
	e.allowNotFound;
	var a = Rm(t, !0), o = a.kind, s = a.coordSysType;
	if (i && o !== 1 && (o = 1, s = n), o === 0 || s !== n) return 0;
	var c = r(n, t);
	return c ? (o === 1 ? t.coordinateSystem = c : t.boxCoordinateSystem = c, o) : 0;
}
//#endregion
//#region node_modules/echarts/lib/util/layout.js
var Bm = F, Vm = [
	"left",
	"right",
	"top",
	"bottom",
	"width",
	"height"
], Hm = [[
	"width",
	"left",
	"right"
], [
	"height",
	"top",
	"bottom"
]];
function Um(e, t, n, r, i) {
	var a = 0, o = 0;
	r ??= Infinity, i ??= Infinity;
	var s = 0;
	t.eachChild(function(c, l) {
		var u = c.getBoundingRect(), d = t.childAt(l + 1), f = d && d.getBoundingRect(), p, m;
		if (e === "horizontal") {
			var h = u.width + (f ? -f.x + u.x : 0);
			p = a + h, p > r || c.newline ? (a = 0, p = h, o += s + n, s = u.height) : s = Math.max(s, u.height);
		} else {
			var g = u.height + (f ? -f.y + u.y : 0);
			m = o + g, m > i || c.newline ? (a += s + n, o = 0, m = g, s = u.width) : s = Math.max(s, u.width);
		}
		c.newline || (c.x = a, c.y = o, c.markRedraw(), e === "horizontal" ? a = p + n : o = m + n);
	});
}
z(Um, "vertical"), z(Um, "horizontal");
function Wm(e, t) {
	return {
		left: e.getShallow("left", t),
		top: e.getShallow("top", t),
		right: e.getShallow("right", t),
		bottom: e.getShallow("bottom", t),
		width: e.getShallow("width", t),
		height: e.getShallow("height", t)
	};
}
function Gm(e, t, n) {
	n = Tm(n || 0);
	var r = t.width, i = t.height, a = za(e.left, r), o = za(e.top, i), s = za(e.right, r), c = za(e.bottom, i), l = za(e.width, r), u = za(e.height, i), d = n[2] + n[0], f = n[1] + n[3], p = e.aspect;
	switch (isNaN(l) && (l = r - s - f - a), isNaN(u) && (u = i - c - d - o), p != null && (isNaN(l) && isNaN(u) && (p > r / i ? l = r * .8 : u = i * .8), isNaN(l) && (l = p * u), isNaN(u) && (u = l / p)), isNaN(a) && (a = r - s - l - f), isNaN(o) && (o = i - c - u - d), e.left || e.right) {
		case "center":
			a = r / 2 - l / 2 - n[3];
			break;
		case "right": a = r - l - f;
	}
	switch (e.top || e.bottom) {
		case "middle":
		case "center":
			o = i / 2 - u / 2 - n[0];
			break;
		case "bottom": o = i - u - d;
	}
	a ||= 0, o ||= 0, isNaN(l) && (l = r - f - a - (s || 0)), isNaN(u) && (u = i - d - o - (c || 0));
	var m = new J((t.x || 0) + a + n[3], (t.y || 0) + o + n[0], l, u);
	return m.margin = n, m;
}
function Km(e, t, n) {
	var r = e.getShallow("preserveAspect", !0);
	if (!r) return t;
	var i = t.width / t.height;
	if (Math.abs(Math.atan(n) - Math.atan(i)) < 1e-9) return t;
	var a = e.getShallow("preserveAspectAlign", !0), o = e.getShallow("preserveAspectVerticalAlign", !0), s = {
		width: t.width,
		height: t.height
	}, c = r === "cover";
	return i > n && !c || i < n && c ? (s.width = t.height * n, a === "left" ? s.left = 0 : a === "right" ? s.right = 0 : s.left = "center") : (s.height = t.width / n, o === "top" ? s.top = 0 : o === "bottom" ? s.bottom = 0 : s.top = "middle"), Gm(s, t);
}
var qm = {
	rect: 1,
	point: 2
};
function Jm(e, t, n) {
	var r, i, a, o = e.boxCoordinateSystem, s;
	if (o) {
		var c = Lm(e), l = c.coord, u = c.from;
		if (o.dataToLayout) {
			a = qm.rect, s = u;
			var d = o.dataToLayout(l);
			r = d.contentRect || d.rect;
		} else n && n.enableLayoutOnlyByCenter && o.dataToPoint && (a = qm.point, s = u, i = o.dataToPoint(l));
	}
	return a ??= qm.rect, a === qm.rect && (r ||= {
		x: 0,
		y: 0,
		width: t.getWidth(),
		height: t.getHeight()
	}, i = [r.x + r.width / 2, r.y + r.height / 2]), {
		type: a,
		refContainer: r,
		refPoint: i,
		boxCoordFrom: s
	};
}
function Ym(e) {
	var t = e.layoutMode || e.constructor.layoutMode;
	return W(t) ? t : t ? { type: t } : null;
}
function Xm(e, t, n) {
	var r = n && n.ignoreSize;
	!B(r) && (r = [r, r]);
	var i = o(Hm[0], 0), a = o(Hm[1], 1);
	c(Hm[0], e, i), c(Hm[1], e, a);
	function o(n, i) {
		var a = {}, o = 0, c = {}, l = 0, u = 2;
		if (Bm(n, function(t) {
			c[t] = e[t];
		}), Bm(n, function(e) {
			ke(t, e) && (a[e] = c[e] = t[e]), s(a, e) && o++, s(c, e) && l++;
		}), r[i]) return s(t, n[1]) ? c[n[2]] = null : s(t, n[2]) && (c[n[1]] = null), c;
		if (l === u || !o) return c;
		if (o >= u) return a;
		for (var d = 0; d < n.length; d++) {
			var f = n[d];
			if (!ke(a, f) && ke(e, f)) {
				a[f] = e[f];
				break;
			}
		}
		return a;
	}
	function s(e, t) {
		return e[t] != null && e[t] !== "auto";
	}
	function c(e, t, n) {
		Bm(e, function(e) {
			t[e] = n[e];
		});
	}
}
function Zm(e) {
	return Qm({}, e);
}
function Qm(e, t) {
	return t && e && Bm(Vm, function(n) {
		ke(t, n) && (e[n] = t[n]);
	}), e;
}
//#endregion
//#region node_modules/echarts/lib/model/Component.js
var $m = Y(), eh = function(e) {
	r(t, e);
	function t(t, n, r) {
		var i = e.call(this, t, n, r) || this;
		return i.uid = Jp("ec_cpt_model"), i;
	}
	return t.prototype.init = function(e, t, n) {
		this.mergeDefaultAndTheme(e, n);
	}, t.prototype.mergeDefaultAndTheme = function(e, t) {
		var n = Ym(this), r = n ? Zm(e) : {};
		A(e, t.getTheme().get(this.mainType)), A(e, this.getDefaultOption()), n && Xm(e, r, n);
	}, t.prototype.mergeOption = function(e, t) {
		A(this.option, e, !0);
		var n = Ym(this);
		n && Xm(this.option, e, n);
	}, t.prototype.optionUpdated = function(e, t) {}, t.prototype.getDefaultOption = function() {
		var e = this.constructor;
		if (!ts(e)) return e.defaultOption;
		var t = $m(this);
		if (!t.defaultOption) {
			for (var n = [], r = e; r;) {
				var i = r.prototype.defaultOption;
				i && n.push(i), r = r.superClass;
			}
			for (var a = {}, o = n.length - 1; o >= 0; o--) a = A(a, n[o], !0);
			t.defaultOption = a;
		}
		return t.defaultOption;
	}, t.prototype.getReferringComponents = function(e, t) {
		var n = e + "Index", r = e + "Id";
		return Ro(this.ecModel, e, {
			index: this.get(n, !0),
			id: this.get(r, !0)
		}, t);
	}, t.prototype.getBoxLayoutParams = function() {
		return Wm(this, !1);
	}, t.prototype.getZLevelKey = function() {
		return "";
	}, t.prototype.setZLevel = function(e) {
		this.option.zlevel = e;
	}, t.protoInitialize = function() {
		var e = t.prototype;
		e.type = "component", e.id = "", e.name = "", e.mainType = "", e.subType = "", e.componentIndex = 0;
	}(), t;
}(Kp);
is(eh, Kp), ls(eh), Yp(eh), Xp(eh, th);
function th(e) {
	var t = [];
	return F(eh.getClassesByMainType(e), function(e) {
		t = t.concat(e.dependencies || e.prototype.dependencies || []);
	}), t = I(t, function(e) {
		return $o(e).main;
	}), e !== "dataset" && N(t, "dataset") <= 0 && t.unshift("dataset"), t;
}
//#endregion
//#region node_modules/echarts/lib/visual/tokens.js
var Q = {
	color: {},
	darkColor: {},
	size: {}
}, nh = Q.color = {
	theme: [
		"#5070dd",
		"#b6d634",
		"#505372",
		"#ff994d",
		"#0ca8df",
		"#ffd10a",
		"#fb628b",
		"#785db0",
		"#3fbe95"
	],
	neutral00: "#fff",
	neutral05: "#f4f7fd",
	neutral10: "#e8ebf0",
	neutral15: "#dbdee4",
	neutral20: "#cfd2d7",
	neutral25: "#c3c5cb",
	neutral30: "#b7b9be",
	neutral35: "#aaacb2",
	neutral40: "#9ea0a5",
	neutral45: "#929399",
	neutral50: "#86878c",
	neutral55: "#797b7f",
	neutral60: "#6d6e73",
	neutral65: "#616266",
	neutral70: "#54555a",
	neutral75: "#48494d",
	neutral80: "#3c3c41",
	neutral85: "#303034",
	neutral90: "#232328",
	neutral95: "#17171b",
	neutral99: "#000",
	accent05: "#eff1f9",
	accent10: "#e0e4f2",
	accent15: "#d0d6ec",
	accent20: "#c0c9e6",
	accent25: "#b1bbdf",
	accent30: "#a1aed9",
	accent35: "#91a0d3",
	accent40: "#8292cc",
	accent45: "#7285c6",
	accent50: "#6578ba",
	accent55: "#5c6da9",
	accent60: "#536298",
	accent65: "#4a5787",
	accent70: "#404c76",
	accent75: "#374165",
	accent80: "#2e3654",
	accent85: "#252b43",
	accent90: "#1b2032",
	accent95: "#121521",
	transparent: "rgba(0,0,0,0)",
	highlight: "rgba(255,231,130,0.8)"
};
for (var rh in j(nh, {
	primary: nh.neutral80,
	secondary: nh.neutral70,
	tertiary: nh.neutral60,
	quaternary: nh.neutral50,
	disabled: nh.neutral20,
	border: nh.neutral30,
	borderTint: nh.neutral20,
	borderShade: nh.neutral40,
	background: nh.neutral05,
	backgroundTint: "rgba(234,237,245,0.5)",
	backgroundTransparent: "rgba(255,255,255,0)",
	backgroundShade: nh.neutral10,
	shadow: "rgba(0,0,0,0.2)",
	shadowTint: "rgba(129,130,136,0.2)",
	axisLine: nh.neutral70,
	axisLineTint: nh.neutral40,
	axisTick: nh.neutral70,
	axisTickMinor: nh.neutral60,
	axisLabel: nh.neutral70,
	axisSplitLine: nh.neutral15,
	axisMinorSplitLine: nh.neutral05
}), nh) if (nh.hasOwnProperty(rh)) {
	var ih = nh[rh];
	rh === "theme" ? Q.darkColor.theme = nh.theme.slice() : rh === "highlight" ? Q.darkColor.highlight = "rgba(255,231,130,0.4)" : rh.indexOf("accent") === 0 ? Q.darkColor[rh] = Or(ih, null, function(e) {
		return e * .5;
	}, function(e) {
		return Math.min(1, 1.3 - e);
	}) : Q.darkColor[rh] = Or(ih, null, function(e) {
		return e * .9;
	}, function(e) {
		return 1 - e ** 1.5;
	});
}
Q.size = {
	xxs: 2,
	xs: 5,
	s: 10,
	m: 15,
	l: 20,
	xl: 30,
	xxl: 40,
	xxxl: 50
};
//#endregion
//#region node_modules/echarts/lib/model/globalDefault.js
var ah = "";
typeof navigator < "u" && (ah = navigator.platform || "");
var oh = "rgba(0, 0, 0, 0.2)", sh = Q.color.theme[0], ch = Or(sh, null, null, .9), lh = {
	darkMode: "auto",
	colorBy: "series",
	color: Q.color.theme,
	gradientColor: [ch, sh],
	aria: { decal: { decals: [
		{
			color: oh,
			dashArrayX: [1, 0],
			dashArrayY: [2, 5],
			symbolSize: 1,
			rotation: Math.PI / 6
		},
		{
			color: oh,
			symbol: "circle",
			dashArrayX: [[8, 8], [
				0,
				8,
				8,
				0
			]],
			dashArrayY: [6, 0],
			symbolSize: .8
		},
		{
			color: oh,
			dashArrayX: [1, 0],
			dashArrayY: [4, 3],
			rotation: -Math.PI / 4
		},
		{
			color: oh,
			dashArrayX: [[6, 6], [
				0,
				6,
				6,
				0
			]],
			dashArrayY: [6, 0]
		},
		{
			color: oh,
			dashArrayX: [[1, 0], [1, 6]],
			dashArrayY: [
				1,
				0,
				6,
				0
			],
			rotation: Math.PI / 4
		},
		{
			color: oh,
			symbol: "triangle",
			dashArrayX: [[9, 9], [
				0,
				9,
				9,
				0
			]],
			dashArrayY: [7, 2],
			symbolSize: .75
		}
	] } },
	textStyle: {
		fontFamily: ah.match(/^Win/) ? "Microsoft YaHei" : "sans-serif",
		fontSize: 12,
		fontStyle: "normal",
		fontWeight: "normal"
	},
	blendMode: null,
	stateAnimation: {
		duration: 300,
		easing: "cubicOut"
	},
	animation: "auto",
	animationDuration: 1e3,
	animationDurationUpdate: 500,
	animationEasing: "cubicInOut",
	animationEasingUpdate: "cubicInOut",
	animationThreshold: 2e3,
	progressiveThreshold: 3e3,
	progressive: 400,
	hoverLayerThreshold: 3e3,
	useUTC: !1
}, uh = {
	Must: 1,
	Might: 2,
	Not: 3
}, dh = Y();
function fh(e) {
	dh(e).datasetMap = K();
}
function ph(e, t, n) {
	var r = {}, i = mh(t);
	if (!i || !e) return r;
	var a = [], o = [], s = t.ecModel, c = dh(s).datasetMap, l = i.uid + "_" + n.seriesLayoutBy, u, d;
	e = e.slice(), F(e, function(t, n) {
		var i = W(t) ? t : e[n] = { name: t };
		i.type === "ordinal" && u == null && (u = n, d = m(i)), r[i.name] = [];
	});
	var f = c.get(l) || c.set(l, {
		categoryWayDim: d,
		valueWayDim: 0
	});
	F(e, function(e, t) {
		var n = e.name, i = m(e);
		if (u == null) {
			var s = f.valueWayDim;
			p(r[n], s, i), p(o, s, i), f.valueWayDim += i;
		} else if (u === t) p(r[n], 0, i), p(a, 0, i);
		else {
			var s = f.categoryWayDim;
			p(r[n], s, i), p(o, s, i), f.categoryWayDim += i;
		}
	});
	function p(e, t, n) {
		for (var r = 0; r < n; r++) e.push(t + r);
	}
	function m(e) {
		var t = e.dimsDef;
		return t ? t.length : 1;
	}
	return a.length && (r.itemName = a), o.length && (r.seriesName = o), r;
}
function mh(e) {
	if (!e.get("data", !0)) return Ro(e.ecModel, "dataset", {
		index: e.get("datasetIndex", !0),
		id: e.get("datasetId", !0)
	}, Lo).models[0];
}
function hh(e) {
	return !e.get("transform", !0) && !e.get("fromTransformResult", !0) ? [] : Ro(e.ecModel, "dataset", {
		index: e.get("fromDatasetIndex", !0),
		id: e.get("fromDatasetId", !0)
	}, Lo).models;
}
function gh(e, t) {
	return _h(e.data, e.sourceFormat, e.seriesLayoutBy, e.dimensionsDefine, e.startIndex, t);
}
function _h(e, t, n, r, i, a) {
	var o, s = 5;
	if (ce(e)) return uh.Not;
	var c, l;
	if (r) {
		var u = r[a];
		W(u) ? (c = u.name, l = u.type) : H(u) && (c = u);
	}
	if (l != null) return l === "ordinal" ? uh.Must : uh.Not;
	if (t === "arrayRows") {
		var d = e;
		if (n === "row") {
			for (var f = d[a], p = 0; p < (f || []).length && p < s; p++) if ((o = b(f[i + p])) != null) return o;
		} else for (var p = 0; p < d.length && p < s; p++) {
			var m = d[i + p];
			if (m && (o = b(m[a])) != null) return o;
		}
	} else if (t === "objectRows") {
		var h = e;
		if (!c) return uh.Not;
		for (var p = 0; p < h.length && p < s; p++) {
			var g = h[p];
			if (g && (o = b(g[c])) != null) return o;
		}
	} else if (t === "keyedColumns") {
		var _ = e;
		if (!c) return uh.Not;
		var f = _[c];
		if (!f || ce(f)) return uh.Not;
		for (var p = 0; p < f.length && p < s; p++) if ((o = b(f[p])) != null) return o;
	} else if (t === "original") for (var v = e, p = 0; p < v.length && p < s; p++) {
		var g = v[p], y = _o(g);
		if (!B(y)) return uh.Not;
		if ((o = b(y[a])) != null) return o;
	}
	function b(e) {
		var t = H(e);
		if (e != null && isFinite(Number(e)) && e !== "") return t ? uh.Might : uh.Not;
		if (t && e !== "-") return uh.Must;
	}
	return uh.Not;
}
//#endregion
//#region node_modules/echarts/lib/model/internalComponentCreator.js
var vh = K();
function yh(e, t, n) {
	var r = vh.get(t);
	if (!r) return n;
	var i = r(e);
	return i ? n.concat(i) : n;
}
//#endregion
//#region node_modules/echarts/lib/model/mixin/palette.js
var bh = Y();
Y();
var xh = function() {
	function e() {}
	return e.prototype.getColorFromPalette = function(e, t, n) {
		var r = mo(this.get("color", !0)), i = this.get("colorLayer", !0);
		return Ch(this, bh, r, i, e, t, n);
	}, e.prototype.clearColorPalette = function() {
		wh(this, bh);
	}, e;
}();
function Sh(e, t) {
	for (var n = e.length, r = 0; r < n; r++) if (e[r].length > t) return e[r];
	return e[n - 1];
}
function Ch(e, t, n, r, i, a, o) {
	a ||= e;
	var s = t(a), c = s.paletteIdx || 0, l = s.paletteNameMap = s.paletteNameMap || {};
	if (l.hasOwnProperty(i)) return l[i];
	var u = o == null || !r ? n : Sh(r, o);
	if (u ||= n, u && u.length) {
		var d = u[c];
		return i && (l[i] = d), s.paletteIdx = (c + 1) % u.length, d;
	}
}
function wh(e, t) {
	t(e).paletteIdx = 0, t(e).paletteNameMap = {};
}
//#endregion
//#region node_modules/echarts/lib/model/Global.js
var Th, Eh, Dh, Oh = "\0_ec_inner", kh = 1, Ah = function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t.prototype.init = function(e, t, n, r, i, a) {
		r ||= {}, this.option = null, this._theme = new Kp(r), this._locale = new Kp(i), this._optionManager = a;
	}, t.prototype.setOption = function(e, t, n) {
		var r = Fh(t);
		this._optionManager.setOption(e, n, r), this._resetOption(null, r);
	}, t.prototype.resetOption = function(e, t) {
		return this._resetOption(e, Fh(t));
	}, t.prototype._resetOption = function(e, t) {
		var n = !1, r = this._optionManager;
		if (!e || e === "recreate") {
			var i = r.mountOption(e === "recreate");
			!this.option || e === "recreate" ? Dh(this, i) : (this.restoreData(), this._mergeOption(i, t)), n = !0;
		}
		if ((e === "timeline" || e === "media") && this.restoreData(), !e || e === "recreate" || e === "timeline") {
			var a = r.getTimelineOption(this);
			a && (n = !0, this._mergeOption(a, t));
		}
		if (!e || e === "recreate" || e === "media") {
			var o = r.getMediaOption(this);
			o.length && F(o, function(e) {
				n = !0, this._mergeOption(e, t);
			}, this);
		}
		return n;
	}, t.prototype.mergeOption = function(e) {
		this._mergeOption(e, null);
	}, t.prototype._mergeOption = function(e, t) {
		var n = this.option, r = this._componentsMap, i = this._componentsCount, a = [], o = K(), s = t && t.replaceMergeMainTypeMap;
		fh(this), F(e, function(e, t) {
			e != null && (eh.hasClass(t) ? t && (a.push(t), o.set(t, !0)) : n[t] = n[t] == null ? k(e) : A(n[t], e, !0));
		}), s && s.each(function(e, t) {
			eh.hasClass(t) && !o.get(t) && (a.push(t), o.set(t, !0));
		}), eh.topologicalTravel(a, eh.getAllClassMainTypes(), c, this);
		function c(t) {
			var a = yh(this, t, mo(e[t])), o = r.get(t), c = yo(o, a, o ? s && s.get(t) ? "replaceMerge" : "normalMerge" : "replaceAll");
			jo(c, t, eh), n[t] = null, r.set(t, null), i.set(t, 0);
			var l = [], u = [], d = 0, f;
			F(c, function(e, n) {
				var r = e.existing, i = e.newOption;
				if (!i) r && (r.mergeOption({}, this), r.optionUpdated({}, !1));
				else {
					var a = t === "series", o = eh.getClass(t, e.keyInfo.subType, !a);
					if (!o) return;
					if (t === "tooltip") {
						if (f) return;
						f = !0;
					}
					if (r && r.constructor === o) r.name = e.keyInfo.name, r.mergeOption(i, this), r.optionUpdated(i, !1);
					else {
						var s = j({ componentIndex: n }, e.keyInfo);
						r = new o(i, this, this, s), j(r, s), e.brandNew && (r.__requireNewView = !0), r.init(i, this, this), r.optionUpdated(null, !0);
					}
				}
				r ? (l.push(r.option), u.push(r), d++) : (l.push(void 0), u.push(void 0));
			}, this), n[t] = l, r.set(t, u), i.set(t, d), t === "series" && Th(this);
		}
		this._seriesIndices || Th(this);
	}, t.prototype.getOption = function() {
		var e = k(this.option);
		return F(e, function(t, n) {
			if (eh.hasClass(n)) {
				for (var r = mo(t), i = r.length, a = !1, o = i - 1; o >= 0; o--) r[o] && !Ao(r[o]) ? a = !0 : (r[o] = null, !a && i--);
				r.length = i, e[n] = r;
			}
		}), delete e[Oh], e;
	}, t.prototype.setTheme = function(e) {
		this._theme = new Kp(e), this._resetOption("recreate", null);
	}, t.prototype.getTheme = function() {
		return this._theme;
	}, t.prototype.getLocaleModel = function() {
		return this._locale;
	}, t.prototype.setUpdatePayload = function(e) {
		this._payload = e;
	}, t.prototype.getUpdatePayload = function() {
		return this._payload;
	}, t.prototype.getComponent = function(e, t) {
		var n = this._componentsMap.get(e);
		if (n) {
			var r = n[t || 0];
			if (r) return r;
			if (t == null) {
				for (var i = 0; i < n.length; i++) if (n[i]) return n[i];
			}
		}
	}, t.prototype.queryComponents = function(e) {
		var t = e.mainType;
		if (!t) return [];
		var n = e.index, r = e.id, i = e.name, a = this._componentsMap.get(t);
		if (!a || !a.length) return [];
		var o;
		return n == null ? o = r == null ? i == null ? ie(a, function(e) {
			return !!e;
		}) : Nh("name", i, a) : Nh("id", r, a) : (o = [], F(mo(n), function(e) {
			a[e] && o.push(a[e]);
		})), Ph(o, e);
	}, t.prototype.findComponents = function(e) {
		var t = e.query, n = e.mainType, r = i(t);
		return a(Ph(r ? this.queryComponents(r) : ie(this._componentsMap.get(n), function(e) {
			return !!e;
		}), e));
		function i(e) {
			var t = n + "Index", r = n + "Id", i = n + "Name";
			return e && (e[t] != null || e[r] != null || e[i] != null) ? {
				mainType: n,
				index: e[t],
				id: e[r],
				name: e[i]
			} : null;
		}
		function a(t) {
			return e.filter ? ie(t, e.filter) : t;
		}
	}, t.prototype.eachComponent = function(e, t, n) {
		var r = this._componentsMap;
		if (V(e)) {
			var i = t, a = e;
			r.each(function(e, t) {
				for (var n = 0; e && n < e.length; n++) {
					var r = e[n];
					r && a.call(i, t, r, r.componentIndex);
				}
			});
		} else for (var o = H(e) ? r.get(e) : W(e) ? this.findComponents(e) : null, s = 0; o && s < o.length; s++) {
			var c = o[s];
			c && t.call(n, c, c.componentIndex);
		}
	}, t.prototype.getSeriesByName = function(e) {
		var t = Oo(e, null);
		return ie(this._componentsMap.get("series"), function(e) {
			return !!e && t != null && e.name === t;
		});
	}, t.prototype.getSeriesByIndex = function(e) {
		return this._componentsMap.get("series")[e];
	}, t.prototype.getSeriesByType = function(e) {
		return ie(this._componentsMap.get("series"), function(t) {
			return !!t && t.subType === e;
		});
	}, t.prototype.getSeries = function() {
		return ie(this._componentsMap.get("series"), function(e) {
			return !!e;
		});
	}, t.prototype.getSeriesCount = function() {
		return this._componentsCount.get("series");
	}, t.prototype.eachSeries = function(e, t) {
		Eh(this), F(this._seriesIndices, function(n) {
			var r = this._componentsMap.get("series")[n];
			e.call(t, r, n);
		}, this);
	}, t.prototype.eachRawSeries = function(e, t) {
		F(this._componentsMap.get("series"), function(n) {
			n && e.call(t, n, n.componentIndex);
		});
	}, t.prototype.eachSeriesByType = function(e, t, n) {
		Eh(this), F(this._seriesIndices, function(r) {
			var i = this._componentsMap.get("series")[r];
			i.subType === e && t.call(n, i, r);
		}, this);
	}, t.prototype.eachRawSeriesByType = function(e, t, n) {
		return F(this.getSeriesByType(e), t, n);
	}, t.prototype.isSeriesFiltered = function(e) {
		return Eh(this), this._seriesIndicesMap.get(e.componentIndex) == null;
	}, t.prototype.getCurrentSeriesIndices = function() {
		return (this._seriesIndices || []).slice();
	}, t.prototype.filterSeries = function(e, t) {
		Eh(this);
		var n = [];
		F(this._seriesIndices, function(r) {
			var i = this._componentsMap.get("series")[r];
			e.call(t, i, r) && n.push(r);
		}, this), this._seriesIndices = n, this._seriesIndicesMap = K(n);
	}, t.prototype.restoreData = function(e) {
		Th(this);
		var t = this._componentsMap, n = [];
		t.each(function(e, t) {
			eh.hasClass(t) && n.push(t);
		}), eh.topologicalTravel(n, eh.getAllClassMainTypes(), function(n) {
			F(t.get(n), function(t) {
				t && (n !== "series" || !jh(t, e)) && t.restoreData();
			});
		});
	}, t.internalField = function() {
		Th = function(e) {
			var t = e._seriesIndices = [];
			F(e._componentsMap.get("series"), function(e) {
				e && t.push(e.componentIndex);
			}), e._seriesIndicesMap = K(t);
		}, Eh = function(e) {}, Dh = function(e, t) {
			e.option = {}, e.option[Oh] = kh, e._componentsMap = K({ series: [] }), e._componentsCount = K();
			var n = t.aria;
			W(n) && n.enabled == null && (n.enabled = !0), Mh(t, e._theme.option), A(t, lh, !1), e._mergeOption(t, null);
		};
	}(), t;
}(Kp);
function jh(e, t) {
	if (t) {
		var n = t.seriesIndex, r = t.seriesId, i = t.seriesName;
		return n != null && e.componentIndex !== n || r != null && e.id !== r || i != null && e.name !== i;
	}
}
function Mh(e, t) {
	var n = e.color && !e.colorLayer;
	F(t, function(t, r) {
		r === "colorLayer" && n || r === "color" && e.color || eh.hasClass(r) || (typeof t == "object" ? e[r] = e[r] ? A(e[r], t, !1) : k(t) : e[r] ?? (e[r] = t));
	});
}
function Nh(e, t, n) {
	if (B(t)) {
		var r = K();
		return F(t, function(e) {
			e != null && Oo(e, null) != null && r.set(e, !0);
		}), ie(n, function(t) {
			return t && r.get(t[e]);
		});
	}
	var i = Oo(t, null);
	return ie(n, function(t) {
		return t && i != null && t[e] === i;
	});
}
function Ph(e, t) {
	return t.hasOwnProperty("subType") ? ie(e, function(e) {
		return e && e.subType === t.subType;
	}) : e;
}
function Fh(e) {
	var t = K();
	return e && F(mo(e.replaceMerge), function(e) {
		t.set(e, !0);
	}), { replaceMergeMainTypeMap: t };
}
ne(Ah, xh);
//#endregion
//#region node_modules/echarts/lib/model/OptionManager.js
var Ih = /^(min|max)?(.+)$/, Lh = function() {
	function e(e) {
		this._timelineOptions = [], this._mediaList = [], this._currentMediaIndices = [], this._api = e;
	}
	return e.prototype.setOption = function(e, t, n) {
		e && (F(mo(e.series), function(e) {
			e && e.data && ce(e.data) && be(e.data);
		}), F(mo(e.dataset), function(e) {
			e && e.source && ce(e.source) && be(e.source);
		})), e = k(e);
		var r = this._optionBackup, i = Rh(e, t, !r);
		this._newBaseOption = i.baseOption, r ? (i.timelineOptions.length && (r.timelineOptions = i.timelineOptions), i.mediaList.length && (r.mediaList = i.mediaList), i.mediaDefault && (r.mediaDefault = i.mediaDefault)) : this._optionBackup = i;
	}, e.prototype.mountOption = function(e) {
		var t = this._optionBackup;
		return this._timelineOptions = t.timelineOptions, this._mediaList = t.mediaList, this._mediaDefault = t.mediaDefault, this._currentMediaIndices = [], k(e ? t.baseOption : this._newBaseOption);
	}, e.prototype.getTimelineOption = function(e) {
		var t, n = this._timelineOptions;
		if (n.length) {
			var r = e.getComponent("timeline");
			r && (t = k(n[r.getCurrentIndex()]));
		}
		return t;
	}, e.prototype.getMediaOption = function(e) {
		var t = this._api.getWidth(), n = this._api.getHeight(), r = this._mediaList, i = this._mediaDefault, a = [], o = [];
		if (!r.length && !i) return o;
		for (var s = 0, c = r.length; s < c; s++) zh(r[s].query, t, n) && a.push(s);
		return !a.length && i && (a = [-1]), a.length && !Vh(a, this._currentMediaIndices) && (o = I(a, function(e) {
			return k(e === -1 ? i.option : r[e].option);
		})), this._currentMediaIndices = a, o;
	}, e;
}();
function Rh(e, t, n) {
	var r = [], i, a, o = e.baseOption, s = e.timeline, c = e.options, l = e.media, u = !!e.media, d = !!(c || s || o && o.timeline);
	o ? (a = o, a.timeline || (a.timeline = s)) : ((d || u) && (e.options = e.media = null), a = e), u && B(l) && F(l, function(e) {
		e && e.option && (e.query ? r.push(e) : i ||= e);
	}), f(a), F(c, function(e) {
		return f(e);
	}), F(r, function(e) {
		return f(e.option);
	});
	function f(e) {
		F(t, function(t) {
			t(e, n);
		});
	}
	return {
		baseOption: a,
		timelineOptions: c || [],
		mediaDefault: i,
		mediaList: r
	};
}
function zh(e, t, n) {
	var r = {
		width: t,
		height: n,
		aspectratio: t / n
	}, i = !0;
	return F(e, function(e, t) {
		var n = t.match(Ih);
		if (n && n[1] && n[2]) {
			var a = n[1];
			Bh(r[n[2].toLowerCase()], e, a) || (i = !1);
		}
	}), i;
}
function Bh(e, t, n) {
	return n === "min" ? e >= t : n === "max" ? e <= t : e === t;
}
function Vh(e, t) {
	return e.join(",") === t.join(",");
}
//#endregion
//#region node_modules/echarts/lib/preprocessor/helper/compatStyle.js
var Hh = F, Uh = W, Wh = [
	"areaStyle",
	"lineStyle",
	"nodeStyle",
	"linkStyle",
	"chordStyle",
	"label",
	"labelLine"
];
function Gh(e) {
	var t = e && e.itemStyle;
	if (t) for (var n = 0, r = Wh.length; n < r; n++) {
		var i = Wh[n], a = t.normal, o = t.emphasis;
		a && a[i] && (e[i] = e[i] || {}, e[i].normal ? A(e[i].normal, a[i]) : e[i].normal = a[i], a[i] = null), o && o[i] && (e[i] = e[i] || {}, e[i].emphasis ? A(e[i].emphasis, o[i]) : e[i].emphasis = o[i], o[i] = null);
	}
}
function Kh(e, t, n) {
	if (e && e[t] && (e[t].normal || e[t].emphasis)) {
		var r = e[t].normal, i = e[t].emphasis;
		r && (n ? (e[t].normal = e[t].emphasis = null, M(e[t], r)) : e[t] = r), i && (e.emphasis = e.emphasis || {}, e.emphasis[t] = i, i.focus && (e.emphasis.focus = i.focus), i.blurScope && (e.emphasis.blurScope = i.blurScope));
	}
}
function qh(e) {
	Kh(e, "itemStyle"), Kh(e, "lineStyle"), Kh(e, "areaStyle"), Kh(e, "label"), Kh(e, "labelLine"), Kh(e, "upperLabel"), Kh(e, "edgeLabel");
}
function Jh(e, t) {
	var n = Uh(e) && e[t], r = Uh(n) && n.textStyle;
	if (r) for (var i = 0, a = go.length; i < a; i++) {
		var o = go[i];
		r.hasOwnProperty(o) && (n[o] = r[o]);
	}
}
function Yh(e) {
	e && (qh(e), Jh(e, "label"), e.emphasis && Jh(e.emphasis, "label"));
}
function Xh(e) {
	if (Uh(e)) {
		Gh(e), qh(e), Jh(e, "label"), Jh(e, "upperLabel"), Jh(e, "edgeLabel"), e.emphasis && (Jh(e.emphasis, "label"), Jh(e.emphasis, "upperLabel"), Jh(e.emphasis, "edgeLabel"));
		var t = e.markPoint;
		t && (Gh(t), Yh(t));
		var n = e.markLine;
		n && (Gh(n), Yh(n));
		var r = e.markArea;
		r && Yh(r);
		var i = e.data;
		if (e.type === "graph") {
			i ||= e.nodes;
			var a = e.links || e.edges;
			if (a && !ce(a)) for (var o = 0; o < a.length; o++) Yh(a[o]);
			F(e.categories, function(e) {
				qh(e);
			});
		}
		if (i && !ce(i)) for (var o = 0; o < i.length; o++) Yh(i[o]);
		if (t = e.markPoint, t && t.data) for (var s = t.data, o = 0; o < s.length; o++) Yh(s[o]);
		if (n = e.markLine, n && n.data) for (var c = n.data, o = 0; o < c.length; o++) B(c[o]) ? (Yh(c[o][0]), Yh(c[o][1])) : Yh(c[o]);
		e.type === "gauge" ? (Jh(e, "axisLabel"), Jh(e, "title"), Jh(e, "detail")) : e.type === "treemap" ? (Kh(e.breadcrumb, "itemStyle"), F(e.levels, function(e) {
			qh(e);
		})) : e.type === "tree" && qh(e.leaves);
	}
}
function Zh(e) {
	return B(e) ? e : e ? [e] : [];
}
function Qh(e) {
	return (B(e) ? e[0] : e) || {};
}
function $h(e, t) {
	Hh(Zh(e.series), function(e) {
		Uh(e) && Xh(e);
	});
	var n = [
		"xAxis",
		"yAxis",
		"radiusAxis",
		"angleAxis",
		"singleAxis",
		"parallelAxis",
		"radar"
	];
	t && n.push("valueAxis", "categoryAxis", "logAxis", "timeAxis"), Hh(n, function(t) {
		Hh(Zh(e[t]), function(e) {
			e && (Jh(e, "axisLabel"), Jh(e.axisPointer, "label"));
		});
	}), Hh(Zh(e.parallel), function(e) {
		var t = e && e.parallelAxisDefault;
		Jh(t, "axisLabel"), Jh(t && t.axisPointer, "label");
	}), Hh(Zh(e.calendar), function(e) {
		Kh(e, "itemStyle"), Jh(e, "dayLabel"), Jh(e, "monthLabel"), Jh(e, "yearLabel");
	}), Hh(Zh(e.radar), function(e) {
		Jh(e, "name"), e.name && e.axisName == null && (e.axisName = e.name, delete e.name), e.nameGap != null && e.axisNameGap == null && (e.axisNameGap = e.nameGap, delete e.nameGap);
	}), Hh(Zh(e.geo), function(e) {
		Uh(e) && (Yh(e), Hh(Zh(e.regions), function(e) {
			Yh(e);
		}));
	}), Hh(Zh(e.timeline), function(e) {
		Yh(e), Kh(e, "label"), Kh(e, "itemStyle"), Kh(e, "controlStyle", !0);
		var t = e.data;
		B(t) && F(t, function(e) {
			W(e) && (Kh(e, "label"), Kh(e, "itemStyle"));
		});
	}), Hh(Zh(e.toolbox), function(e) {
		Kh(e, "iconStyle"), Hh(e.feature, function(e) {
			Kh(e, "iconStyle");
		});
	}), Jh(Qh(e.axisPointer), "label"), Jh(Qh(e.tooltip).axisPointer, "label");
}
//#endregion
//#region node_modules/echarts/lib/preprocessor/backwardCompat.js
function eg(e, t) {
	for (var n = t.split(","), r = e, i = 0; i < n.length && (r &&= r[n[i]], r != null); i++);
	return r;
}
function tg(e, t, n, r) {
	for (var i = t.split(","), a = e, o, s = 0; s < i.length - 1; s++) o = i[s], a[o] ?? (a[o] = {}), a = a[o];
	(r || a[i[s]] == null) && (a[i[s]] = n);
}
function ng(e) {
	e && F(rg, function(t) {
		t[0] in e && !(t[1] in e) && (e[t[1]] = e[t[0]]);
	});
}
var rg = [
	["x", "left"],
	["y", "top"],
	["x2", "right"],
	["y2", "bottom"]
], ig = [
	"grid",
	"geo",
	"parallel",
	"legend",
	"toolbox",
	"title",
	"visualMap",
	"dataZoom",
	"timeline"
], ag = [
	["borderRadius", "barBorderRadius"],
	["borderColor", "barBorderColor"],
	["borderWidth", "barBorderWidth"]
];
function og(e) {
	var t = e && e.itemStyle;
	if (t) for (var n = 0; n < ag.length; n++) {
		var r = ag[n][1], i = ag[n][0];
		t[r] != null && (t[i] = t[r]);
	}
}
function sg(e) {
	e && e.alignTo === "edge" && e.margin != null && e.edgeDistance == null && (e.edgeDistance = e.margin);
}
function cg(e) {
	e && e.downplay && !e.blur && (e.blur = e.downplay);
}
function lg(e) {
	e && e.focusNodeAdjacency != null && (e.emphasis = e.emphasis || {}, e.emphasis.focus ?? (e.emphasis.focus = "adjacency"));
}
function ug(e, t) {
	if (e) for (var n = 0; n < e.length; n++) t(e[n]), e[n] && ug(e[n].children, t);
}
function dg(e, t) {
	$h(e, t), e.series = mo(e.series), F(e.series, function(e) {
		if (W(e)) {
			var t = e.type;
			if (t === "line") e.clipOverflow != null && (e.clip = e.clipOverflow);
			else if (t === "pie" || t === "gauge") {
				e.clockWise != null && (e.clockwise = e.clockWise), sg(e.label);
				var n = e.data;
				if (n && !ce(n)) for (var r = 0; r < n.length; r++) sg(n[r]);
				e.hoverOffset != null && (e.emphasis = e.emphasis || {}, (e.emphasis.scaleSize = null) && (e.emphasis.scaleSize = e.hoverOffset));
			} else if (t === "gauge") {
				var i = eg(e, "pointer.color");
				i != null && tg(e, "itemStyle.color", i);
			} else if (t === "bar") {
				og(e), og(e.backgroundStyle), og(e.emphasis);
				var n = e.data;
				if (n && !ce(n)) for (var r = 0; r < n.length; r++) typeof n[r] == "object" && (og(n[r]), og(n[r] && n[r].emphasis));
			} else if (t === "sunburst") {
				var a = e.highlightPolicy;
				a && (e.emphasis = e.emphasis || {}, e.emphasis.focus || (e.emphasis.focus = a)), cg(e), ug(e.data, cg);
			} else t === "graph" || t === "sankey" ? lg(e) : t === "map" && (e.mapType && !e.map && (e.map = e.mapType), e.mapLocation && M(e, e.mapLocation));
			e.hoverAnimation != null && (e.emphasis = e.emphasis || {}, e.emphasis && e.emphasis.scale == null && (e.emphasis.scale = e.hoverAnimation)), ng(e);
		}
	}), e.dataRange && (e.visualMap = e.dataRange), F(ig, function(t) {
		var n = e[t];
		n && (B(n) || (n = [n]), F(n, function(e) {
			ng(e);
		}));
	});
}
//#endregion
//#region node_modules/echarts/lib/processor/dataStack.js
var fg = Yo(pg);
function pg(e) {
	var t = K();
	e.eachSeries(function(e) {
		var n = e.get("stack");
		if (n) {
			var r = t.get(n) || t.set(n, []), i = e.getData(), a = {
				stackResultDimension: i.getCalculationInfo("stackResultDimension"),
				stackedOverDimension: i.getCalculationInfo("stackedOverDimension"),
				stackedDimension: i.getCalculationInfo("stackedDimension"),
				stackedByDimension: i.getCalculationInfo("stackedByDimension"),
				isStackedByIndex: i.getCalculationInfo("isStackedByIndex"),
				data: i,
				seriesModel: e
			};
			if (!a.stackedDimension || !(a.isStackedByIndex || a.stackedByDimension)) return;
			r.push(a);
		}
	}), t.each(function(e) {
		e.length !== 0 && ((e[0].seriesModel.get("stackOrder") || "seriesAsc") === "seriesDesc" && e.reverse(), F(e, function(t, n) {
			t.data.setCalculationInfo("stackedOnSeries", n > 0 ? e[n - 1].seriesModel : null);
		}), mg(e));
	});
}
function mg(e) {
	F(e, function(t, n) {
		var r = [], i = [NaN, NaN], a = [t.stackResultDimension, t.stackedOverDimension], o = t.data, s = t.isStackedByIndex, c = t.seriesModel.get("stackStrategy") || "samesign";
		o.modify(a, function(a, l, u) {
			var d = o.get(t.stackedDimension, u);
			if (isNaN(d)) return i;
			var f, p;
			s ? p = o.getRawIndex(u) : f = o.get(t.stackedByDimension, u);
			for (var m = NaN, h = n - 1; h >= 0; h--) {
				var g = e[h];
				if (s || (p = g.data.rawIndexOf(g.stackedByDimension, f)), p >= 0) {
					var _ = g.data.getByRawIndex(g.stackResultDimension, p);
					if (c === "all" || c === "positive" && _ > 0 || c === "negative" && _ < 0 || c === "samesign" && d >= 0 && _ > 0 || c === "samesign" && d <= 0 && _ < 0) {
						d = Ja(d, _), m = _;
						break;
					}
				}
			}
			return r[0] = d, r[1] = m, r;
		});
	});
}
//#endregion
//#region node_modules/echarts/lib/data/Source.js
var hg = function() {
	function e(e) {
		this.data = e.data || (e.sourceFormat === "keyedColumns" ? {} : []), this.sourceFormat = e.sourceFormat || "unknown", this.seriesLayoutBy = e.seriesLayoutBy || "column", this.startIndex = e.startIndex || 0, this.dimensionsDetectedCount = e.dimensionsDetectedCount, this.metaRawOption = e.metaRawOption;
		var t = this.dimensionsDefine = e.dimensionsDefine;
		if (t) for (var n = 0; n < t.length; n++) {
			var r = t[n];
			r.type == null && gh(this, n) === uh.Must && (r.type = "ordinal");
		}
	}
	return e;
}();
function gg(e) {
	return e instanceof hg;
}
function _g(e, t, n) {
	n ||= bg(e);
	var r = t.seriesLayoutBy, i = xg(e, n, r, t.sourceHeader, t.dimensions);
	return new hg({
		data: e,
		sourceFormat: n,
		seriesLayoutBy: r,
		dimensionsDefine: i.dimensionsDefine,
		startIndex: i.startIndex,
		dimensionsDetectedCount: i.dimensionsDetectedCount,
		metaRawOption: k(t)
	});
}
function vg(e) {
	return new hg({
		data: e,
		sourceFormat: ce(e) ? Rl : Pl
	});
}
function yg(e) {
	return new hg({
		data: e.data,
		sourceFormat: e.sourceFormat,
		seriesLayoutBy: e.seriesLayoutBy,
		dimensionsDefine: k(e.dimensionsDefine),
		startIndex: e.startIndex,
		dimensionsDetectedCount: e.dimensionsDetectedCount
	});
}
function bg(e) {
	var t = zl;
	if (ce(e)) t = Rl;
	else if (B(e)) {
		e.length === 0 && (t = Fl);
		for (var n = 0, r = e.length; n < r; n++) {
			var i = e[n];
			if (i != null) {
				if (B(i) || ce(i)) {
					t = Fl;
					break;
				}
				if (W(i)) {
					t = Il;
					break;
				}
			}
		}
	} else if (W(e)) {
		for (var a in e) if (ke(e, a) && P(e[a])) {
			t = Ll;
			break;
		}
	}
	return t;
}
function xg(e, t, n, r, i) {
	var a, o;
	if (!e) return {
		dimensionsDefine: Cg(i),
		startIndex: o,
		dimensionsDetectedCount: a
	};
	if (t === "arrayRows") {
		var s = e;
		r === "auto" || r == null ? wg(function(e) {
			e != null && e !== "-" && (H(e) ? o ??= 1 : o = 0);
		}, n, s, 10) : o = U(r) ? r : +!!r, !i && o === 1 && (i = [], wg(function(e, t) {
			i[t] = e == null ? "" : e + "";
		}, n, s, Infinity)), a = i ? i.length : n === "row" ? s.length : s[0] ? s[0].length : null;
	} else if (t === "objectRows") i ||= Sg(e);
	else if (t === "keyedColumns") i || (i = [], F(e, function(e, t) {
		i.push(t);
	}));
	else if (t === "original") {
		var c = _o(e[0]);
		a = B(c) && c.length || 1;
	}
	return {
		startIndex: o,
		dimensionsDefine: Cg(i),
		dimensionsDetectedCount: a
	};
}
function Sg(e) {
	for (var t = 0, n; t < e.length && !(n = e[t++]););
	if (n) return L(n);
}
function Cg(e) {
	if (e) {
		var t = K();
		return I(e, function(e, n) {
			e = W(e) ? e : { name: e };
			var r = {
				name: e.name,
				displayName: e.displayName,
				type: e.type
			};
			if (r.name == null) return r;
			r.name += "", r.displayName ??= r.name;
			var i = t.get(r.name);
			return i ? r.name += "-" + i.count++ : t.set(r.name, { count: 1 }), r;
		});
	}
}
function wg(e, t, n, r) {
	if (t === "row") for (var i = 0; i < n.length && i < r; i++) e(n[i] ? n[i][0] : null, i);
	else for (var a = n[0] || [], i = 0; i < a.length && i < r; i++) e(a[i], i);
}
function Tg(e) {
	var t = e.sourceFormat;
	return t === "objectRows" || t === "keyedColumns";
}
//#endregion
//#region node_modules/echarts/lib/data/helper/dataProvider.js
var Eg, Dg, Og, kg, Ag, jg, Mg = function() {
	function e(e, t) {
		var n = gg(e) ? e : vg(e);
		this._source = n;
		var r = this._data = n.data, i = n.sourceFormat;
		n.seriesLayoutBy, i === "typedArray" && (this._offset = 0, this._dimSize = t, this._data = r), jg(this, r, n);
	}
	return e.prototype.getSource = function() {
		return this._source;
	}, e.prototype.count = function() {
		return 0;
	}, e.prototype.getItem = function(e, t) {}, e.prototype.appendData = function(e) {}, e.prototype.clean = function() {}, e.protoInitialize = function() {
		var t = e.prototype;
		t.pure = !1, t.persistent = !0;
	}(), e.internalField = function() {
		var e;
		jg = function(e, i, a) {
			var o = a.sourceFormat, s = a.seriesLayoutBy, c = a.startIndex, l = a.dimensionsDefine, u = Ag[Ug(o, s)];
			j(e, u), o === "typedArray" ? (e.getItem = t, e.count = r, e.fillStorage = n) : (e.getItem = R(Ig(o, s), null, i, c, l), e.count = R(zg(o, s), null, i, c, l));
		};
		var t = function(e, t) {
			e -= this._offset, t ||= [];
			for (var n = this._data, r = this._dimSize, i = r * e, a = 0; a < r; a++) t[a] = n[i + a];
			return t;
		}, n = function(e, t, n, r) {
			for (var i = this._data, a = this._dimSize, o = 0; o < a; o++) {
				for (var s = r[o], c = s[0] == null ? Infinity : s[0], l = s[1] == null ? -Infinity : s[1], u = t - e, d = n[o], f = 0; f < u; f++) {
					var p = i[f * a + o];
					d[e + f] = p, p < c && (c = p), p > l && (l = p);
				}
				s[0] = c, s[1] = l;
			}
		}, r = function() {
			return this._data ? this._data.length / this._dimSize : 0;
		};
		Ag = (e = {}, e[Fl + "_" + Bl] = {
			pure: !0,
			appendData: i
		}, e[Fl + "_row"] = {
			pure: !0,
			appendData: function() {
				throw Error("Do not support appendData when set seriesLayoutBy: \"row\".");
			}
		}, e[Il] = {
			pure: !0,
			appendData: i
		}, e[Ll] = {
			pure: !0,
			appendData: function(e) {
				var t = this._data;
				F(e, function(e, n) {
					for (var r = t[n] || (t[n] = []), i = 0; i < (e || []).length; i++) r.push(e[i]);
				});
			}
		}, e[Pl] = { appendData: i }, e[Rl] = {
			persistent: !1,
			pure: !0,
			appendData: function(e) {
				this._data = e;
			},
			clean: function() {
				this._offset += this.count(), this._data = null;
			}
		}, e);
		function i(e) {
			for (var t = 0; t < e.length; t++) this._data.push(e[t]);
		}
	}(), e;
}(), Ng = function(e) {
	B(e) || lo("series.data or dataset.source must be an array.");
};
Eg = {}, Eg[Fl + "_" + Bl] = Ng, Eg[Fl + "_row"] = Ng, Eg[Il] = Ng, Eg[Ll] = function(e, t) {
	for (var n = 0; n < t.length; n++) t[n].name ?? lo("dimension name must not be null/undefined.");
}, Eg[Pl] = Ng;
var Pg = function(e, t, n, r) {
	return e[r];
}, Fg = (Dg = {}, Dg[Fl + "_" + Bl] = function(e, t, n, r) {
	return e[r + t];
}, Dg[Fl + "_row"] = function(e, t, n, r, i) {
	r += t;
	for (var a = i || [], o = e, s = 0; s < o.length; s++) {
		var c = o[s];
		a[s] = c ? c[r] : null;
	}
	return a;
}, Dg[Il] = Pg, Dg[Ll] = function(e, t, n, r, i) {
	for (var a = i || [], o = 0; o < n.length; o++) {
		var s = n[o].name, c = s == null ? null : e[s];
		a[o] = c ? c[r] : null;
	}
	return a;
}, Dg[Pl] = Pg, Dg);
function Ig(e, t) {
	return Fg[Ug(e, t)];
}
var Lg = function(e, t, n) {
	return e.length;
}, Rg = (Og = {}, Og[Fl + "_" + Bl] = function(e, t, n) {
	return Math.max(0, e.length - t);
}, Og[Fl + "_row"] = function(e, t, n) {
	var r = e[0];
	return r ? Math.max(0, r.length - t) : 0;
}, Og[Il] = Lg, Og[Ll] = function(e, t, n) {
	var r = n[0].name, i = r == null ? null : e[r];
	return i ? i.length : 0;
}, Og[Pl] = Lg, Og);
function zg(e, t) {
	return Rg[Ug(e, t)];
}
var Bg = function(e, t, n) {
	return e[t];
}, Vg = (kg = {}, kg[Fl] = Bg, kg[Il] = function(e, t, n) {
	return e[n];
}, kg[Ll] = Bg, kg[Pl] = function(e, t, n) {
	var r = _o(e);
	return r instanceof Array ? r[t] : r;
}, kg[Rl] = Bg, kg);
function Hg(e) {
	return Vg[e];
}
function Ug(e, t) {
	return e === "arrayRows" ? e + "_" + t : e;
}
function Wg(e, t, n) {
	if (e) {
		var r = e.getRawDataItem(t);
		if (r != null) {
			var i = e.getStore(), a = i.getSource().sourceFormat;
			if (n != null) {
				var o = e.getDimensionIndex(n), s = i.getDimensionProperty(o);
				return Hg(a)(r, o, s);
			}
			var c = r;
			return a === "original" && (c = _o(r)), c;
		}
	}
}
//#endregion
//#region node_modules/echarts/lib/model/mixin/dataFormat.js
var Gg = /\{@(.+?)\}/g, Kg = function() {
	function e() {}
	return e.prototype.getDataParams = function(e, t) {
		var n = this.getData(t), r = this.getRawValue(e, t), i = n.getRawIndex(e), a = n.getName(e), o = n.getRawDataItem(e), s = n.getItemVisual(e, "style"), c = s && s[n.getItemVisual(e, "drawType") || "fill"], l = s && s.stroke, u = this.mainType, d = u === "series", f = n.userOutput && n.userOutput.get();
		return {
			componentType: u,
			componentSubType: this.subType,
			componentIndex: this.componentIndex,
			seriesType: d ? this.subType : null,
			seriesIndex: this.seriesIndex,
			seriesId: d ? this.id : null,
			seriesName: d ? this.name : null,
			name: a,
			dataIndex: i,
			data: o,
			dataType: t,
			value: r,
			color: c,
			borderColor: l,
			dimensionNames: f ? f.fullDimensions : null,
			encode: f ? f.encode : null,
			$vars: [
				"seriesName",
				"name",
				"value"
			]
		};
	}, e.prototype.getFormattedLabel = function(e, t, n, r, i, a) {
		t ||= "normal";
		var o = this.getData(n), s = this.getDataParams(e, n);
		if (a && (s.value = a.interpolatedValue), r != null && B(s.value) && (s.value = s.value[r]), i ||= o.getItemModel(e).get(t === "normal" ? ["label", "formatter"] : [
			t,
			"label",
			"formatter"
		]), V(i)) return s.status = t, s.dimensionIndex = r, i(s);
		if (H(i)) return km(i, s).replace(Gg, function(t, n) {
			var r = n.length, i = n;
			i.charAt(0) === "[" && i.charAt(r - 1) === "]" && (i = +i.slice(1, r - 1));
			var s = Wg(o, e, i);
			if (a && B(a.interpolatedValue)) {
				var c = o.getDimensionIndex(i);
				c >= 0 && (s = a.interpolatedValue[c]);
			}
			return s == null ? "" : s + "";
		});
	}, e.prototype.getRawValue = function(e, t) {
		return Wg(this.getData(t), e);
	}, e.prototype.formatTooltip = function(e, t, n) {}, e;
}();
function qg(e) {
	var t, n;
	return W(e) ? e.type && (n = e) : t = e, {
		text: t,
		frag: n
	};
}
//#endregion
//#region node_modules/echarts/lib/core/task.js
function Jg(e) {
	return new Yg(e);
}
var Yg = function() {
	function e(e) {
		e ||= {}, this._reset = e.reset, this._plan = e.plan, this._count = e.count, this._onDirty = e.onDirty, this._dirty = !0;
	}
	return e.prototype.perform = function(e) {
		var t = this._upstream, n = e && e.skip;
		if (this._dirty && t) {
			var r = this.context;
			r.data = r.outputData = t.context.outputData;
		}
		this.__pipeline && (this.__pipeline.currentTask = this);
		var i;
		this._plan && !n && (i = this._plan(this.context));
		var a = l(this._modBy), o = this._modDataCount || 0, s = l(e && e.modBy), c = e && e.modDataCount || 0;
		(a !== s || o !== c) && (i = "reset");
		function l(e) {
			return !(e >= 1) && (e = 1), e;
		}
		var u;
		(this._dirty || i === "reset") && (this._dirty = !1, u = this._doReset(n)), this._modBy = s, this._modDataCount = c;
		var d = e && e.step;
		if (this._dueEnd = t ? t._outputDueEnd : this._count ? this._count(this.context) : Infinity, this._progress) {
			var f = this._dueIndex, p = Math.min(d == null ? Infinity : this._dueIndex + d, this._dueEnd);
			if (!n && (u || f < p)) {
				var m = this._progress;
				if (B(m)) for (var h = 0; h < m.length; h++) this._doProgress(m[h], f, p, s, c);
				else this._doProgress(m, f, p, s, c);
			}
			this._dueIndex = p;
			var g = this._settedOutputEnd == null ? p : this._settedOutputEnd;
			this._outputDueEnd = g;
		} else this._dueIndex = this._outputDueEnd = this._settedOutputEnd == null ? this._dueEnd : this._settedOutputEnd;
		return this.unfinished();
	}, e.prototype.dirty = function() {
		this._dirty = !0, this._onDirty && this._onDirty(this.context);
	}, e.prototype._doProgress = function(e, t, n, r, i) {
		Xg.reset(t, n, r, i), this._callingProgress = e, this._callingProgress({
			start: t,
			end: n,
			count: n - t,
			next: Xg.next
		}, this.context);
	}, e.prototype._doReset = function(e) {
		this._dueIndex = this._outputDueEnd = this._dueEnd = 0, this._settedOutputEnd = null;
		var t, n;
		!e && this._reset && (t = this._reset(this.context), t && t.progress && (n = t.forceFirstProgress, t = t.progress), B(t) && !t.length && (t = null)), this._progress = t, this._modBy = this._modDataCount = null;
		var r = this._downstream;
		return r && r.dirty(), n;
	}, e.prototype.unfinished = function() {
		return this._progress && this._dueIndex < this._dueEnd;
	}, e.prototype.pipe = function(e) {
		(this._downstream !== e || this._dirty) && (this._downstream = e, e._upstream = this, e.dirty());
	}, e.prototype.dispose = function() {
		this._disposed ||= (this._upstream && (this._upstream._downstream = null), this._downstream && (this._downstream._upstream = null), this._dirty = !1, !0);
	}, e.prototype.getUpstream = function() {
		return this._upstream;
	}, e.prototype.getDownstream = function() {
		return this._downstream;
	}, e.prototype.setOutputEnd = function(e) {
		this._outputDueEnd = this._settedOutputEnd = e;
	}, e;
}(), Xg = function() {
	var e, t, n, r, i, a = { reset: function(c, l, u, d) {
		t = c, e = l, n = u, r = d, i = Math.ceil(r / n), a.next = n > 1 && r > 0 ? s : o;
	} };
	return a;
	function o() {
		return t < e ? t++ : null;
	}
	function s() {
		var a = t % i * n + Math.ceil(t / i), o = t >= e ? null : a < r ? a : t;
		return t++, o;
	}
}();
//#endregion
//#region node_modules/echarts/lib/data/helper/dataValueHelper.js
function Zg(e, t) {
	var n = t && t.type;
	return n === "ordinal" ? e : (n === "time" && !U(e) && e != null && e !== "-" && (e = +Qa(e)), e == null || e === "" ? NaN : Number(e));
}
K({
	number: function(e) {
		return parseFloat(e);
	},
	time: function(e) {
		return +Qa(e);
	},
	trim: function(e) {
		return H(e) ? ve(e) : e;
	}
});
var Qg = {
	lt: function(e, t) {
		return e < t;
	},
	lte: function(e, t) {
		return e <= t;
	},
	gt: function(e, t) {
		return e > t;
	},
	gte: function(e, t) {
		return e >= t;
	}
};
(function() {
	function e(e, t) {
		U(t) || uo(""), this._opFn = Qg[e], this._rvalFloat = $a(t);
	}
	return e.prototype.evaluate = function(e) {
		return U(e) ? this._opFn(e, this._rvalFloat) : this._opFn($a(e), this._rvalFloat);
	}, e;
})();
var $g = function() {
	function e(e, t) {
		var n = e === "desc";
		this._resultLT = n ? 1 : -1, t ??= n ? "min" : "max", this._incomparable = t === "min" ? -Infinity : Infinity;
	}
	return e.prototype.evaluate = function(e, t) {
		var n = U(e) ? e : $a(e), r = U(t) ? t : $a(t), i = isNaN(n), a = isNaN(r);
		if (i && (n = this._incomparable), a && (r = this._incomparable), i && a) {
			var o = H(e), s = H(t);
			o && (n = s ? e : 0), s && (r = o ? t : 0);
		}
		return n < r ? this._resultLT : n > r ? -this._resultLT : 0;
	}, e;
}();
(function() {
	function e(e, t) {
		this._rval = t, this._isEQ = e, this._rvalTypeof = typeof t, this._rvalFloat = $a(t);
	}
	return e.prototype.evaluate = function(e) {
		var t = e === this._rval;
		if (!t) {
			var n = typeof e;
			n !== this._rvalTypeof && (n === "number" || this._rvalTypeof === "number") && (t = $a(e) === this._rvalFloat);
		}
		return this._isEQ ? t : !t;
	}, e;
})();
function e_(e) {
	var t = "", n = -Infinity, r = -Infinity, i = Infinity, a = Infinity;
	return e && (e.g != null && (t += "G" + e.g, n = e.g), e.ge != null && (t += "GE" + e.ge, r = e.ge), e.l != null && (t += "L" + e.l, i = e.l), e.le != null && (t += "LE" + e.le, a = e.le)), {
		key: t,
		g: n,
		ge: r,
		l: i,
		le: a
	};
}
function t_(e, t) {
	return t > e.g && t >= e.ge && t < e.l && t <= e.le;
}
//#endregion
//#region node_modules/echarts/lib/data/helper/transform.js
var n_ = function() {
	function e() {}
	return e.prototype.getRawData = function() {
		throw Error("not supported");
	}, e.prototype.getRawDataItem = function(e) {
		throw Error("not supported");
	}, e.prototype.cloneRawData = function() {}, e.prototype.getDimensionInfo = function(e) {}, e.prototype.cloneAllDimensionInfo = function() {}, e.prototype.count = function() {}, e.prototype.retrieveValue = function(e, t) {}, e.prototype.retrieveValueFromItem = function(e, t) {}, e.prototype.convertValue = function(e, t) {
		return Zg(e, t);
	}, e;
}();
function r_(e, t) {
	var n = new n_(), r = e.data, i = n.sourceFormat = e.sourceFormat, a = e.startIndex;
	e.seriesLayoutBy !== "column" && uo("");
	var o = [], s = {}, c = e.dimensionsDefine;
	if (c) F(c, function(e, t) {
		var n = e.name, r = {
			index: t,
			name: n,
			displayName: e.displayName
		};
		o.push(r), n != null && (ke(s, n) && uo(""), s[n] = r);
	});
	else for (var l = 0; l < e.dimensionsDetectedCount; l++) o.push({ index: l });
	var u = Ig(i, Bl);
	t.__isBuiltIn && (n.getRawDataItem = function(e) {
		return u(r, a, o, e);
	}, n.getRawData = R(i_, null, e)), n.cloneRawData = R(a_, null, e), n.count = R(zg(i, Bl), null, r, a, o);
	var d = Hg(i);
	n.retrieveValue = function(e, t) {
		return f(u(r, a, o, e), t);
	};
	var f = n.retrieveValueFromItem = function(e, t) {
		if (e != null) {
			var n = o[t];
			if (n) return d(e, t, n.name);
		}
	};
	return n.getDimensionInfo = R(o_, null, o, s), n.cloneAllDimensionInfo = R(s_, null, o), n;
}
function i_(e) {
	var t = e.sourceFormat;
	return f_(t) || uo(""), e.data;
}
function a_(e) {
	var t = e.sourceFormat, n = e.data;
	if (f_(t) || uo(""), t === "arrayRows") {
		for (var r = [], i = 0, a = n.length; i < a; i++) r.push(n[i].slice());
		return r;
	}
	if (t === "objectRows") {
		for (var r = [], i = 0, a = n.length; i < a; i++) r.push(j({}, n[i]));
		return r;
	}
}
function o_(e, t, n) {
	if (n != null) {
		if (U(n) || !isNaN(n) && !ke(t, n)) return e[n];
		if (ke(t, n)) return t[n];
	}
}
function s_(e) {
	return k(e);
}
var c_ = K();
function l_(e) {
	e = k(e);
	var t = e.type, n = "";
	t || uo(n);
	var r = t.split(":");
	r.length !== 2 && uo(n);
	var i = !1;
	r[0] === "echarts" && (t = r[1], i = !0), e.__isBuiltIn = i, c_.set(t, e);
}
function u_(e, t, n) {
	var r = mo(e), i = r.length;
	i || uo("");
	for (var a = 0, o = i; a < o; a++) {
		var s = r[a];
		t = d_(s, t, n, i === 1 ? null : a), a !== o - 1 && (t.length = Math.max(t.length, 1));
	}
	return t;
}
function d_(e, t, n, r) {
	var i = "";
	t.length || uo(i), W(e) || uo(i);
	var a = e.type, o = c_.get(a);
	o || uo(i);
	var s = I(t, function(e) {
		return r_(e, o);
	});
	return I(mo(o.transform({
		upstream: s[0],
		upstreamList: s,
		config: k(e.config)
	})), function(e, n) {
		var r = "";
		W(e) || uo(r), e.data || uo(r), f_(bg(e.data)) || uo(r);
		var i, a = t[0];
		if (a && n === 0 && !e.dimensions) {
			var o = a.startIndex;
			o && (e.data = a.data.slice(0, o).concat(e.data)), i = {
				seriesLayoutBy: Bl,
				sourceHeader: o,
				dimensions: a.metaRawOption.dimensions
			};
		} else i = {
			seriesLayoutBy: Bl,
			sourceHeader: 0,
			dimensions: e.dimensions
		};
		return _g(e.data, i, null);
	});
}
function f_(e) {
	return e === "arrayRows" || e === "objectRows";
}
//#endregion
//#region node_modules/echarts/lib/data/DataStore.js
var p_ = typeof Uint32Array > "u" ? Array : Uint32Array, m_ = typeof Uint16Array > "u" ? Array : Uint16Array, h_ = typeof Int32Array > "u" ? Array : Int32Array, g_ = typeof Float64Array > "u" ? Array : Float64Array, __ = {
	float: g_,
	int: h_,
	ordinal: Array,
	number: Array,
	time: g_
}, v_;
function y_(e) {
	return e > 65535 ? p_ : m_;
}
function b_(e) {
	var t = e.constructor;
	return t === Array ? e.slice() : new t(e);
}
function x_(e, t, n, r, i) {
	var a = __[n || "float"];
	if (i) {
		var o = e[t], s = o && o.length;
		if (s !== r) {
			for (var c = new a(r), l = 0; l < s; l++) c[l] = o[l];
			e[t] = c;
		}
	} else e[t] = new a(r);
}
var S_ = function() {
	function e() {
		this._chunks = [], this._rawExtent = [], this._extent = [], this._count = 0, this._rawCount = 0, this._calcDimNameToIdx = K();
	}
	return e.prototype.initData = function(e, t, n) {
		this._provider = e, this._chunks = [], this._indices = null, this.getRawIndex = this._getRawIdxIdentity;
		var r = e.getSource(), i = this.defaultDimValueGetter = v_[r.sourceFormat];
		this._dimValueGetter = n || i, this._rawExtent = [], Tg(r), this._dimensions = I(t, function(e) {
			return {
				type: e.type,
				property: e.property
			};
		}), this._initDataFromProvider(0, e.count());
	}, e.prototype.getProvider = function() {
		return this._provider;
	}, e.prototype.getSource = function() {
		return this._provider.getSource();
	}, e.prototype.ensureCalculationDimension = function(e, t) {
		var n = this._calcDimNameToIdx, r = this._dimensions, i = n.get(e);
		if (i != null) {
			if (r[i].type === t) return i;
		} else i = r.length;
		return r[i] = { type: t }, n.set(e, i), this._chunks[i] = new __[t || "float"](this._rawCount), this._rawExtent[i] = Uo(), i;
	}, e.prototype.collectOrdinalMeta = function(e, t) {
		var n = this._chunks[e], r = this._dimensions[e], i = this._rawExtent, a = r.ordinalOffset || 0, o = n.length;
		a === 0 && (i[e] = Uo());
		for (var s = i[e], c = a; c < o; c++) {
			var l = n[c] = t.parseAndCollect(n[c]);
			isNaN(l) || (s[0] = Math.min(l, s[0]), s[1] = Math.max(l, s[1]));
		}
		r.ordinalMeta = t, r.ordinalOffset = o, r.type = "ordinal";
	}, e.prototype.getOrdinalMeta = function(e) {
		return this._dimensions[e].ordinalMeta;
	}, e.prototype.getDimensionProperty = function(e) {
		var t = this._dimensions[e];
		return t && t.property;
	}, e.prototype.appendData = function(e) {
		var t = this._provider, n = this.count();
		t.appendData(e);
		var r = t.count();
		return t.persistent || (r += n), n < r && this._initDataFromProvider(n, r, !0), [n, r];
	}, e.prototype.appendValues = function(e, t) {
		for (var n = this._chunks, r = this._dimensions, i = r.length, a = this._rawExtent, o = this.count(), s = o + Math.max(e.length, t || 0), c = 0; c < i; c++) {
			var l = r[c];
			x_(n, c, l.type, s, !0);
		}
		for (var u = [], d = o; d < s; d++) for (var f = d - o, p = 0; p < i; p++) {
			var l = r[p], m = v_.arrayRows.call(this, e[f] || u, l.property, f, p);
			n[p][d] = m;
			var h = a[p];
			m < h[0] && (h[0] = m), m > h[1] && (h[1] = m);
		}
		return this._rawCount = this._count = s, {
			start: o,
			end: s
		};
	}, e.prototype._initDataFromProvider = function(e, t, n) {
		for (var r = this._provider, i = this._chunks, a = this._dimensions, o = a.length, s = this._rawExtent, c = I(a, function(e) {
			return e.property;
		}), l = 0; l < o; l++) {
			var u = a[l];
			s[l] || (s[l] = Uo()), x_(i, l, u.type, t, n);
		}
		if (r.fillStorage) r.fillStorage(e, t, i, s);
		else for (var d = [], f = e; f < t; f++) {
			d = r.getItem(f, d);
			for (var p = 0; p < o; p++) {
				var m = i[p], h = this._dimValueGetter(d, c[p], f, p);
				m[f] = h;
				var g = s[p];
				h < g[0] && (g[0] = h), h > g[1] && (g[1] = h);
			}
		}
		!r.persistent && r.clean && r.clean(), this._rawCount = this._count = t, this._extent = [];
	}, e.prototype.count = function() {
		return this._count;
	}, e.prototype.get = function(e, t) {
		if (!(t >= 0 && t < this._count)) return NaN;
		var n = this._chunks[e];
		return n ? n[this.getRawIndex(t)] : NaN;
	}, e.prototype.getValues = function(e, t) {
		var n = [], r = [];
		if (t == null) {
			t = e, e = [];
			for (var i = 0; i < this._dimensions.length; i++) r.push(i);
		} else r = e;
		for (var i = 0, a = r.length; i < a; i++) n.push(this.get(r[i], t));
		return n;
	}, e.prototype.getByRawIndex = function(e, t) {
		if (!(t >= 0 && t < this._rawCount)) return NaN;
		var n = this._chunks[e];
		return n ? n[t] : NaN;
	}, e.prototype.getSum = function(e) {
		var t = this._chunks[e], n = 0;
		if (t) for (var r = 0, i = this.count(); r < i; r++) {
			var a = this.get(e, r);
			isNaN(a) || (n += a);
		}
		return n;
	}, e.prototype.getMedian = function(e) {
		var t = [];
		this.each([e], function(e) {
			isNaN(e) || t.push(e);
		}), Ga(t);
		var n = this.count();
		return n === 0 ? 0 : n % 2 == 1 ? t[(n - 1) / 2] : (t[n / 2] + t[n / 2 - 1]) / 2;
	}, e.prototype.indexOfRawIndex = function(e) {
		if (e >= this._rawCount || e < 0) return -1;
		if (!this._indices) return e;
		var t = this._indices, n = t[e];
		if (n != null && n < this._count && n === e) return e;
		for (var r = 0, i = this._count - 1; r <= i;) {
			var a = (r + i) / 2 | 0;
			if (t[a] < e) r = a + 1;
			else if (t[a] > e) i = a - 1;
			else return a;
		}
		return -1;
	}, e.prototype.getIndices = function() {
		var e, t = this._indices;
		if (t) {
			var n = t.constructor, r = this._count;
			if (n === Array) {
				e = new n(r);
				for (var i = 0; i < r; i++) e[i] = t[i];
			} else e = new n(t.buffer, 0, r);
		} else {
			var n = y_(this._rawCount);
			e = new n(this.count());
			for (var i = 0; i < e.length; i++) e[i] = i;
		}
		return e;
	}, e.prototype.filter = function(e, t) {
		if (!this._count) return this;
		for (var n = this.clone(), r = n.count(), i = new (y_(n._rawCount))(r), a = [], o = e.length, s = 0, c = e[0], l = n._chunks, u = 0; u < r; u++) {
			var d = void 0, f = n.getRawIndex(u);
			if (o === 0) d = t(u);
			else if (o === 1) {
				var p = l[c][f];
				d = t(p, u);
			} else {
				for (var m = 0; m < o; m++) a[m] = l[e[m]][f];
				a[m] = u, d = t.apply(null, a);
			}
			d && (i[s++] = f);
		}
		return s < r && (n._indices = i), n._count = s, n._extent = [], n._updateGetRawIdx(), n;
	}, e.prototype.selectRange = function(e) {
		var t = this.clone(), n = t._count;
		if (!n) return this;
		var r = L(e), i = r.length;
		if (!i) return this;
		var a = t.count(), o = new (y_(t._rawCount))(a), s = 0, c = r[0], l = e[c][0], u = e[c][1], d = t._chunks, f = !1;
		if (!t._indices) {
			var p = 0;
			if (i === 1) {
				for (var m = d[r[0]], h = 0; h < n; h++) {
					var g = m[h];
					(g >= l && g <= u || isNaN(g)) && (o[s++] = p), p++;
				}
				f = !0;
			} else if (i === 2) {
				for (var m = d[r[0]], _ = d[r[1]], v = e[r[1]][0], y = e[r[1]][1], h = 0; h < n; h++) {
					var g = m[h], b = _[h];
					(g >= l && g <= u || isNaN(g)) && (b >= v && b <= y || isNaN(b)) && (o[s++] = p), p++;
				}
				f = !0;
			}
		}
		if (!f) {
			if (i === 1) for (var h = 0; h < a; h++) {
				var x = t.getRawIndex(h), g = d[r[0]][x];
				(g >= l && g <= u || isNaN(g)) && (o[s++] = x);
			}
			else for (var h = 0; h < a; h++) {
				for (var S = !0, x = t.getRawIndex(h), C = 0; C < i; C++) {
					var w = r[C], g = d[w][x];
					(g < e[w][0] || g > e[w][1]) && (S = !1);
				}
				S && (o[s++] = t.getRawIndex(h));
			}
		}
		return s < a && (t._indices = o), t._count = s, t._extent = [], t._updateGetRawIdx(), t;
	}, e.prototype.map = function(e, t) {
		var n = this.clone(e);
		return this._updateDims(n, e, t), n;
	}, e.prototype.modify = function(e, t) {
		this._updateDims(this, e, t);
	}, e.prototype._updateDims = function(e, t, n) {
		for (var r = e._chunks, i = [], a = t.length, o = e.count(), s = [], c = e._rawExtent, l = 0; l < t.length; l++) c[t[l]] = Uo();
		for (var u = 0; u < o; u++) {
			for (var d = e.getRawIndex(u), f = 0; f < a; f++) s[f] = r[t[f]][d];
			s[a] = u;
			var p = n && n.apply(null, s);
			if (p != null) {
				typeof p != "object" && (i[0] = p, p = i);
				for (var l = 0; l < p.length; l++) {
					var m = t[l], h = p[l], g = c[m], _ = r[m];
					_ && (_[d] = h), h < g[0] && (g[0] = h), h > g[1] && (g[1] = h);
				}
			}
		}
	}, e.prototype.lttbDownSample = function(e, t) {
		var n = this.clone([e], !0), r = n._chunks[e], i = this.count(), a = 0, o = Math.floor(1 / t), s = this.getRawIndex(0), c, l, u, d = new (y_(this._rawCount))(Math.min((Math.ceil(i / o) + 2) * 2, i));
		d[a++] = s;
		for (var f = 1; f < i - 1; f += o) {
			for (var p = Math.min(f + o, i - 1), m = Math.min(f + o * 2, i), h = (m + p) / 2, g = 0, _ = p; _ < m; _++) {
				var v = this.getRawIndex(_), y = r[v];
				isNaN(y) || (g += y);
			}
			g /= m - p;
			var b = f, x = Math.min(f + o, i), S = f - 1, C = r[s];
			c = -1, u = b;
			for (var w = -1, T = 0, _ = b; _ < x; _++) {
				var v = this.getRawIndex(_), y = r[v];
				isNaN(y) ? (T++, w < 0 && (w = v)) : (l = Math.abs((S - h) * (y - C) - (S - _) * (g - C)), l > c && (c = l, u = v));
			}
			T > 0 && T < x - b && (d[a++] = Math.min(w, u), u = Math.max(w, u)), d[a++] = u, s = u;
		}
		return d[a++] = this.getRawIndex(i - 1), n._count = a, n._indices = d, n.getRawIndex = this._getRawIdx, n;
	}, e.prototype.minmaxDownSample = function(e, t) {
		for (var n = this.clone([e], !0), r = n._chunks, i = Math.floor(1 / t), a = r[e], o = this.count(), s = new (y_(this._rawCount))(Math.ceil(o / i) * 2), c = 0, l = 0; l < o; l += i) {
			var u = l, d = a[this.getRawIndex(u)], f = l, p = a[this.getRawIndex(f)], m = i;
			l + i > o && (m = o - l);
			for (var h = 0; h < m; h++) {
				var g = a[this.getRawIndex(l + h)];
				g < d && (d = g, u = l + h), g > p && (p = g, f = l + h);
			}
			var _ = this.getRawIndex(u), v = this.getRawIndex(f);
			u < f ? (s[c++] = _, s[c++] = v) : (s[c++] = v, s[c++] = _);
		}
		return n._count = c, n._indices = s, n._updateGetRawIdx(), n;
	}, e.prototype.downSample = function(e, t, n, r) {
		for (var i = this.clone([e], !0), a = i._chunks, o = [], s = Math.floor(1 / t), c = a[e], l = this.count(), u = i._rawExtent[e] = Uo(), d = new (y_(this._rawCount))(Math.ceil(l / s)), f = 0, p = 0; p < l; p += s) {
			s > l - p && (s = l - p, o.length = s);
			for (var m = 0; m < s; m++) {
				var h = this.getRawIndex(p + m);
				o[m] = c[h];
			}
			var g = n(o), _ = this.getRawIndex(Math.min(p + r(o, g) || 0, l - 1));
			c[_] = g, g < u[0] && (u[0] = g), g > u[1] && (u[1] = g), d[f++] = _;
		}
		return i._count = f, i._indices = d, i._updateGetRawIdx(), i;
	}, e.prototype.each = function(e, t) {
		if (this._count) for (var n = e.length, r = this._chunks, i = 0, a = this.count(); i < a; i++) {
			var o = this.getRawIndex(i);
			switch (n) {
				case 0:
					t(i);
					break;
				case 1:
					t(r[e[0]][o], i);
					break;
				case 2:
					t(r[e[0]][o], r[e[1]][o], i);
					break;
				default:
					for (var s = 0, c = []; s < n; s++) c[s] = r[e[s]][o];
					c[s] = i, t.apply(null, c);
			}
		}
	}, e.prototype.getDataExtent = function(e, t) {
		var n = this._chunks[e], r = Uo();
		if (!n) return r;
		var i = this.count();
		if (!this._indices && !t) return this._rawExtent[e].slice();
		var a = this._extent, o = a[e] || (a[e] = {}), s = e_(t), c = s.key, l = o[c];
		if (l) return l.slice();
		for (var u = r[0], d = r[1], f = 0; f < i; f++) {
			var p = n[this.getRawIndex(f)];
			(!t || t_(s, p)) && (p < u && (u = p), p > d && (d = p));
		}
		return o[c] = [u, d];
	}, e.prototype.getRawDataItem = function(e) {
		var t = this.getRawIndex(e);
		if (this._provider.persistent) return this._provider.getItem(t);
		for (var n = [], r = this._chunks, i = 0; i < r.length; i++) n.push(r[i][t]);
		return n;
	}, e.prototype.clone = function(t, n) {
		var r = new e(), i = this._chunks, a = t && re(t, function(e, t) {
			return e[t] = !0, e;
		}, {});
		if (a) for (var o = 0; o < i.length; o++) r._chunks[o] = a[o] ? b_(i[o]) : i[o];
		else r._chunks = i;
		return this._copyCommonProps(r), n || (r._indices = this._cloneIndices()), r._updateGetRawIdx(), r;
	}, e.prototype._copyCommonProps = function(e) {
		e._count = this._count, e._rawCount = this._rawCount, e._provider = this._provider, e._dimensions = this._dimensions, e._extent = k(this._extent), e._rawExtent = k(this._rawExtent);
	}, e.prototype._cloneIndices = function() {
		if (this._indices) {
			var e = this._indices.constructor, t = void 0;
			if (e === Array) {
				var n = this._indices.length;
				t = new e(n);
				for (var r = 0; r < n; r++) t[r] = this._indices[r];
			} else t = new e(this._indices);
			return t;
		}
		return null;
	}, e.prototype._getRawIdxIdentity = function(e) {
		return e;
	}, e.prototype._getRawIdx = function(e) {
		return e < this._count && e >= 0 ? this._indices[e] : -1;
	}, e.prototype._updateGetRawIdx = function() {
		this.getRawIndex = this._indices ? this._getRawIdx : this._getRawIdxIdentity;
	}, e.internalField = function() {
		function e(e, t, n, r) {
			return Zg(e[r], this._dimensions[r]);
		}
		v_ = {
			arrayRows: e,
			objectRows: function(e, t, n, r) {
				return Zg(e[t], this._dimensions[r]);
			},
			keyedColumns: e,
			original: function(e, t, n, r) {
				var i = e && (e.value == null ? e : e.value);
				return Zg(i instanceof Array ? i[r] : i, this._dimensions[r]);
			},
			typedArray: function(e, t, n, r) {
				return e[r];
			}
		};
	}(), e;
}(), C_ = function() {
	function e(e) {
		this._sourceList = [], this._storeList = [], this._upstreamSignList = [], this._versionSignBase = 0, this._dirty = !0, this._sourceHost = e;
	}
	return e.prototype.dirty = function() {
		this._setLocalSource([], []), this._storeList = [], this._dirty = !0;
	}, e.prototype._setLocalSource = function(e, t) {
		this._sourceList = e, this._upstreamSignList = t, this._versionSignBase++, this._versionSignBase > 9e10 && (this._versionSignBase = 0);
	}, e.prototype._getVersionSign = function() {
		return this._sourceHost.uid + "_" + this._versionSignBase;
	}, e.prototype.prepareSource = function() {
		this._isDirty() && (this._createSource(), this._dirty = !1);
	}, e.prototype._createSource = function() {
		this._setLocalSource([], []);
		var e = this._sourceHost, t = this._getUpstreamSourceManagers(), n = !!t.length, r, i;
		if (w_(e)) {
			var a = e, o = void 0, s = void 0, c = void 0;
			if (n) {
				var l = t[0];
				l.prepareSource(), c = l.getSource(), o = c.data, s = c.sourceFormat, i = [l._getVersionSign()];
			} else o = a.get("data", !0), s = ce(o) ? Rl : Pl, i = [];
			var u = this._getSourceMetaRawOption() || {}, d = c && c.metaRawOption || {}, f = G(u.seriesLayoutBy, d.seriesLayoutBy) || null, p = G(u.sourceHeader, d.sourceHeader), m = G(u.dimensions, d.dimensions);
			r = f !== d.seriesLayoutBy || !!p != !!d.sourceHeader || m ? [_g(o, {
				seriesLayoutBy: f,
				sourceHeader: p,
				dimensions: m
			}, s)] : [];
		} else {
			var h = e;
			if (n) {
				var g = this._applyTransform(t);
				r = g.sourceList, i = g.upstreamSignList;
			} else r = [_g(h.get("source", !0), this._getSourceMetaRawOption(), null)], i = [];
		}
		this._setLocalSource(r, i);
	}, e.prototype._applyTransform = function(e) {
		var t = this._sourceHost, n = t.get("transform", !0), r = t.get("fromTransformResult", !0);
		r != null && e.length !== 1 && T_("");
		var i, a = [], o = [];
		return F(e, function(e) {
			e.prepareSource();
			var t = e.getSource(r || 0);
			r != null && !t && T_(""), a.push(t), o.push(e._getVersionSign());
		}), n ? i = u_(n, a, { datasetIndex: t.componentIndex }) : r != null && (i = [yg(a[0])]), {
			sourceList: i,
			upstreamSignList: o
		};
	}, e.prototype._isDirty = function() {
		if (this._dirty) return !0;
		for (var e = this._getUpstreamSourceManagers(), t = 0; t < e.length; t++) {
			var n = e[t];
			if (n._isDirty() || this._upstreamSignList[t] !== n._getVersionSign()) return !0;
		}
	}, e.prototype.getSource = function(e) {
		e ||= 0;
		var t = this._sourceList[e];
		if (!t) {
			var n = this._getUpstreamSourceManagers();
			return n[0] && n[0].getSource(e);
		}
		return t;
	}, e.prototype.getSharedDataStore = function(e) {
		var t = e.makeStoreSchema();
		return this._innerGetDataStore(t.dimensions, e.source, t.hash);
	}, e.prototype._innerGetDataStore = function(e, t, n) {
		var r = 0, i = this._storeList, a = i[r];
		a ||= i[r] = {};
		var o = a[n];
		if (!o) {
			var s = this._getUpstreamSourceManagers()[0];
			w_(this._sourceHost) && s ? o = s._innerGetDataStore(e, t, n) : (o = new S_(), o.initData(new Mg(t, e.length), e)), a[n] = o;
		}
		return o;
	}, e.prototype._getUpstreamSourceManagers = function() {
		var e = this._sourceHost;
		if (w_(e)) {
			var t = mh(e);
			return t ? [t.getSourceManager()] : [];
		}
		return I(hh(e), function(e) {
			return e.getSourceManager();
		});
	}, e.prototype._getSourceMetaRawOption = function() {
		var e = this._sourceHost, t, n, r;
		if (w_(e)) t = e.get("seriesLayoutBy", !0), n = e.get("sourceHeader", !0), r = e.get("dimensions", !0);
		else if (!this._getUpstreamSourceManagers().length) {
			var i = e;
			t = i.get("seriesLayoutBy", !0), n = i.get("sourceHeader", !0), r = i.get("dimensions", !0);
		}
		return {
			seriesLayoutBy: t,
			sourceHeader: n,
			dimensions: r
		};
	}, e;
}();
function w_(e) {
	return e.mainType === "series";
}
function T_(e) {
	throw Error(e);
}
//#endregion
//#region node_modules/echarts/lib/component/tooltip/tooltipMarkup.js
var E_ = "line-height:1";
function D_(e) {
	var t = e.lineHeight;
	return t == null ? E_ : "line-height:" + ft(t + "") + "px";
}
function O_(e, t) {
	var n = e.color || Q.color.tertiary, r = e.fontSize || 12, i = e.fontWeight || "400", a = e.color || Q.color.secondary, o = e.fontSize || 14, s = e.fontWeight || "900";
	return t === "html" ? {
		nameStyle: "font-size:" + ft(r + "") + "px;color:" + ft(n) + ";font-weight:" + ft(i + ""),
		valueStyle: "font-size:" + ft(o + "") + "px;color:" + ft(a) + ";font-weight:" + ft(s + "")
	} : {
		nameStyle: {
			fontSize: r,
			fill: n,
			fontWeight: i
		},
		valueStyle: {
			fontSize: o,
			fill: a,
			fontWeight: s
		}
	};
}
var k_ = [
	0,
	10,
	20,
	30
], A_ = [
	"",
	"\n",
	"\n\n",
	"\n\n\n"
];
function j_(e, t) {
	return t.type = e, t;
}
function M_(e) {
	return e.type === "section";
}
function N_(e) {
	return M_(e) ? F_ : I_;
}
function P_(e) {
	if (M_(e)) {
		var t = 0, n = e.blocks.length, r = n > 1 || n > 0 && !e.noHeader;
		return F(e.blocks, function(e) {
			var n = P_(e);
			n >= t && (t = n + +(r && (!n || M_(e) && !e.noHeader)));
		}), t;
	}
	return 0;
}
function F_(e, t, n, r) {
	var i = t.noHeader, a = R_(P_(t)), o = [], s = t.blocks || [];
	_e(!s || B(s)), s ||= [];
	var c = e.orderMode;
	if (t.sortBlocks && c) {
		s = s.slice();
		var l = {
			valueAsc: "asc",
			valueDesc: "desc"
		};
		if (ke(l, c)) {
			var u = new $g(l[c], null);
			s.sort(function(e, t) {
				return u.evaluate(e.sortParam, t.sortParam);
			});
		} else c === "seriesDesc" && s.reverse();
	}
	F(s, function(n, i) {
		var s = t.valueFormatter, c = N_(n)(s ? j(j({}, e), { valueFormatter: s }) : e, n, i > 0 ? a.html : 0, r);
		c != null && o.push(c);
	});
	var d = e.renderMode === "richText" ? o.join(a.richText) : z_(r, o.join(""), i ? n : a.html);
	if (i) return d;
	var f = Em(t.header, "ordinal", e.useUTC), p = O_(r, e.renderMode).nameStyle, m = D_(r);
	return e.renderMode === "richText" ? H_(e, f, p) + a.richText + d : z_(r, "<div style=\"" + p + ";" + m + ";\">" + ft(f) + "</div>" + d, n);
}
function I_(e, t, n, r) {
	var i = e.renderMode, a = t.noName, o = t.noValue, s = !t.markerType, c = t.name, l = e.useUTC, u = t.valueFormatter || e.valueFormatter || function(e) {
		return e = B(e) ? e : [e], I(e, function(e, t) {
			return Em(e, B(p) ? p[t] : p, l);
		});
	};
	if (!(a && o)) {
		var d = s ? "" : e.markupStyleCreator.makeTooltipMarker(t.markerType, t.markerColor || Q.color.secondary, i), f = a ? "" : Em(c, "ordinal", l), p = t.valueType, m = o ? [] : u(t.value, t.rawDataIndex), h = !s || !a, g = !s && a, _ = O_(r, i), v = _.nameStyle, y = _.valueStyle;
		return i === "richText" ? (s ? "" : d) + (a ? "" : H_(e, f, v)) + (o ? "" : U_(e, m, h, g, y)) : z_(r, (s ? "" : d) + (a ? "" : B_(f, !s, v)) + (o ? "" : V_(m, h, g, y)), n);
	}
}
function L_(e, t, n, r, i, a) {
	if (e) return N_(e)({
		useUTC: i,
		renderMode: n,
		orderMode: r,
		markupStyleCreator: t,
		valueFormatter: e.valueFormatter
	}, e, 0, a);
}
function R_(e) {
	return {
		html: k_[e],
		richText: A_[e]
	};
}
function z_(e, t, n) {
	var r = "<div style=\"clear:both\"></div>", i = "margin: " + n + "px 0 0", a = D_(e);
	return "<div style=\"" + i + ";" + a + ";\">" + t + r + "</div>";
}
function B_(e, t, n) {
	var r = t ? "margin-left:2px" : "";
	return "<span style=\"" + n + ";" + r + "\">" + ft(e) + "</span>";
}
function V_(e, t, n, r) {
	var i = t ? "float:right;margin-left:" + (n ? "10px" : "20px") : "";
	return e = B(e) ? e : [e], "<span style=\"" + i + ";" + r + "\">" + I(e, function(e) {
		return ft(e);
	}).join("&nbsp;&nbsp;") + "</span>";
}
function H_(e, t, n) {
	return e.markupStyleCreator.wrapRichTextStyle(t, n);
}
function U_(e, t, n, r, i) {
	var a = [i], o = r ? 10 : 20;
	return n && a.push({
		padding: [
			0,
			0,
			0,
			o
		],
		align: "right"
	}), e.markupStyleCreator.wrapRichTextStyle(B(t) ? t.join("  ") : t, a);
}
function W_(e, t) {
	var n = e.getData().getItemVisual(t, "style")[e.visualDrawType];
	return jm(n);
}
function G_(e, t) {
	return e.get("padding") ?? (t === "richText" ? [8, 10] : 10);
}
var K_ = function() {
	function e() {
		this.richTextStyles = {}, this._nextStyleNameId = to();
	}
	return e.prototype._generateStyleName = function() {
		return "__EC_aUTo_" + this._nextStyleNameId++;
	}, e.prototype.makeTooltipMarker = function(e, t, n) {
		var r = n === "richText" ? this._generateStyleName() : null, i = Am({
			color: t,
			type: e,
			renderMode: n,
			markerId: r
		});
		return H(i) ? i : (this.richTextStyles[r] = i.style, i.content);
	}, e.prototype.wrapRichTextStyle = function(e, t) {
		var n = {};
		B(t) ? F(t, function(e) {
			return j(n, e);
		}) : j(n, t);
		var r = this._generateStyleName();
		return this.richTextStyles[r] = n, "{" + r + "|" + e + "}";
	}, e;
}();
//#endregion
//#region node_modules/echarts/lib/component/tooltip/seriesFormatTooltip.js
function q_(e) {
	var t = e.series, n = e.dataIndex, r = e.multipleSeries, i = t.getData(), a = i.mapDimensionsAll("defaultedTooltip"), o = a.length, s = t.getRawValue(n), c = B(s), l = W_(t, n), u, d, f, p;
	if (o > 1 || c && !o) {
		var m = J_(s, t, n, a, l);
		u = m.inlineValues, d = m.inlineValueTypes, f = m.blocks, p = m.inlineValues[0];
	} else if (o) {
		var h = i.getDimensionInfo(a[0]);
		p = u = Wg(i, n, a[0]), d = h.type;
	} else p = u = c ? s[0] : s;
	var g = ko(t), _ = g && t.name || "", v = i.getName(n), y = r ? _ : v;
	return j_("section", {
		header: _,
		noHeader: r || !g,
		sortParam: p,
		blocks: [j_("nameValue", {
			markerType: "item",
			markerColor: l,
			name: y,
			noName: !ve(y),
			value: u,
			valueType: d,
			rawDataIndex: i.getRawIndex(n)
		})].concat(f || [])
	});
}
function J_(e, t, n, r, i) {
	var a = t.getData(), o = re(e, function(e, t, n) {
		var r = a.getDimensionInfo(n);
		return e ||= r && r.tooltip !== !1 && r.displayName != null;
	}, !1), s = [], c = [], l = [];
	r.length ? F(r, function(e) {
		u(Wg(a, n, e), e);
	}) : F(e, u);
	function u(e, t) {
		var n = a.getDimensionInfo(t);
		n && n.otherDims.tooltip !== !1 && (o ? l.push(j_("nameValue", {
			markerType: "subItem",
			markerColor: i,
			name: n.displayName,
			value: e,
			valueType: n.type
		})) : (s.push(e), c.push(n.type)));
	}
	return {
		inlineValues: s,
		inlineValueTypes: c,
		blocks: l
	};
}
//#endregion
//#region node_modules/echarts/lib/model/Series.js
var Y_ = Y();
function X_(e, t) {
	return e.getName(t) || e.getId(t);
}
var Z_ = function(e) {
	r(t, e);
	function t() {
		var t = e !== null && e.apply(this, arguments) || this;
		return t._selectedDataIndicesMap = {}, t;
	}
	return t.prototype.init = function(e, t, n) {
		this.seriesIndex = this.componentIndex, this.dataTask = Jg({
			count: ev,
			reset: tv
		}), this.dataTask.context = { model: this }, this.mergeDefaultAndTheme(e, n), (Y_(this).sourceManager = new C_(this)).prepareSource();
		var r = this.getInitialData(e, n);
		rv(r, this), this.dataTask.context.data = r, Y_(this).dataBeforeProcessed = r, Q_(this), this._initSelectedMapFromData(r);
	}, t.prototype.mergeDefaultAndTheme = function(e, t) {
		var n = Ym(this), r = n ? Zm(e) : {}, i = this.subType;
		eh.hasClass(i) && (i += "Series"), A(e, t.getTheme().get(this.subType)), A(e, this.getDefaultOption()), ho(e, "label", ["show"]), this.fillDataTextStyle(e.data), n && Xm(e, r, n);
	}, t.prototype.mergeOption = function(e, t) {
		e = A(this.option, e, !0), this.fillDataTextStyle(e.data);
		var n = Ym(this);
		n && Xm(this.option, e, n);
		var r = Y_(this).sourceManager;
		r.dirty(), r.prepareSource();
		var i = this.getInitialData(e, t);
		rv(i, this), this.dataTask.dirty(), this.dataTask.context.data = i, Y_(this).dataBeforeProcessed = i, Q_(this), this._initSelectedMapFromData(i);
	}, t.prototype.fillDataTextStyle = function(e) {
		if (e && !ce(e)) for (var t = ["show"], n = 0; n < e.length; n++) e[n] && e[n].label && ho(e[n], "label", t);
	}, t.prototype.getInitialData = function(e, t) {}, t.prototype.appendData = function(e) {
		this.getRawData().appendData(e.data);
	}, t.prototype.getData = function(e) {
		var t = av(this);
		if (t) {
			var n = t.context.data;
			return e == null || !n.getLinkedData ? n : n.getLinkedData(e);
		}
		return Y_(this).data;
	}, t.prototype.getAllData = function() {
		var e = this.getData();
		return e && e.getLinkedDataAll ? e.getLinkedDataAll() : [{ data: e }];
	}, t.prototype.setData = function(e) {
		var t = av(this);
		if (t) {
			var n = t.context;
			n.outputData = e, t !== this.dataTask && (n.data = e);
		}
		Y_(this).data = e;
	}, t.prototype.getEncode = function() {
		var e = this.get("encode", !0);
		if (e) return K(e);
	}, t.prototype.getSourceManager = function() {
		return Y_(this).sourceManager;
	}, t.prototype.getSource = function() {
		return this.getSourceManager().getSource();
	}, t.prototype.getRawData = function() {
		return Y_(this).dataBeforeProcessed;
	}, t.prototype.getColorBy = function() {
		return this.get("colorBy") || "series";
	}, t.prototype.isColorBySeries = function() {
		return this.getColorBy() === "series";
	}, t.prototype.getBaseAxis = function() {
		var e = this.coordinateSystem;
		return e && e.getBaseAxis && e.getBaseAxis();
	}, t.prototype.indicesOfNearest = function(e, t, n, r) {
		var i = this.getData(), a = this.coordinateSystem, o = a && a.getAxis(e);
		if (!a || !o) return [];
		var s = o.dataToCoord(n);
		r ??= Infinity;
		for (var c = [], l = Infinity, u = -1, d = 0, f = i.getDimensionIndex(t), p = i.getStore(), m = 0, h = p.count(); m < h; m++) {
			var g = p.get(f, m), _ = s - o.dataToCoord(g), v = Math.abs(_);
			v <= r && ((v < l || v === l && _ >= 0 && u < 0) && (l = v, u = _, d = 0), _ === u && (c[d++] = m));
		}
		return c.length = d, c;
	}, t.prototype.formatTooltip = function(e, t, n) {
		return q_({
			series: this,
			dataIndex: e,
			multipleSeries: t
		});
	}, t.prototype.isAnimationEnabled = function() {
		var e = this.ecModel;
		if (a.node && !(e && e.ssr)) return !1;
		var t = this.getShallow("animation");
		return t && this.getData().count() > this.getShallow("animationThreshold") && (t = !1), !!t;
	}, t.prototype.restoreData = function() {
		this.dataTask.dirty();
	}, t.prototype.getColorFromPalette = function(e, t, n) {
		var r = this.ecModel, i = xh.prototype.getColorFromPalette.call(this, e, t, n);
		return i ||= r.getColorFromPalette(e, t, n), i;
	}, t.prototype.coordDimToDataDim = function(e) {
		return this.getRawData().mapDimensionsAll(e);
	}, t.prototype.getProgressive = function() {
		return this.get("progressive");
	}, t.prototype.getProgressiveThreshold = function() {
		return this.get("progressiveThreshold");
	}, t.prototype.select = function(e, t) {
		this._innerSelect(this.getData(t), e);
	}, t.prototype.unselect = function(e, t) {
		var n = this.option.selectedMap;
		if (n) {
			var r = this.option.selectedMode, i = this.getData(t);
			if (r === "series" || n === "all") this.option.selectedMap = {}, this._selectedDataIndicesMap = {};
			else for (var a = 0; a < e.length; a++) {
				var o = e[a], s = X_(i, o);
				n[s] = !1, this._selectedDataIndicesMap[s] = -1;
			}
		}
	}, t.prototype.toggleSelect = function(e, t) {
		for (var n = [], r = 0; r < e.length; r++) n[0] = e[r], this.isSelected(e[r], t) ? this.unselect(n, t) : this.select(n, t);
	}, t.prototype.getSelectedDataIndices = function() {
		if (this.option.selectedMap === "all") return [].slice.call(this.getData().getIndices());
		for (var e = this._selectedDataIndicesMap, t = L(e), n = [], r = 0; r < t.length; r++) {
			var i = e[t[r]];
			i >= 0 && n.push(i);
		}
		return n;
	}, t.prototype.isSelected = function(e, t) {
		var n = this.option.selectedMap;
		if (!n) return !1;
		var r = this.getData(t);
		return (n === "all" || n[X_(r, e)]) && !r.getItemModel(e).get(["select", "disabled"]);
	}, t.prototype.isUniversalTransitionEnabled = function() {
		if (this.__universalTransitionEnabled) return !0;
		var e = this.option.universalTransition;
		return e ? e === !0 || e && e.enabled : !1;
	}, t.prototype._innerSelect = function(e, t) {
		var n, r, i = this.option, a = i.selectedMode, o = t.length;
		if (a && o) {
			if (a === "series") i.selectedMap = "all";
			else if (a === "multiple") {
				W(i.selectedMap) || (i.selectedMap = {});
				for (var s = i.selectedMap, c = 0; c < o; c++) {
					var l = t[c], u = X_(e, l);
					s[u] = !0, this._selectedDataIndicesMap[u] = e.getRawIndex(l);
				}
			} else if (a === "single" || a === !0) {
				var d = t[o - 1], u = X_(e, d);
				i.selectedMap = (n = {}, n[u] = !0, n), this._selectedDataIndicesMap = (r = {}, r[u] = e.getRawIndex(d), r);
			}
		}
	}, t.prototype._initSelectedMapFromData = function(e) {
		if (!this.option.selectedMap) {
			var t = [];
			e.hasItemOption && e.each(function(n) {
				var r = e.getRawDataItem(n);
				r && r.selected && t.push(n);
			}), t.length > 0 && this._innerSelect(e, t);
		}
	}, t.registerClass = function(e) {
		return eh.registerClass(e);
	}, t.protoInitialize = function() {
		var e = t.prototype;
		e.type = "series.__base__", e.seriesIndex = 0, e.ignoreStyleOnData = !1, e.hasSymbolVisual = !1, e.defaultSymbol = "circle", e.visualStyleAccessPath = "itemStyle", e.visualDrawType = "fill";
	}(), t;
}(eh);
ne(Z_, Kg), ne(Z_, xh), is(Z_, eh);
function Q_(e) {
	var t = e.name;
	ko(e) || (e.name = $_(e) || t);
}
function $_(e) {
	var t = e.getRawData(), n = t.mapDimensionsAll("seriesName"), r = [];
	return F(n, function(e) {
		var n = t.getDimensionInfo(e);
		n.displayName && r.push(n.displayName);
	}), r.join(" ");
}
function ev(e) {
	return e.model.getRawData().count();
}
function tv(e) {
	var t = e.model;
	return t.setData(t.getRawData().cloneShallow()), nv;
}
function nv(e, t) {
	t.outputData && e.end > t.outputData.count() && t.model.getRawData().cloneShallow(t.outputData);
}
function rv(e, t) {
	F(Ee(e.CHANGABLE_METHODS, e.DOWNSAMPLE_METHODS), function(n) {
		e.wrapMethod(n, z(iv, t));
	});
}
function iv(e, t) {
	var n = av(e);
	return n && n.setOutputEnd((t || this).count()), t;
}
function av(e) {
	var t = (e.ecModel || {}).scheduler, n = t && t.getPipeline(e.uid);
	if (n) {
		var r = n.currentTask;
		if (r) {
			var i = r.agentStubMap;
			i && (r = i.get(e.uid));
		}
		return r;
	}
}
//#endregion
//#region node_modules/echarts/lib/view/Component.js
var ov = function() {
	function e() {
		this.group = new ba(), this.uid = Jp("viewComponent");
	}
	return e.prototype.init = function(e, t) {}, e.prototype.render = function(e, t, n, r) {}, e.prototype.dispose = function(e, t) {}, e.prototype.updateView = function(e, t, n, r) {}, e.prototype.updateLayout = function(e, t, n, r) {}, e.prototype.updateVisual = function(e, t, n, r) {}, e.prototype.toggleBlurSeries = function(e, t, n) {}, e.prototype.eachRendered = function(e) {
		var t = this.group;
		t && t.traverse(e);
	}, e;
}();
ns(ov), ls(ov);
//#endregion
//#region node_modules/echarts/lib/chart/helper/createRenderPlanner.js
function sv() {
	var e = Y();
	return function(t) {
		var n = e(t), r = t.pipelineContext, i = !!n.large, a = !!n.progressiveRender, o = n.large = !!(r && r.large), s = n.progressiveRender = !!(r && r.progressiveRender);
		return (i !== o || a !== s) && "reset";
	};
}
//#endregion
//#region node_modules/echarts/lib/view/Chart.js
var cv = Y(), lv = sv(), uv = function() {
	function e() {
		this.group = new ba(), this.uid = Jp("viewChart"), this.renderTask = Jg({
			plan: pv,
			reset: mv
		}), this.renderTask.context = { view: this };
	}
	return e.prototype.init = function(e, t) {}, e.prototype.render = function(e, t, n, r) {}, e.prototype.highlight = function(e, t, n, r) {
		var i = e.getData(r && r.dataType);
		i && fv(i, r, "emphasis");
	}, e.prototype.downplay = function(e, t, n, r) {
		var i = e.getData(r && r.dataType);
		i && fv(i, r, "normal");
	}, e.prototype.remove = function(e, t) {
		this.group.removeAll();
	}, e.prototype.dispose = function(e, t) {}, e.prototype.updateView = function(e, t, n, r) {
		this.render(e, t, n, r);
	}, e.prototype.updateVisual = function(e, t, n, r) {
		this.render(e, t, n, r);
	}, e.prototype.eachRendered = function(e) {
		sp(this.group, e);
	}, e.markUpdateMethod = function(e, t) {
		cv(e).updateMethod = t;
	}, e.protoInitialize = function() {
		var t = e.prototype;
		t.type = "chart";
	}(), e;
}();
function dv(e, t, n) {
	e && Uu(e) && (t === "emphasis" ? xu : Su)(e, n);
}
function fv(e, t, n) {
	var r = No(e, t), i = t && t.highlightKey != null ? Wu(t.highlightKey) : null;
	r == null ? e.eachItemGraphicEl(function(e) {
		dv(e, n, i);
	}) : F(mo(r), function(t) {
		dv(e.getItemGraphicEl(t), n, i);
	});
}
ns(uv, ["dispose"]), ls(uv);
function pv(e) {
	return lv(e.model);
}
function mv(e) {
	var t = e.model, n = e.ecModel, r = e.api, i = e.payload, a = t.pipelineContext.progressiveRender, o = e.view, s = i && cv(i).updateMethod, c = a ? "incrementalPrepareRender" : s && o[s] ? s : "render";
	return c !== "render" && o[c](t, n, r, i), hv[c];
}
var hv = {
	incrementalPrepareRender: { progress: function(e, t) {
		t.view.incrementalRender(e, t.model, t.ecModel, t.api, t.payload);
	} },
	render: {
		forceFirstProgress: !0,
		progress: function(e, t) {
			t.view.render(t.model, t.ecModel, t.api, t.payload);
		}
	}
}, gv = "\0__throttleOriginMethod", _v = "\0__throttleRate", vv = "\0__throttleType";
function yv(e, t, n) {
	var r, i = 0, a = 0, o = null, s, c, l, u;
	t ||= 0;
	function d() {
		a = (/* @__PURE__ */ new Date()).getTime(), o = null, e.apply(c, l || []);
	}
	var f = function() {
		var e = [...arguments];
		r = (/* @__PURE__ */ new Date()).getTime(), c = this, l = e;
		var f = u || t, p = u || n;
		u = null, s = r - (p ? i : a) - f, clearTimeout(o), p ? o = setTimeout(d, f) : s >= 0 ? d() : o = setTimeout(d, -s), i = r;
	};
	return f.clear = function() {
		o &&= (clearTimeout(o), null);
	}, f.debounceNextCall = function(e) {
		u = e;
	}, f;
}
function bv(e, t, n, r) {
	var i = e[t];
	if (i) {
		var a = i[gv] || i, o = i[vv];
		if (i[_v] !== n || o !== r) {
			if (n == null || !r) return e[t] = a;
			i = e[t] = yv(a, n, r === "debounce"), i[gv] = a, i[vv] = r, i[_v] = n;
		}
		return i;
	}
}
function xv(e, t) {
	var n = e[t];
	n && n[gv] && (n.clear && n.clear(), e[t] = n[gv]);
}
//#endregion
//#region node_modules/echarts/lib/visual/style.js
var Sv = Y(), Cv = {
	itemStyle: us(Up, !0),
	lineStyle: us(Bp, !0)
}, wv = {
	lineStyle: "stroke",
	itemStyle: "fill"
};
function Tv(e, t) {
	return e.visualStyleMapper || Cv[t] || (console.warn("Unknown style type '" + t + "'."), Cv.itemStyle);
}
function Ev(e, t) {
	return e.visualDrawType || wv[t] || (console.warn("Unknown style type '" + t + "'."), "fill");
}
var Dv = {
	createOnAllSeries: !0,
	performRawSeries: !0,
	reset: function(e, t) {
		var n = e.getData(), r = e.visualStyleAccessPath || "itemStyle", i = e.getModel(r), a = Tv(e, r)(i), o = i.getShallow("decal");
		o && (n.setVisual("decal", o), o.dirty = !0);
		var s = Ev(e, r), c = a[s], l = V(c) ? c : null, u = a.fill === "auto" || a.stroke === "auto";
		if (!a[s] || l || u) {
			var d = e.getColorFromPalette(e.name, null, t.getSeriesCount());
			a[s] || (a[s] = d, n.setVisual("colorFromPalette", !0)), a.fill = a.fill === "auto" || V(a.fill) ? d : a.fill, a.stroke = a.stroke === "auto" || V(a.stroke) ? d : a.stroke;
		}
		if (n.setVisual("style", a), n.setVisual("drawType", s), !t.isSeriesFiltered(e) && l) return n.setVisual("colorFromPalette", !1), { dataEach: function(t, n) {
			var r = e.getDataParams(n), i = j({}, a);
			i[s] = l(r), t.setItemVisual(n, "style", i);
		} };
	}
}, Ov = new Kp(), kv = {
	createOnAllSeries: !0,
	reset: function(e, t) {
		if (!e.ignoreStyleOnData) {
			var n = e.getData(), r = e.visualStyleAccessPath || "itemStyle", i = Tv(e, r), a = n.getVisual("drawType");
			return { dataEach: n.hasItemOption ? function(e, t) {
				var n = e.getRawDataItem(t);
				if (n && n[r]) {
					Ov.option = n[r];
					var o = i(Ov);
					j(e.ensureUniqueItemVisual(t, "style"), o), Ov.option.decal && (e.setItemVisual(t, "decal", Ov.option.decal), Ov.option.decal.dirty = !0), a in o && e.setItemVisual(t, "colorFromPalette", !1);
				}
			} : null };
		}
	}
}, Av = {
	performRawSeries: !0,
	overallReset: function(e) {
		var t = K();
		e.eachSeries(function(e) {
			if (!e.isColorBySeries()) {
				var n = e.type + "-" + e.getColorBy();
				Sv(e).scope = t.get(n) || t.set(n, {});
			}
		}), e.eachSeries(function(e) {
			if (!e.isColorBySeries()) {
				var t = e.getRawData(), n = {}, r = e.getData(), i = Sv(e).scope, a = Ev(e, e.visualStyleAccessPath || "itemStyle");
				r.each(function(e) {
					var t = r.getRawIndex(e);
					n[t] = e;
				}), t.each(function(o) {
					var s = n[o];
					if (r.getItemVisual(s, "colorFromPalette")) {
						var c = r.ensureUniqueItemVisual(s, "style"), l = t.getName(o) || o + "", u = t.count();
						c[a] = e.getColorFromPalette(l, i, u);
					}
				});
			}
		});
	}
}, jv = Math.PI;
function Mv(e, t) {
	t ||= {}, M(t, {
		text: "loading",
		textColor: Q.color.primary,
		fontSize: 12,
		fontWeight: "normal",
		fontStyle: "normal",
		fontFamily: "sans-serif",
		maskColor: "rgba(255,255,255,0.8)",
		showSpinner: !0,
		color: Q.color.theme[0],
		spinnerRadius: 10,
		lineWidth: 5,
		zlevel: 0
	});
	var n = new ba(), r = new dl({
		style: { fill: t.maskColor },
		zlevel: t.zlevel,
		z: 1e4
	});
	n.add(r);
	var i = new gl({
		style: {
			text: t.text,
			fill: t.textColor,
			fontSize: t.fontSize,
			fontWeight: t.fontWeight,
			fontStyle: t.fontStyle,
			fontFamily: t.fontFamily
		},
		zlevel: t.zlevel,
		z: 10001
	}), a = new dl({
		style: { fill: "none" },
		textContent: i,
		textConfig: {
			position: "right",
			distance: 10
		},
		zlevel: t.zlevel,
		z: 10001
	});
	n.add(a);
	var o;
	return t.showSpinner && (o = new $d({
		shape: {
			startAngle: -jv / 2,
			endAngle: -jv / 2 + .1,
			r: t.spinnerRadius
		},
		style: {
			stroke: t.color,
			lineCap: "round",
			lineWidth: t.lineWidth
		},
		zlevel: t.zlevel,
		z: 10001
	}), o.animateShape(!0).when(1e3, { endAngle: jv * 3 / 2 }).start("circularInOut"), o.animateShape(!0).when(1e3, { startAngle: jv * 3 / 2 }).delay(300).start("circularInOut"), n.add(o)), n.resize = function() {
		var n = i.getBoundingRect().width, s = t.showSpinner ? t.spinnerRadius : 0, c = (e.getWidth() - s * 2 - (t.showSpinner && n ? 10 : 0) - n) / 2 - (t.showSpinner && n ? 0 : 5 + n / 2) + (t.showSpinner ? 0 : n / 2) + (n ? 0 : s), l = e.getHeight() / 2;
		t.showSpinner && o.setShape({
			cx: c,
			cy: l
		}), a.setShape({
			x: c - s,
			y: l - s,
			width: s * 2,
			height: s * 2
		}), r.setShape({
			x: 0,
			y: 0,
			width: e.getWidth(),
			height: e.getHeight()
		});
	}, n.resize(), n;
}
//#endregion
//#region node_modules/echarts/lib/core/Scheduler.js
var Nv = function() {
	function e(e, t, n, r) {
		this._stageTaskMap = K(), this.ecInstance = e, this.api = t, n = this._dataProcessorHandlers = n.slice(), r = this._visualHandlers = r.slice(), this._allHandlers = n.concat(r);
	}
	return e.prototype.restoreData = function(e, t) {
		e.restoreData(t), this._stageTaskMap.each(function(e) {
			var t = e.overallTask;
			t && t.dirty();
		});
	}, e.prototype.getPerformArgs = function(e, t) {
		if (e.__pipeline) {
			var n = this._pipelineMap.get(e.__pipeline.id), r = n.context, i = !t && n.progressiveEnabled && (!r || r.progressiveRender) && e.__idxInPipeline > n.blockIndex ? n.step : null, a = r && r.modDataCount;
			return {
				step: i,
				modBy: a == null ? null : Math.ceil(a / i),
				modDataCount: a
			};
		}
	}, e.prototype.getPipeline = function(e) {
		return this._pipelineMap.get(e);
	}, e.prototype.updateStreamModes = function(e, t) {
		var n = this._pipelineMap.get(e.uid);
		e.pipelineContext = n.context = e.__preparePipelineContext ? e.__preparePipelineContext(t, n) : qo(e, t, n);
	}, e.prototype.restorePipelines = function(e, t) {
		var n = this, r = n._pipelineMap = K();
		t.eachSeries(function(t) {
			var i = e.painter.type === "canvas" && t.getProgressive(), a = t.uid;
			r.set(a, {
				id: a,
				head: null,
				tail: null,
				threshold: t.getProgressiveThreshold(),
				progressiveEnabled: i && !(t.preventIncremental && t.preventIncremental()),
				blockIndex: -1,
				step: Math.round(i || 700),
				count: 0
			}), n._pipe(t, t.dataTask);
		});
	}, e.prototype.prepareStageTasks = function() {
		var e = this._stageTaskMap, t = this.api.getModel(), n = this.api;
		F(this._allHandlers, function(r) {
			var i = e.get(r.uid) || e.set(r.uid, {});
			_e(!(r.reset && r.overallReset), ""), r.reset && this._createSeriesStageTask(r, i, t, n), r.overallReset && this._createOverallStageTask(r, i, t, n);
		}, this);
	}, e.prototype.prepareView = function(e, t, n, r) {
		var i = e.renderTask, a = i.context;
		a.model = t, a.ecModel = n, a.api = r, i.__block = !e.incrementalPrepareRender, this._pipe(t, i);
	}, e.prototype.performDataProcessorTasks = function(e, t) {
		this._performStageTasks(this._dataProcessorHandlers, e, t, { block: !0 });
	}, e.prototype.performVisualTasks = function(e, t, n) {
		this._performStageTasks(this._visualHandlers, e, t, n);
	}, e.prototype._performStageTasks = function(e, t, n, r) {
		r ||= {};
		var i = !1, a = this;
		F(e, function(e, s) {
			if (!(r.visualType && r.visualType !== e.visualType)) {
				var c = a._stageTaskMap.get(e.uid), l = c.seriesTaskMap, u = c.overallTask;
				if (u) {
					var d, f = u.agentStubMap;
					f.each(function(e) {
						o(r, e) && (e.dirty(), d = !0);
					}), d && u.dirty(), a.updatePayload(u, n);
					var p = a.getPerformArgs(u, r.block);
					f.each(function(e) {
						e.perform(p);
					}), u.perform(p) && (i = !0);
				} else l && l.each(function(s, c) {
					o(r, s) && s.dirty();
					var l = a.getPerformArgs(s, r.block);
					l.skip = !e.performRawSeries && t.isSeriesFiltered(s.context.model), a.updatePayload(s, n), s.perform(l) && (i = !0);
				});
			}
		});
		function o(e, t) {
			return e.setDirty && (!e.dirtyMap || e.dirtyMap.get(t.__pipeline.id));
		}
		this.unfinished = i || this.unfinished;
	}, e.prototype.performSeriesTasks = function(e) {
		var t;
		e.eachSeries(function(e) {
			t = e.dataTask.perform() || t;
		}), this.unfinished = t || this.unfinished;
	}, e.prototype.plan = function() {
		this._pipelineMap.each(function(e) {
			var t = e.tail;
			do {
				if (t.__block) {
					e.blockIndex = t.__idxInPipeline;
					break;
				}
				t = t.getUpstream();
			} while (t);
		});
	}, e.prototype.updatePayload = function(e, t) {
		t !== "remain" && (e.context.payload = t);
	}, e.prototype._createSeriesStageTask = function(e, t, n, r) {
		var i = this, a = t.seriesTaskMap, o = t.seriesTaskMap = K(), s = e.seriesType, c = e.getTargetSeries;
		e.createOnAllSeries ? n.eachRawSeries(l) : s ? n.eachRawSeriesByType(s, l) : c && c(n, r).each(l);
		function l(t) {
			var s = t.uid, c = o.set(s, a && a.get(s) || Jg({
				plan: Rv,
				reset: zv,
				count: Hv
			}));
			c.context = {
				model: t,
				ecModel: n,
				api: r,
				useClearVisual: e.isVisual && !e.isLayout,
				plan: e.plan,
				reset: e.reset,
				scheduler: i
			}, i._pipe(t, c);
		}
	}, e.prototype._createOverallStageTask = function(e, t, n, r) {
		var i = this, a = t.overallTask = t.overallTask || Jg({ reset: Pv });
		a.context = {
			ecModel: n,
			api: r,
			overallReset: e.overallReset,
			scheduler: i
		};
		var o = a.agentStubMap, s = a.agentStubMap = K(), c = e.seriesType, l = e.getTargetSeries, u = e.dirtyOnOverallProgress, d = !1;
		_e(!e.createOnAllSeries, ""), c ? n.eachRawSeriesByType(c, f) : l ? l(n, r).each(f) : F(n.getSeries(), f);
		function f(e) {
			var t = e.uid, n = s.set(t, o && o.get(t) || (d = !0, Jg({
				reset: Fv,
				onDirty: Lv
			})));
			n.context = {
				model: e,
				dirtyOnOverallProgress: u
			}, n.agent = a, n.__block = u, i._pipe(e, n);
		}
		d && a.dirty();
	}, e.prototype._pipe = function(e, t) {
		var n = e.uid, r = this._pipelineMap.get(n);
		!r.head && (r.head = t), r.tail && r.tail.pipe(t), r.tail = t, t.__idxInPipeline = r.count++, t.__pipeline = r;
	}, e.wrapStageHandler = function(e, t) {
		return V(e) && (e = {
			overallReset: e,
			seriesType: Uv(e)
		}), e.uid = Jp("stageHandler"), t && (e.visualType = t), e;
	}, e;
}();
function Pv(e) {
	e.overallReset(e.ecModel, e.api, e.payload);
}
function Fv(e) {
	return e.dirtyOnOverallProgress && Iv;
}
function Iv() {
	this.agent.dirty(), this.getDownstream().dirty();
}
function Lv() {
	this.agent && this.agent.dirty();
}
function Rv(e) {
	return e.plan ? e.plan(e.model, e.ecModel, e.api, e.payload) : null;
}
function zv(e) {
	e.useClearVisual && e.data.clearAllVisual();
	var t = e.resetDefines = mo(e.reset(e.model, e.ecModel, e.api, e.payload));
	return t.length > 1 ? I(t, function(e, t) {
		return Vv(t);
	}) : Bv;
}
var Bv = Vv(0);
function Vv(e) {
	return function(t, n) {
		var r = n.data, i = n.resetDefines[e];
		if (i && i.dataEach) for (var a = t.start; a < t.end; a++) i.dataEach(r, a);
		else i && i.progress && i.progress(t, r);
	};
}
function Hv(e) {
	return e.data.count();
}
function Uv(e) {
	Kv = null;
	try {
		e(Wv, Gv);
	} catch {}
	return Kv;
}
var Wv = {}, Gv = {}, Kv;
qv(Wv, Ah), qv(Gv, Ul), Wv.eachSeriesByType = Wv.eachRawSeriesByType = function(e) {
	Kv = e;
}, Wv.eachComponent = function(e) {
	e.mainType === "series" && e.subType && (Kv = e.subType);
};
function qv(e, t) {
	for (var n in t.prototype) e[n] = Ae;
}
//#endregion
//#region node_modules/echarts/lib/theme/dark.js
var $ = Q.darkColor, Jv = $.background, Yv = function() {
	return {
		axisLine: { lineStyle: { color: $.axisLine } },
		splitLine: { lineStyle: { color: $.axisSplitLine } },
		splitArea: { areaStyle: { color: [$.backgroundTint, $.backgroundTransparent] } },
		minorSplitLine: { lineStyle: { color: $.axisMinorSplitLine } },
		axisLabel: { color: $.axisLabel },
		axisName: {}
	};
}, Xv = {
	label: { color: $.secondary },
	itemStyle: { borderColor: $.borderTint },
	dividerLineStyle: { color: $.border }
}, Zv = {
	darkMode: !0,
	color: $.theme,
	backgroundColor: Jv,
	axisPointer: {
		lineStyle: { color: $.border },
		crossStyle: { color: $.borderShade },
		label: { color: $.tertiary }
	},
	legend: {
		textStyle: { color: $.secondary },
		pageTextStyle: { color: $.tertiary }
	},
	textStyle: { color: $.secondary },
	title: {
		textStyle: { color: $.primary },
		subtextStyle: { color: $.quaternary }
	},
	toolbox: {
		iconStyle: { borderColor: $.accent50 },
		feature: { dataView: {
			backgroundColor: Jv,
			textColor: $.primary,
			textareaColor: $.background,
			textareaBorderColor: $.border,
			buttonColor: $.accent50,
			buttonTextColor: $.neutral00
		} }
	},
	tooltip: {
		backgroundColor: $.neutral20,
		defaultBorderColor: $.border,
		textStyle: { color: $.tertiary }
	},
	dataZoom: {
		borderColor: $.accent10,
		textStyle: { color: $.tertiary },
		brushStyle: { color: $.backgroundTint },
		handleStyle: {
			color: $.neutral00,
			borderColor: $.accent20
		},
		moveHandleStyle: { color: $.accent40 },
		emphasis: { handleStyle: { borderColor: $.accent50 } },
		dataBackground: {
			lineStyle: { color: $.accent30 },
			areaStyle: { color: $.accent20 }
		},
		selectedDataBackground: {
			lineStyle: { color: $.accent50 },
			areaStyle: { color: $.accent30 }
		}
	},
	visualMap: {
		textStyle: { color: $.secondary },
		handleStyle: { borderColor: $.neutral30 }
	},
	timeline: {
		lineStyle: { color: $.accent10 },
		label: { color: $.tertiary },
		controlStyle: {
			color: $.accent30,
			borderColor: $.accent30
		}
	},
	calendar: {
		itemStyle: {
			color: $.neutral00,
			borderColor: $.neutral20
		},
		dayLabel: { color: $.tertiary },
		monthLabel: { color: $.secondary },
		yearLabel: { color: $.secondary }
	},
	matrix: {
		x: Xv,
		y: Xv,
		backgroundColor: { borderColor: $.axisLine },
		body: { itemStyle: { borderColor: $.borderTint } }
	},
	timeAxis: Yv(),
	logAxis: Yv(),
	valueAxis: Yv(),
	categoryAxis: Yv(),
	line: { symbol: "circle" },
	graph: { color: $.theme },
	gauge: {
		title: { color: $.secondary },
		axisLine: { lineStyle: { color: [[1, $.neutral05]] } },
		axisLabel: { color: $.axisLabel },
		detail: { color: $.primary }
	},
	candlestick: { itemStyle: {
		color: "#f64e56",
		color0: "#54ea92",
		borderColor: "#f64e56",
		borderColor0: "#54ea92"
	} },
	funnel: { itemStyle: { borderColor: $.background } },
	radar: function() {
		var e = Yv();
		return e.axisName = { color: $.axisLabel }, e.axisLine.lineStyle.color = $.neutral20, e;
	}(),
	treemap: { breadcrumb: {
		itemStyle: {
			color: $.neutral20,
			textStyle: { color: $.secondary }
		},
		emphasis: { itemStyle: { color: $.neutral30 } }
	} },
	sunburst: { itemStyle: { borderColor: $.background } },
	map: {
		itemStyle: {
			borderColor: $.border,
			areaColor: $.neutral10
		},
		label: { color: $.tertiary },
		emphasis: {
			label: { color: $.primary },
			itemStyle: { areaColor: $.highlight }
		},
		select: {
			label: { color: $.primary },
			itemStyle: { areaColor: $.highlight }
		}
	},
	geo: {
		itemStyle: {
			borderColor: $.border,
			areaColor: $.neutral10
		},
		emphasis: {
			label: { color: $.primary },
			itemStyle: { areaColor: $.highlight }
		},
		select: {
			label: { color: $.primary },
			itemStyle: { color: $.highlight }
		}
	}
};
Zv.categoryAxis.splitLine.show = !1;
//#endregion
//#region node_modules/echarts/lib/util/ECEventProcessor.js
var Qv = function() {
	function e() {}
	return e.prototype.normalizeQuery = function(e) {
		var t = {}, n = {}, r = {};
		if (H(e)) {
			var i = $o(e);
			t.mainType = i.main || null, t.subType = i.sub || null;
		} else {
			var a = [
				"Index",
				"Name",
				"Id"
			], o = {
				name: 1,
				dataIndex: 1,
				dataType: 1
			};
			F(e, function(e, i) {
				for (var s = !1, c = 0; c < a.length; c++) {
					var l = a[c], u = i.lastIndexOf(l);
					if (u > 0 && u === i.length - l.length) {
						var d = i.slice(0, u);
						d !== "data" && (t.mainType = d, t[l.toLowerCase()] = e, s = !0);
					}
				}
				o.hasOwnProperty(i) && (n[i] = e, s = !0), s || (r[i] = e);
			});
		}
		return {
			cptQuery: t,
			dataQuery: n,
			otherQuery: r
		};
	}, e.prototype.filter = function(e, t) {
		var n = this.eventInfo;
		if (!n) return !0;
		var r = n.targetEl, i = n.packedEvent, a = n.model, o = n.view;
		if (!a || !o) return !0;
		var s = t.cptQuery, c = t.dataQuery;
		return l(s, a, "mainType") && l(s, a, "subType") && l(s, a, "index", "componentIndex") && l(s, a, "name") && l(s, a, "id") && l(c, i, "name") && l(c, i, "dataIndex") && l(c, i, "dataType") && (!o.filterForExposedEvent || o.filterForExposedEvent(e, t.otherQuery, r, i));
		function l(e, t, n, r) {
			return e[n] == null || t[r || n] === e[n];
		}
	}, e.prototype.afterTrigger = function() {
		this.eventInfo = null;
	}, e;
}(), $v = [
	"symbol",
	"symbolSize",
	"symbolRotate",
	"symbolOffset"
], ey = $v.concat(["symbolKeepAspect"]), ty = {
	createOnAllSeries: !0,
	performRawSeries: !0,
	reset: function(e, t) {
		var n = e.getData();
		if (e.legendIcon && n.setVisual("legendIcon", e.legendIcon), !e.hasSymbolVisual) return;
		for (var r = {}, i = {}, a = !1, o = 0; o < $v.length; o++) {
			var s = $v[o], c = e.get(s);
			V(c) ? (a = !0, i[s] = c) : r[s] = c;
		}
		if (r.symbol = r.symbol || e.defaultSymbol, n.setVisual(j({
			legendIcon: e.legendIcon || r.symbol,
			symbolKeepAspect: e.get("symbolKeepAspect")
		}, r)), t.isSeriesFiltered(e)) return;
		var l = L(i);
		function u(t, n) {
			for (var r = e.getRawValue(n), a = e.getDataParams(n), o = 0; o < l.length; o++) {
				var s = l[o];
				t.setItemVisual(n, s, i[s](r, a));
			}
		}
		return { dataEach: a ? u : null };
	}
}, ny = {
	createOnAllSeries: !0,
	performRawSeries: !0,
	reset: function(e, t) {
		if (!e.hasSymbolVisual || t.isSeriesFiltered(e)) return;
		var n = e.getData();
		function r(e, t) {
			for (var n = e.getItemModel(t), r = 0; r < ey.length; r++) {
				var i = ey[r], a = n.getShallow(i, !0);
				a != null && e.setItemVisual(t, i, a);
			}
		}
		return { dataEach: n.hasItemOption ? r : null };
	}
};
//#endregion
//#region node_modules/echarts/lib/visual/helper.js
function ry(e, t, n) {
	switch (n) {
		case "color": return e.getItemVisual(t, "style")[e.getVisual("drawType")];
		case "opacity": return e.getItemVisual(t, "style").opacity;
		case "symbol":
		case "symbolSize":
		case "liftZ": return e.getItemVisual(t, n);
	}
}
function iy(e, t) {
	switch (t) {
		case "color": return e.getVisual("style")[e.getVisual("drawType")];
		case "opacity": return e.getVisual("style").opacity;
		case "symbol":
		case "symbolSize":
		case "liftZ": return e.getVisual(t);
	}
}
//#endregion
//#region node_modules/echarts/lib/legacy/dataSelectAction.js
function ay(e, t, n, r, i) {
	var a = e + t;
	n.isSilent(a) || r.eachComponent({
		mainType: "series",
		subType: "pie"
	}, function(e) {
		for (var t = e.seriesIndex, r = e.option.selectedMap, o = i.selected, s = 0; s < o.length; s++) if (o[s].seriesIndex === t) {
			var c = e.getData(), l = No(c, i.fromActionPayload);
			n.trigger(a, {
				type: a,
				seriesId: e.id,
				name: B(l) ? c.getName(l[0]) : c.getName(l),
				selected: H(r) ? r : j({}, r)
			});
		}
	});
}
function oy(e, t, n) {
	e.on("selectchanged", function(e) {
		var r = n.getModel();
		e.isFromClick ? (ay("map", "selectchanged", t, r, e), ay("pie", "selectchanged", t, r, e)) : e.fromAction === "select" ? (ay("map", "selected", t, r, e), ay("pie", "selected", t, r, e)) : e.fromAction === "unselect" && (ay("map", "unselected", t, r, e), ay("pie", "unselected", t, r, e));
	});
}
//#endregion
//#region node_modules/echarts/lib/util/event.js
function sy(e, t, n) {
	for (var r; e && !(t(e) && (r = e, n));) e = e.__hostTarget || e.parent;
	return r;
}
//#endregion
//#region node_modules/echarts/lib/core/lifecycle.js
var cy = new Qe(), ly = {};
function uy(e, t) {
	ly[e] = t;
}
function dy(e) {
	return ly[e];
}
//#endregion
//#region node_modules/echarts/lib/chart/custom/customSeriesRegister.js
var fy = {};
function py(e, t) {
	fy[e] = t;
}
//#endregion
//#region node_modules/echarts/lib/util/cycleCache.js
var my = Y();
function hy(e) {
	my(e).prepare = {};
}
function gy(e) {
	my(e).fullUpdate = {};
}
function _y(e) {
	return my(e).fullUpdate;
}
//#endregion
//#region node_modules/zrender/lib/core/WeakMap.js
var vy = Math.round(Math.random() * 9), yy = typeof Object.defineProperty == "function", by = function() {
	function e() {
		this._id = "__ec_inner_" + vy++;
	}
	return e.prototype.get = function(e) {
		return this._guard(e)[this._id];
	}, e.prototype.set = function(e, t) {
		var n = this._guard(e);
		return yy ? Object.defineProperty(n, this._id, {
			value: t,
			enumerable: !1,
			configurable: !0
		}) : n[this._id] = t, this;
	}, e.prototype.delete = function(e) {
		return this.has(e) ? (delete this._guard(e)[this._id], !0) : !1;
	}, e.prototype.has = function(e) {
		return !!this._guard(e)[this._id];
	}, e.prototype._guard = function(e) {
		if (e !== Object(e)) throw TypeError("Value of WeakMap is not a non-null object.");
		return e;
	}, e;
}(), xy = Z.extend({
	type: "triangle",
	shape: {
		cx: 0,
		cy: 0,
		width: 0,
		height: 0
	},
	buildPath: function(e, t) {
		var n = t.cx, r = t.cy, i = t.width / 2, a = t.height / 2;
		e.moveTo(n, r - a), e.lineTo(n + i, r + a), e.lineTo(n - i, r + a), e.closePath();
	}
}), Sy = {
	line: qd,
	rect: dl,
	roundRect: dl,
	square: dl,
	circle: _d,
	diamond: Z.extend({
		type: "diamond",
		shape: {
			cx: 0,
			cy: 0,
			width: 0,
			height: 0
		},
		buildPath: function(e, t) {
			var n = t.cx, r = t.cy, i = t.width / 2, a = t.height / 2;
			e.moveTo(n, r - a), e.lineTo(n + i, r), e.lineTo(n, r + a), e.lineTo(n - i, r), e.closePath();
		}
	}),
	pin: Z.extend({
		type: "pin",
		shape: {
			x: 0,
			y: 0,
			width: 0,
			height: 0
		},
		buildPath: function(e, t) {
			var n = t.x, r = t.y, i = t.width / 5 * 3, a = Math.max(i, t.height), o = i / 2, s = o * o / (a - o), c = r - a + o + s, l = Math.asin(s / o), u = Math.cos(l) * o, d = Math.sin(l), f = Math.cos(l), p = o * .6, m = o * .7;
			e.moveTo(n - u, c + s), e.arc(n, c, o, Math.PI - l, Math.PI * 2 + l), e.bezierCurveTo(n + u - d * p, c + s + f * p, n, r - m, n, r), e.bezierCurveTo(n, r - m, n - u + d * p, c + s + f * p, n - u, c + s), e.closePath();
		}
	}),
	arrow: Z.extend({
		type: "arrow",
		shape: {
			x: 0,
			y: 0,
			width: 0,
			height: 0
		},
		buildPath: function(e, t) {
			var n = t.height, r = t.width, i = t.x, a = t.y, o = r / 3 * 2;
			e.moveTo(i, a), e.lineTo(i + o, a + n), e.lineTo(i, a + n / 4 * 3), e.lineTo(i - o, a + n), e.lineTo(i, a), e.closePath();
		}
	}),
	triangle: xy
}, Cy = {
	line: function(e, t, n, r, i) {
		i.x1 = e, i.y1 = t + r / 2, i.x2 = e + n, i.y2 = t + r / 2;
	},
	rect: function(e, t, n, r, i) {
		i.x = e, i.y = t, i.width = n, i.height = r;
	},
	roundRect: function(e, t, n, r, i) {
		i.x = e, i.y = t, i.width = n, i.height = r, i.r = Math.min(n, r) / 4;
	},
	square: function(e, t, n, r, i) {
		var a = Math.min(n, r);
		i.x = e, i.y = t, i.width = a, i.height = a;
	},
	circle: function(e, t, n, r, i) {
		i.cx = e + n / 2, i.cy = t + r / 2, i.r = Math.min(n, r) / 2;
	},
	diamond: function(e, t, n, r, i) {
		i.cx = e + n / 2, i.cy = t + r / 2, i.width = n, i.height = r;
	},
	pin: function(e, t, n, r, i) {
		i.x = e + n / 2, i.y = t + r / 2, i.width = n, i.height = r;
	},
	arrow: function(e, t, n, r, i) {
		i.x = e + n / 2, i.y = t + r / 2, i.width = n, i.height = r;
	},
	triangle: function(e, t, n, r, i) {
		i.cx = e + n / 2, i.cy = t + r / 2, i.width = n, i.height = r;
	}
}, wy = {};
F(Sy, function(e, t) {
	wy[t] = new e();
});
var Ty = Z.extend({
	type: "symbol",
	shape: {
		symbolType: "",
		x: 0,
		y: 0,
		width: 0,
		height: 0
	},
	calculateTextPosition: function(e, t, n) {
		var r = na(e, t, n), i = this.shape;
		return i && i.symbolType === "pin" && t.position === "inside" && (r.y = n.y + n.height * .4), r;
	},
	buildPath: function(e, t, n) {
		var r = t.symbolType;
		if (r !== "none") {
			var i = wy[r];
			i ||= (r = "rect", wy[r]), Cy[r](t.x, t.y, t.width, t.height, i.shape), i.buildPath(e, i.shape, n);
		}
	}
});
function Ey(e, t) {
	if (this.type !== "image") {
		var n = this.style;
		this.__isEmptyBrush ? (n.stroke = e, n.fill = t || Q.color.neutral00, n.lineWidth = 2) : this.shape.symbolType === "line" ? n.stroke = e : n.fill = e, this.markRedraw();
	}
}
function Dy(e, t, n, r, i, a, o) {
	var s = e.indexOf("empty") === 0;
	s && (e = e.substr(5, 1).toLowerCase() + e.substr(6));
	var c = e.indexOf("image://") === 0 ? If(e.slice(8), new J(t, n, r, i), o ? "center" : "cover") : e.indexOf("path://") === 0 ? Ff(e.slice(7), {}, new J(t, n, r, i), o ? "center" : "cover") : new Ty({ shape: {
		symbolType: e,
		x: t,
		y: n,
		width: r,
		height: i
	} });
	return c.__isEmptyBrush = s, c.setColor = Ey, a && c.setColor(a), c;
}
function Oy(e) {
	return B(e) || (e = [+e, +e]), [e[0] || 0, e[1] || 0];
}
function ky(e, t) {
	if (e != null) return B(e) || (e = [e, e]), [za(e[0], t[0]) || 0, za(G(e[1], e[0]), t[1]) || 0];
}
//#endregion
//#region node_modules/zrender/lib/canvas/helper.js
function Ay(e) {
	return isFinite(e);
}
function jy(e, t, n) {
	var r = t.x == null ? 0 : t.x, i = t.x2 == null ? 1 : t.x2, a = t.y == null ? 0 : t.y, o = t.y2 == null ? 0 : t.y2;
	return t.global || (r = r * n.width + n.x, i = i * n.width + n.x, a = a * n.height + n.y, o = o * n.height + n.y), r = Ay(r) ? r : 0, i = Ay(i) ? i : 1, a = Ay(a) ? a : 0, o = Ay(o) ? o : 0, e.createLinearGradient(r, a, i, o);
}
function My(e, t, n) {
	var r = n.width, i = n.height, a = Math.min(r, i), o = t.x == null ? .5 : t.x, s = t.y == null ? .5 : t.y, c = t.r == null ? .5 : t.r;
	return t.global || (o = o * r + n.x, s = s * i + n.y, c *= a), o = Ay(o) ? o : .5, s = Ay(s) ? s : .5, c = c >= 0 && Ay(c) ? c : .5, e.createRadialGradient(o, s, 0, o, s, c);
}
function Ny(e, t, n) {
	for (var r = t.type === "radial" ? My(e, t, n) : jy(e, t, n), i = t.colorStops, a = 0; a < i.length; a++) r.addColorStop(i[a].offset, i[a].color);
	return r;
}
function Py(e, t) {
	if (e === t || !e && !t) return !1;
	if (!e || !t || e.length !== t.length) return !0;
	for (var n = 0; n < e.length; n++) if (e[n] !== t[n]) return !0;
	return !1;
}
function Fy(e) {
	return parseInt(e, 10);
}
function Iy(e, t, n) {
	var r = ["width", "height"][t], i = ["clientWidth", "clientHeight"][t], a = ["paddingLeft", "paddingTop"][t], o = ["paddingRight", "paddingBottom"][t];
	if (n[r] != null && n[r] !== "auto") return parseFloat(n[r]);
	var s = document.defaultView.getComputedStyle(e);
	return (e[i] || Fy(s[r]) || Fy(e.style[r])) - (Fy(s[a]) || 0) - (Fy(s[o]) || 0) || 0;
}
//#endregion
//#region node_modules/zrender/lib/canvas/dashStyle.js
function Ly(e, t) {
	return !e || e === "solid" || !(t > 0) ? null : e === "dashed" ? [4 * t, 2 * t] : e === "dotted" ? [t] : U(e) ? [e] : B(e) ? e : null;
}
function Ry(e) {
	var t = e.style, n = t.lineDash && t.lineWidth > 0 && Ly(t.lineDash, t.lineWidth), r = t.lineDashOffset;
	if (n) {
		var i = t.strokeNoScale && e.getLineScale ? e.getLineScale() : 1;
		i && i !== 1 && (n = I(n, function(e) {
			return e / i;
		}), r /= i);
	}
	return [n, r];
}
//#endregion
//#region node_modules/zrender/lib/canvas/graphic.js
var zy = new Dc(!0);
function By(e) {
	var t = e.stroke;
	return !(t == null || t === "none" || !(e.lineWidth > 0));
}
function Vy(e) {
	return typeof e == "string" && e !== "none";
}
function Hy(e) {
	var t = e.fill;
	return t != null && t !== "none";
}
function Uy(e, t) {
	if (t.fillOpacity != null && t.fillOpacity !== 1) {
		var n = e.globalAlpha;
		e.globalAlpha = t.fillOpacity * t.opacity, e.fill(), e.globalAlpha = n;
	} else e.fill();
}
function Wy(e, t) {
	if (t.strokeOpacity != null && t.strokeOpacity !== 1) {
		var n = e.globalAlpha;
		e.globalAlpha = t.strokeOpacity * t.opacity, e.stroke(), e.globalAlpha = n;
	} else e.stroke();
}
function Gy(e, t, n) {
	var r = hs(t.image, t.__image, n);
	if (_s(r)) {
		var i = e.createPattern(r, t.repeat || "repeat");
		if (typeof DOMMatrix == "function" && i && i.setTransform) {
			var a = new DOMMatrix();
			a.translateSelf(t.x || 0, t.y || 0), a.rotateSelf(0, 0, (t.rotation || 0) * je), a.scaleSelf(t.scaleX || 1, t.scaleY || 1), i.setTransform(a);
		}
		return i;
	}
}
function Ky(e, t, n, r, i) {
	var a, o = By(n), s = Hy(n), c = n.strokePercent, l = c < 1, u = !t.path;
	(!t.silent || l) && u && t.createPathProxy();
	var d = t.path || zy, f = t.__dirty;
	if (!r) {
		var p = n.fill, m = n.stroke, h = s && !!p.colorStops, g = o && !!m.colorStops, _ = s && !!p.image, v = o && !!m.image, y = void 0, b = void 0, x = void 0, S = void 0, C = void 0;
		(h || g) && (C = t.getBoundingRect()), h && (y = f ? Ny(e, p, C) : t.__canvasFillGradient, t.__canvasFillGradient = y), g && (b = f ? Ny(e, m, C) : t.__canvasStrokeGradient, t.__canvasStrokeGradient = b), _ && (x = f || !t.__canvasFillPattern ? Gy(e, p, t) : t.__canvasFillPattern, t.__canvasFillPattern = x), v && (S = f || !t.__canvasStrokePattern ? Gy(e, m, t) : t.__canvasStrokePattern, t.__canvasStrokePattern = S), h ? e.fillStyle = y : _ && (x ? e.fillStyle = x : s = !1), g ? e.strokeStyle = b : v && (S ? e.strokeStyle = S : o = !1);
	}
	var w = t.getGlobalScale();
	d.setScale(w[0], w[1], t.segmentIgnoreThreshold);
	var T, E;
	e.setLineDash && n.lineDash && (a = Ry(t), T = a[0], E = a[1]);
	var D = !0;
	(u || f & 4) && (d.setDPR(e.dpr), l ? d.setContext(null) : (d.setContext(e), D = !1), d.reset(), t.buildPath(d, t.shape, r), d.toStatic(), t.pathUpdated()), D && d.rebuildPath(e, l ? c : 1), T && (e.setLineDash(T), e.lineDashOffset = E), r ? (i.batchFill = s, i.batchStroke = o) : n.strokeFirst ? (o && Wy(e, n), s && Uy(e, n)) : (s && Uy(e, n), o && Wy(e, n)), T && e.setLineDash([]);
}
function qy(e, t, n) {
	var r = t.__image = hs(n.image, t.__image, t, t.onload);
	if (r && _s(r)) {
		var i = n.x || 0, a = n.y || 0, o = t.getWidth(), s = t.getHeight(), c = r.width / r.height;
		if (o == null && s != null ? o = s * c : s == null && o != null ? s = o / c : o == null && s == null && (o = r.width, s = r.height), n.sWidth && n.sHeight) {
			var l = n.sx || 0, u = n.sy || 0;
			e.drawImage(r, l, u, n.sWidth, n.sHeight, i, a, o, s);
		} else if (n.sx && n.sy) {
			var l = n.sx, u = n.sy, d = o - l, f = s - u;
			e.drawImage(r, l, u, d, f, i, a, o, s);
		} else e.drawImage(r, i, a, o, s);
	}
}
function Jy(e, t, n) {
	var r, i = n.text;
	if (i != null && (i += ""), i) {
		e.font = n.font || "12px sans-serif", e.textAlign = n.textAlign, e.textBaseline = n.textBaseline;
		var a = void 0, o = void 0;
		e.setLineDash && n.lineDash && (r = Ry(t), a = r[0], o = r[1]), a && (e.setLineDash(a), e.lineDashOffset = o), n.strokeFirst ? (By(n) && e.strokeText(i, n.x, n.y), Hy(n) && e.fillText(i, n.x, n.y)) : (Hy(n) && e.fillText(i, n.x, n.y), By(n) && e.strokeText(i, n.x, n.y)), a && e.setLineDash([]);
	}
}
var Yy = [
	"shadowBlur",
	"shadowOffsetX",
	"shadowOffsetY"
], Xy = [
	["lineCap", "butt"],
	["lineJoin", "miter"],
	["miterLimit", 10]
];
function Zy(e, t, n, r, i) {
	var a = !1;
	if (!r && (n ||= {}, t === n)) return !1;
	if (r || t.opacity !== n.opacity) {
		cb(e, i), a = !0;
		var o = Math.max(Math.min(t.opacity, 1), 0);
		e.globalAlpha = isNaN(o) ? Vs.opacity : o;
	}
	(r || t.blend !== n.blend) && (a ||= (cb(e, i), !0), e.globalCompositeOperation = t.blend || Vs.blend);
	for (var s = 0; s < Yy.length; s++) {
		var c = Yy[s];
		(r || t[c] !== n[c]) && (a ||= (cb(e, i), !0), e[c] = e.dpr * (t[c] || 0));
	}
	return (r || t.shadowColor !== n.shadowColor) && (a ||= (cb(e, i), !0), e.shadowColor = t.shadowColor || Vs.shadowColor), a;
}
function Qy(e, t, n, r, i) {
	var a = t.style, o = r ? null : n && n.style || {};
	if (a === o) return !1;
	var s = Zy(e, a, o, r, i);
	if ((r || a.fill !== o.fill) && (s ||= (cb(e, i), !0), Vy(a.fill) && (e.fillStyle = a.fill)), (r || a.stroke !== o.stroke) && (s ||= (cb(e, i), !0), Vy(a.stroke) && (e.strokeStyle = a.stroke)), (r || a.opacity !== o.opacity) && (s ||= (cb(e, i), !0), e.globalAlpha = a.opacity == null ? 1 : a.opacity), t.hasStroke()) {
		var c = a.lineWidth / (a.strokeNoScale && t.getLineScale ? t.getLineScale() : 1);
		e.lineWidth !== c && (s ||= (cb(e, i), !0), e.lineWidth = c);
	}
	for (var l = 0; l < Xy.length; l++) {
		var u = Xy[l], d = u[0];
		(r || a[d] !== o[d]) && (s ||= (cb(e, i), !0), e[d] = a[d] || u[1]);
	}
	return s;
}
function $y(e, t, n, r, i) {
	return Zy(e, t.style, n && n.style, r, i);
}
function eb(e, t) {
	var n = t.transform, r = e.dpr || 1;
	n ? e.setTransform(r * n[0], r * n[1], r * n[2], r * n[3], r * n[4], r * n[5]) : e.setTransform(r, 0, 0, r, 0, 0);
}
function tb(e, t, n) {
	for (var r = !1, i = 0; i < e.length; i++) {
		var a = e[i];
		r ||= a.isZeroArea(), eb(t, a), t.beginPath(), a.buildPath(t, a.shape), t.clip();
	}
	n.allClipped = r;
}
function nb(e, t) {
	return e && t ? e[0] !== t[0] || e[1] !== t[1] || e[2] !== t[2] || e[3] !== t[3] || e[4] !== t[4] || e[5] !== t[5] : !(!e && !t);
}
var rb = 1, ib = 2, ab = 3, ob = 4;
function sb(e) {
	var t = Hy(e), n = By(e);
	return !(e.lineDash || !(+t ^ n) || t && typeof e.fill != "string" || n && typeof e.stroke != "string" || e.strokePercent < 1 || e.strokeOpacity < 1 || e.fillOpacity < 1);
}
function cb(e, t) {
	t.batchFill && (t.batchFill = !1, e.fill()), t.batchStroke && (t.batchStroke = !1, e.stroke());
}
function lb(e, t) {
	var n = {
		inHover: !1,
		viewWidth: 0,
		viewHeight: 0,
		beforeBrushParam: {}
	};
	ub(e, t, n), db(e, n);
}
function ub(e, t, n) {
	var r = t.transform;
	if (!t.shouldBePainted(n.viewWidth, n.viewHeight, !1, !1)) t.__dirty &= -2, t.__isRendered = !1;
	else {
		var i = t.__clipPaths, a = n.prevElClipPaths, o = t.style, s = !1, c = !1;
		if ((!a || Py(i, a)) && (a && (cb(e, n), e.restore(), c = s = !0, n.prevElClipPaths = null, n.allClipped = !1, n.prevEl = null), i && i.length && (cb(e, n), e.save(), tb(i, e, n), s = !0, n.prevElClipPaths = i)), n.allClipped) t.__dirty &= -2, t.__isRendered = !1;
		else {
			t.beforeBrush && t.beforeBrush(n.beforeBrushParam), t.innerBeforeBrush();
			var l = n.prevEl;
			l || (c = s = !0);
			var u = t instanceof Z && t.autoBatch && sb(o);
			s || nb(r, l.transform) ? (cb(e, n), eb(e, t)) : u || cb(e, n), t instanceof Z ? (n.lastDrawType !== rb && (c = !0, n.lastDrawType = rb), Qy(e, t, l, c, n), (!u || !n.batchFill && !n.batchStroke) && e.beginPath(), Ky(e, t, o, u, n)) : t instanceof $c ? (n.lastDrawType !== ab && (c = !0, n.lastDrawType = ab), Qy(e, t, l, c, n), Jy(e, t, o)) : t instanceof rl ? (n.lastDrawType !== ib && (c = !0, n.lastDrawType = ib), $y(e, t, l, c, n), qy(e, t, o)) : t.getTemporalDisplayables && (n.lastDrawType !== ob && (c = !0, n.lastDrawType = ob), fb(e, t, n)), t.innerAfterBrush(), t.afterBrush && (u && cb(e, n), t.afterBrush()), n.prevEl = t, t.__dirty = 0, t.__isRendered = !0;
		}
	}
}
function db(e, t) {
	cb(e, t), t.prevElClipPaths && e.restore();
}
function fb(e, t, n) {
	var r = t.getDisplayables(), i = t.getTemporalDisplayables();
	e.save();
	for (var a = {
		prevElClipPaths: null,
		prevEl: null,
		allClipped: !1,
		viewWidth: n.viewWidth,
		viewHeight: n.viewHeight,
		inHover: n.inHover,
		beforeBrushParam: {}
	}, o = t.getCursor(), s = r.length; o < s; o++) {
		var c = r[o];
		c.beforeBrush && c.beforeBrush(n.beforeBrushParam), c.innerBeforeBrush(), ub(e, c, a), c.innerAfterBrush(), c.afterBrush && c.afterBrush(), a.prevEl = c;
	}
	db(e, a);
	for (var l = 0, u = i.length; l < u; l++) {
		var c = i[l];
		c.beforeBrush && c.beforeBrush(n.beforeBrushParam), c.innerBeforeBrush(), ub(e, c, a), c.innerAfterBrush(), c.afterBrush && c.afterBrush(), a.prevEl = c;
	}
	db(e, a), t.clearTemporalDisplayables(), t.notClear = !0, e.restore();
}
//#endregion
//#region node_modules/echarts/lib/util/decal.js
var pb = new by(), mb = new dr(100), hb = [
	"symbol",
	"symbolSize",
	"symbolKeepAspect",
	"color",
	"backgroundColor",
	"dashArrayX",
	"dashArrayY",
	"maxTileWidth",
	"maxTileHeight"
];
function gb(e, t) {
	if (e === "none") return null;
	var n = t.getDevicePixelRatio(), r = t.getZr(), i = r.painter.type === "svg";
	e.dirty && pb.delete(e);
	var a = pb.get(e);
	if (a) return a;
	var o = M(e, {
		symbol: "rect",
		symbolSize: 1,
		symbolKeepAspect: !0,
		color: "rgba(0, 0, 0, 0.2)",
		backgroundColor: null,
		dashArrayX: 5,
		dashArrayY: 5,
		rotation: 0,
		maxTileWidth: 512,
		maxTileHeight: 512
	});
	o.backgroundColor === "none" && (o.backgroundColor = null);
	var s = { repeat: "repeat" };
	return c(s), s.rotation = o.rotation, s.scaleX = s.scaleY = i ? 1 : 1 / n, pb.set(e, s), e.dirty = !1, s;
	function c(e) {
		for (var t = [n], a = !0, s = 0; s < hb.length; ++s) {
			var c = o[hb[s]];
			if (c != null && !B(c) && !H(c) && !U(c) && typeof c != "boolean") {
				a = !1;
				break;
			}
			t.push(c);
		}
		var l;
		if (a) {
			l = t.join(",") + (i ? "-svg" : "");
			var u = mb.get(l);
			u && (i ? e.svgElement = u : e.image = u);
		}
		var d = vb(o.dashArrayX), f = yb(o.dashArrayY), m = _b(o.symbol), h = bb(d), g = xb(f), _ = !i && p.createCanvas(), v = i && {
			tag: "g",
			attrs: {},
			key: "dcl",
			children: []
		}, y = x(), b;
		_ && (_.width = y.width * n, _.height = y.height * n, b = _.getContext("2d")), S(), a && mb.put(l, _ || v), e.image = _, e.svgElement = v, e.svgWidth = y.width, e.svgHeight = y.height;
		function x() {
			for (var e = 1, t = 0, n = h.length; t < n; ++t) e = ro(e, h[t]);
			for (var r = 1, t = 0, n = m.length; t < n; ++t) r = ro(r, m[t].length);
			e *= r;
			var i = g * h.length * m.length;
			return {
				width: Math.max(1, Math.min(e, o.maxTileWidth)),
				height: Math.max(1, Math.min(i, o.maxTileHeight))
			};
		}
		function S() {
			b && (b.clearRect(0, 0, _.width, _.height), o.backgroundColor && (b.fillStyle = o.backgroundColor, b.fillRect(0, 0, _.width, _.height)));
			for (var e = 0, t = 0; t < f.length; ++t) e += f[t];
			if (e <= 0) return;
			for (var a = -g, s = 0, c = 0, l = 0; a < y.height;) {
				if (s % 2 == 0) {
					for (var u = c / 2 % m.length, p = 0, h = 0, x = 0; p < y.width * 2;) {
						for (var S = 0, t = 0; t < d[l].length; ++t) S += d[l][t];
						if (S <= 0) break;
						if (h % 2 == 0) {
							var C = (1 - o.symbolSize) * .5, w = p + d[l][h] * C, T = a + f[s] * C, E = d[l][h] * o.symbolSize, D = f[s] * o.symbolSize, O = x / 2 % m[u].length;
							k(w, T, E, D, m[u][O]);
						}
						p += d[l][h], ++x, ++h, h === d[l].length && (h = 0);
					}
					++l, l === d.length && (l = 0);
				}
				a += f[s], ++c, ++s, s === f.length && (s = 0);
			}
			function k(e, t, a, s, c) {
				var l = i ? 1 : n, u = Dy(c, e * l, t * l, a * l, s * l, o.color, o.symbolKeepAspect);
				if (i) {
					var d = r.painter.renderOneToVNode(u);
					d && v.children.push(d);
				} else lb(b, u);
			}
		}
	}
}
function _b(e) {
	if (!e || e.length === 0) return [["rect"]];
	if (H(e)) return [[e]];
	for (var t = !0, n = 0; n < e.length; ++n) if (!H(e[n])) {
		t = !1;
		break;
	}
	if (t) return _b([e]);
	for (var r = [], n = 0; n < e.length; ++n) H(e[n]) ? r.push([e[n]]) : r.push(e[n]);
	return r;
}
function vb(e) {
	if (!e || e.length === 0) return [[0, 0]];
	if (U(e)) {
		var t = Math.ceil(e);
		return [[t, t]];
	}
	for (var n = !0, r = 0; r < e.length; ++r) if (!U(e[r])) {
		n = !1;
		break;
	}
	if (n) return vb([e]);
	for (var i = [], r = 0; r < e.length; ++r) if (U(e[r])) {
		var t = Math.ceil(e[r]);
		i.push([t, t]);
	} else {
		var t = I(e[r], function(e) {
			return Math.ceil(e);
		});
		t.length % 2 == 1 ? i.push(t.concat(t)) : i.push(t);
	}
	return i;
}
function yb(e) {
	if (!e || typeof e == "object" && e.length === 0) return [0, 0];
	if (U(e)) {
		var t = Math.ceil(e);
		return [t, t];
	}
	var n = I(e, function(e) {
		return Math.ceil(e);
	});
	return e.length % 2 ? n.concat(n) : n;
}
function bb(e) {
	return I(e, function(e) {
		return xb(e);
	});
}
function xb(e) {
	for (var t = 0, n = 0; n < e.length; ++n) t += e[n];
	return e.length % 2 == 1 ? t * 2 : t;
}
//#endregion
//#region node_modules/echarts/lib/visual/decal.js
var Sb = Yo(Cb);
function Cb(e, t) {
	e.eachRawSeries(function(n) {
		if (!e.isSeriesFiltered(n)) {
			var r = n.getData();
			r.hasItemVisual() && r.each(function(e) {
				var n = r.getItemVisual(e, "decal");
				if (n) {
					var i = r.ensureUniqueItemVisual(e, "style");
					i.decal = gb(n, t);
				}
			});
			var i = r.getVisual("decal");
			if (i) {
				var a = r.getVisual("style");
				a.decal = gb(i, t);
			}
		}
	});
}
//#endregion
//#region node_modules/echarts/lib/core/echarts.js
var wb = 1, Tb = 800, Eb = 900, Db = 920, Ob = 1e3, kb = 2e3, Ab = 5e3, jb = 1e3, Mb = 1100, Nb = 2e3, Pb = 3e3, Fb = 4e3, Ib = 4500, Lb = 4600, Rb = 5e3, zb = 6e3, Bb = 7e3, Vb = {
	PROCESSOR: {
		SERIES_FILTER: Tb,
		AXIS_STATISTICS: Db,
		FILTER: Ob,
		STATISTIC: Ab,
		STATISTICS: Ab
	},
	VISUAL: {
		LAYOUT: jb,
		PROGRESSIVE_LAYOUT: Mb,
		GLOBAL: Nb,
		CHART: Pb,
		POST_CHART_LAYOUT: Lb,
		COMPONENT: Fb,
		BRUSH: Rb,
		CHART_ITEM: Ib,
		ARIA: zb,
		DECAL: Bb
	}
}, Hb = "__flagInMainProcess", Ub = "__mainProcessVersion", Wb = "__pendingUpdate", Gb = "__needsUpdateStatus", Kb = /^[a-zA-Z0-9_]+$/, qb = "__connectUpdateStatus", Jb = 0, Yb = 1, Xb = 2;
function Zb(e) {
	return function() {
		var t = [...arguments];
		if (this.isDisposed()) this.id;
		else return $b(this, e, t);
	};
}
function Qb(e) {
	return function() {
		var t = [...arguments];
		return $b(this, e, t);
	};
}
function $b(e, t, n) {
	return n[0] = n[0] && n[0].toLowerCase(), Qe.prototype[t].apply(e, n);
}
var ex = function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t;
}(Qe), tx = ex.prototype;
tx.on = Qb("on"), tx.off = Qb("off");
var nx, rx, ix, ax, ox, sx, cx, lx, ux, dx, fx, px, mx, hx, gx, _x, vx, yx, bx, xx = function(e) {
	r(t, e);
	function t(t, n, r) {
		var i = e.call(this, new Qv()) || this;
		i._chartsViews = [], i._chartsMap = {}, i._componentsViews = [], i._componentsMap = {}, i._pendingActions = [], r ||= {}, i.__v_skip = !0, i._dom = t;
		var a = "canvas", o = "auto", s = !1;
		i[Ub] = 1, r.ssr;
		var c = i._zr = Ea(t, {
			renderer: r.renderer || a,
			devicePixelRatio: r.devicePixelRatio,
			width: r.width,
			height: r.height,
			ssr: r.ssr,
			useDirtyRect: G(r.useDirtyRect, s),
			useCoarsePointer: G(r.useCoarsePointer, o),
			pointerSize: r.pointerSize
		});
		i._ssr = r.ssr, i._throttledZrFlush = yv(R(c.flush, c), 17), i._updateTheme(n), i._locale = om(r.locale || im), i._coordSysMgr = new Pm();
		var l = i._api = gx(i);
		function u(e, t) {
			return e.__prio - t.__prio;
		}
		return On(Ax, u), On(Ox, u), i._scheduler = new Nv(i, l, Ox, Ax), i._messageCenter = new ex(), i._initEvents(), i.resize = R(i.resize, i), c.animation.on("frame", i._onframe, i), dx(c, i), fx(c, i), be(i), i;
	}
	return t.prototype._onframe = function() {
		if (!this._disposed) {
			var e = this._scheduler, t = this._model, n = this._api;
			if (yx(this), this[Wb]) {
				var r = this[Wb].silent;
				this[Hb] = !0, bx(this);
				try {
					nx(this), ax.update.call(this, null, this[Wb].updateParams);
				} catch (e) {
					throw this[Hb] = !1, this[Wb] = null, e;
				}
				this._zr.flush(), this[Hb] = !1, this[Wb] = null, lx.call(this, r), ux.call(this, r);
			} else if (e.unfinished) {
				var i = wb;
				do {
					e.unfinished = !1;
					var a = p.getTime();
					e.performSeriesTasks(t), e.performDataProcessorTasks(t), sx(this, t), e.performVisualTasks(t), hx(this, this._model, n, "remain", {}), i -= p.getTime() - a;
				} while (i > 0 && e.unfinished);
				e.unfinished || this._zr.flush();
			}
		}
	}, t.prototype.getDom = function() {
		return this._dom;
	}, t.prototype.getId = function() {
		return this.id;
	}, t.prototype.getZr = function() {
		return this._zr;
	}, t.prototype.isSSR = function() {
		return this._ssr;
	}, t.prototype.setOption = function(e, t, n) {
		if (!this[Hb]) {
			if (this._disposed) this.id;
			else {
				var r, i, a;
				if (W(t) && (n = t.lazyUpdate, r = t.silent, i = t.replaceMerge, a = t.transition, t = t.notMerge), this[Hb] = !0, bx(this), !this._model || t) {
					var o = new Lh(this._api), s = this._theme, c = this._model = new Ah();
					c.scheduler = this._scheduler, c.ssr = this._ssr, c.init(null, null, null, s, this._locale, o);
				}
				this._model.setOption(e, { replaceMerge: i }, kx);
				var l = {
					seriesTransition: a,
					optionChanged: !0
				};
				if (n) this[Wb] = {
					silent: r,
					updateParams: l
				}, this[Hb] = !1, this.getZr().wakeUp();
				else {
					try {
						nx(this), ax.update.call(this, null, l);
					} catch (e) {
						throw this[Wb] = null, this[Hb] = !1, e;
					}
					this._ssr || this._zr.flush(), this[Wb] = null, this[Hb] = !1, lx.call(this, r), ux.call(this, r);
				}
			}
		}
	}, t.prototype.setTheme = function(e, t) {
		if (!this[Hb]) {
			if (this._disposed) this.id;
			else {
				var n = this._model;
				if (n) {
					var r = t && t.silent, i = null;
					this[Wb] && (r ??= this[Wb].silent, i = this[Wb].updateParams, this[Wb] = null), this[Hb] = !0, bx(this);
					try {
						this._updateTheme(e), n.setTheme(this._theme), nx(this), ax.update.call(this, { type: "setTheme" }, i);
					} catch (e) {
						throw this[Hb] = !1, e;
					}
					this[Hb] = !1, lx.call(this, r), ux.call(this, r);
				}
			}
		}
	}, t.prototype._updateTheme = function(e) {
		H(e) && (e = jx[e]), e && (e = k(e), e && dg(e, !0), this._theme = e);
	}, t.prototype.getModel = function() {
		return this._model;
	}, t.prototype.getOption = function() {
		return this._model && this._model.getOption();
	}, t.prototype.getWidth = function() {
		return this._zr.getWidth();
	}, t.prototype.getHeight = function() {
		return this._zr.getHeight();
	}, t.prototype.getDevicePixelRatio = function() {
		return this._zr.painter.dpr || a.hasGlobalWindow && window.devicePixelRatio || 1;
	}, t.prototype.getRenderedCanvas = function(e) {
		return this.renderToCanvas(e);
	}, t.prototype.renderToCanvas = function(e) {
		return e ||= {}, this._zr.painter.getRenderedCanvas({
			backgroundColor: e.backgroundColor || this._model.get("backgroundColor"),
			pixelRatio: e.pixelRatio || this.getDevicePixelRatio()
		});
	}, t.prototype.renderToSVGString = function(e) {
		return e ||= {}, this._zr.painter.renderToString({ useViewBox: e.useViewBox });
	}, t.prototype.getSvgDataURL = function() {
		var e = this._zr;
		return F(e.storage.getDisplayList(), function(e) {
			e.stopAnimation(null, !0);
		}), e.painter.toDataURL();
	}, t.prototype.getDataURL = function(e) {
		if (this._disposed) this.id;
		else {
			e ||= {};
			var t = e.excludeComponents, n = this._model, r = [], i = this;
			F(t, function(e) {
				n.eachComponent({ mainType: e }, function(e) {
					var t = i._componentsMap[e.__viewId];
					t.group.ignore || (r.push(t), t.group.ignore = !0);
				});
			});
			var a = this._zr.painter.getType() === "svg" ? this.getSvgDataURL() : this.renderToCanvas(e).toDataURL("image/" + (e && e.type || "png"));
			return F(r, function(e) {
				e.group.ignore = !1;
			}), a;
		}
	}, t.prototype.getConnectedDataURL = function(e) {
		if (this._disposed) this.id;
		else {
			var t = e.type === "svg", n = this.group, r = Math.min, i = Math.max, a = Infinity;
			if (Px[n]) {
				var o = a, s = a, c = -a, l = -a, u = [], d = e && e.pixelRatio || this.getDevicePixelRatio();
				F(Nx, function(a, d) {
					if (a.group === n) {
						var f = t ? a.getZr().painter.getSvgDom().innerHTML : a.renderToCanvas(k(e)), p = a.getDom().getBoundingClientRect();
						o = r(p.left, o), s = r(p.top, s), c = i(p.right, c), l = i(p.bottom, l), u.push({
							dom: f,
							left: p.left,
							top: p.top
						});
					}
				}), o *= d, s *= d, c *= d, l *= d;
				var f = c - o, m = l - s, h = p.createCanvas(), g = Ea(h, { renderer: t ? "svg" : "canvas" });
				if (g.resize({
					width: f,
					height: m
				}), t) {
					var _ = "";
					return F(u, function(e) {
						var t = e.left - o, n = e.top - s;
						_ += "<g transform=\"translate(" + t + "," + n + ")\">" + e.dom + "</g>";
					}), g.painter.getSvgRoot().innerHTML = _, e.connectedBackgroundColor && g.painter.setBackgroundColor(e.connectedBackgroundColor), g.refreshImmediately(), g.painter.toDataURL();
				}
				return e.connectedBackgroundColor && g.add(new dl({
					shape: {
						x: 0,
						y: 0,
						width: f,
						height: m
					},
					style: { fill: e.connectedBackgroundColor }
				})), F(u, function(e) {
					var t = new rl({ style: {
						x: e.left * d - o,
						y: e.top * d - s,
						image: e.dom
					} });
					g.add(t);
				}), g.refreshImmediately(), h.toDataURL("image/" + (e && e.type || "png"));
			}
			return this.getDataURL(e);
		}
	}, t.prototype.convertToPixel = function(e, t, n) {
		return ox(this, "convertToPixel", e, t, n);
	}, t.prototype.convertToLayout = function(e, t, n) {
		return ox(this, "convertToLayout", e, t, n);
	}, t.prototype.convertFromPixel = function(e, t, n) {
		return ox(this, "convertFromPixel", e, t, n);
	}, t.prototype.containPixel = function(e, t) {
		if (this._disposed) this.id;
		else {
			var n = this._model, r;
			return F(Fo(n, e), function(e, n) {
				n.indexOf("Models") >= 0 && F(e, function(e) {
					var i = e.coordinateSystem;
					if (i && i.containPoint) r ||= !!i.containPoint(t);
					else if (n === "seriesModels") {
						var a = this._chartsMap[e.__viewId];
						a && a.containPoint && (r ||= a.containPoint(t, e));
					}
				}, this);
			}, this), !!r;
		}
	}, t.prototype.getVisual = function(e, t) {
		var n = this._model, r = Fo(n, e, { defaultMainType: "series" }), i = r.seriesModel.getData(), a = r.hasOwnProperty("dataIndexInside") ? r.dataIndexInside : r.hasOwnProperty("dataIndex") ? i.indexOfRawIndex(r.dataIndex) : null;
		return a == null ? iy(i, t) : ry(i, a, t);
	}, t.prototype.getViewOfComponentModel = function(e) {
		return this._componentsMap[e.__viewId];
	}, t.prototype.getViewOfSeriesModel = function(e) {
		return this._chartsMap[e.__viewId];
	}, t.prototype._initEvents = function() {
		var e = this;
		F(Cx, function(t) {
			var n = function(n) {
				var r = e.getModel(), i = n.target, a;
				if (t === "globalout" ? a = {} : i && sy(i, function(e) {
					var t = Al(e);
					if (t && t.dataIndex != null) {
						var n = t.dataModel || r.getSeriesByIndex(t.seriesIndex);
						return a = n && n.getDataParams(t.dataIndex, t.dataType, i) || {}, !0;
					}
					if (t.eventData) return a = j({}, t.eventData), !0;
				}, !0), a) {
					var o = a.componentType, s = a.componentIndex;
					(o === "markLine" || o === "markPoint" || o === "markArea") && (o = "series", s = a.seriesIndex);
					var c = o && s != null && r.getComponent(o, s), l = c && e[c.mainType === "series" ? "_chartsMap" : "_componentsMap"][c.__viewId];
					a.event = n, a.type = t, e._$eventProcessor.eventInfo = {
						targetEl: i,
						packedEvent: a,
						model: c,
						view: l
					}, e.trigger(t, a);
				}
			};
			n.zrEventfulCallAtLast = !0, e._zr.on(t, n, e);
		});
		var t = this._messageCenter;
		F(Dx, function(n, r) {
			t.on(r, function(t) {
				e.trigger(r, t);
			});
		}), oy(t, this, this._api);
	}, t.prototype.isDisposed = function() {
		return this._disposed;
	}, t.prototype.clear = function() {
		this._disposed ? this.id : this.setOption({ series: [] }, !0);
	}, t.prototype.dispose = function() {
		if (this._disposed) this.id;
		else {
			this._disposed = !0, this.getDom() && Bo(this.getDom(), Ix, "");
			var e = this, t = e._api, n = e._model;
			F(e._componentsViews, function(e) {
				e.dispose(n, t);
			}), F(e._chartsViews, function(e) {
				e.dispose(n, t);
			}), e._zr.dispose(), e._dom = e._model = e._chartsMap = e._componentsMap = e._chartsViews = e._componentsViews = e._scheduler = e._api = e._zr = e._throttledZrFlush = e._theme = e._coordSysMgr = e._messageCenter = null, delete Nx[e.id];
		}
	}, t.prototype.resize = function(e) {
		if (!this[Hb]) {
			if (this._disposed) this.id;
			else {
				this._zr.resize(e);
				var t = this._model;
				if (this._loadingFX && this._loadingFX.resize(), t) {
					var n = t.resetOption("media"), r = e && e.silent;
					this[Wb] && (r ??= this[Wb].silent, n = !0, this[Wb] = null), this[Hb] = !0, bx(this);
					try {
						n && nx(this), ax.update.call(this, {
							type: "resize",
							animation: j({ duration: 0 }, e && e.animation)
						});
					} catch (e) {
						throw this[Hb] = !1, e;
					}
					this[Hb] = !1, lx.call(this, r), ux.call(this, r);
				}
			}
		}
	}, t.prototype.showLoading = function(e, t) {
		if (this._disposed) this.id;
		else if (W(e) && (t = e, e = ""), e ||= "default", this.hideLoading(), Mx[e]) {
			var n = Mx[e](this._api, t), r = this._zr;
			this._loadingFX = n, r.add(n);
		}
	}, t.prototype.hideLoading = function() {
		this._disposed ? this.id : (this._loadingFX && this._zr.remove(this._loadingFX), this._loadingFX = null);
	}, t.prototype.makeActionFromEvent = function(e) {
		var t = j({}, e);
		return t.type = Ex[e.type], t;
	}, t.prototype.dispatchAction = function(e, t) {
		if (this._disposed) this.id;
		else if (W(t) || (t = { silent: !!t }), Tx[e.type] && this._model) {
			if (this[Hb]) this._pendingActions.push(e);
			else {
				var n = t.silent;
				cx.call(this, e, n);
				var r = t.flush;
				r ? this._zr.flush() : r !== !1 && a.browser.weChat && this._throttledZrFlush(), lx.call(this, n), ux.call(this, n);
			}
		}
	}, t.prototype.updateLabelLayout = function() {
		cy.trigger("series:layoutlabels", this._model, this._api, { updatedSeries: [] });
	}, t.prototype.appendData = function(e) {
		if (this._disposed) this.id;
		else {
			var t = e.seriesIndex;
			this.getModel().getSeriesByIndex(t).appendData(e), this._scheduler.unfinished = !0, this.getZr().wakeUp();
		}
	}, t.internalField = function() {
		nx = function(e) {
			hy(e._model);
			var t = e._scheduler;
			t.restorePipelines(e._zr, e._model), t.prepareStageTasks(), rx(e, !0), rx(e, !1), t.plan();
		}, rx = function(e, t) {
			for (var n = e._model, r = e._scheduler, i = t ? e._componentsViews : e._chartsViews, a = t ? e._componentsMap : e._chartsMap, o = e._zr, s = e._api, c = 0; c < i.length; c++) i[c].__alive = !1;
			t ? n.eachComponent(function(e, t) {
				e !== "series" && l(t);
			}) : n.eachSeries(l);
			function l(e) {
				var c = e.__requireNewView;
				e.__requireNewView = !1;
				var l = "_ec_" + e.id + "_" + e.type, u = !c && a[l];
				if (!u) {
					var d = $o(e.type);
					u = new (t ? ov.getClass(d.main, d.sub) : uv.getClass(d.sub))(), u.init(n, s), a[l] = u, i.push(u), o.add(u.group);
				}
				e.__viewId = u.__id = l, u.__alive = !0, u.__model = e, u.group.__ecComponentInfo = {
					mainType: e.mainType,
					index: e.componentIndex
				}, !t && r.prepareView(u, e, n, s);
			}
			for (var c = 0; c < i.length;) {
				var u = i[c];
				u.__alive ? c++ : (!t && u.renderTask.dispose(), o.remove(u.group), u.dispose(n, s), i.splice(c, 1), a[u.__id] === u && delete a[u.__id], u.__id = u.group.__ecComponentInfo = null);
			}
		}, ix = function(e, t, n, r, i) {
			var a = e._model;
			if (a.setUpdatePayload(n), !r) {
				F([].concat(e._componentsViews, e._chartsViews), l);
				return;
			}
			var o = zo(n, r, i), s = n.excludeSeriesId, c;
			s != null && (c = K(), F(mo(s), function(e) {
				var t = Oo(e, null);
				t != null && c.set(t, !0);
			})), a && a.eachComponent(o, function(t) {
				if (!(c && c.get(t.id) != null)) {
					if (Ku(n)) {
						if (t instanceof Z_) n.type === "highlight" && !n.notBlur && !t.get(["emphasis", "disabled"]) && ju(t, n, e._api);
						else {
							var r = Mu(t.mainType, t.componentIndex, n.name, e._api), i = r.focusSelf, a = r.dispatchers;
							n.type === "highlight" && i && !n.notBlur && Au(t.mainType, t.componentIndex, e._api), a && F(a, function(e) {
								n.type === "highlight" ? xu(e) : Su(e);
							});
						}
					} else Gu(n) && t instanceof Z_ && (Fu(t, n, e._api), Iu(t), vx(e));
				}
			}, e), a && a.eachComponent(o, function(t) {
				c && c.get(t.id) != null || l(e[r === "series" ? "_chartsMap" : "_componentsMap"][t.__viewId]);
			}, e);
			function l(r) {
				r && r.__alive && r[t] && r[t](r.__model, a, e._api, n);
			}
		}, ax = {
			prepareAndUpdate: function(e) {
				nx(this), ax.update.call(this, e, e && { optionChanged: e.newOption != null });
			},
			update: function(e, n) {
				var r = this._model, i = this._api, a = this._zr, o = this._coordSysMgr, s = this._scheduler;
				if (r) {
					gy(r), r.setUpdatePayload(e), s.restoreData(r, e), s.performSeriesTasks(r), o.create(r, i), cy.trigger("coordsys:aftercreate", r, i), s.performDataProcessorTasks(r, e), sx(this, r), o.update(r, i), t(r), s.performVisualTasks(r, e);
					var c = r.get("backgroundColor") || "transparent";
					a.setBackgroundColor(c);
					var l = r.get("darkMode");
					l != null && l !== "auto" && a.setDarkMode(l), px(this, r, i, e, n), cy.trigger("afterupdate", r, i);
				}
			},
			updateTransform: function(e) {
				var t = this, n = t._model, r = t._api;
				if (n) {
					n.setUpdatePayload(e);
					var i = [];
					n.eachComponent(function(a, o) {
						if (a !== "series") {
							var s = t.getViewOfComponentModel(o);
							if (s && s.__alive) {
								if (s.updateTransform) {
									var c = s.updateTransform(o, n, r, e);
									c && c.update && i.push(s);
								} else i.push(s);
							}
						}
					});
					var a = K();
					n.eachSeries(function(i) {
						var o = t._chartsMap[i.__viewId], s = i.pipelineContext;
						if (o.updateTransform && !s.progressiveRender) {
							var c = o.updateTransform(i, n, r, e);
							c && c.update && a.set(i.uid, 1);
						} else a.set(i.uid, 1);
					}), t._scheduler.performVisualTasks(n, e, {
						setDirty: !0,
						dirtyMap: a
					}), hx(t, n, r, e, {}, a), cy.trigger("afterupdate", n, r);
				}
			},
			updateView: function(e) {
				var n = this._model;
				n && (n.setUpdatePayload(e), uv.markUpdateMethod(e, "updateView"), t(n), this._scheduler.performVisualTasks(n, e, { setDirty: !0 }), px(this, n, this._api, e, {}), cy.trigger("afterupdate", n, this._api));
			},
			updateVisual: function(e) {
				var n = this, r = this._model;
				r && (r.setUpdatePayload(e), r.eachSeries(function(e) {
					e.getData().clearAllVisual();
				}), uv.markUpdateMethod(e, "updateVisual"), t(r), this._scheduler.performVisualTasks(r, e, {
					visualType: "visual",
					setDirty: !0
				}), r.eachComponent(function(t, i) {
					if (t !== "series") {
						var a = n.getViewOfComponentModel(i);
						a && a.__alive && a.updateVisual(i, r, n._api, e);
					}
				}), r.eachSeries(function(t) {
					n._chartsMap[t.__viewId].updateVisual(t, r, n._api, e);
				}), cy.trigger("afterupdate", r, this._api));
			},
			updateLayout: function(e) {
				ax.update.call(this, e);
			}
		};
		function e(e, t, n, r, i) {
			if (e._disposed) e.id;
			else for (var a = e._model, o = e._coordSysMgr.getCoordinateSystems(), s, c = Fo(a, n), l = 0; l < o.length; l++) {
				var u = o[l];
				if (u[t] && (s = u[t](a, c, r, i)) != null) return s;
			}
		}
		ox = e, sx = function(e, t) {
			var n = e._chartsMap, r = e._scheduler;
			t.eachSeries(function(e) {
				r.updateStreamModes(e, n[e.__viewId]);
			});
		}, cx = function(e, t) {
			var n = this, r = this.getModel(), i = e.type, a = e.escapeConnect, o = Tx[i], s = (o.update || "update").split(":"), c = s.pop(), l = s[0] != null && $o(s[0]);
			this[Hb] = !0, bx(this);
			var u = [e], d = !1;
			e.batch && (d = !0, u = I(e.batch, function(t) {
				return t = M(j({}, t), e), t.batch = null, t;
			}));
			var f = [], p, m = [], h = o.nonRefinedEventType, g = Gu(e), _ = Ku(e);
			if (_ && Ou(this._api), F(u, function(t) {
				var i = o.action(t, r, n._api);
				if (o.refineEvent ? m.push(i) : p = i, p ||= j({}, t), p.type = h, f.push(p), _) {
					var a = Io(e), s = a.queryOptionMap, u = a.mainTypeSpecified ? s.keys()[0] : "series";
					ix(n, c, t, u), vx(n);
				} else g ? (ix(n, c, t, "series"), vx(n)) : l && ix(n, c, t, l.main, l.sub);
			}), c !== "none" && !_ && !g && !l) try {
				this[Wb] ? (nx(this), ax.update.call(this, e), this[Wb] = null) : ax[c].call(this, e);
			} catch (e) {
				throw this[Hb] = !1, e;
			}
			if (p = d ? {
				type: h,
				escapeConnect: a,
				batch: f
			} : f[0], this[Hb] = !1, !t) {
				var v = void 0;
				if (o.refineEvent) {
					var y = o.refineEvent(m, e, r, this._api).eventContent;
					_e(W(y)), v = M({ type: o.refinedEventType }, y), v.fromAction = e.type, v.fromActionPayload = e, v.escapeConnect = !0;
				}
				var b = this._messageCenter;
				b.trigger(p.type, p), v && b.trigger(v.type, v);
			}
		}, lx = function(e) {
			for (var t = this._pendingActions; t.length;) {
				var n = t.shift();
				cx.call(this, n, e);
			}
		}, ux = function(e) {
			!e && this.trigger("updated");
		}, dx = function(e, t) {
			e.on("rendered", function(n) {
				t.trigger("rendered", n), e.animation.isFinished() && !t[Wb] && !t._scheduler.unfinished && !t._pendingActions.length ? t.trigger("finished") : e.refresh();
			});
		}, fx = function(e, t) {
			e.on("mouseover", function(e) {
				var n = e.target, r = sy(n, Uu);
				r && (Nu(r, e, t._api), vx(t));
			}).on("mouseout", function(e) {
				var n = e.target, r = sy(n, Uu);
				r && (Pu(r, e, t._api), vx(t));
			}).on("click", function(e) {
				var n = e.target, r = sy(n, function(e) {
					return Al(e).dataIndex != null;
				}, !0);
				if (r) {
					var i = r.selected ? "unselect" : "select", a = Al(r);
					t._api.dispatchAction({
						type: i,
						dataType: a.dataType,
						dataIndexInside: a.dataIndex,
						seriesIndex: a.seriesIndex,
						isFromClick: !0
					});
				}
			});
		};
		function t(e) {
			e.clearColorPalette(), e.eachSeries(function(e) {
				e.clearColorPalette();
			});
		}
		function n(e) {
			var t = [], n = [], r = !1;
			if (e.eachComponent(function(e, i) {
				var a = i.get("zlevel") || 0, o = i.get("z") || 0, s = i.getZLevelKey();
				r ||= !!s, (e === "series" ? n : t).push({
					zlevel: a,
					z: o,
					idx: i.componentIndex,
					type: e,
					key: s
				});
			}), r) {
				var i = t.concat(n), a, o;
				On(i, function(e, t) {
					return e.zlevel === t.zlevel ? e.z - t.z : e.zlevel - t.zlevel;
				}), F(i, function(t) {
					var n = e.getComponent(t.type, t.idx), r = t.zlevel, i = t.key;
					a != null && (r = Math.max(a, r)), i ? (r === a && i !== o && r++, o = i) : o &&= (r === a && r++, ""), a = r, n.setZLevel(r);
				});
			}
		}
		px = function(e, t, r, i, a) {
			n(t), mx(e, t, r, i, a), F(e._chartsViews, function(e) {
				e.__alive = !1;
			}), hx(e, t, r, i, a), F(e._chartsViews, function(e) {
				e.__alive || e.remove(t, r);
			});
		}, mx = function(e, t, n, r, i, a) {
			F(a || e._componentsViews, function(e) {
				var i = e.__model;
				l(i, e), e.render(i, t, n, r), c(i, e), u(i, e);
			});
		}, hx = function(e, t, n, r, i, a) {
			var d = e._scheduler;
			i = j(i || {}, { updatedSeries: t.getSeries() }), cy.trigger("series:beforeupdate", t, n, i);
			var f = !1;
			t.eachSeries(function(t) {
				var n = e._chartsMap[t.__viewId];
				n.__alive = !0;
				var i = n.renderTask;
				d.updatePayload(i, r), l(t, n), a && a.get(t.uid) && i.dirty(), i.perform(d.getPerformArgs(i)) && (f = !0), n.group.silent = !!t.get("silent"), s(t, n), Iu(t);
			}), d.unfinished = f || d.unfinished, cy.trigger("series:layoutlabels", t, n, i), cy.trigger("series:transition", t, n, i), t.eachSeries(function(t) {
				var n = e._chartsMap[t.__viewId];
				c(t, n), u(t, n);
			}), o(e, t), cy.trigger("series:afterupdate", t, n, i);
		}, vx = function(e) {
			e[Gb] = !0, e.getZr().wakeUp();
		}, bx = function(e) {
			e[Ub] = (e[Ub] + 1) % 1e6;
		}, yx = function(e) {
			e[Gb] && (e.getZr().storage.traverse(function(e) {
				xf(e) || i(e);
			}), e[Gb] = !1);
		};
		function i(e) {
			for (var t = [], n = e.currentStates, r = 0; r < n.length; r++) {
				var i = n[r];
				i !== "emphasis" && i !== "blur" && i !== "select" && t.push(i);
			}
			e.selected && e.states.select && t.push("select"), e.hoverState === 2 && e.states.emphasis ? t.push("emphasis") : e.hoverState === 1 && e.states.blur && t.push("blur"), e.useStates(t);
		}
		function o(e, t) {
			var n = e._zr;
			if (n.painter.type === "canvas") {
				var r = n.storage, i = 0;
				r.traverse(function(e) {
					e.isGroup || i++;
				});
				var o = i > G(t.get("hoverLayerThreshold"), lh.hoverLayerThreshold) && !a.node && !a.worker;
				(e._usingTHL || o) && (t.eachSeries(function(t) {
					if (!t.preventUsingHoverLayer) {
						var n = e._chartsMap[t.__viewId];
						n.__alive && n.eachRendered(function(e) {
							var t = e.states.emphasis;
							t && t.hoverLayer !== 2 && (t.hoverLayer = +!!o);
						});
					}
				}), e._usingTHL = o);
			}
		}
		function s(e, t) {
			var n = e.get("blendMode") || null;
			t.eachRendered(function(e) {
				e.isGroup || (e.style.blend = n);
			});
		}
		function c(e, t) {
			if (!e.preventAutoZ) {
				var n = fp(e);
				t.eachRendered(function(e) {
					return mp(e, n.z, n.zlevel), !0;
				});
			}
		}
		function l(e, t) {
			t.eachRendered(function(e) {
				if (!xf(e)) {
					var t = e.getTextContent(), n = e.getTextGuideLine();
					e.stateTransition &&= null, t && t.stateTransition && (t.stateTransition = null), n && n.stateTransition && (n.stateTransition = null), e.hasState() ? (e.prevStates = e.currentStates, e.clearStates()) : e.prevStates &&= null;
				}
			});
		}
		function u(e, t) {
			var n = e.getModel("stateAnimation"), r = e.isAnimationEnabled(), a = n.get("duration"), o = a > 0 ? {
				duration: a,
				delay: n.get("delay"),
				easing: n.get("easing")
			} : null;
			t.eachRendered(function(e) {
				if (e.states && e.states.emphasis) {
					if (xf(e)) return;
					if (e instanceof Z && qu(e), e.__dirty) {
						var t = e.prevStates;
						t && e.useStates(t);
					}
					if (r) {
						e.stateTransition = o;
						var n = e.getTextContent(), a = e.getTextGuideLine();
						n && (n.stateTransition = o), a && (a.stateTransition = o);
					}
					e.__dirty && i(e);
				}
			});
		}
		gx = function(e) {
			return new (function(t) {
				r(n, t);
				function n() {
					return t !== null && t.apply(this, arguments) || this;
				}
				return n.prototype.getCoordinateSystems = function() {
					return e._coordSysMgr.getCoordinateSystems();
				}, n.prototype.getComponentByElement = function(t) {
					for (; t;) {
						var n = t.__ecComponentInfo;
						if (n != null) return e._model.getComponent(n.mainType, n.index);
						t = t.parent;
					}
				}, n.prototype.enterEmphasis = function(t, n) {
					xu(t, n), vx(e);
				}, n.prototype.leaveEmphasis = function(t, n) {
					Su(t, n), vx(e);
				}, n.prototype.enterBlur = function(t) {
					Cu(t), vx(e);
				}, n.prototype.leaveBlur = function(t) {
					wu(t), vx(e);
				}, n.prototype.enterSelect = function(t) {
					Tu(t), vx(e);
				}, n.prototype.leaveSelect = function(t) {
					Eu(t), vx(e);
				}, n.prototype.getModel = function() {
					return e.getModel();
				}, n.prototype.getViewOfComponentModel = function(t) {
					return e.getViewOfComponentModel(t);
				}, n.prototype.getViewOfSeriesModel = function(t) {
					return e.getViewOfSeriesModel(t);
				}, n.prototype.getECUpdateCycleVersion = function() {
					return e[Ub];
				}, n.prototype.usingTHL = function() {
					return e._usingTHL;
				}, n;
			}(Ul))(e);
		}, _x = function(e) {
			function t(e, t) {
				for (var n = 0; n < e.length; n++) {
					var r = e[n];
					r[qb] = t;
				}
			}
			F(Ex, function(n, r) {
				e._messageCenter.on(r, function(n) {
					if (Px[e.group] && e[qb] !== Jb) {
						if (n && n.escapeConnect) return;
						var r = e.makeActionFromEvent(n), i = [];
						F(Nx, function(t) {
							t !== e && t.group === e.group && i.push(t);
						}), t(i, Jb), F(i, function(e) {
							e[qb] !== Yb && e.dispatchAction(r);
						}), t(i, Xb);
					}
				});
			});
		};
	}(), t;
}(Qe), Sx = xx.prototype;
Sx.on = Zb("on"), Sx.off = Zb("off"), Sx.one = function(e, t, n) {
	var r = this;
	function i() {
		var n = [...arguments];
		t && t.apply && t.apply(this, n), r.off(e, i);
	}
	this.on.call(this, e, i, n);
};
var Cx = [
	"click",
	"dblclick",
	"mouseover",
	"mouseout",
	"mousemove",
	"mousedown",
	"mouseup",
	"globalout",
	"contextmenu"
], Tx = {}, Ex = {}, Dx = {}, Ox = [], kx = [], Ax = [], jx = {}, Mx = {}, Nx = {}, Px = {}, Fx = /* @__PURE__ */ new Date() - 0;
/* @__PURE__ */ new Date() - 0;
var Ix = "_echarts_instance_";
function Lx(e, t, n) {
	var r = !(n && n.ssr);
	if (r) {
		var i = Rx(e);
		if (i) return i;
	}
	var a = new xx(e, t, n);
	return a.id = "ec_" + Fx++, Nx[a.id] = a, r && Bo(e, Ix, a.id), _x(a), cy.trigger("afterinit", a), a;
}
function Rx(e) {
	return Nx[Vo(e, Ix)];
}
function zx(e, t) {
	jx[e] = t;
}
function Bx(e) {
	N(kx, e) < 0 && kx.push(e);
}
function Vx(e, t) {
	Xx(Ox, e, t, kb);
}
function Hx(e) {
	Wx("afterinit", e);
}
function Ux(e) {
	Wx("afterupdate", e);
}
function Wx(e, t) {
	cy.on(e, t);
}
function Gx(e, t, n) {
	var r, i, a, o, s;
	V(t) && (n = t, t = ""), W(e) ? (r = e.type, i = e.event, o = e.update, s = e.publishNonRefinedEvent, n ||= e.action, a = e.refineEvent) : (r = e, i = t);
	function c(e) {
		return e.toLowerCase();
	}
	i = c(i || r);
	var l = a ? c(r) : i;
	Tx[r] || (_e(Kb.test(r) && Kb.test(i)), a && _e(i !== r), Tx[r] = {
		actionType: r,
		refinedEventType: i,
		nonRefinedEventType: l,
		update: o,
		action: n,
		refineEvent: a
	}, Dx[i] = 1, a && s && (Dx[l] = 1), Ex[l] = r);
}
function Kx(e, t) {
	Pm.register(e, t);
}
function qx(e, t) {
	Xx(Ax, e, t, jb, "layout", !0);
}
function Jx(e, t) {
	Xx(Ax, e, t, Pb, "visual", !0);
}
var Yx = [];
function Xx(e, t, n, r, i, a) {
	if ((V(t) || W(t)) && (n = t, t = r), !(N(Yx, n) >= 0)) {
		Yx.push(n);
		var o = Nv.wrapStageHandler(n, i);
		o.__prio = t, o.__raw = n, e.push(o);
	}
}
function Zx(e, t) {
	Mx[e] = t;
}
function Qx(e, t, n) {
	var r = dy("registerMap");
	r && r(e, t, n);
}
var $x = l_;
Jx(Nb, Dv), Jx(Ib, kv), Jx(Ib, Av), Jx(Nb, ty), Jx(Ib, ny), Jx(Bb, Sb), Bx(dg), Vx(Eb, fg), Zx("default", Mv), Gx({
	type: Zl,
	event: Zl,
	update: Zl
}, Ae), Gx({
	type: Ql,
	event: Ql,
	update: Ql
}, Ae), Gx({
	type: $l,
	event: nu,
	update: $l,
	action: Ae,
	refineEvent: eS,
	publishNonRefinedEvent: !0
}), Gx({
	type: eu,
	event: nu,
	update: eu,
	action: Ae,
	refineEvent: eS,
	publishNonRefinedEvent: !0
}), Gx({
	type: tu,
	event: nu,
	update: tu,
	action: Ae,
	refineEvent: eS,
	publishNonRefinedEvent: !0
});
function eS(e, t, n, r) {
	return { eventContent: {
		selected: Lu(n),
		isFromClick: t.isFromClick || !1
	} };
}
zx("default", {}), zx("dark", Zv);
//#endregion
//#region node_modules/echarts/lib/data/DataDiffer.js
function tS(e) {
	return e == null ? 0 : e.length || 1;
}
function nS(e) {
	return e;
}
var rS = function() {
	function e(e, t, n, r, i, a) {
		this._old = e, this._new = t, this._oldKeyGetter = n || nS, this._newKeyGetter = r || nS, this.context = i, this._diffModeMultiple = a === "multiple";
	}
	return e.prototype.add = function(e) {
		return this._add = e, this;
	}, e.prototype.update = function(e) {
		return this._update = e, this;
	}, e.prototype.updateManyToOne = function(e) {
		return this._updateManyToOne = e, this;
	}, e.prototype.updateOneToMany = function(e) {
		return this._updateOneToMany = e, this;
	}, e.prototype.updateManyToMany = function(e) {
		return this._updateManyToMany = e, this;
	}, e.prototype.remove = function(e) {
		return this._remove = e, this;
	}, e.prototype.execute = function() {
		this[this._diffModeMultiple ? "_executeMultiple" : "_executeOneToOne"]();
	}, e.prototype._executeOneToOne = function() {
		var e = this._old, t = this._new, n = {}, r = Array(e.length), i = Array(t.length);
		this._initIndexMap(e, null, r, "_oldKeyGetter"), this._initIndexMap(t, n, i, "_newKeyGetter");
		for (var a = 0; a < e.length; a++) {
			var o = r[a], s = n[o], c = tS(s);
			if (c > 1) {
				var l = s.shift();
				s.length === 1 && (n[o] = s[0]), this._update && this._update(l, a);
			} else c === 1 ? (n[o] = null, this._update && this._update(s, a)) : this._remove && this._remove(a);
		}
		this._performRestAdd(i, n);
	}, e.prototype._executeMultiple = function() {
		var e = this._old, t = this._new, n = {}, r = {}, i = [], a = [];
		this._initIndexMap(e, n, i, "_oldKeyGetter"), this._initIndexMap(t, r, a, "_newKeyGetter");
		for (var o = 0; o < i.length; o++) {
			var s = i[o], c = n[s], l = r[s], u = tS(c), d = tS(l);
			if (u > 1 && d === 1) this._updateManyToOne && this._updateManyToOne(l, c), r[s] = null;
			else if (u === 1 && d > 1) this._updateOneToMany && this._updateOneToMany(l, c), r[s] = null;
			else if (u === 1 && d === 1) this._update && this._update(l, c), r[s] = null;
			else if (u > 1 && d > 1) this._updateManyToMany && this._updateManyToMany(l, c), r[s] = null;
			else if (u > 1) for (var f = 0; f < u; f++) this._remove && this._remove(c[f]);
			else this._remove && this._remove(c);
		}
		this._performRestAdd(a, r);
	}, e.prototype._performRestAdd = function(e, t) {
		for (var n = 0; n < e.length; n++) {
			var r = e[n], i = t[r], a = tS(i);
			if (a > 1) for (var o = 0; o < a; o++) this._add && this._add(i[o]);
			else a === 1 && this._add && this._add(i);
			t[r] = null;
		}
	}, e.prototype._initIndexMap = function(e, t, n, r) {
		for (var i = this._diffModeMultiple, a = 0; a < e.length; a++) {
			var o = "_ec_" + this[r](e[a], a);
			if (i || (n[a] = o), t) {
				var s = t[o], c = tS(s);
				c === 0 ? (t[o] = a, i && n.push(o)) : c === 1 ? t[o] = [s, a] : s.push(a);
			}
		}
	}, e;
}(), iS = function() {
	function e(e, t) {
		this._encode = e, this._schema = t;
	}
	return e.prototype.get = function() {
		return {
			fullDimensions: this._getFullDimensionNames(),
			encode: this._encode
		};
	}, e.prototype._getFullDimensionNames = function() {
		return this._cachedDimNames ||= this._schema ? this._schema.makeOutputDimensionNames() : [], this._cachedDimNames;
	}, e;
}();
function aS(e, t) {
	var n = {}, r = n.encode = {}, i = K(), a = [], o = [], s = {};
	F(e.dimensions, function(t) {
		var n = e.getDimensionInfo(t), c = n.coordDim;
		if (c) {
			var l = n.coordDimIndex;
			oS(r, c)[l] = t, n.isExtraCoord || (i.set(c, 1), cS(n.type) && (a[0] = t), oS(s, c)[l] = e.getDimensionIndex(n.name)), n.defaultTooltip && o.push(t);
		}
		Nl.each(function(e, t) {
			var i = oS(r, t), a = n.otherDims[t];
			a != null && a !== !1 && (i[a] = n.name);
		});
	});
	var c = [], l = {};
	i.each(function(e, t) {
		var n = r[t];
		l[t] = n[0], c = c.concat(n);
	}), n.dataDimsOnCoord = c, n.dataDimIndicesOnCoord = I(c, function(t) {
		return e.getDimensionInfo(t).storeDimIndex;
	}), n.encodeFirstDimNotExtra = l;
	var u = r.label;
	u && u.length && (a = u.slice());
	var d = r.tooltip;
	return d && d.length ? o = d.slice() : o.length || (o = a.slice()), r.defaultedLabel = a, r.defaultedTooltip = o, n.userOutput = new iS(s, t), n;
}
function oS(e, t) {
	return e.hasOwnProperty(t) || (e[t] = []), e[t];
}
function sS(e) {
	return e === "category" ? "ordinal" : e === "time" ? "time" : "float";
}
function cS(e) {
	return e !== "ordinal" && e !== "time";
}
//#endregion
//#region node_modules/echarts/lib/data/SeriesDimensionDefine.js
var lS = function() {
	function e(e) {
		this.otherDims = {}, e != null && j(this, e);
	}
	return e;
}(), uS = Y(), dS = {
	float: "f",
	int: "i",
	ordinal: "o",
	number: "n",
	time: "t"
}, fS = function() {
	function e(e) {
		this.dimensions = e.dimensions, this._dimOmitted = e.dimensionOmitted, this.source = e.source, this._fullDimCount = e.fullDimensionCount, this._updateDimOmitted(e.dimensionOmitted);
	}
	return e.prototype.isDimensionOmitted = function() {
		return this._dimOmitted;
	}, e.prototype._updateDimOmitted = function(e) {
		this._dimOmitted = e, e && (this._dimNameMap ||= hS(this.source));
	}, e.prototype.getSourceDimensionIndex = function(e) {
		return G(this._dimNameMap.get(e), -1);
	}, e.prototype.getSourceDimension = function(e) {
		var t = this.source.dimensionsDefine;
		if (t) return t[e];
	}, e.prototype.makeStoreSchema = function() {
		for (var e = this._fullDimCount, t = Tg(this.source), n = !gS(e), r = "", i = [], a = 0, o = 0; a < e; a++) {
			var s = void 0, c = void 0, l = void 0, u = this.dimensions[o];
			if (u && u.storeDimIndex === a) s = t ? u.name : null, c = u.type, l = u.ordinalMeta, o++;
			else {
				var d = this.getSourceDimension(a);
				d && (s = t ? d.name : null, c = d.type);
			}
			i.push({
				property: s,
				type: c,
				ordinalMeta: l
			}), t && s != null && (!u || !u.isCalculationCoord) && (r += n ? s.replace(/\`/g, "`1").replace(/\$/g, "`2") : s), r += "$", r += dS[c] || "f", l && (r += l.uid), r += "$";
		}
		var f = this.source;
		return {
			dimensions: i,
			hash: [
				f.seriesLayoutBy,
				f.startIndex,
				r
			].join("$$")
		};
	}, e.prototype.makeOutputDimensionNames = function() {
		for (var e = [], t = 0, n = 0; t < this._fullDimCount; t++) {
			var r = void 0, i = this.dimensions[n];
			if (i && i.storeDimIndex === t) i.isCalculationCoord || (r = i.name), n++;
			else {
				var a = this.getSourceDimension(t);
				a && (r = a.name);
			}
			e.push(r);
		}
		return e;
	}, e.prototype.appendCalculationDimension = function(e) {
		this.dimensions.push(e), e.isCalculationCoord = !0, this._fullDimCount++, this._updateDimOmitted(!0);
	}, e;
}();
function pS(e) {
	return e instanceof fS;
}
function mS(e) {
	for (var t = K(), n = 0; n < (e || []).length; n++) {
		var r = e[n], i = W(r) ? r.name : r;
		i != null && t.get(i) == null && t.set(i, n);
	}
	return t;
}
function hS(e) {
	var t = uS(e);
	return t.dimNameMap ||= mS(e.dimensionsDefine);
}
function gS(e) {
	return e > 30;
}
//#endregion
//#region node_modules/echarts/lib/data/SeriesData.js
var _S = W, vS = I, yS = typeof Int32Array > "u" ? Array : Int32Array, bS = "e\0\0", xS = -1, SS = [
	"hasItemOption",
	"_nameList",
	"_idList",
	"_invertedIndicesMap",
	"_dimSummary",
	"userOutput",
	"_rawData",
	"_dimValueGetter",
	"_nameDimIdx",
	"_idDimIdx",
	"_nameRepeatCount"
], CS = ["_approximateExtent"], wS, TS, ES, DS, OS, kS, AS, jS = function() {
	function e(e, t) {
		this.type = "list", this._dimOmitted = !1, this._nameList = [], this._idList = [], this._visual = {}, this._layout = {}, this._itemVisuals = [], this._itemLayouts = [], this._graphicEls = [], this._approximateExtent = {}, this._calculationInfo = {}, this.hasItemOption = !1, this.TRANSFERABLE_METHODS = [
			"cloneShallow",
			"downSample",
			"minmaxDownSample",
			"lttbDownSample",
			"map"
		], this.CHANGABLE_METHODS = ["filterSelf", "selectRange"], this.DOWNSAMPLE_METHODS = [
			"downSample",
			"minmaxDownSample",
			"lttbDownSample"
		];
		var n, r = !1;
		pS(e) ? (n = e.dimensions, this._dimOmitted = e.isDimensionOmitted(), this._schema = e) : (r = !0, n = e), n ||= ["x", "y"];
		for (var i = {}, a = [], o = {}, s = !1, c = {}, l = 0; l < n.length; l++) {
			var u = n[l], d = H(u) ? new lS({ name: u }) : u instanceof lS ? u : new lS(u), f = d.name;
			d.type = d.type || "float", d.coordDim || (d.coordDim = f, d.coordDimIndex = 0);
			var p = d.otherDims = d.otherDims || {};
			a.push(f), i[f] = d, c[f] != null && (s = !0), d.createInvertedIndices && (o[f] = []), r && (d.storeDimIndex = l), p.itemName === 0 && (this._nameDimIdx = d.storeDimIndex), p.itemId === 0 && (this._idDimIdx = d.storeDimIndex);
		}
		if (this.dimensions = a, this._dimInfos = i, this._initGetDimensionInfo(s), this.hostModel = t, this._invertedIndicesMap = o, this._dimOmitted) {
			var m = this._dimIdxToName = K();
			F(a, function(e) {
				m.set(i[e].storeDimIndex, e);
			});
		}
	}
	return e.prototype.getDimension = function(e) {
		var t = this._recognizeDimIndex(e);
		if (t == null) return e;
		if (t = e, !this._dimOmitted) return this.dimensions[t];
		var n = this._dimIdxToName.get(t);
		if (n != null) return n;
		var r = this._schema.getSourceDimension(t);
		if (r) return r.name;
	}, e.prototype.getDimensionIndex = function(e) {
		var t = this._recognizeDimIndex(e);
		if (t != null) return t;
		if (e == null) return -1;
		var n = this._getDimInfo(e);
		return n ? n.storeDimIndex : this._dimOmitted ? this._schema.getSourceDimensionIndex(e) : -1;
	}, e.prototype._recognizeDimIndex = function(e) {
		if (U(e) || e != null && !isNaN(e) && !this._getDimInfo(e) && (!this._dimOmitted || this._schema.getSourceDimensionIndex(e) < 0)) return +e;
	}, e.prototype._getStoreDimIndex = function(e) {
		return this.getDimensionIndex(e);
	}, e.prototype.getDimensionInfo = function(e) {
		return this._getDimInfo(this.getDimension(e));
	}, e.prototype._initGetDimensionInfo = function(e) {
		var t = this._dimInfos;
		this._getDimInfo = e ? function(e) {
			return t.hasOwnProperty(e) ? t[e] : void 0;
		} : function(e) {
			return t[e];
		};
	}, e.prototype.getDimensionsOnCoord = function() {
		return this._dimSummary.dataDimsOnCoord.slice();
	}, e.prototype.mapDimension = function(e, t) {
		var n = this._dimSummary;
		if (t == null) return n.encodeFirstDimNotExtra[e];
		var r = n.encode[e];
		return r ? r[t] : null;
	}, e.prototype.mapDimensionsAll = function(e) {
		return (this._dimSummary.encode[e] || []).slice();
	}, e.prototype.getStore = function() {
		return this._store;
	}, e.prototype.initData = function(e, t, n) {
		var r = this, i;
		if (e instanceof S_ && (i = e), !i) {
			var a = this.dimensions, o = gg(e) || P(e) ? new Mg(e, a.length) : e;
			i = new S_();
			var s = vS(a, function(e) {
				return {
					type: r._dimInfos[e].type,
					property: e
				};
			});
			i.initData(o, s, n);
		}
		this._store = i, this._nameList = (t || []).slice(), this._idList = [], this._nameRepeatCount = {}, this._doInit(0, i.count()), this._dimSummary = aS(this, this._schema), this.userOutput = this._dimSummary.userOutput;
	}, e.prototype.appendData = function(e) {
		var t = this._store.appendData(e);
		this._doInit(t[0], t[1]);
	}, e.prototype.appendValues = function(e, t) {
		var n = this._store.appendValues(e, t && t.length), r = n.start, i = n.end, a = this._shouldMakeIdFromName();
		if (this._updateOrdinalMeta(), t) for (var o = r; o < i; o++) {
			var s = o - r;
			this._nameList[o] = t[s], a && AS(this, o);
		}
	}, e.prototype._updateOrdinalMeta = function() {
		for (var e = this._store, t = this.dimensions, n = 0; n < t.length; n++) {
			var r = this._dimInfos[t[n]];
			r.ordinalMeta && e.collectOrdinalMeta(r.storeDimIndex, r.ordinalMeta);
		}
	}, e.prototype._shouldMakeIdFromName = function() {
		var e = this._store.getProvider();
		return this._idDimIdx == null && e.getSource().sourceFormat !== "typedArray" && !e.fillStorage;
	}, e.prototype._doInit = function(e, t) {
		if (!(e >= t)) {
			var n = this._store.getProvider();
			this._updateOrdinalMeta();
			var r = this._nameList, i = this._idList;
			if (n.getSource().sourceFormat === "original" && !n.pure) for (var a = [], o = e; o < t; o++) {
				var s = n.getItem(o, a);
				if (!this.hasItemOption && vo(s) && (this.hasItemOption = !0), s) {
					var c = s.name;
					r[o] == null && c != null && (r[o] = Oo(c, null));
					var l = s.id;
					i[o] == null && l != null && (i[o] = Oo(l, null));
				}
			}
			if (this._shouldMakeIdFromName()) for (var o = e; o < t; o++) AS(this, o);
			wS(this);
		}
	}, e.prototype.getApproximateExtent = function(e, t) {
		return this._approximateExtent[e] || this._store.getDataExtent(this._getStoreDimIndex(e), t);
	}, e.prototype.setApproximateExtent = function(e, t) {
		t = this.getDimension(t), this._approximateExtent[t] = e.slice();
	}, e.prototype.getCalculationInfo = function(e) {
		return this._calculationInfo[e];
	}, e.prototype.setCalculationInfo = function(e, t) {
		_S(e) ? j(this._calculationInfo, e) : this._calculationInfo[e] = t;
	}, e.prototype.getName = function(e) {
		var t = this.getRawIndex(e), n = this._nameList[t];
		return n == null && this._nameDimIdx != null && (n = ES(this, this._nameDimIdx, t)), n ??= "", n;
	}, e.prototype._getCategory = function(e, t) {
		var n = this._store.get(e, t), r = this._store.getOrdinalMeta(e);
		return r ? r.categories[n] : n;
	}, e.prototype.getId = function(e) {
		return TS(this, this.getRawIndex(e));
	}, e.prototype.count = function() {
		return this._store.count();
	}, e.prototype.get = function(e, t) {
		var n = this._store, r = this._dimInfos[e];
		if (r) return n.get(r.storeDimIndex, t);
	}, e.prototype.getByRawIndex = function(e, t) {
		var n = this._store, r = this._dimInfos[e];
		if (r) return n.getByRawIndex(r.storeDimIndex, t);
	}, e.prototype.getIndices = function() {
		return this._store.getIndices();
	}, e.prototype.getDataExtent = function(e) {
		return this._store.getDataExtent(this._getStoreDimIndex(e), null);
	}, e.prototype.getSum = function(e) {
		return this._store.getSum(this._getStoreDimIndex(e));
	}, e.prototype.getMedian = function(e) {
		return this._store.getMedian(this._getStoreDimIndex(e));
	}, e.prototype.getValues = function(e, t) {
		var n = this, r = this._store;
		return B(e) ? r.getValues(vS(e, function(e) {
			return n._getStoreDimIndex(e);
		}), t) : r.getValues(e);
	}, e.prototype.hasValue = function(e) {
		for (var t = this._dimSummary.dataDimIndicesOnCoord, n = 0, r = t.length; n < r; n++) if (isNaN(this._store.get(t[n], e))) return !1;
		return !0;
	}, e.prototype.indexOfName = function(e) {
		for (var t = 0, n = this._store.count(); t < n; t++) if (this.getName(t) === e) return t;
		return -1;
	}, e.prototype.getRawIndex = function(e) {
		return this._store.getRawIndex(e);
	}, e.prototype.indexOfRawIndex = function(e) {
		return this._store.indexOfRawIndex(e);
	}, e.prototype.rawIndexOf = function(e, t) {
		var n = e && this._invertedIndicesMap[e], r = n && n[t];
		return r == null || isNaN(r) ? xS : r;
	}, e.prototype.each = function(e, t, n) {
		V(e) && (n = t, t = e, e = []);
		var r = n || this, i = vS(DS(e), this._getStoreDimIndex, this);
		this._store.each(i, r ? R(t, r) : t);
	}, e.prototype.filterSelf = function(e, t, n) {
		V(e) && (n = t, t = e, e = []);
		var r = n || this, i = vS(DS(e), this._getStoreDimIndex, this);
		return this._store = this._store.filter(i, r ? R(t, r) : t), this;
	}, e.prototype.selectRange = function(e) {
		var t = this, n = {}, r = L(e), i = [];
		return F(r, function(r) {
			var a = t._getStoreDimIndex(r);
			n[a] = e[r], i.push(a);
		}), this._store = this._store.selectRange(n), this;
	}, e.prototype.mapArray = function(e, t, n) {
		V(e) && (n = t, t = e, e = []), n ||= this;
		var r = [];
		return this.each(e, function() {
			r.push(t && t.apply(this, arguments));
		}, n), r;
	}, e.prototype.map = function(e, t, n, r) {
		var i = n || r || this, a = vS(DS(e), this._getStoreDimIndex, this), o = kS(this);
		return o._store = this._store.map(a, i ? R(t, i) : t), o;
	}, e.prototype.modify = function(e, t, n, r) {
		var i = n || r || this, a = vS(DS(e), this._getStoreDimIndex, this);
		this._store.modify(a, i ? R(t, i) : t);
	}, e.prototype.downSample = function(e, t, n, r) {
		var i = kS(this);
		return i._store = this._store.downSample(this._getStoreDimIndex(e), t, n, r), i;
	}, e.prototype.minmaxDownSample = function(e, t) {
		var n = kS(this);
		return n._store = this._store.minmaxDownSample(this._getStoreDimIndex(e), t), n;
	}, e.prototype.lttbDownSample = function(e, t) {
		var n = kS(this);
		return n._store = this._store.lttbDownSample(this._getStoreDimIndex(e), t), n;
	}, e.prototype.getRawDataItem = function(e) {
		return this._store.getRawDataItem(e);
	}, e.prototype.getItemModel = function(e) {
		var t = this.hostModel;
		return new Kp(this.getRawDataItem(e), t, t && t.ecModel);
	}, e.prototype.diff = function(e) {
		var t = this;
		return new rS(e ? e.getStore().getIndices() : [], this.getStore().getIndices(), function(t) {
			return TS(e, t);
		}, function(e) {
			return TS(t, e);
		});
	}, e.prototype.getVisual = function(e) {
		var t = this._visual;
		return t && t[e];
	}, e.prototype.setVisual = function(e, t) {
		this._visual = this._visual || {}, _S(e) ? j(this._visual, e) : this._visual[e] = t;
	}, e.prototype.getItemVisual = function(e, t) {
		var n = this._itemVisuals[e];
		return (n && n[t]) ?? this.getVisual(t);
	}, e.prototype.hasItemVisual = function() {
		return this._itemVisuals.length > 0;
	}, e.prototype.ensureUniqueItemVisual = function(e, t) {
		var n = this._itemVisuals, r = n[e];
		r ||= n[e] = {};
		var i = r[t];
		return i ?? (i = this.getVisual(t), B(i) ? i = i.slice() : _S(i) && (i = j({}, i)), r[t] = i), i;
	}, e.prototype.setItemVisual = function(e, t, n) {
		var r = this._itemVisuals[e] || {};
		this._itemVisuals[e] = r, _S(t) ? j(r, t) : r[t] = n;
	}, e.prototype.clearAllVisual = function() {
		this._visual = {}, this._itemVisuals = [];
	}, e.prototype.setLayout = function(e, t) {
		_S(e) ? j(this._layout, e) : this._layout[e] = t;
	}, e.prototype.getLayout = function(e) {
		return this._layout[e];
	}, e.prototype.getItemLayout = function(e) {
		return this._itemLayouts[e];
	}, e.prototype.setItemLayout = function(e, t, n) {
		this._itemLayouts[e] = n ? j(this._itemLayouts[e] || {}, t) : t;
	}, e.prototype.clearItemLayouts = function() {
		this._itemLayouts.length = 0;
	}, e.prototype.setItemGraphicEl = function(e, t) {
		jl(this.hostModel && this.hostModel.seriesIndex, this.dataType, e, t), this._graphicEls[e] = t;
	}, e.prototype.getItemGraphicEl = function(e) {
		return this._graphicEls[e];
	}, e.prototype.eachItemGraphicEl = function(e, t) {
		F(this._graphicEls, function(n, r) {
			n && e && e.call(t, n, r);
		});
	}, e.prototype.cloneShallow = function(t) {
		return t ||= new e(this._schema ? this._schema : vS(this.dimensions, this._getDimInfo, this), this.hostModel), OS(t, this), t._store = this._store, t;
	}, e.prototype.wrapMethod = function(e, t) {
		var n = this[e];
		V(n) && (this.__wrappedMethods = this.__wrappedMethods || [], this.__wrappedMethods.push(e), this[e] = function() {
			var e = n.apply(this, arguments);
			return t.apply(this, [e].concat(he(arguments)));
		});
	}, e.internalField = function() {
		wS = function(e) {
			var t = e._invertedIndicesMap;
			F(t, function(n, r) {
				var i = e._dimInfos[r], a = i.ordinalMeta, o = e._store;
				if (a) {
					n = t[r] = new yS(a.categories.length);
					for (var s = 0; s < n.length; s++) n[s] = xS;
					for (var s = 0; s < o.count(); s++) n[o.get(i.storeDimIndex, s)] = s;
				}
			});
		}, ES = function(e, t, n) {
			return Oo(e._getCategory(t, n), null);
		}, TS = function(e, t) {
			var n = e._idList[t];
			return n == null && e._idDimIdx != null && (n = ES(e, e._idDimIdx, t)), n ??= bS + t, n;
		}, DS = function(e) {
			return B(e) || (e = e == null ? [] : [e]), e;
		}, kS = function(t) {
			var n = new e(t._schema ? t._schema : vS(t.dimensions, t._getDimInfo, t), t.hostModel);
			return OS(n, t), n;
		}, OS = function(e, t) {
			F(SS.concat(t.__wrappedMethods || []), function(n) {
				t.hasOwnProperty(n) && (e[n] = t[n]);
			}), e.__wrappedMethods = t.__wrappedMethods, F(CS, function(n) {
				e[n] = k(t[n]);
			}), e._calculationInfo = j({}, t._calculationInfo);
		}, AS = function(e, t) {
			var n = e._nameList, r = e._idList, i = e._nameDimIdx, a = e._idDimIdx, o = n[t], s = r[t];
			if (o == null && i != null && (n[t] = o = ES(e, i, t)), s == null && a != null && (r[t] = s = ES(e, a, t)), s == null && o != null) {
				var c = e._nameRepeatCount, l = c[o] = (c[o] || 0) + 1;
				s = o, l > 1 && (s += "__ec__" + l), r[t] = s;
			}
		};
	}(), e;
}();
//#endregion
//#region node_modules/echarts/lib/data/helper/createDimensions.js
function MS(e, t) {
	gg(e) || (e = vg(e)), t ||= {};
	var n = t.coordDimensions || [], r = t.dimensionsDefine || e.dimensionsDefine || [], i = K(), a = [], o = NS(e, n, r, t.dimensionsCount), s = t.canOmitUnusedDimensions && gS(o), c = r === e.dimensionsDefine, l = c ? hS(e) : mS(r), u = t.encodeDefine;
	!u && t.encodeDefaulter && (u = t.encodeDefaulter(e, o));
	for (var d = K(u), f = new h_(o), p = 0; p < f.length; p++) f[p] = -1;
	function m(e) {
		var t = f[e];
		if (t < 0) {
			var n = r[e], i = W(n) ? n : { name: n }, o = new lS(), s = i.name;
			return s != null && l.get(s) != null && (o.name = o.displayName = s), i.type != null && (o.type = i.type), i.displayName != null && (o.displayName = i.displayName), f[e] = a.length, o.storeDimIndex = e, a.push(o), o;
		}
		return a[t];
	}
	if (!s) for (var p = 0; p < o; p++) m(p);
	d.each(function(e, t) {
		var n = mo(e).slice();
		if (n.length === 1 && !H(n[0]) && n[0] < 0) d.set(t, !1);
		else {
			var r = d.set(t, []);
			F(n, function(e, n) {
				var i = H(e) ? l.get(e) : e;
				i != null && i < o && (r[n] = i, g(m(i), t, n));
			});
		}
	});
	var h = 0;
	F(n, function(e) {
		var t, n, r, i;
		if (H(e)) t = e, i = {};
		else {
			i = e, t = i.name;
			var a = i.ordinalMeta;
			i.ordinalMeta = null, i = j({}, i), i.ordinalMeta = a, n = i.dimsDef, r = i.otherDims, i.name = i.coordDim = i.coordDimIndex = i.dimsDef = i.otherDims = null;
		}
		var s = d.get(t);
		if (s !== !1) {
			if (s = mo(s), !s.length) for (var l = 0; l < (n && n.length || 1); l++) {
				for (; h < o && m(h).coordDim != null;) h++;
				h < o && s.push(h++);
			}
			F(s, function(e, a) {
				var o = m(e);
				if (c && i.type != null && (o.type = i.type), g(M(o, i), t, a), o.name == null && n) {
					var s = n[a];
					!W(s) && (s = { name: s }), o.name = o.displayName = s.name, o.defaultTooltip = s.defaultTooltip;
				}
				r && M(o.otherDims, r);
			});
		}
	});
	function g(e, t, n) {
		Nl.get(t) == null ? (e.coordDim = t, e.coordDimIndex = n, i.set(t, !0)) : e.otherDims[t] = n;
	}
	var _ = t.generateCoord, v = t.generateCoordCount, y = v != null;
	v = _ ? v || 1 : 0;
	var b = _ || "value";
	function x(e) {
		e.name ??= e.coordDim;
	}
	if (s) F(a, function(e) {
		x(e);
	}), a.sort(function(e, t) {
		return e.storeDimIndex - t.storeDimIndex;
	});
	else for (var S = 0; S < o; S++) {
		var C = m(S);
		C.coordDim ?? (C.coordDim = PS(b, i, y), C.coordDimIndex = 0, (!_ || v <= 0) && (C.isExtraCoord = !0), v--), x(C), C.type == null && (gh(e, S) === uh.Must || C.isExtraCoord && (C.otherDims.itemName != null || C.otherDims.seriesName != null)) && (C.type = "ordinal");
	}
	return Ko(a, function(e) {
		return e.name;
	}, function(e, t) {
		t > 0 && (e.name += t - 1);
	}), new fS({
		source: e,
		dimensions: a,
		fullDimensionCount: o,
		dimensionOmitted: s
	});
}
function NS(e, t, n, r) {
	var i = Math.max(e.dimensionsDetectedCount || 1, t.length, n.length, r || 0);
	return F(t, function(e) {
		var t;
		W(e) && (t = e.dimsDef) && (i = Math.max(i, t.length));
	}), i;
}
function PS(e, t, n) {
	if (n || t.hasKey(e)) {
		for (var r = 0; t.hasKey(e + r);) r++;
		e += r;
	}
	return t.set(e, !0), e;
}
//#endregion
//#region node_modules/echarts/lib/model/referHelper.js
var FS = function() {
	function e(e) {
		this.coordSysDims = [], this.axisMap = K(), this.categoryAxisMap = K(), this.coordSysName = e;
	}
	return e;
}();
function IS(e) {
	var t = e.get("coordinateSystem"), n = new FS(t), r = LS[t];
	if (r) return r(e, n, n.axisMap, n.categoryAxisMap), n;
}
var LS = {
	cartesian2d: function(e, t, n, r) {
		var i = e.getReferringComponents("xAxis", Lo).models[0], a = e.getReferringComponents("yAxis", Lo).models[0];
		t.coordSysDims = ["x", "y"], n.set("x", i), n.set("y", a), RS(i) && (r.set("x", i), t.firstCategoryDimIndex = 0), RS(a) && (r.set("y", a), t.firstCategoryDimIndex ??= 1);
	},
	singleAxis: function(e, t, n, r) {
		var i = e.getReferringComponents("singleAxis", Lo).models[0];
		t.coordSysDims = ["single"], n.set("single", i), RS(i) && (r.set("single", i), t.firstCategoryDimIndex = 0);
	},
	polar: function(e, t, n, r) {
		var i = e.getReferringComponents("polar", Lo).models[0], a = i.findAxisModel("radiusAxis"), o = i.findAxisModel("angleAxis");
		t.coordSysDims = ["radius", "angle"], n.set("radius", a), n.set("angle", o), RS(a) && (r.set("radius", a), t.firstCategoryDimIndex = 0), RS(o) && (r.set("angle", o), t.firstCategoryDimIndex ??= 1);
	},
	geo: function(e, t, n, r) {
		t.coordSysDims = ["lng", "lat"];
	},
	parallel: function(e, t, n, r) {
		var i = e.ecModel, a = i.getComponent("parallel", e.get("parallelIndex")), o = t.coordSysDims = a.dimensions.slice();
		F(a.parallelAxisIndex, function(e, a) {
			var s = i.getComponent("parallelAxis", e), c = o[a];
			n.set(c, s), RS(s) && (r.set(c, s), t.firstCategoryDimIndex ??= a);
		});
	},
	matrix: function(e, t, n, r) {
		var i = e.getReferringComponents("matrix", Lo).models[0];
		t.coordSysDims = ["x", "y"];
		var a = i.getDimensionModel("x"), o = i.getDimensionModel("y");
		n.set("x", a), n.set("y", o), r.set("x", a), r.set("y", o);
	}
};
function RS(e) {
	return e.get("type") === "category";
}
//#endregion
//#region node_modules/echarts/lib/data/helper/dataStackHelper.js
function zS(e, t, n) {
	n ||= {};
	var r = n.byIndex, i = n.stackedCoordDimension, a, o, s;
	BS(t) ? a = t : (o = t.schema, a = o.dimensions, s = t.store);
	var c = !!(e && e.get("stack")), l, u, d, f, p = !0;
	function m(e) {
		return e.type !== "ordinal" && e.type !== "time";
	}
	if (F(a, function(e, t) {
		H(e) && (a[t] = e = { name: e }), m(e) || (p = !1);
	}), F(a, function(e, t) {
		c && !e.isExtraCoord && (!r && !l && e.ordinalMeta && (l = e), !u && m(e) && (!p || e.coordDim !== "x" && e.coordDim !== "angle") && (!i || i === e.coordDim) && (u = e));
	}), u && !r && !l && (r = !0), u) {
		d = "__\0ecstackresult_" + e.id, f = "__\0ecstackedover_" + e.id, l && (l.createInvertedIndices = !0);
		var h = u.coordDim, g = u.type, _ = 0;
		F(a, function(e) {
			e.coordDim === h && _++;
		});
		var v = {
			name: d,
			coordDim: h,
			coordDimIndex: _,
			type: g,
			isExtraCoord: !0,
			isCalculationCoord: !0,
			storeDimIndex: a.length
		}, y = {
			name: f,
			coordDim: f,
			coordDimIndex: _ + 1,
			type: g,
			isExtraCoord: !0,
			isCalculationCoord: !0,
			storeDimIndex: a.length + 1
		};
		o ? (s && (v.storeDimIndex = s.ensureCalculationDimension(f, g), y.storeDimIndex = s.ensureCalculationDimension(d, g)), o.appendCalculationDimension(v), o.appendCalculationDimension(y)) : (a.push(v), a.push(y));
	}
	return {
		stackedDimension: u && u.name,
		stackedByDimension: l && l.name,
		isStackedByIndex: r,
		stackedOverDimension: f,
		stackResultDimension: d
	};
}
function BS(e) {
	return !pS(e.schema);
}
//#endregion
//#region node_modules/echarts/lib/chart/helper/createSeriesData.js
function VS(e, t) {
	var n = e.get("coordinateSystem"), r = Pm.get(n), i;
	return t && t.coordSysDims && (i = I(t.coordSysDims, function(e) {
		var n = { name: e }, r = t.axisMap.get(e);
		return r && (n.type = sS(r.get("type"))), n;
	})), i ||= r && (r.getDimensionsInfo ? r.getDimensionsInfo() : r.dimensions.slice()) || ["x", "y"], i;
}
function HS(e, t, n) {
	var r, i;
	return n && F(e, function(e, a) {
		var o = e.coordDim, s = n.categoryAxisMap.get(o);
		s && (r ??= a, e.ordinalMeta = s.getOrdinalMeta(), t && (e.createInvertedIndices = !0)), e.otherDims.itemName != null && (i = !0);
	}), !i && r != null && (e[r].otherDims.itemName = 0), r;
}
function US(e, t, n) {
	n ||= {};
	var r = t.getSourceManager(), i, a = !1;
	e ? (a = !0, i = vg(e)) : (i = r.getSource(), a = i.sourceFormat === Pl);
	var o = IS(t), s = VS(t, o), c = n.useEncodeDefaulter, l = V(c) ? c : c ? z(ph, s, t) : null, u = {
		coordDimensions: s,
		generateCoord: n.generateCoord,
		encodeDefine: t.getEncode(),
		encodeDefaulter: l,
		canOmitUnusedDimensions: !a
	}, d = MS(i, u), f = HS(d.dimensions, n.createInvertedIndices, o), p = a ? null : r.getSharedDataStore(d), m = zS(t, {
		schema: d,
		store: p
	}), h = new jS(d, t);
	h.setCalculationInfo(m);
	var g = f != null && WS(i) ? function(e, t, n, r) {
		return r === f ? n : this.defaultDimValueGetter(e, t, n, r);
	} : null;
	return h.hasItemOption = !1, h.initData(a ? i : p, null, g), h;
}
function WS(e) {
	if (e.sourceFormat === "original") return !B(_o(GS(e.data || [])));
}
function GS(e) {
	for (var t = 0; t < e.length && e[t] == null;) t++;
	return e[t];
}
L({
	needTransform: 1,
	normalize: 1,
	scale: 1,
	transformIn: 1,
	transformOut: 1,
	contain: 1,
	getExtent: 1,
	getExtentUnsafe: 1,
	setExtent: 1,
	setExtent2: 1,
	getFilter: 1,
	sanitize: 1,
	getDefaultStartValue: 1,
	freeze: 1
});
function KS(e, t) {
	return e.getExtentUnsafe(1, t) || e.getExtentUnsafe(0, t);
}
function qS(e) {
	var t = KS(e, 3);
	return t[1] - t[0];
}
//#endregion
//#region node_modules/echarts/lib/scale/helper.js
function JS(e) {
	return e.type === "time";
}
function YS(e) {
	return e.type === "ordinal";
}
Y();
function XS(e, t) {
	var n = e.scale;
	return YS(n) ? n.getLabel(t) : t.value;
}
function ZS(e) {
	return e.get("interval") ?? "auto";
}
function QS(e) {
	return e.type === "category" && ZS(e.getLabelModel()) === 0;
}
function $S(e) {
	return e === "middle" || e === "center";
}
function eC(e, t) {
	return YS(e) ? e.getRawOrdinalNumber(t.value) : t.value;
}
Wo();
var tC = Y();
Y();
function nC(e, t) {
	var n = e.model, r = tC(_y(n.ecModel)).keyed, i = r && r.get(t);
	return i && i.get(n.uid);
}
function rC(e, t) {
	return oC(nC(e, t));
}
function iC(e, t) {
	var n = [];
	return aC(e.model.ecModel, function(e) {
		for (var r = 0; r < t.length; r++) t[r] && e.serByIdx[t[r].seriesIndex] && n.push(oC(e));
	}), n;
}
function aC(e, t) {
	var n = tC(_y(e)).keyed;
	n && n.each(function(e, n) {
		e.each(function(e, r) {
			t(e, n, r);
		});
	});
}
function oC(e) {
	return { liPosMinGap: e ? e.liPosMinGap : void 0 };
}
K();
//#endregion
//#region node_modules/echarts/lib/extension.js
var sC = [], cC = {
	registerPreprocessor: Bx,
	registerProcessor: Vx,
	registerPostInit: Hx,
	registerPostUpdate: Ux,
	registerUpdateLifecycle: Wx,
	registerAction: Gx,
	registerCoordinateSystem: Kx,
	registerLayout: qx,
	registerVisual: Jx,
	registerTransform: $x,
	registerLoading: Zx,
	registerMap: Qx,
	registerImpl: uy,
	PRIORITY: Vb,
	ComponentModel: eh,
	ComponentView: ov,
	SeriesModel: Z_,
	ChartView: uv,
	registerComponentModel: function(e) {
		eh.registerClass(e);
	},
	registerComponentView: function(e) {
		ov.registerClass(e);
	},
	registerSeriesModel: function(e) {
		Z_.registerClass(e);
	},
	registerChartView: function(e) {
		uv.registerClass(e);
	},
	registerCustomSeries: function(e, t) {
		py(e, t);
	},
	registerSubTypeDefaulter: function(e, t) {
		eh.registerSubTypeDefaulter(e, t);
	},
	registerPainter: function(e, t) {
		Da(e, t);
	}
};
function lC(e) {
	B(e) ? F(e, function(e) {
		lC(e);
	}) : N(sC, e) >= 0 || (sC.push(e), V(e) && (e = { install: e }), e.install(cC));
}
Y(), Y();
var uC = {
	estimate: 1,
	determine: 2
};
function dC(e) {
	return {
		out: { noPxChangeTryDetermine: [] },
		kind: e
	};
}
//#endregion
//#region node_modules/echarts/lib/coord/axisBand.js
var fC = .8;
function pC(e, t) {
	t ||= {};
	var n = {
		w: NaN,
		w2: NaN
	}, r = e.scale, i = t.fromStat, a = t.min, o = qS(r);
	io(o) || (o = NaN);
	var s = e.getExtent(), c = Na(s[1] - s[0]);
	return YS(r) ? mC(n, e, o, c) : i && hC(n, e, o, c, i), a != null && (n.w = io(n.w) ? Ma(a, n.w) : a), n;
}
function mC(e, t, n, r) {
	var i = t.onBand, a = n + +!!i;
	a === 0 && (a = 1), e.w = r / a, !i && n && r && (e.w2 = e.w * n / r);
}
function hC(e, t, n, r, i) {
	var a = !1, o = -Infinity;
	F(i.key ? [rC(t, i.key)] : iC(t, i.sers || []), function(e) {
		var t = e.liPosMinGap;
		t != null && (t > 0 ? (t > o && (o = t), a = !1) : t === -2 && (a = !0));
	}), io(n) && n > 0 && io(o) ? (e.w = r / n * o, e.w2 = o) : a && (e.w = r * fC, e.w2 = e.w * n / r);
}
//#endregion
//#region node_modules/echarts/lib/label/labelLayoutHelper.js
var gC = [
	"label",
	"labelLine",
	"layoutOption",
	"priority",
	"defaultAttr",
	"marginForce",
	"minMarginForce",
	"marginDefault",
	"suggestIgnore"
], _C = 1, vC = 2, yC = _C | vC;
function bC(e, t, n) {
	n ||= yC, t ? e.dirty |= n : e.dirty &= ~n;
}
function xC(e, t) {
	return t ||= yC, e.dirty == null || !!(e.dirty & t);
}
function SC(e) {
	if (e) return xC(e) && CC(e, e.label, e), e;
}
function CC(e, t, n) {
	var r = t.getComputedTransform();
	e.transform = dp(e.transform, r);
	var i = e.localRect = up(e.localRect, t.getBoundingRect()), a = t.style, o = a.margin, s = n && n.marginForce, c = n && n.minMarginForce, l = n && n.marginDefault, u = a.__marginType;
	u == null && l && (o = l, u = Fp.textMargin);
	for (var d = 0; d < 4; d++) wC[d] = u === Fp.minMargin && c && c[d] != null ? c[d] : s && s[d] != null ? s[d] : o ? o[d] : 0;
	u === Fp.textMargin && np(i, wC, !1, !1);
	var f = e.rect = up(e.rect, i);
	return r && f.applyTransform(r), u === Fp.minMargin && np(f, wC, !1, !1), e.axisAligned = cp(r), (e.label = e.label || {}).ignore = t.ignore, bC(e, !1), bC(e, !0, vC), e;
}
var wC = [
	0,
	0,
	0,
	0
];
function TC(e, t, n) {
	return e.transform = dp(e.transform, n), e.localRect = up(e.localRect, t), e.rect = up(e.rect, t), n && e.rect.applyTransform(n), e.axisAligned = cp(n), e.obb = void 0, (e.label = e.label || {}).ignore = !1, e;
}
function EC(e, t) {
	if (e) {
		e.label.x += t.x, e.label.y += t.y, e.label.markRedraw();
		var n = e.transform;
		n && (n[4] += t.x, n[5] += t.y);
		var r = e.rect;
		r && (r.x += t.x, r.y += t.y);
		var i = e.obb;
		i && i.fromBoundingRect(e.localRect, n);
	}
}
function DC(e, t) {
	for (var n = 0; n < gC.length; n++) {
		var r = gC[n];
		e[r] ?? (e[r] = t[r]);
	}
	return SC(e);
}
function OC(e) {
	var t = e.obb;
	return (!t || xC(e, vC)) && (e.obb = t ||= new pf(), t.fromBoundingRect(e.localRect, e.transform), bC(e, !1, vC)), t;
}
function kC(e) {
	var t = [];
	e.sort(function(e, t) {
		return +!!t.suggestIgnore - !!e.suggestIgnore || t.priority - e.priority;
	});
	function n(e) {
		if (!e.ignore) {
			var t = e.ensureState("emphasis");
			t.ignore ??= !1;
		}
		e.ignore = !0;
	}
	for (var r = 0; r < e.length; r++) {
		var i = SC(e[r]);
		if (!i.label.ignore) {
			for (var a = i.label, o = i.labelLine, s = !1, c = 0; c < t.length; c++) if (AC(i, t[c], null, { touchThreshold: .05 })) {
				s = !0;
				break;
			}
			s ? (n(a), o && n(o)) : t.push(i);
		}
	}
}
function AC(e, t, n, r) {
	return !e || !t || e.label && e.label.ignore || t.label && t.label.ignore || !e.rect.intersect(t.rect, n, r) ? !1 : e.axisAligned && t.axisAligned ? !0 : OC(e).intersect(OC(t), n, r);
}
//#endregion
//#region node_modules/echarts/lib/chart/helper/labelHelper.js
function jC(e, t) {
	var n = e.mapDimensionsAll("defaultedLabel"), r = n.length;
	if (r === 1) {
		var i = Wg(e, t, n[0]);
		return i == null ? null : i + "";
	}
	if (r) {
		for (var a = [], o = 0; o < n.length; o++) a.push(Wg(e, t, n[o]));
		return a.join(" ");
	}
}
//#endregion
//#region node_modules/echarts/lib/chart/helper/Symbol.js
var MC = function(e) {
	r(t, e);
	function t(t, n, r, i) {
		var a = e.call(this) || this;
		return a.updateData(t, n, r, i), a;
	}
	return t.prototype._createSymbol = function(e, t, n, r, i, a) {
		this.removeAll();
		var o = Dy(e, -1, -1, 2, 2, null, a);
		o.attr({
			z2: G(i, 100),
			culling: !0,
			scaleX: r[0] / 2,
			scaleY: r[1] / 2
		}), o.drift = NC, this._symbolType = e, this.add(o);
	}, t.prototype.stopSymbolAnimation = function(e) {
		this.childAt(0).stopAnimation(null, e);
	}, t.prototype.getSymbolType = function() {
		return this._symbolType;
	}, t.prototype.getSymbolPath = function() {
		return this.childAt(0);
	}, t.prototype.highlight = function() {
		xu(this.childAt(0));
	}, t.prototype.downplay = function() {
		Su(this.childAt(0));
	}, t.prototype.setZ = function(e, t) {
		var n = this.childAt(0);
		n.zlevel = e, n.z = t;
	}, t.prototype.setDraggable = function(e, t) {
		var n = this.childAt(0);
		n.draggable = e, n.cursor = !t && e ? "move" : n.cursor;
	}, t.prototype.updateData = function(e, n, r, i) {
		this.silent = !1;
		var a = e.getItemVisual(n, "symbol") || "circle", o = e.hostModel, s = t.getSymbolSize(e, n), c = t.getSymbolZ2(e, n), l = a !== this._symbolType, u = i && i.disableAnimation;
		if (l) {
			var d = e.getItemVisual(n, "symbolKeepAspect");
			this._createSymbol(a, e, n, s, c, d);
		} else {
			var f = this.childAt(0);
			f.silent = !1;
			var p = {
				scaleX: s[0] / 2,
				scaleY: s[1] / 2
			};
			u ? f.attr(p) : yf(f, p, o, n), Tf(f);
		}
		if (this._updateCommon(e, n, s, r, i), l) {
			var f = this.childAt(0);
			if (!u) {
				var p = {
					scaleX: this._sizeX,
					scaleY: this._sizeY,
					style: { opacity: f.style.opacity }
				};
				f.scaleX = f.scaleY = 0, f.style.opacity = 0, bf(f, p, o, n);
			}
		}
		u && this.childAt(0).stopAnimation("leave");
	}, t.prototype._updateCommon = function(e, t, n, r, i) {
		var a = this.childAt(0), o = e.hostModel, s, c, l, u, d, f, p, m, h;
		if (r && (s = r.emphasisItemStyle, c = r.blurItemStyle, l = r.selectItemStyle, u = r.focus, d = r.blurScope, p = r.labelStatesModels, m = r.hoverScale, h = r.cursorStyle, f = r.emphasisDisabled), !r || e.hasItemOption) {
			var g = r && r.itemModel ? r.itemModel : e.getItemModel(t), _ = g.getModel("emphasis");
			s = _.getModel("itemStyle").getItemStyle(), l = g.getModel(["select", "itemStyle"]).getItemStyle(), c = g.getModel(["blur", "itemStyle"]).getItemStyle(), u = _.get("focus"), d = _.get("blurScope"), f = _.get("disabled"), p = wp(g), m = _.getShallow("scale"), h = g.getShallow("cursor");
		}
		var v = e.getItemVisual(t, "symbolRotate");
		a.attr("rotation", (v || 0) * Math.PI / 180 || 0);
		var y = ky(e.getItemVisual(t, "symbolOffset"), n);
		y && (a.x = y[0], a.y = y[1]), h && a.attr("cursor", h);
		var b = e.getItemVisual(t, "style"), x = b.fill;
		if (a instanceof rl) {
			var S = a.style;
			a.useStyle(j({
				image: S.image,
				x: S.x,
				y: S.y,
				width: S.width,
				height: S.height
			}, b));
		} else a.__isEmptyBrush ? a.useStyle(j({}, b)) : a.useStyle(b), a.style.decal = null, a.setColor(x, i && i.symbolInnerColor), a.style.strokeNoScale = !0;
		var C = e.getItemVisual(t, "liftZ"), w = this._z2;
		C == null ? w != null && (a.z2 = w, this._z2 = null) : w ?? (this._z2 = a.z2, a.z2 += C);
		var T = i && i.useNameLabel;
		Cp(a, p, {
			labelFetcher: o,
			labelDataIndex: t,
			defaultText: E,
			inheritColor: x,
			defaultOpacity: b.opacity
		});
		function E(t) {
			return T ? e.getName(t) : jC(e, t);
		}
		this._sizeX = n[0] / 2, this._sizeY = n[1] / 2;
		var D = a.ensureState("emphasis");
		D.style = s, a.ensureState("select").style = l, a.ensureState("blur").style = c;
		var O = m == null || m === !0 ? Math.max(1.1, 3 / this._sizeY) : isFinite(m) && m > 0 ? +m : 1;
		D.scaleX = this._sizeX * O, D.scaleY = this._sizeY * O, this.setSymbolScale(1), Bu(this, u, d, f);
	}, t.prototype.setSymbolScale = function(e) {
		this.scaleX = this.scaleY = e;
	}, t.prototype.fadeOut = function(e, t, n) {
		var r = this.childAt(0), i = Al(this).dataIndex, a = n && n.animation;
		if (this.silent = r.silent = !0, n && n.fadeLabel) {
			var o = r.getTextContent();
			o && Sf(o, { style: { opacity: 0 } }, t, {
				dataIndex: i,
				removeOpt: a,
				cb: function() {
					r.removeTextContent();
				}
			});
		} else r.removeTextContent();
		Sf(r, {
			style: { opacity: 0 },
			scaleX: 0,
			scaleY: 0
		}, t, {
			dataIndex: i,
			cb: e,
			removeOpt: a
		});
	}, t.getSymbolSize = function(e, t) {
		return Oy(e.getItemVisual(t, "symbolSize"));
	}, t.getSymbolZ2 = function(e, t) {
		return e.getItemVisual(t, "z2");
	}, t;
}(ba);
function NC(e, t) {
	this.parent.drift(e, t);
}
//#endregion
//#region node_modules/echarts/lib/chart/helper/SymbolDraw.js
function PC(e, t, n, r) {
	return t && !isNaN(t[0]) && !isNaN(t[1]) && !(r && r.isIgnore && r.isIgnore(n)) && !(r && r.clipShape && !r.clipShape.contain(t[0], t[1])) && e.getItemVisual(n, "symbol") !== "none";
}
function FC(e) {
	return e != null && !W(e) && (e = { isIgnore: e }), e || {};
}
function IC(e) {
	var t = e.hostModel, n = t.getModel("emphasis");
	return {
		emphasisItemStyle: n.getModel("itemStyle").getItemStyle(),
		blurItemStyle: t.getModel(["blur", "itemStyle"]).getItemStyle(),
		selectItemStyle: t.getModel(["select", "itemStyle"]).getItemStyle(),
		focus: n.get("focus"),
		blurScope: n.get("blurScope"),
		emphasisDisabled: n.get("disabled"),
		hoverScale: n.get("scale"),
		labelStatesModels: wp(t),
		cursorStyle: t.get("cursor")
	};
}
function LC(e, t, n, r, i, a, o) {
	var s = new e(t, n, r, i);
	return s.setPosition(a), t.setItemGraphicEl(n, s), o.add(s), s;
}
var RC = function() {
	function e(e) {
		this.group = new ba(), this._SymbolCtor = e || MC;
	}
	return e.prototype.updateData = function(e, t) {
		this._progressiveEls = null, t = FC(t);
		var n = this.group, r = e.hostModel, i = this._data, a = this._SymbolCtor, o = t.disableAnimation, s = this._seriesScope = IC(e), c = { disableAnimation: o }, l = t.getSymbolPoint || function(t) {
			return e.getItemLayout(t);
		};
		i || n.removeAll(), e.diff(i).add(function(r) {
			var i = l(r);
			PC(e, i, r, t) && LC(a, e, r, s, c, i, n);
		}).update(function(u, d) {
			var f = i.getItemGraphicEl(d), p = l(u);
			if (!PC(e, p, u, t)) n.remove(f);
			else {
				var m = e.getItemVisual(u, "symbol") || "circle", h = f && f.getSymbolType && f.getSymbolType();
				if (!f || h && h !== m) n.remove(f), f = new a(e, u, s, c), f.setPosition(p);
				else {
					f.updateData(e, u, s, c);
					var g = {
						x: p[0],
						y: p[1]
					};
					o ? f.attr(g) : yf(f, g, r);
				}
				n.add(f), e.setItemGraphicEl(u, f);
			}
		}).remove(function(e) {
			var t = i.getItemGraphicEl(e);
			t && t.fadeOut(function() {
				n.remove(t);
			}, r);
		}).execute(), this._getSymbolPoint = l, this._data = e;
	}, e.prototype.updateLayout = function(e) {
		var t = this._data;
		if (t) for (var n = this, r = t.getStore(), i = 0, a = r.count(); i < a; i++) {
			var o = t.getItemGraphicEl(i), s = n._getSymbolPoint(i);
			PC(t, s, i, e) ? (o ||= LC(n._SymbolCtor, t, i, n._seriesScope, { disableAnimation: !0 }, s, n.group), o.stopAnimation(), o.setPosition(s), o.markRedraw()) : o && (n.group.remove(o), t.setItemGraphicEl(i, null));
		}
	}, e.prototype.incrementalPrepareUpdate = function(e) {
		this._seriesScope = IC(e), this._data = null, this.group.removeAll();
	}, e.prototype.incrementalUpdate = function(e, t, n, r) {
		this._progressiveEls = [], r = FC(r);
		function i(e) {
			e.isGroup || (e.incremental = n, e.ensureState("emphasis").hoverLayer = 2);
		}
		for (var a = e.start; a < e.end; a++) {
			var o = t.getItemLayout(a);
			if (PC(t, o, a, r)) {
				var s = new this._SymbolCtor(t, a, this._seriesScope);
				s.traverse(i), s.setPosition(o), this.group.add(s), t.setItemGraphicEl(a, s), this._progressiveEls.push(s);
			}
		}
	}, e.prototype.eachRendered = function(e) {
		sp(this._progressiveEls || this.group, e);
	}, e.prototype.remove = function(e) {
		var t = this.group, n = this._data;
		n && e ? n.eachItemGraphicEl(function(e) {
			e.fadeOut(function() {
				t.remove(e);
			}, n.hostModel);
		}) : t.removeAll();
	}, e;
}(), zC = null;
function BC() {
	return zC;
}
//#endregion
//#region node_modules/echarts/lib/component/axis/axisAction.js
var VC = "expandAxisBreak", HC = Math.PI, UC = [
	[
		1,
		2,
		1,
		2
	],
	[
		5,
		3,
		5,
		3
	],
	[
		8,
		3,
		8,
		3
	]
], WC = [
	[
		0,
		1,
		0,
		1
	],
	[
		0,
		3,
		0,
		3
	],
	[
		0,
		3,
		0,
		3
	]
], GC = Y(), KC = Y(), qC = function() {
	function e(e) {
		this.recordMap = {}, this.resolveAxisNameOverlap = e;
	}
	return e.prototype.ensureRecord = function(e) {
		var t = e.axis.dim, n = e.componentIndex, r = this.recordMap, i = r[t] || (r[t] = []);
		return i[n] || (i[n] = { ready: {} });
	}, e;
}();
function JC(e, t, n, r) {
	var i = n.axis, a = t.ensureRecord(n), o = [], s, c = vw(e.axisName) && $S(e.nameLocation);
	F(r, function(e) {
		var t = SC(e);
		if (t && !t.label.ignore) {
			o.push(t);
			var n = a.transGroup;
			c && (n.transform ? It(YC, n.transform) : At(YC), t.transform && Mt(YC, YC, t.transform), J.copy(XC, t.localRect), XC.applyTransform(YC), s ? s.union(XC) : J.copy(s = new J(0, 0, 0, 0), XC));
		}
	});
	var l = Math.abs(a.dirVec.x) > .1 ? "x" : "y", u = a.transGroup[l];
	if (o.sort(function(e, t) {
		return Math.abs(e.label[l] - u) - Math.abs(t.label[l] - u);
	}), c && s) {
		var d = i.getExtent(), f = Math.min(d[0], d[1]), p = Math.max(d[0], d[1]) - f;
		s.union(new J(f, 0, p, 1));
	}
	a.stOccupiedRect = s, a.labelInfoList = o;
}
var YC = kt(), XC = new J(0, 0, 0, 0), ZC = function(e, t, n, r, i, a) {
	if ($S(e.nameLocation)) {
		var o = a.stOccupiedRect;
		o && QC(TC({}, o, a.transGroup.transform), r, i);
	} else $C(a.labelInfoList, a.dirVec, r, i);
};
function QC(e, t, n) {
	var r = new q();
	AC(e, t, r, {
		direction: Math.atan2(n.y, n.x),
		bidirectional: !1,
		touchThreshold: .05
	}) && EC(t, r);
}
function $C(e, t, n, r) {
	for (var i = q.dot(r, t) >= 0, a = 0, o = e.length; a < o; a++) {
		var s = e[i ? a : o - 1 - a];
		s.label.ignore || QC(s, n, r);
	}
}
var ew = function() {
	function e(e, t, n, r) {
		this.group = new ba(), this._axisModel = e, this._api = t, this._local = {}, this._shared = r || new qC(ZC), this._resetCfgDetermined(n);
	}
	return e.prototype.updateCfg = function(e) {
		var t = this._cfg.raw;
		t.position = e.position, t.labelOffset = e.labelOffset, this._resetCfgDetermined(t);
	}, e.prototype.__getRawCfg = function() {
		return this._cfg.raw;
	}, e.prototype._resetCfgDetermined = function(e) {
		var t = this._axisModel, n = t.getDefaultOption ? t.getDefaultOption() : {}, r = G(e.axisName, t.get("name")), i = t.get("nameMoveOverlap");
		(i == null || i === "auto") && (i = G(e.defaultNameMoveOverlap, !0));
		var a = {
			raw: e,
			position: e.position,
			rotation: e.rotation,
			nameDirection: G(e.nameDirection, 1),
			tickDirection: G(e.tickDirection, 1),
			labelDirection: G(e.labelDirection, 1),
			labelOffset: G(e.labelOffset, 0),
			silent: G(e.silent, !0),
			axisName: r,
			nameLocation: me(t.get("nameLocation"), n.nameLocation, "end"),
			shouldNameMoveOverlap: vw(r) && i,
			optionHideOverlap: t.get(["axisLabel", "hideOverlap"]),
			showMinorTicks: t.get(["minorTick", "show"])
		};
		this._cfg = a;
		var o = new ba({
			x: a.position[0],
			y: a.position[1],
			rotation: a.rotation
		});
		o.updateTransform(), this._transformGroup = o;
		var s = this._shared.ensureRecord(t);
		s.transGroup = this._transformGroup, s.dirVec = new q(Math.cos(-a.rotation), Math.sin(-a.rotation));
	}, e.prototype.build = function(e, t) {
		var n = this;
		return e ||= {
			axisLine: !0,
			axisTickLabelEstimate: !1,
			axisTickLabelDetermine: !0,
			axisName: !0
		}, F(tw, function(r) {
			e[r] && nw[r](n._cfg, n._local, n._shared, n._axisModel, n.group, n._transformGroup, n._api, t || {});
		}), this;
	}, e.innerTextLayout = function(e, t, n) {
		var r = Ya(t - e), i, a;
		return Xa(r) ? (a = n > 0 ? "top" : "bottom", i = "center") : Xa(r - HC) ? (a = n > 0 ? "bottom" : "top", i = "center") : (a = "middle", i = r > 0 && r < HC ? n > 0 ? "right" : "left" : n > 0 ? "left" : "right"), {
			rotation: r,
			textAlign: i,
			textVerticalAlign: a
		};
	}, e.makeAxisEventDataBase = function(e) {
		var t = {
			componentType: e.mainType,
			componentIndex: e.componentIndex
		};
		return t[e.mainType + "Index"] = e.componentIndex, t;
	}, e.isLabelSilent = function(e) {
		var t = e.get("tooltip");
		return e.get("silent") || !(e.get("triggerEvent") || t && t.show);
	}, e;
}(), tw = [
	"axisLine",
	"axisTickLabelEstimate",
	"axisTickLabelDetermine",
	"axisName"
], nw = {
	axisLine: function(e, t, n, r, i, a, o) {
		var s = r.get(["axisLine", "show"]);
		if (s === "auto" && (s = !0, e.raw.axisLineAutoShow != null && (s = !!e.raw.axisLineAutoShow)), s) {
			var c = r.axis.getExtent(), l = a.transform, u = [c[0], 0], d = [c[1], 0], f = u[0] > d[0];
			l && (qe(u, u, l), qe(d, d, l));
			var p = j({ lineCap: "round" }, r.getModel(["axisLine", "lineStyle"]).getLineStyle()), m = {
				strokeContainThreshold: e.raw.strokeContainThreshold || 5,
				silent: !0,
				z2: 1,
				style: p
			};
			if (r.get(["axisLine", "breakLine"]) && dm(r.axis.scale)) BC().buildAxisBreakLine(r, i, a, m);
			else {
				var h = new qd(j({ shape: {
					x1: u[0],
					y1: u[1],
					x2: d[0],
					y2: d[1]
				} }, m));
				Bf(h.shape, h.style.lineWidth), h.anid = "line", i.add(h);
			}
			var g = r.get(["axisLine", "symbol"]);
			if (g != null) {
				var _ = r.get(["axisLine", "symbolSize"]);
				H(g) && (g = [g, g]), (H(_) || U(_)) && (_ = [_, _]);
				var v = ky(r.get(["axisLine", "symbolOffset"]) || 0, _), y = _[0], b = _[1];
				F([{
					rotate: e.rotation + Math.PI / 2,
					offset: v[0],
					r: 0
				}, {
					rotate: e.rotation - Math.PI / 2,
					offset: v[1],
					r: Math.sqrt((u[0] - d[0]) * (u[0] - d[0]) + (u[1] - d[1]) * (u[1] - d[1]))
				}], function(t, n) {
					if (g[n] !== "none" && g[n] != null) {
						var r = Dy(g[n], -y / 2, -b / 2, y, b, p.stroke, !0), a = t.r + t.offset, o = f ? d : u;
						r.attr({
							rotation: t.rotate,
							x: o[0] + a * Math.cos(e.rotation),
							y: o[1] - a * Math.sin(e.rotation),
							silent: !0,
							z2: 11
						}), i.add(r);
					}
				});
			}
		}
	},
	axisTickLabelEstimate: function(e, t, n, r, i, a, o, s) {
		dw(t, i, s) && rw(e, t, n, r, i, a, o, uC.estimate);
	},
	axisTickLabelDetermine: function(e, t, n, r, i, a, o, s) {
		dw(t, i, s) && rw(e, t, n, r, i, a, o, uC.determine);
		var c = lw(e, i, a, r);
		ow(e, t.labelLayoutList, c), uw(e, i, a, r, e.tickDirection);
	},
	axisName: function(e, t, n, r, i, a, o, s) {
		var c = n.ensureRecord(r);
		t.nameEl &&= (i.remove(t.nameEl), c.nameLayout = c.nameLocation = null);
		var l = e.axisName;
		if (vw(l)) {
			var u = e.nameLocation, d = e.nameDirection, f = r.getModel("nameTextStyle"), p = r.get("nameGap") || 0, m = r.axis.getExtent(), h = r.axis.inverse ? -1 : 1, g = new q(0, 0), _ = new q(0, 0);
			u === "start" ? (g.x = m[0] - h * p, _.x = -h) : u === "end" ? (g.x = m[1] + h * p, _.x = h) : (g.x = (m[0] + m[1]) / 2, g.y = e.labelOffset + d * p, _.y = d);
			var v = kt();
			_.transform(Pt(v, v, e.rotation));
			var y = r.get("nameRotate");
			y != null && (y = y * HC / 180);
			var b, x;
			$S(u) ? b = ew.innerTextLayout(e.rotation, y ?? e.rotation, d) : (b = iw(e.rotation, u, y || 0, m), x = e.raw.axisNameAvailableWidth, x != null && (x = Math.abs(x / Math.sin(b.rotation)), !isFinite(x) && (x = null)));
			var S = f.getFont(), C = r.get("nameTruncate", !0) || {}, w = C.ellipsis, T = pe(e.raw.nameTruncateMaxWidth, C.maxWidth, x), E = s.nameMarginLevel || 0, D = new gl({
				x: g.x,
				y: g.y,
				rotation: b.rotation,
				silent: ew.isLabelSilent(r),
				style: Tp(f, {
					text: l,
					font: S,
					overflow: "truncate",
					width: T,
					ellipsis: w,
					fill: f.getTextColor() || r.get([
						"axisLine",
						"lineStyle",
						"color"
					]),
					align: f.get("align") || b.textAlign,
					verticalAlign: f.get("verticalAlign") || b.textVerticalAlign
				}),
				z2: 1
			});
			if (ap({
				el: D,
				componentModel: r,
				itemName: l
			}), D.__fullText = l, D.anid = "name", r.get("triggerEvent")) {
				var O = ew.makeAxisEventDataBase(r);
				O.targetType = "axisName", O.name = l, Al(D).eventData = O;
			}
			a.add(D), D.updateTransform(), t.nameEl = D;
			var k = c.nameLayout = SC({
				label: D,
				priority: D.z2,
				defaultAttr: { ignore: D.ignore },
				marginDefault: $S(u) ? UC[E] : WC[E]
			});
			if (c.nameLocation = u, i.add(D), D.decomposeTransform(), e.shouldNameMoveOverlap && k) {
				var A = n.ensureRecord(r);
				n.resolveAxisNameOverlap(e, n, r, k, _, A);
			}
		}
	}
};
function rw(e, t, n, r, i, a, o, s) {
	pw(t) || fw(e, t, i, s, r, o);
	var c = t.labelLayoutList;
	hw(e, r, c, a), bw(r, e.rotation, c);
	var l = e.optionHideOverlap;
	aw(r, c, l), l && kC(ie(c, function(e) {
		return e && !e.label.ignore;
	})), JC(e, n, r, c);
}
function iw(e, t, n, r) {
	var i = Ya(n - e), a, o, s = r[0] > r[1], c = t === "start" && !s || t !== "start" && s;
	return Xa(i - HC / 2) ? (o = c ? "bottom" : "top", a = "center") : Xa(i - HC * 1.5) ? (o = c ? "top" : "bottom", a = "center") : (o = "middle", a = i < HC * 1.5 && i > HC / 2 ? c ? "left" : "right" : c ? "right" : "left"), {
		rotation: i,
		textAlign: a,
		textVerticalAlign: o
	};
}
function aw(e, t, n) {
	var r = e.axis, i = e.get(["axisLabel", "customValues"]);
	if (QS(r)) return;
	function a(e, a, o) {
		var s = SC(t[a]), c = SC(t[o]), l = r.scale;
		if (s && c) {
			if (e == null) {
				if (!n && i) return;
				var u = GC(s.label).labelInfo.tick;
				if (JS(l) && u.notNice || YS(l) && u.offInterval) {
					sw(s.label);
					return;
				}
			}
			if (e === !1 || s.suggestIgnore) sw(s.label);
			else if (c.suggestIgnore) sw(c.label);
			else {
				var d = .1;
				if (!n) {
					var f = [
						0,
						0,
						0,
						0
					];
					s = DC({ marginForce: f }, s), c = DC({ marginForce: f }, c);
				}
				AC(s, c, null, { touchThreshold: d }) && sw(e ? c.label : s.label);
			}
		}
	}
	var o = e.get(["axisLabel", "showMinLabel"]), s = e.get(["axisLabel", "showMaxLabel"]), c = t.length;
	a(o, 0, 1), a(s, c - 1, c - 2);
}
function ow(e, t, n) {
	e.showMinorTicks || F(t, function(e) {
		if (e && e.label.ignore) for (var t = 0; t < n.length; t++) {
			var r = n[t], i = KC(r), a = GC(e.label);
			if (i.tickValue != null && !i.onBand && i.tickValue === a.labelInfo.tick.value) {
				sw(r);
				return;
			}
		}
	});
}
function sw(e) {
	e && (e.ignore = !0);
}
function cw(e, t, n, r, i) {
	for (var a = [], o = [], s = [], c = 0; c < e.length; c++) {
		var l = e[c].coord;
		o[0] = l, o[1] = 0, s[0] = l, s[1] = n, t && (qe(o, o, t), qe(s, s, t));
		var u = new qd({
			shape: {
				x1: o[0],
				y1: o[1],
				x2: s[0],
				y2: s[1]
			},
			style: r,
			z2: 2,
			autoBatch: !0,
			silent: !0
		});
		Bf(u.shape, u.style.lineWidth), u.anid = i + "_" + e[c].tickValue, a.push(u);
		var d = KC(u);
		d.onBand = !!e[c].onBand, d.tickValue = e[c].tickValue;
	}
	return a;
}
function lw(e, t, n, r) {
	var i = r.axis, a = r.getModel("axisTick"), o = a.get("show");
	if (o === "auto" && (o = !0, e.raw.axisTickAutoShow != null && (o = !!e.raw.axisTickAutoShow)), !o || i.scale.isBlank()) return [];
	for (var s = a.getModel("lineStyle"), c = e.tickDirection * a.get("length"), l = cw(i.getTicksCoords(), n.transform, c, M(s.getLineStyle(), { stroke: r.get([
		"axisLine",
		"lineStyle",
		"color"
	]) }), "ticks"), u = 0; u < l.length; u++) t.add(l[u]);
	return l;
}
function uw(e, t, n, r, i) {
	var a = r.axis, o = r.getModel("minorTick");
	if (e.showMinorTicks && !a.scale.isBlank()) {
		var s = a.getMinorTicksCoords();
		if (s.length) for (var c = o.getModel("lineStyle"), l = i * o.get("length"), u = M(c.getLineStyle(), M(r.getModel("axisTick").getLineStyle(), { stroke: r.get([
			"axisLine",
			"lineStyle",
			"color"
		]) })), d = 0; d < s.length; d++) for (var f = cw(s[d], n.transform, l, u, "minorticks_" + d), p = 0; p < f.length; p++) t.add(f[p]);
	}
}
function dw(e, t, n) {
	if (pw(e)) {
		var r = e.axisLabelsCreationContext.out.noPxChangeTryDetermine;
		if (n.noPxChange) {
			for (var i = !0, a = 0; a < r.length; a++) i &&= r[a]();
			if (i) return !1;
		}
		r.length && (t.remove(e.labelGroup), mw(e, null, null, null));
	}
	return !0;
}
function fw(e, t, n, r, i, a) {
	var o = i.axis, s = pe(e.raw.axisLabelShow, i.get(["axisLabel", "show"])), c = new ba();
	n.add(c);
	var l = dC(r);
	if (!s || o.scale.isBlank()) mw(t, [], c, l);
	else {
		var u = i.getModel("axisLabel"), d = o.getViewLabels(l), f = (pe(e.raw.labelRotate, u.get("rotate")) || 0) * HC / 180, p = ew.innerTextLayout(e.rotation, f, e.labelDirection), m = i.getCategories && i.getCategories(!0), h = [], g = i.get("triggerEvent"), _ = Infinity, v = -Infinity;
		F(d, function(e, t) {
			var n = e.tick, r = e.formattedLabel, s = e.rawLabel, l = u, f = eC(o.scale, n);
			if (m && m[f]) {
				var y = m[f];
				W(y) && y.textStyle && (l = new Kp(y.textStyle, u, i.ecModel));
			}
			var b = l.getTextColor() || i.get([
				"axisLine",
				"lineStyle",
				"color"
			]), x = l.getShallow("align", !0) || p.textAlign, S = G(l.getShallow("alignMinLabel", !0), x), C = G(l.getShallow("alignMaxLabel", !0), x), w = l.getShallow("verticalAlign", !0) || l.getShallow("baseline", !0) || p.textVerticalAlign, T = G(l.getShallow("verticalAlignMinLabel", !0), w), E = G(l.getShallow("verticalAlignMaxLabel", !0), w), D = 10 + (n.time?.level || 0);
			_ = Math.min(_, D), v = Math.max(v, D);
			var O = new gl({
				x: 0,
				y: 0,
				rotation: 0,
				silent: ew.isLabelSilent(i),
				z2: D,
				style: Tp(l, {
					text: r,
					align: t === 0 ? S : t === d.length - 1 ? C : x,
					verticalAlign: t === 0 ? T : t === d.length - 1 ? E : w,
					fill: V(b) ? b(o.type === "category" ? s : o.type === "value" ? f + "" : f, t) : b
				})
			});
			O.anid = "label_" + f;
			var k = GC(O);
			if (k.labelInfo = e, k.layoutRotation = p.rotation, ap({
				el: O,
				componentModel: i,
				itemName: r,
				formatterParamsExtra: {
					isTruncated: function() {
						return O.isTruncated;
					},
					value: s,
					tickIndex: t
				}
			}), g) {
				var A = ew.makeAxisEventDataBase(i);
				A.targetType = "axisLabel", A.value = s, A.tickIndex = t;
				var j = e.tick.break;
				if (j) {
					var ee = j.parsedBreak;
					A.break = {
						start: ee.vmin,
						end: ee.vmax
					};
				}
				o.type === "category" && (A.dataIndex = f), Al(O).eventData = A, j && yw(i, a, O, j);
			}
			h.push(O), c.add(O);
		}), mw(t, I(h, function(e) {
			return {
				label: e,
				priority: GC(e).labelInfo.tick.break ? e.z2 + (v - _ + 1) : e.z2,
				defaultAttr: { ignore: e.ignore }
			};
		}), c, l);
	}
}
function pw(e) {
	return !!e.labelLayoutList;
}
function mw(e, t, n, r) {
	e.labelLayoutList = t, e.labelGroup = n, e.axisLabelsCreationContext = r;
}
function hw(e, t, n, r) {
	var i = t.get(["axisLabel", "margin"]);
	F(n, function(n, a) {
		var o = SC(n);
		if (o) {
			var s = o.label, c = GC(s);
			o.suggestIgnore = s.ignore, s.ignore = !1, Hi(gw, _w);
			var l = t.axis;
			gw.x = l.dataToCoord(eC(l.scale, c.labelInfo.tick)), gw.y = e.labelOffset + e.labelDirection * i, gw.rotation = c.layoutRotation, r.add(gw), gw.updateTransform(), r.remove(gw), gw.decomposeTransform(), Hi(s, gw), s.markRedraw(), bC(o, !0), SC(o);
		}
	});
}
var gw = new dl(), _w = new dl();
function vw(e) {
	return !!e;
}
function yw(e, t, n, r) {
	n.on("click", function(n) {
		var i = {
			type: VC,
			breaks: [{
				start: r.parsedBreak.breakOption.start,
				end: r.parsedBreak.breakOption.end
			}]
		};
		i[e.axis.dim + "AxisIndex"] = e.componentIndex, t.dispatchAction(i);
	});
}
function bw(e, t, n) {
	var r = um();
	if (r) {
		var i = r.retrieveAxisBreakPairs(n, function(e) {
			return e && GC(e.label).labelInfo.tick.break;
		}, !0), a = e.get(["breakLabelLayout", "moveOverlap"], !0);
		(a === !0 || a === "auto") && F(i, function(r) {
			BC().adjustBreakLabelPair(e.axis.inverse, t, [SC(n[r[0]]), SC(n[r[1]])]);
		});
	}
}
//#endregion
//#region node_modules/echarts/lib/coord/cartesian/cartesianAxisHelper.js
function xw(e, t, n) {
	n ||= {};
	var r = t.axis, i = {}, a = r.getAxesOnZeroOf()[0], o = r.position, s = a ? "onZero" : o, c = r.dim, l = [
		e.x,
		e.x + e.width,
		e.y,
		e.y + e.height
	], u = {
		left: 0,
		right: 1,
		top: 0,
		bottom: 1,
		onZero: 2
	}, d = t.get("offset") || 0, f = c === "x" ? [l[2] - d, l[3] + d] : [l[0] - d, l[1] + d];
	if (a) {
		var p = a.toGlobalCoord(a.dataToCoord(0));
		f[u.onZero] = Math.max(Math.min(p, f[1]), f[0]);
	}
	i.position = [c === "y" ? f[u[s]] : l[0], c === "x" ? f[u[s]] : l[3]], i.rotation = Math.PI / 2 * (c === "x" ? 0 : 1), i.labelDirection = i.tickDirection = i.nameDirection = {
		top: -1,
		bottom: 1,
		left: -1,
		right: 1
	}[o], i.labelOffset = a ? f[u[o]] - f[u.onZero] : 0, t.get(["axisTick", "inside"]) && (i.tickDirection = -i.tickDirection), pe(n.labelInside, t.get(["axisLabel", "inside"])) && (i.labelDirection = -i.labelDirection);
	var m = t.get(["axisLabel", "rotate"]);
	return i.labelRotate = s === "top" ? -m : m, i.z2 = 1, i;
}
//#endregion
//#region node_modules/echarts/lib/visual/LegendVisualProvider.js
var Sw = function() {
	function e(e, t) {
		this._getDataWithEncodedVisual = e, this._getRawData = t;
	}
	return e.prototype.getAllNames = function() {
		var e = this._getRawData();
		return e.mapArray(e.getName);
	}, e.prototype.containName = function(e) {
		return this._getRawData().indexOfName(e) >= 0;
	}, e.prototype.indexOfName = function(e) {
		return this._getDataWithEncodedVisual().indexOfName(e);
	}, e.prototype.getItemVisual = function(e, t) {
		return this._getDataWithEncodedVisual().getItemVisual(e, t);
	}, e;
}();
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/modelHelper.js
function Cw(e, t) {
	var n = {
		axesInfo: {},
		seriesInvolved: !1,
		coordSysAxesInfo: {},
		coordSysMap: {}
	};
	return ww(n, e, t), n.seriesInvolved && Ew(n, e), n;
}
function ww(e, t, n) {
	var r = t.getComponent("tooltip"), i = t.getComponent("axisPointer"), a = i.get("link", !0) || [], o = [];
	F(n.getCoordinateSystems(), function(n) {
		if (!n.axisPointerEnabled) return;
		var s = Nw(n.model), c = e.coordSysAxesInfo[s] = {};
		e.coordSysMap[s] = n;
		var l = n.model.getModel("tooltip", r);
		if (F(n.getAxes(), z(p, !1, null)), n.getTooltipAxes && r && l.get("show")) {
			var u = l.get("trigger") === "axis", d = l.get(["axisPointer", "type"]) === "cross", f = n.getTooltipAxes(l.get(["axisPointer", "axis"]));
			(u || d) && F(f.baseAxes, z(p, !d || "cross", u)), d && F(f.otherAxes, z(p, "cross", !1));
		}
		function p(r, s, u) {
			var d = u.model.getModel("axisPointer", i), f = d.get("show");
			if (f && (f !== "auto" || r || Mw(d))) {
				s ??= d.get("triggerTooltip"), d = r ? Tw(u, l, i, t, r, s) : d;
				var p = d.get("snap"), m = d.get("triggerEmphasis"), h = Nw(u.model), g = s || p || u.type === "category", _ = e.axesInfo[h] = {
					key: h,
					axis: u,
					coordSys: n,
					axisPointerModel: d,
					triggerTooltip: s,
					triggerEmphasis: m,
					involveSeries: g,
					snap: p,
					useHandle: Mw(d),
					seriesModels: [],
					linkGroup: null
				};
				c[h] = _, e.seriesInvolved = e.seriesInvolved || g;
				var v = Dw(a, u);
				if (v != null) {
					var y = o[v] || (o[v] = { axesInfo: {} });
					y.axesInfo[h] = _, y.mapper = a[v].mapper, _.linkGroup = y;
				}
			}
		}
	});
}
function Tw(e, t, n, r, i, a) {
	var o = t.getModel("axisPointer"), s = [
		"type",
		"snap",
		"lineStyle",
		"shadowStyle",
		"label",
		"animation",
		"animationDurationUpdate",
		"animationEasingUpdate",
		"z"
	], c = {};
	F(s, function(e) {
		c[e] = k(o.get(e));
	}), c.snap = e.type !== "category" && !!a, o.get("type") === "cross" && (c.type = "line");
	var l = c.label ||= {};
	if (l.show ??= !1, i === "cross" && (l.show = o.get(["label", "show"]) ?? !0, !a)) {
		var u = c.lineStyle = o.get("crossStyle");
		u && M(l, u.textStyle);
	}
	return e.model.getModel("axisPointer", new Kp(c, n, r));
}
function Ew(e, t) {
	t.eachSeries(function(t) {
		var n = t.coordinateSystem, r = t.get(["tooltip", "trigger"], !0), i = t.get(["tooltip", "show"], !0);
		n && n.model && r !== "none" && r !== !1 && r !== "item" && i !== !1 && t.get(["axisPointer", "show"], !0) !== !1 && F(e.coordSysAxesInfo[Nw(n.model)], function(e) {
			var r = e.axis;
			n.getAxis(r.dim) === r && (e.seriesModels.push(t), e.seriesDataCount ??= 0, e.seriesDataCount += t.getData().count());
		});
	});
}
function Dw(e, t) {
	for (var n = t.model, r = t.dim, i = 0; i < e.length; i++) {
		var a = e[i] || {};
		if (Ow(a[r + "AxisId"], n.id) || Ow(a[r + "AxisIndex"], n.componentIndex) || Ow(a[r + "AxisName"], n.name)) return i;
	}
}
function Ow(e, t) {
	return e === "all" || B(e) && N(e, t) >= 0 || e === t;
}
function kw(e) {
	var t = Aw(e);
	if (t) {
		var n = t.axisPointerModel, r = t.axis.scale, i = n.option, a = n.get("status"), o = n.get("value");
		o != null && (o = r.parse(o));
		var s = Mw(n);
		a ?? (i.status = s ? "show" : "hide");
		var c = r.getExtent();
		(o == null || o > c[1]) && (o = c[1]), o < c[0] && (o = c[0]), i.value = o, s && (i.status = t.axis.scale.isBlank() ? "hide" : "show");
	}
}
function Aw(e) {
	var t = (e.ecModel.getComponent("axisPointer") || {}).coordSysAxesInfo;
	return t && t.axesInfo[Nw(e)];
}
function jw(e) {
	var t = Aw(e);
	return t && t.axisPointerModel;
}
function Mw(e) {
	return !!e.get(["handle", "show"]);
}
function Nw(e) {
	return e.type + "||" + e.id;
}
//#endregion
//#region node_modules/echarts/lib/component/axis/AxisView.js
var Pw = {}, Fw = function(e) {
	r(t, e);
	function t() {
		var n = e !== null && e.apply(this, arguments) || this;
		return n.type = t.type, n;
	}
	return t.prototype.render = function(t, n, r, i) {
		this.axisPointerClass && kw(t), e.prototype.render.apply(this, arguments), this._doUpdateAxisPointerClass(t, r, !0);
	}, t.prototype.updateAxisPointer = function(e, t, n, r) {
		this._doUpdateAxisPointerClass(e, n, !1);
	}, t.prototype.remove = function(e, t) {
		var n = this._axisPointer;
		n && n.remove(t);
	}, t.prototype.dispose = function(t, n) {
		this._disposeAxisPointer(n), e.prototype.dispose.apply(this, arguments);
	}, t.prototype._doUpdateAxisPointerClass = function(e, n, r) {
		var i = t.getAxisPointerClass(this.axisPointerClass);
		if (i) {
			var a = jw(e);
			a ? (this._axisPointer ||= new i()).render(e, a, n, r) : this._disposeAxisPointer(n);
		}
	}, t.prototype._disposeAxisPointer = function(e) {
		this._axisPointer && this._axisPointer.dispose(e), this._axisPointer = null;
	}, t.registerAxisPointerClass = function(e, t) {
		Pw[e] = t;
	}, t.getAxisPointerClass = function(e) {
		return e && Pw[e];
	}, t.type = "axis", t;
}(ov), Iw = Y();
function Lw(e, t) {
	return !!Iw(e)[t];
}
Gx({
	type: "takeGlobalCursor",
	event: "globalCursorTaken",
	update: "update"
}, Ae);
//#endregion
//#region node_modules/echarts/lib/component/helper/cursorHelper.js
var Rw = {
	axisPointer: 1,
	tooltip: 1,
	brush: 1
};
function zw(e, t, n) {
	var r = t.getComponentByElement(e.topTarget);
	if (!r || r === n || Rw.hasOwnProperty(r.mainType)) return !1;
	var i = r.coordinateSystem;
	if (!i || i.model === n) return !1;
	var a = fp(r), o = fp(n);
	return !((a.zlevel - o.zlevel || a.z - o.z) <= 0);
}
//#endregion
//#region node_modules/echarts/lib/component/helper/RoamController.js
var Bw = function(e) {
	r(t, e);
	function t(t) {
		var n = e.call(this) || this;
		n._zr = t;
		var r = R(n._mousedownHandler, n), i = R(n._mousemoveHandler, n), a = R(n._mouseupHandler, n), o = R(n._mousewheelHandler, n), s = R(n._pinchHandler, n);
		return n.enable = function(e, n) {
			var c = n.zInfo, l = fp(c.component), u = l.z, d = l.zlevel, f = {
				component: c.component,
				z: u,
				zlevel: d,
				z2: G(c.z2, -Infinity)
			}, p = j({}, n.triggerInfo);
			this._opt = M(j({}, n), {
				zoomOnMouseWheel: !0,
				moveOnMouseMove: !0,
				moveOnMouseWheel: !1,
				preventDefaultMouseMove: !0,
				zInfoParsed: f,
				triggerInfo: p,
				cursorGrab: "grab",
				cursorGrabbing: "grabbing"
			}), e ??= !0, (!this._enabled || this._controlType !== e) && (this.disable(), this._enabled = !0, (e === !0 || e === "move" || e === "pan") && (Ww(t, "mousedown", r, f), Ww(t, "mousemove", i, f), Ww(t, "mouseup", a, f)), (e === !0 || e === "scale" || e === "zoom") && (Ww(t, "mousewheel", o, f), Ww(t, "pinch", s, f)));
		}, n.disable = function() {
			this._enabled && (this._enabled = !1, Gw(t, "mousedown", r), Gw(t, "mousemove", i), Gw(t, "mouseup", a), Gw(t, "mousewheel", o), Gw(t, "pinch", s));
		}, n;
	}
	return t.prototype.isDragging = function() {
		return this._dragging;
	}, t.prototype.isPinching = function() {
		return this._pinching;
	}, t.prototype._checkPointer = function(e, t, n) {
		var r = this._opt, i = r.zInfoParsed;
		if (zw(e, r.api, i.component)) return !1;
		var a = r.triggerInfo, o = a.roamTrigger, s = !1;
		return o === "global" && (s = !0), s ||= a.isInSelf(e, t, n), s && a.isInClip && !a.isInClip(e, t, n) && (s = !1), s;
	}, t.prototype._decideCursorStyle = function(e, t, n, r) {
		var i = e.target;
		if (!i && this._checkPointer(e, t, n)) return this._opt.cursorGrab;
		if (r) return i && i.cursor || "default";
	}, t.prototype.dispose = function() {
		this.disable();
	}, t.prototype._mousedownHandler = function(e) {
		if (!(wt(e) || Vw(e))) {
			for (var t = e.target; t;) {
				if (t.draggable) return;
				t = t.__hostTarget || t.parent;
			}
			var n = e.offsetX, r = e.offsetY;
			this._checkPointer(e, n, r) && (this._x = n, this._y = r, this._dragging = !0);
		}
	}, t.prototype._mousemoveHandler = function(e) {
		var t = this._zr;
		if (!(e.gestureEvent === "pinch" || Lw(t, "globalPan") || Vw(e))) {
			var n = e.offsetX, r = e.offsetY;
			if (!this._dragging || !Yw("moveOnMouseMove", e, this._opt)) {
				var i = this._decideCursorStyle(e, n, r, !1);
				i && t.setCursorStyle(i);
			} else {
				t.setCursorStyle(this._opt.cursorGrabbing);
				var a = this._x, o = this._y, s = n - a, c = r - o;
				this._x = n, this._y = r, this._opt.preventDefaultMouseMove && Ct(e.event), e.__ecRoamConsumed = !0, Jw(this, "pan", "moveOnMouseMove", e, {
					dx: s,
					dy: c,
					oldX: a,
					oldY: o,
					newX: n,
					newY: r,
					isAvailableBehavior: null
				});
			}
		}
	}, t.prototype._mouseupHandler = function(e) {
		if (!Vw(e)) {
			var t = this._zr;
			if (!wt(e)) {
				this._dragging = !1;
				var n = this._decideCursorStyle(e, e.offsetX, e.offsetY, !0);
				n && t.setCursorStyle(n);
			}
		}
	}, t.prototype._mousewheelHandler = function(e) {
		if (!Vw(e)) {
			var t = Yw("zoomOnMouseWheel", e, this._opt), n = Yw("moveOnMouseWheel", e, this._opt), r = e.wheelDelta, i = Math.abs(r), a = e.offsetX, o = e.offsetY;
			if (r !== 0 && (t || n)) {
				if (t) {
					var s = i > 3 ? 1.4 : i > 1 ? 1.2 : 1.1, c = r > 0 ? s : 1 / s;
					this._checkTriggerMoveZoom(this, "zoom", "zoomOnMouseWheel", e, {
						scale: c,
						originX: a,
						originY: o,
						isAvailableBehavior: null
					});
				}
				if (n) {
					var l = Math.abs(r), u = (r > 0 ? 1 : -1) * (l > 3 ? .4 : l > 1 ? .15 : .05);
					this._checkTriggerMoveZoom(this, "scrollMove", "moveOnMouseWheel", e, {
						scrollDelta: u,
						originX: a,
						originY: o,
						isAvailableBehavior: null
					});
				}
			}
		}
	}, t.prototype._pinchHandler = function(e) {
		if (!(Lw(this._zr, "globalPan") || Vw(e))) {
			var t = e.pinchScale > 1 ? 1.1 : 1 / 1.1;
			this._checkTriggerMoveZoom(this, "zoom", null, e, {
				scale: t,
				originX: e.pinchX,
				originY: e.pinchY,
				isAvailableBehavior: null
			});
		}
	}, t.prototype._checkTriggerMoveZoom = function(e, t, n, r, i) {
		e._checkPointer(r, i.originX, i.originY) && (Ct(r.event), r.__ecRoamConsumed = !0, Jw(e, t, n, r, i));
	}, t;
}(Qe);
function Vw(e) {
	return e.__ecRoamConsumed;
}
var Hw = Y();
function Uw(e) {
	var t = Hw(e);
	return t.roam = t.roam || {}, t.uniform = t.uniform || {}, t;
}
function Ww(e, t, n, r) {
	for (var i = Uw(e).roam, a = i[t] = i[t] || [], o = 0; o < a.length; o++) {
		var s = a[o].zInfoParsed;
		if ((s.zlevel - r.zlevel || s.z - r.z || s.z2 - r.z2) <= 0) break;
	}
	a.splice(o, 0, {
		listener: n,
		zInfoParsed: r
	}), Kw(e, t);
}
function Gw(e, t, n) {
	for (var r = Uw(e).roam[t] || [], i = 0; i < r.length; i++) if (r[i].listener === n) {
		r.splice(i, 1), r.length || qw(e, t);
		return;
	}
}
function Kw(e, t) {
	var n = Uw(e);
	n.uniform[t] || e.on(t, n.uniform[t] = function(e) {
		var r = n.roam[t];
		if (r) for (var i = 0; i < r.length; i++) r[i].listener(e);
	});
}
function qw(e, t) {
	var n = Uw(e).uniform;
	n[t] && (e.off(t, n[t]), n[t] = null);
}
function Jw(e, t, n, r, i) {
	i.isAvailableBehavior = R(Yw, null, n, r), e.trigger(t, i);
}
function Yw(e, t, n) {
	var r = n[e];
	return !e || r && (!H(r) || t.event[r + "Key"]);
}
function Xw(e) {
	return e;
}
var Zw = "view", Qw = function(e) {
	r(t, e);
	function t(t, n, r) {
		var i = e.call(this) || this;
		i.type = Zw, i.dimensions = ["x", "y"];
		var a = Xw(i);
		a.invertY = t, a.lgCt = n, a.lgGeo = r;
		var o = a.trans = [];
		return o[0] = Bi(), o[1] = Bi(), o[2] = Bi(), a.mtRaw = kt(), a.mtRawInv = kt(), a.mtOverall = kt(), a.mtOverallInv = kt(), a.zoom = 1, i;
	}
	return t.prototype.getBoundingRect = function() {
		return tT(null, this);
	}, t.prototype.getViewRect = function() {
		return nT(null, this);
	}, t.prototype.getRoamTransform = function() {
		return zi(Xw(this).trans[1]);
	}, t.prototype.dataToPoint = function(e, t, n) {
		var r = t ? Xw(this).mtRaw : Xw(this).mtOverall;
		return n ||= [], r ? qe(n, e, r) : Ne(n, e);
	}, t.prototype.pointToData = function(e, t, n) {
		n ||= [];
		var r = Xw(this).mtOverallInv;
		return r ? qe(n, e, r) : Ne(n, e);
	}, t.prototype.convertToPixel = function(e, t, n) {
		var r = ST(t);
		return r === this ? r.dataToPoint(n) : null;
	}, t.prototype.convertFromPixel = function(e, t, n) {
		var r = ST(t);
		return r === this ? r.pointToData(n) : null;
	}, t.prototype.containPoint = function(e) {
		var t = Xw(this);
		return Qt($w, t.dataRect), en($w, $w, t.mtOverall), tn($w, e[0], e[1]);
	}, t.dimensions = ["x", "y"], t;
}(Ri), $w = Xt();
function eT(e, t) {
	return jt(e || [], Xw(t).mtOverall);
}
function tT(e, t) {
	return Qt(e || Xt(), Xw(t).dataRect);
}
function nT(e, t) {
	return Qt(e || Xt(), Xw(t).viewRect);
}
function rT(e, t, n) {
	return Hi(e || Bi(), Xw(t).trans[n]);
}
function iT(e) {
	return !!(e.dataRect && e.viewRect);
}
function aT(e, t, n, r) {
	r === 1 ? yT(e, t.trans[0], n) : Hi(e, n);
}
function oT(e, t, n) {
	zi(n, sT), Mt(sT, sT, t.mtRawInv), _p(e, sT);
}
var sT = kt();
function cT(e, t) {
	var n = Xw(e);
	n.centerOption = t.getShallow("center");
	var r = n.zoomLimit = t.getShallow("scaleLimit");
	n.zoom = HT(t.getShallow("zoom") || 1, r) || 1, iT(n) && dT(n);
}
function lT(e, t, n, r, i) {
	var a = Xw(e);
	a.dataRect = new J(t, n, r, i), iT(a) && dT(a);
}
function uT(e, t, n, r, i) {
	var a = Xw(e);
	a.viewRect = new J(t, n, r, i), iT(a) && dT(a);
}
function dT(e) {
	fT(e), hT(e), _T(e);
}
function fT(e) {
	var t = e.dataRect, n = e.viewRect, r = e.trans[0], i = e.invertY;
	i && (t = Qt(mT, t), t.y = -t.y - t.height), $t(pT, t, n), _p(r, pT), i && (r.scaleY = -r.scaleY);
	var a = zi(r, e.mtRaw);
	It(e.mtRawInv, a);
}
var pT = kt(), mT = Xt();
function hT(e) {
	var t = CT(e), n = FT(gT, e, e.centerOption) ? qe(gT, gT, e.mtRaw) : t, r = e.zoom, i = e.trans[1];
	i.x = t[0] - r * n[0], i.y = t[1] - r * n[1], i.scaleX = i.scaleY = r;
}
var gT = [];
function _T(e) {
	var t = e.trans, n = t[1], r = t[0], i = t[2];
	yT(i, r, n);
	var a = zi(i, e.mtOverall), o = It(e.mtOverallInv, a);
	vT(e, i, a, o), vT(e.lgGeo, i, a, o);
}
function vT(e, t, n, r) {
	e && (Hi(e, t), jt(e.transform ||= [], n), jt(e.invTransform ||= [], r));
}
function yT(e, t, n) {
	zi(t, bT), zi(n, xT), Mt(xT, xT, bT), _p(e, xT);
}
var bT = kt(), xT = kt();
function ST(e) {
	var t = e.seriesModel;
	return t ? t.coordinateSystem : null;
}
function CT(e) {
	var t = e.viewRect;
	return wT[0] = t.x + t.width / 2, wT[1] = t.y + t.height / 2, wT;
}
var wT = [];
function TT(e) {
	return e && e.type === "view";
}
function ET(e, t, n, r) {
	var i = Xw(n);
	i.syncBackEl = e, i.syncBackType = t, r ? yf(e, rT(null, n, t), r) : (rT(e, n, t), e.dirty());
}
function DT(e, t, n, r) {
	var i = Xw(e), a = i.syncBackEl;
	a ? (a.stopAnimation(), aT(OT, i, a, i.syncBackType)) : Hi(OT, i.trans[2]), oT(kT, i, OT), r ? NT(OT, kT, i, r) : Hi(OT, kT), oT(OT, i, OT), zT(i, t, n, OT);
}
var OT = Bi(), kT = Bi();
function AT(e, t, n) {
	var r = jT(t);
	r && (DT(r, t, n, e), cT(r, t));
}
function jT(e) {
	return e.__ownRoamView ? e.__ownRoamView() : null;
}
function MT(e, t, n, r) {
	n.setUpdatePayload(gp(e));
	var i = Wl(r, t);
	i && i.__updateOnOwnRoam && i.__updateOnOwnRoam(e, t, r);
}
function NT(e, t, n, r) {
	r.dx != null && r.dy != null && (e.x += r.dx, e.y += r.dy);
	var i = r.zoom;
	if (i != null) {
		var a = PT(t), o = HT(a * i, n.zoomLimit) / a;
		e.x -= (r.originX - e.x) * (o - 1), e.y -= (r.originY - e.y) * (o - 1), e.scaleX *= o, e.scaleY *= o;
	}
}
function PT(e) {
	return e.scaleX;
}
function FT(e, t, n) {
	var r = t.dataRect;
	if (!n) return !1;
	var i = t.lgCt;
	return i ? Fe(e, za(n[0], i.w), za(n[1], i.h)) : r && Fe(e, za(n[0], r.width, r.x), za(n[1], r.height, r.y)), !0;
}
function IT(e, t) {
	var n = e.centerOption, r = e.dataRect;
	return !n || e.lgCt ? t.slice() : [LT(0, t, n, r), LT(1, t, n, r)];
}
function LT(e, t, n, r) {
	return n && r && r[kf[e]] && Ha(n[e]) ? (t[e] - r[Of[e]]) / r[kf[e]] * 100 + "%" : t[e];
}
function RT(e, t) {
	return t && e && e.getShallow("legacyViewCoordSysCenterBase") ? {
		w: t.getWidth(),
		h: t.getHeight()
	} : null;
}
function zT(e, t, n, r) {
	var i = CT(e), a = PT(r), o = Na(a) > 1e-6;
	BT[0] = o ? (i[0] - r.x) / a : i[0], BT[1] = o ? (i[1] - r.y) / a : i[1], qe(BT, BT, e.mtRawInv);
	var s = IT(e, BT);
	VT(t, s, a), F(n, function(e) {
		e !== t && VT(e, s.slice(), a);
	});
}
var BT = [];
function VT(e, t, n) {
	var r = e.option;
	r.center = t, r.zoom = n;
}
function HT(e, t) {
	if (t) {
		var n = t.min || 0, r = t.max || Infinity;
		e = Math.max(Math.min(r, e), n);
	}
	return e;
}
function UT(e, t) {
	var n = t.getShallow("nodeScaleRatio", !0) || 1, r = Xw(e);
	return ((r.zoom - 1) * n + 1) / (r.trans[2].scaleX || 1);
}
//#endregion
//#region node_modules/echarts/lib/component/helper/roamHelper.js
function WT(e, t, n, r, i, a, o, s) {
	if (!jT(e)) {
		n.disable();
		return;
	}
	n.enable(G(e.get("roam"), o), {
		api: t,
		zInfo: { component: e },
		triggerInfo: {
			roamTrigger: e.get("roamTrigger"),
			isInSelf: r,
			isInClip: function(e, t, n) {
				return !i || i.contain(t, n);
			}
		}
	});
	function c(n) {
		var r = e.mainType, i = gp(M({ type: KT(r, e.subType, Vl) }, n));
		s && (i.componentType = r), i[r + "Id"] = e.id, t.dispatchAction(i);
	}
	n.off("pan").off("zoom").on("pan", function(e) {
		a && a("pan"), c({
			dx: e.dx,
			dy: e.dy
		});
	}).on("zoom", function(e) {
		a && a("zoom"), c({
			zoom: e.scale,
			originX: e.originX,
			originY: e.originY
		});
	});
}
new J(0, 0, 0, 0);
function GT(e, t, n) {
	var r = KT(t, n, Vl);
	e.registerAction({
		type: r,
		event: r,
		update: "none"
	}, function(e, r, i) {
		r.eachComponent(zo(e, t, n), function(t) {
			AT(e, t), MT(e, t, r, i);
		});
	});
}
function KT(e, t, n) {
	return (e === "series" ? t === "map" ? "geo" : t : e) + n;
}
function qT(e) {
	return e.zoom != null;
}
function JT(e, t, n, r, i, a, o) {
	var s = new Qw(null, RT(e.ecModel, t));
	return lT(s, n, r, i, a), o ? uT(s, o.x, o.y, o.width, o.height) : uT(s, n, r, i, a), cT(s, e), s;
}
//#endregion
//#region node_modules/echarts/lib/data/helper/linkSeriesData.js
var YT = Y();
function XT(e) {
	var t = e.mainData, n = e.datas;
	n || (n = { main: t }, e.datasAttr = { main: "data" }), e.datas = e.mainData = null, rE(t, n, e), F(n, function(n) {
		F(t.TRANSFERABLE_METHODS, function(t) {
			n.wrapMethod(t, z(ZT, e));
		});
	}), t.wrapMethod("cloneShallow", z($T, e)), F(t.CHANGABLE_METHODS, function(n) {
		t.wrapMethod(n, z(QT, e));
	}), _e(n[t.dataType] === t);
}
function ZT(e, t) {
	if (nE(this)) {
		var n = j({}, YT(this).datas);
		n[this.dataType] = t, rE(t, n, e);
	} else iE(t, this.dataType, YT(this).mainData, e);
	return t;
}
function QT(e, t) {
	return e.struct && e.struct.update(), t;
}
function $T(e, t) {
	return F(YT(t).datas, function(n, r) {
		n !== t && iE(n.cloneShallow(), r, t, e);
	}), t;
}
function eE(e) {
	var t = YT(this).mainData;
	return e == null || t == null ? t : YT(t).datas[e];
}
function tE() {
	var e = YT(this).mainData;
	return e == null ? [{ data: e }] : I(L(YT(e).datas), function(t) {
		return {
			type: t,
			data: YT(e).datas[t]
		};
	});
}
function nE(e) {
	return YT(e).mainData === e;
}
function rE(e, t, n) {
	YT(e).datas = {}, F(t, function(t, r) {
		iE(t, r, e, n);
	});
}
function iE(e, t, n, r) {
	YT(n).datas[t] = e, YT(e).mainData = n, e.dataType = t, r.struct && (e[r.structAttr] = r.struct, r.struct[r.datasAttr[t]] = e), e.getLinkedData = eE, e.getLinkedDataAll = tE;
}
//#endregion
//#region node_modules/echarts/lib/data/Graph.js
function aE(e) {
	return "_EC_" + e;
}
var oE = function() {
	function e(e) {
		this.type = "graph", this.nodes = [], this.edges = [], this._nodesMap = {}, this._edgesMap = {}, this._directed = e || !1;
	}
	return e.prototype.isDirected = function() {
		return this._directed;
	}, e.prototype.addNode = function(e, t) {
		e = e == null ? "" + t : "" + e;
		var n = this._nodesMap;
		if (!n[aE(e)]) {
			var r = new sE(e, t);
			return r.hostGraph = this, this.nodes.push(r), n[aE(e)] = r, r;
		}
	}, e.prototype.getNodeByIndex = function(e) {
		var t = this.data.getRawIndex(e);
		return this.nodes[t];
	}, e.prototype.getNodeById = function(e) {
		return this._nodesMap[aE(e)];
	}, e.prototype.addEdge = function(e, t, n) {
		var r = this._nodesMap, i = this._edgesMap;
		if (U(e) && (e = this.nodes[e]), U(t) && (t = this.nodes[t]), e instanceof sE || (e = r[aE(e)]), t instanceof sE || (t = r[aE(t)]), e && t) {
			var a = e.id + "-" + t.id, o = new cE(e, t, n);
			return o.hostGraph = this, this._directed && (e.outEdges.push(o), t.inEdges.push(o)), e.edges.push(o), e !== t && t.edges.push(o), this.edges.push(o), i[a] = o, o;
		}
	}, e.prototype.getEdgeByIndex = function(e) {
		var t = this.edgeData.getRawIndex(e);
		return this.edges[t];
	}, e.prototype.getEdge = function(e, t) {
		e instanceof sE && (e = e.id), t instanceof sE && (t = t.id);
		var n = this._edgesMap;
		return this._directed ? n[e + "-" + t] : n[e + "-" + t] || n[t + "-" + e];
	}, e.prototype.eachNode = function(e, t) {
		for (var n = this.nodes, r = n.length, i = 0; i < r; i++) n[i].dataIndex >= 0 && e.call(t, n[i], i);
	}, e.prototype.eachEdge = function(e, t) {
		for (var n = this.edges, r = n.length, i = 0; i < r; i++) n[i].dataIndex >= 0 && n[i].node1.dataIndex >= 0 && n[i].node2.dataIndex >= 0 && e.call(t, n[i], i);
	}, e.prototype.breadthFirstTraverse = function(e, t, n, r) {
		if (t instanceof sE || (t = this._nodesMap[aE(t)]), t) {
			for (var i = n === "out" ? "outEdges" : n === "in" ? "inEdges" : "edges", a = 0; a < this.nodes.length; a++) this.nodes[a].__visited = !1;
			if (!e.call(r, t, null)) for (var o = [t]; o.length;) for (var s = o.shift(), c = s[i], a = 0; a < c.length; a++) {
				var l = c[a], u = l.node1 === s ? l.node2 : l.node1;
				if (!u.__visited) {
					if (e.call(r, u, s)) return;
					o.push(u), u.__visited = !0;
				}
			}
		}
	}, e.prototype.update = function() {
		for (var e = this.data, t = this.edgeData, n = this.nodes, r = this.edges, i = 0, a = n.length; i < a; i++) n[i].dataIndex = -1;
		for (var i = 0, a = e.count(); i < a; i++) n[e.getRawIndex(i)].dataIndex = i;
		t.filterSelf(function(e) {
			var n = r[t.getRawIndex(e)];
			return n.node1.dataIndex >= 0 && n.node2.dataIndex >= 0;
		});
		for (var i = 0, a = r.length; i < a; i++) r[i].dataIndex = -1;
		for (var i = 0, a = t.count(); i < a; i++) r[t.getRawIndex(i)].dataIndex = i;
	}, e.prototype.clone = function() {
		for (var t = new e(this._directed), n = this.nodes, r = this.edges, i = 0; i < n.length; i++) t.addNode(n[i].id, n[i].dataIndex);
		for (var i = 0; i < r.length; i++) {
			var a = r[i];
			t.addEdge(a.node1.id, a.node2.id, a.dataIndex);
		}
		return t;
	}, e;
}(), sE = function() {
	function e(e, t) {
		this.inEdges = [], this.outEdges = [], this.edges = [], this.dataIndex = -1, this.id = e ?? "", this.dataIndex = t ?? -1;
	}
	return e.prototype.degree = function() {
		return this.edges.length;
	}, e.prototype.inDegree = function() {
		return this.inEdges.length;
	}, e.prototype.outDegree = function() {
		return this.outEdges.length;
	}, e.prototype.getModel = function(e) {
		if (!(this.dataIndex < 0)) return this.hostGraph.data.getItemModel(this.dataIndex).getModel(e);
	}, e.prototype.getAdjacentDataIndices = function() {
		for (var e = {
			edge: [],
			node: []
		}, t = 0; t < this.edges.length; t++) {
			var n = this.edges[t];
			n.dataIndex < 0 || (e.edge.push(n.dataIndex), e.node.push(n.node1.dataIndex, n.node2.dataIndex));
		}
		return e;
	}, e.prototype.getTrajectoryDataIndices = function() {
		for (var e = K(), t = K(), n = 0, r = this.edges.length; n < r; n++) {
			var i = this.edges[n];
			if (!(i.dataIndex < 0)) {
				e.set(i.dataIndex, !0);
				for (var a = [i.node1], o = [i.node2], s = 0; s < a.length;) {
					var c = a[s];
					s++, t.set(c.dataIndex, !0);
					for (var l = c.inEdges, u = 0, d = l.length, f = void 0, p = void 0; u < d; u++) f = l[u], p = f.dataIndex, p >= 0 && !e.hasKey(p) && (e.set(p, !0), a.push(f.node1));
				}
				for (s = 0; s < o.length;) {
					var m = o[s];
					s++, t.set(m.dataIndex, !0);
					for (var h = m.outEdges, u = 0, g = h.length, _ = void 0, v = void 0; u < g; u++) _ = h[u], v = _.dataIndex, v >= 0 && !e.hasKey(v) && (e.set(v, !0), o.push(_.node2));
				}
			}
		}
		return {
			edge: e.keys(),
			node: t.keys()
		};
	}, e;
}(), cE = function() {
	function e(e, t, n) {
		this.dataIndex = -1, this.node1 = e, this.node2 = t, this.dataIndex = n ?? -1;
	}
	return e.prototype.getModel = function(e) {
		if (!(this.dataIndex < 0)) return this.hostGraph.edgeData.getItemModel(this.dataIndex).getModel(e);
	}, e.prototype.getAdjacentDataIndices = function() {
		return {
			edge: [this.dataIndex],
			node: [this.node1.dataIndex, this.node2.dataIndex]
		};
	}, e.prototype.getTrajectoryDataIndices = function() {
		var e = K(), t = K();
		e.set(this.dataIndex, !0);
		for (var n = [this.node1], r = [this.node2], i = 0; i < n.length;) {
			var a = n[i];
			i++, t.set(a.dataIndex, !0);
			for (var o = a.inEdges, s = 0, c = o.length, l = void 0, u = void 0; s < c; s++) l = a.inEdges[s], u = l.dataIndex, u >= 0 && !e.hasKey(u) && (e.set(u, !0), n.push(l.node1));
		}
		for (i = 0; i < r.length;) {
			var d = r[i];
			i++, t.set(d.dataIndex, !0);
			for (var f = d.outEdges, s = 0, c = f.length, p = void 0, m = void 0; s < c; s++) p = d.outEdges[s], m = p.dataIndex, m >= 0 && !e.hasKey(m) && (e.set(m, !0), r.push(p.node2));
		}
		return {
			edge: e.keys(),
			node: t.keys()
		};
	}, e;
}();
function lE(e, t) {
	return {
		getValue: function(n) {
			var r = this[e][t];
			return r.getStore().get(r.getDimensionIndex(n || "value"), this.dataIndex);
		},
		setVisual: function(n, r) {
			this.dataIndex >= 0 && this[e][t].setItemVisual(this.dataIndex, n, r);
		},
		getVisual: function(n) {
			return this[e][t].getItemVisual(this.dataIndex, n);
		},
		setLayout: function(n, r) {
			this.dataIndex >= 0 && this[e][t].setItemLayout(this.dataIndex, n, r);
		},
		getLayout: function() {
			return this[e][t].getItemLayout(this.dataIndex);
		},
		getGraphicEl: function() {
			return this[e][t].getItemGraphicEl(this.dataIndex);
		},
		getRawIndex: function() {
			return this[e][t].getRawIndex(this.dataIndex);
		}
	};
}
ne(sE, lE("hostGraph", "data")), ne(cE, lE("hostGraph", "edgeData"));
//#endregion
//#region node_modules/echarts/lib/chart/helper/createGraphFromNodeEdge.js
function uE(e, t, n, r, i) {
	for (var a = new oE(r), o = 0; o < e.length; o++) a.addNode(pe(e[o].id, e[o].name, o), o);
	for (var s = [], c = [], l = 0, o = 0; o < t.length; o++) {
		var u = t[o], d = u.source, f = u.target;
		a.addEdge(d, f, l) && (c.push(u), s.push(pe(Oo(u.id, null), d + " > " + f)), l++);
	}
	var p = n.get("coordinateSystem"), m;
	if (p === "cartesian2d" || p === "polar" || p === "matrix") m = US(e, n);
	else {
		var h = Pm.get(p), g = h && h.dimensions || [];
		N(g, "value") < 0 && g.concat(["value"]);
		var _ = MS(e, {
			coordDimensions: g,
			encodeDefine: n.getEncode()
		}).dimensions;
		m = new jS(_, n), m.initData(e);
	}
	var v = new jS(["value"], n);
	return v.initData(c, s), i && i(m, v), XT({
		mainData: m,
		struct: a,
		structAttr: "graph",
		datas: {
			node: m,
			edge: v
		},
		datasAttr: {
			node: "data",
			edge: "edgeData"
		}
	}), a.update(), a;
}
//#endregion
//#region node_modules/echarts/lib/chart/helper/multipleGraphEdgeHelper.js
var dE = "-->", fE = function(e) {
	return e.get("autoCurveness") || null;
}, pE = function(e, t) {
	var n = fE(e), r = 20, i = [];
	if (U(n)) r = n;
	else if (B(n)) {
		e.__curvenessList = n;
		return;
	}
	t > r && (r = t);
	var a = r % 2 ? r + 2 : r + 3;
	i = [];
	for (var o = 0; o < a; o++) i.push((o % 2 ? o + 1 : o) / 10 * (o % 2 ? -1 : 1));
	e.__curvenessList = i;
}, mE = function(e, t, n) {
	var r = [e.id, e.dataIndex].join("."), i = [t.id, t.dataIndex].join(".");
	return [
		n.uid,
		r,
		i
	].join(dE);
}, hE = function(e) {
	var t = e.split(dE);
	return [
		t[0],
		t[2],
		t[1]
	].join(dE);
}, gE = function(e, t) {
	var n = mE(e.node1, e.node2, t);
	return t.__edgeMap[n];
}, _E = function(e, t) {
	return vE(mE(e.node1, e.node2, t), t) + vE(mE(e.node2, e.node1, t), t);
}, vE = function(e, t) {
	var n = t.__edgeMap;
	return n[e] ? n[e].length : 0;
};
function yE(e) {
	fE(e) && (e.__curvenessList = [], e.__edgeMap = {}, pE(e));
}
function bE(e, t, n, r) {
	if (fE(n)) {
		var i = mE(e, t, n), a = n.__edgeMap, o = a[hE(i)];
		a[i] && !o ? a[i].isForward = !0 : o && a[i] && (o.isForward = !0, a[i].isForward = !1), a[i] = a[i] || [], a[i].push(r);
	}
}
function xE(e, t, n, r) {
	var i = fE(t), a = B(i);
	if (!i) return null;
	var o = gE(e, t);
	if (!o) return null;
	for (var s = -1, c = 0; c < o.length; c++) if (o[c] === n) {
		s = c;
		break;
	}
	var l = _E(e, t);
	pE(t, l), e.lineStyle = e.lineStyle || {};
	var u = mE(e.node1, e.node2, t), d = t.__curvenessList, f = a || l % 2 ? 0 : 1;
	if (o.isForward) return d[f + s];
	var p = vE(hE(u), t), m = d[s + p + f];
	return r ? a ? i && i[0] === 0 ? (p + f) % 2 ? m : -m : ((p % 2 ? 0 : 1) + f) % 2 ? m : -m : (p + f) % 2 ? m : -m : d[s + p + f];
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/GraphSeries.js
var SE = "graph", CE = function(e) {
	r(t, e);
	function t() {
		var n = e !== null && e.apply(this, arguments) || this;
		return n.type = t.type, n.hasSymbolVisual = !0, n;
	}
	return t.prototype.init = function(t) {
		e.prototype.init.apply(this, arguments);
		var n = this;
		function r() {
			return n._categoriesData;
		}
		this.legendVisualProvider = new Sw(r, r), this.fillDataTextStyle(t.edges || t.links), this._updateCategoriesData();
	}, t.prototype.mergeOption = function(t) {
		e.prototype.mergeOption.apply(this, arguments), this.fillDataTextStyle(t.edges || t.links), this._updateCategoriesData();
	}, t.prototype.mergeDefaultAndTheme = function(t) {
		e.prototype.mergeDefaultAndTheme.apply(this, arguments), ho(t, "edgeLabel", ["show"]);
	}, t.prototype.getInitialData = function(e, t) {
		var n = e.edges || e.links || [], r = e.data || e.nodes || [], i = this;
		if (r && n) {
			yE(this);
			var a = uE(r, n, this, !0, o);
			return F(a.edges, function(e) {
				bE(e.node1, e.node2, this, e.dataIndex);
			}, this), a.data;
		}
		function o(e, t) {
			e.wrapMethod("getItemModel", function(e) {
				var t = i._categoriesModels[e.getShallow("category")];
				return t && (t.parentModel = e.parentModel, e.parentModel = t), e;
			});
			var n = Kp.prototype.getModel;
			function r(e, t) {
				var r = n.call(this, e, t);
				return r.resolveParentPath = a, r;
			}
			t.wrapMethod("getItemModel", function(e) {
				return e.resolveParentPath = a, e.getModel = r, e;
			});
			function a(e) {
				if (e && (e[0] === "label" || e[1] === "label")) {
					var t = e.slice();
					return e[0] === "label" ? t[0] = "edgeLabel" : e[1] === "label" && (t[1] = "edgeLabel"), t;
				}
				return e;
			}
		}
	}, t.prototype.getGraph = function() {
		return this.getData().graph;
	}, t.prototype.getEdgeData = function() {
		return this.getGraph().edgeData;
	}, t.prototype.getCategoriesData = function() {
		return this._categoriesData;
	}, t.prototype.formatTooltip = function(e, t, n) {
		if (n === "edge") {
			var r = this.getData(), i = this.getDataParams(e, n), a = r.graph.getEdgeByIndex(e), o = r.getName(a.node1.dataIndex), s = r.getName(a.node2.dataIndex), c = [];
			return o != null && c.push(o), s != null && c.push(s), j_("nameValue", {
				name: c.join(" > "),
				value: i.value,
				noValue: i.value == null
			});
		}
		return q_({
			series: this,
			dataIndex: e,
			multipleSeries: t
		});
	}, t.prototype._updateCategoriesData = function() {
		var e = I(this.option.categories || [], function(e) {
			return e.value == null ? j({ value: 0 }, e) : e;
		}), t = new jS(["value"], this);
		t.initData(e), this._categoriesData = t, this._categoriesModels = t.mapArray(function(e) {
			return t.getItemModel(e);
		});
	}, t.prototype.isAnimationEnabled = function() {
		return e.prototype.isAnimationEnabled.call(this) && !(this.get("layout") === "force" && this.get(["force", "layoutAnimation"]));
	}, t.prototype.__ownRoamView = function() {
		var e = this.coordinateSystem;
		return TT(e) && e;
	}, t.type = "series." + SE, t.dependencies = [
		"grid",
		"polar",
		"geo",
		"singleAxis",
		"calendar"
	], t.defaultOption = {
		z: 2,
		coordinateSystem: "view",
		legendHoverLink: !0,
		layout: null,
		circular: { rotateLabel: !1 },
		force: {
			initLayout: null,
			repulsion: [0, 50],
			gravity: .1,
			friction: .6,
			edgeLength: 30,
			layoutAnimation: !0
		},
		left: "center",
		top: "center",
		symbol: "circle",
		symbolSize: 10,
		edgeSymbol: ["none", "none"],
		edgeSymbolSize: 10,
		edgeLabel: {
			position: "middle",
			distance: 5
		},
		draggable: !1,
		roam: !1,
		center: null,
		zoom: 1,
		nodeScaleRatio: .6,
		label: {
			show: !1,
			formatter: "{b}"
		},
		itemStyle: {},
		lineStyle: {
			color: Q.color.neutral50,
			width: 1,
			opacity: .5
		},
		emphasis: {
			scale: !0,
			label: { show: !0 }
		},
		select: { itemStyle: { borderColor: Q.color.primary } }
	}, t;
}(Z_);
//#endregion
//#region node_modules/echarts/lib/chart/graph/edgeVisual.js
function wE(e) {
	return e instanceof Array || (e = [e, e]), e;
}
var TE = Jo(SE, EE);
function EE(e) {
	e.eachSeriesByType(SE, function(e) {
		var t = e.getGraph(), n = e.getEdgeData(), r = wE(e.get("edgeSymbol")), i = wE(e.get("edgeSymbolSize"));
		n.setVisual("fromSymbol", r && r[0]), n.setVisual("toSymbol", r && r[1]), n.setVisual("fromSymbolSize", i && i[0]), n.setVisual("toSymbolSize", i && i[1]), n.setVisual("style", e.getModel("lineStyle").getLineStyle()), n.each(function(e) {
			var r = n.getItemModel(e), i = t.getEdgeByIndex(e), a = wE(r.getShallow("symbol", !0)), o = wE(r.getShallow("symbolSize", !0)), s = r.getModel("lineStyle").getLineStyle(), c = n.ensureUniqueItemVisual(e, "style");
			switch (j(c, s), c.stroke) {
				case "source":
					var l = i.node1.getVisual("style");
					c.stroke = l && l.fill;
					break;
				case "target":
					var l = i.node2.getVisual("style");
					c.stroke = l && l.fill;
			}
			a[0] && i.setVisual("fromSymbol", a[0]), a[1] && i.setVisual("toSymbol", a[1]), o[0] && i.setVisual("fromSymbolSize", o[0]), o[1] && i.setVisual("toSymbolSize", o[1]);
		});
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/simpleLayoutHelper.js
function DE(e) {
	var t = e.coordinateSystem;
	if (!(t && t.type !== "view")) {
		var n = e.getGraph();
		n.eachNode(function(e) {
			var t = e.getModel();
			e.setLayout([+t.get("x"), +t.get("y")]);
		}), OE(n, e);
	}
}
function OE(e, t) {
	e.eachEdge(function(e, n) {
		var r = me(e.getModel().get(["lineStyle", "curveness"]), -xE(e, t, n, !0), 0), i = Pe(e.node1.getLayout()), a = Pe(e.node2.getLayout()), o = [i, a];
		+r && o.push([(i[0] + a[0]) / 2 - (i[1] - a[1]) * r, (i[1] + a[1]) / 2 - (a[0] - i[0]) * r]), e.setLayout(o);
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/simpleLayout.js
var kE = Jo(SE, AE);
function AE(e, t) {
	e.eachSeriesByType(SE, function(e) {
		var t = e.get("layout"), n = e.coordinateSystem;
		if (n && n.type !== "view") {
			var r = e.getData(), i = [];
			F(n.dimensions, function(e) {
				i = i.concat(r.mapDimensionsAll(e));
			});
			for (var a = 0; a < r.count(); a++) {
				for (var o = [], s = !1, c = 0; c < i.length; c++) {
					var l = r.get(i[c], a);
					isNaN(l) || (s = !0), o.push(l);
				}
				s ? r.setItemLayout(a, n.dataToPoint(o)) : r.setItemLayout(a, [NaN, NaN]);
			}
			OE(r.graph, e);
		} else (!t || t === "none") && DE(e);
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/graphHelper.js
function jE(e) {
	var t = e.coordinateSystem;
	return TT(t) ? UT(t, e) : 1;
}
function ME(e) {
	var t = e.getVisual("symbolSize");
	return t instanceof Array && (t = (t[0] + t[1]) / 2), +t;
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/circularLayoutHelper.js
var NE = Math.PI, PE = [];
function FE(e, t, n, r) {
	var i = e.coordinateSystem;
	if (!(i && i.type !== "view")) {
		var a = i.getBoundingRect(), o = e.getData(), s = o.graph, c = a.width / 2 + a.x, l = a.height / 2 + a.y, u = Math.min(a.width, a.height) / 2, d = o.count();
		if (o.setLayout({
			cx: c,
			cy: l
		}), d) {
			if (n) {
				var f = i.pointToData(r), p = f[0], m = f[1], h = [p - c, m - l];
				He(h, h), Ve(h, h, u), n.setLayout([c + h[0], l + h[1]], !0), LE(n, e.get(["circular", "rotateLabel"]), c, l);
			}
			IE[t](e, s, o, u, c, l, d), s.eachEdge(function(t, n) {
				var r = me(t.getModel().get(["lineStyle", "curveness"]), xE(t, e, n), 0), i = Pe(t.node1.getLayout()), a = Pe(t.node2.getLayout()), o, s = (i[0] + a[0]) / 2, u = (i[1] + a[1]) / 2;
				+r && (r *= 3, o = [c * r + s * (1 - r), l * r + u * (1 - r)]), t.setLayout([
					i,
					a,
					o
				]);
			});
		}
	}
}
var IE = {
	value: function(e, t, n, r, i, a, o) {
		var s = 0, c = n.getSum("value"), l = Math.PI * 2 / (c || o);
		t.eachNode(function(e) {
			var t = e.getValue("value"), n = l * (c ? t : 1) / 2;
			s += n, e.setLayout([r * Math.cos(s) + i, r * Math.sin(s) + a]), s += n;
		});
	},
	symbolSize: function(e, t, n, r, i, a, o) {
		var s = 0;
		PE.length = o;
		var c = jE(e);
		t.eachNode(function(e) {
			var t = ME(e);
			isNaN(t) && (t = 2), t < 0 && (t = 0), t *= c;
			var n = Math.asin(t / 2 / r);
			isNaN(n) && (n = NE / 2), PE[e.dataIndex] = n, s += n * 2;
		});
		var l = (2 * NE - s) / o / 2, u = 0;
		t.eachNode(function(e) {
			var t = l + PE[e.dataIndex];
			u += t, (!e.getLayout() || !e.getLayout().fixed) && e.setLayout([r * Math.cos(u) + i, r * Math.sin(u) + a]), u += t;
		});
	}
};
function LE(e, t, n, r) {
	var i = e.getGraphicEl();
	if (i) {
		var a = e.getModel().get(["label", "rotate"]) || 0, o = i.getSymbolPath();
		if (t) {
			var s = e.getLayout(), c = Math.atan2(s[1] - r, s[0] - n);
			c < 0 && (c = Math.PI * 2 + c);
			var l = s[0] < n;
			l && (c -= Math.PI);
			var u = l ? "left" : "right";
			o.setTextConfig({
				rotation: -c,
				position: u,
				origin: "center"
			});
			var d = o.ensureState("emphasis");
			j(d.textConfig ||= {}, { position: u });
		} else o.setTextConfig({ rotation: a *= Math.PI / 180 });
	}
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/circularLayout.js
var RE = Jo(SE, zE);
function zE(e) {
	e.eachSeriesByType("graph", function(e) {
		e.get("layout") === "circular" && FE(e, "symbolSize");
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/forceHelper.js
var BE = Le;
function VE(e, t, n) {
	for (var r = e, i = t, a = n.rect, o = a.width, s = a.height, c = [a.x + o / 2, a.y + s / 2], l = n.gravity == null ? .1 : n.gravity, u = 0; u < r.length; u++) {
		var d = r[u];
		d.p ||= Me(o * (Math.random() - .5) + c[0], s * (Math.random() - .5) + c[1]), d.pp = Pe(d.p), d.edges = null;
	}
	var f = n.friction == null ? .6 : n.friction, p = f, m, h;
	return {
		warmUp: function() {
			p = f * .8;
		},
		setFixed: function(e) {
			r[e].fixed = !0;
		},
		setUnfixed: function(e) {
			r[e].fixed = !1;
		},
		beforeStep: function(e) {
			m = e;
		},
		afterStep: function(e) {
			h = e;
		},
		step: function(e) {
			m && m(r, i);
			for (var t = [], n = r.length, a = 0; a < i.length; a++) {
				var o = i[a];
				if (!o.ignoreForceLayout) {
					var s = o.n1, u = o.n2;
					Re(t, u.p, s.p);
					var d = ze(t) - o.d, f = u.w / (s.w + u.w);
					isNaN(f) && (f = 0), He(t, t), !s.fixed && BE(s.p, s.p, t, f * d * p), !u.fixed && BE(u.p, u.p, t, -(1 - f) * d * p);
				}
			}
			for (var a = 0; a < n; a++) {
				var g = r[a];
				g.fixed || (Re(t, c, g.p), BE(g.p, g.p, t, l * p));
			}
			for (var a = 0; a < n; a++) for (var s = r[a], _ = a + 1; _ < n; _++) {
				var u = r[_];
				Re(t, u.p, s.p);
				var d = ze(t);
				d === 0 && (Fe(t, Math.random() - .5, Math.random() - .5), d = 1);
				var v = (s.rep + u.rep) / d / d;
				!s.fixed && BE(s.pp, s.pp, t, v), !u.fixed && BE(u.pp, u.pp, t, -v);
			}
			for (var y = [], a = 0; a < n; a++) {
				var g = r[a];
				g.fixed || (Re(y, g.p, g.pp), BE(g.p, g.p, y, p), Ne(g.pp, g.p));
			}
			p *= .992;
			var b = p < .01;
			h && h(r, i, b), e && e(b);
		}
	};
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/forceLayout.js
var HE = Jo(SE, UE);
function UE(e) {
	e.eachSeriesByType(SE, function(e) {
		var t = e.coordinateSystem;
		if (!(t && t.type !== "view")) {
			if (e.get("layout") === "force") {
				var n = e.preservedPoints || {}, r = e.getGraph(), i = r.data, a = r.edgeData, o = e.getModel("force"), s = o.get("initLayout");
				e.preservedPoints ? i.each(function(e) {
					var t = i.getId(e);
					i.setItemLayout(e, n[t] || [NaN, NaN]);
				}) : !s || s === "none" ? DE(e) : s === "circular" && FE(e, "value");
				var c = i.getDataExtent("value"), l = a.getDataExtent("value"), u = o.get("repulsion"), d = o.get("edgeLength"), f = B(u) ? u : [u, u], p = B(d) ? d : [d, d];
				p = [p[1], p[0]];
				var m = i.mapArray("value", function(e, t) {
					var n = i.getItemLayout(t), r = Ra(e, c, f);
					return isNaN(r) && (r = (f[0] + f[1]) / 2), {
						w: r,
						rep: r,
						fixed: i.getItemModel(t).get("fixed"),
						p: !n || isNaN(n[0]) || isNaN(n[1]) ? null : n
					};
				}), h = VE(m, a.mapArray("value", function(t, n) {
					var i = r.getEdgeByIndex(n), a = Ra(t, l, p);
					isNaN(a) && (a = (p[0] + p[1]) / 2);
					var o = i.getModel(), s = me(i.getModel().get(["lineStyle", "curveness"]), -xE(i, e, n, !0), 0);
					return {
						n1: m[i.node1.dataIndex],
						n2: m[i.node2.dataIndex],
						d: a,
						curveness: s,
						ignoreForceLayout: o.get("ignoreForceLayout")
					};
				}), {
					rect: t.getBoundingRect(),
					gravity: o.get("gravity"),
					friction: o.get("friction")
				});
				h.beforeStep(function(e, t) {
					for (var n = 0, i = e.length; n < i; n++) e[n].fixed && Ne(e[n].p, r.getNodeByIndex(n).getLayout());
				}), h.afterStep(function(e, t, a) {
					for (var o = 0, s = e.length; o < s; o++) e[o].fixed || r.getNodeByIndex(o).setLayout(e[o].p), n[i.getId(o)] = e[o].p;
					for (var o = 0, s = t.length; o < s; o++) {
						var c = t[o], l = r.getEdgeByIndex(o), u = c.n1.p, d = c.n2.p, f = l.getLayout();
						f = f ? f.slice() : [], f[0] = f[0] || [], f[1] = f[1] || [], Ne(f[0], u), Ne(f[1], d), +c.curveness && (f[2] = [(u[0] + d[0]) / 2 - (u[1] - d[1]) * c.curveness, (u[1] + d[1]) / 2 - (d[0] - u[0]) * c.curveness]), l.setLayout(f);
					}
				}), e.forceLayout = h, e.preservedPoints = n, h.step();
			} else e.forceLayout = null;
		}
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/createView.js
function WE(e, t, n) {
	var r = Jm(e, t);
	return Km(e, Gm(j(e.getBoxLayoutParams(), { aspect: n }), r.refContainer), n);
}
function GE(e, t) {
	var n = [];
	return e.eachSeriesByType("graph", function(e) {
		zm({
			targetModel: e,
			coordSysType: "view",
			coordSysProvider: r,
			isDefaultDataCoordSys: !0
		});
		function r() {
			var r = e.getData(), i = r.mapArray(function(e) {
				var t = r.getItemModel(e);
				return [+t.get("x"), +t.get("y")];
			}), a = [], o = [];
			rc(i, a, o), o[0] - a[0] === 0 && (o[0] += 1, --a[0]), o[1] - a[1] === 0 && (o[1] += 1, --a[1]);
			var s = (o[0] - a[0]) / (o[1] - a[1]), c = WE(e, t, s);
			isNaN(s) && (a = [c.x, c.y], o = [c.x + c.width, c.y + c.height]);
			var l = o[0] - a[0], u = o[1] - a[1], d = JT(e, t, a[0], a[1], l, u, c);
			return n.push(d), d;
		}
	}), n;
}
//#endregion
//#region node_modules/echarts/lib/chart/helper/LinePath.js
var KE = qd.prototype, qE = Zd.prototype, JE = function() {
	function e() {
		this.x1 = 0, this.y1 = 0, this.x2 = 0, this.y2 = 0, this.percent = 1;
	}
	return e;
}();
(function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t;
})(JE);
function YE(e) {
	return isNaN(+e.cpx1) || isNaN(+e.cpy1);
}
var XE = function(e) {
	r(t, e);
	function t(t) {
		var n = e.call(this, t) || this;
		return n.type = "ec-line", n;
	}
	return t.prototype.getDefaultStyle = function() {
		return {
			stroke: Q.color.neutral99,
			fill: null
		};
	}, t.prototype.getDefaultShape = function() {
		return new JE();
	}, t.prototype.buildPath = function(e, t) {
		YE(t) ? KE.buildPath.call(this, e, t) : qE.buildPath.call(this, e, t);
	}, t.prototype.pointAt = function(e) {
		return YE(this.shape) ? KE.pointAt.call(this, e) : qE.pointAt.call(this, e);
	}, t.prototype.tangentAt = function(e) {
		var t = this.shape, n = YE(t) ? [t.x2 - t.x1, t.y2 - t.y1] : qE.tangentAt.call(this, e);
		return He(n, n);
	}, t;
}(Z), ZE = ["fromSymbol", "toSymbol"];
function QE(e) {
	return "_" + e + "Type";
}
function $E(e, t, n) {
	var r = t.getItemVisual(n, e);
	if (!r || r === "none") return r;
	var i = t.getItemVisual(n, e + "Size"), a = t.getItemVisual(n, e + "Rotate"), o = t.getItemVisual(n, e + "Offset"), s = t.getItemVisual(n, e + "KeepAspect"), c = Oy(i), l = ky(o || 0, c);
	return r + c + l + (a || "") + (s || "");
}
function eD(e, t, n) {
	var r = t.getItemVisual(n, e);
	if (r && r !== "none") {
		var i = t.getItemVisual(n, e + "Size"), a = t.getItemVisual(n, e + "Rotate"), o = t.getItemVisual(n, e + "Offset"), s = t.getItemVisual(n, e + "KeepAspect"), c = Oy(i), l = ky(o || 0, c), u = Dy(r, -c[0] / 2 + l[0], -c[1] / 2 + l[1], c[0], c[1], null, s);
		return u.__specifiedRotation = a == null || isNaN(a) ? void 0 : a * Math.PI / 180 || 0, u.name = e, u;
	}
}
function tD(e) {
	var t = new XE({
		name: "line",
		subPixelOptimize: !0
	});
	return nD(t.shape, e), t;
}
function nD(e, t) {
	e.x1 = t[0][0], e.y1 = t[0][1], e.x2 = t[1][0], e.y2 = t[1][1], e.percent = 1;
	var n = t[2];
	n ? (e.cpx1 = n[0], e.cpy1 = n[1]) : (e.cpx1 = NaN, e.cpy1 = NaN);
}
var rD = function(e) {
	r(t, e);
	function t(t, n, r) {
		var i = e.call(this) || this;
		return i._createLine(t, n, r), i;
	}
	return t.prototype._createLine = function(e, t, n) {
		var r = e.hostModel, i = e.getItemLayout(t), a = e.getItemVisual(t, "z2"), o = tD(i);
		o.shape.percent = 0, bf(o, {
			z2: G(a, 0),
			shape: { percent: 1 }
		}, r, t), this.add(o), F(ZE, function(n) {
			var r = eD(n, e, t);
			this.add(r), this[QE(n)] = $E(n, e, t);
		}, this), this._updateCommonStl(e, t, n);
	}, t.prototype.updateData = function(e, t, n) {
		var r = e.hostModel, i = this.childOfName("line"), a = e.getItemLayout(t), o = { shape: {} };
		nD(o.shape, a), yf(i, o, r, t), F(ZE, function(n) {
			var r = $E(n, e, t), i = QE(n);
			if (this[i] !== r) {
				this.remove(this.childOfName(n));
				var a = eD(n, e, t);
				this.add(a);
			}
			this[i] = r;
		}, this), this._updateCommonStl(e, t, n);
	}, t.prototype.getLinePath = function() {
		return this.childAt(0);
	}, t.prototype._updateCommonStl = function(e, t, n) {
		var r = e.hostModel, i = this.childOfName("line"), a = n && n.emphasisLineStyle, o = n && n.blurLineStyle, s = n && n.selectLineStyle, c = n && n.labelStatesModels, l = n && n.emphasisDisabled, u = n && n.focus, d = n && n.blurScope;
		if (!n || e.hasItemOption) {
			var f = e.getItemModel(t), p = f.getModel("emphasis");
			a = p.getModel("lineStyle").getLineStyle(), o = f.getModel(["blur", "lineStyle"]).getLineStyle(), s = f.getModel(["select", "lineStyle"]).getLineStyle(), l = p.get("disabled"), u = p.get("focus"), d = p.get("blurScope"), c = wp(f);
		}
		var m = e.getItemVisual(t, "style"), h = m.stroke;
		i.useStyle(m), i.style.fill = null, i.style.strokeNoScale = !0, i.ensureState("emphasis").style = a, i.ensureState("blur").style = o, i.ensureState("select").style = s, F(ZE, function(e) {
			var t = this.childOfName(e);
			if (t) {
				t.setColor(h), t.style.opacity = m.opacity;
				for (var n = 0; n < Yl.length; n++) {
					var r = Yl[n], a = i.getState(r);
					if (a) {
						var o = a.style || {}, s = t.ensureState(r), c = s.style ||= {};
						o.stroke != null && (c[t.__isEmptyBrush ? "stroke" : "fill"] = o.stroke), o.opacity != null && (c.opacity = o.opacity);
					}
				}
				t.markRedraw();
			}
		}, this);
		var g = r.getRawValue(t);
		Cp(this, c, {
			labelDataIndex: t,
			labelFetcher: { getFormattedLabel: function(t, n) {
				return r.getFormattedLabel(t, n, e.dataType);
			} },
			inheritColor: h || Q.color.neutral99,
			defaultOpacity: m.opacity,
			defaultText: (g == null ? e.getName(t) : isFinite(g) ? Wa(g, 10) : g) + ""
		});
		var _ = this.getTextContent();
		if (_) {
			var v = c.normal;
			_.__align = _.style.align, _.__verticalAlign = _.style.verticalAlign, _.__position = v.get("position") || "middle";
			var y = v.get("distance");
			B(y) || (y = [y, y]), _.__labelDistance = y;
		}
		this.setTextConfig({
			position: null,
			local: !0,
			inside: !1
		}), Bu(this, u, d, l);
	}, t.prototype.highlight = function() {
		xu(this);
	}, t.prototype.downplay = function() {
		Su(this);
	}, t.prototype.updateLayout = function(e, t) {
		this.childOfName("line").stopAnimation(), this.setLinePoints(e.getItemLayout(t));
	}, t.prototype.setLinePoints = function(e) {
		var t = this.childOfName("line");
		nD(t.shape, e), t.dirty();
	}, t.prototype.beforeUpdate = function() {
		var e = this, t = e.childOfName("fromSymbol"), n = e.childOfName("toSymbol"), r = e.getTextContent();
		if (!t && !n && (!r || r.ignore)) return;
		for (var i = 1, a = this.parent; a;) a.scaleX && (i /= a.scaleX), a = a.parent;
		var o = e.childOfName("line");
		if (!this.__dirty && !o.__dirty) return;
		var s = o.shape.percent, c = o.pointAt(0), l = o.pointAt(s), u = Re([], l, c);
		He(u, u);
		function d(e, t) {
			var n = e.__specifiedRotation;
			if (n == null) {
				var r = o.tangentAt(t);
				e.attr("rotation", (t === 1 ? -1 : 1) * Math.PI / 2 - Math.atan2(r[1], r[0]));
			} else e.attr("rotation", n);
		}
		if (t && (t.setPosition(c), d(t, 0), t.scaleX = t.scaleY = i * s, t.markRedraw()), n && (n.setPosition(l), d(n, 1), n.scaleX = n.scaleY = i * s, n.markRedraw()), r && !r.ignore) {
			r.x = r.y = 0, r.originX = r.originY = 0;
			var f = void 0, p = void 0, m = r.__labelDistance, h = m[0] * i, g = m[1] * i, _ = s / 2, v = o.tangentAt(_), y = [v[1], -v[0]], b = o.pointAt(_);
			y[1] > 0 && (y[0] = -y[0], y[1] = -y[1]);
			var x = v[0] < 0 ? -1 : 1;
			if (r.__position !== "start" && r.__position !== "end") {
				var S = -Math.atan2(v[1], v[0]);
				l[0] < c[0] && (S = Math.PI + S), r.rotation = S;
			}
			var C = void 0;
			switch (r.__position) {
				case "insideStartTop":
				case "insideMiddleTop":
				case "insideEndTop":
				case "middle":
					C = -g, p = "bottom";
					break;
				case "insideStartBottom":
				case "insideMiddleBottom":
				case "insideEndBottom":
					C = g, p = "top";
					break;
				default: C = 0, p = "middle";
			}
			switch (r.__position) {
				case "end":
					r.x = u[0] * h + l[0], r.y = u[1] * g + l[1], f = u[0] > .8 ? "left" : u[0] < -.8 ? "right" : "center", p = u[1] > .8 ? "top" : u[1] < -.8 ? "bottom" : "middle";
					break;
				case "start":
					r.x = -u[0] * h + c[0], r.y = -u[1] * g + c[1], f = u[0] > .8 ? "right" : u[0] < -.8 ? "left" : "center", p = u[1] > .8 ? "bottom" : u[1] < -.8 ? "top" : "middle";
					break;
				case "insideStartTop":
				case "insideStart":
				case "insideStartBottom":
					r.x = h * x + c[0], r.y = c[1] + C, f = v[0] < 0 ? "right" : "left", r.originX = -h * x, r.originY = -C;
					break;
				case "insideMiddleTop":
				case "insideMiddle":
				case "insideMiddleBottom":
				case "middle":
					r.x = b[0], r.y = b[1] + C, f = "center", r.originY = -C;
					break;
				case "insideEndTop":
				case "insideEnd":
				case "insideEndBottom": r.x = -h * x + l[0], r.y = l[1] + C, f = v[0] >= 0 ? "right" : "left", r.originX = h * x, r.originY = -C;
			}
			r.scaleX = r.scaleY = i, r.setStyle({
				verticalAlign: r.__verticalAlign || p,
				align: r.__align || f
			});
		}
	}, t;
}(ba), iD = function() {
	function e(e) {
		this.group = new ba(), this._LineCtor = e || rD;
	}
	return e.prototype.updateData = function(e) {
		var t = this;
		this._progressiveEls = null;
		var n = this, r = n.group, i = n._lineData;
		n._lineData = e, i || r.removeAll();
		var a = oD(e);
		e.diff(i).add(function(n) {
			t._doAdd(e, n, a);
		}).update(function(n, r) {
			t._doUpdate(i, e, r, n, a);
		}).remove(function(e) {
			r.remove(i.getItemGraphicEl(e));
		}).execute();
	}, e.prototype.updateLayout = function() {
		var e = this._lineData;
		e && e.eachItemGraphicEl(function(t, n) {
			t.updateLayout(e, n);
		}, this);
	}, e.prototype.incrementalPrepareUpdate = function(e) {
		this._seriesScope = oD(e), this._lineData = null, this.group.removeAll();
	}, e.prototype.incrementalUpdate = function(e, t, n) {
		this._progressiveEls = [];
		function r(e) {
			!e.isGroup && !aD(e) && (e.incremental = n, e.ensureState("emphasis").hoverLayer = 2);
		}
		for (var i = e.start; i < e.end; i++) if (cD(t.getItemLayout(i))) {
			var a = new this._LineCtor(t, i, this._seriesScope);
			a.traverse(r), this.group.add(a), t.setItemGraphicEl(i, a), this._progressiveEls.push(a);
		}
	}, e.prototype.remove = function() {
		this.group.removeAll();
	}, e.prototype.eachRendered = function(e) {
		sp(this._progressiveEls || this.group, e);
	}, e.prototype._doAdd = function(e, t, n) {
		if (cD(e.getItemLayout(t))) {
			var r = new this._LineCtor(e, t, n);
			e.setItemGraphicEl(t, r), this.group.add(r);
		}
	}, e.prototype._doUpdate = function(e, t, n, r, i) {
		var a = e.getItemGraphicEl(n);
		cD(t.getItemLayout(r)) ? (a ? a.updateData(t, r, i) : a = new this._LineCtor(t, r, i), t.setItemGraphicEl(r, a), this.group.add(a)) : this.group.remove(a);
	}, e;
}();
function aD(e) {
	return e.animators && e.animators.length > 0;
}
function oD(e) {
	var t = e.hostModel, n = t.getModel("emphasis");
	return {
		lineStyle: t.getModel("lineStyle").getLineStyle(),
		emphasisLineStyle: n.getModel(["lineStyle"]).getLineStyle(),
		blurLineStyle: t.getModel(["blur", "lineStyle"]).getLineStyle(),
		selectLineStyle: t.getModel(["select", "lineStyle"]).getLineStyle(),
		emphasisDisabled: n.get("disabled"),
		blurScope: n.get("blurScope"),
		focus: n.get("focus"),
		labelStatesModels: wp(t)
	};
}
function sD(e) {
	return isNaN(e[0]) || isNaN(e[1]);
}
function cD(e) {
	return e && !sD(e[0]) && !sD(e[1]);
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/adjustEdge.js
var lD = [], uD = [], dD = [], fD = $n, pD = Ke, mD = Math.abs;
function hD(e, t, n) {
	for (var r = e[0], i = e[1], a = e[2], o = Infinity, s, c = n * n, l = .1, u = .1; u <= .9; u += .1) {
		lD[0] = fD(r[0], i[0], a[0], u), lD[1] = fD(r[1], i[1], a[1], u);
		var d = mD(pD(lD, t) - c);
		d < o && (o = d, s = u);
	}
	for (var f = 0; f < 32; f++) {
		var p = s + l;
		uD[0] = fD(r[0], i[0], a[0], s), uD[1] = fD(r[1], i[1], a[1], s), dD[0] = fD(r[0], i[0], a[0], p), dD[1] = fD(r[1], i[1], a[1], p);
		var d = pD(uD, t) - c;
		if (mD(d) < .01) break;
		var m = pD(dD, t) - c;
		l /= 2, d < 0 ? m >= 0 ? s += l : s -= l : m >= 0 ? s -= l : s += l;
	}
	return s;
}
function gD(e, t) {
	var n = [], r = rr, i = [
		[],
		[],
		[]
	], a = [[], []], o = [];
	t /= 2, e.eachEdge(function(e, s) {
		var c = e.getLayout(), l = e.getVisual("fromSymbol"), u = e.getVisual("toSymbol");
		c.__original || (c.__original = [Pe(c[0]), Pe(c[1])], c[2] && c.__original.push(Pe(c[2])));
		var d = c.__original;
		if (c[2] != null) {
			if (Ne(i[0], d[0]), Ne(i[1], d[2]), Ne(i[2], d[1]), l && l !== "none") {
				var f = ME(e.node1), p = hD(i, d[0], f * t);
				r(i[0][0], i[1][0], i[2][0], p, n), i[0][0] = n[3], i[1][0] = n[4], r(i[0][1], i[1][1], i[2][1], p, n), i[0][1] = n[3], i[1][1] = n[4];
			}
			if (u && u !== "none") {
				var f = ME(e.node2), p = hD(i, d[1], f * t);
				r(i[0][0], i[1][0], i[2][0], p, n), i[1][0] = n[1], i[2][0] = n[2], r(i[0][1], i[1][1], i[2][1], p, n), i[1][1] = n[1], i[2][1] = n[2];
			}
			Ne(c[0], i[0]), Ne(c[1], i[2]), Ne(c[2], i[1]);
		} else {
			if (Ne(a[0], d[0]), Ne(a[1], d[1]), Re(o, a[1], a[0]), He(o, o), l && l !== "none") {
				var f = ME(e.node1);
				Le(a[0], a[0], o, f * t);
			}
			if (u && u !== "none") {
				var f = ME(e.node2);
				Le(a[1], a[1], o, -f * t);
			}
			Ne(c[0], a[0]), Ne(c[1], a[1]);
		}
	});
}
//#endregion
//#region node_modules/echarts/lib/component/helper/thumbnailBridge.js
var _D = Y();
function vD(e) {
	if (e) return _D(e).bridge;
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/GraphView.js
var yD = function(e) {
	r(t, e);
	function t() {
		var t = e !== null && e.apply(this, arguments) || this;
		return t.type = SE, t;
	}
	return t.prototype.init = function(e, t) {
		var n = new RC(), r = new iD(), i = this.group, a = new ba();
		this._controller = new Bw(t.getZr()), a.add(n.group), a.add(r.group), i.add(a), this._symbolDraw = n, this._lineDraw = r, this._mainGroup = a, this._firstRender = !0;
	}, t.prototype.render = function(e, t, n) {
		var r = this, i = jT(e), a = !1;
		this._model = e, this._api = n, this._active = !0;
		var o = this._mainGroup, s = this._getThumbnailInfo();
		s && s.bridge.reset(n);
		var c = this._symbolDraw, l = this._lineDraw;
		i && ET(o, 2, i, this._firstRender ? null : e), gD(e.getGraph(), jE(e));
		var u = e.getData();
		c.updateData(u);
		var d = e.getEdgeData();
		l.updateData(d), this._updateNodeAndLinkScale(), i && WT(e, n, this._controller, function(t, n, r) {
			return e.coordinateSystem.containPoint([n, r]);
		}, null), clearTimeout(this._layoutTimeout);
		var f = e.forceLayout, p = e.get(["force", "layoutAnimation"]);
		f && (a = !0, this._startForceLayoutIteration(f, n, p));
		var m = e.get("layout");
		u.graph.eachNode(function(t) {
			var i = t.dataIndex, a = t.getGraphicEl(), o = t.getModel();
			if (a) {
				a.off("drag").off("dragend");
				var s = o.get("draggable");
				s && a.on("drag", function(o) {
					switch (m) {
						case "force":
							f.warmUp(), !r._layouting && r._startForceLayoutIteration(f, n, p), f.setFixed(i), u.setItemLayout(i, [a.x, a.y]);
							break;
						case "circular":
							u.setItemLayout(i, [a.x, a.y]), t.setLayout({ fixed: !0 }, !0), FE(e, "symbolSize", t, [o.offsetX, o.offsetY]), r.updateLayout(e);
							break;
						default: u.setItemLayout(i, [a.x, a.y]), OE(e.getGraph(), e), r.updateLayout(e);
					}
				}).on("dragend", function() {
					f && f.setUnfixed(i);
				}), a.setDraggable(s, !!o.get("cursor")), o.get(["emphasis", "focus"]) === "adjacency" && (Al(a).focus = t.getAdjacentDataIndices());
			}
		}), u.graph.eachEdge(function(e) {
			var t = e.getGraphicEl(), n = e.getModel().get(["emphasis", "focus"]);
			t && n === "adjacency" && (Al(t).focus = {
				edge: [e.dataIndex],
				node: [e.node1.dataIndex, e.node2.dataIndex]
			});
		});
		var h = e.get("layout") === "circular" && e.get(["circular", "rotateLabel"]), g = u.getLayout("cx"), _ = u.getLayout("cy");
		u.graph.eachNode(function(e) {
			LE(e, h, g, _);
		}), this._firstRender = !1, a || this._renderThumbnail(e, n, this._symbolDraw, this._lineDraw);
	}, t.prototype.dispose = function() {
		this.remove(), this._controller && this._controller.dispose();
	}, t.prototype._startForceLayoutIteration = function(e, t, n) {
		var r = this, i = !1;
		(function a() {
			e.step(function(e) {
				r.updateLayout(r._model), (e || !i) && (i = !0, r._renderThumbnail(r._model, t, r._symbolDraw, r._lineDraw)), (r._layouting = !e) && (n ? r._layoutTimeout = setTimeout(a, 16) : a());
			});
		})();
	}, t.prototype.__updateOnOwnRoam = function(e, t, n) {
		var r = jT(t);
		this._active && r && (ET(this._mainGroup, 2, r, null), qT(e) && (this._updateNodeAndLinkScale(), gD(t.getGraph(), jE(t)), this._lineDraw.updateLayout(), n.updateLabelLayout()), this._updateThumbnailWindow());
	}, t.prototype._updateNodeAndLinkScale = function() {
		var e = this._model, t = e.getData(), n = jE(e);
		t.eachItemGraphicEl(function(e, t) {
			e && e.setSymbolScale(n);
		});
	}, t.prototype.updateLayout = function(e) {
		this._active && (gD(e.getGraph(), jE(e)), this._symbolDraw.updateLayout(), this._lineDraw.updateLayout());
	}, t.prototype.remove = function() {
		this._active = !1, clearTimeout(this._layoutTimeout), this._layouting = !1, this._layoutTimeout = null, this._symbolDraw && this._symbolDraw.remove(), this._lineDraw && this._lineDraw.remove(), this._controller && this._controller.disable();
	}, t.prototype._getThumbnailInfo = function() {
		var e = this._model, t = e.coordinateSystem;
		if (t.type === "view") {
			var n = vD(e);
			if (n) return {
				bridge: n,
				coordSys: t
			};
		}
	}, t.prototype._updateThumbnailWindow = function() {
		var e = this._getThumbnailInfo();
		e && e.bridge.updateWindow(eT(null, e.coordSys), this._api);
	}, t.prototype._renderThumbnail = function(e, t, n, r) {
		var i = this._getThumbnailInfo();
		if (i) {
			var a = new ba(), o = n.group.children(), s = r.group.children(), c = new ba(), l = new ba();
			a.add(l), a.add(c);
			for (var u = 0; u < o.length; u++) {
				var d = o[u], f = d.children()[0], p = d.x, m = d.y, h = j(k(f.shape), {
					width: f.scaleX,
					height: f.scaleY,
					x: p - f.scaleX / 2,
					y: m - f.scaleY / 2
				}), g = k(f.style), _ = new f.constructor({
					shape: h,
					style: g,
					z2: 151
				});
				l.add(_);
			}
			for (var u = 0; u < s.length; u++) {
				var d = s[u], v = d.children()[0], g = k(v.style), h = k(v.shape), y = new XE({
					style: g,
					shape: h,
					z2: 151
				});
				c.add(y);
			}
			i.bridge.renderContent({
				api: t,
				roamType: e.get("roam"),
				viewportRect: null,
				group: a,
				targetTrans: eT(null, i.coordSys)
			});
		}
	}, t.type = SE, t;
}(uv), bD = Jo(SE, xD);
function xD(e) {
	var t = e.findComponents({ mainType: "legend" });
	t && t.length && e.eachSeriesByType(SE, function(e) {
		var n = e.getCategoriesData(), r = e.getGraph().data, i = n.mapArray(n.getName);
		r.filterSelf(function(e) {
			var n = r.getItemModel(e).getShallow("category");
			if (n != null) {
				U(n) && (n = i[n]);
				for (var a = 0; a < t.length; a++) if (!t[a].isSelected(n)) return !1;
			}
			return !0;
		});
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/categoryVisual.js
var SD = Jo(SE, CD);
function CD(e) {
	var t = {};
	e.eachSeriesByType(SE, function(e) {
		var n = e.getCategoriesData(), r = e.getData(), i = {};
		n.each(function(r) {
			var a = n.getName(r);
			i["ec-" + a] = r;
			var o = n.getItemModel(r), s = o.getModel("itemStyle").getItemStyle();
			s.fill ||= e.getColorFromPalette(a, t), n.setItemVisual(r, "style", s);
			for (var c = [
				"symbol",
				"symbolSize",
				"symbolKeepAspect"
			], l = 0; l < c.length; l++) {
				var u = o.getShallow(c[l], !0);
				u != null && n.setItemVisual(r, c[l], u);
			}
		}), n.count() && r.each(function(e) {
			var t = r.getItemModel(e).getShallow("category");
			if (t != null) {
				H(t) && (t = i["ec-" + t]);
				var a = n.getItemVisual(t, "style");
				j(r.ensureUniqueItemVisual(e, "style"), a);
				for (var o = [
					"symbol",
					"symbolSize",
					"symbolKeepAspect"
				], s = 0; s < o.length; s++) r.setItemVisual(e, o[s], n.getItemVisual(t, o[s]));
			}
		});
	});
}
//#endregion
//#region node_modules/echarts/lib/chart/graph/install.js
function wD(e) {
	e.registerChartView(yD), e.registerSeriesModel(CE), e.registerProcessor(bD), e.registerVisual(SD), e.registerVisual(TE), e.registerLayout(kE), e.registerLayout(e.PRIORITY.VISUAL.POST_CHART_LAYOUT, RE), e.registerLayout(HE), e.registerCoordinateSystem("graphView", {
		dimensions: Qw.dimensions,
		create: GE
	}), e.registerAction({
		type: "focusNodeAdjacency",
		event: "focusNodeAdjacency",
		update: "series:focusNodeAdjacency"
	}, Ae), e.registerAction({
		type: "unfocusNodeAdjacency",
		event: "unfocusNodeAdjacency",
		update: "series:unfocusNodeAdjacency"
	}, Ae), GT(e, Ml, SE);
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/BaseAxisPointer.js
var TD = Y(), ED = k, DD = R, OD = function() {
	function e() {
		this._dragging = !1, this.animationThreshold = 15;
	}
	return e.prototype.render = function(e, t, n, r) {
		var i = t.get("value"), a = t.get("status");
		if (this._axisModel = e, this._axisPointerModel = t, this._api = n, r || this._lastValue !== i || this._lastStatus !== a) {
			this._lastValue = i, this._lastStatus = a;
			var o = this._group, s = this._handle;
			if (!a || a === "hide") o && o.hide(), s && s.hide();
			else {
				o && o.show(), s && s.show();
				var c = {};
				this.makeElOption(c, i, e, t, n);
				var l = c.graphicKey;
				l !== this._lastGraphicKey && this.clear(n), this._lastGraphicKey = l;
				var u = this._moveAnimation = this.determineAnimation(e, t);
				if (!o) o = this._group = new ba(), this.createPointerEl(o, c, e, t), this.createLabelEl(o, c, e, t), n.getZr().add(o);
				else {
					var d = z(kD, t, u);
					this.updatePointerEl(o, c, d), this.updateLabelEl(o, c, d, t);
				}
				ND(o, t, !0), this._renderHandle(i);
			}
		}
	}, e.prototype.remove = function(e) {
		this.clear(e);
	}, e.prototype.dispose = function(e) {
		this.clear(e);
	}, e.prototype.determineAnimation = function(e, t) {
		var n = t.get("animation"), r = e.axis, i = r.type === "category", a = t.get("snap");
		if (!a && !i) return !1;
		if (n === "auto" || n == null) {
			var o = this.animationThreshold;
			if (i && pC(r).w > o) return !0;
			if (a) {
				var s = Aw(e).seriesDataCount, c = r.getExtent();
				return Math.abs(c[0] - c[1]) / s > o;
			}
			return !1;
		}
		return n === !0;
	}, e.prototype.makeElOption = function(e, t, n, r, i) {}, e.prototype.createPointerEl = function(e, t, n, r) {
		var i = t.pointer;
		if (i) {
			var a = TD(e).pointerEl = new Ef[i.type](ED(t.pointer));
			e.add(a);
		}
	}, e.prototype.createLabelEl = function(e, t, n, r) {
		if (t.label) {
			var i = TD(e).labelEl = new gl(ED(t.label));
			e.add(i), jD(i, r);
		}
	}, e.prototype.updatePointerEl = function(e, t, n) {
		var r = TD(e).pointerEl;
		r && t.pointer && (r.setStyle(t.pointer.style), n(r, { shape: t.pointer.shape }));
	}, e.prototype.updateLabelEl = function(e, t, n, r) {
		var i = TD(e).labelEl;
		i && (i.setStyle(t.label.style), n(i, {
			x: t.label.x,
			y: t.label.y
		}), jD(i, r));
	}, e.prototype._renderHandle = function(e) {
		if (!this._dragging && this.updateHandleTransform) {
			var t = this._axisPointerModel, n = this._api.getZr(), r = this._handle, i = t.getModel("handle"), a = t.get("status");
			if (!i.get("show") || !a || a === "hide") r && n.remove(r), this._handle = null;
			else {
				var o;
				this._handle || (o = !0, r = this._handle = Zf(i.get("icon"), {
					cursor: "move",
					draggable: !0,
					onmousemove: function(e) {
						Ct(e.event);
					},
					onmousedown: DD(this._onHandleDragMove, this, 0, 0),
					drift: DD(this._onHandleDragMove, this),
					ondragend: DD(this._onHandleDragEnd, this)
				}), n.add(r)), ND(r, t, !1), r.setStyle(i.getItemStyle(null, [
					"color",
					"borderColor",
					"borderWidth",
					"opacity",
					"shadowColor",
					"shadowBlur",
					"shadowOffsetX",
					"shadowOffsetY"
				]));
				var s = i.get("size");
				B(s) || (s = [s, s]), r.scaleX = s[0] / 2, r.scaleY = s[1] / 2, bv(this, "_doDispatchAxisPointer", i.get("throttle") || 0, "fixRate"), this._moveHandleToValue(e, o);
			}
		}
	}, e.prototype._moveHandleToValue = function(e, t) {
		kD(this._axisPointerModel, !t && this._moveAnimation, this._handle, MD(this.getHandleTransform(e, this._axisModel, this._axisPointerModel)));
	}, e.prototype._onHandleDragMove = function(e, t) {
		var n = this._handle;
		if (n) {
			this._dragging = !0;
			var r = this.updateHandleTransform(MD(n), [e, t], this._axisModel, this._axisPointerModel);
			this._payloadInfo = r, n.stopAnimation(), n.attr(MD(r)), TD(n).lastProp = null, this._doDispatchAxisPointer();
		}
	}, e.prototype._doDispatchAxisPointer = function() {
		if (this._handle) {
			var e = this._payloadInfo, t = this._axisModel;
			this._api.dispatchAction({
				type: "updateAxisPointer",
				x: e.cursorPoint[0],
				y: e.cursorPoint[1],
				tooltipOption: e.tooltipOption,
				axesInfo: [{
					axisDim: t.axis.dim,
					axisIndex: t.componentIndex
				}]
			});
		}
	}, e.prototype._onHandleDragEnd = function() {
		if (this._dragging = !1, this._handle) {
			var e = this._axisPointerModel.get("value");
			this._moveHandleToValue(e), this._api.dispatchAction({ type: "hideTip" });
		}
	}, e.prototype.clear = function(e) {
		this._lastValue = null, this._lastStatus = null;
		var t = e.getZr(), n = this._group, r = this._handle;
		t && n && (this._lastGraphicKey = null, n && t.remove(n), r && t.remove(r), this._group = null, this._handle = null, this._payloadInfo = null), xv(this, "_doDispatchAxisPointer");
	}, e.prototype.doClear = function() {}, e.prototype.buildLabel = function(e, t, n) {
		return n ||= 0, {
			x: e[n],
			y: e[1 - n],
			width: t[n],
			height: t[1 - n]
		};
	}, e;
}();
function kD(e, t, n, r) {
	AD(TD(n).lastProp, r) || (TD(n).lastProp = r, t ? yf(n, r, e) : (n.stopAnimation(), n.attr(r)));
}
function AD(e, t) {
	if (W(e) && W(t)) {
		var n = !0;
		return F(t, function(t, r) {
			n &&= AD(e[r], t);
		}), !!n;
	}
	return e === t;
}
function jD(e, t) {
	e[t.get(["label", "show"]) ? "show" : "hide"]();
}
function MD(e) {
	return {
		x: e.x || 0,
		y: e.y || 0,
		rotation: e.rotation || 0
	};
}
function ND(e, t, n) {
	var r = t.get("z"), i = t.get("zlevel");
	e && e.traverse(function(e) {
		e.type !== "group" && (r != null && (e.z = r), i != null && (e.zlevel = i), e.silent = n);
	});
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/viewHelper.js
function PD(e) {
	var t = e.get("type"), n = e.getModel(t + "Style"), r;
	return t === "line" ? (r = n.getLineStyle(), r.fill = null) : t === "shadow" && (r = n.getAreaStyle(), r.stroke = null), r;
}
function FD(e, t, n, r, i) {
	var a = LD(n.get("value"), t.axis, t.ecModel, n.get("seriesDataIndices"), {
		precision: n.get(["label", "precision"]),
		formatter: n.get(["label", "formatter"])
	}), o = n.getModel("label"), s = Tm(o.get("padding") || 0), c = o.getFont(), l = Zi(a, c), u = i.position, d = l.width + s[1] + s[3], f = l.height + s[0] + s[2], p = i.align;
	p === "right" && (u[0] -= d), p === "center" && (u[0] -= d / 2);
	var m = i.verticalAlign;
	m === "bottom" && (u[1] -= f), m === "middle" && (u[1] -= f / 2), ID(u, d, f, r);
	var h = o.get("backgroundColor");
	(!h || h === "auto") && (h = t.get([
		"axisLine",
		"lineStyle",
		"color"
	])), e.label = {
		x: u[0],
		y: u[1],
		style: Tp(o, {
			text: a,
			font: c,
			fill: o.getTextColor(),
			padding: s,
			backgroundColor: h
		}),
		z2: 10
	};
}
function ID(e, t, n, r) {
	var i = r.getWidth(), a = r.getHeight();
	e[0] = Math.min(e[0] + t, i) - t, e[1] = Math.min(e[1] + n, a) - n, e[0] = Math.max(e[0], 0), e[1] = Math.max(e[1], 0);
}
function LD(e, t, n, r, i) {
	e = t.scale.parse(e);
	var a = t.scale.getLabel({ value: e }, { precision: i.precision }), o = i.formatter;
	if (o) {
		var s = {
			value: XS(t, { value: e }),
			axisDimension: t.dim,
			axisIndex: t.index,
			seriesData: []
		};
		F(r, function(e) {
			var t = n.getSeriesByIndex(e.seriesIndex), r = e.dataIndexInside, i = t && t.getDataParams(r);
			i && s.seriesData.push(i);
		}), H(o) ? a = o.replace("{value}", a) : V(o) && (a = o(s));
	}
	return a;
}
function RD(e, t, n) {
	var r = kt();
	return Pt(r, r, n.rotation), Nt(r, r, n.position), Wf([e.dataToCoord(t), (n.labelOffset || 0) + (n.labelDirection || 1) * (n.labelMargin || 0)], r);
}
function zD(e, t, n, r, i, a) {
	var o = ew.innerTextLayout(n.rotation, 0, n.labelDirection);
	n.labelMargin = i.get(["label", "margin"]), FD(t, r, i, a, {
		position: RD(r.axis, e, n),
		align: o.textAlign,
		verticalAlign: o.textVerticalAlign
	});
}
function BD(e, t, n) {
	return n ||= 0, {
		x1: e[n],
		y1: e[1 - n],
		x2: t[n],
		y2: t[1 - n]
	};
}
function VD(e, t, n) {
	return n ||= 0, {
		x: e[n],
		y: e[1 - n],
		width: t[n],
		height: t[1 - n]
	};
}
function HD(e, t, n) {
	return pC(e, {
		fromStat: { sers: I(t, function(e) {
			return n.getSeriesByIndex(e.seriesIndex);
		}) },
		min: 1
	}).w;
}
function UD(e, t, n) {
	return [Ma(ja(t[0], t[1]), e - n / 2), ja(e + n / 2, Ma(t[0], t[1]))];
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/CartesianAxisPointer.js
var WD = function(e) {
	r(t, e);
	function t() {
		return e !== null && e.apply(this, arguments) || this;
	}
	return t.prototype.makeElOption = function(e, t, n, r, i) {
		var a = n.axis, o = a.grid, s = r.get("type"), c = a.getGlobalExtent(), l = GD(o, a).getOtherAxis(a).getGlobalExtent(), u = a.toGlobalCoord(a.dataToCoord(t, !0));
		if (s && s !== "none") {
			var d = PD(r), f = KD[s](a, u, c, l, r.get("seriesDataIndices"), r.ecModel);
			f.style = d, e.graphicKey = f.type, e.pointer = f;
		}
		zD(t, e, xw(o.getRect(), n), n, r, i);
	}, t.prototype.getHandleTransform = function(e, t, n) {
		var r = xw(t.axis.grid.getRect(), t, { labelInside: !1 });
		r.labelMargin = n.get(["handle", "margin"]);
		var i = RD(t.axis, e, r);
		return {
			x: i[0],
			y: i[1],
			rotation: r.rotation + (r.labelDirection < 0 ? Math.PI : 0)
		};
	}, t.prototype.updateHandleTransform = function(e, t, n, r) {
		var i = n.axis, a = i.grid, o = i.getGlobalExtent(!0), s = GD(a, i).getOtherAxis(i).getGlobalExtent(), c = i.dim === "x" ? 0 : 1, l = [e.x, e.y];
		l[c] += t[c], l[c] = ja(o[1], l[c]), l[c] = Ma(o[0], l[c]);
		var u = (s[1] + s[0]) / 2, d = [u, u];
		return d[c] = l[c], {
			x: l[0],
			y: l[1],
			rotation: e.rotation,
			cursorPoint: d,
			tooltipOption: [{ verticalAlign: "middle" }, { align: "center" }][c]
		};
	}, t;
}(OD);
function GD(e, t) {
	var n = {};
	return n[t.dim + "AxisIndex"] = t.index, e.getCartesian(n);
}
var KD = {
	line: function(e, t, n, r) {
		return {
			type: "Line",
			subPixelOptimize: !0,
			shape: BD([t, r[0]], [t, r[1]], qD(e))
		};
	},
	shadow: function(e, t, n, r, i, a) {
		var o = HD(e, i, a), s = r[1] - r[0], c = UD(t, n, o), l = c[0], u = c[1];
		return {
			type: "Rect",
			shape: VD([l, r[0]], [u - l, s], qD(e))
		};
	}
};
function qD(e) {
	return e.dim === "x" ? 0 : 1;
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/AxisPointerModel.js
var JD = function(e) {
	r(t, e);
	function t() {
		var n = e !== null && e.apply(this, arguments) || this;
		return n.type = t.type, n;
	}
	return t.type = "axisPointer", t.defaultOption = {
		show: "auto",
		z: 50,
		type: "line",
		snap: !1,
		triggerTooltip: !0,
		triggerEmphasis: !0,
		value: null,
		status: null,
		link: [],
		animation: null,
		animationDurationUpdate: 200,
		lineStyle: {
			color: Q.color.border,
			width: 1,
			type: "dashed"
		},
		shadowStyle: { color: Q.color.shadowTint },
		label: {
			show: !0,
			formatter: null,
			precision: "auto",
			margin: 3,
			color: Q.color.neutral00,
			padding: [
				5,
				7,
				5,
				7
			],
			backgroundColor: Q.color.accent60,
			borderColor: null,
			borderWidth: 0,
			borderRadius: 3
		},
		handle: {
			show: !1,
			icon: "M10.7,11.9v-1.3H9.3v1.3c-4.9,0.3-8.8,4.4-8.8,9.4c0,5,3.9,9.1,8.8,9.4h1.3c4.9-0.3,8.8-4.4,8.8-9.4C19.5,16.3,15.6,12.2,10.7,11.9z M13.3,24.4H6.7v-1.2h6.6z M13.3,22H6.7v-1.2h6.6z M13.3,19.6H6.7v-1.2h6.6z",
			size: 45,
			margin: 50,
			color: Q.color.accent40,
			throttle: 40
		}
	}, t;
}(eh), YD = Y(), XD = F;
function ZD(e, t, n) {
	if (!a.node) {
		var r = t.getZr();
		YD(r).records || (YD(r).records = {}), QD(r, t);
		var i = YD(r).records[e] || (YD(r).records[e] = {});
		i.handler = n;
	}
}
function QD(e, t) {
	if (YD(e).initialized) return;
	YD(e).initialized = !0, n("click", z(tO, "click")), n("mousemove", z(tO, "mousemove")), n("mousewheel", z(tO, "mousewheel")), n("globalout", eO);
	function n(n, r) {
		e.on(n, function(n) {
			var i = nO(t);
			XD(YD(e).records, function(e) {
				e && r(e, n, i.dispatchAction);
			}), $D(i.pendings, t);
		});
	}
}
function $D(e, t) {
	var n = e.showTip.length, r = e.hideTip.length, i;
	n ? i = e.showTip[n - 1] : r && (i = e.hideTip[r - 1]), i && (i.dispatchAction = null, t.dispatchAction(i));
}
function eO(e, t, n) {
	e.handler("leave", null, n);
}
function tO(e, t, n, r) {
	t.handler(e, n, r);
}
function nO(e) {
	var t = {
		showTip: [],
		hideTip: []
	}, n = function(r) {
		var i = t[r.type];
		i ? i.push(r) : (r.dispatchAction = n, e.dispatchAction(r));
	};
	return {
		dispatchAction: n,
		pendings: t
	};
}
function rO(e, t) {
	if (!a.node) {
		var n = t.getZr();
		(YD(n).records || {})[e] && (YD(n).records[e] = null);
	}
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/AxisPointerView.js
var iO = function(e) {
	r(t, e);
	function t() {
		var n = e !== null && e.apply(this, arguments) || this;
		return n.type = t.type, n;
	}
	return t.prototype.render = function(e, t, n) {
		var r = t.getComponent("tooltip"), i = e.get("triggerOn") || r && r.get("triggerOn") || "mousemove|click|mousewheel";
		ZD("axisPointer", n, function(e, t, n) {
			i !== "none" && (e === "leave" || i.indexOf(e) >= 0) && n({
				type: "updateAxisPointer",
				currTrigger: e,
				x: t && t.offsetX,
				y: t && t.offsetY
			});
		});
	}, t.prototype.remove = function(e, t) {
		rO("axisPointer", t);
	}, t.prototype.dispose = function(e, t) {
		rO("axisPointer", t);
	}, t.type = "axisPointer", t;
}(ov);
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/findPointFromSeries.js
function aO(e, t) {
	var n = [], r = e.seriesIndex, i;
	if (r == null || !(i = t.getSeriesByIndex(r))) return { point: [] };
	var a = i.getData(), o = No(a, e);
	if (o == null || o < 0 || B(o)) return { point: [] };
	var s = a.getItemGraphicEl(o), c = i.coordinateSystem;
	if (i.getTooltipPosition) n = i.getTooltipPosition(o) || [];
	else if (c && c.dataToPoint) {
		if (e.isStacked) {
			var l = c.getBaseAxis(), u = c.getOtherAxis(l).dim, d = l.dim, f = +(u === "x" || u === "radius"), p = a.mapDimension(d), m = [];
			m[f] = a.get(p, o), m[1 - f] = a.get(a.getCalculationInfo("stackResultDimension"), o), n = c.dataToPoint(m) || [];
		} else n = c.dataToPoint(a.getValues(I(c.dimensions, function(e) {
			return a.mapDimension(e);
		}), o)) || [];
	} else if (s) {
		var h = s.getBoundingRect().clone();
		h.applyTransform(s.transform), n = [h.x + h.width / 2, h.y + h.height / 2];
	}
	return {
		point: n,
		el: s
	};
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/axisTrigger.js
var oO = Y();
function sO(e, t, n) {
	var r = e.currTrigger, i = [e.x, e.y], a = e, o = e.dispatchAction || R(n.dispatchAction, n), s = t.getComponent("axisPointer").coordSysAxesInfo;
	if (s) {
		_O(i) && (i = aO({
			seriesIndex: a.seriesIndex,
			dataIndex: a.dataIndex
		}, t).point);
		var c = _O(i), l = a.axesInfo, u = s.axesInfo, d = r === "leave" || _O(i), f = {}, p = {}, m = {
			list: [],
			map: {}
		}, h = {
			showPointer: z(uO, p),
			showTooltip: z(dO, m)
		};
		F(s.coordSysMap, function(e, t) {
			var n = c || e.containPoint(i);
			F(s.coordSysAxesInfo[t], function(e, t) {
				var r = e.axis, a = hO(l, e);
				if (!d && n && (!l || a)) {
					var o = a && a.value;
					o == null && !c && (o = r.pointToData(i)), o != null && cO(e, o, h, !1, f);
				}
			});
		});
		var g = {};
		return F(u, function(e, t) {
			var n = e.linkGroup;
			n && !p[t] && F(n.axesInfo, function(t, r) {
				var i = p[r];
				if (t !== e && i) {
					var a = i.value;
					n.mapper && (a = e.axis.scale.parse(n.mapper(a, gO(t), gO(e)))), g[e.key] = a;
				}
			});
		}), F(g, function(e, t) {
			cO(u[t], e, h, !0, f);
		}), fO(p, u, f), pO(m, i, e, o), mO(u, o, n), f;
	}
}
function cO(e, t, n, r, i) {
	var a = e.axis;
	if (!a.scale.isBlank() && a.containData(t)) {
		if (!e.involveSeries) n.showPointer(e, t);
		else {
			var o = lO(t, e), s = o.payloadBatch, c = o.snapToValue;
			s[0] && i.seriesIndex == null && j(i, s[0]), !r && e.snap && a.containData(c) && c != null && (t = c), n.showPointer(e, t, s), n.showTooltip(e, o, c);
		}
	}
}
function lO(e, t) {
	var n = t.axis, r = n.dim, i = e, a = [], o = Number.MAX_VALUE, s = -1;
	return F(t.seriesModels, function(t, c) {
		var l = t.getData().mapDimensionsAll(r), u, d;
		if (t.getAxisTooltipData) {
			var f = t.getAxisTooltipData(l, e, n);
			d = f.dataIndices, u = f.nestestValue;
		} else {
			if (d = t.indicesOfNearest(r, l[0], e, n.type === "category" ? .5 : null), !d.length) return;
			u = t.getData().get(l[0], d[0]);
		}
		if (io(u)) {
			var p = e - u, m = Math.abs(p);
			m <= o && ((m < o || p >= 0 && s < 0) && (o = m, s = p, i = u, a.length = 0), F(d, function(e) {
				a.push({
					seriesIndex: t.seriesIndex,
					dataIndexInside: e,
					dataIndex: t.getData().getRawIndex(e)
				});
			}));
		}
	}), {
		payloadBatch: a,
		snapToValue: i
	};
}
function uO(e, t, n, r) {
	e[t.key] = {
		value: n,
		payloadBatch: r
	};
}
function dO(e, t, n, r) {
	var i = n.payloadBatch, a = t.axis, o = a.model, s = t.axisPointerModel;
	if (t.triggerTooltip && i.length) {
		var c = t.coordSys.model, l = Nw(c), u = e.map[l];
		u || (u = e.map[l] = {
			coordSysId: c.id,
			coordSysIndex: c.componentIndex,
			coordSysType: c.type,
			coordSysMainType: c.mainType,
			dataByAxis: []
		}, e.list.push(u)), u.dataByAxis.push({
			axisDim: a.dim,
			axisIndex: o.componentIndex,
			axisType: o.type,
			axisId: o.id,
			value: r,
			valueLabelOpt: {
				precision: s.get(["label", "precision"]),
				formatter: s.get(["label", "formatter"])
			},
			seriesDataIndices: i.slice()
		});
	}
}
function fO(e, t, n) {
	var r = n.axesInfo = [];
	F(t, function(t, n) {
		var i = t.axisPointerModel.option, a = e[n];
		a ? (!t.useHandle && (i.status = "show"), i.value = a.value, i.seriesDataIndices = (a.payloadBatch || []).slice()) : !t.useHandle && (i.status = "hide"), i.status === "show" && r.push({
			axisDim: t.axis.dim,
			axisIndex: t.axis.model.componentIndex,
			value: i.value
		});
	});
}
function pO(e, t, n, r) {
	if (_O(t) || !e.list.length) r({ type: "hideTip" });
	else {
		var i = ((e.list[0].dataByAxis[0] || {}).seriesDataIndices || [])[0] || {};
		r({
			type: "showTip",
			escapeConnect: !0,
			x: t[0],
			y: t[1],
			tooltipOption: n.tooltipOption,
			position: n.position,
			dataIndexInside: i.dataIndexInside,
			dataIndex: i.dataIndex,
			seriesIndex: i.seriesIndex,
			dataByCoordSys: e.list
		});
	}
}
function mO(e, t, n) {
	var r = n.getZr(), i = "axisPointerLastHighlights", a = oO(r)[i] || {}, o = oO(r)[i] = {};
	F(e, function(e, t) {
		var n = e.axisPointerModel.option;
		n.status === "show" && e.triggerEmphasis && F(n.seriesDataIndices, function(e) {
			o[e.seriesIndex + "|" + e.dataIndex] = e;
		});
	});
	var s = [], c = [];
	function l(e) {
		return {
			seriesIndex: e.seriesIndex,
			dataIndex: e.dataIndex
		};
	}
	F(a, function(e, t) {
		!o[t] && c.push(l(e));
	}), F(o, function(e, t) {
		!a[t] && s.push(l(e));
	}), c.length && n.dispatchAction({
		type: "downplay",
		escapeConnect: !0,
		notBlur: !0,
		batch: c
	}), s.length && n.dispatchAction({
		type: "highlight",
		escapeConnect: !0,
		notBlur: !0,
		batch: s
	});
}
function hO(e, t) {
	for (var n = 0; n < (e || []).length; n++) {
		var r = e[n];
		if (t.axis.dim === r.axisDim && t.axis.model.componentIndex === r.axisIndex) return r;
	}
}
function gO(e) {
	var t = e.axis.model, n = {}, r = n.axisDim = e.axis.dim;
	return n.axisIndex = n[r + "AxisIndex"] = t.componentIndex, n.axisName = n[r + "AxisName"] = t.name, n.axisId = n[r + "AxisId"] = t.id, n;
}
function _O(e) {
	return !e || e[0] == null || isNaN(e[0]) || e[1] == null || isNaN(e[1]);
}
//#endregion
//#region node_modules/echarts/lib/component/axisPointer/install.js
function vO(e) {
	Fw.registerAxisPointerClass("CartesianAxisPointer", WD), e.registerComponentModel(JD), e.registerComponentView(iO), e.registerPreprocessor(function(e) {
		if (e) {
			(!e.axisPointer || e.axisPointer.length === 0) && (e.axisPointer = {});
			var t = e.axisPointer.link;
			t && !B(t) && (e.axisPointer.link = [t]);
		}
	}), e.registerProcessor(e.PRIORITY.PROCESSOR.STATISTIC, { overallReset: function(e, t) {
		e.getComponent("axisPointer").coordSysAxesInfo = Cw(e, t);
	} }), e.registerAction({
		type: "updateAxisPointer",
		event: "updateAxisPointer",
		update: ":updateAxisPointer"
	}, sO);
}
//#endregion
//#region node_modules/echarts/lib/component/tooltip/TooltipModel.js
var yO = function(e) {
	r(t, e);
	function t() {
		var n = e !== null && e.apply(this, arguments) || this;
		return n.type = t.type, n;
	}
	return t.type = "tooltip", t.dependencies = ["axisPointer"], t.defaultOption = {
		z: 60,
		show: !0,
		showContent: !0,
		trigger: "item",
		triggerOn: "mousemove|click|mousewheel",
		alwaysShowContent: !1,
		renderMode: "auto",
		confine: null,
		showDelay: 0,
		hideDelay: 100,
		transitionDuration: .4,
		displayTransition: !0,
		enterable: !1,
		backgroundColor: Q.color.neutral00,
		shadowBlur: 10,
		shadowColor: "rgba(0, 0, 0, .2)",
		shadowOffsetX: 1,
		shadowOffsetY: 2,
		borderRadius: 4,
		borderWidth: 1,
		defaultBorderColor: Q.color.border,
		padding: null,
		extraCssText: "",
		axisPointer: {
			type: "line",
			axis: "auto",
			animation: "auto",
			animationDurationUpdate: 200,
			animationEasingUpdate: "exponentialOut",
			crossStyle: {
				color: Q.color.borderShade,
				width: 1,
				type: "dashed",
				textStyle: {}
			}
		},
		textStyle: {
			color: Q.color.tertiary,
			fontSize: 14
		}
	}, t;
}(eh);
//#endregion
//#region node_modules/echarts/lib/component/tooltip/helper.js
function bO(e) {
	var t = e.get("confine");
	return t == null ? e.get("renderMode") === "richText" : !!t;
}
function xO(e) {
	if (a.domSupported) {
		for (var t = document.documentElement.style, n = 0, r = e.length; n < r; n++) if (e[n] in t) return e[n];
	}
}
var SO = xO([
	"transform",
	"webkitTransform",
	"OTransform",
	"MozTransform",
	"msTransform"
]), CO = xO([
	"webkitTransition",
	"transition",
	"OTransition",
	"MozTransition",
	"msTransition"
]);
function wO(e, t) {
	if (!e) return t;
	t = wm(t, !0);
	var n = e.indexOf(t);
	return e = n === -1 ? t : "-" + e.slice(0, n) + "-" + t, e.toLowerCase();
}
function TO(e, t) {
	var n = e.currentStyle || document.defaultView && document.defaultView.getComputedStyle(e);
	return n ? t ? n[t] : n : null;
}
//#endregion
//#region node_modules/echarts/lib/component/tooltip/TooltipHTMLContent.js
var EO = wO(CO, "transition"), DO = wO(SO, "transform"), OO = "position:absolute;display:block;border-style:solid;white-space:nowrap;z-index:9999999;" + (a.transform3dSupported ? "will-change:transform;" : "");
function kO(e) {
	return e = e === "left" ? "right" : e === "right" ? "left" : e === "top" ? "bottom" : "top", e;
}
function AO(e, t, n) {
	if (!H(n) || n === "inside") return "";
	var r = e.get("backgroundColor"), i = e.get("borderWidth");
	t = jm(t);
	var a = kO(n), o = Math.max(Math.round(i) * 1.5, 6), s = "", c = DO + ":", l;
	N(["left", "right"], a) > -1 ? (s += "top:50%", c += "translateY(-50%) rotate(" + (l = a === "left" ? -225 : -45) + "deg)") : (s += "left:50%", c += "translateX(-50%) rotate(" + (l = a === "top" ? 225 : 45) + "deg)");
	var u = l * Math.PI / 180, d = o + i, f = d * Math.abs(Math.cos(u)) + d * Math.abs(Math.sin(u)), p = Math.round(((f - Math.SQRT2 * i) / 2 + Math.SQRT2 * i - (f - d) / 2) * 100) / 100;
	s += ";" + a + ":-" + p + "px";
	var m = t + " solid " + i + "px;";
	return "<div style=\"" + [
		"position:absolute;width:" + o + "px;height:" + o + "px;z-index:-1;",
		s + ";" + c + ";",
		"border-bottom:" + m,
		"border-right:" + m,
		"background-color:" + r + ";"
	].join("") + "\"></div>";
}
function jO(e, t, n) {
	var r = "cubic-bezier(0.23,1,0.32,1)", i = "", o = "";
	return n && (i = " " + e / 2 + "s " + r, o = "opacity" + i + ",visibility" + i), t || (i = " " + e + "s " + r, o += (o.length ? "," : "") + (a.transformSupported ? "" + DO + i : ",left" + i + ",top" + i)), EO + ":" + o;
}
function MO(e, t, n) {
	var r = e.toFixed(0) + "px", i = t.toFixed(0) + "px";
	if (!a.transformSupported) return n ? "top:" + i + ";left:" + r + ";" : [["top", i], ["left", r]];
	var o = a.transform3dSupported, s = "translate" + (o ? "3d" : "") + "(" + r + "," + i + (o ? ",0" : "") + ")";
	return n ? "top:0;left:0;" + DO + ":" + s + ";" : [
		["top", 0],
		["left", 0],
		[SO, s]
	];
}
function NO(e) {
	var t = [], n = e.get("fontSize"), r = e.getTextColor();
	r && t.push("color:" + r), t.push("font:" + e.getFont());
	var i = G(e.get("lineHeight"), Math.round(n * 3 / 2));
	n && t.push("line-height:" + i + "px");
	var a = e.get("textShadowColor"), o = e.get("textShadowBlur") || 0, s = e.get("textShadowOffsetX") || 0, c = e.get("textShadowOffsetY") || 0;
	return a && o && t.push("text-shadow:" + s + "px " + c + "px " + o + "px " + a), F(["decoration", "align"], function(n) {
		var r = e.get(n);
		r && t.push("text-" + n + ":" + r);
	}), t.join(";");
}
function PO(e, t, n, r) {
	var i = [], a = e.get("transitionDuration"), o = e.get("backgroundColor"), s = e.get("shadowBlur"), c = e.get("shadowColor"), l = e.get("shadowOffsetX"), u = e.get("shadowOffsetY"), d = e.getModel("textStyle"), f = G_(e, "html"), p = l + "px " + u + "px " + s + "px " + c;
	return i.push("box-shadow:" + p), t && a > 0 && i.push(jO(a, n, r)), o && i.push("background-color:" + o), F([
		"width",
		"color",
		"radius"
	], function(t) {
		var n = "border-" + t, r = wm(n), a = e.get(r);
		a != null && i.push(n + ":" + a + (t === "color" ? "" : "px"));
	}), i.push(NO(d)), f != null && i.push("padding:" + Tm(f).join("px ") + "px"), i.join(";") + ";";
}
function FO(e, t, n, r, i) {
	var a = t && t.painter;
	if (n) {
		var o = a && a.getViewportRoot();
		o && it(e, o, n, r, i);
	} else {
		e[0] = r, e[1] = i;
		var s = a && a.getViewportRootOffset();
		s && (e[0] += s.offsetLeft, e[1] += s.offsetTop);
	}
	e[2] = e[0] / t.getWidth(), e[3] = e[1] / t.getHeight();
}
var IO = function() {
	function e(e, t) {
		if (this._show = !1, this._styleCoord = [
			0,
			0,
			0,
			0
		], this._enterable = !0, this._alwaysShowContent = !1, this._firstShow = !0, this._longHide = !0, a.wxa) return null;
		var n = document.createElement("div");
		n.domBelongToZr = !0, this.el = n;
		var r = this._zr = e.getZr(), i = t.appendTo, o = i && (H(i) ? document.querySelector(i) : le(i) ? i : V(i) && i(e.getDom()));
		FO(this._styleCoord, r, o, e.getWidth() / 2, e.getHeight() / 2), (o || e.getDom()).appendChild(n), this._api = e, this._container = o;
		var s = this;
		n.onmouseenter = function() {
			s._enterable && (clearTimeout(s._hideTimeout), s._show = !0), s._inContent = !0;
		}, n.onmousemove = function(e) {
			if (e ||= window.event, !s._enterable) {
				var t = r.handler;
				yt(r.painter.getViewportRoot(), e, !0), t.dispatch("mousemove", e);
			}
		}, n.onmouseleave = function() {
			s._inContent = !1, s._enterable && s._show && s.hideLater(s._hideDelay);
		};
	}
	return e.prototype.update = function(e) {
		if (!this._container) {
			var t = this._api.getDom(), n = TO(t, "position"), r = t.style;
			r.position !== "absolute" && n !== "absolute" && (r.position = "relative");
		}
		var i = e.get("alwaysShowContent");
		i && this._moveIfResized(), this._alwaysShowContent = i, this._enableDisplayTransition = e.get("displayTransition") && e.get("transitionDuration") > 0, this.el.className = e.get("className") || "";
	}, e.prototype.show = function(e, t) {
		clearTimeout(this._hideTimeout), clearTimeout(this._longHideTimeout);
		var n = this.el, r = n.style, i = this._styleCoord;
		n.innerHTML ? r.cssText = OO + PO(e, !this._firstShow, this._longHide, this._enableDisplayTransition) + MO(i[0], i[1], !0) + ("border-color:" + jm(t) + ";") + (e.get("extraCssText") || "") + (";pointer-events:" + (this._enterable ? "auto" : "none")) : r.display = "none", this._show = !0, this._firstShow = !1, this._longHide = !1;
	}, e.prototype.setContent = function(e, t, n, r, i) {
		var a = this.el;
		if (e == null) a.innerHTML = "";
		else {
			var o = "";
			if (H(i) && n.get("trigger") === "item" && !bO(n) && (o = AO(n, r, i)), H(e)) a.innerHTML = e + o;
			else if (e) {
				a.innerHTML = "", B(e) || (e = [e]);
				for (var s = 0; s < e.length; s++) le(e[s]) && e[s].parentNode !== a && a.appendChild(e[s]);
				if (o && a.childNodes.length) {
					var c = document.createElement("div");
					c.innerHTML = o, a.appendChild(c);
				}
			}
		}
	}, e.prototype.setEnterable = function(e) {
		this._enterable = e;
	}, e.prototype.getSize = function() {
		var e = this.el;
		return e ? [e.offsetWidth, e.offsetHeight] : [0, 0];
	}, e.prototype.moveTo = function(e, t) {
		if (this.el) {
			var n = this._styleCoord;
			if (FO(n, this._zr, this._container, e, t), n[0] != null && n[1] != null) {
				var r = this.el.style;
				F(MO(n[0], n[1]), function(e) {
					r[e[0]] = e[1];
				});
			}
		}
	}, e.prototype._moveIfResized = function() {
		var e = this._styleCoord[2], t = this._styleCoord[3];
		this.moveTo(e * this._zr.getWidth(), t * this._zr.getHeight());
	}, e.prototype.hide = function() {
		var e = this, t = this.el.style;
		this._enableDisplayTransition ? (t.visibility = "hidden", t.opacity = "0") : t.display = "none", a.transform3dSupported && (t.willChange = ""), this._show = !1, this._longHideTimeout = setTimeout(function() {
			return e._longHide = !0;
		}, 500);
	}, e.prototype.hideLater = function(e) {
		this._show && !(this._inContent && this._enterable) && !this._alwaysShowContent && (e ? (this._hideDelay = e, this._show = !1, this._hideTimeout = setTimeout(R(this.hide, this), e)) : this.hide());
	}, e.prototype.isShow = function() {
		return this._show;
	}, e.prototype.dispose = function() {
		clearTimeout(this._hideTimeout), clearTimeout(this._longHideTimeout);
		var e = this._zr;
		at(e && e.painter && e.painter.getViewportRoot(), this._container);
		var t = this.el;
		if (t) {
			t.onmouseenter = t.onmousemove = t.onmouseleave = null;
			var n = t.parentNode;
			n && n.removeChild(t);
		}
		this.el = this._container = null;
	}, e;
}(), LO = function() {
	function e(e) {
		this._show = !1, this._styleCoord = [
			0,
			0,
			0,
			0
		], this._alwaysShowContent = !1, this._enterable = !0, this._zr = e.getZr(), BO(this._styleCoord, this._zr, e.getWidth() / 2, e.getHeight() / 2);
	}
	return e.prototype.update = function(e) {
		var t = e.get("alwaysShowContent");
		t && this._moveIfResized(), this._alwaysShowContent = t;
	}, e.prototype.show = function() {
		this._hideTimeout && clearTimeout(this._hideTimeout), this.el.show(), this._show = !0;
	}, e.prototype.setContent = function(e, t, n, r, i) {
		var a = this;
		W(e) && uo(""), this.el && this._zr.remove(this.el);
		var o = n.getModel("textStyle");
		this.el = new gl({
			style: {
				rich: t.richTextStyles,
				text: e,
				lineHeight: 22,
				borderWidth: 1,
				borderColor: r,
				textShadowColor: o.get("textShadowColor"),
				fill: n.get(["textStyle", "color"]),
				padding: G_(n, "richText"),
				verticalAlign: "top",
				align: "left"
			},
			z: n.get("z")
		}), F([
			"backgroundColor",
			"borderRadius",
			"shadowColor",
			"shadowBlur",
			"shadowOffsetX",
			"shadowOffsetY"
		], function(e) {
			a.el.style[e] = n.get(e);
		}), F([
			"textShadowBlur",
			"textShadowOffsetX",
			"textShadowOffsetY"
		], function(e) {
			a.el.style[e] = o.get(e) || 0;
		}), this._zr.add(this.el);
		var s = this;
		this.el.on("mouseover", function() {
			s._enterable && (clearTimeout(s._hideTimeout), s._show = !0), s._inContent = !0;
		}), this.el.on("mouseout", function() {
			s._enterable && s._show && s.hideLater(s._hideDelay), s._inContent = !1;
		});
	}, e.prototype.setEnterable = function(e) {
		this._enterable = e;
	}, e.prototype.getSize = function() {
		var e = this.el, t = this.el.getBoundingRect(), n = zO(e.style);
		return [t.width + n.left + n.right, t.height + n.top + n.bottom];
	}, e.prototype.moveTo = function(e, t) {
		var n = this.el;
		if (n) {
			var r = this._styleCoord;
			BO(r, this._zr, e, t), e = r[0], t = r[1];
			var i = n.style, a = RO(i.borderWidth || 0), o = zO(i);
			n.x = e + a + o.left, n.y = t + a + o.top, n.markRedraw();
		}
	}, e.prototype._moveIfResized = function() {
		var e = this._styleCoord[2], t = this._styleCoord[3];
		this.moveTo(e * this._zr.getWidth(), t * this._zr.getHeight());
	}, e.prototype.hide = function() {
		this.el && this.el.hide(), this._show = !1;
	}, e.prototype.hideLater = function(e) {
		this._show && !(this._inContent && this._enterable) && !this._alwaysShowContent && (e ? (this._hideDelay = e, this._show = !1, this._hideTimeout = setTimeout(R(this.hide, this), e)) : this.hide());
	}, e.prototype.isShow = function() {
		return this._show;
	}, e.prototype.dispose = function() {
		this._zr.remove(this.el);
	}, e;
}();
function RO(e) {
	return Math.max(0, e);
}
function zO(e) {
	var t = RO(e.shadowBlur || 0), n = RO(e.shadowOffsetX || 0), r = RO(e.shadowOffsetY || 0);
	return {
		left: RO(t - n),
		right: RO(t + n),
		top: RO(t - r),
		bottom: RO(t + r)
	};
}
function BO(e, t, n, r) {
	e[0] = n, e[1] = r, e[2] = e[0] / t.getWidth(), e[3] = e[1] / t.getHeight();
}
//#endregion
//#region node_modules/echarts/lib/component/tooltip/TooltipView.js
var VO = new dl({ shape: {
	x: -1,
	y: -1,
	width: 2,
	height: 2
} }), HO = function(e) {
	r(t, e);
	function t() {
		var n = e !== null && e.apply(this, arguments) || this;
		return n.type = t.type, n;
	}
	return t.prototype.init = function(e, t) {
		if (!a.node && t.getDom()) {
			var n = e.getComponent("tooltip"), r = this._renderMode = Ho(n.get("renderMode"));
			this._tooltipContent = r === "richText" ? new LO(t) : new IO(t, { appendTo: n.get("appendToBody", !0) ? "body" : n.get("appendTo", !0) });
		}
	}, t.prototype.render = function(e, t, n) {
		if (!a.node && n.getDom()) {
			this.group.removeAll(), this._tooltipModel = e, this._ecModel = t, this._api = n;
			var r = this._tooltipContent;
			r.update(e), r.setEnterable(e.get("enterable")), this._initGlobalListener(), this._keepShow(), this._renderMode !== "richText" && e.get("transitionDuration") ? bv(this, "_updatePosition", 50, "fixRate") : xv(this, "_updatePosition");
		}
	}, t.prototype._initGlobalListener = function() {
		var e = this._tooltipModel.get("triggerOn");
		ZD("itemTooltip", this._api, R(function(t, n, r) {
			e !== "none" && (e.indexOf(t) >= 0 ? this._tryShow(n, r) : t === "leave" && this._hide(r));
		}, this));
	}, t.prototype._keepShow = function() {
		var e = this._tooltipModel, t = this._ecModel, n = this._api, r = e.get("triggerOn");
		if (e.get("trigger") !== "axis" && (this._lastDataByCoordSys = null, this._cbParamsList = null), this._lastX != null && this._lastY != null && r !== "none" && r !== "click") {
			var i = this;
			clearTimeout(this._refreshUpdateTimeout), this._refreshUpdateTimeout = setTimeout(function() {
				!n.isDisposed() && i.manuallyShowTip(e, t, n, {
					x: i._lastX,
					y: i._lastY,
					dataByCoordSys: i._lastDataByCoordSys
				});
			});
		}
	}, t.prototype.manuallyShowTip = function(e, t, n, r) {
		if (r.from !== this.uid && !a.node && n.getDom()) {
			var i = WO(r, n);
			this._ticket = "";
			var o = r.dataByCoordSys, s = YO(r, t, n);
			if (s) {
				var c = s.el.getBoundingRect().clone();
				c.applyTransform(s.el.transform), this._tryShow({
					offsetX: c.x + c.width / 2,
					offsetY: c.y + c.height / 2,
					target: s.el,
					position: r.position,
					positionDefault: "bottom"
				}, i);
			} else if (r.tooltip && r.x != null && r.y != null) {
				var l = VO;
				l.x = r.x, l.y = r.y, l.update(), Al(l).tooltipConfig = {
					name: null,
					option: r.tooltip
				}, this._tryShow({
					offsetX: r.x,
					offsetY: r.y,
					target: l
				}, i);
			} else if (o) this._tryShow({
				offsetX: r.x,
				offsetY: r.y,
				position: r.position,
				dataByCoordSys: o,
				tooltipOption: r.tooltipOption
			}, i);
			else if (r.seriesIndex != null) {
				if (this._manuallyAxisShowTip(e, t, n, r)) return;
				var u = aO(r, t), d = u.point[0], f = u.point[1];
				d != null && f != null && this._tryShow({
					offsetX: d,
					offsetY: f,
					target: u.el,
					position: r.position,
					positionDefault: "bottom"
				}, i);
			} else r.x != null && r.y != null && (n.dispatchAction({
				type: "updateAxisPointer",
				x: r.x,
				y: r.y
			}), this._tryShow({
				offsetX: r.x,
				offsetY: r.y,
				position: r.position,
				target: n.getZr().findHover(r.x, r.y).target
			}, i));
		}
	}, t.prototype.manuallyHideTip = function(e, t, n, r) {
		var i = this._tooltipContent;
		this._tooltipModel && i.hideLater(this._tooltipModel.get("hideDelay")), this._lastX = this._lastY = this._lastDataByCoordSys = null, this._cbParamsList = null, r.from !== this.uid && this._hide(WO(r, n));
	}, t.prototype._manuallyAxisShowTip = function(e, t, n, r) {
		var i = r.seriesIndex, a = r.dataIndex, o = t.getComponent("axisPointer").coordSysAxesInfo;
		if (i != null && a != null && o != null) {
			var s = t.getSeriesByIndex(i);
			if (s && UO([
				s.getData().getItemModel(a),
				s,
				(s.coordinateSystem || {}).model
			], this._tooltipModel).get("trigger") === "axis") return n.dispatchAction({
				type: "updateAxisPointer",
				seriesIndex: i,
				dataIndex: a,
				position: r.position
			}), !0;
		}
	}, t.prototype._tryShow = function(e, t) {
		var n = e.target;
		if (this._tooltipModel) {
			this._lastX = e.offsetX, this._lastY = e.offsetY;
			var r = e.dataByCoordSys;
			if (r && r.length) this._showAxisTooltip(r, e);
			else if (n) {
				if (Al(n).ssrType === "legend") return;
				this._lastDataByCoordSys = null, this._cbParamsList = null;
				var i, a;
				sy(n, function(e) {
					if (e.tooltipDisabled) return i = a = null, !0;
					i || a || (Al(e).dataIndex == null ? Al(e).tooltipConfig != null && (a = e) : i = e);
				}, !0), i ? this._showSeriesItemTooltip(e, i, t) : a ? this._showComponentItemTooltip(e, a, t) : this._hide(t);
			} else this._lastDataByCoordSys = null, this._cbParamsList = null, this._hide(t);
		}
	}, t.prototype._showOrMove = function(e, t) {
		var n = e.get("showDelay");
		t = R(t, this), clearTimeout(this._showTimout), n > 0 ? this._showTimout = setTimeout(t, n) : t();
	}, t.prototype._showAxisTooltip = function(e, t) {
		var n = this._ecModel, r = this._tooltipModel, i = [t.offsetX, t.offsetY], a = UO([t.tooltipOption], r), o = this._renderMode, s = [], c = j_("section", {
			blocks: [],
			noHeader: !0
		}), l = [], u = new K_();
		F(e, function(e) {
			F(e.dataByAxis, function(e) {
				var t = n.getComponent(e.axisDim + "Axis", e.axisIndex), i = e.value, a = t.axis, d = a.scale.parse(i);
				if (t && i != null) {
					var f = LD(i, a, n, e.seriesDataIndices, e.valueLabelOpt), p = j_("section", {
						header: f,
						noHeader: !ve(f),
						sortBlocks: !0,
						blocks: []
					});
					c.blocks.push(p), F(e.seriesDataIndices, function(i) {
						var a = n.getSeriesByIndex(i.seriesIndex), c = i.dataIndexInside, m = a.getDataParams(c);
						if (!(m.dataIndex < 0)) {
							m.axisDim = e.axisDim, m.axisIndex = e.axisIndex, m.axisType = e.axisType, m.axisId = e.axisId, m.axisValue = XS(t.axis, { value: d }), m.axisValueLabel = f, m.marker = u.makeTooltipMarker("item", jm(m.color), o);
							var h = qg(a.formatTooltip(c, !0, null)), g = h.frag;
							if (g) {
								var _ = UO([a], r).get("valueFormatter");
								p.blocks.push(_ ? j({ valueFormatter: _ }, g) : g);
							}
							h.text && l.push(h.text), s.push(m);
						}
					});
				}
			});
		}), c.blocks.reverse(), l.reverse();
		var d = t.position, f = L_(c, u, o, a.get("order"), n.get("useUTC"), a.get("textStyle"));
		f && l.unshift(f);
		var p = o === "richText" ? "\n\n" : "<br/>", m = l.join(p);
		this._showOrMove(a, function() {
			this._updateContentNotChangedOnAxis(e, s) ? this._updatePosition(a, d, i[0], i[1], this._tooltipContent, s) : this._showTooltipContent(a, m, s, Math.random() + "", i[0], i[1], d, null, u);
		});
	}, t.prototype._showSeriesItemTooltip = function(e, t, n) {
		var r = this._ecModel, i = Al(t), a = i.seriesIndex, o = r.getSeriesByIndex(a), s = i.dataModel || o, c = i.dataIndex, l = i.dataType, u = s.getData(l), d = this._renderMode, f = e.positionDefault, p = UO([
			u.getItemModel(c),
			s,
			o && (o.coordinateSystem || {}).model
		], this._tooltipModel, f ? { position: f } : null), m = p.get("trigger");
		if (m == null || m === "item") {
			var h = s.getDataParams(c, l), g = new K_();
			h.marker = g.makeTooltipMarker("item", jm(h.color), d);
			var _ = qg(s.formatTooltip(c, !1, l)), v = p.get("order"), y = p.get("valueFormatter"), b = _.frag, x = b ? L_(y ? j({ valueFormatter: y }, b) : b, g, d, v, r.get("useUTC"), p.get("textStyle")) : _.text, S = "item_" + s.name + "_" + c;
			this._showOrMove(p, function() {
				this._showTooltipContent(p, x, h, S, e.offsetX, e.offsetY, e.position, e.target, g);
			}), n({
				type: "showTip",
				dataIndexInside: c,
				dataIndex: u.getRawIndex(c),
				seriesIndex: a,
				from: this.uid
			});
		}
	}, t.prototype._showComponentItemTooltip = function(e, t, n) {
		var r = this._renderMode === "html", i = Al(t), a = i.tooltipConfig.option || {}, o = a.encodeHTMLContent;
		if (H(a)) {
			var s = a;
			a = {
				content: s,
				formatter: s
			}, o = !0;
		}
		o && r && a.content && (a = k(a), a.content = ft(a.content));
		var c = [a], l = this._ecModel.getComponent(i.componentMainType, i.componentIndex);
		l && c.push(l), c.push({ formatter: a.content });
		var u = e.positionDefault, d = UO(c, this._tooltipModel, u ? { position: u } : null), f = d.get("content"), p = Math.random() + "", m = new K_();
		this._showOrMove(d, function() {
			var n = k(d.get("formatterParams") || {});
			this._showTooltipContent(d, f, n, p, e.offsetX, e.offsetY, e.position, t, m);
		}), n({
			type: "showTip",
			from: this.uid
		});
	}, t.prototype._showTooltipContent = function(e, t, n, r, i, a, o, s, c) {
		if (this._ticket = "", e.get("showContent") && e.get("show")) {
			var l = this._tooltipContent;
			l.setEnterable(e.get("enterable"));
			var u = e.get("formatter");
			o ||= e.get("position");
			var d = t, f = this._getNearestPoint([i, a], n, e.get("trigger"), e.get("borderColor"), e.get("defaultBorderColor", !0)).color;
			if (u) {
				if (H(u)) {
					var p = e.ecModel.get("useUTC"), m = B(n) ? n[0] : n, h = m && m.axisType && m.axisType.indexOf("time") >= 0;
					d = u, h && (d = hm(m.axisValue, d, p)), d = km(d, n, !0);
				} else if (V(u)) {
					var g = R(function(t, r) {
						t === this._ticket && (l.setContent(r, c, e, f, o), this._updatePosition(e, o, i, a, l, n, s));
					}, this);
					this._ticket = r, d = u(n, r, g);
				} else d = u;
			}
			l.setContent(d, c, e, f, o), l.show(e, f), this._updatePosition(e, o, i, a, l, n, s);
		}
	}, t.prototype._getNearestPoint = function(e, t, n, r, i) {
		if (n === "axis" || B(t)) return { color: r || i };
		if (!B(t)) return { color: r || t.color || t.borderColor };
	}, t.prototype._updatePosition = function(e, t, n, r, i, a, o) {
		var s = this._api.getWidth(), c = this._api.getHeight();
		t ||= e.get("position");
		var l = i.getSize(), u = e.get("align"), d = e.get("verticalAlign"), f = o && o.getBoundingRect().clone();
		if (o && f.applyTransform(o.transform), V(t) && (t = t([n, r], a, i.el, f, {
			viewSize: [s, c],
			contentSize: l.slice()
		})), B(t)) n = za(t[0], s), r = za(t[1], c);
		else if (W(t)) {
			var p = t;
			p.width = l[0], p.height = l[1];
			var m = Gm(p, {
				width: s,
				height: c
			});
			n = m.x, r = m.y, u = null, d = null;
		} else if (H(t) && o) {
			var h = qO(t, f, l, e.get("borderWidth"));
			n = h[0], r = h[1];
		} else {
			var h = GO(n, r, i, s, c, u ? null : 20, d ? null : 20);
			n = h[0], r = h[1];
		}
		if (u && (n -= JO(u) ? l[0] / 2 : u === "right" ? l[0] : 0), d && (r -= JO(d) ? l[1] / 2 : d === "bottom" ? l[1] : 0), bO(e)) {
			var h = KO(n, r, i, s, c);
			n = h[0], r = h[1];
		}
		i.moveTo(n, r);
	}, t.prototype._updateContentNotChangedOnAxis = function(e, t) {
		var n = this._lastDataByCoordSys, r = this._cbParamsList, i = !!n && n.length === e.length;
		return i && F(n, function(n, a) {
			var o = n.dataByAxis || [], s = (e[a] || {}).dataByAxis || [];
			i &&= o.length === s.length, i && F(o, function(e, n) {
				var a = s[n] || {}, o = e.seriesDataIndices || [], c = a.seriesDataIndices || [];
				i = i && e.value === a.value && e.axisType === a.axisType && e.axisId === a.axisId && o.length === c.length, i && F(o, function(e, t) {
					var n = c[t];
					i = i && e.seriesIndex === n.seriesIndex && e.dataIndex === n.dataIndex;
				}), r && F(e.seriesDataIndices, function(e) {
					var n = e.seriesIndex, a = t[n], o = r[n];
					a && o && o.data !== a.data && (i = !1);
				});
			});
		}), this._lastDataByCoordSys = e, this._cbParamsList = t, !!i;
	}, t.prototype._hide = function(e) {
		this._lastDataByCoordSys = null, this._cbParamsList = null, e({
			type: "hideTip",
			from: this.uid
		});
	}, t.prototype.dispose = function(e, t) {
		!a.node && t.getDom() && (xv(this, "_updatePosition"), this._tooltipContent.dispose(), rO("itemTooltip", t), this._tooltipContent = null, this._tooltipModel = null, this._lastDataByCoordSys = null, this._cbParamsList = null);
	}, t.type = "tooltip", t;
}(ov);
function UO(e, t, n) {
	var r = t.ecModel, i;
	n ? (i = new Kp(n, r, r), i = new Kp(t.option, i, r)) : i = t;
	for (var a = e.length - 1; a >= 0; a--) {
		var o = e[a];
		o && (o instanceof Kp && (o = o.get("tooltip", !0)), H(o) && (o = { formatter: o }), o && (i = new Kp(o, i, r)));
	}
	return i;
}
function WO(e, t) {
	return e.dispatchAction || R(t.dispatchAction, t);
}
function GO(e, t, n, r, i, a, o) {
	var s = n.getSize(), c = s[0], l = s[1];
	return a != null && (e + c + a + 2 > r ? e -= c + a : e += a), o != null && (t + l + o > i ? t -= l + o : t += o), [e, t];
}
function KO(e, t, n, r, i) {
	var a = n.getSize(), o = a[0], s = a[1];
	return e = Math.min(e + o, r) - o, t = Math.min(t + s, i) - s, e = Math.max(e, 0), t = Math.max(t, 0), [e, t];
}
function qO(e, t, n, r) {
	var i = n[0], a = n[1], o = Math.ceil(Math.SQRT2 * r) + 8, s = 0, c = 0, l = t.width, u = t.height;
	switch (e) {
		case "inside":
			s = t.x + l / 2 - i / 2, c = t.y + u / 2 - a / 2;
			break;
		case "top":
			s = t.x + l / 2 - i / 2, c = t.y - a - o;
			break;
		case "bottom":
			s = t.x + l / 2 - i / 2, c = t.y + u + o;
			break;
		case "left":
			s = t.x - i - o, c = t.y + u / 2 - a / 2;
			break;
		case "right": s = t.x + l + o, c = t.y + u / 2 - a / 2;
	}
	return [s, c];
}
function JO(e) {
	return e === "center" || e === "middle";
}
function YO(e, t, n) {
	var r = Io(e).queryOptionMap, i = r.keys()[0];
	if (i && i !== "series") {
		var a = Ro(t, i, r.get(i), {
			useDefault: !1,
			enableAll: !1,
			enableNone: !1
		}).models[0];
		if (a) {
			var o = n.getViewOfComponentModel(a), s;
			if (o.group.traverse(function(t) {
				var n = Al(t).tooltipConfig;
				if (n && n.name === e.name) return s = t, !0;
			}), s) return {
				componentMainType: i,
				componentIndex: a.componentIndex,
				el: s
			};
		}
	}
}
//#endregion
//#region node_modules/echarts/lib/component/tooltip/install.js
function XO(e) {
	lC(vO), e.registerComponentModel(yO), e.registerComponentView(HO), e.registerAction({
		type: "showTip",
		event: "showTip",
		update: "tooltip:manuallyShowTip"
	}, Ae), e.registerAction({
		type: "hideTip",
		event: "hideTip",
		update: "tooltip:manuallyHideTip"
	}, Ae);
}
//#endregion
//#region node_modules/zrender/lib/canvas/Layer.js
function ZO(e, t, n) {
	var r = p.createCanvas(), i = t.getWidth(), a = t.getHeight(), o = r.style;
	return o && (o.position = "absolute", o.left = "0", o.top = "0", o.width = i + "px", o.height = a + "px", r.setAttribute("data-zr-dom-id", e)), r.width = i * n, r.height = a * n, r;
}
function QO(e) {
	return !e.__cursors.get(0);
}
function $O(e) {
	var t = e.__cursors.get(0);
	return {
		startIdx: t ? t.startIdx : 0,
		endIdx: t ? t.endIdx : 0
	};
}
var ek = function(e) {
	r(t, e);
	function t(t, n, r) {
		var i = e.call(this) || this;
		i.motionBlur = !1, i.lastFrameAlpha = .7, i.dpr = 1, i.virtual = !1, i.config = {}, i.zlevel = 0, i.zlevel2 = 0, i.maxRepaintRectCount = 5, i.__dirty = !0, i.__firstTimePaint = !0, i.__prevIdx = {
			startIdx: 0,
			endIdx: 0
		};
		var a;
		r ||= Ei, typeof t == "string" ? a = ZO(t, n, r) : W(t) && (a = t, t = a.id), i.id = t, i.dom = a;
		var o = a.style;
		return o && (Oe(a), a.onselectstart = function() {
			return !1;
		}, o.padding = "0", o.margin = "0", o.borderWidth = "0"), i.painter = n, i.dpr = r, i;
	}
	return t.prototype.afterBrush = function() {
		this.__prevIdx = $O(this);
	}, t.prototype.initContext = function() {
		this.ctx = this.dom.getContext("2d"), this.ctx.dpr = this.dpr;
	}, t.prototype.setUnpainted = function() {
		this.__firstTimePaint = !0;
	}, t.prototype.createBackBuffer = function() {
		var e = this.dpr;
		this.domBack = ZO("back-" + this.id, this.painter, e), this.ctxBack = this.domBack.getContext("2d"), e !== 1 && this.ctxBack.scale(e, e);
	}, t.prototype.createRepaintRects = function(e, t, n, r) {
		if (this.__firstTimePaint) return this.__firstTimePaint = !1, null;
		var i = [], a = this.maxRepaintRectCount, o = !1, s = new J(0, 0, 0, 0);
		function c(e) {
			if (e.isFinite() && !e.isZero()) {
				if (i.length === 0) {
					var t = new J(0, 0, 0, 0);
					t.copy(e), i.push(t);
				} else {
					for (var n = !1, r = Infinity, c = 0, l = 0; l < i.length; ++l) {
						var u = i[l];
						if (u.intersect(e)) {
							var d = new J(0, 0, 0, 0);
							d.copy(u), d.union(e), i[l] = d, n = !0;
							break;
						}
						if (o) {
							s.copy(e), s.union(u);
							var f = e.width * e.height, p = u.width * u.height, m = s.width * s.height - f - p;
							m < r && (r = m, c = l);
						}
					}
					if (o && (i[c].union(e), n = !0), !n) {
						var t = new J(0, 0, 0, 0);
						t.copy(e), i.push(t);
					}
					o ||= i.length >= a;
				}
			}
		}
		for (var l = $O(this), u = l.startIdx; u < l.endIdx; ++u) {
			var d = e[u];
			if (d) {
				var f = d.shouldBePainted(n, r, !0, !0), p = d.__isRendered && (d.__dirty & 1 || !f) ? d.getPrevPaintRect() : null;
				p && c(p);
				var m = f && (d.__dirty & 1 || !d.__isRendered) ? d.getPaintRect() : null;
				m && c(m);
			}
		}
		for (var h = this.__prevIdx, u = h.startIdx; u < h.endIdx; ++u) {
			var d = t[u], f = d && d.shouldBePainted(n, r, !0, !0);
			if (d && (!f || !d.__zr) && d.__isRendered) {
				var p = d.getPrevPaintRect();
				p && c(p);
			}
		}
		var g;
		do {
			g = !1;
			for (var u = 0; u < i.length;) if (i[u].isZero()) i.splice(u, 1);
			else {
				for (var _ = u + 1; _ < i.length;) i[u].intersect(i[_]) ? (g = !0, i[u].union(i[_]), i.splice(_, 1)) : _++;
				u++;
			}
		} while (g);
		return this._paintRects = i, i;
	}, t.prototype.debugGetPaintRects = function() {
		return (this._paintRects || []).slice();
	}, t.prototype.resize = function(e, t) {
		var n = this.dpr, r = this.dom, i = r.style, a = this.domBack;
		i && (i.width = e + "px", i.height = t + "px"), r.width = e * n, r.height = t * n, a && (a.width = e * n, a.height = t * n, n !== 1 && this.ctxBack.scale(n, n));
	}, t.prototype.clear = function(e, t, n) {
		var r = this.dom, i = this.ctx, a = r.width, o = r.height;
		t ||= this.clearColor;
		var s = this.motionBlur && !e, c = this.lastFrameAlpha, l = this.dpr, u = this;
		s && (this.domBack || this.createBackBuffer(), this.ctxBack.globalCompositeOperation = "copy", this.ctxBack.drawImage(r, 0, 0, a / l, o / l));
		var d = this.domBack;
		function f(e, n, r, a) {
			if (i.clearRect(e, n, r, a), t && t !== "transparent") {
				var o = void 0;
				ue(t) ? (o = (t.global || t.__width === r && t.__height === a) && t.__canvasGradient || Ny(i, t, {
					x: 0,
					y: 0,
					width: r,
					height: a
				}), t.__canvasGradient = o, t.__width = r, t.__height = a) : de(t) && (t.scaleX = t.scaleX || l, t.scaleY = t.scaleY || l, o = Gy(i, t, { dirty: function() {
					u.setUnpainted(), u.painter.refresh();
				} })), i.save(), i.fillStyle = o || t, i.fillRect(e, n, r, a), i.restore();
			}
			s && (i.save(), i.globalAlpha = c, i.drawImage(d, e, n, r, a), i.restore());
		}
		!n || s ? f(0, 0, a, o) : n.length && F(n, function(e) {
			f(e.x * l, e.y * l, e.width * l, e.height * l);
		});
	}, t;
}(Qe), tk = 1e5, nk = 314159, rk = void 0, ik = 1, ak = 2;
function ok(e) {
	return e ? e.__builtin__ ? !0 : typeof e.resize == "function" && typeof e.refresh == "function" : !1;
}
function sk(e, t) {
	var n = document.createElement("div");
	return n.style.cssText = [
		"position:relative",
		"width:" + e + "px",
		"height:" + t + "px",
		"padding:0",
		"margin:0",
		"border-width:0"
	].join(";") + ";", n;
}
function ck(e, t, n, r) {
	var i = new ek(e, t, t.dpr);
	return i.zlevel = n, i.zlevel2 = r, i.__builtin__ = !0, lk(i), i;
}
function lk(e) {
	e.__cursorStack = [], e.__cursors = K();
}
function uk(e) {
	return e.startIdx = e.drawIdx = e.endIdx = e.endIdxNew = 0, e.used = !1, e.first = e.last = NaN, e.notClearIdx = -1, e;
}
function dk(e, t) {
	var n = e.__cursors, r = +t;
	return n.get(r) || (e.__cursorStack.push(r), n.set(r, uk({ key: r })));
}
function fk(e, t) {
	for (var n = e.__cursorStack, r = 0; r < n.length; r++) t(e.__cursors.get(n[r]));
}
function pk(e, t) {
	var n = e.layers;
	return n[t] || (n[t] = [
		,
		,
		,
	]);
}
function mk(e, t, n) {
	for (var r = e.layerStack, i = 0; i < r.length; i++) {
		var a = r[i].zl, o = r[i].zl2, s = e.layers[a][o];
		(!n || (!(n & hk) || s.__builtin__) && (!(n & gk) || !s.__builtin__) && (!(n & _k) || s !== e.hoverlayer)) && t(s, a, o, i);
	}
}
var hk = 1, gk = 2, _k = 4, vk = hk | _k, yk = function() {
	function e(e, t, n, r) {
		this.type = "canvas", this._prevDisplayList = [], this._layerConfig = {}, this._needsManuallyCompositing = !1, this.type = "canvas", this._i = {
			layerStack: [],
			layers: []
		};
		var i = !e.nodeName || e.nodeName.toUpperCase() === "CANVAS";
		if (this._opts = n = j({}, n || {}), this.dpr = n.devicePixelRatio || Ei, this._singleCanvas = i, this.root = e, e.style && (Oe(e), e.innerHTML = ""), this.storage = t, this._prevDisplayList = [], i) {
			var a = e, o = a.width, s = a.height;
			n.width != null && (o = n.width), n.height != null && (s = n.height), this.dpr = n.devicePixelRatio || 1, a.width = o * this.dpr, a.height = s * this.dpr, this._width = o, this._height = s;
			var c = ck(a, this, nk, 0);
			c.initContext(), this._insertLayer(c, nk, 0, !0), this._domRoot = e;
		} else {
			this._width = Iy(e, 0, n), this._height = Iy(e, 1, n);
			var l = this._domRoot = sk(this._width, this._height);
			e.appendChild(l);
		}
	}
	return e.prototype.getType = function() {
		return "canvas";
	}, e.prototype.isSingleCanvas = function() {
		return this._singleCanvas;
	}, e.prototype.getViewportRoot = function() {
		return this._domRoot;
	}, e.prototype.getViewportRootOffset = function() {
		var e = this.getViewportRoot();
		if (e) return {
			offsetLeft: e.offsetLeft || 0,
			offsetTop: e.offsetTop || 0
		};
	}, e.prototype.refresh = function(e) {
		var t = e && !W(e) ? { paintAll: !!e } : e || {}, n = G(t.refresh, !0), r = G(t.refreshHover, !1);
		if (r && (this._hoverLayerDirty = ak), !n) return r && this._paintHoverList(this.storage.getDisplayList(!1)), this;
		var i = this.storage.getDisplayList(!0);
		this._updateLayerStatus(i, t.paintAll), this._redrawId = Math.random();
		var a = this._prevDisplayList;
		this._paintList(i, a, this._redrawId);
		var o = this._backgroundColor;
		return mk(this._i, function(e, t, n, r) {
			e.refresh && e.refresh(r === 0 ? o : null);
		}, gk), this._opts.useDirtyRect && (this._prevDisplayList = i.slice()), this;
	}, e.prototype._paintHoverList = function(e) {
		var t = this._i.hoverlayer, n = this._hoverLayerDirty;
		if (this._hoverLayerDirty = rk, n !== rk && (!t && n === ak && (t = this._i.hoverlayer = this._ensureLayer(tk)), t)) {
			t.clear();
			for (var r = {
				inHover: !0,
				viewWidth: this._width,
				viewHeight: this._height,
				beforeBrushParam: {}
			}, i, a = 0, o = e.length; a < o; a++) {
				var s = e[a];
				if (s.__inHover) {
					i || (i = t.ctx, i.save());
					var c = s.__hoverStyle, l = void 0;
					c && (l = s.style, s.style = c), ub(i, s, r), c && (s.style = l);
				}
			}
			i && (db(i, r), i.restore());
		}
	}, e.prototype.getHoverLayer = function() {
		return this._ensureLayer(tk);
	}, e.prototype.paintOne = function(e, t) {
		lb(e, t);
	}, e.prototype._paintList = function(e, t, n) {
		if (this._redrawId === n) {
			var r = this._doPaintList(e, t);
			if (this._needsManuallyCompositing && this._compositeManually(), r) mk(this._i, function(e) {
				e.afterBrush && e.afterBrush();
			}, vk), this._paintHoverList(e);
			else {
				var i = this;
				Nn(function() {
					i._paintList(e, t, n);
				});
			}
		}
	}, e.prototype._compositeManually = function() {
		var e = this._ensureLayer(nk).ctx, t = this._domRoot.width, n = this._domRoot.height;
		e.clearRect(0, 0, t, n), mk(this._i, function(r) {
			r.virtual && e.drawImage(r.dom, 0, 0, t, n);
		}, hk);
	}, e.prototype._doPaintList = function(e, t) {
		var n = this, r = !0;
		return mk(this._i, function(i) {
			var a = !1;
			if (fk(i, function(e) {
				(e.drawIdx < e.endIdx || e.notClearIdx >= 0) && (a = !0);
			}), a || i.__dirty) {
				var o = n._opts.useDirtyRect && !QO(i) ? i.createRepaintRects(e, t, n._width, n._height) : null, s = n._i.layerStack[0], c = !0;
				if (i.__dirty) {
					c = !1, i.__dirty = !1;
					var l = i.zlevel === s.zl && i.zlevel2 === s.zl2 ? n._backgroundColor : null;
					i.clear(!1, l, o);
				}
				fk(i, function(t) {
					var a = n._paintPerCursor(i, t, e, o, c);
					r &&= a;
				});
			}
		}, vk), a.wxa && mk(this._i, function(e) {
			e && e.ctx && e.ctx.draw && e.ctx.draw();
		}), r;
	}, e.prototype._paintPerCursor = function(e, t, n, r, i) {
		var a = e.ctx;
		if (r) {
			if (!r.length) t.drawIdx = t.endIdx;
			else for (var o = this.dpr, s = 0; s < r.length; ++s) {
				var c = r[s];
				a.save(), a.beginPath(), a.rect(c.x * o, c.y * o, c.width * o, c.height * o), a.clip(), this._paintPerCursorInRect(e, t, n, c, i), a.restore();
			}
		} else a.save(), this._paintPerCursorInRect(e, t, n, null, i), a.restore();
		return t.drawIdx >= t.endIdx;
	}, e.prototype._paintPerCursorInRect = function(e, t, n, r, i) {
		for (var a = {
			inHover: !1,
			allClipped: !1,
			prevEl: null,
			viewWidth: this._width,
			viewHeight: this._height,
			beforeBrushParam: { contentRetained: i }
		}, o = e.ctx, s = QO(e), c = s && p.getTime(), l = t.drawIdx, u = t.notClearIdx, d = u >= 0 ? Math.min(u, l) : l; d < t.endIdx; d++) {
			var f = n[d];
			if (!(d < l && !f.notClear)) {
				if (f.__inHover && (this._hoverLayerDirty = ak), r != null) {
					var m = f.getPaintRect();
					m && m.intersect(r) && (ub(o, f, a), f.setPrevPaintRect(m));
				} else ub(o, f, a);
				if (s && p.getTime() - c > 15) {
					d++;
					break;
				}
			}
		}
		db(o, a), t.drawIdx = Math.max(d, l);
	}, e.prototype.getLayer = function(e, t) {
		return this._ensureLayer(e, 0, t);
	}, e.prototype._ensureLayer = function(e, t, n) {
		t ||= 0;
		var r = this._singleCanvas;
		r && !this._needsManuallyCompositing && (e = nk, t = 0);
		var i = pk(this._i, e)[t];
		return i || (i = ck("zr_" + e + "." + t, this, e, t), this._layerConfig[e] && A(i, this._layerConfig[e], !0), (n || r && e !== nk) && (i.virtual = !0), this._insertLayer(i, e, t, !1), i.initContext()), i;
	}, e.prototype.insertLayer = function(e, t) {
		this._insertLayer(t, e, 0, !1);
	}, e.prototype._insertLayer = function(e, t, n, r) {
		var i = this._i, a = i.layers, o = i.layerStack, s = this._domRoot, c = null;
		if (!(a[t] && a[t][n]) && ok(e)) {
			for (var l = o.length, u = 0; u < l && (o[u].zl < t || o[u].zl === t && o[u].zl2 < n);) u++;
			if (u > 0 && (c = pk(i, o[u - 1].zl)[o[u - 1].zl2]), o.splice(u, 0, {
				zl: t,
				zl2: n
			}), pk(i, t)[n] = e, !r && !e.virtual) {
				if (c) {
					var d = c.dom;
					d.nextSibling ? s.insertBefore(e.dom, d.nextSibling) : s.appendChild(e.dom);
				} else s.firstChild ? s.insertBefore(e.dom, s.firstChild) : s.appendChild(e.dom);
			}
			e.painter ||= this;
		}
	}, e.prototype.eachLayer = function(e, t) {
		return mk(this._i, function(n, r) {
			e.call(t, n, r);
		});
	}, e.prototype.eachBuiltinLayer = function(e, t) {
		return mk(this._i, function(n, r) {
			e.call(t, n, r);
		}, hk);
	}, e.prototype.eachOtherLayer = function(e, t) {
		return mk(this._i, function(n, r) {
			e.call(t, n, r);
		}, gk);
	}, e.prototype.getLayers = function() {
		var e = {};
		return mk(this._i, function(t, n, r) {
			e[t.id] = t;
		}), e;
	}, e.prototype._updateLayerStatus = function(e, t) {
		var n = this;
		if (n._singleCanvas) for (var r = 1; r < e.length; r++) {
			var i = e[r];
			if (i.zlevel !== e[r - 1].zlevel || i.incremental) {
				n._needsManuallyCompositing = !0;
				break;
			}
		}
		mk(n._i, function(e) {
			e.__dirty = !1, fk(e, function(e) {
				e.used = !1, e.endIdxNew = 0, e.notClearIdx = -1;
			});
		}, vk);
		for (var a, o = null, s = null, c = !1, l = 0, u = e.length; l < u; l++) {
			var i = e[l], d = i.zlevel, f = i.incremental, p = void 0;
			if (a !== d && (a = d, c = !1), f ? (c = !0, p = 1) : p = c ? 2 : 0, (!o || d !== o.zlevel || p !== o.zlevel2) && (o = n._ensureLayer(d, p), s = null, !o.__builtin__)) O("ZLevel " + d + " has been used by unknown layer " + o.id);
			else {
				if ((!s || f !== s.key) && (s = dk(o, f), !s.used)) {
					if (s.used = !0, !t && s.first === i.id) {
						var m = l - s.startIdx;
						s.startIdx = l, s.drawIdx += m, s.endIdx += m;
					} else o.__dirty = !0, s.first = i.id, s.startIdx = s.drawIdx = l, s.endIdx = l + 1;
				}
				s.endIdxNew = l + 1, i.__dirty & 1 && !i.__inHover && ((!f || !i.notClear && l < s.drawIdx) && (o.__dirty = !0), f && i.notClear && s.notClearIdx < 0 && (s.notClearIdx = l));
			}
		}
		mk(n._i, function(t) {
			for (var r = t.__cursorStack, i = t.__cursors, a = r.length - 1; a >= 0; a--) {
				var o = i.get(r[a]);
				if (!o.used) t.__dirty = !0, i.removeKey(r[a]), r.splice(a, 1);
				else {
					var s = o.endIdxNew;
					(QO(t) ? s < o.drawIdx : s !== o.endIdx || !s || e[s - 1].id !== o.last) && (t.__dirty = !0), o.endIdx = o.endIdxNew, o.last = s ? e[s - 1].id : NaN;
				}
			}
			t.__dirty && (fk(t, function(e) {
				e.drawIdx = e.startIdx;
			}), n._hoverLayerDirty === rk && (n._hoverLayerDirty = ik));
		}, vk);
	}, e.prototype.clear = function() {
		return mk(this._i, function(e) {
			e.clear(), lk(e);
		}, hk), this;
	}, e.prototype.setBackgroundColor = function(e) {
		this._backgroundColor = e, mk(this._i, function(e) {
			e.setUnpainted();
		});
	}, e.prototype.configLayer = function(e, t) {
		if (t) {
			var n = this._layerConfig;
			n[e] ? A(n[e], t, !0) : n[e] = t, mk(this._i, function(e, t) {
				A(e, n[t], !0);
			});
		}
	}, e.prototype.delLayer = function(e) {
		for (var t = this._i.layerStack, n = this._i.layers, r = t.length - 1; r >= 0; r--) {
			var i = t[r];
			if (i.zl === e) {
				var a = n[e][i.zl2];
				if (a.__builtin__) continue;
				if (t.splice(r, 1), n[e][i.zl2] = void 0, !a.virtual) {
					var o = a.dom.parentNode;
					o && o.removeChild(a.dom);
				}
			}
		}
	}, e.prototype.resize = function(e, t) {
		if (this._domRoot.style) {
			var n = this._domRoot;
			n.style.display = "none";
			var r = this._opts, i = this.root;
			e != null && (r.width = e), t != null && (r.height = t), e = Iy(i, 0, r), t = Iy(i, 1, r), n.style.display = "", (this._width !== e || t !== this._height) && (n.style.width = e + "px", n.style.height = t + "px", mk(this._i, function(n) {
				n.resize(e, t);
			}), this.refresh({ paintAll: !0 })), this._width = e, this._height = t;
		} else {
			if (e == null || t == null) return;
			this._width = e, this._height = t, this._ensureLayer(nk).resize(e, t);
		}
		return this;
	}, e.prototype.clearLayer = function(e) {
		F(this._i.layers[e], function(e) {
			e && !e.__builtin__ && e.clear();
		});
	}, e.prototype.dispose = function() {
		this.root.innerHTML = "", this.root = this.storage = this._domRoot = this._i = null;
	}, e.prototype.getRenderedCanvas = function(e) {
		if (e ||= {}, this._singleCanvas && !this._compositeManually) return this._i.layers[nk][0].dom;
		var t = new ek("image", this, e.pixelRatio || this.dpr);
		t.initContext(), t.clear(!1, e.backgroundColor || this._backgroundColor);
		var n = t.ctx;
		if (e.pixelRatio <= this.dpr) {
			this.refresh();
			var r = t.dom.width, i = t.dom.height;
			mk(this._i, function(e) {
				e.__builtin__ ? n.drawImage(e.dom, 0, 0, r, i) : e.renderToCanvas && (n.save(), e.renderToCanvas(n), n.restore());
			});
		} else {
			for (var a = {
				inHover: !1,
				viewWidth: this._width,
				viewHeight: this._height,
				beforeBrushParam: {}
			}, o = this.storage.getDisplayList(!0), s = 0, c = o.length; s < c; s++) {
				var l = o[s];
				ub(n, l, a);
			}
			db(n, a);
		}
		return t.dom;
	}, e.prototype.getWidth = function() {
		return this._width;
	}, e.prototype.getHeight = function() {
		return this._height;
	}, e;
}();
//#endregion
//#region node_modules/echarts/lib/renderer/installCanvasRenderer.js
function bk(e) {
	e.registerPainter("canvas", yk);
}
//#endregion
//#region Assets/Scripts/topology-graph.js
lC([
	wD,
	XO,
	bk
]);
var xk = "#111111", Sk = "#E2DED5", Ck = "#6E675C", wk = "#D97706", Tk = "#FCFBF7", Ek = "#F4F4F0", Dk = "'IBM Plex Mono', ui-monospace, SFMono-Regular, Menlo, Consolas, monospace", Ok = 170, kk = 150, Ak = 110, jk = 34, Mk = /* @__PURE__ */ new Map();
function Nk() {
	return {
		show: !0,
		formatter: "{c}",
		color: Ck,
		fontFamily: Dk,
		fontSize: 10,
		backgroundColor: Ek,
		padding: [1, 3]
	};
}
function Pk(e) {
	return {
		color: Tk,
		borderColor: e.isCenter ? wk : xk,
		borderWidth: e.isCenter ? 2 : 1,
		borderType: e.isMissing ? "dashed" : "solid",
		opacity: e.isMissing ? .65 : 1
	};
}
function Fk(e) {
	let t = (e.nodes || []).map((e) => ({
		id: e.id,
		name: e.label,
		kind: e.kind,
		kindText: e.kindText,
		status: e.statusHint || "",
		x: (e.order || 0) * Ok,
		y: (e.layer || 0) * kk,
		symbol: "rect",
		symbolSize: [Ak, jk],
		itemStyle: Pk(e),
		label: {
			show: !0,
			position: "inside",
			formatter: [e.label, e.kindText].join("\n"),
			color: xk,
			fontFamily: Dk,
			fontSize: 10,
			lineHeight: 13
		}
	})), n = (e.edges || []).map((e) => ({
		source: e.from,
		target: e.to,
		value: e.relation,
		lineStyle: {
			color: Sk,
			width: 1,
			curveness: 0
		},
		symbol: ["none", "arrow"],
		symbolSize: 6,
		label: Nk()
	}));
	return {
		animation: !1,
		tooltip: {
			backgroundColor: Tk,
			borderColor: Sk,
			textStyle: {
				color: xk,
				fontFamily: Dk,
				fontSize: 12
			},
			formatter: (e) => {
				if (e.dataType !== "node") return "";
				let t = [e.data.name, e.data.kindText];
				return e.data.status && t.push(e.data.status), t.join("\n");
			}
		},
		series: [{
			type: "graph",
			layout: "none",
			roam: !0,
			data: t,
			links: n,
			top: 30,
			bottom: 30,
			left: 40,
			right: 40,
			emphasis: {
				focus: "adjacency",
				itemStyle: { borderColor: wk }
			}
		}]
	};
}
function Ik(e, t) {
	Rk(e);
	let n = Lx(e);
	n.setOption(Fk(t));
	let r = () => n.resize();
	window.addEventListener("resize", r), n.on("click", (t) => {
		if (t.dataType !== "node") return;
		let n = Mk.get(e)?.onNodeClick;
		n && n.invokeMethodAsync("OnNodeClicked", t.data.id).catch(() => {});
	}), Mk.set(e, {
		chart: n,
		onResize: r,
		onNodeClick: t.dotnetRef
	});
}
function Lk(e, t) {
	let n = Mk.get(e);
	n && (n.onNodeClick = t.dotnetRef, n.chart.setOption(Fk(t)));
}
function Rk(e) {
	let t = Mk.get(e);
	t && (window.removeEventListener("resize", t.onResize), t.chart.dispose(), Mk.delete(e));
}
//#endregion
export { Rk as dispose, Ik as mount, Lk as update };

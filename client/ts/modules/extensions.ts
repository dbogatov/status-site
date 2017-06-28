// These are the extensions to native JavaScript objects

// Ensure this is treated as a module.
export { };


declare global {
	interface Array<T> {
		sortByProperty(delegate: (el: T) => number): T[];
		min(delegate: (el: T) => number): number;
		max(delegate: (el: T) => number): number;
	}
}

Array.prototype.sortByProperty = function (delegate: (el) => number) {
	return this.sort((a, b) => delegate(a) - delegate(b));
};

Array.prototype.min = function (delegate: (el) => number) {
	var i = 0;
	var _min = delegate(this[i]);
	for (i = 0; i < this.length; i++) {
		if (_min > delegate(this[i]))
			_min = delegate(this[i]);
	}
	return _min;
};

Array.prototype.max = function (delegate: (el) => number) {
	var i = 0;
	var _max = delegate(this[i]);
	for (i = 0; i < this.length; i++) {
		if (_max < delegate(this[i]))
			_max = delegate(this[i]);
	}
	return _max;
};

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

Array.prototype.min = function(delegate: (el) => number) {
  return Math.min.apply(delegate, this);
};

Array.prototype.max = function(delegate: (el) => number) {
  return Math.max.apply(delegate, this);
};

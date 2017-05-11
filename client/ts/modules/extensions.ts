// These are the extensions to native JavaScript objects

// Ensure this is treated as a module.
export { };


declare global {
	interface Array<T> {
		sortByProperty(delegate: (el: T) => number): T[];
	}
}

Array.prototype.sortByProperty = function (delegate: (el) => number) {
	return this.sort((a, b) => delegate(a) - delegate(b));
};

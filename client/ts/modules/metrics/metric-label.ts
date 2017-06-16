/**
 * 
 * Abstract class responsible for labels displayed withing the metric card.
 * Renders as SPAN in the UI.
 * 
 * @export
 * @abstract
 * @class MetricLabel
 * @template T - Type of severity for label
 */
export abstract class MetricLabel<T>  {

	/**
	 * Severity of a the label
	 * 
	 * @protected
	 * @type {T}
	 * @memberOf MetricLabel
	 */
	protected severity: T;

	/**
	 * Color of the text for the label
	 * 
	 * @protected
	 * @type {string}
	 * @memberOf MetricLabel
	 */
	protected textColor: string;
	/**
	 * Background color of the label
	 * 
	 * @protected
	 * @type {string}
	 * @memberOf MetricLabel
	 */
	protected backgroundColor: string;
	
	/**
	 * Text part of the label
	 * 
	 * @private
	 * @type {string}
	 * @memberOf MetricLabel
	 */
	private text: string;
	/**
	 * jQuery element representing a SPAN in which label is displayed.
	 * 
	 * @private
	 * @type {JQuery}
	 * @memberOf MetricLabel
	 */
	private domElement: JQuery;


	/**
	 * Creates an instance of MetricLabel.
	 * @param {string} text - label's text
	 * @param {string} severity - label's severity
	 * @param {string} cssSelector - selector which will resolve to label's span element
	 * 
	 * @memberOf MetricLabel
	 */
	constructor(text: string, severity: string, cssSelector: string) {
		this.text = text;
		this.setSeverity(severity);
		this.domElement = $(cssSelector);
	}

	/**
	 * 
	 * Renders (properly displays or hides) label in the UI.
	 * 
	 * @memberOf MetricLabel
	 */
	public render(): void {
		this.setColor();
		this.renderText();

		this.shouldShow() ? this.domElement.show() : this.domElement.hide();
	}

	/**
	 * Set's the color property of the label
	 * 
	 * @protected
	 * @abstract
	 * 
	 * @memberOf MetricLabel
	 */
	protected abstract setColor() : void;
	/**
	 * Set the severity property of the label
	 * 
	 * @protected
	 * @abstract
	 * @param {string} severity - text representation of severity (usually received from server)
	 * 
	 * @memberOf MetricLabel
	 */
	protected abstract setSeverity(severity: string) : void;
	/**
	 * Returns true if metric needs to be displayed in UI, and false otherwise
	 * 
	 * @protected
	 * @abstract
	 * @returns {boolean} - whether metric should be displayed or not
	 * 
	 * @memberOf MetricLabel
	 */
	protected abstract shouldShow() : boolean;

	/**
	 * Renders text along with foreground and background color in the UI
	 * 
	 * @private
	 * 
	 * @memberOf MetricLabel
	 */
	private renderText() : void {
		this.domElement.text(this.text);
		this.domElement.css("background-color", this.backgroundColor);
		this.domElement.css("color", this.textColor);
	}
}


/**
 * Severity options for AutoLabel.
 * 
 * @enum {number}
 */
enum AutoLabelSeverity {
	Normal = 1, Warning, Critical
}


/**
 * Responsible for automatic labels displayed withing the metric card.
 * 
 * @export
 * @class AutoLabel
 * @extends {MetricLabel<AutoLabelSeverity>}
 */
export class AutoLabel extends MetricLabel<AutoLabelSeverity> {


	/**
	 * Creates an instance of AutoLabel.
	 * @param {string} text - Text part of the label
	 * @param {string} severity - String representation of severity of the label 
	 * @param {string} metricIdentifier - UI identifier (the one in data-identifier attribute) of the containing metric
	 * 
	 * @memberOf AutoLabel
	 */
	constructor(text: string, severity: string, metricIdentifier: string) {
		super(text, severity, `[data-identifier="${metricIdentifier}"].metric-auto-label`);
	}


	/**
	 * 
	 * 
	 * @protected
	 * 
	 * @memberOf AutoLabel
	 */
	protected setColor() : void {
		switch (this.severity) {
			case AutoLabelSeverity.Normal:
				this.backgroundColor = "aquamarine";
				this.textColor = "black";
				break;
			case AutoLabelSeverity.Warning:
				this.backgroundColor = "gold";
				this.textColor = "black";
				break;
			case AutoLabelSeverity.Critical:
				this.backgroundColor = "crimson";
				this.textColor = "white";
				break;
		}
	}


	/**
	 * 
	 * 
	 * @protected
	 * @param {string} severity 
	 * 
	 * @memberOf AutoLabel
	 */
	protected setSeverity(severity: string) : void {
		this.severity = AutoLabelSeverity[severity];
	};


	/**
	 * 
	 * 
	 * @protected
	 * @returns {boolean} 
	 * 
	 * @memberOf AutoLabel
	 */
	protected shouldShow() : boolean {
		return true;
	}
}


/**
 * Severity options for ManualLabel.
 * 
 * @enum {number}
 */
enum ManualLabelSeverity {
	None = 1, Investigating
}


/**
 * Responsible for manual labels displayed withing the metric card.
 * 
 * @export
 * @class ManualLabel
 * @extends {MetricLabel<ManualLabelSeverity>}
 */
export class ManualLabel extends MetricLabel<ManualLabelSeverity> {


	/**
	 * Creates an instance of AutoLabel.
	 * @param {string} text - Text part of the label
	 * @param {string} severity - String representation of severity of the label 
	 * @param {string} metricIdentifier - UI identifier (the one in data-identifier attribute) of the containing metric
	 * 
	 * @memberOf ManualLabel
	 */
	constructor(text: string, severity: string, metricIdentifier: string) {
		super(text, severity, `[data-identifier="${metricIdentifier}"].metric-manual-label`);
	}


	/**
	 * 
	 * 
	 * @protected
	 * 
	 * @memberOf ManualLabel
	 */
	protected setColor() : void {
		switch (this.severity) {
			case ManualLabelSeverity.Investigating:
				this.backgroundColor = "gold";
				this.textColor = "black";
				break;
		}
	}


	/**
	 * 
	 * 
	 * @protected
	 * @param {string} severity 
	 * 
	 * @memberOf ManualLabel
	 */
	protected setSeverity(severity: string) : void {
		this.severity = ManualLabelSeverity[severity];
	};


	/**
	 * 
	 * 
	 * @protected
	 * @returns {boolean} 
	 * 
	 * @memberOf ManualLabel
	 */
	protected shouldShow() : boolean {
		return this.severity != ManualLabelSeverity.None;
	}
}

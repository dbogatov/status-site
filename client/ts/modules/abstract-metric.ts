import { Constants } from "./constants";
import { Utility, UIHelper } from "./utility";
import { AutoLabel, ManualLabel } from "./metric-label";


/**
 * Types of metrics
 * 
 * @export
 * @enum {number}
 */
export enum MetricType {
	CpuLoad = 1, UserAction, Compilation, Log, Ping
}


/**
 * Data structure represents common fields for data points
 * 
 * @export
 * @abstract
 * @class DataPoint
 */
export abstract class DataPoint {
	public timestamp: Date;
}

type jsonLabelValue = {
	Title: string;
	Severity: string;
}

type JsonMetricValues = {
	LastUpdated: string;

	CurrentValue: number;

	DayMin: number;
	DayMax: number;
	DayAvg: number;

	HourMin: number;
	HourMax: number;
	HourAvg: number;

	AutoLabel: jsonLabelValue;
	ManualLabel: jsonLabelValue;
}


/**
 * Data structure represents a label
 * 
 * @export
 * @class LabelValue
 */
export class LabelValue {

	/**
	 * Text part of the label
	 * 
	 * @type {string}
	 * @memberOf LabelValue
	 */
	public title: string;

	/**
	 * Severity of the label
	 * 
	 * @type {string}
	 * @memberOf LabelValue
	 */
	public severity: string;


	/**
	 * Creates an instance of LabelValue.
	 * @param {jsonLabelValue} json - parsed JSON object representing a label
	 * 
	 * @memberOf LabelValue
	 */
	constructor(json : jsonLabelValue) {
		this.title = json.Title;
		this.severity = json.Severity;
	}
}


/**
 * Data structure that represents metric values
 * 
 * @export
 * @class MetricValues
 */
export class MetricValues {

	/**
	 * 
	 * 
	 * @type {Date}
	 * @memberOf MetricValues
	 */
	public lastUpdated: Date;


	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public currentValue: number;


	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	
	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public dayMin: number;
	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public dayMax: number;
	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public dayAvg: number;

	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public hourMin: number;
	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public hourMax: number;
	/**
	 * 
	 * 
	 * @type {number}
	 * @memberOf MetricValues
	 */
	public hourAvg: number;

	/**
	 * 
	 * 
	 * @type {LabelValue}
	 * @memberOf MetricValues
	 */
	public autoLabel: LabelValue;
	/**
	 * 
	 * 
	 * @type {LabelValue}
	 * @memberOf MetricValues
	 */
	public manualLabel: LabelValue;

	/**
	 * Creates an instance of MetricValues.
	 * @param {JsonMetricValues} json - parsed JSON object representing a metric
	 * 
	 * @memberOf MetricValues
	 */
	constructor(json: JsonMetricValues) {
		this.lastUpdated = Utility.toDate(json.LastUpdated);

		this.currentValue = json.CurrentValue;

		this.dayMin = json.DayMin;
		this.dayMax = json.DayMax;
		this.dayAvg = json.DayAvg;

		this.hourMin = json.HourMin;
		this.hourMax = json.HourMax;
		this.hourAvg = json.HourAvg;

		this.autoLabel = new LabelValue(json.AutoLabel);
		this.manualLabel = new LabelValue(json.ManualLabel);
	}
}


/**
 * Abstract class representing a metric in the UI.
 * 
 * @export
 * @abstract
 * @class Metric
 * @template T - Data Point type for the specific metric
 */
export abstract class Metric<T extends DataPoint> {


	/**
	 * Type of the metric
	 * 
	 * @protected
	 * @type {MetricType}
	 * @memberOf Metric
	 */
	protected _metricType: MetricType;
	public get metricType(): MetricType {
		return this._metricType;
	}
	/**
	 * The source of the metric
	 * 
	 * @protected
	 * @type {string}
	 * @memberOf Metric
	 */
	protected _source: string;
	public get source(): string {
		return this._source;
	}


	/**
	 * Automatic label of the metric
	 * 
	 * @protected
	 * @type {AutoLabel}
	 * @memberOf Metric
	 */
	protected _autoLabel: AutoLabel;
	public get autoLabel(): AutoLabel {
		return this._autoLabel;
	}

	/**
	 * Manual label of the metric
	 * 
	 * @protected
	 * @type {ManualLabel}
	 * @memberOf Metric
	 */
	protected _manualLabel: ManualLabel;
	public get manualLabel(): ManualLabel {
		return this._manualLabel;
	}

	/**
	 * Array of data points for the metric
	 * 
	 * @protected
	 * @type {Array<T>}
	 * @memberOf Metric
	 */
	protected _data: Array<T>;
	public get data(): Array<T> {
		return this._data;
	}

	/**
	 * Numeric values for the metric
	 * 
	 * @protected
	 * @type {MetricValues}
	 * @memberOf Metric
	 */
	protected _values: MetricValues;
	public get values(): MetricValues {
		return this._values;
	}

	/**
	 * Identifier of the load and render task repetition
	 * 
	 * @private
	 * @type {number}
	 * @memberOf Metric
	 */
	private repetitionId: number;
	/**
	 * Flag representing if load and render task is running
	 * 
	 * @private
	 * @type {boolean}
	 * @memberOf Metric
	 */
	private isRunning: boolean = false;

	/**
	 * Creates an instance of Metric.
	 * @param {string} source - source of the metric
	 * 
	 * @memberOf Metric
	 */
	constructor(source: string) {
		this._source = source;
	}

	/**
	 * Renders plot in the UI (does not load data)
	 * 
	 * @protected
	 * @abstract
	 * 
	 * @memberOf Metric
	 */
	protected abstract renderPlot(): void;
	/**
	 * Returns data point for the metric from json object
	 * 
	 * @protected
	 * @abstract
	 * @param {*} json - JSON object of the data point for the metric
	 * @returns {T} - data point for the metric
	 * 
	 * @memberOf Metric
	 */
	protected abstract getDataPointFromJson(json: any): T;

	/**
	 * Renders labels in the UI (does not load data)
	 * 
	 * @protected
	 * 
	 * @memberOf Metric
	 */
	protected renderLabels(): void {
		this.autoLabel.render();
		this.manualLabel.render();
	}

	/**
	 * Render metric numeric values in the UI (does not load data)
	 * 
	 * @protected
	 * 
	 * @memberOf Metric
	 */
	protected renderValues(): void {
		$(`[data-identifier="${this.getMetricIdentifier()}"] .last-updated`).text(
			`at ${Utility.toHHMMSS(this.values.lastUpdated)}`
		);

		$(`[data-identifier="${this.getMetricIdentifier()}"] .metric-current-value strong`)
			.text(this.values.currentValue);

		$(`[data-identifier="${this.getMetricIdentifier()}"] .day-avg strong`).text(this.values.dayAvg);
		$(`[data-identifier="${this.getMetricIdentifier()}"] .day-min strong`).text(this.values.dayMin);
		$(`[data-identifier="${this.getMetricIdentifier()}"] .day-max strong`).text(this.values.dayMax);

		$(`[data-identifier="${this.getMetricIdentifier()}"] .hour-avg strong`).text(this.values.hourAvg);
		$(`[data-identifier="${this.getMetricIdentifier()}"] .hour-min strong`).text(this.values.hourMin);
		$(`[data-identifier="${this.getMetricIdentifier()}"] .hour-max strong`).text(this.values.hourMax);
	};

	/**
	 * Loads data from the API and stores it in properties.
	 * Does not render anything.
	 * 
	 * @param {number} [interval] - time interval representing how old data to load
	 * @returns {Promise<void>}
	 * 
	 * @memberOf Metric
	 */
	public async loadData(interval?: number): Promise<void> {

		this._data =
			(<Array<any>>(await Utility.get(
				Utility.generateQuery(
					Constants.GET_DATA_ENDPOINT,
					["Type", MetricType[this.metricType]],
					["Source", this.source],
					["TimePeriod", `${interval ? interval : 30 * Constants.UPDATE_INTERVAL}`]
				)
			))).map((element) => {
				return this.getDataPointFromJson(element);
			});

		this._values = new MetricValues(
			(await Utility.get(
				Utility.generateQuery(
					Constants.GET_METRICS_ENDPOINT,
					["Type", MetricType[this.metricType]],
					["Source", this.source]
				)
			))[0] // array of one element is expected
		);

		this._autoLabel = new AutoLabel(
			this.values.autoLabel.title, 
			this.values.autoLabel.severity, 
			this.getMetricIdentifier()
		);
		this._manualLabel = new ManualLabel(
			this.values.manualLabel.title, 
			this.values.manualLabel.severity, 
			this.getMetricIdentifier()
		);
	}


	/**
	 * 
	 * Starts load and render task.
	 * Executes the task immediately.
	 * 
	 * @memberOf Metric
	 */
	public turnOn(): void {
		let task = async () => {
			await this.loadData();
			this.render();
			UIHelper.notify(`Metric ${MetricType[this.metricType]} of ${this.source} has been updated!`, "inverse");
		};

		if (!this.isRunning) {
			this.repetitionId = window.setInterval(
				task, Constants.UPDATE_INTERVAL * 1000
			);

			window.setTimeout(task, 0);

			this.isRunning = true;
		}
	}


	/**
	 * 
	 * Stop load and render task.
	 * 
	 * @memberOf Metric
	 */
	public turnOff(): void {
		if (this.isRunning) {
			window.clearInterval(this.repetitionId);
			this.isRunning = false;
		}
	}


	/**
	 * 
	 * Renders all UI components of the metric.
	 * Does not load the data.
	 * 
	 * @memberOf Metric
	 */
	public render(): void {
		this.renderValues();
		this.renderPlot();
		this.renderLabels();
	}


	/**
	 * Stops load and render task.
	 * Send delete request to remove metric from the system.
	 * Nullifies (helps GC) its members.
	 * 
	 * @returns {Promise<void>} 
	 * 
	 * @memberOf Metric
	 */
	public async destroy() : Promise<void> {
		this.turnOff();
		$(`[data-identifier="${this.getMetricIdentifier()}"]`)
			.parent()
			.fadeOut(200, function() {
				$(this).remove();
			});

		await Utility.delete(Constants.REMOVE_METRIC_ENDPOINT, {
			source: this.source,
			type: MetricType[this.metricType]
		});

		this._data = null;
		this._values = null;
		this._autoLabel = null;
		this._manualLabel = null;
	}

	/**
	 * Returns a unique metric identifier used in data-identifier attribute of HTML
	 * 
	 * @public
	 * @returns {string} - unique identifier
	 * 
	 * @memberOf Metric
	 */
	public getMetricIdentifier(): string {
		return `${this.metricType}-${this.source}`;
	}
}

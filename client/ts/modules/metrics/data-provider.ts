import { MetricType, DataPoint, MetricValues } from "./abstract";
import * as Collections from 'typescript-collections';
import { Constants } from "../constants";
import { UIHelper, Utility } from "../utility";

/**
 * Supplies data to metrics.
 * 
 * @export
 * @interface IDataProvider
 */
export interface IDataProvider {

	/**
	 * Returns available raw data points for the metric.
	 * 
	 * @param {MetricType} type 
	 * @param {string} source 
	 * @returns {any[]} 
	 * @memberof IDataProvider
	 */
	getData(type: MetricType, source: string): any[];

	/**
	 * Returns available metric values for the metric
	 * 
	 * @param {MetricType} type 
	 * @param {string} source 
	 * @returns {MetricValues} 
	 * @memberof IDataProvider
	 */
	getValues(type: MetricType, source: string): MetricValues;

	/**
	 * Returns true if data is available in the provider, and false otherwise
	 * 
	 * @returns {boolean} 
	 * @memberof IDataProvider
	 */
	isLoaded(): boolean;
}

/**
 * POTO class
 * 
 * @class TypeSourceTuple
 */
class TypeSourceTuple {
	public type: MetricType;
	public source: string;

	constructor(type: MetricType, source: string) {
		this.type = type;
		this.source = source;
	}

	toString() {
		return `${this.type}-${this.source}`;
	}
}

/**
 * Data provider that retrieves and stores data for all metrics.
 * 
 * @export
 * @class SharedDataProvider
 * @implements {IDataProvider}
 */
export class SharedDataProvider implements IDataProvider {

	private _isLoaded: boolean = false;
	public isLoaded(): boolean {
		return this._isLoaded;
	}

	private _data: Collections.Dictionary<TypeSourceTuple, any[]> =
	new Collections.Dictionary<TypeSourceTuple, any[]>();

	private _values: Collections.Dictionary<TypeSourceTuple, MetricValues> =
	new Collections.Dictionary<TypeSourceTuple, MetricValues>();

	constructor() {
		this.turnOn();
	}

	getData(type: MetricType, source: string): any[] {
		return this._data.getValue(new TypeSourceTuple(type, source));
	}

	getValues(type: MetricType, source: string): MetricValues {
		return this._values.getValue(new TypeSourceTuple(type, source));
	}

	/**
	 * Starts periodic task of loading data for the provider.
	 * 
	 * @private
	 * @memberof SharedDataProvider
	 */
	private turnOn(): void {

		let task = async () => {
			await this.loadData();
			await this.loadValues();

			this._isLoaded = true;

			UIHelper.notify(`New data has been loaded!`, "inverse");
		};

		window.setInterval(
			task, Constants.UPDATE_INTERVAL * 1000
		);

		window.setTimeout(task, 0);
	}

	/**
	 * Loads data pints for all metrics to provider's cache.
	 * 
	 * @private
	 * @returns {Promise<void>} 
	 * @memberof SharedDataProvider
	 */
	private async loadData(): Promise<void> {
		let raw = await Utility.get(
			Utility.generateQuery(
				Constants.GET_DATA_ENDPOINT,
				["TimePeriod", `${3 * 30 * Constants.UPDATE_INTERVAL}`]
			)
		);

		this._data.clear();

		raw.forEach(element => {
			this._data.setValue(
				new TypeSourceTuple(element.Type, element.Source),
				element.Data
			)
		});
	}

	/**
	 * Loads metric values for all metrics to provider's cache.
	 * 
	 * @private
	 * @returns {Promise<void>} 
	 * @memberof SharedDataProvider
	 */
	private async loadValues(): Promise<void> {
		let raw = await Utility.get(
			Utility.generateQuery(
				Constants.GET_METRICS_ENDPOINT
			)
		);

		this._values.clear();

		raw.forEach(element => {
			this._values.setValue(
				new TypeSourceTuple(element.Type, element.Source),
				new MetricValues(element)
			)
		});
	}
}

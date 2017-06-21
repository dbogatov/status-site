import { MetricType, DataPoint, MetricValues } from "./abstract";
import * as Collections from 'typescript-collections';
import { Constants } from "../constants";
import { UIHelper, Utility } from "../utility";

export interface IDataProvider {

	getData(type: MetricType, source: string): any[];
	getValues(type: MetricType, source: string): MetricValues;
	isLoaded(): boolean;
}

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

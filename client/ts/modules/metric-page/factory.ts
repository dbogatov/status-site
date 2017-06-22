import { MetricType, Metric, DataPoint } from "../metrics/abstract";
import { MetricPage } from "./abstract";
import { PingMetricPage } from "./ping";
import { CpuLoadMetricPage } from "./cpu-load";

/**
 * 
 * 
 * @export
 * @class MetricPageFactory
 */
export class MetricPageFactory {

	private source: string;
	private min: number;
	private max: number;

	constructor(source: string, min: number, max: number) {
		this.source = source;
		this.min = min;
		this.max = max;
	}

	/**
	 * Returns a proper metric page for the given type
	 * 
	 * @param {MetricType} type 
	 * @returns {MetricPage<Metric<DataPoint>>} 
	 * 
	 * @memberof MetricPageFactory
	 */
	public getMetricPage(type: MetricType): MetricPage<Metric<DataPoint>> {
		switch (type) {
			case MetricType.CpuLoad:
				return new CpuLoadMetricPage(this.source, this.min, this.max);
			case MetricType.Ping:
				return new PingMetricPage(this.source, this.min, this.max);
			default:
				throw `MetricPage type not supported. Type: ${type}`;
		}
	}
}

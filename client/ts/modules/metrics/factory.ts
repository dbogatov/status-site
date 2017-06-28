import { Metric, MetricType, DataPoint } from "./abstract";
import { PingMetric } from "../metrics/ping";
import { CpuLoadMetric } from "../metrics/cpu-load";
import { UserActionMetric } from "./user-action";

/**
 * 
 * 
 * @export
 * @class MetricFactory
 */
export class MetricFactory {

	private source : string;

	constructor(source: string) {
		this.source = source;
	}

	/**
	 * Returns a proper metric for the given type
	 * 
	 * @param {MetricType} type 
	 * @returns {Metric<DataPoint>} 
	 * 
	 * @memberof MetricFactory
	 */
	public getMetric(type: MetricType): Metric<DataPoint> {
		switch (type) {
			case MetricType.CpuLoad:
				return new CpuLoadMetric(this.source);
			case MetricType.Ping:
				return new PingMetric(this.source);
			case MetricType.UserAction:
				return new UserActionMetric(this.source);
			default:
				throw `Metric type not supported. Type: ${type}`;
		}
	}
}

import { Metric, MetricType, DataPoint } from "./abstract-metric";
import { Utility } from "./utility";
import "flot";
import "../vendor/jquery.flot.threshold.js";
import "../vendor/jquery.flot.tooltip.js";

type JsonCpuLoadDataPoint = {
	Timestamp: string;
	Value: number;
}

type JsonPingDataPoint = {
	Timestamp: string;
	ResponseTime: number;
	HttpStatusCode: number;
}


/**
 * Data type representing CPU Load data point
 * 
 * @export
 * @class CpuLoadDataPoint
 * @extends {DataPoint}
 */
export class CpuLoadDataPoint extends DataPoint {

	/**
	 * Value of load of CPU 0-100 percents
	 * 
	 * @type {number}
	 * @memberOf CpuLoadDataPoint
	 */
	public value: number;


	/**
	 * Creates an instance of CpuLoadDataPoint.
	 * @param {JsonCpuLoadDataPoint} json - JSON object representing CPU Load data point
	 * 
	 * @memberOf CpuLoadDataPoint
	 */
	constructor(json: JsonCpuLoadDataPoint) {
		super();

		this.timestamp = Utility.toDate(json.Timestamp);
		this.value = json.Value;
	}
}

export class PingDataPoint extends DataPoint {

	public responseTime: number;
	public httpStatusCode: number;

	constructor(json: JsonPingDataPoint) {
		super();

		this.timestamp = Utility.toDate(json.Timestamp);
		this.responseTime = json.ResponseTime;
		this.httpStatusCode = json.HttpStatusCode;
	}
}


/**
 * Class responsible for manipulating and rendering CPU Load metric
 * 
 * @export
 * @class CpuLoadMetric
 * @extends {Metric<CpuLoadDataPoint>}
 */
export class CpuLoadMetric extends Metric<CpuLoadDataPoint> {


	/**
	 * Creates an instance of CpuLoadMetric.
	 * @param {string} source - source of the metric
	 * 
	 * @memberOf CpuLoadMetric
	 */
	constructor(source: string) {
		super(source);

		this._metricType = MetricType.CpuLoad;
	}


	/**
	 * 
	 * 
	 * @protected
	 * 
	 * @memberOf CpuLoadMetric
	 */
	protected renderPlot(): void {
		var data = [];
		this
			.data
			.sort(dp => dp.timestamp.getMilliseconds())
			.reverse()
			.forEach((value, index, array) => data.push([index, value.value]));

		let plotOptions: any = {
			yaxis: {
				max: 100,
				min: 0
			},
			xaxis: {
				tickDecimals: 0,
				ticks: false
			},
			grid: {
				borderWidth: 0,
				labelMargin: 10,
				hoverable: true,
				clickable: true,
				mouseActiveRadius: 6
			},
			tooltip: {
				show: true,
				content: "Load %y%",
				defaultTheme: false,
				cssClass: "flot-tooltip"
			}
		};

		$.plot(
			$(`[data-identifier="${this.getMetricIdentifier()}"] .metric-chart`),
			[
				{
					data: data,
					lines: {
						show: true,
						fill: 0.50
					},
					color: 'rgb(200, 20, 30)',
					threshold: {
						below: 90,
						color: "#FFC107"
					}
				}
			],
			plotOptions
		);
	}

	/**
	 * 
	 * 
	 * @protected
	 * @param {*} json 
	 * @returns {CpuLoadDataPoint} 
	 * 
	 * @memberOf CpuLoadMetric
	 */
	protected getDataPointFromJson(json: any): CpuLoadDataPoint {
		return new CpuLoadDataPoint(json);
	}
}

export class PingMetric extends Metric<PingDataPoint> {


	constructor(source: string) {
		super(source);

		this._metricType = MetricType.Ping;
	}

	protected renderPlot(): void {
		var data = [], errors = [];
		this
			.data
			.sort(dp => dp.timestamp.getMilliseconds())
			.reverse()
			.forEach(
			(value, index, array) => {
				if (value.httpStatusCode == 200) {
					data.push([index, value.responseTime]);
				} else {
					errors.push([index, Number.MAX_SAFE_INTEGER]);
				}
			}
			);

		let plotOptions: any = {
			yaxis: {
				max: data.map(value => value[1]).reduce((a, b) => Math.max(a, b)),
				min: 0
			},
			xaxis: {
				tickDecimals: 0,
				ticks: false
			},
			grid: {
				borderWidth: 0,
				labelMargin: 10,
				hoverable: true,
				clickable: true,
				mouseActiveRadius: 6
			},
			tooltip: {
				show: true,
				content: "Response %y ms",
				defaultTheme: false,
				cssClass: "flot-tooltip"
			}
		};

		$.plot(
			$(`[data-identifier="${this.getMetricIdentifier()}"] .metric-chart`),
			[
				{
					data: data,
					lines: {
						show: true,
						fill: 0.50
					},
					color: "#FFC107"
				},
				{
					data: errors,
					bars: {
						show: true,
						align: 'center',
						barWidth: 1.9,
						lineWidth: 0,
						fillColor: {
							colors: [{
								opacity: 1.0
							}, {
								opacity: 1.0
							}]
						}
					},
					color: "rgb(200, 20, 30)",
					hoverable: false,
					highlightColor: "rgb(200, 20, 30)"
				},
			],
			plotOptions
		);
	}

	protected getDataPointFromJson(json: any): PingDataPoint {
		return new PingDataPoint(json);
	}
}

import { Metric, MetricType, DataPoint } from "./abstract";
import { Utility } from "../utility";
import "../extensions";
import "flot";
import "../../vendor/jquery.flot.threshold.js";
import "../../vendor/jquery.flot.tooltip.js";

type JsonHealthDataPoint = {
	Timestamp: string;
	Health: number;
	Data: HealthReportDataPoint[];
}

/**
 * Model for individual metric health (status)
 * 
 * @export
 * @class HealthReportDataPoint
 */
export class HealthReportDataPoint {
	public label: string;
	public source: string;
	public type: string;
}

/**
 * Data type representing system health data point
 * 
 * @export
 * @class HealthDataPoint
 * @extends {DataPoint}
 */
export class HealthDataPoint extends DataPoint {

	/**
	 * Health, 0-100 percents
	 * 
	 * @type {number}
	 * @memberOf HealthDataPoint
	 */
	public health: number;

	/**
	 * The detailed data used to compute overall health
	 * 
	 * @type {HealthReportDataPoint[]}
	 * @memberof HealthDataPoint
	 */
	public data: HealthReportDataPoint[];


	/**
	 * Creates an instance of HealthDataPoint.
	 * @param {JsonHealthDataPoint} json - JSON object representing Health data point
	 * 
	 * @memberOf HealthDataPoint
	 */
	constructor(json: JsonHealthDataPoint) {
		super();

		this.timestamp = Utility.toDate(json.Timestamp);
		this.health = json.Health;
		this.data = json.Data;
	}
}

/**
 * 
 * 
 * @export
 * @class HealthMetric
 * @extends {Metric<HealthDataPoint>}
 */
export class HealthMetric extends Metric<HealthDataPoint> {

	constructor(source: string) {
		super(source);

		this._metricType = MetricType.Health;

		this.startLoadUI();
	}

	public generatePlotData(): any {
		var data = [];
		this
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.forEach((value, index, array) => data.push([index, value.health]));

		return data;
	}

	protected renderPlot(): void {
		var data = this.generatePlotData();

		let plotOptions: any = {
			yaxis: {
				max: this.max,
				min: this.min
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
				content: "Health %y%",
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
					color: "rgb(77,167,77)",
					threshold: [
						{
							below: 90,
							color: "#FFC107"
						}, {
							below: 70,
							color: "rgb(200, 20, 30)"
						}
					]
				}
			],
			plotOptions
		);
	}

	protected getDataPointFromJson(json: any): HealthDataPoint {
		return new HealthDataPoint(json);
	}
}

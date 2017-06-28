import { Metric, MetricType, DataPoint } from "./abstract";
import { Utility } from "../utility";
import * as Collections from 'typescript-collections';
import "../extensions";
import "flot";
import "../../vendor/jquery.flot.threshold.js";
import "../../vendor/jquery.flot.tooltip.js";

type JsonUserActionDataPoint = {
	Timestamp: string;
	Count: number;
	Action: string;
}

/**
 * 
 * 
 * @export
 * @class UserActionDataPoint
 * @extends {DataPoint}
 */
export class UserActionDataPoint extends DataPoint {

	public count: number;
	public action: string;


	constructor(json: JsonUserActionDataPoint) {
		super();

		this.timestamp = Utility.toDate(json.Timestamp);
		this.count = json.Count;
		this.action = json.Action;
	}
}

/**
 *
 * 
 * @export
 * @class UserActionMetric
 * @extends {Metric<UserActionDataPoint>}
 */
export class UserActionMetric extends Metric<UserActionDataPoint> {

	constructor(source: string) {
		super(source);

		this._metricType = MetricType.UserAction;

		this.startLoadUI();
	}

	public generatePlotData(): any {
		var data = new Collections.Dictionary<string, number>();
		this
			.data
			.sortByProperty(dp => dp.timestamp.getTime())
			.forEach(
			(value, index, array) => {
				if (data.containsKey(value.action)) {
					data.setValue(value.action, data.getValue(value.action) + value.count);
				} else {
					data.setValue(value.action, value.count);
				}
			}
			);

		return data;
	}

	protected renderPlot(): void {
		let data = <Collections.Dictionary<string, number>>this.generatePlotData();
		let min = 0, max = Number.MIN_SAFE_INTEGER, series = [], index = 0;

		data.forEach((key, value) => {
			max = value > max ? value : max;
			series.push({
				data: [[index, value, key]],
				bars: {
					show: true,
					align: 'center'
				}
			});
			index++;
		});

		let plotOptions: any = {
			yaxis: {
				max: max,
				min: min
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
				content: (label, x, y, item) => `${y} actions "${item.series.data[0][2]}"`,
				defaultTheme: false,
				cssClass: "flot-tooltip"
			}
		};

		$.plot(
			$(`[data-identifier="${this.getMetricIdentifier()}"] .metric-chart`),
			series,
			plotOptions
		);
	}

	protected getDataPointFromJson(json: any): UserActionDataPoint {
		return new UserActionDataPoint(json);
	}
}

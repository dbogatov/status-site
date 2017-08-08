/**
 * Data structure representing application level constants
 * 
 * @export
 * @class Constants
 */
export class Constants {
    /**
	 * Root API URL
	 * 
	 * @static
	 * @type {string}
	 * @memberOf Constants
	 */
	static API_URL : string = "/api";
    /**
	 * Get data API URL
	 * 
	 * @static
	 * @type {string}
	 * @memberOf Constants
	 */
	static GET_DATA_ENDPOINT : string = `${Constants.API_URL}/getData`;
	/**
	 * Get metric API URL
	 * 
	 * @static
	 * @type {string}
	 * @memberOf Constants
	 */
	static GET_METRICS_ENDPOINT : string = `${Constants.API_URL}/getMetrics`;
	/**
	 * How often metrics need to be reloaded and re-rendered (in seconds)
	 * if no data provider specified
	 * 
	 * @static
	 * @type {number}
	 * @memberOf Constants
	 */
	static UPDATE_INTERVAL : number = 20;

	/**
	 * How often metrics need to be reloaded and re-rendered (in seconds)
	 * if if data is loaded through provider
	 * 
	 * @static
	 * @type {number}
	 * @memberof Constants
	 */
	static UPDATE_INTERVAL_PROVIDER : number = 2;

	/**
	 * The interval in minutes used to aggregate the data points for user actions.
	 * For example, if the interval is 5, then data series will be split into intervals
	 * of 5 minutes and sums of user actions will be displayed per interval.
	 */
	static USER_ACTIONS_AGGREGATION_INTERVAL : number = 30;

	/**
	 * The interval in milliseconds that defines a default time frame of data
	 * on metric pages.
	 */
	static METRIC_PAGE_DATA_PREVIEW : number = 2 * 60 * 60 * 1000;
}

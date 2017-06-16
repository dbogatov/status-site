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
	 * 
	 * @static
	 * @type {number}
	 * @memberOf Constants
	 */
	static UPDATE_INTERVAL : number = 20;

	static UPDATE_INTERVAL_PROVIDER : number = 2;

	static REMOVE_METRIC_ENDPOINT : string = `${Constants.API_URL}/removeMetric`;
}

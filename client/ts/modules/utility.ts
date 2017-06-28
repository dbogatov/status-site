import "bootstrap-notify";


/**
 * Static class that provides useful utility methods.
 * 
 * @export
 * @class Utility
 */
export class Utility {

	/**
	 * Returns properly formatted GET request query URL.
	 * 
	 * @static
	 * @param {string} url - URL of the endpoint
	 * @param {...[string, string][]} parameters - set of tuples: param - value
	 * @returns {string} - proper URL for GET request
	 * 
	 * @memberOf Utility
	 */
	public static generateQuery(url: string, ...parameters: [string, string][]): string {

		let result = `${url}?`;
		for (var param of parameters) {
			result += `${param[0]}=${param[1]}&`;
		}

		if (result.charAt(result.length - 1) == '&') {
			result.substr(0, result.length - 1);
		}

		return result;
	}

	/**
	 * GET request AJAX wrapper that works on promises
	 * 
	 * @static
	 * @param {string} url - endpoint URL
	 * @returns {Promise<any>} - promise of get request result
	 * 
	 * @memberOf Utility
	 */
	public static async get(url: string): Promise<any> {
		return new Promise((resolve, reject) => {

			$.ajax(url, {
				type: "GET",
				dataType: "json",
				statusCode: {
					204: (result) => { resolve([]) },
					200: (result) => { resolve(result) }
				},
				error: (result) => { reject(result) }
			});
		});
	}


	/**
	 * DELETE request AJAX wrapper that works on promises
	 * 
	 * @static
	 * @param {string} url - endpoint URL
	 * @param {*} data - request parameters
	 * @returns {Promise<any>} - promise of delete request result
	 * 
	 * @memberOf Utility
	 */
	public static async delete(url: string, data: any): Promise<any> {
		return new Promise((resolve, reject) => {

			$.ajax(url, {
				type: "DELETE",
				data: data,
				statusCode: {
					200: (result) => { resolve(result) }
				},
				error: (result) => { reject(result) }
			});
		});
	}

	/**
	 * Converts .NET timestamp (ISO8601) string to TS Date
	 * Thanks to https://stackoverflow.com/a/20223090/1644554
	 * 
	 * @static
	 * @param {string} dotNetDate - .NET formatted timestamp
	 * @returns {Date} - equivalent TS Date object
	 * 
	 * @memberOf Utility
	 */
	public static toDate(dotNetDate: string): Date {
		var parts = dotNetDate.match(/\d+/g).map(el => parseInt(el));
		var isoTime = Date.UTC(parts[0], parts[1] - 1, parts[2], parts[3], parts[4], parts[5]);
		var isoDate = new Date(isoTime);

		return isoDate;
	}

	/**
	 * Mutes thread for a time interval
	 * 
	 * @static
	 * @param {number} time - time interval in milliseconds
	 * @returns {Promise<any>} 
	 * 
	 * @memberOf Utility
	 */
	public static async sleep(time: number): Promise<any> {
		return new Promise(resolve => window.setTimeout(resolve, time));
	}

	/**
	 * Returns nicely looking string representing time.
	 * Example: 3:09:05 PM
	 * 
	 * @static
	 * @param {Date} date - date to display
	 * @returns {string} - string representation of the date
	 * 
	 * @memberOf Utility
	 */
	public static toHHMMSS(date: Date): string {
		let hours = date.getHours();

		let ampm = hours >= 12 ? 'PM' : 'AM';

		hours = hours % 12;
		hours = hours ? hours : 12; // the hour '0' should be '12'

		let minutes = date.getMinutes() < 10 ? '0' + date.getMinutes() : date.getMinutes();
		let seconds = date.getSeconds() < 10 ? '0' + date.getSeconds() : date.getSeconds();

		return `${hours}:${minutes}:${seconds} ${ampm}`;
	}

	// http://stackoverflow.com/a/18330682/1644554
	public static toLocalTimezone(date: Date): Date {
		let newDate = new Date(date.getTime() + date.getTimezoneOffset() * 60 * 1000);

		let offset = date.getTimezoneOffset() / 60;
		let hours = date.getHours();

		newDate.setHours(hours - offset);

		return newDate;
	}

	public static fixUtcTime(): void {

		$(".utc-time").each(function () {
			$(this).text(
				Utility.toHHMMSS(
					Utility.toLocalTimezone(
						new Date($(this).text())
					)
				)
			);
		});

	}

	public static toUtcDate(date : Date): number {
		return Date.UTC(
			date.getUTCFullYear(), 
			date.getUTCMonth(), 
			date.getUTCDate(),  
			date.getUTCHours(), 
			date.getUTCMinutes(), 
			date.getUTCSeconds(), 
			date.getUTCMilliseconds()
		);
	}
}

/**
 * Static class that provides useful UI related methods.
 * 
 * @export
 * @class UIHelper
 */
export class UIHelper {

	/**
	 * Triggers notification message in the UI.
	 * Uses "bootstrap-notify" library.
	 * 
	 * @static
	 * @param {any} message 
	 * @param {any} type 
	 * 
	 * @memberOf UIHelper
	 */
	public static notify(message, type) {
		$.notify(
			{
				message: message
			},
			{
				type: type,
				allow_dismiss: true,
				allow_duplicates: false,
				timer: 1000,
				placement: {
					from: 'bottom',
					align: 'left'
				},
				delay: 500,
				animate: {
					enter: 'animated fadeInUp',
					exit: 'animated fadeOutDown'
				},
				offset: {
					x: 30,
					y: 30
				}
			}
		);
	};
}

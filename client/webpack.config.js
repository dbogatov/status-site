var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var UglifyJSPlugin = require('uglifyjs-webpack-plugin');
var OptimizeCssAssetsPlugin = require('optimize-css-assets-webpack-plugin');

// Different configurations are used for development and production
// Development produces sourcemaps, production minifies/uglifies js output
// They also use different output paths
module.exports = function (env) {

	var tsPath = env == "prod" ? "ts/" : "dist/ts/";
	var lessPath = env == "prod" ? "" : "dist/";
	var outFile = env == "prod" ? ".min" : "";
	var cssName = env == "prod" ? ".min" : "";

	var plugins = [
		new webpack.ProvidePlugin({
			jQuery: 'jquery',
			$: 'jquery',
			jquery: 'jquery'
		}),
		// Webpack produces JS from LESS, this plugin extracts CSS from it
		new ExtractTextPlugin("app" + cssName + ".css")
	];
	var devtool = "";

	if (env == "prod") {
		plugins.push(new UglifyJSPlugin());
		plugins.push(
			new OptimizeCssAssetsPlugin({
				cssProcessor: require('cssnano'),
				cssProcessorOptions: {
					discardComments: {
						removeAll: true
					}
				},
				canPrint: true
			})
		);
	} else {
		// Turn on sourcemaps
		devtool = "source-map";
	}

	return {
		entry: {
			overview: './' + tsPath + 'overview.ts',
			metric: './' + tsPath + 'metric.ts',
			logs: './' + tsPath + 'logs.ts',
			log: './' + tsPath + 'log.ts',
			admin: './' + tsPath + 'admin.ts',
			less: './' + lessPath + 'less/app.less'
		},
		output: {
			filename: '[name]' + outFile + '.js'
		},
		devtool: devtool,
		resolve: {
			extensions: ['.webpack.js', '.web.js', '.ts', '.js', '.less']
		},
		plugins: plugins,
		module: {
			loaders: [{
					test: /\.ts$/,
					loader: 'ts-loader'
				},
				{
					test: /\.less$/,
					use: ExtractTextPlugin.extract({
						fallback: 'style-loader',
						use: ['css-loader', 'less-loader']
					})
				},
				{
					test: /\.(png|woff|woff2|eot|ttf|svg)$/,
					loader: 'url-loader?limit=100000'
				}
			]
		}
	}
}

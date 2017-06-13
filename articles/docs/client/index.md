# Client documentation

We design our views in `.cshtml` files.
These files contain [Razor](https://docs.microsoft.com/en-us/aspnet/core/mvc/views/razor) syntax and get converted to HTML upon request.

In this app, we try to minimize client load and leave as much as possible to server.
That is, we follow *fat server, thin client* approach.
The reasons are

* different users may have different devices, possibly not capable of the work we put on the client
* server-side code is generally more manageable[^1]
* compatibility issues

**However**, it does not mean we do not have dynamic front end, where necessary.
For example, the main view with metric cards updates data asynchronously and re-renders itself periodically.

For client-side development we use [TypeScript](https://www.typescriptlang.org) as our transpiler, [LESS](http://lesscss.org) as our style engine and [Webpack](https://webpack.js.org) as our bundler.

[^1]: It is solely an opinion not a fact.
In our opinion, front-end technologies (say, JS) were not designed to handle the complex large tasks we have.
We do have JS frameworks, transpilers, web inspector tools, etc., but still JS was designed to hook button click to an alert window, not to run a full featured multi window responsive application.
That is, although it is *possible* to have very interactive front-end, I would avoid it if I can.

## TypeScript

The TypeScript has been chosen for its typed nature.
With little configuration, it is almost C# code on the client, including inheritance, polymorphism, encapsulation and generics.
Fortunately, there is a number of TS extensions - [linters](https://marketplace.visualstudio.com/items?itemName=eg2.tslint) and [language servers](https://code.visualstudio.com/docs/languages/typescript) - which provide [IntelliSense](https://code.visualstudio.com/docs/editor/intellisense).

We generally have two types of TS code - page code and module code.
Page code is a very simple snippet that gets executed when page loads.
This snippet operates on other classes - models and services - which are designed as modules.

For example, `metric-page` is a page code, while `abstract-metric`, `concrete-metric` and `label` are modules.

!!! note
	TypeScript is a typed language which is generally an awesome thing, but sometimes it backfires.
	One of the side effects is that it is tricky to use external libraries, like jQuery because TS does not know the types of that lib.
	One way to work around is to declare external types as `any` which makes TS treat them as dynamic objects.
	This trick is not recommended, so we solve the problem another way.
	Good people(r) developed a [project](https://github.com/typings/typings) that includes type declarations for major libs, and a tool to automatically install typings.

## LESS

LESS has been chosen for its popularity and establishment.
Main style sheet is a material theme boilerplate - a minified version of a product of our work, which includes open-source material theme, bootstrap and a couple overrides for popular plugins inspired by some admin themes.

Some plugins that provide style sheets are downloaded with NPM at build stage and LESS includes those as imports.

## Bundling

We use Webpack to bundle our assets.

TypeScript `imports` are resolved and it gets compiled to JS.
For dev configuration sourcemaps get generated and for production config the JS output gets uglified.
One entry point is defined per page.

LESS `imports` are resolved and it gets compiled to CSS.
For dev configuration sourcemaps get generated and for production config the CSS output gets minified.
By default Webpack attempts to generate CSS-in-JS, but with little magic we extract pure CSS from it.
We use one giant CSS for all pages. It is cached anyway.

!!! note
	Because of how sourcemaps work and for convenient debug experience assets for development and production configurations are put to different folders.
	It is not a problem for views, because we can easily load different assets depending on configuration with Razor syntax

		<environment names="Development">
			<link href="/css/app.css" rel="stylesheet">
		</environment>
		<environment names="Staging,Production">
			<link href="/css/app.min.css" rel="stylesheet">
		</environment>

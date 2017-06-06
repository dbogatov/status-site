# Server documentation

We use ASP.NET Core with C# for out back-end.
ASP.NET is a well-established enterprise-level cross-platform technology stack developed by Microsoft and community.
We think it is the best fit for the application this level and size.

!!! note
	ASP.NET is not an "easy-to-pick-up" technology.
	There is a number of books (at least [this one](https://www.apress.com/us/book/9781484203989) and [this one](https://www.apress.com/us/book/9781484213339)) on this topic.
	It is **recommended** to read through Microsoft docs first, before getting involved in development of the app.

## MVC + Services

Application follows an MVC pattern, where logic is split into 3 major branches - model, view and controller.

Model is responsible for data representation. 
An example of model is a simple POCO class `NumericDataPoint` picked up by Entity Framework to represent a table in a relational database.

View is responsible for visual representation. 
In this app, view branch is a collection of `.cshtml` files and view components, tag helpers. View should not contain business logic, they need to have only visual representation logic.

Controller is responsible for connecting view and model.
Controller needs to have a business logic (frequently encapsulated in services).
Controller is not concerned with how data is stored or displayed.
In this app, controller is a class that accepts API requests (from view), performs a business logic, and delegates data changes to model.

Service is a special part of the controller branch, which encapsulate a single logical task, or a set related tasks.
They are designed as pair of an interface and its implementation (eq. `INotificationService` and `NotificationService`).
Having interface greatly helps with dependency injection and unit testing of the services.

## Dependency Injection

Service may depend on each other and controllers frequently require services to operate.
That effectively creates a *dependency tree*.
Without DI, developer is responsible for instantiating all services starting from the bottom.
Matters get worse when developer wants to have some services being transient (instantiated on each request) or singletons (only one instance of the service lives in the app).

To solve above problems, DI comes into play.
Following *service locator* pattern, this mechanism takes care of resolving a dependency tree providing correct types of services.
All developer needs to do is to teach DI container, which implementations he wants for certain types.
After that, it is enough to just have service types as parameters in constructor, and DI will substitute correct dependencies.

## Entity Framework

We extensively use EF in our model part of the app.
EF follows ORM - object-relational mapping pattern.
Simply put, developer defines POCO classes, which may use inheritance and composition, and EF will automatically generate a scheme for the database, as well as a collection of access methods.

For example

	#!csharp
	public class Cat
	{
		public int Age { get; set; }

		public string Name { get; set; }
	}

	...

	var oldCats = context.Cats.Where(cat => cat.Age > 10);

One of the great features of EF is the fact that developer does not depend on particular data provider.
In our case, it means that we can use in-memory database for testing, SQLite database for development and PostgreSQL for production, **not changing a line of model code**.

!!! summary
    Here are the helpful links:
	
	* [ASP.Core: MVC](https://docs.microsoft.com/en-us/aspnet/core/mvc/overview)
	* [ASP.Core: Dependency Injection](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection)
	* [ASP.Core: Entity Framework](https://docs.microsoft.com/en-us/aspnet/mvc/overview/getting-started/getting-started-with-ef-using-mvc/creating-an-entity-framework-data-model-for-an-asp-net-mvc-application)

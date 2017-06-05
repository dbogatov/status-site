# Documentation

We use 4 types of documentation for the app:

* C# Docs in MSDoc style
* TypeScript Docs in JSDoc style
* API Docs in a Swagger / OpenAPI Spec format
* Articles written in markdown

We believe these set of tools and practices covers the necessary documentation for users and contributors of the app.

## C# Docs

In-code documentation is critical. 
We use [MS Docs](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/xmldoc/xml-documentation-comments).
We follow the following policies when documenting C# code:

* Every method has to be documented
	* Except testing classes (see further)
	* If class implements an interface, the method is documented once - in interface declaration
	* Methods provided by the framework do not have to be documented
	* Class constructor does not have to be documented if it does not contain business logic (eq. just assigns DI objects)
* If a body of a function needs inline comments - rewrite the body
* If there is something special you want to write about the class/interface/method, create an article in markdown
* Properties of model classes have to be documented if
	* the name is not self-explanatory, or
	* explanation is necessary to correctly use the property (eq. property `int Time` where units are ambiguous)
* Test classes and methods are not documented
	* Their names have to be self-explanatory, long names with underscores are welcome
	* If logic is ambiguous - rewrite the test

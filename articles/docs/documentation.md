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
	- Except testing classes (see further)
	- If class implements an interface, the method is documented once - in interface declaration
	- Methods provided by the framework do not have to be documented
	- Class constructor does not have to be documented if it does not contain business logic (eq. just assigns DI objects)
* Interface has to have a summary doc (right above the declaration) explaining the purpose of the interface
	- If class serves as a only one specific implementation of an interface - no need for summary comment
	- If a class does not implement an interface, summary comment is required
* If a body of a function needs inline comments - rewrite the body
* If there is something special you want to write about the class/interface/method, create an article in markdown
* Properties of model classes have to be documented if
	- the name is not self-explanatory, or
	- explanation is necessary to correctly use the property (eq. property `int Time` where units are ambiguous)
* Test classes and methods are not documented
	- Their names have to be self-explanatory, long names with underscores are welcome
	- If logic is ambiguous - rewrite the test

## TypeScript Docs

We use [TypeDocs](http://typedoc.org/guides/doccomments/) style docs.
The policies for TypeScript Docs match those defined for C# Docs.

## API Docs

We use [OpenAPI](http://swagger.io/specification/) spec to document our back-end APIs.
General policies include:

* Each and every API method has to be documented with OpenAPI spec.
* If method returns an object, its definition and example have to be presented.
* All status codes have to be present.

## Articles

We use [MkDocs](http://www.mkdocs.org) with [Material theme](http://squidfunk.github.io/mkdocs-material/) to generate the articles.
We use the articles to provide some conceptual developer documentation and provide guidelines for the user.
Articles serve as an entry point for the whole app documentation.

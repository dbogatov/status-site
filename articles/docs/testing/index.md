# Testing

We follow strict practices regarding testing.
Using [thin client](/client/) approach we stress server-side testing more than the client-side.
Nevertheless, manual testing in a form of user scenario is required before marking an issue resolved.

Our practices include:

* every interface method (that is, public method of a class that implements a service interface) needs to be tested
* heavy-logic internal methods need to be tested (it is possible with C#)
* every API endpoint needs to be tested for all possible status codes
* every view method on view controllers needs to be tested
	* returns correct type
	* returns correct value
	* calls / does not call correct methods
	* sets proper values on static objects (request, response, session, etc.)
* when testing a method, each path needs to get tested
* when writing unit tests, do not write "one test to rule them all". Each unit test tests a singly specific piece of logic'
* use mocks, do not resolve service dependencies

These policies lead to high coverage, although the coverage is not how we define the quality of testing.
The best metric is the developer's confidence when automatically deploying the app.
If developer is nervous, more tests are needed.

For both kinds of tests - unit and integration - we use *[arrange - act - assert](https://msdn.microsoft.com/en-us/library/hh694602.aspx#Anchor_3)* approach.

We do not use [TDD](http://agiledata.org/essays/tdd.html) or similar practices, but nevertheless design our code in a modular manner to enable cleaner testing.

We use [xUnit](https://xunit.github.io) and [Moq](https://github.com/moq/moq4) frameworks for testing.

## Unit and controller tests

The purpose of a [unit test](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test) is to test a specific piece of logic.
We use unit tests to test literally every aspect of the app (except simple API endpoints which we test in integration tests).

When testing services, we use mock objects to substitute dependencies to isolate the logic we want to test from the dependencies' logic.

A special kind of unit tests is a [controller tests](https://docs.microsoft.com/en-us/aspnet/core/mvc/controllers/testing).
The difference is that an ASP controller is heavily built into the framework, and that requires some extra work for testing.
In particular, not only the return value gets tested for the method, but also how the given method interacts with some objects in scope, like session object, or request object.
We use Moq extensively for controller tests.

## Integration tests

[Integration tests](https://docs.microsoft.com/en-us/aspnet/core/testing/integration-testing) test how well system components work together.
We use integration test to check our API endpoints - to ensure correct status code and changes to the state of the app (eq. database).
Integration test usually looks like the following

* test server is spawned with app in its core
* test client is spawned connected to the test server
* test code uses the client to make certain request to the server and verifies the response

We used to test a docker composition but gave up because the testing system was too complex and unstable.
When better tools emerge we will reconsider enabling those tests.

## Integrity tests

We want to ensure the quality of front-end too.
In addition to manual testing we have two more integrity tests.

[BLC](https://www.npmjs.com/package/broken-link-checker) utility consumes a URL, follows a tree of links verifying that each one is working (200+ status code).

[Tidy](http://www.html-tidy.org) ensures that given HTML is W3C compliant.

!!! summary
    Here are the helpful links:
	
	* [Testing and Debugging](https://docs.microsoft.com/en-us/aspnet/core/testing/)

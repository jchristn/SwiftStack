# SwiftStack Coding Standards

This document outlines the mandatory coding standards for the SwiftStack project. All code must strictly adhere to these rules to ensure consistency and maintainability.

## File Organization

### Namespace and Using Statements
- The namespace declaration should always be at the top
- Using statements should be contained INSIDE the namespace block
- All Microsoft and standard system library usings should be first, in alphabetical order
- Other using statements should follow, in alphabetical order

### File Structure
- Limit each file to containing exactly one class or exactly one enum
- Do not nest multiple classes or multiple enums in a single file
- Regions for Public-Members, Private-Members, Constructors-and-Factories, Public-Methods, and Private-Methods are NOT required for small files under 500 lines

## Code Documentation

- All public members, constructors, and public methods must have code documentation
- No code documentation should be applied to private members or private methods
- Document which exceptions public methods can throw using `/// <exception>` tags
- Document nullability in XML comments
- Document thread safety guarantees in XML comments
- Where appropriate, ensure code documentation outlines default values, minimum values, and maximum values
- In such cases, specify what different values mean or what effect they may have

## Naming Conventions

- Private class member variable names must start with an underscore and then be Pascal cased
  - Correct: `_FooBar`
  - Incorrect: `_fooBar`
- Do not use `var` when defining a variable - use its actual type

## Public Members and Properties

- All public members should have explicit getters and setters using backing variables when value requires range or null validation
- Avoid using constant values for things that a developer may later want to configure or otherwise change
- Instead use a public member with a backing private member set to a reasonable default

## Async/Await Patterns

- Async calls should use `.ConfigureAwait(false)` where appropriate
- Every async method should accept a CancellationToken as an input property, unless the class has a CancellationToken as a class member or a CancellationTokenSource as a class member
- Async calls should check whether or not cancellation has been requested at appropriate places
- When implementing a method that returns an IEnumerable, also create an async variant of that same method that includes a CancellationToken

## Data Structures

- Do not use tuples unless absolutely, absolutely necessary

## Exception Handling

- Use specific exception types rather than generic Exception
- Always include meaningful error messages with context
- Consider using custom exception types for domain-specific errors
- Use exception filters when appropriate: `catch (SqlException ex) when (ex.Number == 2601)`

## Resource Management

- Implement IDisposable/IAsyncDisposable when holding unmanaged resources or disposable objects
- Use 'using' statements or 'using' declarations for IDisposable objects
- Follow the full Dispose pattern with `protected virtual void Dispose(bool disposing)`
- Always call `base.Dispose()` in derived classes

## Nullable Reference Types

- Use nullable reference types (enable `<Nullable>enable</Nullable>` in project files)
- Validate input parameters with guard clauses at method start
- Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual null checks
- Consider using the Result pattern or Option/Maybe types for methods that can fail
- Proactively identify and eliminate any situations in code where null might cause exceptions to be thrown

## Thread Safety

- Document thread safety guarantees in XML comments
- Use Interlocked operations for simple atomic operations
- Prefer ReaderWriterLockSlim over lock for read-heavy scenarios

## LINQ Usage

- Prefer LINQ methods over manual loops when readability is not compromised
- Use `.Any()` instead of `.Count() > 0` for existence checks
- Be aware of multiple enumeration issues - consider `.ToList()` when needed
- Use `.FirstOrDefault()` with null checks rather than `.First()` when element might not exist

## Library Code

- Ensure NO Console.WriteLine statements are added to library code

## Special Considerations

- Do not make any assumptions about what class members or class methods exist on a class that is opaque to you
- If code uses manually prepared strings for SQL statements, assume there is a good reason for it
- If a README exists, analyze it and ensure it is accurate
- Compile the code and ensure it is free of errors and warnings

## Code Review Checklist

Before committing code, verify:
- [ ] Namespace declaration at top, using statements inside namespace
- [ ] Using statements properly ordered (Microsoft/System first, then alphabetically)
- [ ] All public members documented
- [ ] No documentation on private members
- [ ] Private members use `_PascalCase` naming
- [ ] No `var` usage
- [ ] Async methods have CancellationToken parameters
- [ ] `.ConfigureAwait(false)` used appropriately
- [ ] Specific exception types used
- [ ] IDisposable implemented and used correctly
- [ ] Nullable reference types enabled and handled
- [ ] LINQ usage optimized
- [ ] No Console.WriteLine in library code
- [ ] Code compiles without errors or warnings

# Agent Instructions

# Conversations
any resulting updates to agents.md should go under the section "## Rules to follow"
When you see a convincing argument from me on how to solve or do something. add a summary for this in agents.md. so you learn what I want over time.
If I say any of the following point, you do this: add the context to agents.md, and associate this with a specific type of task.
if I say "never do x" in some way.
if I say "always do x" in some way.
if I say "the process is x" in some way.
If I tell you to remember something, you do the same, update

## Scope
- This is a Server Emulator for a old game called AISpace.
- The server is built using .NET 8 and C#.
- The project follows a clean architecture with separation of concerns between configuration, data access, and service layers.
- The database layer uses Entity Framework Core for data access and migrations.
- Configuration is managed through strongly-typed models.
- Logging is done using Microsoft.Extensions.Logging.
- Dependency Injection is used throughout the project for better testability and maintainability.
- The project adheres to modern C# coding standards and best practices.

## Rules to follow
- Never introduce fallback logic that silently overrides user or config values; surface configuration errors instead of masking them in code.

## Testing
 - Use xUnit for unit and integration tests.
 - To test run `dotnet test` from the solution root.`
 - Use Moq for mocking dependencies in unit tests.

## Toolchain
- Primary language: C# (modern features)
- Target frameworks: .NET 8

## Project Structure
- Configuration models go into AISpace.Common/Config
- Database context go into AISpace.Common/DAL/
- Migrations go into AISpace.Common/DAL/Migrations/
- Entity models go into AISpace.Common/DAL/Entities/
- Repositories go into AISpace.Common/DAL/Repositories/
- Service layer code goes into /AISpace.Server/

## Coding Style & Naming Conventions
Projects target `net8.0`, `Nullable` enabled, and treat warnings as errors. Follow standard C# layout: four-space indents, braces on new lines, and `PascalCase` for types/methods, `camelCase` for locals and parameters. Prefer expression-bodied members only when they improve clarity. Use `var` when the right-hand side makes the type obvious. log messages actionable.
Prefer Primary Constructor syntax for classes with readonly properties when possible. Use the least amount of using statements possible.

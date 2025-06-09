# Coding Agents Instructions

These guidelines apply to all pull requests in this repository.

- **Run the .NET test suite** after making changes to any C# code. Use:
  ```bash
  dotnet test Src/Nemcache.Tests/Nemcache.Tests.csproj
  ```
  Ensure the tests pass or note any environment-related failures in your PR.
- **Client changes** in the `client` folder should build successfully. Run:
  ```bash
  yarn install
  yarn build
  ```
- Summarize your changes clearly in the PR description.

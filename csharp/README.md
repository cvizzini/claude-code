# Claude Code

## Overview

Claude Code is a C# solution structured to provide a modular and scalable architecture. The solution contains several projects, each fulfilling a specific role within the ecosystem. It is built to target `.NET Core` framework version `net8.0`.

---

## C# Projects

This solution consists of the following projects:

1. **ClaudeCode.Cli**:
   - Purpose: Command-line interface.
   - Dependencies:
     - Microsoft.Extensions.DependencyInjection
     - Microsoft.Extensions.Hosting
     - Microsoft.Extensions.Logging
     - Spectre.Console
     - System.CommandLine

2. **ClaudeCode.Commands**:
   - Purpose: Contains command implementations to interact with the project.
   - Dependencies:
     - Spectre.Console
     - Other project references.

3. **ClaudeCode.Constants**:
   - Purpose: Holds constants and static resources shared across the solution.

4. **ClaudeCode.Core**:
   - Purpose: Contains core business logic and abstractions.
   - Dependencies:
     - Microsoft.Extensions.DependencyInjection.Abstractions
     - Microsoft.Extensions.Logging.Abstractions

5. **ClaudeCode.Services**:
   - Purpose: Service layer managing application workflows and external interactions.
   - Dependencies:
     - Microsoft.Extensions.Http
     - Microsoft.Extensions.Logging.Abstractions

6. **ClaudeCode.Tools**:
   - Purpose: Utilities and supporting tools used across the solution.
   - Dependencies:
     - Microsoft.Extensions.FileSystemGlobbing
     - Microsoft.Extensions.Http
     - Microsoft.Extensions.Logging.Abstractions

---

## Solution Architecture

The directory structure of the `Claude Code` solution is as follows:

```
/Solution Items/
/src/
    - ClaudeCode.Cli/
    - ClaudeCode.Commands/
    - ClaudeCode.Constants/
    - ClaudeCode.Core/
    - ClaudeCode.Services/
    - ClaudeCode.Tools/
```

### Target Framework

All projects target the `.NET 8.0` framework for high performance and maintainability.

---

### Building and Running the Solution

1. Ensure `dotnet` SDK version `8.0` or higher is installed.
2. Navigate to the root directory of the solution.
3. Use the following commands:
    ```bash
    dotnet restore
    dotnet build
    dotnet run --project src/ClaudeCode.Cli/ClaudeCode.Cli.csproj
    ```
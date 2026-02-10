# Change 001: Initial Project Structure Setup

## Prompt

The project uses React for the frontend and .NET Web API for the backend. Data is stored in LiteDB.
The repository includes these files from the start:
- README.md (initial overview)
- GITHUB_COPILOT_INSTRUCTIONS.md (usage patterns, workflow requirements: all PRs must add a changelog file like /changelog/change001.md containing the prompt and implementation details).
- Empty /changelog folder.
- The repo should use standard project structure for React app and .NET API, with client and server folders.

## Implementation Details

### Files Created

1. **GITHUB_COPILOT_INSTRUCTIONS.md**
   - Documented workflow requirements for all pull requests
   - Defined changelog file format and naming conventions
   - Outlined usage patterns for GitHub Copilot in this project
   - Specified project structure conventions

2. **changelog/ directory**
   - Created empty changelog folder with README.md
   - This directory will contain changelog entries for all future PRs

3. **client/ directory - React Frontend**
   - Created React application using create-react-app with TypeScript template
   - Standard React project structure with:
     - `/src` - Source code for React components
     - `/public` - Static assets
     - `package.json` - Node.js dependencies
     - `tsconfig.json` - TypeScript configuration
   - Ready for development with `npm start`

4. **server/ directory - .NET Web API Backend**
   - Created ASP.NET Core Web API project using dotnet CLI
   - Named as PlanningPoker.Api
   - Added LiteDB NuGet package (v5.0.21) for database functionality
   - Standard .NET API structure with:
     - Controllers folder for API endpoints
     - Program.cs for application configuration
     - PlanningPoker.Api.csproj for project configuration

5. **Updated .gitignore**
   - Added Node.js/React patterns (node_modules, build artifacts, etc.)
   - Added IDE-specific patterns (.vscode, .idea)
   - Added environment files (.env.*)
   - Added LiteDB database files (*.db)

6. **Updated README.md**
   - Added comprehensive project overview
   - Documented technology stack (React, .NET, LiteDB)
   - Included project structure diagram
   - Added getting started instructions for both client and server
   - Documented development workflow requirements

### Technical Decisions

1. **TypeScript for React**: Chose TypeScript template for better type safety and developer experience
2. **LiteDB**: Selected as the NoSQL database solution - lightweight, serverless, file-based storage
3. **Separate client/server folders**: Clear separation of concerns between frontend and backend
4. **Standard tooling**: Used official scaffolding tools (create-react-app, dotnet new) to ensure best practices

### Next Steps

The basic project structure is now in place. Future development can focus on:
- Implementing Planning Poker specific features (rooms, voting, story cards)
- Setting up API endpoints in the server
- Creating React components for the UI
- Configuring LiteDB models and repositories
- Adding CORS configuration for client-server communication

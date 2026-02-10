# Planning Poker

AI-driven development of a Planning Poker application for agile teams.

## Overview

Planning Poker is a collaborative estimation tool used by agile teams to estimate the complexity of user stories and tasks. This application provides a digital platform for distributed teams to perform planning poker sessions remotely.

## Technology Stack

### Frontend
- **React** with TypeScript
- Modern React development with hooks and functional components
- Located in `/client` directory

### Backend
- **.NET Web API** (ASP.NET Core)
- RESTful API design
- Located in `/server` directory

### Database
- **LiteDB** - A lightweight, serverless NoSQL database
- Perfect for small to medium-sized applications
- File-based storage with no separate database server required

## Project Structure

```
planning-poker/
├── client/              # React frontend application
│   ├── public/          # Static assets
│   ├── src/             # React components and application code
│   ├── package.json     # Node.js dependencies
│   └── tsconfig.json    # TypeScript configuration
├── server/              # .NET Web API backend
│   ├── Controllers/     # API controllers
│   ├── Models/          # Data models
│   ├── Services/        # Business logic services
│   ├── Program.cs       # Application entry point
│   └── PlanningPoker.Api.csproj  # .NET project file
├── changelog/           # Changelog entries for all PRs
└── README.md           # This file
```

## Getting Started

### Prerequisites
- Node.js (v16 or higher)
- .NET SDK (v8.0 or higher)
- npm or yarn package manager

### Running the Frontend

```bash
cd client
npm install
npm start
```

The React app will start on `http://localhost:3000`

### Running the Backend

```bash
cd server
dotnet restore
dotnet run
```

The API will start on `https://localhost:7XXX` (HTTPS) and `http://localhost:5XXX` (HTTP)

### Building for Production

#### Frontend
```bash
cd client
npm run build
```

#### Backend
```bash
cd server
dotnet publish -c Release
```

## Development Workflow

This project follows specific workflow requirements documented in `GITHUB_COPILOT_INSTRUCTIONS.md`. All pull requests must include a changelog entry in the `/changelog` directory.

## Contributing

1. Create a feature branch
2. Make your changes
3. Add a changelog entry in `/changelog`
4. Submit a pull request

## License

This is an AI-driven development project for demonstration and learning purposes.

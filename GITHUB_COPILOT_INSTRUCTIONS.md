# GitHub Copilot Usage Instructions

## Workflow Requirements

### Pull Request Changelog

All pull requests must include a changelog file that documents the changes made. This helps maintain a clear history of the project's evolution.

#### Changelog File Format

1. **Location**: All changelog files must be placed in the `/changelog` directory
2. **Naming Convention**: Use sequential numbering with the format `changeXXX.md` (e.g., `change001.md`, `change002.md`, etc.)
3. **Content Requirements**: Each changelog file must contain:
   - The original prompt or issue description
   - Implementation details describing what was changed and why
   - Any relevant technical decisions or considerations

#### Example Changelog Entry

```markdown
# Change 001: Initial Project Structure

## Prompt
Set up the standard project structure for a Planning Poker application with React frontend and .NET Web API backend.

## Implementation Details
- Created client folder with React application structure
- Created server folder with .NET Web API structure
- Added LiteDB integration for data storage
- Updated README with project overview
```

### Usage Patterns

When using GitHub Copilot for this project:

1. **Incremental Development**: Make small, focused changes rather than large sweeping modifications
2. **Documentation**: Keep documentation up-to-date with code changes
3. **Testing**: Write tests for new features and ensure existing tests pass
4. **Code Review**: Review AI-generated code carefully before committing
5. **Changelog**: Always create a changelog entry before finalizing your pull request

## Project Structure

- `/client` - React frontend application
- `/server` - .NET Web API backend application
- `/changelog` - Changelog entries for all pull requests

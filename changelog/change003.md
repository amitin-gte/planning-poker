# Changelog Entry

- Added a 'Test' button to the UI for verifying API connectivity.
- The button calls the /health endpoint and displays success if the API is healthy.
- The API base URL is now defined as a constant in the client code.
- Enabled CORS in the API for http://localhost:3000 to allow frontend-backend communication during development.
- Added a test to verify the Test button and health check functionality.
- Fixed ESLint import order issues in client files.

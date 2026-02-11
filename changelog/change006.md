# Change 006: User Authentication and Authorization System

## Initial Prompt
Create a new branch for the following changes.

Design a model for storing User data.
Requirements are: the user has credentials for signing in. Note, the name is used as username for signing in and as a display name in the poker rooms. The password can be stored as a plain text, because the app does not require great security. Also, the user has a role, either Admin, or User.

Create a repository for users:
 * create - create a user with given username, password, and role
 * signin - verify the given username is found and the given password matches the stored one, if success return the user object
 * list - return the list of usernames and roles
 * delete - delete the user with the given username if found
Implement unit tests.

Create in-memory token service, which can cache user objects associated with a temporary token.
Cover the service with unit tests.

Create and update API endpoints:
 * POST /users/signin - check the given username and password against the repository and issue a token if success. If the specified username is not found, create a new user and issue a token.
 * GET /users/list - query the list of users from the repository, but at first validate if the given token is found and the user has role Admin
 * GET /users/any - if the list of users from the repository is empty, return 404, else 200. No token required to call it.
 * DELETE /users/{username} - delete the user via repository function, but firstly verify the Admin role
 * Update the room list endpoint, so that it accepts a token and verifies the user role is Admin
 * Similarly update the delete room endpoint

Create page /admin/users similar to /admin/rooms, but for user management.

Implement scenarios:
 * The root page is open, the list of users is empty (as per endpoint /users/any) - present the site initiation form asking for the admin username and password. Create the first user with role Admin. Automatically sign in and present the regular home page.
 * The root page is open, the user is not signed in (no token on the client side) - present the sign-in/sign-up form asking for username and password. Once credentials are submitted, use the POST /users/signin endpoint to get the token. After that present the regular home page.
 * The root page is open, the user role is not Admin - don't show button 'See all rooms'
 * The root page is open, the user role is Admin - show button 'See all rooms' and button 'Manage users' - guiding to /admin/users

## Implementation

### Backend Changes

#### 1. User Model and DTOs
**server/Models/User.cs**
- Created `User` class with `Username`, `Password`, and `Role` properties
- Created `UserRole` enum with `User` and `Admin` values
- Used LiteDB BsonId attribute for Username as primary key
- Added `required` modifiers to fix nullable warnings

**server/Models/UserDtos.cs**
- Created `SignInRequest` DTO for authentication requests
- Created `SignInResponse` DTO returning token, username, and role
- Created `UserListItem` DTO for user list responses
- Added `required` modifiers for non-nullable properties

#### 2. User Repository
**server/Repositories/UserRepository.cs**
- Implemented Create method - creates user if username doesn't exist
- Implemented SignIn method - validates credentials and returns user
- Implemented List method - returns all users
- Implemented Delete method - removes user by username
- Implemented Count method - returns total user count
- Used in-memory LiteDB database for testing
- Proper IDisposable implementation

**server/Tests/UserRepositoryTests.cs**
- Created comprehensive unit tests (11 tests)
- Tests for Create, SignIn, List, Delete, and Count operations
- Tests for edge cases (duplicate users, wrong passwords, non-existent users)

#### 3. Token Service
**server/Services/TokenService.cs**
- Created in-memory token service using ConcurrentDictionary
- Implemented GenerateToken - creates GUID-based tokens
- Implemented ValidateToken - retrieves user by token
- Implemented RevokeToken - removes token
- Implemented Clear - removes all tokens
- Thread-safe implementation for concurrent requests

**server/Tests/TokenServiceTests.cs**
- Created comprehensive unit tests (7 tests)
- Tests for token generation, validation, revocation, and clearing
- Tests for unique token generation and invalid token handling

#### 4. API Endpoints
**server/Program.cs**
- Added JSON enum serialization (UserRole as string instead of integer)
- Registered UserRepository and TokenService as singletons
- Created `POST /users/signin` - sign in or create new user (first user gets Admin role)
- Created `GET /users/list` - list users (Admin only)
- Created `GET /users/any` - check if users exist (for initialization)
- Created `DELETE /users/{username}` - delete user (Admin only)
- Updated `GET /rooms` - Admin only
- Updated `DELETE /rooms/{roomId}` - Admin only
- Updated `POST /rooms` - requires authentication
- Updated `PUT /rooms/{roomId}` - requires authentication
- Updated `GET /rooms/{roomId}` - requires authentication
- Used `Results.StatusCode(403)` instead of `Results.Forbid()` to avoid authentication service requirement

**server/Tests/UserApiTests.cs**
- Created integration tests using WebApplicationFactory
- Tests for all user endpoints with various authorization scenarios
- Tests for admin vs regular user access control
- Made Program class public for testing support

**server/Tests/PlanningPoker.Api.Tests.csproj**
- Added Microsoft.AspNetCore.Mvc.Testing package reference

### Frontend Changes

#### 1. Authentication Context
**client/src/AuthContext.tsx**
- Created AuthProvider with user state, token storage, and auth methods
- Implemented signIn method - calls API and stores token in localStorage
- Implemented signOut method - clears token and user state
- Implemented isAdmin helper - checks user role
- Added needsInitialization flag - checks if site needs initial setup
- Auto-loads token from localStorage on mount

#### 2. Sign-In Page
**client/src/SignInPage.tsx**
- Created sign-in/sign-up form
- Single form handles both sign-in and registration
- Username and password inputs with validation
- Error message display
- Integrates with AuthContext

#### 3. Initialization Page
**client/src/InitializationPage.tsx**
- Created site initialization page for first-time setup
- Clear messaging: "🎉 Site Initialization"
- Explains that this creates the first admin account
- Same form flow as sign-in but with different messaging
- Labels show "Admin Username" and "Admin Password"
- Button text: "Create Admin Account & Sign In"

#### 4. Admin Users Page
**client/src/AdminUsersPage.tsx**
- Created user management interface (similar to AdminRoomsPage)
- Displays table of users with username and role
- Delete button with confirmation dialog
- Admin-only access via token validation
- Proper error handling and loading states

#### 5. Updated Pages
**client/src/App.tsx**
- Wrapped app in AuthProvider
- Implemented authentication flow:
  - Show InitializationPage if no users exist
  - Show SignInPage if not authenticated
  - Show main app if authenticated
- Updated Home component with role-based UI:
  - Shows username and sign-out button
  - Shows "See all rooms" button only for Admins
  - Shows "Manage users" button only for Admins
- Added route for /admin/users

**client/src/NewRoomPage.tsx**
- Updated to use AuthContext for token
- Added Authorization header to API calls
- Changed API_BASE_URL to http://localhost:5233

**client/src/AdminRoomsPage.tsx**
- Updated to use AuthContext for token
- Added Authorization header to API calls
- Changed API_BASE_URL to http://localhost:5233
- Fixed React Hook dependency warnings with useCallback

**client/src/RoomPage.tsx**
- Updated to use AuthContext for token
- Added Authorization header to API calls
- Changed API_BASE_URL to http://localhost:5233

**client/src/App.css**
- Added styling for authentication forms (.centered-form, .form-container)
- Added form-group and input styling
- Added error-message styling
- Responsive design for sign-in/initialization pages

### Configuration Decisions (from clarifications)

1. **Token Lifespan**: Valid until server restart (in-memory storage)
2. **Token Storage**: localStorage (persists across browser sessions)
3. **New User Role**: First user gets Admin, subsequent users get User role
4. **Room Authentication**: All room operations require authentication
5. **Enum Serialization**: UserRole serialized as strings ("Admin", "User") instead of integers

## Subsequent Corrections

### 1. Initialization Form Messaging
**Issue**: User requested better messaging for site initialization form
**Fix**: Updated InitializationPage.tsx to include:
- "🎉 Site Initialization" header
- Explanatory text about first-time setup
- Clear labels: "Admin Username" and "Admin Password"
- Button text: "Create Admin Account & Sign In"

### 2. API URL Mismatch
**Issue**: Server was listening on http://localhost:5233, but client was configured for https://localhost:7290
**Fix**: Updated all API_BASE_URL constants in client files to http://localhost:5233:
- AuthContext.tsx
- NewRoomPage.tsx
- AdminRoomsPage.tsx
- AdminUsersPage.tsx
- RoomPage.tsx

### 3. Role Display as Integer
**Issue**: /admin/users page showed Role as integer (0, 1) instead of string
**Fix**: Added JSON serialization configuration in Program.cs:
```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
});
```

### 4. HTTP 500 Error and Authentication Service
**Issue**: /users/list endpoint returned HTTP 500 error due to missing IAuthenticationService
**Fix**: Replaced all `Results.Forbid()` calls with `Results.StatusCode(403)`:
- Updated 4 endpoints in Program.cs
- Updated test expectations in UserApiTests.cs
- Avoided need for full authentication middleware setup

### 5. React Hook Dependency Warnings
**Issue**: ESLint warnings about missing dependencies in useEffect hooks
**Fix**: Wrapped fetchRooms and fetchUsers functions with useCallback:
- AdminRoomsPage.tsx
- AdminUsersPage.tsx

## Testing

### Unit Tests Created
- UserRepositoryTests.cs (11 tests) - CREATED
- TokenServiceTests.cs (7 tests) - CREATED
- UserApiTests.cs (10 integration tests) - CREATED

### Test Status
- All repository and service tests passing
- Integration tests created but pending server restart for execution
- Client build successful with no errors

## Security Notes
- Passwords stored in plain text as per requirements
- Token-based authentication using in-memory storage
- Tokens valid until server restart
- Admin role required for user management and room administration
- First user automatically gets Admin role

## Branch
All changes committed to: `feature/user-authentication`

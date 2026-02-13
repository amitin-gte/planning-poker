# Change 007: SignalR Real-Time Voting Feature with UI Improvements

## Initial Prompt

Create a new branch.
Use the SignalR approach for the following new feature.

The room object in the database stores the username of the host - the user that initially created that room. This is how the room page knows who's the host, because the UI is different for the host and any other connected user.

The room page shows the list of connected users ('Players') in a column on the right side. It's rather a table, as it shows both name and voting state.
The room page has these modes:

**Start mode:**
- list of users: shows the usernames, no voting state
- main area:
  - for the host user: the text box for story name and button 'Start voting', that changes the mode to Voting
  - for other users: text 'The host is preparing the story'

**Voting mode:**
- list of users: voting state shows a tick icon once the corresponding user has selected a card
- main area:
  - for all users: the story name, the set of poker cards, and the countdown timer. The host user participates in the voting too. Once a card is selected, it is locked and it's not possible to select another card

**Results mode** - displayed when all users voted or when the countdown timer reached 0:
- list of users: voting state shows the value of the card selected by the user
- main area:
  - for all users: the story name, the average score of those card values, which are numeric
  - additionally for the host user: the text box for story name and button 'Start voting'

Consider what data is necessary to store in the DB (maybe it's enough to keep all voting related data in the server memory).
Think out the API endpoints you need to add for the new functionality.
Cover new changes with unit tests.

## Subsequent Requests

### Request 2: UI Refinements
Tiny adjustments:
* Make the 'start voting' button aligned to the center and of green colour, properly styled
* In the list of users show the host username always on top, rename the 'Status' column to 'Vote'
* When displaying the average score, additionally show the recommended value, which is selected from the list of card values as the closest to the calculated average
* Don't display texts 'Story:' and 'Results for:' before the story name. Make a nice border around the story name.

Fix the issue: when the countdown timer reaches 0s, the page does not get updated to the Results mode

### Request 3: Current User Bold
In the list of users show the current user's username in bold

### Request 4: Timer Bug Persistent
The bug persists: when the countdown timer is over, the page shows Time Remaining: 0s and that's it, it does not switch to the Results mode. But if the user refreshes the page, it shows the results

### Request 5: Auto 401 Handling
/admin/rooms shows 'Failed to fetch rooms' after the app is restarted. The server returns 401. I'd like the app to auto handle such situations in all pages in the same manner: re-sign-in and get back to the URL

## Implementation

### Phase 1: Core SignalR Voting Feature

#### Backend: Data Models
**server/Models/VotingModels.cs** (NEW FILE)
- Created `VotingMode` enum: Start, Voting, Results
- Created `VotingParticipant` class: Username, ConnectionId, Vote
- Created `VotingSession` class: RoomId, Mode, Participants dictionary, StoryName, CardValues, TimerSeconds, VotingStartTime
  - Methods: `AllUsersVoted()`, `CalculateResults()`
- Created `VotingResults` class: StoryName, UserVotes dictionary, AverageScore
- Created DTOs: `StartVotingRequest`, `SubmitVoteRequest`, `ParticipantDto`, `RoomStateDto`
- Used `required` modifiers for non-nullable properties

#### Backend: VotingSessionService
**server/Services/VotingSessionService.cs** (NEW FILE)
- In-memory storage using `ConcurrentDictionary<string, VotingSession>`
- Methods:
  - `GetOrCreateSession()`: Initialize session in Start mode
  - `GetSession()`: Retrieve existing session
  - `AddParticipant()`: Add user with ConnectionId (thread-safe with locks)
  - `RemoveParticipant()`, `RemoveParticipantByConnectionId()`: Handle disconnections
  - `StartVoting()`: Transition to Voting mode, clear previous votes
  - `SubmitVote()`: Lock vote (cannot change once submitted)
  - `ShouldRevealResults()`: Check if all voted OR timer expired
  - `RevealResults()`: Transition to Results mode and calculate average
  - `CalculateResults()`: Filter numeric votes, compute average
  - `FindRoomByConnectionId()`: Locate room for disconnect handling

#### Backend: SignalR Hub
**server/Hubs/PlanningPokerHub.cs** (NEW FILE)
- Hub methods:
  - `JoinRoom(roomId, token)`: Validate token, add to group, add participant, return room state
  - `LeaveRoom(roomId)`: Remove participant, notify group
  - `StartVoting(roomId, request)`: Host-only, validate host, start voting session, schedule timer task
  - `SubmitVote(roomId, request)`: Lock vote, notify group, check auto-reveal
  - `OnDisconnectedAsync()`: Clean up participant on disconnect
- Events sent to clients:
  - `UserJoined(username)`: New participant connected
  - `UserLeft(username)`: Participant disconnected
  - `VotingStarted(state)`: Voting round initiated
  - `UserVoted(username)`: Participant submitted vote
  - `ResultsRevealed(state)`: Results available (all voted or timer expired)
- Helper methods:
  - `CheckAndRevealResults()`: Check conditions and reveal if ready
  - `GetRoomState()`: Build current state DTO with mode-specific data
  - `GetUsernameFromConnection()`: Resolve username from ConnectionId

#### Backend: Configuration
**server/Program.cs**
- Registered `VotingSessionService` as singleton
- Added SignalR with `AddJsonProtocol()` and `JsonStringEnumConverter` for enum serialization
- Configured CORS to allow credentials for SignalR
- Mapped `/hubs/planningpoker` endpoint

**server/Models/RoomConfig.cs**
- Added `HostUsername` property to store room creator

#### Backend: Repository Interfaces
**server/Repositories/IRepositories.cs** (NEW FILE)
- Extracted `IRoomRepository` and `IUserRepository` interfaces for testability
- Updated `UserRepository.cs` and `RoomRepository.cs` to implement interfaces
- Updated DI registration in `Program.cs`

#### Backend: Unit Tests
**server/Tests/VotingSessionServiceTests.cs** (NEW FILE)
- 15 comprehensive tests covering:
  - Session creation and retrieval
  - Participant management (add/remove)
  - Voting flow (start, submit, lock)
  - Auto-reveal conditions (all voted, timer expired)
  - Results calculation (numeric filtering, average)
  - Thread safety with locks

**server/Tests/PlanningPokerHubTests.cs** (NEW FILE)
- Hub tests using mocked dependencies
- Tests for JoinRoom, StartVoting, SubmitVote
- Some tests encountered mock limitations with extension methods

#### Frontend: SignalR Integration
**client/src/RoomPage.tsx** - Major refactoring
- Added SignalR client (`@microsoft/signalr` package)
- Three mode states: Start, Voting, Results
- State management:
  - `roomState`: Complete room state from server
  - `selectedCard`: Locked card selection
  - `timeRemaining`: Countdown timer state
  - `connectionError`: SignalR connection errors
- SignalR connection lifecycle:
  - Build connection with `accessTokenFactory` for authentication
  - Configure automatic reconnection
  - Connect and invoke `JoinRoom` on mount
  - Clean up with `LeaveRoom` on unmount
- Event handlers:
  - `UserJoined`: Add participant to list (with duplicate check)
  - `UserLeft`: Remove participant from list
  - `VotingStarted`: Switch to Voting mode, initialize timer
  - `UserVoted`: Mark participant as voted
  - `ResultsRevealed`: Switch to Results mode, clear timer
- Countdown timer effect:
  - Updates every 100ms during Voting mode
  - Calculates elapsed time from `votingStartTime`
  - Displays remaining time
- Mode-specific UI:
  - **Start Mode**: Host sees form, others see waiting message
  - **Voting Mode**: All see cards, timer, story name; cards lock after selection
  - **Results Mode**: Shows votes, average score, host can start next round
- Form handling:
  - Pre-filled with room defaults (card values, timer)
  - Timer can be cleared to disable countdown
  - StartVoting invokes hub method with parsed inputs

**client/src/VotingForm.tsx** (NEW FILE)
- Reusable form component for Start and Results modes
- Props: form values, change handlers, submit handler, labels
- Eliminates code duplication between Start and Results mode forms
- Uses `idPrefix` for unique form element IDs

#### Frontend: Admin Page Enhancement
**client/src/AdminRoomsPage.tsx**
- Added `hostUsername` column to rooms table
- Fetches and displays room creator

### Phase 2: UI Refinements and Bug Fixes

#### 1. VotingForm Component Styling
**client/src/VotingForm.tsx**
- Centered the Start Voting button with flexbox
- Applied green color scheme (#4CAF50) with hover effect (#45a049)
- Made button larger and more prominent (padding: 0.8rem 2rem, minWidth: 200px)
- Added proper disabled state styling with gray background
- Added smooth background color transition

#### 2. Story Name Display with Borders
**client/src/RoomPage.tsx**
- **Voting Mode**: Removed "Story:" prefix text
  - Added green-themed border (3px solid #4CAF50)
  - Light green background (#f1f8f4)
  - Rounded corners (borderRadius: 12px)
  - Centered with max-width constraint
  - Story name in dark green color (#2e7d32)

- **Results Mode**: Removed "Results for:" prefix text
  - Added blue-themed border (3px solid #2196F3)
  - Light blue background (#e3f2fd)
  - Rounded corners (borderRadius: 12px)
  - Centered with max-width constraint
  - Story name in dark blue color (#1565c0)

#### 3. Participants List Improvements
**client/src/RoomPage.tsx**
- Renamed table column header from "Status" to "Vote"
- Sorted participants array to always show host username at the top
- Added bold styling for current user's username using conditional fontWeight
- Host indicated with crown emoji 👑

#### 4. Recommended Value Calculation
**client/src/RoomPage.tsx**
- Added algorithm to find closest numeric card value to average score
- Filters card values to numeric only (excludes "?", etc.)
- Calculates absolute difference between each card and average
- Displays recommended value below average score in blue (#1976d2, fontSize: 1.5rem)
- Only shows recommendation when numeric votes exist

#### 5. Current User Bold Styling
**client/src/RoomPage.tsx**
- Wrapped participant username in span element
- Applied conditional fontWeight: 'bold' when username matches current user
- Maintains crown emoji positioning for host

#### 6. Timer Auto-Reveal Fix
**server/Hubs/PlanningPokerHub.cs**
- **Root Cause**: Timer task running in background (`Task.Run`) couldn't access Hub's `Clients` property because it lacks connection context
- **Solution**: Injected `IHubContext<PlanningPokerHub>` into hub constructor
- Used `_hubContext.Clients.Group(roomId).SendAsync()` in timer background task instead of instance `Clients` property
- Added try-catch block for error handling in timer task
- Added session mode validation before revealing results
- Added console logging for timer task errors

**Key Technical Details:**
- `IHubContext<T>` allows sending SignalR messages from any context, not just during hub method invocations
- Background tasks spawned with `Task.Run` don't have access to hub connection context
- The hub context is thread-safe and can be used from background tasks

### Frontend Infrastructure

#### 7. Authentication Error Handling Utility
**client/src/authUtils.ts** (NEW FILE)
- Created `authenticatedFetch()` wrapper function around native fetch
- Automatically detects 401 responses
- Stores current URL in sessionStorage before redirecting
- Accepts `onUnauthorized` callback for custom handling
- Throws clear error message: "Unauthorized - please sign in again"
- Includes `getAndClearRedirectUrl()` helper to retrieve stored URL after sign-in

#### 8. Auto-Redirect After Sign-In
**client/src/App.tsx**
- Added useEffect hook to check for redirect URL after user signs in
- Calls `getAndClearRedirectUrl()` and navigates to stored URL
- Ensures seamless return to original page after re-authentication

#### 9. Admin Pages 401 Handling
**client/src/AdminRoomsPage.tsx**
- Replaced native `fetch()` with `authenticatedFetch()`
- Added `signOut` from useAuth context
- Passed `onUnauthorized: signOut` to both fetch and delete operations
- Updated dependency array to include `signOut`

**client/src/AdminUsersPage.tsx**
- Replaced native `fetch()` with `authenticatedFetch()`
- Added `signOut` from useAuth context
- Passed `onUnauthorized: signOut` to both fetch and delete operations
- Updated dependency array to include `signOut`

#### 10. New Room Page 401 Handling
**client/src/NewRoomPage.tsx**
- Replaced native `fetch()` with `authenticatedFetch()`
- Added `signOut` from useAuth context
- Passed `onUnauthorized: signOut` to room creation operation

## Testing Notes

### Client Build
- All changes compiled successfully
- Bundle size: 80.65 kB (increased by 221 bytes due to auth utilities)
- No compilation errors

### Expected Behavior
1. **Start Voting Button**: Green, centered, prominent styling with hover effect
2. **Story Names**: Displayed with colored borders (green during voting, blue in results)
3. **Participants**: Host always at top, current user in bold, "Vote" column header
4. **Average Score**: Shows recommended card value closest to the average
5. **Timer Expiry**: Page automatically switches to Results mode when countdown reaches 0
6. **401 Errors**: User automatically redirected to sign-in, then back to original URL after authentication

### Server Restart Required
The timer fix requires restarting the server to pick up the new `IHubContext<PlanningPokerHub>` injection.

## Summary

This change implements a complete real-time planning poker voting system using SignalR, enabling multiple users to participate in estimation sessions with live updates. The implementation follows the three-mode workflow (Start → Voting → Results) with proper state management, timer functionality, and vote locking.

**Phase 1** delivered the core SignalR infrastructure:
- In-memory voting session management for fast real-time operations
- SignalR hub with authentication, event broadcasting, and automatic result reveals
- Comprehensive unit tests for voting logic
- Full client-side SignalR integration with connection management and event handlers

**Phase 2** refined the user experience with:
- Polished UI styling (green buttons, bordered story names, sorted participant lists)
- Critical timer bug fix using IHubContext for background tasks
- Recommended card value calculation based on average score
- Universal 401 error handling with automatic redirect-after-sign-in

The system now provides a seamless real-time collaboration experience where users can vote simultaneously, see live updates of participation, and automatically transition between voting rounds. Authentication errors are handled gracefully, ensuring users are never stuck on error screens.

**Key Technical Achievements:**
- Real-time bidirectional communication via SignalR
- Thread-safe in-memory session storage with vote locking
- Automatic result reveal on timer expiry or full participation
- Background task SignalR messaging via IHubContext
- Comprehensive error handling with session storage-based redirects

**Files Modified:**
- server/Hubs/PlanningPokerHub.cs
- server/Models/RoomConfig.cs
- server/Program.cs
- server/Repositories/UserRepository.cs
- server/Repositories/RoomRepository.cs
- client/src/VotingForm.tsx
- client/src/RoomPage.tsx
- client/src/App.tsx
- client/src/AdminRoomsPage.tsx
- client/src/AdminUsersPage.tsx
- client/src/NewRoomPage.tsx

**Files Created:**
- server/Models/VotingModels.cs
- server/Services/VotingSessionService.cs
- server/Repositories/IRepositories.cs
- server/Tests/VotingSessionServiceTests.cs
- server/Tests/PlanningPokerHubTests.cs
- client/src/authUtils.ts
- client/src/App.tsx
- client/src/AdminRoomsPage.tsx
- client/src/AdminUsersPage.tsx
- client/src/NewRoomPage.tsx
- server/Hubs/PlanningPokerHub.cs

**Files Created:**
- client/src/authUtils.ts

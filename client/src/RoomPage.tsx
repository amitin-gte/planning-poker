import React, { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { API_BASE_URL } from './config';
import * as signalR from '@microsoft/signalr';
import VotingForm from './VotingForm';

enum VotingMode {
  Start = 'Start',
  Voting = 'Voting',
  Results = 'Results'
}

interface Participant {
  username: string;
  hasVoted: boolean;
  vote: string | null;
}

interface VotingResults {
  storyName: string;
  userVotes: { [username: string]: string };
  averageScore: number | null;
}

interface RoomState {
  roomId: string;
  roomName: string;
  hostUsername: string;
  mode: VotingMode;
  participants: Participant[];
  storyName: string | null;
  cardValues: string[] | null;
  timerSeconds: number | null;
  votingStartTime: string | null;
  results: VotingResults | null;
}

export default function RoomPage() {
  const { id } = useParams<{ id: string }>();
  const [roomState, setRoomState] = useState<RoomState | null>(null);
  const [notFound, setNotFound] = useState(false);
  const [storyName, setStoryName] = useState('');
  const [cardValues, setCardValues] = useState('');
  const [timerSeconds, setTimerSeconds] = useState('');
  const [selectedCard, setSelectedCard] = useState<string | null>(null);
  const [timeRemaining, setTimeRemaining] = useState<number | null>(null);
  const [connectionError, setConnectionError] = useState<string | null>(null);
  const navigate = useNavigate();
  const { token, user, signOut } = useAuth();
  const username = user?.username;
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    if (!id || !token || !username) return;

    const hubUrl = `${API_BASE_URL}/hubs/planningpoker`;
    console.log('SignalR Hub URL:', hubUrl);
    
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .build();

    connectionRef.current = connection;

    // Event handlers
    connection.on('UserJoined', (joinedUsername: string) => {
      console.log(`${joinedUsername} joined the room`);
      setRoomState(prev => {
        if (!prev) return prev;
        // Check if user already exists
        if (prev.participants.some(p => p.username === joinedUsername)) {
          return prev;
        }
        // Add new participant
        return {
          ...prev,
          participants: [...prev.participants, {
            username: joinedUsername,
            hasVoted: false,
            vote: null
          }]
        };
      });
    });

    connection.on('UserLeft', (leftUsername: string) => {
      console.log(`${leftUsername} left the room`);
      setRoomState(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          participants: prev.participants.filter(p => p.username !== leftUsername)
        };
      });
    });

    connection.on('VotingStarted', (state: RoomState) => {
      console.log('Voting started');
      setRoomState(state);
      setSelectedCard(null);
      // Initialize card values and timer for display
      if (state.cardValues) {
        setCardValues(state.cardValues.join(','));
      }
      if (state.timerSeconds) {
        setTimerSeconds(state.timerSeconds.toString());
        setTimeRemaining(state.timerSeconds);
      } else {
        setTimeRemaining(null);
      }
    });

    connection.on('UserVoted', (votedUsername: string) => {
      console.log(`${votedUsername} voted`);
      setRoomState(prev => {
        if (!prev) return prev;
        return {
          ...prev,
          participants: prev.participants.map(p =>
            p.username === votedUsername ? { ...p, hasVoted: true } : p
          )
        };
      });
    });

    connection.on('ResultsRevealed', (state: RoomState) => {
      console.log('Results revealed');
      setRoomState(state);
      setTimeRemaining(null);
    });

    // Connect and join room
    connection.start()
      .then(() => {
        console.log('Connected to SignalR hub');
        return connection.invoke<RoomState>('JoinRoom', id, token);
      })
      .then(state => {
        console.log('Joined room:', state);
        console.log('Room mode:', state.mode, 'Type:', typeof state.mode);
        console.log('Is host:', state.hostUsername === username);
        console.log('VotingMode.Start:', VotingMode.Start);
        setRoomState(state);
        setConnectionError(null);
        // Initialize form fields with room defaults
        if (state.cardValues) {
          setCardValues(state.cardValues.join(','));
        }
        // Timer: use the value from state (which includes room default in Start/Results mode)
        if (state.timerSeconds !== null && state.timerSeconds !== undefined) {
          setTimerSeconds(state.timerSeconds.toString());
        } else {
          setTimerSeconds('');
        }
      })
      .catch(err => {
        console.error('Error connecting to room:', err);
        const errorMessage = err.message || err.toString();
        console.error('Full error:', errorMessage);
        
        // Check for authentication errors
        if (errorMessage?.toLowerCase().includes('unauthorized')) {
          console.log('Authentication failed, redirecting to sign-in');
          signOut();
          navigate('/');
          return;
        }
        
        if (errorMessage?.toLowerCase().includes('not found') || errorMessage?.toLowerCase().includes('room not found')) {
          setNotFound(true);
        } else {
          setConnectionError(errorMessage);
        }
      });

    return () => {
      if (connection.state === signalR.HubConnectionState.Connected) {
        connection.invoke('LeaveRoom', id)
          .then(() => connection.stop())
          .catch(err => console.error('Error leaving room:', err));
      }
    };
  }, [id, token, username]);

  // Timer countdown effect
  useEffect(() => {
    if (roomState?.mode === VotingMode.Voting && roomState.votingStartTime && roomState.timerSeconds) {
      const startTime = new Date(roomState.votingStartTime).getTime();
      const interval = setInterval(() => {
        const now = Date.now();
        const elapsed = (now - startTime) / 1000;
        const remaining = Math.max(0, roomState.timerSeconds! - elapsed);
        setTimeRemaining(remaining);
        
        if (remaining === 0) {
          clearInterval(interval);
        }
      }, 100);

      return () => clearInterval(interval);
    }
  }, [roomState?.mode, roomState?.votingStartTime, roomState?.timerSeconds]);

  const handleStartVoting = async () => {
    if (!connectionRef.current || !storyName.trim()) return;

    try {
      const cards = cardValues.trim() ? cardValues.split(',').map(c => c.trim()) : null;
      const timer = timerSeconds.trim() ? parseInt(timerSeconds) : null;

      await connectionRef.current.invoke('StartVoting', id, {
        storyName: storyName.trim(),
        cardValues: cards,
        timerSeconds: timer
      });
      
      setStoryName('');
    } catch (err) {
      console.error('Error starting voting:', err);
    }
  };

  const handleSubmitVote = async (cardValue: string) => {
    if (!connectionRef.current || selectedCard !== null) return;

    try {
      await connectionRef.current.invoke('SubmitVote', id, {
        cardValue
      });
      setSelectedCard(cardValue);
    } catch (err) {
      console.error('Error submitting vote:', err);
    }
  };

  if (notFound) {
    return (
      <div className="room-not-found">
        <h2 style={{ textAlign: 'center' }}>Room not found.</h2>
        <p style={{ textAlign: 'center' }}>Probably it was deleted by admin. Would you like to create a new room?</p>
        <div className="centered-buttons">
          <button onClick={() => navigate('/new-room')}>Create a new room</button>
          <Link to="/" className="btn btn-grey">Home</Link>
        </div>
      </div>
    );
  }

  if (connectionError) {
    return (
      <div className="room-not-found">
        <h2 style={{ textAlign: 'center' }}>Connection Error</h2>
        <p style={{ textAlign: 'center', color: 'red' }}>{connectionError}</p>
        <p style={{ textAlign: 'center' }}>Please check that the server is running and try refreshing the page.</p>
        <div className="centered-buttons">
          <button onClick={() => window.location.reload()}>Retry</button>
          <Link to="/" className="btn btn-grey">Home</Link>
        </div>
      </div>
    );
  }

  if (!roomState) return <div style={{ textAlign: 'center', marginTop: '3rem' }}>Loading...</div>;

  const isHost = roomState.hostUsername === username;
  const currentUserParticipant = roomState.participants.find(p => p.username === username);
  const hasCurrentUserVoted = currentUserParticipant?.hasVoted || selectedCard !== null;
  const isHostConnected = roomState.participants.some(p => p.username === roomState.hostUsername);

  // Debug logging
  console.log('Rendering - Mode:', roomState.mode, 'IsHost:', isHost, 'Participants:', roomState.participants.length);

  return (
    <div className="room-page" style={{ display: 'flex', minHeight: '100vh', padding: '2rem' }}>
      {/* Main Area */}
      <div style={{ flex: 1, marginRight: '2rem' }}>
        <h2 style={{ textAlign: 'center', fontSize: '2.2rem', marginBottom: '2rem', fontWeight: 600 }}>
          {roomState.roomName}
        </h2>

        {/* Start Mode */}
        {roomState.mode === VotingMode.Start && (
          <div style={{ maxWidth: '600px', margin: '0 auto' }}>
            {isHost ? (
              <VotingForm
                storyName={storyName}
                cardValues={cardValues}
                timerSeconds={timerSeconds}
                onStoryNameChange={setStoryName}
                onCardValuesChange={setCardValues}
                onTimerSecondsChange={setTimerSeconds}
                onSubmit={handleStartVoting}
                submitDisabled={!storyName.trim()}
                submitLabel="Start Voting"
                title="Start a New Voting Round"
                idPrefix="start-"
              />
            ) : (
              <div style={{ textAlign: 'center', fontSize: '1.2rem', color: '#666' }}>
                {isHostConnected 
                  ? 'The host is preparing the story' 
                  : 'Waiting for the host to connect'}
              </div>
            )}
          </div>
        )}

        {/* Voting Mode */}
        {roomState.mode === VotingMode.Voting && (
          <div>
            <div style={{ 
              textAlign: 'center', 
              marginBottom: '1.5rem',
              padding: '1rem 2rem',
              border: '3px solid #4CAF50',
              borderRadius: '12px',
              backgroundColor: '#f1f8f4',
              maxWidth: '600px',
              margin: '0 auto 1.5rem'
            }}>
              <h3 style={{ margin: 0, fontSize: '1.5rem', color: '#2e7d32' }}>{roomState.storyName}</h3>
            </div>
            
            {timeRemaining !== null && (
              <div style={{ textAlign: 'center', fontSize: '1.5rem', marginBottom: '1rem', color: timeRemaining < 10 ? 'red' : '#333' }}>
                Time Remaining: {Math.ceil(timeRemaining)}s
              </div>
            )}

            <div style={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'center', gap: '1rem', marginTop: '2rem' }}>
              {roomState.cardValues?.map(card => (
                <button
                  key={card}
                  onClick={() => handleSubmitVote(card)}
                  disabled={hasCurrentUserVoted}
                  style={{
                    width: '80px',
                    height: '120px',
                    fontSize: '2rem',
                    fontWeight: 'bold',
                    border: selectedCard === card ? '3px solid #4CAF50' : '2px solid #ccc',
                    backgroundColor: selectedCard === card ? '#e8f5e9' : 'white',
                    borderRadius: '8px',
                    cursor: hasCurrentUserVoted ? 'not-allowed' : 'pointer',
                    opacity: hasCurrentUserVoted && selectedCard !== card ? 0.5 : 1
                  }}
                >
                  {card}
                </button>
              ))}
            </div>

            {hasCurrentUserVoted && (
              <div style={{ textAlign: 'center', marginTop: '2rem', fontSize: '1.1rem', color: '#4CAF50' }}>
                ✓ Your vote has been locked
              </div>
            )}
          </div>
        )}

        {/* Results Mode */}
        {roomState.mode === VotingMode.Results && roomState.results && (
          <div>
            <div style={{ 
              textAlign: 'center', 
              marginBottom: '2rem',
              padding: '1rem 2rem',
              border: '3px solid #2196F3',
              borderRadius: '12px',
              backgroundColor: '#e3f2fd',
              maxWidth: '600px',
              margin: '0 auto 2rem'
            }}>
              <h3 style={{ margin: 0, fontSize: '1.5rem', color: '#1565c0' }}>{roomState.results.storyName}</h3>
            </div>
            
            {roomState.results.averageScore !== null && (() => {
              const average = roomState.results.averageScore;
              // Find closest card value to average
              const availableCards = roomState.cardValues || [];
              const numericCards = availableCards
                .map(c => ({ value: c, numeric: parseFloat(c) }))
                .filter(c => !isNaN(c.numeric));
              
              let closest = null;
              if (numericCards.length > 0) {
                closest = numericCards.reduce((closest, card) => {
                  return Math.abs(card.numeric - average) <= Math.abs(closest.numeric - average) 
                    ? card 
                    : closest;
                }).value;
              }
              
              return (
                <div style={{ textAlign: 'center', marginBottom: '2rem' }}>
                  <div style={{ 
                    fontSize: '3rem', 
                    fontWeight: 'bold', 
                    color: '#4CAF50',
                    marginBottom: '0.5rem'
                  }}>
                    Average Score: {average}
                  </div>
                  {closest && (
                    <div style={{
                      fontSize: '1.5rem',
                      color: '#1976d2',
                      fontWeight: '600'
                    }}>
                      Closest: {closest}
                    </div>
                  )}
                </div>
              );
            })()}

            {roomState.results.averageScore === null && (
              <div style={{ textAlign: 'center', fontSize: '1.2rem', color: '#666', marginBottom: '2rem' }}>
                No numeric votes to calculate average
              </div>
            )}

            {/* Show start voting controls for host */}
            {isHost && (
              <div style={{ maxWidth: '600px', margin: '2rem auto', padding: '2rem', border: '2px solid #e0e0e0', borderRadius: '8px' }}>
                <VotingForm
                  storyName={storyName}
                  cardValues={cardValues}
                  timerSeconds={timerSeconds}
                  onStoryNameChange={setStoryName}
                  onCardValuesChange={setCardValues}
                  onTimerSecondsChange={setTimerSeconds}
                  onSubmit={handleStartVoting}
                  submitDisabled={!storyName.trim()}
                  submitLabel="Start Voting"
                  title="Start Next Round"
                  idPrefix="results-"
                />
              </div>
            )}
          </div>
        )}

        <div className="centered-buttons" style={{ marginTop: '3rem' }}>
          <Link to="/" className="btn btn-grey">Home</Link>
        </div>
      </div>

      {/* Right Sidebar - Players List */}
      <div style={{ 
        width: '300px', 
        borderLeft: '2px solid #e0e0e0', 
        paddingLeft: '2rem',
        minHeight: '100%'
      }}>
        <h3 style={{ marginBottom: '1rem' }}>Players ({roomState.participants.length})</h3>
        <table style={{ width: '100%', borderCollapse: 'collapse' }}>
          <thead>
            <tr style={{ borderBottom: '2px solid #e0e0e0' }}>
              <th style={{ textAlign: 'left', padding: '0.5rem' }}>Name</th>
              <th style={{ textAlign: 'center', padding: '0.5rem' }}>Vote</th>
            </tr>
          </thead>
          <tbody>
            {[...roomState.participants]
              .sort((a, b) => {
                // Host always on top
                if (a.username === roomState.hostUsername) return -1;
                if (b.username === roomState.hostUsername) return 1;
                return 0;
              })
              .map(participant => (
              <tr key={participant.username} style={{ borderBottom: '1px solid #f0f0f0' }}>
                <td style={{ padding: '0.5rem' }}>
                  <span style={{ fontWeight: participant.username === username ? 'bold' : 'normal' }}>
                    {participant.username}
                  </span>
                  {participant.username === roomState.hostUsername && ' 👑'}
                </td>
                <td style={{ textAlign: 'center', padding: '0.5rem' }}>
                  {roomState.mode === VotingMode.Start && '-'}
                  {roomState.mode === VotingMode.Voting && (participant.hasVoted ? '✓' : '-')}
                  {roomState.mode === VotingMode.Results && (participant.vote || '?')}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

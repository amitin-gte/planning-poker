import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';

const API_BASE_URL = 'http://localhost:5233';

export default function NewRoomPage() {
  const [name, setName] = useState('');
  const [cards, setCards] = useState('1,2,3,5,8,13,21,?');
  const [timer, setTimer] = useState(60);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();
  const { token } = useAuth();

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      const res = await fetch(`${API_BASE_URL}/rooms`, {
        method: 'POST',
        headers: { 
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`
        },
        body: JSON.stringify({
          name,
          pokerCards: cards.split(',').map(c => c.trim()),
          votingCountdownSeconds: timer
        })
      });
      if (!res.ok) throw new Error('Failed to create room');
      const data = await res.json();
      navigate(`/room/${data.roomId}`);
    } catch (err: any) {
      setError(err.message || 'Unknown error');
    }
  };

  return (
    <div className="new-room-page">
      <h2 style={{ textAlign: 'center' }}>Create a New Poker Room</h2>
      <form onSubmit={handleCreate} className="centered-buttons" style={{ maxWidth: 400, margin: '0 auto' }}>
        <div style={{ width: '100%' }}>
          <label htmlFor="room-name">Room Name:</label>
          <input id="room-name" value={name} onChange={e => setName(e.target.value)} required style={{ width: '100%' }} />
        </div>
        <div style={{ width: '100%' }}>
          <label htmlFor="poker-cards">Poker Cards (comma separated):</label>
          <input id="poker-cards" value={cards} onChange={e => setCards(e.target.value)} required style={{ width: '100%' }} />
        </div>
        <div style={{ width: '100%' }}>
          <label htmlFor="voting-countdown">Voting Countdown (seconds):</label>
          <input id="voting-countdown" type="number" value={timer} min={10} max={600} onChange={e => setTimer(Number(e.target.value))} required style={{ width: '100%' }} />
        </div>
        <button type="submit">Create</button>
        {error && <div className="error">{error}</div>}
      </form>
      <div className="centered-buttons" style={{ marginTop: '1.5rem' }}>
        <Link to="/" className="btn btn-grey">Home</Link>
      </div>
    </div>
  );
}

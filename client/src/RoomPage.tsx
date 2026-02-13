import React, { useEffect, useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { API_BASE_URL } from './config';

interface RoomConfig {
  roomId: string;
  name: string;
}

export default function RoomPage() {
  const { id } = useParams<{ id: string }>();
  const [room, setRoom] = useState<RoomConfig | null>(null);
  const [notFound, setNotFound] = useState(false);
  const navigate = useNavigate();
  const { token } = useAuth();

  useEffect(() => {
    if (!id || !token) return;
    fetch(`${API_BASE_URL}/rooms/${id}`, {
      headers: {
        'Authorization': `Bearer ${token}`
      }
    })
      .then(res => {
        if (res.ok) return res.json();
        if (res.status === 404) throw new Error('notfound');
        throw new Error('API error');
      })
      .then(data => setRoom(data))
      .catch(err => {
        if (err.message === 'notfound') setNotFound(true);
      });
  }, [id, token]);

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

  if (!room) return <div>Loading...</div>;

  return (
    <div className="room-page" style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', marginTop: '3rem' }}>
      <h2 style={{ textAlign: 'center', fontSize: '2.2rem', marginBottom: '2rem', fontWeight: 600, letterSpacing: '0.03em' }}>{room.name}</h2>
      <div className="centered-buttons" style={{ marginTop: '1.5rem' }}>
        <Link to="/" className="btn btn-grey">Home</Link>
      </div>
    </div>
  );
}

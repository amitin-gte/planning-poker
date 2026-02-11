import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';

const API_BASE_URL = 'http://localhost:5233';

interface RoomConfig {
  roomId: string;
  name: string;
}

export default function RoomPage() {
  const { id } = useParams<{ id: string }>();
  const [room, setRoom] = useState<RoomConfig | null>(null);
  const [notFound, setNotFound] = useState(false);
  const navigate = useNavigate();

  useEffect(() => {
    if (!id) return;
    fetch(`${API_BASE_URL}/rooms/${id}`)
      .then(res => {
        if (res.ok) return res.json();
        if (res.status === 404) throw new Error('notfound');
        throw new Error('API error');
      })
      .then(data => setRoom(data))
      .catch(err => {
        if (err.message === 'notfound') setNotFound(true);
      });
  }, [id]);

  if (notFound) {
    return (
      <div className="room-not-found">
        <h2>Room not found.</h2>
        <p>Probably it was deleted by admin. Would you like to create a new room?</p>
        <button onClick={() => navigate('/new-room')}>Create a new room</button>
      </div>
    );
  }

  if (!room) return <div>Loading...</div>;

  return (
    <div className="room-page">
      <h2>{room.name}</h2>
    </div>
  );
}

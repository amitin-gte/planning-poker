import React, { useEffect, useState } from 'react';

const API_BASE_URL = 'http://localhost:5233';

interface RoomConfig {
  roomId: string;
  name: string;
  pokerCards: string[];
  votingCountdownSeconds: number;
}

export default function AdminRoomsPage() {
  const [rooms, setRooms] = useState<RoomConfig[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRooms = async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(`${API_BASE_URL}/rooms`);
      if (!res.ok) throw new Error('Failed to fetch rooms');
      const data = await res.json();
      setRooms(data);
    } catch (err: any) {
      setError(err.message || 'Unknown error');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchRooms(); }, []);

  const handleDelete = async (roomId: string) => {
    if (!window.confirm('Delete this room?')) return;
    try {
      const res = await fetch(`${API_BASE_URL}/rooms/${roomId}`, { method: 'DELETE' });
      if (!res.ok) throw new Error('Failed to delete');
      setRooms(rooms => rooms.filter(r => r.roomId !== roomId));
    } catch (err: any) {
      alert(err.message || 'Unknown error');
    }
  };

  return (
    <div className="admin-rooms-page">
      <h2>All Poker Rooms</h2>
      {loading ? <div>Loading...</div> : error ? <div className="error">{error}</div> : (
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Cards</th>
              <th>Timer</th>
              <th>Room ID</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {rooms.map(room => (
              <tr key={room.roomId}>
                <td>{room.name}</td>
                <td>{room.pokerCards.join(', ')}</td>
                <td>{room.votingCountdownSeconds}s</td>
                <td>{room.roomId}</td>
                <td><button onClick={() => handleDelete(room.roomId)}>Delete</button></td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}

import React, { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { API_BASE_URL } from './config';

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
  const { token } = useAuth();

  const fetchRooms = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(`${API_BASE_URL}/rooms`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!res.ok) throw new Error('Failed to fetch rooms');
      const data = await res.json();
      setRooms(data);
    } catch (err: any) {
      setError(err.message || 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => { 
    if (token) {
      fetchRooms(); 
    }
  }, [token, fetchRooms]);

  const handleDelete = async (roomId: string) => {
    if (!window.confirm('Are you sure you want to delete this room? This action cannot be undone.')) return;
    try {
      const res = await fetch(`${API_BASE_URL}/rooms/${roomId}`, { 
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!res.ok) throw new Error('Failed to delete');
      await fetchRooms();
    } catch (err: any) {
      alert(err.message || 'Unknown error');
    }
  };

  return (
    <div className="admin-rooms-page">
      <h2 style={{ textAlign: 'center' }}>All Poker Rooms</h2>
      {loading ? <div>Loading...</div> : error ? <div className="error">{error}</div> : (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
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
                  <td>
                    <Link to={`/room/${room.roomId}`} className="room-link">{room.roomId}</Link>
                  </td>
                  <td>
                    <div className="centered-buttons" style={{ margin: 0, gap: 0 }}>
                      <button className="btn btn-red" title="Delete room" onClick={() => handleDelete(room.roomId)}>
                        <span role="img" aria-label="Delete">🗑️</span>
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div className="centered-buttons" style={{ marginTop: '1.5rem' }}>
            <Link to="/" className="btn btn-grey">Home</Link>
          </div>
        </div>
      )}
    </div>
  );
}

import React, { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from './AuthContext';

const API_BASE_URL = 'http://localhost:5233';

interface UserListItem {
  username: string;
  role: 'Admin' | 'User';
}

export default function AdminUsersPage() {
  const [users, setUsers] = useState<UserListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const { token } = useAuth();

  const fetchUsers = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const res = await fetch(`${API_BASE_URL}/users/list`, {
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!res.ok) throw new Error('Failed to fetch users');
      const data = await res.json();
      setUsers(data);
    } catch (err: any) {
      setError(err.message || 'Unknown error');
    } finally {
      setLoading(false);
    }
  }, [token]);

  useEffect(() => { 
    if (token) {
      fetchUsers(); 
    }
  }, [token, fetchUsers]);

  const handleDelete = async (username: string) => {
    if (!window.confirm(`Are you sure you want to delete user "${username}"? This action cannot be undone.`)) return;
    try {
      const res = await fetch(`${API_BASE_URL}/users/${username}`, { 
        method: 'DELETE',
        headers: {
          'Authorization': `Bearer ${token}`
        }
      });
      if (!res.ok) throw new Error('Failed to delete');
      setUsers(users => users.filter(u => u.username !== username));
    } catch (err: any) {
      alert(err.message || 'Unknown error');
    }
  };

  return (
    <div className="admin-users-page">
      <h2 style={{ textAlign: 'center' }}>All Users</h2>
      {loading ? <div>Loading...</div> : error ? <div className="error">{error}</div> : (
        <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
          <table>
            <thead>
              <tr>
                <th>Username</th>
                <th>Role</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {users.map(user => (
                <tr key={user.username}>
                  <td>{user.username}</td>
                  <td>{user.role}</td>
                  <td>
                    <div className="centered-buttons" style={{ margin: 0, gap: 0 }}>
                      <button className="btn btn-red" title="Delete user" onClick={() => handleDelete(user.username)}>
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

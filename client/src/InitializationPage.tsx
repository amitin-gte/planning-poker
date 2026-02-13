import React, { useState } from 'react';
import { useAuth } from './AuthContext';

interface InitializationPageProps {
  onComplete: () => void;
}

const InitializationPage: React.FC<InitializationPageProps> = ({ onComplete }) => {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const { signIn } = useAuth();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!username || !password) {
      setError('Username and password are required');
      return;
    }

    const success = await signIn(username, password);
    if (success) {
      onComplete();
    } else {
      setError('Failed to initialize site');
    }
  };

  return (
    <div className="centered-form">
      <div className="form-container">
        <h1>Welcome to Planning Poker!</h1>
        <p style={{ marginBottom: '1rem' }}>🎉 <strong>Site Initialization</strong></p>
        <p style={{ fontSize: '0.95rem', lineHeight: '1.6', marginBottom: '1.5rem' }}>
          This appears to be your first time here. Please create an administrator account to get started. 
          You'll have full access to manage rooms and users.
        </p>
        
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="username">Admin Username:</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Choose admin username"
              autoFocus
            />
          </div>
          
          <div className="form-group">
            <label htmlFor="password">Admin Password:</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Choose admin password"
            />
          </div>
          
          {error && <div className="error-message">{error}</div>}
          
          <button type="submit" className="btn">Create Admin Account & Sign In</button>
        </form>
      </div>
    </div>
  );
};

export default InitializationPage;

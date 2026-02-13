import React, { useState } from 'react';
import { useAuth } from './AuthContext';

interface SignInPageProps {
  onComplete: () => void;
}

const SignInPage: React.FC<SignInPageProps> = ({ onComplete }) => {
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
      setError('Sign in failed');
    }
  };

  return (
    <div className="centered-form">
      <div className="form-container">
        <h1>Planning Poker</h1>
        <p>Sign in or sign up to continue</p>
        
        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="username">Username:</label>
            <input
              id="username"
              type="text"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="Enter username"
              autoFocus
            />
          </div>
          
          <div className="form-group">
            <label htmlFor="password">Password:</label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter password"
            />
          </div>
          
          {error && <div className="error-message">{error}</div>}
          
          <button type="submit" className="btn">Sign In / Sign Up</button>
        </form>
      </div>
    </div>
  );
};

export default SignInPage;

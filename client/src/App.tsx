
import React, { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from 'react-router-dom';
import './App.css';
import NewRoomPage from './NewRoomPage';
import AdminRoomsPage from './AdminRoomsPage';
import AdminUsersPage from './AdminUsersPage';
import RoomPage from './RoomPage';
import { AuthProvider, useAuth } from './AuthContext';
import InitializationPage from './InitializationPage';
import SignInPage from './SignInPage';
import { getAndClearRedirectUrl } from './authUtils';

function Home() {
  const { user, isAdmin, signOut } = useAuth();

  return (
    <div className="home-options centered-buttons">
      <h1 style={{ marginBottom: '2rem' }}>Planning Poker</h1>
      <p>Welcome, {user?.username}! <button onClick={signOut} className="btn btn-grey" style={{ display: 'inline', padding: '0.25rem 0.5rem', marginLeft: '0.5rem' }}>Sign Out</button></p>
      
      <Link to="/new-room" className="btn">Create a new poker room</Link>
      {isAdmin() && (
        <>
          <Link to="/admin/rooms" className="btn">See all rooms</Link>
          <Link to="/admin/users" className="btn">Manage users</Link>
        </>
      )}
    </div>
  );
}

function AppContent() {
  const { user, isLoading, needsInitialization } = useAuth();
  const [initComplete, setInitComplete] = useState(false);
  const navigate = useNavigate();

  // Handle redirect after sign-in
  useEffect(() => {
    if (user) {
      const redirectUrl = getAndClearRedirectUrl();
      if (redirectUrl) {
        navigate(redirectUrl);
      }
    }
  }, [user, navigate]);

  if (isLoading) {
    return <div className="centered-form"><div>Loading...</div></div>;
  }

  // Show initialization page if needed
  if (needsInitialization && !initComplete) {
    return <InitializationPage onComplete={() => setInitComplete(true)} />;
  }

  // Show sign-in page if not authenticated
  if (!user) {
    return <SignInPage onComplete={() => {}} />;
  }

  // Show main app
  return (
    <Routes>
      <Route path="/" element={<Home />} />
      <Route path="/new-room" element={<NewRoomPage />} />
      <Route path="/admin/rooms" element={<AdminRoomsPage />} />
      <Route path="/admin/users" element={<AdminUsersPage />} />
      <Route path="/room/:id" element={<RoomPage />} />
    </Routes>
  );
}

function App() {
  return (
    <AuthProvider>
      <Router>
        <AppContent />
      </Router>
    </AuthProvider>
  );
}

export default App;

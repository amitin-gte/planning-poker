
import React from 'react';
import { BrowserRouter as Router, Routes, Route, Link, useNavigate } from 'react-router-dom';
import './App.css';
import NewRoomPage from './NewRoomPage';
import AdminRoomsPage from './AdminRoomsPage';
import RoomPage from './RoomPage';

function Home() {
  return (
    <div className="home-options">
      <h1>Planning Poker</h1>
      <Link to="/new-room" className="btn">Create a new poker room</Link>
      <Link to="/admin/rooms" className="btn">See all rooms</Link>
    </div>
  );
}


function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/new-room" element={<NewRoomPage />} />
        <Route path="/admin/rooms" element={<AdminRoomsPage />} />
        <Route path="/room/:id" element={<RoomPage />} />
      </Routes>
    </Router>
  );
}

export default App;

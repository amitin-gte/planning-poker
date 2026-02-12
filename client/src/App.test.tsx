import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import App from './App';
import * as AuthContext from './AuthContext';

// Mock fetch globally
global.fetch = jest.fn();

describe('Planning Poker App', () => {
  beforeEach(() => {
    (fetch as jest.Mock).mockReset();
    // Mock localStorage
    Storage.prototype.getItem = jest.fn();
    Storage.prototype.setItem = jest.fn();
    Storage.prototype.removeItem = jest.fn();
  });

  it('renders initialization page when no users exist', async () => {
    // Mock the initialization check to return 404 (no users)
    (fetch as jest.Mock).mockResolvedValueOnce({
      status: 404
    });

    render(<App />);
    
    await waitFor(() => {
      expect(screen.getByText(/create your admin account/i)).toBeInTheDocument();
    });
  });

  it('renders sign-in page when users exist but not authenticated', async () => {
    // Mock the initialization check to return OK (users exist)
    (fetch as jest.Mock).mockResolvedValueOnce({
      status: 200,
      ok: true
    });

    render(<App />);
    
    await waitFor(() => {
      expect(screen.getByText(/sign in/i)).toBeInTheDocument();
    });
  });

  it('renders home page for authenticated admin user', async () => {
    // Mock localStorage to have an authenticated admin user
    (Storage.prototype.getItem as jest.Mock).mockImplementation((key) => {
      if (key === 'authToken') return 'fake-token';
      if (key === 'authUser') return JSON.stringify({ username: 'admin', role: 'Admin' });
      return null;
    });

    render(<App />);
    
    await waitFor(() => {
      expect(screen.getByText(/Welcome, admin!/i)).toBeInTheDocument();
      expect(screen.getByText(/See all rooms/i)).toBeInTheDocument();
      expect(screen.getByText(/Manage users/i)).toBeInTheDocument();
    });
  });

  it('renders home page for authenticated regular user without admin options', async () => {
    // Mock localStorage to have an authenticated regular user
    (Storage.prototype.getItem as jest.Mock).mockImplementation((key) => {
      if (key === 'authToken') return 'fake-token';
      if (key === 'authUser') return JSON.stringify({ username: 'user', role: 'User' });
      return null;
    });

    render(<App />);
    
    await waitFor(() => {
      expect(screen.getByText(/Welcome, user!/i)).toBeInTheDocument();
      expect(screen.queryByText(/See all rooms/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/Manage users/i)).not.toBeInTheDocument();
    });
  });
});

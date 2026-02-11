import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import App from './App';

global.fetch = jest.fn();

describe('Planning Poker App', () => {
  beforeEach(() => {
    (fetch as jest.Mock).mockReset();
  });

  it('renders root options', () => {
    render(<MemoryRouter><App /></MemoryRouter>);
    expect(screen.getByText(/Create a new poker room/i)).toBeInTheDocument();
    expect(screen.getByText(/See all rooms/i)).toBeInTheDocument();
  });

  it('navigates to new room page and creates room', async () => {
    (fetch as jest.Mock).mockResolvedValueOnce({ ok: true, json: async () => ({ roomId: 'abc123' }) });
    render(<MemoryRouter initialEntries={['/new-room']}><App /></MemoryRouter>);
    fireEvent.change(screen.getByLabelText(/Room Name/i), { target: { value: 'Test Room' } });
    fireEvent.change(screen.getByLabelText(/Poker Cards/i), { target: { value: '1,2,3' } });
    fireEvent.change(screen.getByLabelText(/Voting Countdown/i), { target: { value: 30 } });
    fireEvent.click(screen.getByText(/Create/i));
    await waitFor(() => expect(fetch).toHaveBeenCalled());
  });

  it('shows not found on missing room', async () => {
    (fetch as jest.Mock).mockResolvedValueOnce({ ok: false, status: 404 });
    render(<MemoryRouter initialEntries={['/room/doesnotexist']}><App /></MemoryRouter>);
    await waitFor(() => expect(screen.getByText(/Room not found/i)).toBeInTheDocument());
  });
});

import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import App from './App';

test('renders learn react link', () => {
  render(<App />);
  const linkElement = screen.getByText(/learn react/i);
  expect(linkElement).toBeInTheDocument();
});

test('renders Test button and handles health check', async () => {
  // Mock fetch
  global.fetch = jest.fn(() =>
    Promise.resolve({ ok: true })
  ) as jest.Mock;

  render(<App />);
  const testButton = screen.getByText('Test');
  expect(testButton).toBeInTheDocument();

  fireEvent.click(testButton);

  // Wait for the health status to appear
  const status = await screen.findByText('API is healthy!');
  expect(status).toBeInTheDocument();

  // Clean up fetch mock
  (global.fetch as jest.Mock).mockRestore && (global.fetch as jest.Mock).mockRestore();
});

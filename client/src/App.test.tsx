import React from 'react';
import { render, screen } from '@testing-library/react';
import App from './App';

test('renders learn react link', () => {
  render(<App />);
  const linkElement = screen.getByText(/learn react/i);
  expect(linkElement).toBeInTheDocument();
});

test('renders Test button and handles health check', async () => {
  // Mock fetch using spyOn for proper cleanup
  const fetchSpy = jest.spyOn(global, 'fetch').mockResolvedValue({
    ok: true,
  } as Response);

  render(<App />);
  const testButton = screen.getByText('Test');
  expect(testButton).toBeInTheDocument();

  testButton.click();

  // Wait for the health status to appear
  const status = await screen.findByText('API is healthy!');
  expect(status).toBeInTheDocument();

  // Clean up fetch mock
  fetchSpy.mockRestore();
});

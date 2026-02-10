import React from 'react';
import logo from './logo.svg';
import './App.css';

const API_BASE_URL = 'http://localhost:5233';

function App() {
  const [healthStatus, setHealthStatus] = React.useState<string | null>(null);

  const testHealth = async () => {
    setHealthStatus(null);
    try {
      const response = await fetch(`${API_BASE_URL}/health`);
      if (response.ok) {
        setHealthStatus('API is healthy!');
      } else {
        setHealthStatus('API health check failed.');
      }
    } catch (error) {
      console.error('Error while checking API health:', error);
      console.error('Error while checking API health:', error);
      setHealthStatus(`Error connecting to API: ${error instanceof Error ? error.message : 'Unknown error'}`);
    }
  };

  return (
    <div className="App">
      <header className="App-header">
        <img src={logo} className="App-logo" alt="logo" />
        <p>
          Edit <code>src/App.tsx</code> and save to reload.
        </p>
        <a
          className="App-link"
          href="https://reactjs.org"
          target="_blank"
          rel="noopener noreferrer"
        >
          Learn React
        </a>
        <button style={{ marginTop: 20 }} onClick={testHealth}>Test API Connection</button>
        {healthStatus && <div style={{ marginTop: 10 }}>{healthStatus}</div>}
      </header>
    </div>
  );
}

export default App;

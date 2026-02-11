import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';

interface User {
  username: string;
  role: 'Admin' | 'User';
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  signIn: (username: string, password: string) => Promise<boolean>;
  signOut: () => void;
  isAdmin: () => boolean;
  isLoading: boolean;
  needsInitialization: boolean;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [needsInitialization, setNeedsInitialization] = useState(false);

  useEffect(() => {
    // Check if site needs initialization
    const checkInitialization = async () => {
      try {
        const response = await fetch('http://localhost:5233/users/any');
        setNeedsInitialization(response.status === 404);
      } catch (error) {
        console.error('Failed to check initialization status:', error);
      }
    };

    // Load token from localStorage
    const storedToken = localStorage.getItem('authToken');
    const storedUser = localStorage.getItem('authUser');
    
    if (storedToken && storedUser) {
      setToken(storedToken);
      setUser(JSON.parse(storedUser));
      setIsLoading(false);
    } else {
      checkInitialization().finally(() => setIsLoading(false));
    }
  }, []);

  const signIn = async (username: string, password: string): Promise<boolean> => {
    try {
      const response = await fetch('http://localhost:5233/users/signin', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({ username, password }),
      });

      if (response.ok) {
        const data = await response.json();
        setToken(data.token);
        setUser({ username: data.username, role: data.role });
        
        // Store in localStorage
        localStorage.setItem('authToken', data.token);
        localStorage.setItem('authUser', JSON.stringify({ username: data.username, role: data.role }));
        
        setNeedsInitialization(false);
        return true;
      }
      return false;
    } catch (error) {
      console.error('Sign in failed:', error);
      return false;
    }
  };

  const signOut = () => {
    setToken(null);
    setUser(null);
    localStorage.removeItem('authToken');
    localStorage.removeItem('authUser');
  };

  const isAdmin = () => {
    return user?.role === 'Admin';
  };

  return (
    <AuthContext.Provider value={{ user, token, signIn, signOut, isAdmin, isLoading, needsInitialization }}>
      {children}
    </AuthContext.Provider>
  );
};

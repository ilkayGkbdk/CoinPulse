import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './context/AuthContext';

// Sayfalar ve Bileşenler
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Portfolio from './pages/Portfolio';
import Layout from './components/Layout';
import type { JSX } from 'react';

// Korumalı Rota Bileşeni (Giriş yapmamışsa Login'e atar)
const ProtectedRoute = ({ children }: { children: JSX.Element }) => {
  const { isAuthenticated } = useAuth();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  
  return children;
};

export default function App() {
  return (
    <BrowserRouter>
      {/* AuthProvider tüm uygulamayı sarmalar */}
      <AuthProvider>
        <Routes>
          
          {/* Giriş Sayfası (Korumasız) */}
          <Route path="/login" element={<Login />} />

          {/* Korumalı Alanlar (Layout içinde gösterilir) */}
          <Route element={
            <ProtectedRoute>
              <Layout />
            </ProtectedRoute>
          }>
            <Route path="/" element={<Dashboard />} />
            <Route path="/portfolio" element={<Portfolio />} />
          </Route>

          {/* Bilinmeyen rotaları Login'e yönlendir */}
          <Route path="*" element={<Navigate to="/login" />} />

        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
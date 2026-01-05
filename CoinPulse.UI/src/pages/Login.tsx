import { useState } from 'react';
import axios from 'axios';
import { useAuth, API_URL } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

export default function Login() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      const res = await axios.post(`${API_URL}/auth/login`, { email, password });
      login(res.data.token, res.data.username);
      navigate('/'); // Ana sayfaya yönlendir
    } catch (err) {
      setError('Giriş başarısız. Bilgileri kontrol edin.');
    }
  };

  return (
    <div className="flex items-center justify-center h-screen bg-slate-900">
      <div className="bg-slate-800 p-8 rounded-xl shadow-lg w-96 border border-slate-700">
        <h2 className="text-2xl font-bold text-white mb-6 text-center">CoinPulse Giriş</h2>
        {error && <div className="bg-red-500/20 text-red-400 p-2 rounded mb-4 text-sm">{error}</div>}
        <form onSubmit={handleLogin} className="space-y-4">
          <div>
            <label className="text-slate-400 text-sm">Email</label>
            <input 
              type="email" 
              className="w-full bg-slate-700 text-white p-2 rounded border border-slate-600 focus:outline-none focus:border-blue-500"
              value={email} onChange={e => setEmail(e.target.value)}
            />
          </div>
          <div>
            <label className="text-slate-400 text-sm">Şifre</label>
            <input 
              type="password" 
              className="w-full bg-slate-700 text-white p-2 rounded border border-slate-600 focus:outline-none focus:border-blue-500"
              value={password} onChange={e => setPassword(e.target.value)}
            />
          </div>
          <button type="submit" className="w-full bg-blue-600 hover:bg-blue-700 text-white p-2 rounded font-semibold transition">
            Giriş Yap
          </button>
        </form>
        <p className="mt-4 text-center text-slate-500 text-sm">
          Hesabın yok mu? <a href="#" className="text-blue-400">Kayıt Ol</a>
        </p>
      </div>
    </div>
  );
}
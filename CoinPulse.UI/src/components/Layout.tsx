import { Link, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { LayoutDashboard, Wallet, LogOut, User } from 'lucide-react';

export default function Layout() {
  const { user, logout } = useAuth();

  return (
    <div className="min-h-screen bg-slate-900 text-slate-200 font-sans">
      {/* Üst Menü (Navbar) */}
      <nav className="bg-slate-800 border-b border-slate-700 sticky top-0 z-50 shadow-md">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16 items-center">
            
            {/* Logo ve Linkler */}
            <div className="flex items-center gap-8">
              <h1 className="text-2xl font-bold bg-gradient-to-r from-blue-400 to-indigo-500 bg-clip-text text-transparent">
                CoinPulse
              </h1>
              <div className="hidden md:flex gap-4">
                <Link to="/" className="flex items-center gap-2 px-3 py-2 rounded-md hover:bg-slate-700 hover:text-white transition">
                  <LayoutDashboard size={18}/> Piyasa
                </Link>
                <Link to="/portfolio" className="flex items-center gap-2 px-3 py-2 rounded-md hover:bg-slate-700 hover:text-white transition">
                  <Wallet size={18}/> Portföyüm
                </Link>
              </div>
            </div>

            {/* Kullanıcı Bölümü */}
            <div className="flex items-center gap-4">
              <div className="flex items-center gap-2 text-sm text-slate-400 bg-slate-900 px-3 py-1 rounded-full border border-slate-700">
                <User size={14} />
                <span>{user}</span>
              </div>
              <button 
                onClick={logout} 
                className="text-red-400 hover:bg-red-500/10 p-2 rounded-full transition" 
                title="Çıkış Yap"
              >
                <LogOut size={20}/>
              </button>
            </div>

          </div>
        </div>
      </nav>

      {/* Sayfa İçeriği Burada Gösterilecek */}
      <main>
        <Outlet />
      </main>
    </div>
  );
}
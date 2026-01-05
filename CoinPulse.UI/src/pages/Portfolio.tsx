import { useEffect, useState } from 'react';
import axios from 'axios';
import { API_URL } from '../context/AuthContext';
import { PlusCircle, TrendingUp, TrendingDown, Wallet } from 'lucide-react';

interface PortfolioItem {
  symbol: string;
  totalAmount: number;
  averageCost: number;
  currentPrice: number;
  currentValue: number;
  profitLoss: number;
  profitLossPercentage: number;
}

export default function Portfolio() {
  const [items, setItems] = useState<PortfolioItem[]>([]);
  const [loading, setLoading] = useState(true);
  
  // Modal State
  const [showModal, setShowModal] = useState(false);
  const [buyForm, setBuyForm] = useState({ symbol: 'BTC', amount: '', price: '' });

  const fetchPortfolio = async () => {
    try {
      const res = await axios.get(`${API_URL}/portfolio`);
      setItems(res.data.data);
    } catch (err) {
      console.error("Portföy yüklenemedi", err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchPortfolio(); }, []);

  const handleBuy = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await axios.post(`${API_URL}/portfolio/buy`, {
        symbol: buyForm.symbol,
        amount: parseFloat(buyForm.amount),
        price: buyForm.price ? parseFloat(buyForm.price) : null // Boşsa null gönder (Market Emri)
      });
      setShowModal(false);
      setBuyForm({ symbol: 'BTC', amount: '', price: '' });
      fetchPortfolio(); // Listeyi güncelle
      alert("Alım Başarılı! 🚀");
    } catch (err) {
      alert("İşlem Başarısız");
    }
  };

  // Toplam Portföy Değeri
  const totalValue = items.reduce((acc, item) => acc + item.currentValue, 0);
  const totalPnL = items.reduce((acc, item) => acc + item.profitLoss, 0);

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Üst Özet Kartı */}
      <div className="bg-slate-800 p-6 rounded-2xl border border-slate-700 mb-8 flex justify-between items-center">
        <div>
          <h2 className="text-slate-400 flex items-center gap-2 mb-1"><Wallet size={20}/> Toplam Varlık</h2>
          <div className="text-4xl font-bold text-white">${totalValue.toLocaleString()}</div>
        </div>
        <div className={`text-right ${totalPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
          <div className="text-lg font-semibold">Toplam PnL</div>
          <div className="text-2xl font-bold">{totalPnL >= 0 ? '+' : ''}{totalPnL.toLocaleString()} $</div>
        </div>
      </div>

      {/* Alım Butonu */}
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-xl font-semibold text-white">Varlıklarım</h3>
        <button onClick={() => setShowModal(true)} className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg flex items-center gap-2 transition">
          <PlusCircle size={18}/> Yeni Ekle
        </button>
      </div>

      {/* Tablo */}
      <div className="bg-slate-800 rounded-xl border border-slate-700 overflow-hidden">
        <table className="w-full text-left text-slate-300">
          <thead className="bg-slate-900/50 text-slate-400 uppercase text-xs">
            <tr>
              <th className="p-4">Coin</th>
              <th className="p-4">Miktar</th>
              <th className="p-4">Ort. Maliyet</th>
              <th className="p-4">Anlık Fiyat</th>
              <th className="p-4">Değer</th>
              <th className="p-4">Kar/Zarar</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-700">
            {items.map(item => (
              <tr key={item.symbol} className="hover:bg-slate-700/30 transition">
                <td className="p-4 font-bold text-white">{item.symbol}</td>
                <td className="p-4">{item.totalAmount}</td>
                <td className="p-4">${item.averageCost.toLocaleString()}</td>
                <td className="p-4 text-blue-300">${item.currentPrice.toLocaleString()}</td>
                <td className="p-4 font-semibold text-white">${item.currentValue.toLocaleString()}</td>
                <td className={`p-4 font-bold flex items-center gap-1 ${item.profitLoss >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                  {item.profitLoss >= 0 ? <TrendingUp size={16}/> : <TrendingDown size={16}/>}
                  {item.profitLossPercentage.toFixed(2)}% (${item.profitLoss.toLocaleString()})
                </td>
              </tr>
            ))}
            {items.length === 0 && !loading && (
              <tr><td colSpan={6} className="p-8 text-center text-slate-500">Henüz varlık eklemediniz.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {/* Alım Modalı */}
      {showModal && (
        <div className="fixed inset-0 bg-black/70 flex items-center justify-center p-4 z-50">
          <div className="bg-slate-800 p-6 rounded-xl w-full max-w-md border border-slate-700">
            <h3 className="text-xl font-bold text-white mb-4">Varlık Ekle</h3>
            <form onSubmit={handleBuy} className="space-y-4">
              <div>
                <label className="text-slate-400 text-sm">Coin Sembolü</label>
                <select 
                  className="w-full bg-slate-700 text-white p-2 rounded border border-slate-600"
                  value={buyForm.symbol} onChange={e => setBuyForm({...buyForm, symbol: e.target.value})}
                >
                  {['BTC', 'ETH', 'SOL', 'AVAX', 'XRP'].map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </div>
              <div>
                <label className="text-slate-400 text-sm">Miktar</label>
                <input 
                  type="number" step="any" required
                  className="w-full bg-slate-700 text-white p-2 rounded border border-slate-600"
                  value={buyForm.amount} onChange={e => setBuyForm({...buyForm, amount: e.target.value})}
                />
              </div>
              <div>
                <label className="text-slate-400 text-sm">Fiyat (Opsiyonel - Boş bırakırsan güncel fiyat)</label>
                <input 
                  type="number" step="any" placeholder="Örn: 50000 (Geçmiş işlem için)"
                  className="w-full bg-slate-700 text-white p-2 rounded border border-slate-600"
                  value={buyForm.price} onChange={e => setBuyForm({...buyForm, price: e.target.value})}
                />
              </div>
              <div className="flex gap-2 pt-2">
                <button type="button" onClick={() => setShowModal(false)} className="flex-1 bg-slate-700 hover:bg-slate-600 text-white py-2 rounded">İptal</button>
                <button type="submit" className="flex-1 bg-blue-600 hover:bg-blue-700 text-white py-2 rounded font-bold">Satın Al</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
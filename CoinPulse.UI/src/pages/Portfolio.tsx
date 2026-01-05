import { useEffect, useState } from 'react';
import axios from 'axios';
import { API_URL } from '../context/AuthContext';
import { PlusCircle, TrendingUp, TrendingDown, Wallet, Search, X } from 'lucide-react';

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
  
  // Modal ve Form State'leri
  const [showModal, setShowModal] = useState(false);
  const [buyForm, setBuyForm] = useState({ symbol: '', amount: '', price: '' });
  
  // Arama (Search) State'leri
  const [searchResults, setSearchResults] = useState<string[]>([]);
  const [isSearching, setIsSearching] = useState(false);

  // Portföyü Yükle
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

  // Coin Arama Fonksiyonu (Binance API üzerinden)
  const handleSearch = async (query: string) => {
    // Kullanıcının yazdığını büyük harfe çevir ve state'e at
    const upperQuery = query.toUpperCase();
    setBuyForm({ ...buyForm, symbol: upperQuery });

    // Eğer 2 karakterden fazlaysa aramaya başla
    if (upperQuery.length >= 2) {
        setIsSearching(true);
        try {
            const res = await axios.get(`${API_URL}/market/search?query=${upperQuery}`);
            setSearchResults(res.data);
        } catch (error) {
            console.error("Arama hatası", error);
            setSearchResults([]);
        }
    } else {
        setSearchResults([]);
        setIsSearching(false);
    }
  };

  // Listeden Coin Seçince
  const selectCoin = (symbol: string) => {
      setBuyForm({ ...buyForm, symbol: symbol });
      setSearchResults([]); // Listeyi temizle
      setIsSearching(false); // Arama modundan çık
  };

  // Satın Alma İşlemi
  const handleBuy = async (e: React.FormEvent) => {
    e.preventDefault();
    try {
      await axios.post(`${API_URL}/portfolio/buy`, {
        symbol: buyForm.symbol,
        amount: parseFloat(buyForm.amount),
        price: buyForm.price ? parseFloat(buyForm.price) : null 
      });
      
      setShowModal(false);
      setBuyForm({ symbol: '', amount: '', price: '' });
      setSearchResults([]);
      fetchPortfolio(); // Listeyi güncelle
      alert(`${buyForm.symbol} portföye eklendi! 🎉`);
    } catch (err) {
      alert("İşlem Başarısız! Sembolü kontrol edin.");
    }
  };

  // Hesaplamalar
  const totalValue = items.reduce((acc, item) => acc + item.currentValue, 0);
  const totalPnL = items.reduce((acc, item) => acc + item.profitLoss, 0);

  return (
    <div className="p-8 max-w-7xl mx-auto">
      
      {/* --- ÜST KART (ÖZET) --- */}
      <div className="bg-slate-800 p-6 rounded-2xl border border-slate-700 mb-8 flex justify-between items-center shadow-lg">
        <div>
          <h2 className="text-slate-400 flex items-center gap-2 mb-1"><Wallet size={20}/> Toplam Varlık</h2>
          <div className="text-4xl font-bold text-white">${totalValue.toLocaleString(undefined, { minimumFractionDigits: 2 })}</div>
        </div>
        <div className={`text-right ${totalPnL >= 0 ? 'text-green-400' : 'text-red-400'}`}>
          <div className="text-lg font-semibold">Toplam Kar/Zarar</div>
          <div className="text-2xl font-bold flex items-center justify-end gap-2">
            {totalPnL >= 0 ? <TrendingUp size={24}/> : <TrendingDown size={24}/>}
            {totalPnL >= 0 ? '+' : ''}{totalPnL.toLocaleString()} $
          </div>
        </div>
      </div>

      {/* --- BUTONLAR --- */}
      <div className="flex justify-between items-center mb-4">
        <h3 className="text-xl font-semibold text-white">Varlıklarım</h3>
        <button onClick={() => setShowModal(true)} className="bg-blue-600 hover:bg-blue-700 text-white px-4 py-2 rounded-lg flex items-center gap-2 transition shadow-md hover:shadow-blue-500/20">
          <PlusCircle size={18}/> Yeni Ekle
        </button>
      </div>

      {/* --- PORTFÖY TABLOSU --- */}
      <div className="bg-slate-800 rounded-xl border border-slate-700 overflow-hidden shadow-md">
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
                <td className="p-4 font-bold text-white flex items-center gap-2">
                    <div className="w-8 h-8 rounded-full bg-slate-700 flex items-center justify-center text-xs text-slate-400 border border-slate-600">
                        {item.symbol.substring(0,1)}
                    </div>
                    {item.symbol}
                </td>
                <td className="p-4">{item.totalAmount}</td>
                <td className="p-4 text-slate-400">${item.averageCost.toLocaleString()}</td>
                <td className="p-4 text-blue-300 font-mono">${item.currentPrice.toLocaleString()}</td>
                <td className="p-4 font-semibold text-white">${item.currentValue.toLocaleString()}</td>
                <td className={`p-4 font-bold ${item.profitLoss >= 0 ? 'text-green-400' : 'text-red-400'}`}>
                  {item.profitLossPercentage.toFixed(2)}% <br/>
                  <span className="text-xs opacity-75">(${item.profitLoss.toLocaleString()})</span>
                </td>
              </tr>
            ))}
            {items.length === 0 && !loading && (
              <tr><td colSpan={6} className="p-8 text-center text-slate-500 italic">Henüz varlık eklemediniz. "Yeni Ekle" butonuna basın.</td></tr>
            )}
          </tbody>
        </table>
      </div>

      {/* --- ALIM MODALI --- */}
      {showModal && (
        <div className="fixed inset-0 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4 z-50 transition-opacity">
          <div className="bg-slate-800 p-6 rounded-2xl w-full max-w-md border border-slate-700 shadow-2xl relative">
            
            {/* Kapat Butonu */}
            <button onClick={() => setShowModal(false)} className="absolute top-4 right-4 text-slate-400 hover:text-white">
                <X size={20}/>
            </button>

            <h3 className="text-xl font-bold text-white mb-6 flex items-center gap-2">
                <PlusCircle className="text-blue-500"/> Varlık Ekle
            </h3>
            
            <form onSubmit={handleBuy} className="space-y-5">
              
              {/* COIN ARAMA KUTUSU (GÜNCELLENEN KISIM) */}
              <div className="relative">
                <label className="text-slate-400 text-sm mb-1 block">Coin Sembolü</label>
                <div className="relative">
                    <input 
                        type="text" 
                        required
                        className="w-full bg-slate-900 text-white p-3 pl-10 rounded-lg border border-slate-600 focus:border-blue-500 focus:ring-1 focus:ring-blue-500 outline-none transition uppercase placeholder-slate-600"
                        placeholder="Örn: DOGE, PEPE"
                        value={buyForm.symbol} 
                        onChange={e => handleSearch(e.target.value)}
                    />
                    <Search className="absolute left-3 top-3.5 text-slate-500" size={18}/>
                </div>

                {/* Arama Sonuçları Listesi */}
                {searchResults.length > 0 && isSearching && (
                    <ul className="absolute z-10 w-full bg-slate-800 border border-slate-600 mt-1 rounded-lg shadow-xl max-h-40 overflow-y-auto">
                        {searchResults.map(s => (
                            <li 
                                key={s} 
                                className="p-3 hover:bg-slate-700 cursor-pointer text-white border-b border-slate-700/50 last:border-0 flex justify-between items-center"
                                onClick={() => selectCoin(s)}
                            >
                                <span className="font-bold">{s}</span>
                                <span className="text-xs text-slate-500 bg-slate-900 px-2 py-1 rounded">USDT</span>
                            </li>
                        ))}
                    </ul>
                )}
              </div>

              {/* MİKTAR */}
              <div>
                <label className="text-slate-400 text-sm mb-1 block">Miktar</label>
                <input 
                  type="number" step="any" required
                  className="w-full bg-slate-900 text-white p-3 rounded-lg border border-slate-600 focus:border-blue-500 outline-none"
                  placeholder="0.00"
                  value={buyForm.amount} onChange={e => setBuyForm({...buyForm, amount: e.target.value})}
                />
              </div>

              {/* FİYAT (OPSİYONEL) */}
              <div>
                <label className="text-slate-400 text-sm mb-1 block">
                    Alış Fiyatı <span className="text-xs text-slate-500">(Boş bırakırsanız güncel fiyat alınır)</span>
                </label>
                <div className="relative">
                    <input 
                        type="number" step="any" 
                        className="w-full bg-slate-900 text-white p-3 pl-8 rounded-lg border border-slate-600 focus:border-blue-500 outline-none"
                        placeholder="0.00"
                        value={buyForm.price} onChange={e => setBuyForm({...buyForm, price: e.target.value})}
                    />
                    <span className="absolute left-3 top-3 text-slate-500">$</span>
                </div>
              </div>

              {/* BUTONLAR */}
              <div className="flex gap-3 pt-2">
                <button type="button" onClick={() => setShowModal(false)} className="flex-1 bg-slate-700 hover:bg-slate-600 text-white py-3 rounded-lg font-medium transition">
                    İptal
                </button>
                <button type="submit" className="flex-1 bg-blue-600 hover:bg-blue-500 text-white py-3 rounded-lg font-bold shadow-lg shadow-blue-900/20 transition">
                    Satın Al
                </button>
              </div>

            </form>
          </div>
        </div>
      )}
    </div>
  );
}
import { useEffect, useState } from "react";
import { useSignalR, type PriceUpdate } from "./hooks/useSignalR";
import {
	LineChart,
	Line,
	XAxis,
	YAxis,
	CartesianGrid,
	Tooltip,
	ResponsiveContainer,
} from "recharts";
import axios from "axios";

// Backend URL (Senin port numaranla güncelle!)
const API_BASE_URL = "http://localhost:5089";

function App() {
	// 1. Canlı Veriyi Dinle
	const { latestPrice } = useSignalR(`${API_BASE_URL}/hubs/crypto`);

	// State: Anlık Fiyatlar Listesi
	const [prices, setPrices] = useState<Record<string, PriceUpdate>>({});

	// State: Seçili Coin'in Geçmiş Verisi (Grafik için)
	const [selectedCoin, setSelectedCoin] = useState<string>("BTC");
	const [historyData, setHistoryData] = useState<any[]>([]);

	// Canlı veri gelince listeyi güncelle
	useEffect(() => {
		if (latestPrice) {
			setPrices((prev) => ({
				...prev,
				[latestPrice.symbol]: latestPrice,
			}));
		}
	}, [latestPrice]);

	// Seçili coin değişince Elasticsearch'ten geçmişi çek
	useEffect(() => {
		const fetchHistory = async () => {
			try {
				const response = await axios.get(
					`${API_BASE_URL}/api/prices/history/${selectedCoin}?hours=24`
				);

				// Debug için konsola basalım
				console.log("API Yanıtı:", response.data);

				// Veri boşsa veya hatalıysa kontrol et
				if (
					!response.data ||
					!response.data.data ||
					!Array.isArray(response.data.data)
				) {
					console.warn("Geçmiş verisi boş veya format hatalı.");
					setHistoryData([]);
					return;
				}

				const formattedData = response.data.data
					.map((item: any) => ({
						// Elastic'ten gelen tarih bazen farklı formatta olabilir, güvenli parse:
						time: new Date(item.dataTimestamp).toLocaleTimeString([], {
							hour: "2-digit",
							minute: "2-digit",
						}),
						price: item.price,
					}))
					.reverse();

				setHistoryData(formattedData);
			} catch (err) {
				console.error("Geçmiş veri çekilemedi:", err);
			}
		};

		fetchHistory();
		// Her 10 saniyede bir grafiği tazele
		const interval = setInterval(fetchHistory, 10000);
		return () => clearInterval(interval);
	}, [selectedCoin]);

	return (
		<div className="min-h-screen p-8 font-sans">
			<header className="mb-8 flex justify-between items-center">
				<div>
					<h1 className="text-3xl font-bold bg-gradient-to-r from-green-400 to-blue-500 bg-clip-text text-transparent">
						CoinPulse Dashboard
					</h1>
					<p className="text-slate-400">Real-time Market Monitor</p>
				</div>
				<div className="text-sm bg-slate-800 px-3 py-1 rounded-full border border-slate-700">
					Status: <span className="text-green-400">● Live</span>
				</div>
			</header>

			{/* Üst Kısım: Fiyat Kartları */}
			<div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
				{Object.values(prices)
					.sort((a, b) => a.symbol.localeCompare(b.symbol))
					.map((coin) => (
						<div
							key={coin.symbol}
							onClick={() => setSelectedCoin(coin.symbol)}
							className={`p-4 rounded-xl border border-slate-700 cursor-pointer transition-all hover:scale-105
              ${
					selectedCoin === coin.symbol
						? "bg-slate-800 ring-2 ring-blue-500"
						: "bg-slate-800/50"
				}
            `}
						>
							<div className="flex justify-between items-center mb-2">
								<span className="font-bold text-lg">
									{coin.symbol}
								</span>
								<span className="text-xs text-slate-500">
									Live
								</span>
							</div>
							<div className="text-2xl font-mono text-white">
								$
								{coin.price.toLocaleString("en-US", {
									minimumFractionDigits: 2,
								})}
							</div>
						</div>
					))}
				{Object.keys(prices).length === 0 && (
					<div className="col-span-4 text-center p-8 text-slate-500 border border-dashed border-slate-700 rounded-xl">
						Veri akışı bekleniyor... (./seed_data.sh çalıştırın)
					</div>
				)}
			</div>

			{/* Alt Kısım: Grafik */}
			<div className="bg-slate-800/50 p-6 rounded-2xl border border-slate-700">
				<h2 className="text-xl font-semibold mb-4 flex items-center gap-2">
					📊 {selectedCoin} - Son 24 Saat Analizi
				</h2>
				<div className="h-[400px] w-full">
					<ResponsiveContainer width="100%" height="100%">
						<LineChart data={historyData}>
							<CartesianGrid
								strokeDasharray="3 3"
								stroke="#334155"
							/>
							<XAxis dataKey="time" stroke="#94a3b8" />
							<YAxis domain={["auto", "auto"]} stroke="#94a3b8" />
							<Tooltip
								contentStyle={{
									backgroundColor: "#1e293b",
									borderColor: "#334155",
									color: "#fff",
								}}
							/>
							<Line
								type="monotone"
								dataKey="price"
								stroke="#3b82f6"
								strokeWidth={3}
								dot={false}
								animationDuration={500}
							/>
						</LineChart>
					</ResponsiveContainer>
				</div>
			</div>
		</div>
	);
}

export default App;

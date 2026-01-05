import { useEffect, useState } from "react";
import { useSignalR, type PriceUpdate } from "../hooks/useSignalR";
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
import { API_URL } from "../context/AuthContext"; // API URL'i buradan alıyoruz

// SignalR Hub adresi (API_URL /api ile bitiyor, onu kırpıp /hubs ekliyoruz)
const HUB_URL = API_URL.replace("/api", "/hubs/crypto");

export default function Dashboard() {
	const { latestPrice } = useSignalR(HUB_URL);
	const [prices, setPrices] = useState<Record<string, PriceUpdate>>({});
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

	// Grafik verisini çek
	useEffect(() => {
		const fetchHistory = async () => {
			try {
				// API URL'in sonundaki /api kısmını çıkarıp root URL'i buluyoruz
				const rootUrl = API_URL.replace("/api", "");
				const response = await axios.get(
					`${rootUrl}/api/prices/history/${selectedCoin}?hours=24`
				);

				if (!response.data?.data) return;

				const formattedData = response.data.data
					.map((item: any) => ({
						// DÜZELTME: Backend 'DataTimestamp' (veya camelCase: dataTimestamp) dönüyor.
						// Garanti olsun diye ikisini de kontrol ediyoruz.
						time: new Date(
							item.dataTimestamp || item.timestamp
						).toLocaleTimeString([], {
							hour: "2-digit",
							minute: "2-digit",
						}),
						price: item.price,
					}))
					.reverse();

				setHistoryData(formattedData);
			} catch (err) {
				console.error("Grafik verisi hatası:", err);
			}
		};

		fetchHistory();
		const interval = setInterval(fetchHistory, 10000); // 10 sn'de bir güncelle
		return () => clearInterval(interval);
	}, [selectedCoin]);

	return (
		<div className="p-8 max-w-7xl mx-auto">
			<h2 className="text-2xl font-bold text-white mb-6">Piyasa Özeti</h2>

			{/* 1. Fiyat Kartları */}
			<div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
				{Object.values(prices)
					.sort((a, b) => a.symbol.localeCompare(b.symbol))
					.map((coin) => (
						<div
							key={coin.symbol}
							onClick={() => setSelectedCoin(coin.symbol)}
							className={`p-4 rounded-xl border border-slate-700 cursor-pointer hover:scale-105 transition 
              ${
					selectedCoin === coin.symbol
						? "bg-slate-800 ring-2 ring-blue-500"
						: "bg-slate-800/50"
				}`}
						>
							<div className="flex justify-between items-center mb-2">
								<span className="font-bold text-white">
									{coin.symbol}
								</span>
								<span className="text-xs text-green-400 animate-pulse">
									● Canlı
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
						Veri akışı bekleniyor... (Worker çalışıyor mu?)
					</div>
				)}
			</div>

			{/* 2. Grafik Alanı */}
			<div className="bg-slate-800 p-6 rounded-2xl border border-slate-700 h-[500px]">
				<h3 className="text-xl font-semibold mb-4 text-slate-300">
					📊 {selectedCoin} - 24 Saatlik Analiz
				</h3>
				<div className="w-full h-[90%]">
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
								itemStyle={{ color: "#3b82f6" }}
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

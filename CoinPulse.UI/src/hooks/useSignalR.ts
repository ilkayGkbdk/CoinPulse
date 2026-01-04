import { useEffect, useState } from 'react';
import * as signalR from '@microsoft/signalr';

// Gelecek verinin tipi (Interface sayesinde hata yapamayız!)
export interface PriceUpdate {
    symbol: string;
    price: number;
    dataTimestamp: string;
}

export const useSignalR = (hubUrl: string) => {
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [latestPrice, setLatestPrice] = useState<PriceUpdate | null>(null);

    useEffect(() => {
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl(hubUrl)
            .withAutomaticReconnect()
            .build();

        setConnection(newConnection);
    }, [hubUrl]);

    useEffect(() => {
        if (connection) {
            connection.start()
                .then(() => console.log('SignalR Bağlandı! 🟢'))
                .catch(err => console.error('SignalR Bağlantı Hatası:', err));

            // Backend'deki metod adı: "ReceivePriceUpdate"
            connection.on('ReceivePriceUpdate', (data: PriceUpdate) => {
                setLatestPrice(data);
            });
        }
    }, [connection]);

    return { latestPrice };
};
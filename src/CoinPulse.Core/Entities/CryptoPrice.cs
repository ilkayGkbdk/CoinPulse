using System;
using CoinPulse.Core.Common;

namespace CoinPulse.Core.Entities;

// Artık BaseEntity'den miras alıyor
public class CryptoPrice : BaseEntity
{
    public string Symbol { get; set; } = string.Empty;
    public decimal Price { get; set; }

    // Timestamp yerine BaseEntity'deki CreatedAt kullanılacak.
    // Ancak dışarıdan gelen verinin zamanını tutmak için ayrı bir alan tutabiliriz
    // veya direkt CreatedAt'i set edebiliriz.
    // Karışıklık olmaması için "DataTimestamp" diyelim (Verinin oluştuğu an).
    public DateTime DataTimestamp { get; set; }
}

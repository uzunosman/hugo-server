# Hugo Game Server

Bu proje, Hugo taş oyununun çevrimiçi çok oyunculu versiyonu için backend sunucusudur.

## Teknolojiler

- .NET 8
- ASP.NET Core
- SignalR (gerçek zamanlı iletişim)
- Redis (oyun durumu yönetimi)

## Proje Yapısı

```
hugo-server/
├── src/
│   ├── Hugo.API/            # Web API ve SignalR Hub'lar
│   ├── Hugo.Core/           # Domain modeller ve iş mantığı
│   └── Hugo.Infrastructure/ # Veritabanı, cache vb.
└── tests/                   # Unit ve Integration testler
```

## Kurulum

1. .NET 8 SDK'yı yükleyin
2. Projeyi klonlayın:
   ```bash
   git clone https://github.com/uzunosman/hugo-serveer.git
   ```
3. Proje dizinine gidin:
   ```bash
   cd hugo-serveer
   ```
4. Bağımlılıkları yükleyin:
   ```bash
   dotnet restore
   ```
5. Projeyi çalıştırın:
   ```bash
   dotnet run --project src/Hugo.API
   ```

## Oyun Kuralları

- 4 oyunculu bir oyun
- 106 taş (4 renk, her renkte 1-13 arası sayılar)
- Her sayıdan 8 taş (her renkte 2'şer adet)
- 2 adet özel joker taşı
- 9 tur oynanır (1., 5. ve 9. turlar Hugo turlarıdır)
- Minimum 51 değerinde per açma zorunluluğu
- Detaylı kurallar için [Oyun Kuralları](docs/GAME_RULES.md) dökümanına bakınız

## Katkıda Bulunma

1. Bu repository'yi fork edin
2. Feature branch'i oluşturun (`git checkout -b feature/amazing-feature`)
3. Değişikliklerinizi commit edin (`git commit -m 'feat: Add some amazing feature'`)
4. Branch'inizi push edin (`git push origin feature/amazing-feature`)
5. Pull Request oluşturun

## Lisans

Bu proje MIT lisansı altında lisanslanmıştır. Detaylar için [LICENSE](LICENSE) dosyasına bakınız. 
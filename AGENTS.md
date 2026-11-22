Sen kıdemli bir Unity 6 ve DOTS / ECS (Entities 1.x+) oyun mimarısın. Birlikte "Hades" tarzında, 3D karakterlere ve 2.5D izometrik bakış açısına sahip aksiyon roguelike bir oyun geliştireceğiz.

TEKNİK KARARLAR VE MİMARİ SINIRLAR:

1. **Unity Sürümü & Paketler:** Unity 6, `com.unity.entities`, `com.unity.entities.graphics`, `com.unity.physics` (3D çarpışma ve raycast için) ve `com.unity.inputsystem` kullanacağız.
2. **Görsel ve Hibrit Animasyon Köprüsü:** Oyun 3D izometrik (2.5D) olacak. Karakter animasyonları ve görselleri için standart `Animator` bileşenine sahip **Hibrit Companion GameObject** yaklaşımını kullanacağız. ECS tarafındaki durum/hareket verileri, Managed (ana iş parçacığı) bir köprü sistemi ile Companion GameObject üzerindeki `Animator` parametrelerine aktarılacak.
3. **Eğitici ve Şeffaf Yaklaşım:** Kodu sadece kopyala-yapıştır olarak verme. Her yeni sistem, component veya dünya (World) yapısında; bunun ECS'de neden o şekilde kurgulandığını, bellekte (Chunk/Archetype) nasıl davrandığını ve büyük resme nasıl oturduğunu detaylıca açıkla.
4. **Manuel Sistem Yönetimi:** Unity'nin otomatik sistem gruplandırması yerine, sistemlerin çalışma sırasını ve güncellemelerini (`[DisableAutoCreation]` ve özel `ComponentSystemGroup` / `World` bootstrap yapıları ile) manuel olarak kontrol edeceğiz. Hangi sistemin neden önce/sonra çalıştığını her adımda belirteceksin.
5. **Tam Modülerlik (Decoupled Architecture):** Her mekanik bağımsız bir modül (Assembly Definition / klasör yapısı) olarak tasarlanacak. Modüller birbirine sıkı sıkıya bağlı (tightly-coupled) olmayacak; aralarındaki iletişim ortak Veri Component'leri (Data Components), DynamicBuffer'lar veya Event/Tag Component'leri üzerinden sağlanacak.

İNŞA EDECEĞİMİZ MODÜL LİSTESİ (YOL HARİTASI):

1. **Çekirdek & Manuel Sistem Yöneticisi (Core & Bootstrap):** Custom World kurulumu, manuel sistem kayıt/güncelleme döngüsü, Oyun Durumları (Game State: Menu, InRun, Paused, GameOver).
2. **Yaşam Döngüsü & Fizik Temeli (Lifecycle & Physics):** Entity Spawn/Despawn (`EntityCommandBuffer`), 3D izometrik sınırlar/çarpışma (`Unity.Physics`), Can/Hasar/Ölüm döngüsü ve UI üzerinden ayarlanabilen oyunun genel zorluk seviyesi. Zorluğun nelere bağlı olacağını sen seç.
3. **Hibrit Görsel & Animasyon Köprüsü (Hybrid Visual & Animation Bridge):** ECS Entity pozisyon/rotasyon ve durumlarının (Idle, Run, Attack, Dash, Die) Companion GameObject `Animator`'üne senkronizasyonu ve Kamera takip köprüsü.
4. **Özellik/Stat Sistemi (Stats & Attributes):** Temel statlar (Can, Hız, Saldırı Gücü, Bekleme Süresi, Kritik vb.) ve `DynamicBuffer` tabanlı dinamik stat değiştiriciler (Modifiers/Buffs).
5. **Karakter Sınıfları (Character Classes):** Veri odaklı (ScriptableObject -> Baker) esnek sınıf altyapısı (Savaşçı, Okçu, Büyücü, Tetikçi). _İlk etapta sadece Savaşçı (Kılıç/Bıçak) implement edilecek ancak diğer sınıflar yeni kod yazmadan veri eklenerek sisteme dahil edilebilecek._
6. **Hareket Sistemi (Movement & Input):** Yeni Input System entegrasyonu, izometrik düzlemde hareket, fare/yön bakışı ve Dash (atılma) mekaniği.
7. **Envanter ve Silah Sistemi (Inventory & Weapons):** Silah türlerini (Kılıç/Bıçak, Pistol/AR/SMG/Sniper, Asa) destekleyen modüler silah/ekipman yapısı.
8. **Temel Saldırı & Mermi/Hitbox Sistemi (Basic Attack & Combat):** Silah ve sınıfa göre şekillenen kombo vuruşları, yakın dövüş hitbox'ları ve menzilli mermi (Projectile/Pooling) altyapısı.
9. **Yetenek Sistemi (Skill System):** Bekleme süreli (Cooldown) aktif yetenekler ve alan/hedef etkileri.
10. **Dinamik Rün / Boon Sistemi (Dynamic Rune System):** Hades'teki Boon'lar ve LoL rünleri mantığında; oyun/run içinde anlık olarak kazanılan, değiştirilebilen, karakterin statlarını veya saldırı/skill davranışlarını tetiklemeli (OnHit, OnDash, OnKill) değiştiren sistem.
11. **Yetenek Ağacı & Meta İlerleme (Skill Tree & Progression):** Hem run içi seviye/XP ilerlemesini hem de run dışı kalıcı düğüm (node) tabanlı gelişimi yöneten sistem.
12. **Görev & Oda Dalga Sistemi (Quest & Encounter System):** Oda temizleme, düşman dalgaları ve dinamik görev takibi.
13. **Veri Kalıcılığı & UI Köprüsü (Serialization & UI Bridge):** ECS verilerini (Skill Tree, açık karakterler, meta-progression, anlık run durumu) dışarı aktarıp diske JSON olarak kaydeden/yükleyen ve UI (Can barı, Rün seçim ekranı) ile haberleşen Managed köprü modülü.

ÇALIŞMA ŞEKLİMİZ:

- Adım adım ilerleyeceğiz. Ben "Başlayalım" dediğimde önce **Proje Klasör/Assembly Definition (asmdef) Mimarisi** ve **1. Modül (Çekirdek, World Kurulumu ve Manuel Sistem Güncelleme Altyapısı)** ile başlayacaksın.
- Her modül bittiğinde benden onay almadan bir sonrakine geçme. O modülü Unity Editor'de nasıl kurup test edeceğimi de adım adım anlat.

Eğer kuralları ve mimariyi anladıysan, sadece anladığını teyit et, projenin genel klasör/Assembly yapısını çiz ve 1. Modül ile başla.

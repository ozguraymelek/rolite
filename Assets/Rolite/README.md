# Rolite proje mimarisi

Bu dizin oyun kodunun tek giriş noktasıdır. `TutorialInfo`, örnek sahne ve URP
varlıkları Unity'nin oluşturduğu içeriktir; oyun kodu ile karıştırılmaz.

```text
Assets/Rolite
├── Shared/                       # Modüller arası saf veri sözleşmeleri
├── Modules/
│   ├── Core/                     # World bootstrap, oyun akışı ve sistem kök grupları
│   ├── Lifecycle/                # Spawn/despawn, yaşam/hasar ve fizik temeli
│   ├── HybridVisual/             # Companion GameObject, Animator ve kamera köprüsü
│   ├── Stats/                    # Statlar ve DynamicBuffer modifier'ları
│   ├── CharacterClasses/         # ScriptableObject → Baker sınıf verisi
│   ├── Movement/                 # Input, izometrik hareket, bakış ve dash
│   ├── Inventory/                # Ekipman ve silah envanteri
│   ├── Combat/                   # Hitbox, projectile, kombo ve hasar olayları
│   ├── Skills/                   # Aktif yetenekler ve cooldown
│   ├── Runes/                    # Run içi boon/rün davranışları
│   ├── Progression/              # XP, seviye ve kalıcı yetenek ağacı
│   ├── Encounter/                # Oda, dalga ve görev akışı
│   └── PersistenceUI/            # JSON kalıcılık ve managed UI köprüsü
├── Content/                      # Prefab, ScriptableObject, material ve adreslenebilir içerik
├── Scenes/                       # Oyun sahneleri (Unity'nin örnek sahnesinden ayrı)
└── Tests/                        # EditMode ve PlayMode testleri
```

Assembly bağımlılığı yalnızca aşağıdaki yöne gider:

```text
Unity paketleri ← Rolite.Shared ← Rolite.Core ← mekanik modülleri ← PersistenceUI / HybridVisual
```

Bir modül başka bir mekaniğin sistem sınıfını çağırmaz. İletişim, sahibinin
tanımladığı `IComponentData`, `IBufferElementData` veya tek-frame event buffer'ı
ile yapılır. Henüz kaynak kodu olmayan modüllerin asmdef'leri kasten bağımlılıksız
başlatıldı; ilgili modül uygulanırken yalnızca gereken referans eklenecek.

Her modülün veri sahipliği, event sınırları, planlanan sistem sırası ve Core akışının
chunk/query ayrıntıları için [ModuleArchitecture.md](Documentation/ModuleArchitecture.md) belgesine bakın.
Core'ta kullanılan ECS API'lerinin teknik ve başlangıç seviyesi açıklamaları için de
[CoreEcsPrimer.md](Documentation/CoreEcsPrimer.md) belgesine bakın.

// IComponentData ve IBufferElementData ECS'nin unmanaged veri sözleşmelerini sağlar.
using Unity.Entities;

namespace Rolite.Core
{
    /// <summary>
    /// Oyunun tek otoriter akış durumudur; yalnızca GameState singleton'ında bulunur.
    /// IComponentData seçimi, veriyi entity'nin archetype chunk'ında bitişik saklar ve Burst/job
    /// kodundan erişilebilir yapar. Karşılığı olan MonoBehaviour kolay Inspector erişimi sağlar,
    /// fakat GameObject başına managed bellek ve ana iş parçacığı bağımlılığı getirir.
    /// </summary>
    public struct GameState : IComponentData
    {
        // byte tabanlı enum, state'i 1 byte'ta saklar. Ayrı tag component'ler okunaklı query'ler
        // sağlayabilir; ancak her geçişte Add/Remove ile archetype değişimi ve chunk taşıması yaratır.
        public GameFlowState Value;

        // Geçiş sayacı, UI/dinleyicilerin aynı state'i tekrar işlemesini önleyen sürüm damgasıdır.
        // uint seçimi negatif olmayan, 4 byte'lık ve Burst uyumlu bir sayaç verir.
        public uint Version;
    }

    /// <summary>
    /// GameState.Value için kapalı değer kümesidir. `byte` taban türü, int varsayılanına göre
    /// singleton ve buffer elemanlarında daha az yer kaplar; en fazla 256 state sınırı getirir.
    /// </summary>
    public enum GameFlowState : byte
    {
        // Menüde oyun simülasyonu ilerlememelidir.
        Menu = 0,
        // Aktif roguelike koşusu; gameplay modülleri bu değeri filtreleyecektir.
        InRun = 1,
        // Koşu verisi korunur, zaman-temelli gameplay sistemleri durur.
        Paused = 2,
        // Run sonu; sonuç ve meta-ilerleme akışına geçiş noktasıdır.
        GameOver = 3
    }

    /// <summary>
    /// Durum değişimi isteyen modüllerin yazdığı kuyruk elemanıdır. IBufferElementData, değişken
    /// sayıda isteği singleton entity üzerinde tutar. Küçük buffer chunk içinde kalır; büyürse ECS
    /// heap'e taşır. Bu, her istek için entity üretmeye göre daha az yapısal maliyetlidir; karşılığı,
    /// producer/consumer sırasının açıkça tanımlanmasını gerektirmesidir.
    /// </summary>
    public struct GameStateTransitionRequest : IBufferElementData
    {
        // Hedef değer; kaynak bilgisi gerekirse ileride ayrı bir enum/FixedString alanı eklenir.
        public GameFlowState Target;
    }

    /// <summary>
    /// Bu frame'de gerçekleşen değişimin gözlemlenebilir kaydıdır. Ayrı event entity'leri dinleyici
    /// sayısı arttığında yararlı olabilir; burada tek singleton buffer tahsisi daha ucuzdur. Event
    /// buffer'ı sonraki frame başında temizlenir; kalıcı geçmiş gerekiyorsa başka bir buffer kullanılır.
    /// </summary>
    public struct GameStateTransitionEvent : IBufferElementData
    {
        // Geçiş öncesi state, tetikleyicilerin yönlü davranış kurmasını sağlar.
        public GameFlowState Previous;
        // Geçiş sonrası state, dinleyicinin güncel değeri tekrar okumadan karar vermesini sağlar.
        public GameFlowState Current;
        // GameState.Version kopyası, event'lerin sıra ve tekrar kontrolünde kullanılır.
        public uint Version;
    }
}

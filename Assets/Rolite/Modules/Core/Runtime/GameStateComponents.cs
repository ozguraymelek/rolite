using Unity.Entities;

namespace Rolite.Core
{
    /// <summary>
    /// Oyunun tek otoriter akış durumudur. Bu component yalnızca GameState singleton'ında bulunur.
    /// </summary>
    public struct GameState : IComponentData
    {
        public GameFlowState Value;
        public uint Version;
    }

    public enum GameFlowState : byte
    {
        Menu = 0,
        InRun = 1,
        Paused = 2,
        GameOver = 3
    }

    /// <summary>
    /// Durum değişimi isteyen sistemler hedefi bu buffer'a ekler. Çoklu üretici güvenliğini
    /// sonraki modüllerde ECB üzerinden sağlayacağız; tüketici bu modülde tektir.
    /// </summary>
    public struct GameStateTransitionRequest : IBufferElementData
    {
        public GameFlowState Target;
    }

    /// <summary>
    /// Sadece üretildiği frame'de geçerlidir. Sonraki frame'in başında temizlenir.
    /// Dinleyici modüller kendi sistemlerini GameStateSystemGroup sonrasına ekler.
    /// </summary>
    public struct GameStateTransitionEvent : IBufferElementData
    {
        public GameFlowState Previous;
        public GameFlowState Current;
        public uint Version;
    }
}

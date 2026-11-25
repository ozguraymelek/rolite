// ComponentSystemGroup ve DisableAutoCreation, sistem handle'larını elle sıralamak için kullanılır.
using Unity.Entities;

namespace Rolite.Core
{
    // Bu gruplar ISystem değildir: Entities'in PlayerLoop entegrasyonu ComponentSystemGroup için
    // managed bir kök nesne kullanır. İçlerine eklenen tüm frame sistemleri ISystem/SystemHandle'dır.
    // Avantajı güvenilir PlayerLoop entegrasyonu; dezavantajı yalnızca grup katmanında managed maliyettir.
    [DisableAutoCreation]
    public sealed partial class RogueliteRootSystemGroup : ComponentSystemGroup { }

    // Dünya kurulum sistemlerini ilk çalıştıran managed sıralama kabıdır.
    [DisableAutoCreation]
    public sealed partial class RogueliteInitializationSystemGroup : ComponentSystemGroup { }

    // Global oyun akışı event ve request sözleşmesinin sahibi olan sıralama kabıdır.
    [DisableAutoCreation]
    public sealed partial class GameStateSystemGroup : ComponentSystemGroup { }

    // Sonraki saf oyun mekaniği ISystem handle'ları burada çalışacaktır.
    [DisableAutoCreation]
    public sealed partial class RogueliteSimulationSystemGroup : ComponentSystemGroup { }

    // Animator/UI gibi zorunlu managed köprü sistemleri en son burada çalışacaktır.
    [DisableAutoCreation]
    public sealed partial class RoguelitePresentationSystemGroup : ComponentSystemGroup { }
}

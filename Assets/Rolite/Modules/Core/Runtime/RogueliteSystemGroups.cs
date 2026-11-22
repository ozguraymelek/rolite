using Unity.Entities;

namespace Rolite.Core
{
    // Bu grupların hiçbiri Unity tarafından otomatik oluşturulmaz veya sıralanmaz.
    [DisableAutoCreation]
    public sealed partial class RogueliteRootSystemGroup : ComponentSystemGroup { }

    [DisableAutoCreation]
    public sealed partial class RogueliteInitializationSystemGroup : ComponentSystemGroup { }

    [DisableAutoCreation]
    public sealed partial class GameStateSystemGroup : ComponentSystemGroup { }

    // Sonraki modüller sistemlerini Bootstrap içinde bu iki gruba açıkça ekleyecek.
    [DisableAutoCreation]
    public sealed partial class RogueliteSimulationSystemGroup : ComponentSystemGroup { }

    [DisableAutoCreation]
    public sealed partial class RoguelitePresentationSystemGroup : ComponentSystemGroup { }
}

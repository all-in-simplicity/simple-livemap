using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace LiveMap.Server.LiveMap;

public sealed class MapPlayer(string handle)
{
    public MapPlayer(Player player)
        : this(player.Handle)
    {
    }

    public string Name => API.GetPlayerName(handle);

    public Position Position => GetPlayerPedPosition();

    private bool DoesPlayerExist()
    {
        return API.DoesPlayerExist(handle) && API.DoesEntityExist(API.GetPlayerPed(handle));
    }

    private int GetPlayerPed()
    {
        return API.GetPlayerPed(handle);
    }

    private Vector3 GetPlayerPedCoords()
    {
        return DoesPlayerExist() ? API.GetEntityCoords(GetPlayerPed()) : default;
    }

    private Position GetPlayerPedPosition()
    {
        if (!DoesPlayerExist()) return default;

        var position = GetPlayerPedCoords();

        return new Position(position.X, position.Y);
    }
}
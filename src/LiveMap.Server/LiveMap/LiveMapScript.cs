using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Lib.AspNetCore.ServerSentEvents;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace LiveMap.Server.LiveMap;

public class LiveMapScript : ServerScript
{
    private static readonly JsonSerializerSettings SerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        Formatting = Formatting.None
    };

    private readonly IServerSentEventsService _serverSentEventsService;

    public LiveMapScript(IServerSentEventsService serverSentEventsService)
    {
        _serverSentEventsService = serverSentEventsService;

        Tick += OnTick;
    }

    public async Task OnTick()
    {
        if (_serverSentEventsService.GetClients().Count > 0)
        {
            var playerList = ServerMain.Self.Players.Where(p => API.DoesEntityExist(API.GetPlayerPed(p.Handle)))
                .ToList()
                .Select(player => new MapPlayer(player)).ToList();

            var serializedPlayerList =
                JsonConvert.SerializeObject(playerList, SerializerSettings);

            var serverEvent = new ServerSentEvent()
            {
                Type = "positions", Data = [serializedPlayerList]
            };

            await _serverSentEventsService.SendEventAsync(serverEvent);
        }

        await Delay((int)ServerMain.Self.Configuration.ServerSentEvents.TickInterval);
    }
}
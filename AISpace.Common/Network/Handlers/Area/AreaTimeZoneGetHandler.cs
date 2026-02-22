using AISpace.Common.Network.Packets;
using Microsoft.Extensions.Logging;
using System;

namespace AISpace.Common.Network.Handlers;

public class AreaTimeZoneGetHandler : IPacketHandler
{
    public PacketType RequestType => PacketType.TimeZoneGetRequest;
    public PacketType ResponseType => PacketType.TimeZoneGetResponse;
    public MessageDomain Domain => MessageDomain.Area;

    private readonly ILogger<AreaTimeZoneGetHandler> _logger;

    public AreaTimeZoneGetHandler(ILogger<AreaTimeZoneGetHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandleAsync(ReadOnlyMemory<byte> payload, ClientConnection connection, CancellationToken ct = default)
    {
        // 1. Берем текущее время сервера
        DateTime now = DateTime.Now;
        double totalSecondsToday = now.TimeOfDay.TotalSeconds;
        int hour = now.Hour;

        uint zone;
        uint currentTime;
        uint maxTime;

        // 2. Логика периодов на основе декомпилятора (sub_7188C0):
        // Зона 4: Early Morning (5:00 - 7:00)  -> Длительность 2ч
        // Зона 0: Morning (7:00 - 10:00)       -> Длительность 3ч
        // Зона 1: Day (10:00 - 17:00)           -> Длительность 7ч
        // Зона 2: Evening (17:00 - 20:00)       -> Длительность 3ч
        // Зона 3: Night (20:00 - 5:00 следующего дня) -> Длительность 9ч

        if (hour >= 5 && hour < 7)
        {
            zone = 4;
            currentTime = (uint)(totalSecondsToday - (5 * 3600));
            maxTime = 2 * 3600; 
        }
        else if (hour >= 7 && hour < 10)
        {
            zone = 0;
            currentTime = (uint)(totalSecondsToday - (7 * 3600));
            maxTime = 3 * 3600;
        }
        else if (hour >= 10 && hour < 17)
        {
            zone = 1;
            currentTime = (uint)(totalSecondsToday - (10 * 3600));
            maxTime = 7 * 3600;
        }
        else if (hour >= 17 && hour < 20)
        {
            zone = 2;
            currentTime = (uint)(totalSecondsToday - (17 * 3600));
            maxTime = 3 * 3600;
        }
        else // Ночь (с 20:00 до 05:00)
        {
            zone = 3;
            maxTime = 9 * 3600;
            if (hour >= 20)
                currentTime = (uint)(totalSecondsToday - (20 * 3600));
            else // Время после полуночи (00:00 - 05:00)
                currentTime = (uint)(totalSecondsToday + (4 * 3600)); // 4 часа - это разница между 20:00 и 24:00
        }

        // Логируем для отладки
        _logger.LogInformation($"[TIME SYNC] Server Time: {now:HH:mm:ss} | Zone: {zone} | Progress: {currentTime}/{maxTime}");

        // Отправляем пакет. Flag = 1 заставляет клиента принудительно обновить положение солнца под наши цифры
        var response = new TimeZoneGetResponse(0, zone, currentTime, maxTime, 1);
        await connection.SendAsync(ResponseType, response.ToBytes(), ct);
    }
}
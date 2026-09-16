using aisp.Common.DAL;
using aisp.Common.DAL.Entities;
using aisp.Common.DAL.Repositories;
using aisp.Common.Game;
using aisp.Common.Handlers.Msg;
using aisp.Common.Tests.Support;
using aisp.Network;
using aisp.Network.Data;
using aisp.Network.Packets.Msg;
using Microsoft.Extensions.Logging.Abstractions;

namespace aisp.Common.Tests;

public sealed class MailboxPersistenceTests
{
    [Fact]
    public async Task OfflineRecipient_ReceivesPersistedMailFromMailboxRequest()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionScope = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);
        await TestDb.SeedCharacterAsync(options, 2, ct);

        await using (var sendDb = new MainContext(options))
        {
            var sender = new CapturingPlayerSession
            {
                User = new User { Id = 1, Username = "alice" },
                UserId = 1,
                CharacterId = 1,
                Character = new Character { Id = 1, Name = "character-1" },
            };
            var post = new MailPostHandler(
                new CharacterRepository(sendDb, NullLogger<CharacterRepository>.Instance),
                new MailRepository(sendDb),
                new SharedState(),
                WordFilter.FromTerms([])
            );

            var response = await post.HandleAsync(
                new MailPostRequest(2, string.Empty, "offline subject", "offline body"),
                sender,
                ct
            );

            Assert.NotNull(response);
            Assert.Equal(0u, response.Result);
        }

        await using var receiveDb = new MainContext(options);
        var recipient = new CapturingPlayerSession
        {
            User = new User { Id = 2, Username = "bob" },
            UserId = 2,
            CharacterId = 2,
        };
        var mailbox = new MailBoxGetDataHandler(new MailRepository(receiveDb));
        await mailbox.HandleAsync(ReadOnlyMemory<byte>.Empty, recipient, ct);

        var packet = Assert.Single(recipient.Sent);
        Assert.Equal(PacketType.MailBoxGetDataResponse, packet.Type);
        var reader = new PacketReader(packet.Payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(1u, reader.ReadUInt());
        var message = MailData.Read(ref reader);
        Assert.Equal(0u, message.Type);
        Assert.Equal(1u, message.SenderId);
        Assert.Equal(2u, message.DistId);
        Assert.Equal("offline subject", message.Subject);
        Assert.Equal("offline body", message.Body);
    }

    [Fact]
    public async Task MailboxRequest_ReturnsNewest350Messages()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionScope = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);
        await TestDb.SeedCharacterAsync(options, 2, ct);

        await using (var seedDb = new MainContext(options))
        {
            var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            seedDb.MailMessages.AddRange(
                Enumerable
                    .Range(1, 351)
                    .Select(i => new MailMessage
                    {
                        Id = i,
                        SenderCharacterId = 1,
                        SenderName = "character-1",
                        DestinationType = MailDestinationType.Character,
                        DestinationId = 2,
                        DestinationName = "character-2",
                        Subject = $"subject-{i}",
                        Body = $"body-{i}",
                        CreatedAtUtc = start.AddMinutes(i),
                        Recipients = [new MailRecipient { CharacterId = 2 }],
                    })
            );
            await seedDb.SaveChangesAsync(ct);
        }

        await using var requestDb = new MainContext(options);
        var session = new CapturingPlayerSession
        {
            User = new User { Id = 2, Username = "bob" },
            CharacterId = 2,
        };
        var handler = new MailBoxGetDataHandler(new MailRepository(requestDb));
        await handler.HandleAsync(ReadOnlyMemory<byte>.Empty, session, ct);

        var payload = Assert.Single(session.Sent).Payload;
        Assert.Equal(8 + MailRepository.MaxPageSize * MailData.WireSize, payload.Length);
        var reader = new PacketReader(payload);
        Assert.Equal(0u, reader.ReadUInt());
        Assert.Equal(350u, reader.ReadUInt());
        var newest = MailData.Read(ref reader);
        for (var i = 1; i < 349; i++)
            _ = MailData.Read(ref reader);
        var oldestIncluded = MailData.Read(ref reader);
        Assert.Equal("subject-351", newest.Subject);
        Assert.Equal("subject-2", oldestIncluded.Subject);
    }

    [Fact]
    public async Task MailboxRequest_PrioritizesProtectedMailBeforeFillingRemainingSlots()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionScope = connection;
        var ct = TestContext.Current.CancellationToken;

        await using var db = new MainContext(options);
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        db.MailMessages.Add(
            new MailMessage
            {
                Id = 1,
                SenderCharacterId = 1,
                SenderName = "sender",
                DestinationType = MailDestinationType.Character,
                DestinationId = 2,
                DestinationName = "recipient",
                Subject = "old-protected",
                Body = "body",
                CreatedAtUtc = start,
                Recipients = [new MailRecipient { CharacterId = 2, IsProtected = true }],
            }
        );
        db.MailMessages.AddRange(
            Enumerable
                .Range(2, 350)
                .Select(i => new MailMessage
                {
                    Id = i,
                    SenderCharacterId = 1,
                    SenderName = "sender",
                    DestinationType = MailDestinationType.Character,
                    DestinationId = 2,
                    DestinationName = "recipient",
                    Subject = $"normal-{i}",
                    Body = "body",
                    CreatedAtUtc = start.AddMinutes(i),
                    Recipients = [new MailRecipient { CharacterId = 2 }],
                })
        );
        await db.SaveChangesAsync(ct);

        var messages = await new MailRepository(db).ListRecentAsync(2, 350, ct);

        Assert.Equal(350, messages.Count);
        Assert.Equal("old-protected", messages[0].Message.Subject);
        Assert.Equal(3u, messages[0].Type);
        Assert.Contains(messages, x => x.Message.Subject == "normal-351");
        Assert.DoesNotContain(messages, x => x.Message.Subject == "normal-2");
    }

    [Fact]
    public async Task OpenProtectCancelAndDelete_PersistForTheOwningMailbox()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionScope = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);
        await TestDb.SeedCharacterAsync(options, 2, ct);

        await using var db = new MainContext(options);
        var repository = new MailRepository(db);
        var stored = await repository.AddAsync(
            new MailMessage
            {
                SenderCharacterId = 1,
                SenderName = "character-1",
                DestinationType = MailDestinationType.Character,
                DestinationId = 2,
                DestinationName = "character-2",
                Subject = "subject",
                Body = "body",
                Recipients = [new MailRecipient { CharacterId = 2 }],
            },
            ct
        );
        var recipient = new CapturingPlayerSession
        {
            User = new User { Id = 2, Username = "recipient" },
            CharacterId = 2,
        };
        var intruder = new CapturingPlayerSession
        {
            User = new User { Id = 99, Username = "intruder" },
            CharacterId = 99,
        };

        var rejectedDelete = await new MailDeleteHandler(repository).HandleAsync(
            new MailDeleteRequest((ulong)stored.Id, 0),
            intruder,
            ct
        );
        Assert.Equal(1u, ReadResult(rejectedDelete!.ToBytes()));
        var rejectedProtect = await new MailProtectHandler(repository).HandleAsync(
            new MailProtectRequest((ulong)stored.Id),
            intruder,
            ct
        );
        Assert.Equal(1u, ReadResult(rejectedProtect!.ToBytes()));

        var opened = await new MailOpenHandler(repository).HandleAsync(
            new MailOpenRequest((ulong)stored.Id, 0),
            recipient,
            ct
        );
        Assert.Equal(0u, opened!.Result);
        var afterOpen = Assert.Single(await repository.ListRecentAsync(2, 350, ct));
        Assert.True(afterOpen.IsRead);

        var protectedResponse = await new MailProtectHandler(repository).HandleAsync(
            new MailProtectRequest((ulong)stored.Id),
            recipient,
            ct
        );
        Assert.Equal(0u, ReadResult(protectedResponse!.ToBytes()));
        Assert.Equal(3u, Assert.Single(await repository.ListRecentAsync(2, 350, ct)).Type);

        var unprotectedResponse = await new MailProtectCancelHandler(repository).HandleAsync(
            new MailProtectCancelRequest((ulong)stored.Id),
            recipient,
            ct
        );
        Assert.Equal(0u, ReadResult(unprotectedResponse!.ToBytes()));
        Assert.Equal(0u, Assert.Single(await repository.ListRecentAsync(2, 350, ct)).Type);

        var recipientDelete = await new MailDeleteHandler(repository).HandleAsync(
            new MailDeleteRequest((ulong)stored.Id, 0),
            recipient,
            ct
        );
        Assert.Equal(0u, ReadResult(recipientDelete!.ToBytes()));
        Assert.Empty(await repository.ListRecentAsync(2, 350, ct));
        Assert.Single(await repository.ListRecentAsync(1, 350, ct));

        var sender = new CapturingPlayerSession
        {
            User = new User { Id = 1, Username = "sender" },
            CharacterId = 1,
        };
        var senderDelete = await new MailDeleteHandler(repository).HandleAsync(
            new MailDeleteRequest((ulong)stored.Id, 1),
            sender,
            ct
        );
        Assert.Equal(0u, ReadResult(senderDelete!.ToBytes()));
        Assert.Empty(await repository.ListRecentAsync(1, 350, ct));
    }

    [Fact]
    public async Task CircleMail_IsPersistedForEveryOfflineMember()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionScope = connection;
        var ct = TestContext.Current.CancellationToken;
        await TestDb.SeedCharacterAsync(options, 1, ct);
        await TestDb.SeedCharacterAsync(options, 2, ct);
        await TestDb.SeedCharacterAsync(options, 3, ct);

        await using var db = new MainContext(options);
        var circles = new CircleRepository(db);
        var created = await circles.CreateAsync(1, "Circle One", 0, ct);
        Assert.Equal(CircleResult.Ok, created.Result);
        var circleId = created.Circle!.Id;
        Assert.Equal(
            CircleResult.Ok,
            (await circles.EnsureMemberDirectAsync(circleId, 2, ct: ct)).Result
        );
        Assert.Equal(
            CircleResult.Ok,
            (await circles.EnsureMemberDirectAsync(circleId, 3, ct: ct)).Result
        );

        var repository = new MailRepository(db);
        var sender = new CapturingPlayerSession
        {
            User = new User { Id = 1, Username = "sender" },
            CharacterId = 1,
            Character = new Character { Id = 1, Name = "character-1" },
        };
        var response = await new MailPostHandler(
            new CharacterRepository(db, NullLogger<CharacterRepository>.Instance),
            repository,
            new SharedState(),
            WordFilter.FromTerms([]),
            circles
        ).HandleAsync(
            new MailPostRequest((uint)circleId, "Circle One", "circle subject", "circle body"),
            sender,
            ct
        );

        Assert.Equal(0u, response!.Result);
        Assert.Equal(1u, response.Mail.Type);
        Assert.Equal(2u, Assert.Single(await repository.ListRecentAsync(2, 350, ct)).Type);
        Assert.Equal(2u, Assert.Single(await repository.ListRecentAsync(3, 350, ct)).Type);
        Assert.Single(await repository.ListRecentAsync(1, 350, ct));
    }

    [Fact]
    public async Task CancelProtection_MovesPersistedCircleMailToNormalInbox()
    {
        var (connection, options) = TestDb.CreateInMemoryMainContext();
        await using var connectionScope = connection;
        var ct = TestContext.Current.CancellationToken;

        await using var db = new MainContext(options);
        var repository = new MailRepository(db);
        var stored = await repository.AddAsync(
            new MailMessage
            {
                SenderCharacterId = 1,
                SenderName = "sender",
                DestinationType = MailDestinationType.Circle,
                DestinationId = 10,
                DestinationName = "circle",
                Subject = "subject",
                Body = "body",
                Recipients = [new MailRecipient { CharacterId = 2, InboxType = 2 }],
            },
            ct
        );

        Assert.Equal(2u, Assert.Single(await repository.ListRecentAsync(2, 350, ct)).Type);
        Assert.True(await repository.SetProtectedAsync(2, stored.Id, true, ct));
        Assert.Equal(3u, Assert.Single(await repository.ListRecentAsync(2, 350, ct)).Type);
        Assert.True(await repository.SetProtectedAsync(2, stored.Id, false, ct));
        Assert.Equal(0u, Assert.Single(await repository.ListRecentAsync(2, 350, ct)).Type);
    }

    private static uint ReadResult(byte[] payload)
    {
        var reader = new PacketReader(payload);
        return reader.ReadUInt();
    }
}

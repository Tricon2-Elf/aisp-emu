namespace aisp.Common.Localisation;

public static class L
{
    public static class Item
    {
        public static LocKey Name(int itemId) => new($"item.{itemId}.name");

        public static LocKey Description(int itemId) => new($"item.{itemId}.description");

        public static LocKey LimitDescription(int itemId) =>
            new($"item.{itemId}.limit_description");

        public static readonly LocKey NoDescription = new("item.no_description");
        public static readonly LocKey NoLimitDescription = new("item.no_limit_description");
    }

    public static class Npc
    {
        public static LocKey Name(long npcObjectId) => new($"npc.{npcObjectId}.name");
    }

    public static class Shop
    {
        public static LocKey DisplayName(string code) => new($"shop.{code}.display_name");
    }

    public static class Map
    {
        public static LocKey Name(long mapId) => new($"map.{mapId}.name");

        public static LocKey Island(long mapId) => new($"map.{mapId}.island");

        public static readonly LocKey IslandTitleFormat = new("map.island_title_format");
        public static readonly LocKey IslandFallbackFormat = new("map.island_fallback_format");
        public static readonly LocKey NoCurrentMap = new("map.no_current_map");
        public static readonly LocKey UnknownMapFormat = new("map.unknown_format");
    }

    public static class Island
    {
        public static LocKey Name(uint islandId) => new($"island.{islandId}.name");

        public static readonly LocKey NotSelected = new("island.not_selected");
        public static readonly LocKey UnknownFormat = new("island.unknown_format");
    }

    public static class World
    {
        public static LocKey Name(string code) => new($"world.{code}.name");

        public static LocKey Description(string code) => new($"world.{code}.description");
    }

    public static class Emotion
    {
        public static LocKey Name(uint id) => new($"emotion.{id}.name");

        public static readonly LocKey GameFormat = new("emotion.game_format");
        public static readonly LocKey WaitFormat = new("emotion.wait_format");
        public static readonly LocKey VoiceFormat = new("emotion.voice_format");
    }

    public static class System
    {
        public static readonly LocKey Name = new("system.name");
    }

    public static class Maintenance
    {
        public static readonly LocKey Warning = new("maintenance.warning");
        public static readonly LocKey WarningOneMinute = new("maintenance.warning_one_minute");
        public static readonly LocKey Shutdown = new("maintenance.shutdown");
    }

    public static class Enquete
    {
        public static readonly LocKey MusicQuestion = new("enquete.music.question");
        public static readonly LocKey MusicAnswer0 = new("enquete.music.answer.0");
        public static readonly LocKey MusicAnswer1 = new("enquete.music.answer.1");
        public static readonly LocKey MusicAnswer2 = new("enquete.music.answer.2");
        public static readonly LocKey MusicAnswer3 = new("enquete.music.answer.3");
    }

    public static class Cmd
    {
        public static readonly LocKey InvalidRoomId = new("cmd.room.invalid_id");
        public static readonly LocKey RoomNotFound = new("cmd.room.not_found");
        public static readonly LocKey RoomPrivate = new("cmd.room.private");
        public static readonly LocKey RoomDenied = new("cmd.room.denied");
        public static readonly LocKey RoomSetNotOwned = new("cmd.room.set_not_owned");
        public static readonly LocKey RoomSetSuccess = new("cmd.room.set_success");
        public static readonly LocKey RoomListEmpty = new("cmd.room.list_empty");
        public static readonly LocKey RoomListHeader = new("cmd.room.list_header");
        public static readonly LocKey RoomListEntry = new("cmd.room.list_entry");
        public static readonly LocKey RoomListDefault = new("cmd.room.list_default");
        public static readonly LocKey RoomRemoveNotOwned = new("cmd.room.remove_not_owned");
        public static readonly LocKey RoomRemoveDefault = new("cmd.room.remove_default");
        public static readonly LocKey RoomRemoveNotEmpty = new("cmd.room.remove_not_empty");
        public static readonly LocKey RoomRemoveCurrent = new("cmd.room.remove_current");
        public static readonly LocKey RoomRemoveSuccess = new("cmd.room.remove_success");
        public static readonly LocKey KickUsage = new("cmd.kick.usage");
        public static readonly LocKey BanUsage = new("cmd.ban.usage");
        public static readonly LocKey ModUsage = new("cmd.mod.usage");
        public static readonly LocKey UnmodUsage = new("cmd.unmod.usage");
        public static readonly LocKey KickSuccess = new("cmd.kick.success");
        public static readonly LocKey BanSuccess = new("cmd.ban.success");
        public static readonly LocKey BanSuccessPermanent = new("cmd.ban.success_permanent");
        public static readonly LocKey ModSuccess = new("cmd.mod.success");
        public static readonly LocKey UnmodSuccess = new("cmd.unmod.success");
        public static readonly LocKey TargetNotFound = new("cmd.moderation.target_not_found");
        public static readonly LocKey PermissionDenied = new("cmd.moderation.permission_denied");
        public static readonly LocKey CannotTargetSelf = new("cmd.moderation.cannot_target_self");
        public static readonly LocKey AlreadyModerator = new("cmd.moderation.already_moderator");
        public static readonly LocKey NotModerator = new("cmd.moderation.not_moderator");
        public static readonly LocKey InvalidBanDuration = new("cmd.moderation.invalid_ban_duration");
        public static readonly LocKey ModerationFailed = new("cmd.moderation.failed");
    }

    public static class Equipment
    {
        public static LocKey Slot(byte slotIndex) => new($"equipment.slot.{slotIndex}");

        public static readonly LocKey Accessory = new("equipment.slot.accessory");
        public static readonly LocKey UnknownItemFormat = new("equipment.unknown_item_format");
    }

    public static class Charadoll
    {
        public static readonly LocKey PersonalityActive = new("charadoll.personality.active");
        public static readonly LocKey PersonalityQuiet = new("charadoll.personality.quiet");
        public static readonly LocKey PersonalityNone = new("charadoll.personality.none");
    }

    public static class Script
    {
        public static class Shinju
        {
            public static readonly LocKey Help = new("script.shinju.help");
            public static readonly LocKey Welcome = new("script.shinju.welcome");
            public static readonly LocKey CharadollQuestion = new(
                "script.shinju.charadoll.question"
            );
            public static readonly LocKey CharadollPrompt = new("script.shinju.charadoll.prompt");
            public static readonly LocKey CharadollActive = new(
                "script.shinju.charadoll.option.active"
            );
            public static readonly LocKey CharadollQuiet = new(
                "script.shinju.charadoll.option.quiet"
            );
            public static readonly LocKey CharadollNone = new(
                "script.shinju.charadoll.option.none"
            );
        }

        public static class StationStaff
        {
            public static readonly LocKey RegisterFirst = new(
                "script.station_staff.register_first"
            );
            public static readonly LocKey ChooseIsland = new("script.station_staff.choose_island");
            public static readonly LocKey ReturnToAkihabara = new(
                "script.station_staff.return_akihabara"
            );
        }

        public static class Introduction
        {
            public static readonly LocKey ChineseCosplayerHello = new(
                "script.introduction.chinese_cosplayer.hello"
            );
        }

        public static class MyRoom
        {
            public static readonly LocKey DoorGoOutside = new("script.myroom.door.go_outside");
            public static readonly LocKey DoorGoOtherRoom = new("script.myroom.door.go_other_room");
            public static readonly LocKey DoorStayIn = new("script.myroom.door.stay_in");
            public static readonly LocKey DoorLeavePrompt = new("script.myroom.door.leave_prompt");
            public static readonly LocKey WardrobeUse = new("script.myroom.wardrobe.use");
            public static readonly LocKey WardrobeSkip = new("script.myroom.wardrobe.skip");
            public static readonly LocKey WardrobePrompt = new("script.myroom.wardrobe.prompt");
        }
    }
}

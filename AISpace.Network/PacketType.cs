namespace AISpace.Network;

public enum PacketServerType
{
    Unknown,
    Msg,
    Auth,
    Area,
}

public enum PacketDirection
{
    Unknown,
    ServerToClient,
    ClientToServer,
}

public enum ImplementationState
{
    NotImplemented,
    Implemented,
}

[AttributeUsage(AttributeTargets.All)]
public class PacketMetadata(PacketServerType PacketServerType, PacketDirection direction, string decompiledName, ImplementationState implementationState) : Attribute
{
    public PacketServerType Server { get; } = PacketServerType;
    public PacketDirection Direction { get; set; } = direction;
    public string DecompiledName { get; } = decompiledName;
    public ImplementationState State { get; } = implementationState;
}

public enum PacketType : ushort
{
    [PacketMetadata(PacketServerType.Auth, PacketDirection.ClientToServer, "send_check_version", ImplementationState.Implemented)]
    VersionCheckRequest = 0x62BC,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ServerToClient, "recv_check_version_r", ImplementationState.Implemented)]
    VersionCheckResponse = 0xB6B4,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ClientToServer, "send_get_worldlist", ImplementationState.Implemented)]
    WorldListRequest = 0x6676,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ServerToClient, "recv_get_worldlist_r", ImplementationState.Implemented)]
    WorldListResponse = 0xEE7E,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ClientToServer, "send_select_world", ImplementationState.Implemented)]
    WorldSelectRequest = 0x7FE7,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ServerToClient, "recv_select_world_r", ImplementationState.Implemented)]
    WorldSelectResponse = 0x3491,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ClientToServer, "send_authenticate", ImplementationState.Implemented)]
    AuthenticateRequest = 0xF24B,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ServerToClient, "recv_authenticate_r", ImplementationState.Implemented)]
    AuthenticateResponse = 0xD4AB,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ServerToClient, "recv_authenticate_r_failure", ImplementationState.Implemented)]
    AuthenticateFailureResponse = 0xD845,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_avatar_create", ImplementationState.Implemented)]
    AvatarCreateRequest = 0x29A4,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_avatar_create_r", ImplementationState.Implemented)]
    AvatarCreateResponse = 0x788F,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_avatar_data", ImplementationState.Implemented)]
    AvatarDataResponse = 0x6747,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_avatar_destroy", ImplementationState.Implemented)]
    AvatarDestroyRequest = 0x765A,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_avatar_destroy_r", ImplementationState.Implemented)]
    AvatarDestroyResponse = 0x6587,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_enquete", ImplementationState.Implemented)]
    EnqueteGetRequest = 0xC578,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_enquete_r", ImplementationState.Implemented)]
    EnqueteGetResponse = 0x24EE,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_enquete_answer", ImplementationState.Implemented)]
    EnqueteAnswerRequest = 0x0352,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_enquete_answer_r", ImplementationState.Implemented)]
    EnqueteAnswerResponse = 0x615A,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_login", ImplementationState.Implemented)]
    LoginRequest = 0x34EF,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_login_r", ImplementationState.Implemented)]
    LoginResponse = 0x1FEA,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_logout", ImplementationState.Implemented)]
    LogoutRequest = 0x0AD0,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_logout_r", ImplementationState.Implemented)]
    LogoutResponse = 0xB7B9,

    [PacketMetadata(PacketServerType.Auth, PacketDirection.ServerToClient, "recv_notify_logout", ImplementationState.Implemented)]
    LogoutNotify = 0x2D66,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_adventure_upload_rate", ImplementationState.NotImplemented)]
    AdventureUploadRateGetRequest = 0x71CF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_adventure_upload_rate_r", ImplementationState.Implemented)]
    AdventureUploadRateGetResponse = 0x9061,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_ai_download_list", ImplementationState.Implemented)]
    AiDownloadListGetRequest = 0x1D3F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_ai_download_list_r", ImplementationState.Implemented)]
    AiDownloadListGetResponse = 0xBEE1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_ai_upload_rate", ImplementationState.Implemented)]
    AiUploadRateGetRequest = 0xE30D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_ai_upload_rate_r", ImplementationState.Implemented)]
    AiUploadRateGetResponse = 0xB2BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_enter_areasv", ImplementationState.Implemented)]
    AreasvEnterRequest = 0x4646,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_enter_areasv_r", ImplementationState.Implemented)]
    AreasvEnterResponse = 0x0149,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_leave_areasv", ImplementationState.Implemented)]
    AreasvLeaveRequest = 0xF7B9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_leave_areasv_r", ImplementationState.Implemented)]
    AreasvLeaveResponse = 0xE31D,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_avatar_create_info", ImplementationState.Implemented)]
    AvatarGetCreateInfoRequest = 0x04F6,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_avatar_create_info_r", ImplementationState.Implemented)]
    AvatarGetCreateInfoResponse = 0xA5AD,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_avatar_data", ImplementationState.Implemented)]
    AvatarGetDataRequest = 0xAD9E,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_avatar_data_r", ImplementationState.Implemented)]
    AvatarGetDataResponse = 0xB055,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_avatar_data", ImplementationState.Implemented)]
    AvatarNotifyData = 0x7D78,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_move_chara", ImplementationState.Implemented)]
    AvatarNotifyMove = 0xAADB,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_select_avatar", ImplementationState.Implemented)]
    AvatarSelectRequest = 0x113D,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_select_avatar_r", ImplementationState.Implemented)]
    AvatarSelectResponse = 0x2C5F,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_channellist", ImplementationState.Implemented)]
    ChannelListGetRequest = 0x0300,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_channellist_r", ImplementationState.Implemented)]
    ChannelListGetResponse = 0xF27F,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_select_channel", ImplementationState.Implemented)]
    ChannelSelectRequest = 0xFFE1,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_select_channel_r", ImplementationState.Implemented)]
    ChannelSelectResponse = 0xFFEA,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_select_channel_r_myroom", ImplementationState.Implemented)]
    ChannelSelectMyRoomResponse = 0x1A5C,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_change_core_authority", ImplementationState.NotImplemented)]
    CircleChangeCoreAuthorityRequest = 0x05ED,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_change_core_authority_r", ImplementationState.NotImplemented)]
    CircleChangeCoreAuthorityResponse = 0xC097,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_chat_in", ImplementationState.Implemented)]
    CircleChatInRequest = 0x9514,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_in_r", ImplementationState.Implemented)]
    CircleChatInResponse = 0x81C6,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_chat_out", ImplementationState.Implemented)]
    CircleChatOutRequest = 0x05E5,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_chat_post", ImplementationState.Implemented)]
    CircleChatPostRequest = 0x3D7F,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_post_r", ImplementationState.Implemented)]
    CircleChatPostResponse = 0xA9C1,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_create_new_circle", ImplementationState.Implemented)]
    CircleCreateRequest = 0x1048,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_circle_data", ImplementationState.Implemented)]
    CircleGetDataRequest = 0xDB5F,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_circle_data_r", ImplementationState.Implemented)]
    CircleGetDataResponse = 0x90AD,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_leader_change_r", ImplementationState.NotImplemented)]
    CircleLeaderChangeResponse = 0xBB59,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_leader_change", ImplementationState.NotImplemented)]
    CircleNotifyLeaderChange = 0x239B,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_mark_change", ImplementationState.NotImplemented)]
    CircleMarkChangeRequest = 0xD895,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_mark_change_r", ImplementationState.NotImplemented)]
    CircleMarkChangeResponse = 0xD0EF,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_mark_change", ImplementationState.NotImplemented)]
    CircleNotifyMarkChange = 0x6719,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_request_join_answer", ImplementationState.NotImplemented)]
    CircleMemberJoinAnswerRequest = 0x1B70,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_request_join_member_cancel", ImplementationState.NotImplemented)]
    CircleMemberJoinMemberCancelRequest = 0x83C1,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_request_join_member_cancel_r", ImplementationState.NotImplemented)]
    CircleMemberJoinMemberCancelResponse = 0x2BF6,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_request_join_member", ImplementationState.NotImplemented)]
    CircleMemberJoinMemberRequest = 0xAB2D,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_request_join_member_r", ImplementationState.NotImplemented)]
    CircleMemberJoinMemberResponse = 0xDC3A,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_member_kick", ImplementationState.NotImplemented)]
    CircleMemberKickRequest = 0xBF32,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_member_kick_r", ImplementationState.NotImplemented)]
    CircleMemberKickResponse = 0x5559,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_message_change", ImplementationState.NotImplemented)]
    CircleMessageChangeRequest = 0x2D2B,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_message_change_r", ImplementationState.NotImplemented)]
    CircleMessageChangeResponse = 0x59EB,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_chat_out", ImplementationState.NotImplemented)]
    CircleNotiftyChatOut = 0xBBC4,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_chat_in", ImplementationState.NotImplemented)]
    CircleNotifyChatIn = 0xCBFA,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_request_join", ImplementationState.NotImplemented)]
    CircleNotifyJoinRequest = 0x9888,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_request_join_result", ImplementationState.NotImplemented)]
    CircleNotifyJoinRequestResult = 0x8FED,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_add_member", ImplementationState.NotImplemented)]
    CircleNotifyAddMember = 0x0E08,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_kick", ImplementationState.NotImplemented)]
    CircleNotifyKick = 0x27DF,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_member_login", ImplementationState.NotImplemented)]
    CircleNotifyMemberLogin = 0x3946,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_member_logout", ImplementationState.NotImplemented)]
    CircleNotifyMemberLogout = 0x1391,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_member", ImplementationState.Implemented)]
    CircleNotifyMember = 0xBF0E,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_message_change", ImplementationState.NotImplemented)]
    CircleNotifyMessageChange = 0x215B,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_change_core_authority", ImplementationState.NotImplemented)]
    CircleNotifyChangeCoreAuthority = 0x7E50,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_circle_resign_member", ImplementationState.NotImplemented)]
    CircleNotifyResignMember = 0x77F1,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_resign_circle", ImplementationState.NotImplemented)]
    CircleResignRequest = 0x7382,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_resign_circle_r", ImplementationState.NotImplemented)]
    CircleResignResponse = 0x16AF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_emotion_base_list", ImplementationState.Implemented)]
    EmotionGetBaseListRequest = 0x7FCD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_emotion_base_list_r", ImplementationState.Implemented)]
    EmotionGetBaseListResponse = 0x28E3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_obtained_emotion_list", ImplementationState.Implemented)]
    EmotionGetObtainedListRequest = 0xFD42,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_obtained_emotion_list_r", ImplementationState.Implemented)]
    EmotionGetObtainedListResponse = 0xC3D7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_emotion_chara", ImplementationState.Implemented)]
    EmotionCharaRequest = 0xCB64,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_emotion_chara_r", ImplementationState.Implemented)]
    EmotionCharaResponse = 0x1CC7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_emotion_chara", ImplementationState.Implemented)]
    NotifyEmotionChara = 0x67B5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_equip_order_list", ImplementationState.Implemented)]
    EquipOrderListRequest = 0xF74C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_equip_order_list_r", ImplementationState.Implemented)]
    EquipOrderListResponse = 0x2DAE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_my_avatar_myprofile_data", ImplementationState.Implemented)]
    GetMyAvatarMyprofileDataRequest = 0x3915,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_my_avatar_myprofile_data_r", ImplementationState.Implemented)]
    GetMyAvatarMyprofileDataResponse = 0xDDEE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_friend_list_data", ImplementationState.Implemented)]
    FriendGetListDataRequest = 0x805F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_friend_list_data_r", ImplementationState.Implemented)]
    FriendGetListDataResponse = 0x2411,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_free_friend_link_tag", ImplementationState.NotImplemented)]
    FriendLinkTagGetFreeRequest = 0xC88F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_friend_link_tag_data", ImplementationState.NotImplemented)]
    FriendLinkTagGetRequest = 0x0F97,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_friend_link_tag_data_r", ImplementationState.Implemented)]
    FriendLinkTagGetResponse = 0x239E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_furniture_base_list", ImplementationState.Implemented)]
    FurnitureGetBaseListRequest = 0x2FDA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_furniture_base_list_r", ImplementationState.Implemented)]
    FurnitureGetBaseListResponse = 0xA0D1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_heroine_ticket_get_base", ImplementationState.Implemented)]
    HeroineGetTicketBaseRequest = 0x25CE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_heroine_ticket_get_base_r", ImplementationState.Implemented)]
    HeroineGetTicketBaseResponse = 0x16E6,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_item_base_list", ImplementationState.Implemented)]
    ItemGetBaseListRequest = 0xC8EA,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_item_base_list_r", ImplementationState.Implemented)]
    ItemGetBaseListResponse = 0xC7A9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_item_list", ImplementationState.Implemented)]
    ItemGetListRequest = 0x2A9A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_item_list_r", ImplementationState.Implemented)]
    ItemGetListResponse = 0xA522,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_create", ImplementationState.NotImplemented)]
    ItemCreateNotify = 0x6ACB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_update_list", ImplementationState.NotImplemented)]
    ItemUpdateListNotify = 0x084E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_itembox_item_list_r", ImplementationState.NotImplemented)]
    ItemboxGetItemListResponse = 0xA137,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_itembox_item_create", ImplementationState.NotImplemented)]
    ItemboxItemCreateNotify = 0x8782,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_item_equip_start", ImplementationState.Implemented)]
    ItemEquipStartRequest = 0x3768,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_start_r", ImplementationState.Implemented)]
    ItemEquipStartResponse = 0x6448,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_started", ImplementationState.Implemented)]
    ItemEquipStarted = 0x0F60,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_force_started", ImplementationState.Implemented)]
    ItemEquipForceStarted = 0x7E82,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_item_try_equip_fix", ImplementationState.Implemented)]
    ItemTryEquipFixRequest = 0x3CDE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_fix_r", ImplementationState.Implemented)]
    ItemTryEquipFixResponse = 0x8D54,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_item_try_equip_reset", ImplementationState.Implemented)]
    ItemTryEquipResetRequest = 0x9703,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_reset_r", ImplementationState.Implemented)]
    ItemTryEquipResetResponse = 0xA87A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_item_try_equip_replace", ImplementationState.Implemented)]
    ItemTryEquipReplaceRequest = 0x0083,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equipped", ImplementationState.Implemented)]
    ItemTryEquipped = 0xBB7C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_item_equip_end", ImplementationState.Implemented)]
    ItemEquipEndRequest = 0x1CC2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_end_r", ImplementationState.Implemented)]
    ItemEquipEndResponse = 0xDF80,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_ended", ImplementationState.Implemented)]
    ItemEquipEnded = 0xB4A8,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_mail_box_data", ImplementationState.Implemented)]
    MailBoxGetDataRequest = 0x8D92,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_mail_box_data_r", ImplementationState.Implemented)]
    MailBoxGetDataResponse = 0x147A,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_delete_mail", ImplementationState.NotImplemented)]
    MailDeleteRequest = 0xF96D,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_delete_mail_r", ImplementationState.NotImplemented)]
    MailDeleteResponse = 0xE501,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_open_mail", ImplementationState.NotImplemented)]
    MailOpenRequest = 0x1292,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_open_mail_r", ImplementationState.NotImplemented)]
    MailOpenResponse = 0xDF76,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_post_mail", ImplementationState.NotImplemented)]
    MailPostRequest = 0x34BC,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_post_mail_r", ImplementationState.NotImplemented)]
    MailPostResponse = 0x2306,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_cancel_protect_mail", ImplementationState.NotImplemented)]
    MailProtectCancelRequest = 0xFEAD,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_cancel_protect_mail_r", ImplementationState.NotImplemented)]
    MailProtectCancelResponse = 0x05C3,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_protect_mail", ImplementationState.NotImplemented)]
    MailProtectRequest = 0x024C,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_protect_mail_r", ImplementationState.NotImplemented)]
    MailProtectResponse = 0xC3E4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_enter_map_data_request_end", ImplementationState.Implemented)]
    MapDataEnterEndRequest = 0x04B4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_enter_map_data_request_end_r", ImplementationState.Implemented)]
    MapDataEnterEndResponse = 0xBE02,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_enter_map", ImplementationState.Implemented)]
    MapEnterRequest = 0x2810,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_enter_map_r", ImplementationState.Implemented)]
    MapEnterResponse = 0x1DCD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_maplink_data", ImplementationState.Implemented)]
    MapLinkGetDataRequest = 0x30C8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_maplink_data_r", ImplementationState.Implemented)]
    MapLinkGetDataResponse = 0x6C4E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_maplink_data", ImplementationState.Implemented)]
    MapLinkNotifyData = 0x5755,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_select_map", ImplementationState.Implemented)]
    NotifySelectMap = 0x68A5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_areamap_select_exec", ImplementationState.Implemented)]
    EventAreaMapSelectExec = 0x14B3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_mascot_count", ImplementationState.Implemented)]
    MascotGetCountRequest = 0x0CBC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_mascot_count_r", ImplementationState.Implemented)]
    MascotGetCountResponse = 0x7790,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_mission_data", ImplementationState.Implemented)]
    MissionDataRequest = 0x7D29,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_mission_data_r", ImplementationState.Implemented)]
    MissionDataResponse = 0x47F9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_monster_data", ImplementationState.Implemented)]
    MonsterGetDataRequest = 0x0466,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_monster_data_r", ImplementationState.Implemented)]
    MonsterGetDataResponse = 0xD094,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_money_data", ImplementationState.Implemented)]
    MoneyDataGetRequest = 0x61E7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_money_data_r", ImplementationState.Implemented)]
    MoneyDataGetResponse = 0xDC19,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_nps_point_get", ImplementationState.Implemented)]
    MoneyNpsPointsRequest = 0xBF17,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nps_point_get_r", ImplementationState.Implemented)]
    MoneyNpsPointsResponse = 0x3CF5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_money_updated_nicopoint", ImplementationState.NotImplemented)]
    MoneyUpdatedNicopoint = 0x7B72,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_money_updated_aipoint", ImplementationState.NotImplemented)]
    MoneyUpdatedAipoint = 0x196F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_myroom_furniture", ImplementationState.Implemented)]
    MyRoomGetFurnitureRequest = 0xE868,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_myroom_furniture_r", ImplementationState.Implemented)]
    MyRoomGetFurnitureResponse = 0x943D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myroom_furniture", ImplementationState.Implemented)]
    MyRoomNotifyFurniture = 0xA64A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_niconi_commons_base_list", ImplementationState.Implemented)]
    NiconiCommonsBaseListRequest = 0x97B7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_niconi_commons_base_list_r", ImplementationState.Implemented)]
    NiconiCommonsBaseListResponse = 0xE60C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_move_avatar", ImplementationState.Implemented)]
    AvatarMoveRequest = 0x9483,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_npc_data", ImplementationState.Implemented)]
    NpcGetDataRequest = 0x461B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_npc_data_r", ImplementationState.Implemented)]
    NpcGetDataResponse = 0x4403,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_npc_data", ImplementationState.Implemented)]
    NpcNotifyData = 0xCD67,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_access_npc", ImplementationState.Implemented)]
    EventAccessNpcRequest = 0x0D29,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_access_npc_r", ImplementationState.Implemented)]
    EventAccessNpcResponse = 0x3300,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_supply_npc_exec", ImplementationState.Implemented)]
    NotifySupplyNpcExec = 0x202B,

    [PacketMetadata(PacketServerType.Unknown, PacketDirection.Unknown, "", ImplementationState.Implemented)]
    Ping = 0xC202,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_talk_post", ImplementationState.Implemented)]
    PostTalkRequest = 0xEB2E,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_talk_post_r", ImplementationState.Implemented)]
    PostTalkResponse = 0x2407,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_cmd_exec", ImplementationState.Implemented)]
    CmdExecRequest = 0x2E64,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_cmd_exec_r", ImplementationState.Implemented)]
    CmdExecResponse = 0x6F32,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_robo_list", ImplementationState.Implemented)]
    RoboGetListRequest = 0x44CE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_robo_list_r", ImplementationState.Implemented)]
    RoboGetListResponse = 0xF606,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_robo_data", ImplementationState.Implemented)]
    NotifyRoboData = 0x1029,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_robo_create_info", ImplementationState.Implemented)]
    GetRoboCreateInfoRequest = 0x252C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_robo_create_info_r", ImplementationState.Implemented)]
    GetRoboCreateInfoResponse = 0xAE5B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_create", ImplementationState.Implemented)]
    RoboCreateRequest = 0x39EF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_create_r", ImplementationState.Implemented)]
    RoboCreateResponse = 0xB4AD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_call", ImplementationState.Implemented)]
    RoboCallRequest = 0xFA6E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_call_r", ImplementationState.Implemented)]
    RoboCallResponse = 0x70CA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_aiscript_start_r", ImplementationState.Implemented)]
    RoboAiscriptStartResponse = 0xF3BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_throwout_others_r", ImplementationState.NotImplemented)]
    MyRoomThrowoutOthersResponse = 0xB05A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_obtained_skill_list", ImplementationState.NotImplemented)]
    RoboGetObtainedSkillListRequest = 0xDCBF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_obtained_skill_list_r", ImplementationState.NotImplemented)]
    RoboGetObtainedSkillListResponse = 0x1159,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_update_robo_voice_type", ImplementationState.Implemented)]
    RoboVoiceTypeUpdateRequest = 0x9305,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_update_robo_voice_type_r", ImplementationState.Implemented)]
    RoboVoiceTypeUpdateResponse = 0x8F10,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_time_zone", ImplementationState.Implemented)]
    TimeZoneGetRequest = 0x5F53,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_time_zone_r", ImplementationState.Implemented)]
    TimeZoneGetResponse = 0xCD38,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_ucc_adv_figure_base_list", ImplementationState.Implemented)]
    UccAdvFigureBaseListRequest = 0x86DD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_ucc_adv_figure_base_list_r", ImplementationState.Implemented)]
    UccAdvFigureBaseListResponse = 0x878A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_ucc_voice_base_list", ImplementationState.Implemented)]
    UccVoiceBaseListRequest = 0x1149,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_ucc_voice_base_list_r", ImplementationState.Implemented)]
    UccVoiceBaseListResponse = 0xBB8F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_update_option", ImplementationState.Implemented)]
    UpdateOptionRequest = 0x79A1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_update_option_r", ImplementationState.Implemented)]
    UpdateOptionResponse = 0xB314,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_map", ImplementationState.Implemented)]
    NotifyChangeMap = 0xB315,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_map_failed", ImplementationState.Implemented)]
    NotifyChangeMapFailed = 0x6648,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_myroom", ImplementationState.Implemented)]
    NotifyChangeMyRoom = 0x0FA0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_select_init_island_start", ImplementationState.NotImplemented)]
    SelectInitIslandStart = 0x3E25,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_select_init_island_end", ImplementationState.NotImplemented)]
    SelectInitIslandEndRequest = 0xDDB3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_trashbox_open", ImplementationState.Implemented)]
    TrashboxOpenRequest = 0xF41F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trashbox_open_r", ImplementationState.Implemented)]
    TrashboxOpenResponse = 0x770E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_trashbox_close", ImplementationState.Implemented)]
    TrashboxCloseRequest = 0x6A3A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trashbox_close_r", ImplementationState.Implemented)]
    TrashboxCloseResponse = 0x9A7E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_trashbox_discard_item", ImplementationState.NotImplemented)]
    TrashboxDiscardItemRequest = 0xB18E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trashbox_discard_item_r", ImplementationState.NotImplemented)]
    TrashboxDiscardItemResponse = 0xBBEB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_edit_avatar_myprofile", ImplementationState.Implemented)]
    MyProfileAvatarEditRequest = 0xA063,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_edit_avatar_myprofile_r", ImplementationState.Implemented)]
    MyProfileAvatarEditResponse = 0x873B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_close_myprofile", ImplementationState.Implemented)]
    MyProfileCloseRequest = 0xEF6F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_trade_request", ImplementationState.Implemented)]
    TradeRequest = 0x2896,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_avatar_profile_data", ImplementationState.Implemented)]
    AvatarProfileGetDataRequest = 0xCF9A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_avatar_profile_data_r", ImplementationState.Implemented)]
    AvatarProfileGetDataResponse = 0xB670,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_friend_link_tag_get_other", ImplementationState.Implemented)]
    FriendLinkTagGetOtherRequest = 0xC9D8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_other_profile_text", ImplementationState.Implemented)]
    OtherProfileTextRequest = 0xD9DA,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_circle_talk", ImplementationState.Implemented)]
    CircleTalkRequest = 0xD65C,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_talk_forward", ImplementationState.Implemented)]
    TalkForwardNotify = 0x20F6,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_forward", ImplementationState.Implemented)]
    CircleChatForwardNotify = 0x2035,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_quest_work", ImplementationState.NotImplemented)]
    QuestWorkGetRequest = 0xF582,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_quest_history", ImplementationState.NotImplemented)]
    QuestHistoryGetRequest = 0x4BED,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_quest_work_r", ImplementationState.NotImplemented)]
    QuestWorkGetResponse = 0x7162,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_quest_history_r", ImplementationState.NotImplemented)]
    QuestHistoryGetResponse = 0xF8D9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_disappear_chara", ImplementationState.Implemented)]
    NotifyDisappearChara = 0xD3A4,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_create_new_circle_r", ImplementationState.Implemented)]
    CircleCreateResponse = 0xFFEB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_create_tag_r", ImplementationState.NotImplemented)]
    AdventureCreateTagResponse = 0x0021,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_delete_tag_r", ImplementationState.NotImplemented)]
    AdventureDeleteTagResponse = 0x4220,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_download_delete_request_r", ImplementationState.NotImplemented)]
    AdventureDownloadDeleteRequestResponse = 0x35CA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_end_r", ImplementationState.NotImplemented)]
    AdventureEndResponse = 0xC1D1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_lock_tag_r", ImplementationState.NotImplemented)]
    AdventureLockTagResponse = 0xBCEF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_added_buy_history", ImplementationState.NotImplemented)]
    AdventureShopAddedBuyHistoryNotify = 0xEEE8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_attn_tag", ImplementationState.NotImplemented)]
    AdventureShopAttnTagNotify = 0x7B72,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_buy_r", ImplementationState.NotImplemented)]
    AdventureShopBuyResponse = 0xFAA8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_buys_r", ImplementationState.NotImplemented)]
    AdventureShopBuysResponse = 0xB95B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_download_request_r", ImplementationState.NotImplemented)]
    AdventureShopDownloadRequestResponse = 0x46BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_end_r", ImplementationState.NotImplemented)]
    AdventureShopEndResponse = 0xC605,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_ended", ImplementationState.NotImplemented)]
    AdventureShopEndedNotify = 0xAD06,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_genre_search_r", ImplementationState.NotImplemented)]
    AdventureShopGenreSearchResponse = 0x6DC0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_item", ImplementationState.NotImplemented)]
    AdventureShopItemNotify = 0x9EA9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_keyword_search_r", ImplementationState.NotImplemented)]
    AdventureShopKeywordSearchResponse = 0x0AE8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_remove_all_buy_history_r", ImplementationState.NotImplemented)]
    AdventureShopRemoveAllBuyHistoryResponse = 0xB736,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_remove_buy_history_r", ImplementationState.NotImplemented)]
    AdventureShopRemoveBuyHistoryResponse = 0x1915,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_started", ImplementationState.NotImplemented)]
    AdventureShopStartedNotify = 0x0566,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_shop_tag_search_r", ImplementationState.NotImplemented)]
    AdventureShopTagSearchResponse = 0x53D5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_start_r", ImplementationState.NotImplemented)]
    AdventureStartResponse = 0x7C69,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_updated_sheet_stack", ImplementationState.NotImplemented)]
    AdventureUpdatedSheetStackNotify = 0xABE0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_upload_delete_request_r", ImplementationState.NotImplemented)]
    AdventureUploadDeleteRequestResponse = 0xFEF7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_upload_end_r", ImplementationState.NotImplemented)]
    AdventureUploadEndResponse = 0x2562,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_upload_request_r", ImplementationState.NotImplemented)]
    AdventureUploadRequestResponse = 0xF857,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_upload_request_report_r", ImplementationState.NotImplemented)]
    AdventureUploadRequestReportResponse = 0x1F30,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_upload_started", ImplementationState.NotImplemented)]
    AdventureUploadStartedNotify = 0x90BD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_work_add_sheet_r", ImplementationState.NotImplemented)]
    AdventureWorkAddSheetResponse = 0xCEF4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_work_create_r", ImplementationState.NotImplemented)]
    AdventureWorkCreateResponse = 0x7CD2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_work_delete_r", ImplementationState.NotImplemented)]
    AdventureWorkDeleteResponse = 0x2083,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_adventure_work_sub_sheet_r", ImplementationState.NotImplemented)]
    AdventureWorkSubSheetResponse = 0x216E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_download_delete_request_r", ImplementationState.NotImplemented)]
    AiDownloadDeleteRequestResponse = 0xC6C2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_added_buy_history", ImplementationState.NotImplemented)]
    AiShopAddedBuyHistoryNotify = 0x8071,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_buy_r", ImplementationState.NotImplemented)]
    AiShopBuyResponse = 0x7633,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_download_request_r", ImplementationState.NotImplemented)]
    AiShopDownloadRequestResponse = 0xA832,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_end_r", ImplementationState.NotImplemented)]
    AiShopEndResponse = 0x4A9E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_ended", ImplementationState.NotImplemented)]
    AiShopEndedNotify = 0x21B6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_genre_search_r", ImplementationState.NotImplemented)]
    AiShopGenreSearchResponse = 0xE32A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_item", ImplementationState.NotImplemented)]
    AiShopItemNotify = 0xBBBA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_ranking_search_r", ImplementationState.NotImplemented)]
    AiShopRankingSearchResponse = 0xD477,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_remove_all_buy_history_r", ImplementationState.NotImplemented)]
    AiShopRemoveAllBuyHistoryResponse = 0x1D3E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_remove_buy_history_r", ImplementationState.NotImplemented)]
    AiShopRemoveBuyHistoryResponse = 0xE0A1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_shop_removed_buy_history", ImplementationState.NotImplemented)]
    AiShopRemovedBuyHistoryNotify = 0x38CE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_upload_delete_request_r", ImplementationState.NotImplemented)]
    AiUploadDeleteRequestResponse = 0x1079,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_upload_request_r", ImplementationState.NotImplemented)]
    AiUploadRequestResponse = 0xE7C2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ai_upload_request_report_r", ImplementationState.NotImplemented)]
    AiUploadRequestReportResponse = 0xF1BE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_aipower_data", ImplementationState.NotImplemented)]
    AiPowerDataNotify = 0x59C3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_attack_blaze_r", ImplementationState.Implemented)]
    BattleAttackBlazeResponse = 0x2176,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_attack_cancel_r", ImplementationState.Implemented)]
    BattleAttackCancelResponse = 0x9666,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_attack_exec_r", ImplementationState.Implemented)]
    BattleAttackExecResponse = 0xF1A0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_attack_start_r", ImplementationState.Implemented)]
    BattleAttackStartResponse = 0xD752,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_dash_exec_r", ImplementationState.Implemented)]
    BattleDashExecResponse = 0xBCD9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_dash_finish_r", ImplementationState.Implemented)]
    BattleDashFinishResponse = 0xF93A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_kill_shot_ready_r", ImplementationState.Implemented)]
    BattleKillShotReadyResponse = 0xFD13,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_battle_target_lock_r", ImplementationState.Implemented)]
    BattleTargetLockResponse = 0x8AB2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_catalog_list", ImplementationState.NotImplemented)]
    CatalogListNotify = 0xB791,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_circle_chat_out_r", ImplementationState.NotImplemented)]
    CircleChatOutResponse = 0x42C4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_close_aipower_window_r", ImplementationState.NotImplemented)]
    CloseAiPowerWindowResponse = 0x6EE1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_close_myprofile_r", ImplementationState.NotImplemented)]
    CloseMyProfileResponse = 0x158E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_close_npc_rank_windor_r", ImplementationState.NotImplemented)]
    CloseNpcRankWindorResponse = 0xD077,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_debug_add_item_r", ImplementationState.NotImplemented)]
    DebugAddItemResponse = 0x3819,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_delete_friend_list_r", ImplementationState.NotImplemented)]
    DeleteFriendListResponse = 0x4BD6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_distribute_status_point_add_r", ImplementationState.Implemented)]
    DistributeStatusPointAddResponse = 0x7764,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_distribute_status_point_finish_r", ImplementationState.Implemented)]
    DistributeStatusPointFinishResponse = 0x7735,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_edit_robo_myprofile_r", ImplementationState.NotImplemented)]
    EditRoboMyProfileResponse = 0x2180,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_emotion_obtain", ImplementationState.NotImplemented)]
    EmotionObtainNotify = 0xD683,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_areamap_select_close", ImplementationState.Implemented)]
    EventAreaMapSelectCloseNotify = 0xD48C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_bbs_select_exec", ImplementationState.NotImplemented)]
    EventBbsSelectExecNotify = 0x052C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_bgm_play", ImplementationState.NotImplemented)]
    EventBgmPlayNotify = 0x59D3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_bgm_play", ImplementationState.NotImplemented)]
    NotifyBgmPlay = 0x36C1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_board_open", ImplementationState.NotImplemented)]
    EventBoardOpenNotify = 0xFC57,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_channel_select_exec", ImplementationState.NotImplemented)]
    EventChannelSelectExecNotify = 0x31B6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_chara_hide", ImplementationState.NotImplemented)]
    EventCharaHideNotify = 0x239C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_end", ImplementationState.Implemented)]
    EventEndNotify = 0x099D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_fade_in", ImplementationState.Implemented)]
    EventFadeInNotify = 0x0F8E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_get_tps_mode", ImplementationState.Implemented)]
    EventGetTpsModeNotify = 0xD758,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_start", ImplementationState.Implemented)]
    EventStartNotify = 0x1B5C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_sync", ImplementationState.Implemented)]
    EventSyncNotify = 0x462E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_fade_out", ImplementationState.Implemented)]
    EventFadeOutNotify = 0x3925,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_message", ImplementationState.Implemented)]
    EventMessageNotify = 0x662F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_message_close", ImplementationState.Implemented)]
    EventMessageCloseNotify = 0x317C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_island_select_exec", ImplementationState.Implemented)]
    EventIslandSelectExecNotify = 0x25F1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_notice", ImplementationState.Implemented)]
    EventNoticeNotify = 0xCD6F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_notice_close", ImplementationState.Implemented)]
    EventNoticeCloseNotify = 0xF477,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_quest_select_exec", ImplementationState.NotImplemented)]
    EventQuestSelectExecNotify = 0x7640,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_script_play", ImplementationState.Implemented)]
    EventScriptPlayNotify = 0x8091,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_select_exec", ImplementationState.Implemented)]
    EventSelectExecNotify = 0x6A56,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_select_init", ImplementationState.Implemented)]
    EventSelectInitNotify = 0xC0E7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_select_push", ImplementationState.Implemented)]
    EventSelectPushNotify = 0x32F7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_set_motion", ImplementationState.NotImplemented)]
    EventSetMotionNotify = 0xB2A9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_sleep", ImplementationState.NotImplemented)]
    EventSleepNotify = 0x8C7F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_trade_item_reset_r", ImplementationState.NotImplemented)]
    EventTradeItemResetResponse = 0xB6CD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_event_trade_item_set_r", ImplementationState.NotImplemented)]
    EventTradeItemSetResponse = 0xA73E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_exe_type_get_request", ImplementationState.NotImplemented)]
    ExeTypeGetRequestNotify = 0xDFDC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_exec_ai_palette_r", ImplementationState.NotImplemented)]
    ExecAiPaletteResponse = 0x8D5B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_expire_item_create", ImplementationState.NotImplemented)]
    ExpireItemCreateNotify = 0x067D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_expire_item_delete", ImplementationState.NotImplemented)]
    ExpireItemDeleteNotify = 0x9A01,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_expire_item_limit_select", ImplementationState.NotImplemented)]
    ExpireItemLimitSelectNotify = 0x0951,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_expire_item_notify_next_state", ImplementationState.NotImplemented)]
    ExpireItemNotifyNextStateNotify = 0x38B3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_expire_item_select_continue_result", ImplementationState.NotImplemented)]
    ExpireItemSelectContinueResultNotify = 0x17BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_expire_item_select_return_result", ImplementationState.NotImplemented)]
    ExpireItemSelectReturnResultNotify = 0xAAA0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_friend_link_tag_change_r", ImplementationState.NotImplemented)]
    FriendLinkTagChangeResponse = 0x690F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_gacha_end_r", ImplementationState.NotImplemented)]
    GachaEndResponse = 0x1380,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_gacha_started", ImplementationState.NotImplemented)]
    GachaStartedNotify = 0xCC88,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_gachaticket_exchange_item_add_r", ImplementationState.NotImplemented)]
    GachaTicketExchangeItemAddResponse = 0x7F77,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_gachaticket_exchange_item_del_r", ImplementationState.NotImplemented)]
    GachaTicketExchangeItemDelResponse = 0xC6DA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_gachaticket_exchange_open", ImplementationState.NotImplemented)]
    GachaTicketExchangeOpenNotify = 0x7D14,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_adventure_download_list_r", ImplementationState.NotImplemented)]
    GetAdventureDownloadListResponse = 0xA39A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_adventure_upload_list_r", ImplementationState.NotImplemented)]
    GetAdventureUploadListResponse = 0x49B5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_ai_palette_list_r", ImplementationState.Implemented)]
    GetAiPaletteListResponse = 0x87C7,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_channellist_map_r", ImplementationState.NotImplemented)]
    GetChannelListMapResponse = 0x5203,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_cosplay_list_r", ImplementationState.Implemented)]
    GetCosplayListResponse = 0x13CF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_free_friend_link_tag_r", ImplementationState.NotImplemented)]
    GetFreeFriendLinkTagResponse = 0x9865,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_my_robo_myprofile_data_r", ImplementationState.NotImplemented)]
    GetMyRoboMyProfileDataResponse = 0xFEAE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_myhouse_list_r", ImplementationState.NotImplemented)]
    GetMyHouseListResponse = 0xA206,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_other_avatar_myprofile_data_r", ImplementationState.NotImplemented)]
    GetOtherAvatarMyProfileDataResponse = 0xC5AD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_other_robo_myprofile_data_r", ImplementationState.NotImplemented)]
    GetOtherRoboMyProfileDataResponse = 0x1E61,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_get_placard_comment_log_r", ImplementationState.NotImplemented)]
    GetPlacardCommentLogResponse = 0x511B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_robo_job_list_r", ImplementationState.NotImplemented)]
    GetRoboJobListResponse = 0x8DE2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_get_tps_use_item_list_r", ImplementationState.NotImplemented)]
    GetTpsUseItemListResponse = 0x6841,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_hair_shop_buy_r", ImplementationState.NotImplemented)]
    HairShopBuyResponse = 0x4EF2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_hair_shop_end_r", ImplementationState.NotImplemented)]
    HairShopEndResponse = 0x725F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_hair_shop_ended", ImplementationState.NotImplemented)]
    HairShopEndedNotify = 0x1977,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_hair_shop_item", ImplementationState.NotImplemented)]
    HairShopItemNotify = 0xA359,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_delete", ImplementationState.Implemented)]
    ItemDeleteNotify = 0xF6B7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_r", ImplementationState.NotImplemented)]
    ItemEquipResponse = 0x551E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equip_replaced", ImplementationState.NotImplemented)]
    ItemEquipReplacedNotify = 0x8D71,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_equipped", ImplementationState.NotImplemented)]
    ItemEquippedNotify = 0xE63C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_move_r", ImplementationState.NotImplemented)]
    ItemMoveResponse = 0x708B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_remove_r", ImplementationState.NotImplemented)]
    ItemRemoveResponse = 0x46EA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_removed", ImplementationState.NotImplemented)]
    ItemRemovedNotify = 0x23D6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_r", ImplementationState.NotImplemented)]
    ItemTryEquipResponse = 0xA5DE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_replace_r", ImplementationState.NotImplemented)]
    ItemTryEquipReplaceResponse = 0x8C08,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_equip_replaced", ImplementationState.NotImplemented)]
    ItemTryEquipReplacedNotify = 0x7CDB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_try_removed", ImplementationState.NotImplemented)]
    ItemTryRemovedNotify = 0xD46E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_update_num", ImplementationState.NotImplemented)]
    ItemUpdateNumNotify = 0x05F8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_item_use_r", ImplementationState.NotImplemented)]
    ItemUseResponse = 0x2BBF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_itembox_close_r", ImplementationState.NotImplemented)]
    ItemBoxCloseResponse = 0x4835,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_itembox_takeout_r", ImplementationState.NotImplemented)]
    ItemBoxTakeoutResponse = 0x4135,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_live_contest_entry_participant_r", ImplementationState.NotImplemented)]
    LiveContestEntryParticipantResponse = 0x5E6E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_live_contest_play_r", ImplementationState.NotImplemented)]
    LiveContestPlayResponse = 0x6A62,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_live_contest_start_r", ImplementationState.NotImplemented)]
    LiveContestStartResponse = 0xBF8A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_mission_party_list_cancel_r", ImplementationState.NotImplemented)]
    MissionPartyListCancelResponse = 0xB75D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_mission_shop_choice_open", ImplementationState.NotImplemented)]
    MissionShopChoiceOpenNotify = 0x5C26,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myhouse_payment_rent_r", ImplementationState.NotImplemented)]
    MyHousePaymentRentResponse = 0xE5FF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myhouse_replacement_r", ImplementationState.NotImplemented)]
    MyHouseReplacementResponse = 0xE0FD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_set_furniture_r", ImplementationState.Implemented)]
    MyRoomSetFurnitureResponse = 0x1840,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_end_furniture_r", ImplementationState.Implemented)]
    MyRoomEndFurnitureResponse = 0xCECA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_remove_furniture_r", ImplementationState.Implemented)]
    MyRoomRemoveFurnitureResponse = 0xFD30,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_start_furniture_r", ImplementationState.Implemented)]
    MyRoomStartFurnitureResponse = 0x19BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_update_furniture_r", ImplementationState.Implemented)]
    MyRoomUpdateFurnitureResponse = 0x50A3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_update_name_r", ImplementationState.Implemented)]
    MyRoomUpdateNameResponse = 0xB186,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_update_security_r", ImplementationState.Implemented)]
    MyRoomUpdateSecurityResponse = 0xCE31,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_myroom_use_furniture_r", ImplementationState.Implemented)]
    MyRoomUseFurnitureResponse = 0xC437,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_niconi_commons_shop_buy_r", ImplementationState.NotImplemented)]
    NiconiCommonsShopBuyResponse = 0x96BE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_niconi_commons_shop_end_r", ImplementationState.NotImplemented)]
    NiconiCommonsShopEndResponse = 0xAA13,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_niconi_commons_shop_ended", ImplementationState.NotImplemented)]
    NiconiCommonsShopEndedNotify = 0xBFAB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_niconi_commons_shop_item", ImplementationState.NotImplemented)]
    NiconiCommonsShopItemNotify = 0x8C47,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_niconi_commons_shop_started", ImplementationState.NotImplemented)]
    NiconiCommonsShopStartedNotify = 0x7D98,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_close_r", ImplementationState.NotImplemented)]
    NicotvCloseResponse = 0x00E1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_get_info_by_furniture_r", ImplementationState.NotImplemented)]
    NicotvGetInfoByFurnitureResponse = 0x35A3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_get_playhead_time_r", ImplementationState.NotImplemented)]
    NicotvGetPlayheadTimeResponse = 0x0AAD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_get_playhead_time_request", ImplementationState.NotImplemented)]
    NicotvGetPlayheadTimeRequestNotify = 0xE858,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_open_r", ImplementationState.NotImplemented)]
    NicotvOpenResponse = 0xE88E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_set_channel_r", ImplementationState.NotImplemented)]
    NicotvSetChannelResponse = 0x528B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_set_movie_r", ImplementationState.NotImplemented)]
    NicotvSetMovieResponse = 0x31B0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_nicotv_set_playhead_time_r", ImplementationState.NotImplemented)]
    NicotvSetPlayheadTimeResponse = 0x3C45,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_add_cosplay", ImplementationState.NotImplemented)]
    NotifyAddCosplay = 0xF852,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_add_friend_list_result", ImplementationState.NotImplemented)]
    NotifyAddFriendListResult = 0xE04C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_battle_raise_start", ImplementationState.NotImplemented)]
    NotifyBattleRaiseStart = 0x1CC8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_battle_kill_shot_ready_end", ImplementationState.Implemented)]
    NotifyBattleKillShotReadyEnd = 0xFF7A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_battle_report_target_obj", ImplementationState.NotImplemented)]
    NotifyBattleReportTargetObj = 0x733E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_battle_report_target_pos", ImplementationState.NotImplemented)]
    NotifyBattleReportTargetPos = 0x58A2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_battle_target_lock", ImplementationState.Implemented)]
    NotifyBattleTargetLock = 0x7E96,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_battle_target_unlock", ImplementationState.Implemented)]
    NotifyBattleTargetUnlock = 0x4CA2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_avatar_job", ImplementationState.NotImplemented)]
    NotifyChangeAvatarJob = 0xBE37,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_change_friend_list_comment", ImplementationState.NotImplemented)]
    NotifyChangeFriendListComment = 0x6790,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_complete_state_open", ImplementationState.NotImplemented)]
    NotifyCompleteStateOpen = 0x9933,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_complete_state_update_num", ImplementationState.NotImplemented)]
    NotifyCompleteStateUpdateNum = 0x41A5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_cosplay_level_up", ImplementationState.NotImplemented)]
    NotifyCosplayLevelUp = 0xE731,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_debug_remove_object", ImplementationState.NotImplemented)]
    NotifyDebugRemoveObject = 0x9761,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_effectobj_data", ImplementationState.NotImplemented)]
    NotifyEffectobjData = 0x55AB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_end_effect_pos", ImplementationState.NotImplemented)]
    NotifyEndEffectPos = 0x773C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_hide_chara", ImplementationState.NotImplemented)]
    NotifyHideChara = 0x2E43,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_show_chara", ImplementationState.Implemented)]
    NotifyShowChara = 0xD1AE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_item_base", ImplementationState.NotImplemented)]
    NotifyItemBase = 0x737F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_itemcode_obtain_item", ImplementationState.NotImplemented)]
    NotifyItemCodeObtainItem = 0x0FE0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_live_contest_add_audience", ImplementationState.NotImplemented)]
    NotifyLiveContestAddAudience = 0x3221,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_live_contest_add_participant", ImplementationState.NotImplemented)]
    NotifyLiveContestAddParticipant = 0x937F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_live_contest_close", ImplementationState.NotImplemented)]
    NotifyLiveContestClose = 0x0427,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_live_contest_comment_delete", ImplementationState.NotImplemented)]
    NotifyLiveContestCommentDelete = 0xD19A,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_live_contest_comment_forward", ImplementationState.NotImplemented)]
    NotifyLiveContestCommentForward = 0xEC7F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_live_contest_end", ImplementationState.NotImplemented)]
    NotifyLiveContestEnd = 0x7AA7,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_live_contest_leave_participant", ImplementationState.NotImplemented)]
    NotifyLiveContestLeaveParticipant = 0x0582,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_live_contest_play", ImplementationState.NotImplemented)]
    NotifyLiveContestPlay = 0x7B2F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_login_message", ImplementationState.NotImplemented)]
    NotifyLoginMessage = 0x7D2C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_milestone_show", ImplementationState.NotImplemented)]
    NotifyMilestoneShow = 0x05E2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_action", ImplementationState.NotImplemented)]
    NotifyMissionAction = 0xF7CD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_list_pack", ImplementationState.NotImplemented)]
    NotifyMissionListPack = 0x4401,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_breakup", ImplementationState.NotImplemented)]
    NotifyMissionPartyBreakup = 0xFF75,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_change_mission", ImplementationState.NotImplemented)]
    NotifyMissionPartyChangeMission = 0x8964,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_info", ImplementationState.NotImplemented)]
    NotifyMissionPartyInfo = 0x9D6C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_list_open_start", ImplementationState.NotImplemented)]
    NotifyMissionPartyListOpenStart = 0x88C1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_list_pack", ImplementationState.NotImplemented)]
    NotifyMissionPartyListPack = 0xF9B3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_remove_host", ImplementationState.NotImplemented)]
    NotifyMissionPartyRemoveHost = 0x7E34,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_party_start_ok_update", ImplementationState.NotImplemented)]
    NotifyMissionPartyStartOkUpdate = 0x16AB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_result_open", ImplementationState.NotImplemented)]
    NotifyMissionResultOpen = 0xFE40,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_mission_situation_message", ImplementationState.NotImplemented)]
    NotifyMissionSituationMessage = 0x39C6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_monster_data", ImplementationState.Implemented)]
    NotifyMonsterData = 0x63FB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_appear", ImplementationState.NotImplemented)]
    NotifyMyHouseAppear = 0x701C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_auction_list", ImplementationState.NotImplemented)]
    NotifyMyHouseAuctionList = 0xC1E6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_auction_list_end", ImplementationState.NotImplemented)]
    NotifyMyHouseAuctionListEnd = 0x4BF2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_auction_open", ImplementationState.NotImplemented)]
    NotifyMyHouseAuctionOpen = 0xBA5A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_change_looks", ImplementationState.NotImplemented)]
    NotifyMyHouseChangeLooks = 0x4BE3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_change_security", ImplementationState.NotImplemented)]
    NotifyMyHouseChangeSecurity = 0x8F88,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_disappear", ImplementationState.NotImplemented)]
    NotifyMyHouseDisappear = 0x3C3A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_list", ImplementationState.NotImplemented)]
    NotifyMyHouseList = 0x3C63,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_relocate_failed", ImplementationState.NotImplemented)]
    NotifyMyHouseRelocateFailed = 0x671E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_relocate_succeed", ImplementationState.NotImplemented)]
    NotifyMyHouseRelocateSucceed = 0xCA17,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_release", ImplementationState.NotImplemented)]
    NotifyMyHouseRelease = 0xFA51,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_rent_open", ImplementationState.NotImplemented)]
    NotifyMyHouseRentOpen = 0x7B64,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myhouse_replacement_open", ImplementationState.NotImplemented)]
    NotifyMyHouseReplacementOpen = 0x67AC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myroom_remove_furniture", ImplementationState.Implemented)]
    NotifyMyRoomRemoveFurniture = 0x7A75,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myroom_set_furniture", ImplementationState.Implemented)]
    NotifyMyRoomSetFurniture = 0x7BBD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myroom_update_furniture", ImplementationState.Implemented)]
    NotifyMyRoomUpdateFurniture = 0xCEAB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_myroom_use_furniture", ImplementationState.Implemented)]
    NotifyMyRoomUseFurniture = 0xF777,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_new_mail", ImplementationState.NotImplemented)]
    NotifyNewMail = 0x61A8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_nicolive_reload", ImplementationState.NotImplemented)]
    NotifyNicoliveReload = 0xE4B2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_nicotv_close", ImplementationState.NotImplemented)]
    NotifyNicotvClose = 0xD39A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_nicotv_play", ImplementationState.NotImplemented)]
    NotifyNicotvPlay = 0x8A86,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_nicotv_set_movie", ImplementationState.NotImplemented)]
    NotifyNicotvSetMovie = 0x8E2A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_nicotv_set_playhead_time", ImplementationState.NotImplemented)]
    NotifyNicotvSetPlayheadTime = 0xAAAE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_option_data", ImplementationState.NotImplemented)]
    NotifyOptionData = 0xBC40,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ServerToClient, "recv_notify_placard_comment_log", ImplementationState.NotImplemented)]
    NotifyPlacardCommentLog = 0x553F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_placard_in_map", ImplementationState.NotImplemented)]
    NotifyPlacardInMap = 0x9089,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_placard_setting", ImplementationState.NotImplemented)]
    NotifyPlacardSetting = 0x71CB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_placard_update_popular", ImplementationState.NotImplemented)]
    NotifyPlacardUpdatePopular = 0xD9DD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_remove_ai_palette", ImplementationState.NotImplemented)]
    NotifyRemoveAiPalette = 0x9004,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_request_friend_list", ImplementationState.NotImplemented)]
    NotifyRequestFriendList = 0xABE6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_request_friend_list_cancel", ImplementationState.NotImplemented)]
    NotifyRequestFriendListCancel = 0x54D6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_robo_furnact_end", ImplementationState.NotImplemented)]
    NotifyRoboFurnactEnd = 0xB45C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_robo_furnact_start", ImplementationState.NotImplemented)]
    NotifyRoboFurnactStart = 0xB77E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_robo_job_work_result", ImplementationState.NotImplemented)]
    NotifyRoboJobWorkResult = 0xE7BB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_robo_jobs_pack", ImplementationState.NotImplemented)]
    NotifyRoboJobsPack = 0xAF52,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_room_list_open_start", ImplementationState.NotImplemented)]
    NotifyRoomListOpenStart = 0xA5BA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_room_list_open_end", ImplementationState.NotImplemented)]
    NotifyRoomListOpenEnd = 0xDC32,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_room_list_pack", ImplementationState.NotImplemented)]
    NotifyRoomListPack = 0xC0B2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_se_play", ImplementationState.NotImplemented)]
    NotifySePlay = 0xACFC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_skill_obtain", ImplementationState.NotImplemented)]
    NotifySkillObtain = 0x23A0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_start_effect", ImplementationState.NotImplemented)]
    NotifyStartEffect = 0x43BA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_start_use_item_effect", ImplementationState.NotImplemented)]
    NotifyStartUseItemEffect = 0xD656,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_timelimit_show", ImplementationState.NotImplemented)]
    NotifyTimelimitShow = 0x1D2F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_tps_use_item_end", ImplementationState.NotImplemented)]
    NotifyTpsUseItemEnd = 0xA2CD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_tps_use_item_start", ImplementationState.NotImplemented)]
    NotifyTpsUseItemStart = 0xFBF6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_buff_hitpoint_max", ImplementationState.NotImplemented)]
    NotifyUpdateBuffHitpointMax = 0xFAAB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_cosplay_exp", ImplementationState.NotImplemented)]
    NotifyUpdateCosplayExp = 0x56EE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_heart", ImplementationState.NotImplemented)]
    NotifyUpdateHeart = 0x64BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_heart_max", ImplementationState.NotImplemented)]
    NotifyUpdateHeartMax = 0xC9AC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_hitpoint", ImplementationState.NotImplemented)]
    NotifyUpdateHitpoint = 0x9CC0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_move_speed", ImplementationState.NotImplemented)]
    NotifyUpdateMoveSpeed = 0xB091,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_now_cosplay", ImplementationState.NotImplemented)]
    NotifyUpdateNowCosplay = 0xA67C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_pickup_mascot_count", ImplementationState.NotImplemented)]
    NotifyUpdatePickupMascotCount = 0x8FB2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_robo_equip", ImplementationState.Implemented)]
    NotifyUpdateRoboEquip = 0x372D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_robo_state", ImplementationState.Implemented)]
    NotifyUpdateRoboState = 0x2666,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_status_point", ImplementationState.NotImplemented)]
    NotifyUpdateStatusPoint = 0xE943,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_update_tank_max", ImplementationState.NotImplemented)]
    NotifyUpdateTankMax = 0x7134,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_user_status_update", ImplementationState.NotImplemented)]
    NotifyUserStatusUpdate = 0x7016,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_notify_voice_chara", ImplementationState.NotImplemented)]
    NotifyVoiceChara = 0xE37A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_open_ai_palette_r", ImplementationState.NotImplemented)]
    OpenAiPaletteResponse = 0xBD16,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_party_chat_talk_post_r", ImplementationState.NotImplemented)]
    PartyChatTalkPostResponse = 0xFB41,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_placard_remove_r", ImplementationState.NotImplemented)]
    PlacardRemoveResponse = 0x7C34,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_present_fix_r", ImplementationState.NotImplemented)]
    PresentFixResponse = 0xB40C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_present_npc_started", ImplementationState.NotImplemented)]
    PresentNpcStartedNotify = 0x0BA3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_present_robo_start_r", ImplementationState.NotImplemented)]
    PresentRoboStartResponse = 0x539B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_quest_ended", ImplementationState.NotImplemented)]
    QuestEndedNotify = 0xB02C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_quest_get_history_r", ImplementationState.NotImplemented)]
    QuestGetHistoryResponse = 0x32A4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_quest_get_work_r", ImplementationState.NotImplemented)]
    QuestGetWorkResponse = 0x8D0C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_quest_update_target", ImplementationState.NotImplemented)]
    QuestUpdateTargetNotify = 0x710C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_quest_updated_chapter", ImplementationState.NotImplemented)]
    QuestUpdatedChapterNotify = 0xA25C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_request_mission_outmap_r", ImplementationState.NotImplemented)]
    RequestMissionOutmapResponse = 0xB8AF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_request_mission_party_breakup_r", ImplementationState.NotImplemented)]
    RequestMissionPartyBreakupResponse = 0xE81E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_request_mission_party_change_mission_r", ImplementationState.NotImplemented)]
    RequestMissionPartyChangeMissionResponse = 0xF09C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_request_mission_party_mission_start_r", ImplementationState.NotImplemented)]
    RequestMissionPartyMissionStartResponse = 0x2167,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_request_mission_party_start_ok_r", ImplementationState.NotImplemented)]
    RequestMissionPartyStartOkResponse = 0x99EF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_request_mission_shop_open_r", ImplementationState.NotImplemented)]
    RequestMissionShopOpenResponse = 0x2CC9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_attach_r", ImplementationState.Implemented)]
    RoboAttachResponse = 0xABA1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_attach_request", ImplementationState.Implemented)]
    RoboAttachRequestNotify = 0x51CE,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_destroy_r", ImplementationState.NotImplemented)]
    RoboDestroyResponse = 0x1EAF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_detach_notice_from_avatar", ImplementationState.Implemented)]
    RoboDetachNoticeFromAvatarNotify = 0xE5AC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_detach_notice_from_robo", ImplementationState.Implemented)]
    RoboDetachNoticeFromRoboNotify = 0xBF2C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_grant_next_message_notice", ImplementationState.Implemented)]
    RoboGrantNextMessageNoticeNotify = 0x3611,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_talk_forward", ImplementationState.Implemented)]
    RoboTalkForwardNotify = 0x8EC4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_job_giveup_r", ImplementationState.NotImplemented)]
    RoboJobGiveupResponse = 0xF725,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_rest_r", ImplementationState.NotImplemented)]
    RoboRestResponse = 0xB235,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_robo_squire_r", ImplementationState.Implemented)]
    RoboSquireResponse = 0x899D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_room_list_close_r", ImplementationState.Implemented)]
    RoomListCloseResponse = 0xCBE8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_sheet_shop_buy_r", ImplementationState.NotImplemented)]
    SheetShopBuyResponse = 0x92AB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_sheet_shop_end_r", ImplementationState.NotImplemented)]
    SheetShopEndResponse = 0xAE06,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_sheet_shop_start_r", ImplementationState.NotImplemented)]
    SheetShopStartResponse = 0x6F5C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_chara_equip", ImplementationState.NotImplemented)]
    ShopCharaEquipNotify = 0xC66A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_item", ImplementationState.NotImplemented)]
    ShopItemNotify = 0x3C9A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_buy_r", ImplementationState.NotImplemented)]
    ShopBuyResponse = 0x2467,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_end_r", ImplementationState.NotImplemented)]
    ShopEndResponse = 0x18CA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_ended", ImplementationState.NotImplemented)]
    ShopEndedNotify = 0x73E2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_started", ImplementationState.NotImplemented)]
    ShopStartedNotify = 0x290F,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_shop_update_chara_r", ImplementationState.NotImplemented)]
    ShopUpdateCharaResponse = 0x2547,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_skill_exec_r", ImplementationState.NotImplemented)]
    SkillExecResponse = 0x1C6C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_stall_closed", ImplementationState.NotImplemented)]
    StallClosedNotify = 0x96F6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_stall_opened", ImplementationState.NotImplemented)]
    StallOpenedNotify = 0xFC49,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_stall_shop_chara_equip", ImplementationState.NotImplemented)]
    StallShopCharaEquipNotify = 0x2BA2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_stall_shop_item", ImplementationState.NotImplemented)]
    StallShopItemNotify = 0x5C30,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_stall_shop_started", ImplementationState.NotImplemented)]
    StallShopStartedNotify = 0x8CEA,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_storage_deposit_r", ImplementationState.NotImplemented)]
    StorageDepositResponse = 0x541C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_storage_opened", ImplementationState.Implemented)]
    StorageOpenedNotify = 0x2CA5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_storage_updated_deposit", ImplementationState.NotImplemented)]
    StorageUpdatedDepositNotify = 0xC515,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_storage_withdraw_r", ImplementationState.NotImplemented)]
    StorageWithdrawResponse = 0xE42A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_support_aipower_aipoint_r", ImplementationState.NotImplemented)]
    SupportAiPowerAiPointResponse = 0x5DB8,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_dele_update_r", ImplementationState.NotImplemented)]
    TradeDeleUpdateResponse = 0x6198,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_item_added", ImplementationState.NotImplemented)]
    TradeItemAddedNotify = 0x2F29,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_item_remove_r", ImplementationState.NotImplemented)]
    TradeItemRemoveResponse = 0x4356,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_item_removed", ImplementationState.NotImplemented)]
    TradeItemRemovedNotify = 0x0898,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_refused", ImplementationState.NotImplemented)]
    TradeRefusedNotify = 0x1975,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_request_r", ImplementationState.NotImplemented)]
    TradeRequestResponse = 0xE305,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_requested", ImplementationState.NotImplemented)]
    TradeRequestedNotify = 0x882D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_trade_respond_r", ImplementationState.NotImplemented)]
    TradeRespondResponse = 0x570C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_ucc_voice_obtain", ImplementationState.NotImplemented)]
    UccVoiceObtainNotify = 0xCDF6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_user_status_update_r", ImplementationState.NotImplemented)]
    UserStatusUpdateResponse = 0xD824,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ServerToClient, "recv_voice_chara_r", ImplementationState.NotImplemented)]
    VoiceCharaResponse = 0xC62B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_download_delete_request", ImplementationState.NotImplemented)]
    AdventureDownloadDeleteRequestRequest = 0x628C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_shop_download_request", ImplementationState.NotImplemented)]
    AdventureShopDownloadRequestRequest = 0x9F15,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_shop_end", ImplementationState.NotImplemented)]
    AdventureShopEndRequest = 0xADB6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_shop_buy", ImplementationState.NotImplemented)]
    ShopBuyRequest = 0xADB6,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_shop_end", ImplementationState.NotImplemented)]
    ShopEndRequest = 0x1C70,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_shop_ranking_search", ImplementationState.NotImplemented)]
    AdventureShopRankingSearchRequest = 0xD861,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_shop_remove_all_buy_history", ImplementationState.NotImplemented)]
    AdventureShopRemoveAllBuyHistoryRequest = 0x454B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_upload_delete_request", ImplementationState.NotImplemented)]
    AdventureUploadDeleteRequestRequest = 0xCB22,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_adventure_work_create", ImplementationState.NotImplemented)]
    AdventureWorkCreateRequest = 0xB1D9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_ai_download_delete_request", ImplementationState.NotImplemented)]
    AiDownloadDeleteRequestRequest = 0x8C02,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_ai_shop_download_request", ImplementationState.NotImplemented)]
    AiShopDownloadRequestRequest = 0xD5CB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_ai_shop_ranking_search", ImplementationState.NotImplemented)]
    AiShopRankingSearchRequest = 0x568B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_ai_shop_remove_all_buy_history", ImplementationState.NotImplemented)]
    AiShopRemoveAllBuyHistoryRequest = 0xABC5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_ai_upload_delete_request", ImplementationState.NotImplemented)]
    AiUploadDeleteRequestRequest = 0x81FC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_attack_blaze", ImplementationState.Implemented)]
    BattleAttackBlazeRequest = 0x5095,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_attack_cancel", ImplementationState.Implemented)]
    BattleAttackCancelRequest = 0x8355,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_attack_exec", ImplementationState.Implemented)]
    BattleAttackExecRequest = 0xC395,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_attack_start", ImplementationState.Implemented)]
    BattleAttackStartRequest = 0x3B2D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_dash_exec", ImplementationState.Implemented)]
    BattleDashExecRequest = 0x90F0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_dash_finish", ImplementationState.Implemented)]
    BattleDashFinishRequest = 0xE046,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_kill_shot_ready", ImplementationState.Implemented)]
    BattleKillShotReadyRequest = 0x0F3D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_target_lock", ImplementationState.Implemented)]
    BattleTargetLockRequest = 0xC11A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_battle_target_unlock", ImplementationState.Implemented)]
    BattleTargetUnlockRequest = 0x99E4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_delete_friend_list", ImplementationState.NotImplemented)]
    DeleteFriendListRequest = 0x343B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_distribute_status_point_add", ImplementationState.Implemented)]
    DistributeStatusPointAddRequest = 0x6755,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_distribute_status_point_finish", ImplementationState.Implemented)]
    DistributeStatusPointFinishRequest = 0xC252,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_tps_use_item_list", ImplementationState.NotImplemented)]
    GetTpsUseItemListRequest = 0x96B9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_edit_robo_myprofile", ImplementationState.NotImplemented)]
    EditRoboMyProfileRequest = 0x5AA9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_board_close", ImplementationState.NotImplemented)]
    EventBoardCloseRequest = 0x4A90,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_fade_in_r", ImplementationState.Implemented)]
    EventFadeInRequest = 0x2C4A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_get_tps_mode_r", ImplementationState.Implemented)]
    EventGetTpsModeRequest = 0xC290,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_fade_out_r", ImplementationState.Implemented)]
    EventFadeOutRequest = 0xF962,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_script_play_r", ImplementationState.Implemented)]
    EventScriptPlayRequest = 0xA3B5,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_areamap_select_exec_r", ImplementationState.Implemented)]
    EventAreaMapSelectExecRRequest = 0xD8FD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_island_select_exec_r", ImplementationState.Implemented)]
    EventIslandSelectExecRRequest = 0x0580,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_select_exec_r", ImplementationState.Implemented)]
    EventSelectExecRRequest = 0x6439,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_sync_r", ImplementationState.Implemented)]
    EventSyncRRequest = 0x701D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_event_set_motion_r", ImplementationState.NotImplemented)]
    EventSetMotionRRequest = 0x7221,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_exe_type_get_request_r", ImplementationState.NotImplemented)]
    ExeTypeGetRequestRRequest = 0x7BFF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_expire_item_limit_select_r", ImplementationState.NotImplemented)]
    ExpireItemLimitSelectRRequest = 0xBBE3,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_gacha_buy", ImplementationState.NotImplemented)]
    GachaBuyRequest = 0x663A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_gachaticket_exchange_close", ImplementationState.NotImplemented)]
    GachaTicketExchangeCloseRequest = 0x953B,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_ai_palette_list", ImplementationState.Implemented)]
    GetAiPaletteListRequest = 0xA628,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_channellist_map", ImplementationState.NotImplemented)]
    GetChannelListMapRequest = 0x23BF,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_cosplay_list", ImplementationState.Implemented)]
    GetCosplayListRequest = 0x14D0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_get_other_robo_myprofile_data", ImplementationState.NotImplemented)]
    GetOtherRoboMyProfileDataRequest = 0x16CA,

    [PacketMetadata(PacketServerType.Msg, PacketDirection.ClientToServer, "send_get_placard_comment_log", ImplementationState.NotImplemented)]
    GetPlacardCommentLogRequest = 0xA50E,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_heroine_ticket_check", ImplementationState.NotImplemented)]
    HeroineTicketCheckRequest = 0x1A73,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_itemcode_close", ImplementationState.NotImplemented)]
    ItemCodeCloseRequest = 0xD09A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_mission_outmap_choice_open_r", ImplementationState.NotImplemented)]
    MissionOutmapChoiceOpenRRequest = 0x32E9,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_mission_party_create_enter", ImplementationState.NotImplemented)]
    MissionPartyCreateEnterRequest = 0x3AF1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_mission_party_list_enter", ImplementationState.NotImplemented)]
    MissionPartyListEnterRequest = 0x0245,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_mission_raise_choice_open_r", ImplementationState.NotImplemented)]
    MissionRaiseChoiceOpenRRequest = 0x39CC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_mission_result_close", ImplementationState.NotImplemented)]
    MissionResultCloseRequest = 0x75AC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_mission_shop_choice_open_r", ImplementationState.NotImplemented)]
    MissionShopChoiceOpenRRequest = 0x53B2,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_move_robo", ImplementationState.Implemented)]
    MoveRoboRequest = 0x8DB1,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_end_furniture", ImplementationState.Implemented)]
    MyRoomEndFurnitureRequest = 0xB739,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_start_furniture", ImplementationState.Implemented)]
    MyRoomStartFurnitureRequest = 0x6A58,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_use_furniture", ImplementationState.Implemented)]
    MyRoomUseFurnitureRequest = 0x2231,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_update_name", ImplementationState.Implemented)]
    MyRoomUpdateNameRequest = 0xB154,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_update_security", ImplementationState.Implemented)]
    MyRoomUpdateSecurityRequest = 0xE54D,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_set_furniture", ImplementationState.Implemented)]
    MyRoomSetFurnitureRequest = 0xAEFB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_remove_furniture", ImplementationState.Implemented)]
    MyRoomRemoveFurnitureRequest = 0xD0DB,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_myroom_update_furniture", ImplementationState.Implemented)]
    MyRoomUpdateFurnitureRequest = 0x6405,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_room_list_close", ImplementationState.Implemented)]
    RoomListCloseRequest = 0x9A24,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_nicotv_close", ImplementationState.NotImplemented)]
    NicotvCloseRequest = 0x69BD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_nicotv_get_info_by_furniture", ImplementationState.NotImplemented)]
    NicotvGetInfoByFurnitureRequest = 0xC87A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_nicotv_open_by_furniture", ImplementationState.NotImplemented)]
    NicotvOpenByFurnitureRequest = 0x13CD,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_nicotv_get_playhead_time", ImplementationState.NotImplemented)]
    NicotvGetPlayheadTimeRequest = 0x1359,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_request_friend_list_answer", ImplementationState.NotImplemented)]
    RequestFriendListAnswerRequest = 0xC42A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_request_mission_party_breakup", ImplementationState.NotImplemented)]
    RequestMissionPartyBreakupRequest = 0x0061,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_aiscript_end", ImplementationState.Implemented)]
    RoboAiscriptEndRequest = 0xBDC0,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_aiscript_start", ImplementationState.Implemented)]
    RoboAiscriptStartRequest = 0xF522,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_attach", ImplementationState.Implemented)]
    RoboAttachRequest = 0xA595,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_attach_request_r", ImplementationState.Implemented)]
    RoboAttachRequestRRequest = 0x126C,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_destroy", ImplementationState.NotImplemented)]
    RoboDestroyRequest = 0x6652,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_detach_from_avatar", ImplementationState.Implemented)]
    RoboDetachFromAvatarRequest = 0x8198,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_detach_from_robo", ImplementationState.Implemented)]
    RoboDetachFromRoboRequest = 0x6843,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_furnact_end", ImplementationState.NotImplemented)]
    RoboFurnactEndRequest = 0xE7BC,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_rest", ImplementationState.NotImplemented)]
    RoboRestRequest = 0xF480,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_squire", ImplementationState.Implemented)]
    RoboSquireRequest = 0xE005,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_robo_talk_post", ImplementationState.Implemented)]
    RoboTalkPostRequest = 0xB368,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_set_ai_palette", ImplementationState.NotImplemented)]
    SetAiPaletteRequest = 0x2B59,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_shot_skill_cancell", ImplementationState.NotImplemented)]
    ShotSkillCancellRequest = 0x5791,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_storage_deposit", ImplementationState.NotImplemented)]
    StorageDepositRequest = 0x51A4,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_storage_withdraw", ImplementationState.NotImplemented)]
    StorageWithdrawRequest = 0x9C26,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_trade_cancel", ImplementationState.NotImplemented)]
    TradeCancelRequest = 0x726A,

    [PacketMetadata(PacketServerType.Area, PacketDirection.ClientToServer, "send_trade_dele_update", ImplementationState.NotImplemented)]
    TradeDeleUpdateRequest = 0xB8E9,
}

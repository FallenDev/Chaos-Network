#pragma warning disable CS1591
namespace Chaos.DarkAges.Definitions;

#region Custom Stuff
public enum ShardingType : byte
{
    None,
    AbsolutePlayerLimit,
    PlayerLimit,
    AbsoluteGroupLimit
}

[Flags]
public enum UserState : ulong
{
    None = 0,
    IsChanting = 1
    //add more user states here, double each time
}

public enum ChaosDialogType : byte
{
    Menu,
    MenuWithArgs,
    MenuTextEntry,
    MenuTextEntryWithArgs,
    ShowItems,
    ShowPlayerItems,
    ShowSpells,
    ShowSkills,
    ShowPlayerSpells,
    ShowPlayerSkills,
    Normal,
    DialogMenu,
    DialogTextEntry,
    Speak,
    CreatureMenu,
    Protected,
    CloseDialog
}
#endregion

#region Misc server enums
/// <summary>
///     A custom enum to separate server types for the purposes of redirects.
/// </summary>
public enum ServerType : byte
{
    /// <summary>
    ///     When you first open the client, you are in the lobby where you can choose a server to log in to
    /// </summary>
    Lobby = 0,

    /// <summary>
    ///     After choosing a server, you are redirected to a login server
    /// </summary>
    Login = 1,

    /// <summary>
    ///     After logging in, you are redirected to a world server.
    /// </summary>
    World = 2
}

/// <summary>
///     A byte switch as used for ClientOpCode.ServerTableRequest
/// </summary>
public enum ServerTableRequestType : byte
{
    /// <summary>
    ///     When a user chooses a server from the server list, the Id of that server is sent to us. This is the Id from the
    ///     server table.
    /// </summary>
    ServerId = 0,

    /// <summary>
    ///     Requests the server table, which is a list of servers for the user to choose to join
    /// </summary>
    RequestTable = 1
}

/// <summary>
///     A byte switch as used for ClientOpCode.MetaDataRequest
/// </summary>
public enum MetaDataRequestType : byte
{
    /// <summary>
    ///     The client is requesting the data for a specific metadata file
    /// </summary>
    DataByName = 0,

    /// <summary>
    ///     The client is requesting checksums of all metadata files, to ensure it has everything
    /// </summary>
    AllCheckSums = 1
}
#endregion

#region Messages
/// <summary>
///     A byte switch as used for ServerOpCode.ServerMessage
/// </summary>
public enum ServerMessageType : byte
{
    /// <summary>
    ///     Text appears blue, and appears in the top left
    /// </summary>
    Whisper = 0,

    /// <summary>
    ///     Text is only in the action bar, and will not show up in Shift+F
    /// </summary>
    OrangeBar1 = 1,

    /// <summary>
    ///     Text is only in the action bar, and will not show up in Shift+F
    /// </summary>
    OrangeBar2 = 2,

    /// <summary>
    ///     Texts appears in the action bar and Shift+F
    /// </summary>
    ActiveMessage = 3,

    /// <summary>
    ///     Text is only in the action bar, and will not show up in Shift+F
    /// </summary>
    OrangeBar3 = 4,

    /// <summary>
    ///     Text is only in the action bar, and will not show up in Shift+F. In official this was used for admin world messages
    /// </summary>
    AdminMessage = 5,

    /// <summary>
    ///     Text is only in the action bar, and will not show up in Shift+F
    /// </summary>
    OrangeBar5 = 6,

    /// <summary>
    ///     <see cref="UserOption" />s are sent via this text channel
    /// </summary>
    UserOptions = 7,

    /// <summary>
    ///     Pops open a window with a scroll bar. In official this was used for Sense
    /// </summary>
    ScrollWindow = 8,

    /// <summary>
    ///     Pops open a window with no scroll bar. In official this was used for perish lore
    /// </summary>
    NonScrollWindow = 9,

    /// <summary>
    ///     Pops open a window with a wooden boarder. In official this was used for signposts and wooden boards
    /// </summary>
    WoodenBoard = 10,

    /// <summary>
    ///     Text appears in a puke-green color. In official this was used for group chat
    /// </summary>
    GroupChat = 11,

    /// <summary>
    ///     Text appears in an olive-green color. In official this was used for guild chat
    /// </summary>
    GuildChat = 12,

    /// <summary>
    ///     Closes opened pop-up windows. <see cref="ScrollWindow" />, <see cref="NonScrollWindow" />,
    ///     <see cref="WoodenBoard" />
    /// </summary>
    ClosePopup = 17,

    /// <summary>
    ///     Text appears white, and persists indefinitely until cleared in the top right corner
    /// </summary>
    PersistentMessage = 18
}

/// <summary>
///     A byte switch as used by ServerOpCode.LoginMessage
/// </summary>
public enum LoginMessageType : byte
{
    /// <summary>
    ///     A generic confirmation window with an ok button
    /// </summary>
    Confirm = 0,

    /// <summary>
    ///     Clears the password fields on login and presents a message with an ok button
    /// </summary>
    WrongPassword = 1,

    /// <summary>
    ///     Clears the name fields and presents a message with an ok button
    /// </summary>
    CheckName = 2,

    /// <summary>
    ///     Clears the passwords fields and presents a message with an ok button
    /// </summary>
    CheckPassword = 3
}

/// <summary>
///     A byte switch used by ServerOpCode.LoginControls
/// </summary>
public enum LoginControlsType : byte
{
    /// <summary>
    ///     Tells the client that the packet contains the homepage url
    /// </summary>
    Homepage = 3
}

/// <summary>
///     A byte switch as used by ServerOpCode.PublicMessage
/// </summary>
public enum PublicMessageType : byte
{
    /// <summary>
    ///     Normal white chat message
    /// </summary>
    Normal = 0,

    /// <summary>
    ///     Yellow shout message
    /// </summary>
    Shout = 1,

    /// <summary>
    ///     Blue chant message
    /// </summary>
    Chant = 2
}

/// <summary>
///     Byte codes for the characters used for each color in the pattern '{=a'. Used by many things.
/// </summary>
public enum MessageColor : byte
{
    /// <summary>
    ///     The default color, keep the existing color
    /// </summary>
    Default = 0,

    /// <summary>
    ///     "{=a"
    /// </summary>
    Gray = 97,

    /// <summary>
    ///     "{=b"
    /// </summary>
    Red = 98,

    /// <summary>
    ///     "{=c" This color does not work in public chat
    /// </summary>
    Yellow = 99,

    /// <summary>
    ///     "{=d"
    /// </summary>
    DarkGreen = 100,

    /// <summary>
    ///     "{=e" This color does not work in public chat
    /// </summary>
    Silver = 101,

    /// <summary>
    ///     "{=f"
    /// </summary>
    Blue = 102,

    /// <summary>
    ///     "{=g"
    /// </summary>
    Gainsboro = 103,

    /// <summary>
    ///     "{=i"
    /// </summary>
    SpanishGray = 105,

    /// <summary>
    ///     "{=j"
    /// </summary>
    Nickel = 106,

    /// <summary>
    ///     "{=k"
    /// </summary>
    Slate = 107,

    /// <summary>
    ///     "{=l"
    /// </summary>
    Charcoal = 108,

    /// <summary>
    ///     "{=m"
    /// </summary>
    DirtyBlack = 109,

    /// <summary>
    ///     "{=n"
    /// </summary>
    Black = 110,

    /// <summary>
    ///     "{=o"
    /// </summary>
    HotPink = 111,

    /// <summary>
    ///     "{=p"
    /// </summary>
    Purple = 112,

    /// <summary>
    ///     "{=q"
    /// </summary>
    NeonGreen = 113,

    /// <summary>
    ///     "{=s"
    /// </summary>
    Orange = 115,

    /// <summary>
    ///     "{=t"
    /// </summary>
    Brown = 116,

    /// <summary>
    ///     "{=u"
    /// </summary>
    White = 117,

    /// <summary>
    ///     "{=x"
    /// </summary>
    Invisible = 120
}
#endregion

#region Legend
/// <summary>
///     A byte representing the icon of a legend mark. Used by ServerOpCode.Profile and ServerOpCode.SelfProfile
/// </summary>
public enum MarkIcon : byte
{
    Yay = 0,
    Warrior = 1,
    Rogue = 2,
    Wizard = 3,
    Priest = 4,
    Monk = 5,
    Heart = 6,
    Victory = 7
}

/// <summary>
///     A byte representing the color of a legend mark. Used by ServerOpCode.Profile and ServerOpCode.SelfProfile
/// </summary>
public enum MarkColor : byte
{
    Invisible = 0,
    Cyan = 1,
    Red = 2,
    Red1 = 3,
    Red2 = 4,
    Red3 = 5,
    Red4 = 6,
    Red5 = 7,
    Red6 = 8,
    Red7 = 9,
    GreenGrayG1 = 10,
    GreenGrayG2 = 11,
    GreenGrayG3 = 12,
    GreenGrayG4 = 13,
    GreenGrayG5 = 14,
    GreenGrayG6 = 15,
    WhiteBlackG1 = 16,
    WhiteBlackG2 = 17,
    WhiteBlackG3 = 18,
    WhiteBlackG4 = 19,
    WhiteBlackG5 = 20,
    WhiteBlackG6 = 21,
    WhiteBlackG7 = 22,
    WhiteBlackG8 = 23,
    WhiteBlackG9 = 24,
    WhiteBlackG10 = 25,
    WhiteBlackG11 = 26,
    WhiteBlackG12 = 27,
    WhiteBlackG13 = 28,
    WhiteBlackG14 = 29,
    WhiteBlackG15 = 30,
    WhiteBlackG16 = 31,
    PinkRedG1 = 32,
    PinkRedG2 = 33,
    PinkRedG3 = 34,
    PinkRedG4 = 35,
    PinkRedG5 = 36,
    PinkRedG6 = 37,
    PinkRedG7 = 38,
    PinkRedG8 = 39,
    PinkRedG9 = 40,
    PinkRedG10 = 41,
    PinkRedG11 = 42,
    PinkRedG12 = 43,
    PinkRedG13 = 44,
    PinkRedG14 = 45,
    PinkRedG15 = 46,
    PinkRedG16 = 47,
    OrangeG1 = 48,
    OrangeG2 = 49,
    OrangeG3 = 50,
    OrangeG4 = 51,
    OrangeG5 = 52,
    OrangeG6 = 53,
    OrangeG7 = 54,
    OrangeG8 = 55,
    PaleSkinToTanSkinG1 = 56,
    PaleSkinToTanSkinG2 = 57,
    PaleSkinToTanSkinG3 = 58,
    PaleSkinToTanSkinG4 = 59,
    PaleSkinToTanSkinG5 = 60,
    PaleSkinToTanSkinG6 = 61,
    PaleSkinToTanSkinG7 = 62,
    PaleSkinToTanSkinG8 = 63,
    YellowG1 = 64,
    YellowG2 = 65,
    YellowG3 = 66,
    YellowG4 = 67,
    YellowG5 = 68,
    YellowG6 = 69,
    YellowG7 = 70,
    YellowG8 = 71,
    LightGreenG1 = 72,
    LightGreenG2 = 73,
    LightGreenG3 = 74,
    LightGreenG4 = 75,
    LightGreenG5 = 76,
    LightGreenG6 = 77,
    LightGreenG7 = 78,
    LightGreenG8 = 79,
    TurquoiseG1 = 80,
    TurquoiseG2 = 81,
    TurquoiseG3 = 82,
    TurquoiseG4 = 83,
    TurquoiseG5 = 84,
    TurquoiseG6 = 85,
    TurquoiseG7 = 86,
    TurquoiseG8 = 87,
    BlueG1 = 88,
    BlueG2 = 89,
    BlueG3 = 90,
    BlueG4 = 91,
    BlueG5 = 92,
    BlueG6 = 93,
    BlueG7 = 94,
    BlueG8 = 95,
    BluePurpleG1 = 96,
    BluePurpleG2 = 97,
    BluePurpleG3 = 98,
    BluePurpleG4 = 99,
    BluePurpleG5 = 100,
    BluePurpleG6 = 101,
    BluePurpleG7 = 102,
    BluePurpleG8 = 103,
    RedPurpleG1 = 104,
    RedPurpleG2 = 105,
    RedPurpleG3 = 106,
    RedPurpleG4 = 107,
    RedPurpleG5 = 108,
    RedPurpleG6 = 109,
    RedPurpleG7 = 110,
    RedPurpleG8 = 111,
    GreenG1 = 112,
    GreenG2 = 113,
    GreenG3 = 114,
    GreenG4 = 115,
    GreenG5 = 116,
    GreenG6 = 117,
    GreenG7 = 118,
    GreenG8 = 119,
    GreenG9 = 120,
    GreenG10 = 121,
    GreenG11 = 122,
    GreenG12 = 123,
    GreenG13 = 124,
    GreenG14 = 125,
    GreenG15 = 126,
    GreenG16 = 127,
    LightGreenDarkGreenG1 = 128,
    LightGreenDarkGreenG2 = 129,
    LightGreenDarkGreenG3 = 130,
    LightGreenDarkGreenG4 = 131,
    LightGreenDarkGreenG5 = 132,
    LightGreenDarkGreenG6 = 133,
    LightGreenDarkGreenG7 = 134,
    LightGreenDarkGreenG8 = 135,
    LightGreenDarkGreenG9 = 136,
    LightGreenDarkGreenG10 = 137,
    LightGreenDarkGreenG11 = 138,
    LightGreenDarkGreenG12 = 139,
    LightGreenDarkGreenG13 = 140,
    LightGreenDarkGreenG14 = 141,
    LightGreenDarkGreenG15 = 142,
    LightGreenDarkGreenG16 = 143,
    LightOrangeDarkOrangeG1 = 144,
    LightOrangeDarkOrangeG2 = 145,
    LightOrangeDarkOrangeG3 = 146,
    LightOrangeDarkOrangeG4 = 147,
    LightOrangeDarkOrangeG5 = 148,
    LightOrangeDarkOrangeG6 = 149,
    LightOrangeDarkOrangeG7 = 150,
    LightOrangeDarkOrangeG8 = 151,
    LightOrangeDarkOrangeG9 = 152,
    LightOrangeDarkOrangeG10 = 153,
    LightOrangeDarkOrangeG11 = 154,
    LightOrangeDarkOrangeG12 = 155,
    LightOrangeDarkOrangeG13 = 156,
    LightOrangeDarkOrangeG14 = 157,
    LightOrangeDarkOrangeG15 = 158,
    LightOrangeDarkOrangeG16 = 159,
    LightBrownDarkBrownG1 = 160,
    LightBrownDarkBrownG2 = 161,
    LightBrownDarkBrownG3 = 162,
    LightBrownDarkBrownG4 = 163,
    LightBrownDarkBrownG5 = 164,
    LightBrownDarkBrownG6 = 165,
    LightBrownDarkBrownG7 = 166,
    LightBrownDarkBrownG8 = 167,
    LightBrownDarkBrownG9 = 168,
    LightBrownDarkBrownG10 = 169,
    LightBrownDarkBrownG11 = 170,
    LightBrownDarkBrownG12 = 171,
    LightBrownDarkBrownG13 = 172,
    LightBrownDarkBrownG14 = 173,
    LightBrownDarkBrownG15 = 174,
    LightBrownDarkBrownG16 = 175,
    LightGrayDarkGrayG1 = 176,
    LightGrayDarkGrayG2 = 177,
    LightGrayDarkGrayG3 = 178,
    LightGrayDarkGrayG4 = 179,
    LightGrayDarkGrayG5 = 180,
    LightGrayDarkGrayG6 = 181,
    LightGrayDarkGrayG7 = 182,
    LightGrayDarkGrayG8 = 183,
    LightGrayDarkGrayG9 = 184,
    LightGrayDarkGrayG10 = 185,
    LightGrayDarkGrayG11 = 186,
    LightGrayDarkGrayG12 = 187,
    LightGrayDarkGrayG13 = 188,
    LightGrayDarkGrayG14 = 189,
    LightGrayDarkGrayG15 = 190,
    LightGrayDarkGrayG16 = 191,
    LightBlueDarkBlueG1 = 192,
    LightBlueDarkBlueG2 = 193,
    LightBlueDarkBlueG3 = 194,
    LightBlueDarkBlueG4 = 195,
    LightBlueDarkBlueG5 = 196,
    LightBlueDarkBlueG6 = 197,
    LightBlueDarkBlueG7 = 198,
    LightBlueDarkBlueG8 = 199,
    LightBlueDarkBlueG9 = 200,
    LightBlueDarkBlueG10 = 201,
    LightBlueDarkBlueG11 = 202,
    LightBlueDarkBlueG12 = 203,
    LightBlueDarkBlueG13 = 204,
    LightBlueDarkBlueG14 = 205,
    LightBlueDarkBlueG15 = 206,
    LightBlueDarkBlueG16 = 207,
    LightBoardDarkBoardG1 = 208,
    LightBoardDarkBoardG2 = 209,
    LightBoardDarkBoardG3 = 210,
    LightBoardDarkBoardG4 = 211,
    LightBoardDarkBoardG5 = 212,
    LightBoardDarkBoardG6 = 213,
    LightBoardDarkBoardG7 = 214,
    LightBoardDarkBoardG8 = 215,
    LightBoardDarkBoardG9 = 216,
    LightBoardDarkBoardG10 = 217,
    LightBoardDarkBoardG11 = 218,
    LightBoardDarkBoardG12 = 219,
    LightBoardDarkBoardG13 = 220,
    LightBoardDarkBoardG14 = 221,
    LightBoardDarkBoardG15 = 222,
    LightBoardDarkBoardG16 = 223,
    LightOrangeRedDarkOrangeRedG1 = 224,
    LightOrangeRedDarkOrangeRedG2 = 225,
    LightOrangeRedDarkOrangeRedG3 = 226,
    LightOrangeRedDarkOrangeRedG4 = 227,
    LightOrangeRedDarkOrangeRedG5 = 228,
    LightOrangeRedDarkOrangeRedG6 = 229,
    LightOrangeRedDarkOrangeRedG7 = 230,
    LightOrangeRedDarkOrangeRedG8 = 231,
    LightOrangeRedDarkOrangeRedG9 = 232,
    LightOrangeRedDarkOrangeRedG10 = 233,
    LightOrangeRedDarkOrangeRedG11 = 234,
    LightOrangeRedDarkOrangeRedG12 = 235,
    LightOrangeRedDarkOrangeRedG13 = 236,
    LightOrangeRedDarkOrangeRedG14 = 237,
    LightOrangeRedDarkOrangeRedG15 = 238,
    LightOrangeRedDarkOrangeRedG16 = 239,
    TanG1 = 240,
    TanG2 = 241,
    TanG3 = 242,
    TanG4 = 243,
    TanG5 = 244,
    TanG6 = 245,
    Red8 = 246,
    Red9 = 247,
    Red10 = 248,
    Yellow = 249,
    BrightBlue = 250,
    BrightGreen = 251,
    DarkBlue = 252,
    DarkRed = 253,
    Orange = 254,
    White = 255
}
#endregion

#region DisplayData
/// <summary>
///     A byte representing the color of hair and items. Used by many things.
/// </summary>
public enum DisplayColor : byte
{
    /// <summary>
    ///     Actually Lavender
    /// </summary>
    Default,
    Black,
    Apple,
    Carrot,
    Yellow,
    Teal,
    Blue,
    Violet,
    Olive,
    Green,
    Pumpkin,
    Brown,
    Gray,
    Navy,
    Tan,
    White,
    Pink,
    Chartreuse,
    Orange,
    LightBlonde,
    Midnight,
    Sky,
    Mauve,
    Orchid,
    BubbleGum,
    LightBlue,
    HotPink,
    Cyan,
    Lilac,
    Salmon,
    NeonBlue,
    NeonGreen,
    PastelGreen,
    Blonde,
    RoyalBlue,
    Leather,
    Scarlet,
    Forest,
    Scarlet2,
    YaleBlue,
    Tangerine,
    DirtyBlonde,
    Sage,
    Grass,
    Cobalt,
    Blush,
    Glitch,
    Aqua,
    Lime,
    Purple,
    NeonRed,
    NeonYellow,
    PalePink,
    Peach,
    Crimson,
    Mustard,
    Silver,
    Fire,
    Ice,
    Magenta,
    PaleGreen,
    BabyBlue,
    Void,
    GhostBlue,
    Mint,
    Fern,
    GhostPink,
    Flamingo,
    Turquoise,
    MatteBlack,
    Taffy,
    NeonPurple
}

/// <summary>
///     A byte representing the style a player's name is shown in. Used by ServerOpCode.DisplayAisling
/// </summary>
public enum NameTagStyle : byte
{
    NeutralHover = 0,
    Hostile = 1,
    FriendlyHover = 2,
    Neutral = 3
}

/// <summary>
///     A byte representing the size of the lantern effect around a player. Used by ServerOpCode.DisplayAisling
/// </summary>
public enum LanternSize : byte
{
    None = 0,
    Small = 1,
    Large = 2
}

/// <summary>
///     A byte representing the body position a player is in while using a rest cloak. Used by ServerOpCode.DisplayAisling
/// </summary>
public enum RestPosition : byte
{
    None = 0,
    Kneel = 1,
    Lay = 2,
    Sprawl = 3
}

/// <summary>
///     A byte representing the color of the player's skin. Used by ServerOpCode.DisplayAisling
/// </summary>
public enum BodyColor : byte
{
    White = 0,
    Pale = 1,
    Brown = 2,
    Green = 3,
    Yellow = 4,
    Tan = 5,
    Grey = 6,
    LightBlue = 7,
    Orange = 8,
    Purple = 9
}

/// <summary>
///     A byte representing the sprite used for a player's body. Used by ServerOpCode.DisplayAisling
/// </summary>
public enum BodySprite : byte
{
    None = 0,
    Male = 16,
    Female = 32,
    MaleGhost = 48,
    FemaleGhost = 64,
    MaleInvis = 80,
    FemaleInvis = 96,
    MaleJester = 112,
    MaleHead = 128,
    FemaleHead = 144,
    BlankMale = 160,
    BlankFemale = 176
}

/// <summary>
///     A byte representing the gender of the player. Used by many things.
/// </summary>
[Flags]
public enum Gender : byte
{
    None = 0,
    Male = 1,
    Female = 2,
    Unisex = Male | Female
}
#endregion

#region Attributes
/// <summary>
///     A byte switch used for ClientOpCode.RaiseStat
/// </summary>
public enum Stat
{
    STR = 1,
    DEX = 2,
    INT = 4,
    WIS = 8,
    CON = 16
}

/// <summary>
///     A byte representing attack and defense elements on the Shift+G panel. Used by ServerOpCode.Attributes
/// </summary>
public enum Element : byte
{
    None = 0,
    Fire = 1,
    Water = 2,
    Wind = 3,
    Earth = 4,
    Holy = 5,
    Darkness = 6,
    Wood = 7,
    Metal = 8,
    Undead = 9
}

/// <summary>
///     A byte representing which type of mail was received. Used by ServerOpCode.Attributes
/// </summary>
public enum MailFlag : byte
{
    None = 0,
    HasMail = 16
}

/// <summary>
///     A byte representing the nation emblem displayed in the player's profile. Used by ServerOpCode.Profile and
///     ServerOpCode.SelfProfile
/// </summary>
public enum Nation : byte
{
    Exile,
    Suomi,
    Ellas,
    Loures,
    Mileth,
    Tagor,
    Rucesion,
    Noes,
    Illuminati,
    Piet,
    Atlantis,
    Abel,
    Undine,
    Purgatory
}

/// <summary>
///     A flag representing what combination of stats are being sent to a user. Used by ServerOpCode.Attributes. Modified for Zolian
/// </summary>
[Flags]
public enum StatUpdateType : byte
{
    None = 0,

    /// <summary>
    ///     mail -- Do Not Use, mail is sent as a boolean if true on the server-side then the flag is added in the converter
    /// </summary>
    UnreadMail = 1,
    Unknown = 1 << 1,

    /// <summary>
    ///     Blind, Mail, Elements, Ressists, AC, DMG, HIT
    /// </summary>
    Secondary = 1 << 2,

    /// <summary>
    ///     Exp, Gold
    /// </summary>
    ExpGold = 1 << 3,

    /// <summary>
    ///     Current HP, Current MP
    /// </summary>
    Vitality = 1 << 4,

    /// <summary>
    ///     Level, Max HP/MP, Current stats, Weight, Unspent
    /// </summary>
    Primary = 1 << 5,

    /// <summary>
    ///    Exp, Gold, Level, Max HP/MP, Current stats, Weight, Unspent
    /// </summary>
    WeightGold = ExpGold | Primary,

    /// <summary>
    ///   Level, Max & Current HP/MP, Current stats, Weight, Unspent
    /// </summary>
    FullVitality = Vitality | Primary,

    /// <summary>
    ///    All Status Flags - Minus GameMasterA, GameMasterB, and swimming
    /// </summary>
    Full = Primary | Vitality | ExpGold | Secondary,

    GameMasterA = 1 << 6,
    GameMasterB = 1 << 7,
    Swimming = GameMasterA | GameMasterB
}
#endregion

#region Profile
/// <summary>
///     A byte representing the 'temuair class' of an aisling. Used in many places. Modified for Zolian
/// </summary>
public enum BaseClass : byte
{
    Peasant = 0,
    Berserker = 1,
    Defender = 2,
    Assassin = 3,
    Cleric = 4,
    Arcanus = 5,
    Monk = 6,
    DualBash = 7,
    DualCast = 8,
    Racial = 9,
    Monster = 10,
    Quest = 11
}

/// <summary>
///     A flag representing the 'job class' of player; Modified for Zolian
/// </summary>
[Flags]
public enum JobClass
{
    None = 0,
    Thief = 1,
    DarkKnight = 1 << 1,
    Templar = 1 << 2,
    Knight = 1 << 3,
    Ninja = 1 << 4,
    SharpShooter = 1 << 5,
    Oracle = 1 << 6,
    Bard = 1 << 7,
    Summoner = 1 << 8,
    Samurai = 1 << 9,
    ShaolinMonk = 1 << 10,
    Necromancer = 1 << 11,
    Dragoon = 1 << 12
}

/// <summary>
///     A byte representing the slot of a piece of equipment. Used in many places.
/// </summary>
public enum EquipmentSlot : byte
{
    None = 0,
    Weapon = 1,
    Armor = 2,
    Shield = 3,
    Helmet = 4,
    Earrings = 5,
    Necklace = 6,
    LeftRing = 7,
    RightRing = 8,
    LeftGaunt = 9,
    RightGaunt = 10,
    Belt = 11,
    Greaves = 12,
    Boots = 13,
    Accessory1 = 14,
    Overcoat = 15,
    OverHelm = 16,
    Accessory2 = 17,
    Accessory3 = 18
}

/// <summary>
///     A byte representing the type of a piece of equipment.
/// </summary>
public enum EquipmentType : byte
{
    NotEquipment,
    Weapon,
    Armor,
    OverArmor,
    Shield,
    Helmet,
    OverHelmet,
    Earrings,
    Necklace,
    Ring,
    Gauntlet,
    Belt,
    Greaves,
    Boots,
    Accessory
}
#endregion

#region Options
/// <summary>
///     A byte representing the social status of the user. Used by ServerOpCode.Profile, and ServerOpCode.WorldList
/// </summary>
public enum SocialStatus : byte
{
    Awake = 0,
    DoNotDisturb = 1,
    DayDreaming = 2,
    NeedGroup = 3,
    Grouped = 4,
    LoneHunter = 5,
    GroupHunting = 6,
    NeedHelp = 7
}

/// <summary>
///     A byte representing an option on the client. Used by ClientOpCode.UserOptionToggle and ServerOpCode.ServerMessage
/// </summary>
public enum UserOption
{
    Request = 0,
    Option1 = 1,
    Option2 = 2,
    Option3 = 3,
    Option4 = 4,
    Option5 = 5,
    Option6 = 6,
    Option7 = 7,
    Option8 = 8,
    Option9 = 9,
    Option10 = 10,
    Option11 = 11,
    Option12 = 12,
    Option13 = 13
}
#endregion

#region GuI
/// <summary>
///     A byte representing the color of a name in the world list. Used by ServerOpCode.WorldList
/// </summary>
public enum WorldListColor : byte
{
    Guilded = 84,
    WithinLevelRange = 151,
    White = 255
}

/// <summary>
///     A byte representing the color an effect, for the purposes of showing it's remaining duration. Used by
///     ServerOpCode.Effect
/// </summary>
public enum EffectColor : byte
{
    None = 0,
    Blue = 1,
    Green = 2,
    Yellow = 3,
    Orange = 4,
    Red = 5,
    White = 6
}

/// <summary>
///     A byte representing the type of a panel. Used by ClientOpCode.SwapSlot
/// </summary>
public enum PanelType : byte
{
    Inventory = 0,
    SpellBook = 1,
    SkillBook = 2,
    Equipment = 3
}

public enum PageType : byte
{
    Page1,
    Page2,
    Page3
}

/// <summary>
///     A byte representing the type of a merchant menu. Used by ServerOpCode.Menu
/// </summary>
public enum MenuType : byte
{
    Menu = 0,
    MenuWithArgs = 1,
    TextEntry = 2,
    TextEntryWithArgs = 3,
    ShowItems = 4,
    ShowPlayerItems = 5,
    ShowSpells = 6,
    ShowSkills = 7,
    ShowPlayerSpells = 8,
    ShowPlayerSkills = 9
}

/// <summary>
///     A byte switch used to specify the type of additional args included in a dialog response. Used by
///     ClientOpCode.DialogResponse
/// </summary>
public enum DialogArgsType : byte
{
    None = 0,
    MenuResponse = 1,
    TextResponse = 2
}

/// <summary>
///     A byte switched used to specify the type of dialog. Used by ServerOpCode.Dialog
/// </summary>
public enum DialogType : byte
{
    Normal = 0,
    DialogMenu = 2,
    TextEntry = 4,
    Speak = 5,
    CreatureMenu = 6,
    Protected = 9,
    CloseDialog = 10
}

/// <summary>
///     A byte representing which button on a dialog window was pressed. Used by ClientOpCode.DialogResponse
/// </summary>
public enum DialogResult : sbyte
{
    Previous = -1,
    Close = 0,
    Next = 1
}

/// <summary>
///     A byte representing the type of board being displayed. Used by ServerOpCode.BulletinBoard
/// </summary>
public enum BoardOrResponseType : byte
{
    BoardList = 1,
    PublicBoard = 2,
    PublicPost = 3,
    MailBoard = 4,
    MailPost = 5,
    SubmitPostResponse = 6,
    DeletePostResponse = 7,
    HighlightPostResponse = 8
}

/// <summary>
///     A byte representing the type of request in association with a board. Used by ClientOpCode.BoardRequest
/// </summary>
public enum BoardRequestType : byte
{
    BoardList = 1,
    ViewBoard = 2,
    ViewPost = 3,
    NewPost = 4,
    Delete = 5,
    SendMail = 6,
    Highlight = 7
}

public enum BoardControls : sbyte
{
    NextPage = -1,
    RequestPost = 0,
    PreviousPage = 1
}

public enum NotepadType
{
    Brown = 0,
    GlitchedBlue1 = 1,
    GlitchedBlue2 = 2,
    Orange = 3,
    White = 4
}
#endregion

#region Skill/Spell
/// <summary>
///     A byte representing the type of a spell. Used by ServerOpCode.AddSpellToPane
/// </summary>
public enum SpellType : byte
{
    None = 0,
    Prompt = 1,
    Targeted = 2,
    Prompt4Nums = 3,
    Prompt3Nums = 4,
    NoTarget = 5,
    Prompt2Nums = 6,
    Prompt1Num = 7
}

/// <summary>
///     A byte representing the emote/body animation being sent. Used by ServerOpCode.BodyAnimation
/// </summary>
public enum BodyAnimation : byte
{
    None = 0,
    Assail = 1,
    HandsUp = 6,
    Smile = 9,
    Cry = 10,
    Frown = 11,
    Wink = 12,
    Surprise = 13,
    Tongue = 14,
    Pleasant = 15,
    Snore = 16,
    Mouth = 17,
    BlowKiss = 21,
    Wave = 22,
    RockOn = 23,
    Peace = 24,
    Stop = 25,
    Ouch = 26,
    Impatient = 27,
    Shock = 28,
    Pleasure = 29,
    Love = 30,
    SweatDrop = 31,
    Whistle = 32,
    Irritation = 33,
    Silly = 34,
    Cute = 35,
    Yelling = 36,
    Mischievous = 37,
    Evil = 38,
    Horror = 39,
    PuppyDog = 40,
    StoneFaced = 41,
    Tears = 42,
    FiredUp = 43,
    Confused = 44,
    PriestCast = 128,
    TwoHandAtk = 129,
    Jump = 130,
    Kick = 131,
    Punch = 132,
    RoundHouseKick = 133,
    Stab = 134,
    DoubleStab = 135,
    WizardCast = 136,
    PlayNotes = 137,
    HandsUp2 = 138,
    Swipe = 139,
    HeavySwipe = 140,
    JumpAttack = 141,
    BowShot = 142,
    HeavyBowShot = 143,
    LongBowShot = 144,
    Summon = 145
}
#endregion

#region Game
/// <summary>
///     A byte switch used when receiving information about a click. Used by ClientOpCode.Click
/// </summary>
public enum ClickType : byte
{
    TargetId = 1,
    TargetPoint = 3
}

public enum LevelCircle : byte
{
    /// <summary>
    ///     Levels 1-10
    /// </summary>
    One = 1,

    /// <summary>
    ///     Levels 11-40
    /// </summary>
    Two = 2,

    /// <summary>
    ///     Levels 41-70
    /// </summary>
    Three = 3,

    /// <summary>
    ///     Levels 71-98
    /// </summary>
    Four = 4,

    /// <summary>
    ///     Levels 99+
    /// </summary>
    Five = 5,

    /// <summary>
    ///     Master
    /// </summary>
    Six = 6,

    /// <summary>
    ///     Advanced Class
    /// </summary>
    Seven = 7
}

/// <summary>
///     A byte switch used when receiving information about an action performed on an exchange window. Used by
///     ClientOpCode.Exchange
/// </summary>
public enum ExchangeRequestType : byte
{
    StartExchange = 0,
    AddItem = 1,
    AddStackableItem = 2,
    SetGold = 3,
    Cancel = 4,
    Accept = 5
}

/// <summary>
///     A byte switch used to specify the action performed on an exchange window. Used by ServerOpCode.Exchange
/// </summary>
public enum ExchangeResponseType : byte
{
    StartExchange = 0,
    RequestAmount = 1,
    AddItem = 2,
    SetGold = 3,
    Cancel = 4,
    Accept = 5
}

public enum EntityType : byte
{
    Creature = 1,
    Item = 2,
    Aisling = 4
}

public enum IgnoreType : byte
{
    Request = 1,
    AddUser = 2,
    RemoveUser = 3
}

public enum CreatureType : byte
{
    Normal = 0,
    WalkThrough = 1,
    Merchant = 2,
    WhiteSquare = 3,
    Aisling = 4
}

public enum ClientGroupSwitch : byte
{
    FormalInvite = 1,
    TryInvite = 2,
    AcceptInvite = 3,
    CreateGroupbox = 4,
    ViewGroupBox = 5,
    RemoveGroupBox = 6,
    RequestToJoin = 7
}

public enum ServerGroupSwitch : byte
{
    Invite = 1,
    ShowGroupBox = 4,
    RequestToJoin = 5
}

/// <summary>
///     Feel free to rename these to align with whatever you have configured in your light metadata.
/// </summary>
/// <remarks>
///     You can have all 12 values be in sequence of darkest to lightest (or vice versa), but this will constrain you to
///     only having 1 light type per map. Alternatively, you could 2 different light types defined here, 6 values for each,
///     which would allow you to define 2 different light types per map without needing to restart the server. Each set of
///     6 values would both be defined under the same light type in the metadata.
/// </remarks>
public enum LightLevel : byte
{
    Darkest_A = 0,
    Darker_A = 1,
    Dark_A = 2,
    Light_A = 3,
    Lighter_A = 4,
    Lightest_A = 5,
    Darkest_B = 6,
    Darker_B = 7,
    Dark_B = 8,
    Light_B = 9,
    Lighter_B = 10,
    Lightest_B = 11
}

[Flags]
public enum MapFlags : byte
{
    None = 0,
    Snow = 1,
    Rain = 2,
    Darkness = Rain | Snow,
    NoTabMap = 64,
    SnowTileset = 128
}

[Flags]
public enum TileFlags : byte
{
    None = 0,
    Wall = 15,
    Unknown = 128
}
#endregion
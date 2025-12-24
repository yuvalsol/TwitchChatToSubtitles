namespace TwitchChatToSubtitles.Library;

// measurements are for Calibri font
// default video resolution for ASSA files is 384x288
// braille position is based on a line of 30 characters

public enum SubtitlesFontSize
{
    [SubtitlesFontSizeMeasurements(LineLength = 0, TextPosXLocationRight = 0, BraillePosXLocationRight = 0, MaxBottomPosY = 0, TimestampFontSize = 0)]
    None = 0,

    [SubtitlesFontSizeMeasurements(LineLength = 45, TextPosXLocationRight = 290, BraillePosXLocationRight = 287, MaxBottomPosY = 275, TimestampFontSize = 5)]
    Regular = 8,

    [SubtitlesFontSizeMeasurements(LineLength = 40, TextPosXLocationRight = 290, BraillePosXLocationRight = 275, MaxBottomPosY = 273, TimestampFontSize = 6)]
    Medium = 9,

    [SubtitlesFontSizeMeasurements(LineLength = 36, TextPosXLocationRight = 289, BraillePosXLocationRight = 263, MaxBottomPosY = 272, TimestampFontSize = 6)]
    Large = 10,

    [SubtitlesFontSizeMeasurements(LineLength = 33, TextPosXLocationRight = 289, BraillePosXLocationRight = 251, MaxBottomPosY = 270, TimestampFontSize = 7)]
    XL = 11,

    [SubtitlesFontSizeMeasurements(LineLength = 30, TextPosXLocationRight = 281, BraillePosXLocationRight = 239, MaxBottomPosY = 269, TimestampFontSize = 7)]
    X2L = 12,

    [SubtitlesFontSizeMeasurements(LineLength = 30, TextPosXLocationRight = 272, BraillePosXLocationRight = 227, MaxBottomPosY = 268, TimestampFontSize = 8)]
    X3L = 13,

    [SubtitlesFontSizeMeasurements(LineLength = 30, TextPosXLocationRight = 264, BraillePosXLocationRight = 215, MaxBottomPosY = 266, TimestampFontSize = 8)]
    X4L = 14,

    [SubtitlesFontSizeMeasurements(LineLength = 30, TextPosXLocationRight = 255, BraillePosXLocationRight = 203, MaxBottomPosY = 265, TimestampFontSize = 9)]
    X5L = 15
}

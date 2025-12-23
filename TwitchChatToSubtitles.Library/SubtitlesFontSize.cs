namespace TwitchChatToSubtitles.Library;

public enum SubtitlesFontSize
{
    [SubtitlesFontSizeMeasurements(LineLength = 0, PosXLocationRight = 0, MaxBottomPosY = 0, TimestampFontSize = 0)]
    None = 0,

    [SubtitlesFontSizeMeasurements(LineLength = 45, PosXLocationRight = 270, MaxBottomPosY = 275, TimestampFontSize = 5)]
    Regular = 8,

    [SubtitlesFontSizeMeasurements(LineLength = 40, PosXLocationRight = 270, MaxBottomPosY = 273, TimestampFontSize = 6)]
    Medium = 9,

    [SubtitlesFontSizeMeasurements(LineLength = 36, PosXLocationRight = 270, MaxBottomPosY = 272, TimestampFontSize = 6)]
    Large = 10,

    [SubtitlesFontSizeMeasurements(LineLength = 33, PosXLocationRight = 269, MaxBottomPosY = 270, TimestampFontSize = 7)]
    XL = 11,

    [SubtitlesFontSizeMeasurements(LineLength = 30, PosXLocationRight = 270, MaxBottomPosY = 269, TimestampFontSize = 7)]
    X2L = 12,

    [SubtitlesFontSizeMeasurements(LineLength = 30, PosXLocationRight = 261, MaxBottomPosY = 268, TimestampFontSize = 8)]
    X3L = 13,

    [SubtitlesFontSizeMeasurements(LineLength = 30, PosXLocationRight = 252, MaxBottomPosY = 266, TimestampFontSize = 8)]
    X4L = 14,

    [SubtitlesFontSizeMeasurements(LineLength = 30, PosXLocationRight = 243, MaxBottomPosY = 265, TimestampFontSize = 9)]
    X5L = 15
}

/*

(0,0)           (384,0)
+---------------+
|               |
|               |
+---------------+
(0,288)         (384,288)

measurements are for Calibri font

108
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,66)\fnCalibri\fs8\bord0\shad0}
123456789012345678901234567890123456789012345                                                   Regular, fs8, 45 chars

109
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,72)\fnCalibri\fs9\bord0\shad0}
1234567890123456789012345678901234567890                                           Medium, fs9, 40 chars

110
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,78)\fnCalibri\fs10\bord0\shad0}
123456789012345678901234567890123456                                      Large, fs10, 36 chars

111
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,85)\fnCalibri\fs11\bord0\shad0}
123456789012345678901234567890123                                   XL, fs11, 33 chars

112
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,92)\fnCalibri\fs12\bord0\shad0}
123456789012345678901234567890                              X2L, fs12, 30 chars

113
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,99)\fnCalibri\fs13\bord0\shad0}
123456789012345678901234567890                     X3L, fs13, 30 chars

114
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,107)\fnCalibri\fs14\bord0\shad0}
123456789012345678901234567890             X4L, fs14, 30 chars

115
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(3,115)\fnCalibri\fs15\bord0\shad0}
123456789012345678901234567890      X5L, fs15, 30 chars

208
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,66)\fnCalibri\fs8\bord0\shad0}
543210987654321098765432109876543210987654321

209
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,72)\fnCalibri\fs9\bord0\shad0}
0987654321098765432109876543210987654321

210
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,78)\fnCalibri\fs10\bord0\shad0}
654321098765432109876543210987654321

211
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(269,85)\fnCalibri\fs11\bord0\shad0}
321098765432109876543210987654321

212
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,92)\fnCalibri\fs12\bord0\shad0}
098765432109876543210987654321

213
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(261,99)\fnCalibri\fs13\bord0\shad0}
098765432109876543210987654321

214
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(252,107)\fnCalibri\fs14\bord0\shad0}
098765432109876543210987654321

215
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(243,115)\fnCalibri\fs15\bord0\shad0}
098765432109876543210987654321

*/

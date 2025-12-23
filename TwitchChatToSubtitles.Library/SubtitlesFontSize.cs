namespace TwitchChatToSubtitles.Library;

public enum SubtitlesFontSize
{
    [FontSizeMeasurements(BodyLineLength = 0, PosXLocationRight = 0)]
    None = 0,

    [FontSizeMeasurements(BodyLineLength = 45, PosXLocationRight = 270)]
    Regular = 8,

    [FontSizeMeasurements(BodyLineLength = 40, PosXLocationRight = 270)]
    Bigger = 9,

    [FontSizeMeasurements(BodyLineLength = 36, PosXLocationRight = 270)]
    Biggest = 10
}

/*

(0,0)           (384,0)
+---------------+
|               |
|               |
+---------------+
(0,288)         (384,288)

measurements are for Calibri font and BIGGER_LINE_LENGTH = 45.
REGULAR_LINE_LENGTH = 50 has more chars and BIGGEST_LINE_LENGTH = 40 has less chars,
so, although they both are positioned at X = 255,
they don't overflow out of the right side of the screen

108
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(5,65)\fnCalibri\fs8\bord0\shad0}
123456789012345678901234567890123456789012345

109
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(5,71)\fnCalibri\fs9\bord0\shad0}
1234567890123456789012345678901234567890

110
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(5,77)\fnCalibri\fs10\bord0\shad0}
123456789012345678901234567890123456

208
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,65)\fnCalibri\fs8\bord0\shad0}
543210987654321098765432109876543210987654321

209
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,71)\fnCalibri\fs9\bord0\shad0}
0987654321098765432109876543210987654321

210
00:00:00,000 --> 9:59:59,999
{\a5\an7\pos(270,77)\fnCalibri\fs10\bord0\shad0}
654321098765432109876543210987654321

*/

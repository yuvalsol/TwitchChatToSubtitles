![Twitch Chat To Subtitles](./Solution%20Items/Images/TwitchChatToSubtitlesUI.jpg "Twitch Chat To Subtitles")

# Twitch Chat To Subtitles

Twitch Chat To Subtitles converts a Twitch chat JSON file to SubRip .srt subtitle file.

The program provides 3 types of subtitles and one text file:
1. **Regular Subtitles**. Chat messages will appear at the center-bottom of the screen.
2. **Rolling Chat Subtitles**. Chat messages will roll vertically across the screen and then disappear. Past chat messages won't clutter the screen.
3. **Static Chat Subtitles**. Chat messages are added to the bottom of all the previous chat messages and remain there. Similar to what Twitch chat does.
4. **Chat Text File**. Save Twitch chat to a text file.

The program uses [ASSA tags](https://www.nikse.dk/subtitleedit/formats/assa-override-tags "ASSA - Override tags") extensively. ASSA tags are what allows the program to position the subtitles at different locations on the screen. ASSA tags are not part of SubRip specification but some media players have the ability to read ASSA tags from a SubRip file. The program was tested successfully with MPC-HC. On the other hand, VLC, or at least VLC 3, ignores ASSA tags in a SubRip file. For this case, the program can create [Subtitles Without ASSA Tags](#subtitles-without-assa-tags "Subtitles Without ASSA Tags"). If the program generates subtitles with ASSA tags, it will also underline moderators and URL links.

The program is provided both as Command Line (**TwitchChatToSubtitles.exe**) and as UI (**TwitchChatToSubtitlesUI.exe**).

Twitch Chat To Subtitles requires .NET 8 Runtime.

## Download Twitch Chat

Twitch Chat To Subtitles doesn't download the JSON file itself, the JSON file must be retrieved beforehand. There are a few programs for downloading Twitch chat JSON file. [Twitch Downloader](https://github.com/lay295/TwitchDownloader "Twitch Downloader") seems to be the best choice to start with.

- [Twitch Downloader](https://github.com/lay295/TwitchDownloader "Twitch Downloader"). Has GUI and CLI. Supports Windows, Linux & Mac.
- Chrome Extension [Twitch Chat Downloader](https://chromewebstore.google.com/detail/twitch-chat-downloader/fkcglcjlhbfbechmbmcajldcfkcpklng "Twitch Chat Downloader"). Make sure to download the .json file, not the .csv file.
- [RechatTool](https://github.com/jdpurcell/RechatTool "RechatTool"). Command line tool to download the chat log from a Twitch VOD.
- [Chat Downloader](https://github.com/xenova/chat-downloader "Chat Downloader"). Command line tool, in Python, used to retrieve chat messages.

## Regular Subtitles

Chat messages will appear at the center-bottom of the screen.

```console
TwitchChatToSubtitles.exe --RegularSubtitles
                          --JsonFile <file>
                          [--ColorUserNames]
                          [--RemoveEmoticonNames]
                          [--ShowTimestamps]
                          [--SubtitleShowDuration <seconds>]
                          [--SubtitlesFontSize <font size>]
                          [--TextColor <color>]
                          [--TimeOffset <seconds>]
```

### Subtitles Without ASSA Tags

The following subset of options create a subtitles file without any ASSA tags.

```console
TwitchChatToSubtitles.exe --RegularSubtitles
                          --JsonFile <file>
                          [--RemoveEmoticonNames]
                          [--SubtitleShowDuration <seconds>]
                          [--TimeOffset <seconds>]
```

#### Usage

1. Default usage:

```console
TwitchChatToSubtitles.exe --RegularSubtitles --JsonFile "C:\Path\To\Twitch Chat.json"
```

2. Color user names, remove emoticon names, font size `Bigger`, subtitles will use ASSA tags:
```console
TwitchChatToSubtitles.exe --RegularSubtitles --JsonFile "C:\Path\To\Twitch Chat.json" --ColorUserNames --RemoveEmoticonNames --SubtitlesFontSize Bigger
```

## Rolling Chat Subtitles

Chat messages will roll vertically across the screen and then disappear. Past chat messages won't clutter the screen.

```console
TwitchChatToSubtitles.exe --RollingChatSubtitles
                          --JsonFile <file>
                          [--ColorUserNames]
                          [--RemoveEmoticonNames]
                          [--ShowTimestamps]
                          [--SubtitlesFontSize <font size>]
                          [--SubtitlesLocation <location>]
                          [--SubtitlesRollingDirection <rolling direction>]
                          [--SubtitlesSpeed <speed>]
                          [--TextColor <color>]
                          [--TimeOffset <seconds>]
```

#### Usage

1. Default usage:

```console
TwitchChatToSubtitles.exe --RollingChatSubtitles --JsonFile "C:\Path\To\Twitch Chat.json"
```

2. Color user names, remove emoticon names, font size `Bigger`, subtitles location will default to whole `Left` side of the screen:
```console
TwitchChatToSubtitles.exe --RollingChatSubtitles --JsonFile "C:\Path\To\Twitch Chat.json" --ColorUserNames --RemoveEmoticonNames --SubtitlesFontSize Bigger
```

3. Subtitles will roll faster than regular speed and will appear on the right side and top half of the screen:

```console
TwitchChatToSubtitles.exe --RollingChatSubtitles --JsonFile "C:\Path\To\Twitch Chat.json" --SubtitlesLocation RightTopHalf --SubtitlesSpeed Faster
```

## Static Chat Subtitles

Chat messages are added to the bottom of all the previous chat messages and remain there. Similar to what Twitch chat does.

```console
TwitchChatToSubtitles.exe --StaticChatSubtitles
                          --JsonFile <file>
                          [--ColorUserNames]
                          [--RemoveEmoticonNames]
                          [--ShowTimestamps]
                          [--SubtitlesFontSize <font size>]
                          [--SubtitlesLocation <location>]
                          [--TextColor <color>]
                          [--TimeOffset <seconds>]
```

#### Usage

1. Default usage:

```console
TwitchChatToSubtitles.exe --StaticChatSubtitles --JsonFile "C:\Path\To\Twitch Chat.json"
```

2. Color user names, remove emoticon names, font size `Bigger`, subtitles location will default to whole `Left` side of the screen:
```console
TwitchChatToSubtitles.exe --StaticChatSubtitles --JsonFile "C:\Path\To\Twitch Chat.json" --ColorUserNames --RemoveEmoticonNames --SubtitlesFontSize Bigger
```

3. Subtitles will appear on the left side and top two-thirds of the screen:

```console
TwitchChatToSubtitles.exe --StaticChatSubtitles --JsonFile "C:\Path\To\Twitch Chat.json" --SubtitlesLocation LeftTopTwoThirds
```

## Chat Text File

Save Twitch chat to a text file. In the text file, "[M]" before the user name indicates a moderator.

```console
TwitchChatToSubtitles.exe --ChatTextFile
                          --JsonFile <file>
                          [--RemoveEmoticonNames]
                          [--ShowTimestamps]
```

#### Usage

1. Default usage:

```console
TwitchChatToSubtitles.exe --ChatTextFile --JsonFile "C:\Path\To\Twitch Chat.json"
```

2. Remove emoticon names, show chat message timestamps:
```console
TwitchChatToSubtitles.exe --ChatTextFile --JsonFile "C:\Path\To\Twitch Chat.json" --RemoveEmoticonNames --ShowTimestamps
```

## Command Line Options

These options select which subtitles type to convert the Twitch chat JSON file to.

```console
--RegularSubtitles      Convert Twitch chat to regular subtitles.
--RollingChatSubtitles  Convert Twitch chat to rolling chat subtitles.
--StaticChatSubtitles   Convert Twitch chat to static chat subtitles.
--ChatTextFile          Save Twitch chat to a text file.
```

The full path to the Twitch chat JSON file. The name of the subtitles file is the same as the name of the JSON file with .srt extension.

```console
--JsonFile              Path to Twitch chat JSON file.
```

Users, who post in Twitch chat, have an assigned color to their name. This option colors the name of the users across all subtitles, both the title and the body of chat messages. If a user doesn't have an assigned color, it will be colored with Twitch's purple color.

```console
--ColorUserNames        Whether to color user names.
```

SubRip specification doesn't support displaying images. As a result of that, emoticons are written by their name in the chat message. This option removes the emoticon and badge names from chat messages.

If you are downloading chat JSON file using [Twitch Downloader](https://github.com/lay295/TwitchDownloader "Twitch Downloader"), I suggest to check the boxes "Embed Images" and all the "3rd Party Emotes" boxes. Twitch Chat To Subtitles retrieves the names of the emoticon and badge names from the list of embed emoticons in the JSON file.

```console
--RemoveEmoticonNames   Remove emoticon and badge names.
```

This option displays the timestamp of when the chat message was posted in chat.

```console
--ShowTimestamps        Whether to show chat message timestamps.
```

This option is applicable only for `RegularSubtitles`. A chat message has a timestamp of when it was posted in chat. This timestamp determines when it is going to be shown as a regular subtitle and this option determines how long the subtitle will be visible on screen. The default is 5 seconds.

```console
--SubtitleShowDuration  (Default: 5) For how long a subtitle is visible
                        on the screen, in seconds.
```

The font size of the subtitles. If the options is not specified, `RollingChatSubtitles` and `StaticChatSubtitles` will default to `Regular` font size and for `RegularSubtitles`, the font size will be determined by the media player.

```console
--SubtitlesFontSize     The font size of the subtitles.
                        Valid values: Regular, Bigger, Biggest.
```

This option determines where the subtitles are displayed on the screen. This option is applicable only for `RollingChatSubtitles` and `StaticChatSubtitles`.

```console
--SubtitlesLocation     The location of the subtitles on the screen.
                        Valid values: Left, LeftTopHalf, LeftBottomHalf,
                        LeftTopTwoThirds,LeftBottomTwoThirds, Right, RightTopHalf,
                        RightBottomHalf,RightTopTwoThirds, RightBottomTwoThirds.
```

This option determines the direction that the subtitles roll from the bottom to the top of the screen or from the top to the bottom of the screen. This option is applicable only for `RollingChatSubtitles`. If not specified, it will default to `BottomToTop` direction.

```console
--SubtitlesRollingDirection     The direction that the subtitles roll.
                                Valid values: BottomToTop, TopToBottom.
```

This option determines the speed of the subtitles rolling vertically across the screen. This option is applicable only for `RollingChatSubtitles`. If not specified, it will default to `Regular` speed.

```console
--SubtitlesSpeed        How fast the subtitles roll.
                        Valid values: Regular, Faster, Fastest.
```

This option determines the color of the subtitles text. If not specified, the color will be determined by the media player's default text color. Valid color values are hex format (`#000000`) or known names (`Black`).

```console
--TextColor             The color of the subtitles text.
```

This option shifts the timing of all the subtitles. This option is applicable for all subtitles types but very useful for `RegularSubtitles`. For `RegularSubtitles`, the subtitles are visible only for a few seconds and then disappear. A Twitch streamer is more likely to interact with a chat message as it passes around the mid point of the chat, not when it first appears at the bottom of the chat. By adding a few seconds (3-7 seconds), the subtitles will appear **closer in time** to when the Twitch streamer have read and responded to it.

```console
--TimeOffset            Time offset for all subtitles, in seconds.
```

## Acknowledgments

[Subtitle icons created by Azland Studio - Flaticon](https://www.flaticon.com/free-icons/subtitle "subtitle icons")

namespace TwitchChatToSubtitles.Library
{
    internal struct ProcessedComment
    {
        public TimeSpan Timestamp;
        public string User;
        public string Body;
        public bool IsBrailleArt;
        public Color Color;
    }
}

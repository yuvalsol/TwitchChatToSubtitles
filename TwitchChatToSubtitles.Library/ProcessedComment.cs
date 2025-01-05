namespace TwitchChatToSubtitles.Library
{
    internal struct ProcessedComment
    {
        public TimeSpan Timestamp;
        public string User;
        public bool IsModerator;
        public string Body;
        public bool IsBrailleArt;
        public Color Color;
    }
}

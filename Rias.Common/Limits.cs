namespace Rias.Common;

public static class Limits
{
    public static class Guild
    {
        public static class Channel
        {
            public const int MinNameLength = 1;
            public const int MaxNameLength = 100;
        }

        public static class Emoji
        {
            public const int MinNameLength = 2;
            public const int MaxNameLength = 32;
            public const int SizeLimit = 2 * 1024 * 1024;
        }
    }
}
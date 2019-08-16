using System;
using System.Collections.Generic;
using System.Text;
using Discord;

namespace Unreal_Dumping_Agent.Discord
{
    public static class DiscordText
    {
        private static readonly List<string> _angryEmoji = new List<string> { "triumph", "rage", "pouting_cat", "unamused" };
        private static readonly List<string> _happyEmoji = new List<string> { "smile", "joy", "joy_cat", "smile_cat", "smiley_cat", "smiley" };
        private static readonly List<string> _sadEmoji = new List<string> { "cry", "tired_face", "crying_cat_face", "grimacing", "cold_sweat", "disappointed_relieved", "sob", "crying_cat_face" };
        private static readonly List<string> _digitEmoji = new List<string> { "0\u20e3", "1\u20e3", "2\u20e3", "3\u20e3", "4\u20e3", "5\u20e3", "6\u20e3", "7\u20e3", "8\u20e3", "9\u20e3" };

        private static readonly List<string> _understandWordPrefix = new List<string>
        {
            "Nani",
            "Nah",
            "Off",
            "Noooo"
        };
        private static readonly List<string> _understandString = new List<string>
        {
            "i can't understand what u want from me !!",
            "can u explain more .?",
            "i can't get it !!",
            "i can't get it kill me pls !",
            "i think we can't keep talking.",
            "man i can't get it can you start be clear.?",
            "don't talk to me Again"
        };

        private static T GetRandomItem<T>(this IList<T> source)
        {
            return source[new Random().Next(0, source.Count)];
        }
        public static string GetRandomAngryEmoji()
        {
            return $":{_angryEmoji.GetRandomItem()}:";
        }
        public static string GetRandomHappyEmoji()
        {
            return $":{_happyEmoji.GetRandomItem()}:";
        }
        public static string GetRandomSadEmoji()
        {
            return $":{_sadEmoji.GetRandomItem()}:";
        }
        public static string GetRandomNotUnderstandString()
        {
            return $"{_understandWordPrefix.GetRandomItem()}, {_understandString.GetRandomItem()} {GetRandomAngryEmoji()}";
        }
        public static IEmote GetEmojiNumber(int num)
        {
            return new Emoji(_digitEmoji[num]);
        }
        public static string GetEmojiNumber(int num, bool getStr)
        {
            return _digitEmoji[num];
        }
        public static IEmote[] GenEmojiNumberList(int count, bool withZero)
        {
            var emojis = new List<IEmote>();

            for (int i = withZero ? 0 : 1; i < count + (!withZero ? 1 : 0); i++)
            {
                if (i <= count)
                    emojis.Add(new Emoji(_digitEmoji[i]));
            }

            return emojis.ToArray();
        }
    }
}

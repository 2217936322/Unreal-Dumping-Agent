using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Discord
{
    public static class DiscordText
    {
        private static readonly List<string> _angryEmoji = new List<string> { "triumph", "rage", "pouting_cat", "unamused" };
        private static readonly List<string> _happyEmoji = new List<string> { "smile", "joy", "joy_cat", "smile_cat", "smiley_cat", "smiley" };
        private static readonly List<string> _sadEmoji = new List<string> { "cry", "tired_face", "crying_cat_face", "grimacing", "cold_sweat", "disappointed_relieved", "sob", "crying_cat_face" };

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

        private static T GetRandomItem<T>(this List<T> source)
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
    }
}

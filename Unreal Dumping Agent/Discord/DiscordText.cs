using System;
using System.Collections.Generic;
using System.Text;

namespace Unreal_Dumping_Agent.Discord
{
    public static class DiscordText
    {
        private static readonly List<string> AngryEmoji = new List<string> { "triumph", "rage", "pouting_cat", "unamused" };
        private static readonly List<string> HappyEmoji = new List<string> { "smile", "joy", "joy_cat", "smile_cat", "smiley_cat", "smiley" };

        private static readonly List<string> UnderstandWordPrefix = new List<string>
        {
            "Fuck",
            "Nah",
            "Off",
            "Noooo"
        };
        private static readonly List<string> UnderstandString = new List<string>
        {
            "i can't understand what u want from me !!",
            "can u explain more .?",
            "i can't get it !!",
            "i can't get it kill me pls !",
            "i think we can't keep talking.",
            "go kill your self NOW.",
            "man i can't get it can u start be clear.?",
            "don't talk to me Again"
        };

        private static T GetRandomItem<T>(this List<T> source)
        {
            return source[new Random().Next(0, source.Count)];
        }
        public static string GetRandomAngryEmoji()
        {
            return $":{AngryEmoji.GetRandomItem()}:";
        }
        public static string GetRandomHappyEmoji()
        {
            return $":{HappyEmoji.GetRandomItem()}:";
        }
        public static string GetRandomNotUnderstandString()
        {
            return $"{UnderstandWordPrefix.GetRandomItem()}, {UnderstandString.GetRandomItem()} {GetRandomAngryEmoji()}";
        }
    }
}

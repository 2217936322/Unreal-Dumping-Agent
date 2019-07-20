using System.Diagnostics;
using Microsoft.ML.Data;

namespace Unreal_Dumping_Agent.Chat
{
    public enum EQuestionType
    {
        None = 0,
        HiWelcome = 1,
        Thanks = 2,
        LifeAsk = 3,
        Ask = 4,
        Funny = 5,
        Find = 101,
        LockProcess = 102
    }
    public enum EQuestionTask
    {
        None = 0,
        GNames = 1,
        GObject = 2,
        Process = 3
    }

    internal class ChatQuestion
    {
        public string QuestionText { get; set; }
        public int QuestionType { get; set; } // EQuestionType
        public int QuestionTask { get; set; } // EQuestionTask
    }
    [DebuggerDisplay("Type = {TypeEnum()}, Task = {TaskEnum()}")]
    public class QuestionPrediction
    {
        [ColumnName("PredictedType")]
        public int QuestionType;

        public int QuestionTask;

        public EQuestionType TypeEnum() => (EQuestionType)QuestionType;
        public EQuestionTask TaskEnum() => (EQuestionTask)QuestionTask;
    }
}

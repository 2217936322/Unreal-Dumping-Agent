using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ML.Data;

namespace Unreal_Dumping_Agent.Chat
{
    public enum EQuestionType
    {
        None = 0,
        Find = 1
    }
    public enum EQuestionTask
    {
        None = 0,
        GNames = 1,
        GObject = 2,
        GNamesAndGObject = GNames | GObject
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

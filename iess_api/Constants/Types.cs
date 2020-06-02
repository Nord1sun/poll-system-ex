using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace iess_api.Constants
{
    public static class Types
    {
        public static readonly Dictionary<string, QuizAnswerType> QuizAnswerTypes =
            new Dictionary<string, QuizAnswerType>(StringComparer.InvariantCultureIgnoreCase);

        public static readonly Dictionary<string, PollAnswerType> PollAnswerTypes =
            new Dictionary<string, PollAnswerType>(StringComparer.InvariantCultureIgnoreCase);
            
        public static readonly List<string> DateRangeLabels=new List<string>{"notstarted","live","ended"};

        static Types()
        {
            foreach (var value in Enum.GetValues(typeof(QuizAnswerType)))
            {
                QuizAnswerTypes.Add(Enum.GetName(typeof(QuizAnswerType),value),(QuizAnswerType) value);
            }
            foreach (var value in Enum.GetValues(typeof(PollAnswerType)))
            {
                PollAnswerTypes.Add(Enum.GetName(typeof(PollAnswerType),value),(PollAnswerType) value);
            }
        }

        public static string GetQuizAnswerTypeAsString(QuizAnswerType quizAnswerType) =>
            QuizAnswerTypes.FirstOrDefault(p => p.Value == quizAnswerType).Key;

        public static string GetPollAnswerTypeAsString(PollAnswerType pollAnswerType) =>
            PollAnswerTypes.FirstOrDefault(p => p.Value == pollAnswerType).Key;
    }

    public enum QuizAnswerType
    {
        [Description("Only one answer is correct")]
        Singleselection,
        [Description("Multiple answers are correct")]
        Multiselection,
        [Description("Answer options not given, arbitrary answer from user")]
        TextInput,
    }

    public enum PollAnswerType
    {
        [Description("Only one answer is correct")]
        Singleselection,
        [Description("Multiple answers are correct")]
        Multiselection,
    }

    public enum PicturesAssociateMode
    {
        Link,
        Unlink
    }
}

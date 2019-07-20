using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.ML;

namespace Unreal_Dumping_Agent.Chat
{
    public class ChatManager
    {
        // Keys must be lower
        private static readonly Dictionary<string, EQuestionTask> ChatTasks = new Dictionary<string, EQuestionTask>
        {
            { "gname", EQuestionTask.GNames },
            { "gnames", EQuestionTask.GNames },
            { "gobject", EQuestionTask.GObject },
            { "gobjects", EQuestionTask.GObject },
            { "process", EQuestionTask.Process },
            { "target", EQuestionTask.Process },
        };
        private static string AppPath => Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
        private static string ModelPath => Path.Combine(AppPath, "Models", "model.zip");

        private bool _init;
        private readonly MLContext _mlContext;
        private PredictionEngine<ChatQuestion, QuestionPrediction> _predEngine;
        private ITransformer _trainedModel;

        public ChatManager()
        {
            _mlContext = new MLContext(seed: 0);
        }

        private static IEnumerable<ChatQuestion> LoadData(string filePath)
        {
            var trainingDataText = File.ReadAllLines(@"C:\Users\CorrM\Desktop\train.txt");

            var ret = trainingDataText
                .Where(s => !s.StartsWith("//") && !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Split(new[] { "||CorrM||" }, StringSplitOptions.None))
                .Select(d => new ChatQuestion { QuestionText = d[0], QuestionType = int.Parse(d[1]) })
                .ToList();

            return ret;
        }
        private static IEnumerable<ChatQuestion> LoadTrainData()
        {
            return LoadData(@"C:\Users\CorrM\Desktop\train.txt");
        }
        private static IEnumerable<ChatQuestion> LoadTestData()
        {
            return LoadData(@"C:\Users\CorrM\Desktop\test.txt");
        }

        public Task Init()
        {
            return Task.Run(() => 
            {
                var trainingDataView = _mlContext.Data.LoadFromEnumerable(LoadTrainData());
                BuildAndTrainModel(trainingDataView);
                // Evaluate(trainingDataView.Schema); /* Test and get Accuracy*/

                // Must be last
                _init = true;
            });
        }
        private IEstimator<ITransformer> PreparePipLine(string inputName, string outName)
        {
            // Use the multi-class SDCA algorithm to predict the label using features.
            // For StochasticDualCoordinateAscent the KeyToValue needs to be PredictedLabel.

            return _mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: inputName, outputColumnName: "Label")
                .Append(_mlContext.Transforms.Text.FeaturizeText(inputColumnName: "QuestionText", outputColumnName: "Features"))
                .AppendCacheCheckpoint(_mlContext)
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("Label", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue(outName, "PredictedLabel"));
        }
        private void BuildAndTrainModel(IDataView trainingDataView)
        {
            var pipeline = PreparePipLine("QuestionType", "PredictedType");

            _trainedModel = pipeline.Fit(trainingDataView);
            _predEngine = _mlContext.Model.CreatePredictionEngine<ChatQuestion, QuestionPrediction>(_trainedModel);
        }
        private void Evaluate(DataViewSchema trainingDataViewSchema)
        {
            Console.WriteLine($@"=============== Evaluating to get model's accuracy metrics - Starting time: {DateTime.Now.ToString(CultureInfo.InvariantCulture)} ===============");
            var testDataView = _mlContext.Data.LoadFromEnumerable(LoadTestData());
            var testMetrics = _mlContext.MulticlassClassification.Evaluate(_trainedModel.Transform(testDataView), "Label");

            Console.WriteLine($@"=============== Evaluating to get model's accuracy metrics - Ending time: {DateTime.Now.ToString(CultureInfo.InvariantCulture)} ===============");
            Console.WriteLine($@"*************************************************************************************************************");
            Console.WriteLine($@"*       Metrics for Multi-class Classification model - Test Data     ");
            Console.WriteLine($@"*------------------------------------------------------------------------------------------------------------");
            Console.WriteLine($@"*       MicroAccuracy:    {testMetrics.MicroAccuracy:0.###}");
            Console.WriteLine($@"*       MacroAccuracy:    {testMetrics.MacroAccuracy:0.###}");
            Console.WriteLine($@"*       LogLoss:          {testMetrics.LogLoss:#.###}");
            Console.WriteLine($@"*       LogLossReduction: {testMetrics.LogLossReduction:#.###}");
            Console.WriteLine($@"*************************************************************************************************************");

            // Save the new model to .ZIP file
            //SaveModelAsFile(_mlContext, trainingDataViewSchema, _trainedModel);
        }
        private static void SaveModelAsFile(MLContext mlContext, DataViewSchema trainingDataViewSchema, ITransformer model)
        {
            mlContext.Model.Save(model, trainingDataViewSchema, ModelPath);
            Console.WriteLine(@"The model is saved to {0}", ModelPath);
        }

        public Task<QuestionPrediction> PredictQuestion(string question)
        {
            if (!_init)
                throw new Exception("Call Init function first");

            // Load model form file
            // ITransformer loadedModel = _mlContext.Model.Load(_modelPath, out var modelInputSchema);

            ChatQuestion singleQuestion = new ChatQuestion() { QuestionText = question.ToLower() };
            foreach (var s in question.ToLower().Split(' '))
            {
                if (!ChatTasks.ContainsKey(s)) continue;

                singleQuestion.QuestionTask = (int)ChatTasks[s];
                break;
            }

            return Task.Run(() => _predEngine.Predict(singleQuestion));
        }
    }
}

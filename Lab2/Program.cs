using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Lab2
{
    internal class Program
    {
        private const char CsvDelimiter = ';';
        private const int JokePartsCount = 3;

        private static readonly Logger _logger;

        static Program()
        {
            _logger = new Logger($"log_{DateTime.UtcNow:yyyy-MM-dd}.txt");
        }

        private static void Main(string[] args)
        {
            LogLine();
            LogLine("-----");
            LogLine(DateTime.UtcNow.ToString("yyyy-MM-dd-HH:mm"));
            LogLine();

            if (args is null || args.Length != 2)
            {
                return;
            }

            var learnDataFilePath = args[0];

            if (!File.Exists(learnDataFilePath))
            {
                return;
            }

            var testDataFilePath = args[1];

            if (!File.Exists(testDataFilePath))
            {
                return;
            }

            LogLine($"Learn data '{learnDataFilePath}'");
            LogLine($"Test data '{testDataFilePath}'");
            LogLine();

            var learnJokes = ReadJokesClassed(learnDataFilePath);

            if (learnJokes is null)
            {
                LogLine("An error has occurred");

                return;
            }

            var totalJokesCount = learnJokes.Sum(x => x.Value.Count);
            var totalWordsCount = learnJokes.Sum(x => x.Value.Sum(x => x.Words.Count));

            LogLine("Read Jokes to learn");
            LogLine($"Total jokes count '{totalJokesCount}'");
            LogLine($"Total words count '{totalWordsCount}'");
            LogLine();

            var words = learnJokes.ToDictionary(x => x.Key, x => x.Value.SelectMany(y => y.Words).ToList());
            
            var wordsCount = 
                learnJokes
                    .ToDictionary
                    (
                        x => x.Key,
                        x => 
                            x.Value
                                .SelectMany(y => y.Words)
                                .GroupBy(y => y)
                                .ToDictionary(y => y.Key, y => y.Count())
                    );

            foreach (var (jokeClass, jokes) in learnJokes)
            {
                LogLine($"Class '{jokeClass}'");
                LogLine($"Jokes count '{jokes.Count}'");
                LogLine($"Words count '{words[jokeClass].Count}'");
                LogLine();
            }

            var testJokes = ReadJokes(testDataFilePath);

            if (testJokes is null)
            {
                LogLine("An error has occurred");

                return;
            }

            LogLine($"Loaded test jokes (count '{testJokes.Count}')");
            LogLine();

            var uniqueWordsCount = wordsCount.Sum(x => x.Value.Count);
            var classifyResults = new List<(int JokeClass, double ClassifyResult)>();
            var hits = new List<bool>();

            foreach (var testJoke in testJokes)
            {
                LogLine($"Joke number '{testJoke.Number}' (actual class '{testJoke.Class}')");

                classifyResults.Clear();

                foreach (var jokeClass in learnJokes.Keys)
                {
                    var classifyResult =
                        Classify
                        (
                            testJoke.Words, 
                            learnJokes[jokeClass].Count, 
                            totalJokesCount, 
                            uniqueWordsCount,
                            words[jokeClass].Count, 
                            wordsCount[jokeClass]
                        );

                    classifyResults.Add((jokeClass, classifyResult));

                    LogLine($"Classified to class '{jokeClass}' with classify result '{classifyResult}'");
                }

                var maxClassifyResult = classifyResults[0].ClassifyResult;
                var classifyJokeClass = classifyResults[0].JokeClass;

                for (var i = 1; i < classifyResults.Count; ++i)
                {
                    // ReSharper disable once InvertIf
                    if (classifyResults[i].ClassifyResult > maxClassifyResult)
                    {
                        maxClassifyResult = classifyResults[i].ClassifyResult;
                        classifyJokeClass = classifyResults[i].JokeClass;
                    }
                }

                LogLine($"Joke is probably belongs to '{classifyJokeClass}' class");

                var hit = classifyJokeClass == testJoke.Class;

                hits.Add(hit);

                LogLine(hit.ToString());

                LogLine();
            }

            var hitsCount = hits.Count(x => x is true);

            LogLine($"Hits count '{hitsCount}' of '{testJokes.Count}'");
            LogLine();

            LogLine("-----");
            LogLine();
        }

        private static Dictionary<int, List<Joke>> ReadJokesClassed(string filePath)
        {
            string text;

            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                return null;
            }

            var split = text.Split(new [] { Environment.NewLine, CsvDelimiter.ToString() }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (split.Length % 3 != 0)
            {
                return null;
            }
            
            var result = new Dictionary<int, List<Joke>>();

            for (var i = 0; i < split.Length; i += 3)
            {
                if (!int.TryParse(split[i], out var number) || !int.TryParse(split[i + 2], out var jokeClass))
                {
                    continue;
                }

                var jokeText = split[i + 1];

                var joke = new Joke
                {
                    Number = number,
                    Text = jokeText,
                    Class = jokeClass,
                    Words = RetrieveWords(jokeText)
                };

                if (result.TryGetValue(jokeClass, out var jokes))
                {
                    jokes.Add(joke);
                }
                else
                {
                    result.Add(joke.Class, new List<Joke> { joke });
                }
            }

            return result;
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static List<Joke> ReadJokes(string filePath)
        {
            string text;

            try
            {
                text = File.ReadAllText(filePath);
            }
            catch (Exception)
            {
                return null;
            }

            var split = text.Split(new[] { Environment.NewLine, CsvDelimiter.ToString() }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (split.Length % 3 != 0)
            {
                return null;
            }

            var result = new List<Joke>();

            for (var i = 0; i < split.Length; i += 3)
            {
                if (!int.TryParse(split[i], out var number) || !int.TryParse(split[i + 2], out var jokeClass))
                {
                    continue;
                }

                var jokeText = split[i + 1];

                var joke = new Joke
                {
                    Number = number,
                    Text = jokeText,
                    Class = jokeClass,
                    Words = RetrieveWords(jokeText)
                };

                result.Add(joke);
            }

            return result;
        }

        private static List<string> RetrieveWords(string text)
        {
            var result = new List<string>();

            var lastWordIndex = 0;

            for (var i = 0; i < text.Length; ++i)
            {
                if (char.IsLetter(text[i]))
                {
                    continue;
                }

                if (lastWordIndex != i)
                {
                    result.Add(text[lastWordIndex..i].ToLower());
                }

                lastWordIndex = i + 1;
            }

            return result;
        }

        private static double Classify(IEnumerable<string> words, int classJokesCount, int totalJokesCount, int uniqueWordsCount, int classTotalWordsCount, IReadOnlyDictionary<string, int> wordsCount)
        {
            var left = Math.Log((double)classJokesCount / totalJokesCount);
            var right = words.Sum(word => Math.Log((GetValueOrCustom(wordsCount, word, 0) + 1d) / (uniqueWordsCount + classTotalWordsCount)));
            var result = left + right;

            return result;
        }

        private static TValue GetValueOrCustom<TKey, TValue>(IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue customValue)
        {
            return dictionary.TryGetValue(key, out var value) ? value : customValue;
        }

        private static void LogLine()
        {
            Console.WriteLine();

            _logger.LogLine();
        }

        private static void LogLine(string text)
        {
            Console.WriteLine(text);

            _logger.LogLine(text);
        }
    }
}

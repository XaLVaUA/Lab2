using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Lab2
{
    internal class Program
    {
        private const char CsvDelimiter = ';';
        private const int JokePartsCount = 3;

        private static void Main(string[] args)
        {
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

            var learnJokes = ReadJokesClassed(learnDataFilePath);

            if (learnJokes is null)
            {
                Console.WriteLine("An error has occurred");

                return;
            }

            var totalJokesCount = learnJokes.Sum(x => x.Value.Count);
            var totalWordsCount = learnJokes.Sum(x => x.Value.Sum(x => x.Words.Count));

            Console.WriteLine("Read Jokes to learn");
            Console.WriteLine($"Total jokes count '{totalJokesCount}'");
            Console.WriteLine($"Total words count '{totalWordsCount}'");
            Console.WriteLine();

            var words = learnJokes.ToDictionary(x => x.Key, x => x.Value.SelectMany(y => y.Words).ToList());
            var wordsCount = learnJokes.ToDictionary
            (
                x => x.Key,
                x => x.Value.SelectMany(y => y.Words).GroupBy(y => y).ToDictionary(y => y.Key, y => y.Count())
            );

            foreach (var (jokeClass, jokes) in learnJokes)
            {
                Console.WriteLine($"Class '{jokeClass}'");
                Console.WriteLine($"Jokes count '{jokes.Count}'");
                Console.WriteLine($"Words count '{words[jokeClass].Count}'");
                Console.WriteLine();
            }
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

                var joke = new Joke
                {
                    Number = number,
                    Text = split[i + 1],
                    Class = jokeClass
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
    }
}

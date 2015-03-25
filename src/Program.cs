using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SqlServer.Server;

namespace WordPlay {
    class Program {

        public enum TypeOfWord {
            ProperNoun,
            EndsInLy,
            EndsInS,
            CommonWord,
            None
        }

        static void Main(string[] args) {
            var lines = File.ReadAllLines("linuxwords.txt");
            var structures = File.ReadAllLines("HaikuBase.txt");
            StartHaikuGenerator(lines, structures);
        }

        /// <summary>
        /// Starts the haiku generator.
        /// </summary>
        /// <param name="wordsToUse">The words that need to be used in the Haiku.</param>
        /// <param name="structures">The lines that provide the structure.</param>
        private static void StartHaikuGenerator(IEnumerable<string> wordsToUse, IList<string> structures) {
            // Creating a dictionary of syllables sorted by the number of syllables and all respective words
            var sylDict = new Dictionary<int, List<string>>();
            foreach (var w in wordsToUse) {
                var sCount = SyllableCount(w);
                if (sylDict.ContainsKey(sCount)) {
                    sylDict[sCount].Add(w);
                }
                else {
                    sylDict.Add(sCount, new List<string>() { w });
                }
            }

            // Our fabulous random number generator.
            var rnd = new Random(DateTime.Now.Millisecond);

            // Defining the syllable pattern for a Haiku
            var pattern = new int[] { 5, 7, 5 };

            // Going into the do-while loop now
            var key = 'C';

            // Array used to split line into words
            var lineSplitArray = new char[] { ' ', '.', '-', ',', '!' };

            do {
                Console.Clear();
                PrintToScreenCenter("Random Haiku Generator");
                PrintToScreenCenter("By Saurabh Jain");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                PrintToScreenCenter("--");

                // Picking up a random base to use for the Haiku
                var structure = structures[rnd.Next(structures.Count)];

                // Splitting the structure into lines
                var lines = structure.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                var haiku = new List<string>();

                for (var i = 0; i < lines.Length; i++) {
                    // Number of syllables required for this line
                    var syllablesRemaining = pattern[i];

                    // Splitting the line into words
                    var words = lines[i].Split(lineSplitArray, StringSplitOptions.RemoveEmptyEntries);

                    // Going through all the words and calculating the actual syllables remaining
                    foreach (var w in words) {
                        if (!w.StartsWith("<")) {
                            syllablesRemaining -= SyllableCount(w);
                        }
                    }

                    // Finding the number of words to be inserted in this line
                    var numOfWordsToInsert = lines[i].Count(x => x == '<');

                    // Defining a string builder to build the line
                    var sbLine = new StringBuilder();

                    // Keep completing the words until you can
                    while (syllablesRemaining != 0) {
                        foreach (var w in words) {

                            var syllablesToFill = syllablesRemaining;
                            if (numOfWordsToInsert > 1) {
                                syllablesToFill = rnd.Next(1, (syllablesRemaining - numOfWordsToInsert));
                            }

                            var typeOfWord = TypeOfWord.None;

                            switch (w) {
                                case "<W>":
                                    // Fill with any word meeting the requirements
                                    typeOfWord = TypeOfWord.CommonWord;
                                    break;

                                case "<P>":
                                    // Fill with a proper noun
                                    typeOfWord = TypeOfWord.ProperNoun;
                                    break;

                                case "<S>":
                                    // Word should end with S
                                    typeOfWord = TypeOfWord.EndsInS;
                                    break;

                                case "<L>":
                                    // Word should end with LY
                                    typeOfWord = TypeOfWord.EndsInLy;
                                    break;
                            }

                            if (typeOfWord != TypeOfWord.None) {
                                sbLine.AppendFormat("{0} ", GetWord(sylDict, typeOfWord, rnd, syllablesToFill));
                                syllablesRemaining -= syllablesToFill;
                                numOfWordsToInsert--;
                            }
                            else {
                                sbLine.AppendFormat("{0} ", w);
                            }
                        }
                    }

                    haiku.Add(CapitalizeFirtChar(sbLine.ToString()));
                }

                PrintBeautifulHaiku(haiku);
                PrintToScreenCenter("--");
                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
                PrintToScreenCenter("Hit `esc` to quit or any other key to generate another awesome haiku...");
                key = Console.ReadKey(true).KeyChar;

            } while (key != (char)27);
        }

        /// <summary>
        /// Prints the beautiful haiku.
        /// </summary>
        /// <param name="lines">The lines.</param>
        private static void PrintBeautifulHaiku(IEnumerable<string> lines) {
            var columns = 80;
            foreach (var l in lines) {
                PrintToScreenCenter(l);
            }
        }

        /// <summary>
        /// Prints to screen center.
        /// </summary>
        /// <param name="l">The l.</param>
        private static void PrintToScreenCenter(string l) {
            Console.Write(new string(' ', (Console.WindowWidth - l.Trim().Length) / 2));
            Console.WriteLine(l.Trim());
        }

        /// <summary>
        /// Gets the word depending on the parameters.
        /// </summary>
        /// <param name="sylDict">The syl dictionary.</param>
        /// <param name="properNouns">The proper nouns.</param>
        /// <param name="endingInLy">The ending in ly.</param>
        /// <param name="endingInS">The ending in s.</param>
        /// <param name="typeOfWord">The type of word.</param>
        /// <param name="rnd">The random.</param>
        /// <param name="syllablesToFill">The syllables to fill.</param>
        /// <returns></returns>
        private static string GetWord(IReadOnlyDictionary<int, List<string>> sylDict, TypeOfWord typeOfWord, Random rnd, int syllablesToFill) {
            var possibleWords = new List<string>();
            switch (typeOfWord) {
                case TypeOfWord.CommonWord:
                    possibleWords = sylDict[syllablesToFill].Where(x => Char.IsLower(x[0])).ToList();
                    break;

                case TypeOfWord.EndsInLy:
                    possibleWords = sylDict[syllablesToFill].Where(x => x.EndsWith("ly")).ToList();
                    break;

                case TypeOfWord.EndsInS:
                    possibleWords = sylDict[syllablesToFill].Where(x => x.EndsWith("s")).ToList();
                    break;

                case TypeOfWord.ProperNoun:
                    possibleWords = sylDict[syllablesToFill].Where(x => Char.IsUpper(x[0])).ToList();
                    break;
            }

            // Checking if there is atleast one entry in the possible words.
            // If not, then just return any word which the syllable matches.
            var result = String.Empty;
            if (possibleWords.Count > 0) {
                result = possibleWords[rnd.Next(possibleWords.Count)];
            }

            if (sylDict[syllablesToFill].Count > 1) {
                result = sylDict[syllablesToFill][rnd.Next(sylDict[syllablesToFill].Count)];
            }

            // Can't return anything. But we have to return something.
            if (String.IsNullOrEmpty(result)) {
                var sb = new StringBuilder();
                for (var i = 0; i < syllablesToFill; i++) {
                    sb.Append("Blah");
                }

                // Written "Blah" x number of times in the worst case scenario
                result = sb.ToString();
            }

            return result;
        }

        /// <summary>
        /// Counts the approximate number of syllables in a word.
        /// Not accurate - but decent enough.
        /// Credit: http://codereview.stackexchange.com/a/9974/63379
        /// </summary>
        /// <param name="word">The word.</param>
        /// <returns></returns>
        private static int SyllableCount(string word) {
            word = word.ToLower().Trim();
            var lastWasVowel = false;
            var vowels = new[] { 'a', 'e', 'i', 'o', 'u', 'y' };
            var count = 0;

            //a string is an IEnumerable<char>; convenient.
            foreach (var c in word) {
                if (vowels.Contains(c)) {
                    if (!lastWasVowel) {
                        count++;
                    }

                    lastWasVowel = true;
                }
                else {
                    lastWasVowel = false;
                }
            }

            if ((word.EndsWith("e") || (word.EndsWith("es") || word.EndsWith("ed"))) && !word.EndsWith("le")) {
                count--;
            }

            return count;
        }

        /// <summary>
        /// Makes the first character upper.
        /// </summary>
        /// <param name="str">The string.</param>
        /// <returns></returns>
        private static string CapitalizeFirtChar(string str) {
            if (str == null) {
                return null;
            }

            if (str.Length > 1) {
                return char.ToUpper(str[0]) + str.Substring(1);
            }

            return str.ToUpper();
        }
    }
}

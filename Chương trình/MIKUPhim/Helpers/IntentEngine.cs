using System.Text.Json;

namespace MIKUPhim.Helpers
{
    public class IntentEngine
    {
        private List<Intent> intents;

        public IntentEngine()
        {
            LoadDataset();
        }

        // ===============================
        // Load intent dataset
        // ===============================

        private void LoadDataset()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "AI", "intent_dataset.json");

            var json = File.ReadAllText(path);

            var dataset = JsonSerializer.Deserialize<IntentDataset>(json);

            if (dataset == null || dataset.intents == null)
            {
                intents = new List<Intent>();
            }
            else
            {
                intents = dataset.intents;
            }
        }

        // ===============================
        // Detect intent
        // ===============================

        public string DetectIntent(string message)
        {
            message = NormalizeText(message);

            var tokens = Tokenize(message);

            double bestScore = 0;

            string bestIntent = "fallback";

            foreach (var intent in intents)
            {
                foreach (var pattern in intent.patterns)
                {
                    double score = CalculateSimilarity(tokens, pattern);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestIntent = intent.tag;
                    }
                }
            }

            // threshold để tránh nhận sai intent
            if (bestScore < 0.35)
                return "fallback";

            Console.WriteLine("Intent detected: " + bestIntent + " score=" + bestScore);
            return bestIntent;
        }

        // ===============================
        // Normalize text
        // ===============================

        private string NormalizeText(string text)
        {
            text = text.ToLower();

            text = text.Replace("?", "")
                       .Replace(".", "")
                       .Replace(",", "")
                       .Replace("!", "");

            return text;
        }

        // ===============================
        // Tokenize
        // ===============================

        private List<string> Tokenize(string text)
        {
            return text.Split(' ')
                       .Where(x => !string.IsNullOrWhiteSpace(x))
                       .ToList();
        }

        // ===============================
        // Similarity
        // ===============================

        private double CalculateSimilarity(List<string> tokens, string pattern)
        {
            var patternTokens = pattern.Split(' ');

            int match = 0;

            foreach (var token in tokens)
            {
                foreach (var p in patternTokens)
                {
                    if (IsSimilar(token, p))
                    {
                        match++;
                        break;
                    }
                }
            }

            return (double)match / patternTokens.Length;
        }

        // ===============================
        // xử lý sai chính tả nhẹ
        // ===============================

        private bool IsSimilar(string a, string b)
        {
            if (a == b)
                return true;

            int distance = LevenshteinDistance(a, b);

            return distance <= 1;
        }

        // ===============================
        // thuật toán Levenshtein
        // ===============================

        private int LevenshteinDistance(string s, string t)
        {
            int[,] d = new int[s.Length + 1, t.Length + 1];

            for (int i = 0; i <= s.Length; i++)
                d[i, 0] = i;

            for (int j = 0; j <= t.Length; j++)
                d[0, j] = j;

            for (int i = 1; i <= s.Length; i++)
            {
                for (int j = 1; j <= t.Length; j++)
                {
                    int cost = (s[i - 1] == t[j - 1]) ? 0 : 1;

                    d[i, j] = Math.Min(
                        Math.Min(
                            d[i - 1, j] + 1,
                            d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost
                    );
                }
            }

            return d[s.Length, t.Length];
        }
    }

    // ===============================
    // Dataset classes
    // ===============================

    public class IntentDataset
    {
        public List<Intent> intents { get; set; }
    }

    public class Intent
    {
        public string tag { get; set; }

        public List<string> patterns { get; set; }
    }
}
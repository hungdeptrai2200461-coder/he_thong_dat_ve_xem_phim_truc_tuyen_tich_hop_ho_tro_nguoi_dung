using System.Text;
using System.Text.Json;

namespace MIKUPhim.Helpers
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(IConfiguration configuration)
        {
            _httpClient = new HttpClient();

            _apiKey = configuration["Gemini:ApiKey"];
        }

        // =====================================
        // AI Rewrite Response
        // =====================================

        public async Task<string> RewriteResponse(string rawText)
        {
            string prompt = $@"
Bạn là chatbot của website đặt vé phim MIKUPhim.

Hãy viết lại câu sau cho tự nhiên và thân thiện.

QUY TẮC:
- Chỉ trả lời 1 câu duy nhất
- Không giải thích
- Không đưa nhiều lựa chọn
- Không liệt kê
- Không thay đổi dữ liệu

Câu cần viết lại:
{rawText}
";

            return await AskGemini(prompt);
        }

        // =====================================
        // AI Fallback
        // =====================================

        public async Task<string> AskFallback(string userMessage)
        {
            string prompt = $@"
Bạn là chatbot của rạp phim MIKUPhim.

Người dùng hỏi:
{userMessage}

Hãy trả lời thân thiện và liên quan đến phim nếu có thể.
Không được bịa dữ liệu rạp.

Nếu câu hỏi không liên quan đến phim thì trả lời nhẹ nhàng.
";

            return await AskGemini(prompt);
        }

        // =====================================
        // AI Recommendation
        // =====================================

        public async Task<string> RecommendMovie(string userMessage)
        {
            string prompt = $@"
Bạn là chatbot gợi ý phim.

Người dùng nói:
{userMessage}

Hãy gợi ý một vài bộ phim phù hợp với tâm trạng.

Trả lời tự nhiên và ngắn gọn.
";

            return await AskGemini(prompt);
        }

        // =====================================
        // Call Gemini API
        // =====================================

        private async Task<string> AskGemini(string prompt)
        {
            var url =
 $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_apiKey}";
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);

            var responseJson = await response.Content.ReadAsStringAsync();
            responseJson = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(responseJson));
            Console.WriteLine("Status: " + response.StatusCode);
            Console.WriteLine(responseJson);

            if (!response.IsSuccessStatusCode)
            {
                return "Xin lỗi, AI hiện đang bận. Bạn thử lại sau nhé.";
            }

            using var doc = JsonDocument.Parse(responseJson);

            if (!doc.RootElement.TryGetProperty("candidates", out var candidates))
            {
                return "AI chưa phản hồi.";
            }

            var result =
                candidates[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            return result ?? "AI chưa trả lời.";
        }
    }
}
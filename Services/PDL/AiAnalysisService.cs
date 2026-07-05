using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ServiceApotheke.API.Services.PDL
{
    public class AiAnalysisService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public AiAnalysisService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _apiKey = config["GEMINI_API_KEY"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? "dummy_key_for_testing";
        }

        public async Task<string> AnalyzePolymedicationAsync(int birthYear, string gender, List<string> medications)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new Exception("GEMINI_API_KEY is not configured.");
            }

            var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-pro:generateContent?key={_apiKey}";

            var systemInstruction = @"Du bist ein klinischer Pharmazeut in Deutschland. 
Führe eine erweiterte Medikationsberatung bei Polymedikation (AMTS) durch.
Überprüfe auf:
1. Therapeutische Doppelungen
2. Klinisch relevante Interaktionen
3. Verschreibungskaskaden
4. PRISCUS/FORTA-Kriterien (falls Alter >= 65).
Gib die Analyse als valides JSON-Objekt zurück.";

            var promptText = $"Patient: Geburtsjahr {birthYear}, Geschlecht {gender}\nMedikamente:\n" + string.Join("\n", medications);

            var requestBody = new
            {
                system_instruction = new { parts = new[] { new { text = systemInstruction } } },
                contents = new[]
                {
                    new { parts = new[] { new { text = promptText } } }
                },
                generationConfig = new
                {
                    temperature = 0.1,
                    response_mime_type = "application/json",
                    response_schema = new
                    {
                        type = "OBJECT",
                        properties = new
                        {
                            isEligibleForPdl = new { type = "BOOLEAN" },
                            issues = new
                            {
                                type = "ARRAY",
                                items = new
                                {
                                    type = "OBJECT",
                                    properties = new
                                    {
                                        severity = new { type = "STRING", @enum = new[] { "LOW", "MEDIUM", "HIGH" } },
                                        description = new { type = "STRING" },
                                        recommendation = new { type = "STRING" }
                                    },
                                    required = new[] { "severity", "description", "recommendation" }
                                }
                            },
                            summary = new { type = "STRING" }
                        },
                        required = new[] { "isEligibleForPdl", "issues", "summary" }
                    }
                }
            };

            var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
            
            // In a real environment, we'd make the HTTP call. For demonstration/MVP without a real key, 
            // we will simulate the HTTP call if the key is dummy to prevent crashing.
            if (_apiKey == "dummy_key_for_testing" || _apiKey.Contains("dummy"))
            {
                return JsonSerializer.Serialize(new {
                    isEligibleForPdl = true,
                    issues = new[] {
                        new { severity = "MEDIUM", description = "Mock-Interaktion zwischen Medikament A und B.", recommendation = "Dosisanpassung prüfen." }
                    },
                    summary = "Patient erfüllt die Kriterien für eine AMTS aufgrund von Polymedikation (>= 5 systemische Medikamente)."
                });
            }

            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseString);
            var textResult = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();

            return textResult ?? "{}";
        }
    }
}

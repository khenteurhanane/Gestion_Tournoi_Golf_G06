using System.Net.Http.Json;
using System.Text.Json;

namespace croupe_06_TournoiGolf.Services
{
    /// <summary>
    /// Service chargé de récupérer les données météorologiques en temps réel.
    /// Utilise l'API externe gratuite Open-Meteo sans nécessiter de clé API.
    /// </summary>
    public class WeatherService
    {
        private readonly HttpClient _httpClient;
        private const string LAT = "45.5017";
        private const string LON = "-73.5673";
        private const string CITY = "Montréal, QC";

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Récupère la météo actuelle pour des coordonnées GPS données.
        /// Par défaut, utilise les coordonnées de Montréal.
        /// </summary>
        /// <param name="lat">Latitude optionnelle</param>
        /// <param name="lon">Longitude optionnelle</param>
        /// <returns>Un objet WeatherData contenant les infos formatées ou null en cas d'erreur.</returns>
        public async Task<WeatherData?> GetCurrentWeatherAsync(string? lat = null, string? lon = null)
        {
            try
            {
                // Sélection des coordonnées (défaut ou paramètres fournis)
                string latitude = lat ?? "45.5017";
                string longitude = lon ?? "-73.5673";
                string cityLabel = (lat != null && lon != null) ? "Ma position" : "Montréal, QC";

                // Construction de l'URL d'appel avec les paramètres météo requis (température, vent, humidité, UV, pluie)
                string url = $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,apparent_temperature,weather_code,wind_speed_10m,relative_humidity_2m&hourly=precipitation_probability,uv_index&timezone=America/Toronto&forecast_days=1";
                
                // Appel HTTP asynchrone et récupération du JSON
                var data = await _httpClient.GetFromJsonAsync<JsonElement>(url);
                
                var current = data.GetProperty("current");
                var hourly = data.GetProperty("hourly");
                
                // Extraction de l'heure actuelle pour faire correspondre avec les prévisions horaires (pluie, UV)
                string timeStr = current.GetProperty("time").GetString()!;
                string currentHourStr = timeStr.Substring(0, 13) + ":00";
                
                int hourIndex = 0;
                var times = hourly.GetProperty("time");
                for (int i = 0; i < times.GetArrayLength(); i++)
                {
                    if (times[i].GetString() == currentHourStr)
                    {
                        hourIndex = i;
                        break;
                    }
                }

                // Construction de l'objet de données simplifié pour le frontend
                return new WeatherData
                {
                    Temp = (float)current.GetProperty("temperature_2m").GetDouble(),
                    ApparentTemp = (float)current.GetProperty("apparent_temperature").GetDouble(),
                    WeatherCode = current.GetProperty("weather_code").GetInt32(),
                    WindSpeed = (float)current.GetProperty("wind_speed_10m").GetDouble(),
                    Humidity = current.GetProperty("relative_humidity_2m").GetInt32(),
                    RainProb = hourly.GetProperty("precipitation_probability")[hourIndex].GetInt32(),
                    UvIndex = (float)hourly.GetProperty("uv_index")[hourIndex].GetDouble(),
                    City = cityLabel
                };
            }
            catch (Exception ex)
            {
                // En cas d'erreur réseau ou de format JSON, on log l'erreur et on retourne null
                Console.WriteLine($"[WeatherService] Erreur API : {ex.Message}");
                return null;
            }
        }
    }


    public class WeatherData
    {
        public float Temp { get; set; }
        public float ApparentTemp { get; set; }
        public int WeatherCode { get; set; }
        public float WindSpeed { get; set; }
        public int Humidity { get; set; }
        public int RainProb { get; set; }
        public float UvIndex { get; set; }
        public string City { get; set; } = string.Empty;
    }
}

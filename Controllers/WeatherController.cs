using croupe_06_TournoiGolf.Services;
using Microsoft.AspNetCore.Mvc;

namespace croupe_06_TournoiGolf.Controllers
{
    public class WeatherController(croupe_06_TournoiGolf.Services.WeatherService weatherService) : Controller
    {
        private readonly croupe_06_TournoiGolf.Services.WeatherService _weatherService = weatherService;

        [HttpGet]
        public async Task<IActionResult> GetWeather(string? lat, string? lon)
        {
            var weather = await _weatherService.GetCurrentWeatherAsync(lat, lon);
            if (weather == null) return NotFound();
            return Json(weather);
        }
    }
}

using System.ComponentModel.DataAnnotations;

namespace Device.FingerPrint.TransactionAPI
{
    /// <summary>
    /// Weather forecast data model
    /// </summary>
    public class WeatherForecast
    {
        /// <summary>
        /// The date of the weather forecast
        /// </summary>
        [Required]
        public DateOnly Date { get; set; }

        /// <summary>
        /// Temperature in Celsius
        /// </summary>
        [Range(-100, 100)]
        public int TemperatureC { get; set; }

        /// <summary>
        /// Temperature in Fahrenheit (calculated from Celsius)
        /// </summary>
        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        /// <summary>
        /// Weather summary description
        /// </summary>
        public string? Summary { get; set; }
    }
}

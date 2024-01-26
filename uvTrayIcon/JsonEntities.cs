using System.Collections.Generic;
using Newtonsoft.Json;

namespace uvTrayIcon;
public class DataPoint
{
    [JsonProperty("hour")]
    public string Hour { get; set; } //hh:mmtt for current, hhmmtt for graph data points
    [JsonProperty("value")]
    public int Value { get; set; } // 0-14 (never seen 13 and 14 but the assets are there if it exists)
    [JsonProperty("band")]
    public string Band { get; set; } // Low, Moderate, High, Very High
    [JsonProperty("date")]
    public string Date { get; set; } // their timestamp is ISO 8601 (2024-01-26T00:00:00)
    [JsonProperty("color")]
    public string Color { get; set; } // they could have checked this client side this was unnecessary
}
public class Root
{
    [JsonProperty("Date")]
    public string Date; // 26 Jan 2024
    [JsonProperty("CurrentUV")]
    public DataPoint CurrentUV;
    [JsonProperty("UVList")]
    public List<DataPoint> DataPoints;
}

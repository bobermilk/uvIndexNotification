using System.Collections.Generic;
using Newtonsoft.Json;

namespace uvTrayIcon;
public class Index
{
    [JsonProperty("value")]
    public int IndexValue { get; set; }
    [JsonProperty("timestamp")]
    public string IndexTimestamp { get; set; }
}
public class Items
{
    [JsonProperty("timestamp")]
    public string LatestTimestamp;
    [JsonProperty("update_timestamp")]
    public string LatestUpdatedTimestamp;
    [JsonProperty("index")]
    public List<Index> DataPoints;
}
public class RootObject
{
    [JsonProperty("items")]
    public List<Items> Root { get; set; }
}

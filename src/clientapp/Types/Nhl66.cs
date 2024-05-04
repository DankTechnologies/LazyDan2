using Newtonsoft.Json;

namespace LazyDan2.Types;

public class Nhl66Response
{
    public List<Nhl66Game> Games;
}

public class Nhl66Game
{
    [JsonProperty("away_name")]
    public string AwayName;
    [JsonProperty("home_name")]
    public string HomeName;
    [JsonProperty("away_short")]
    public string AwayNameShort;
    [JsonProperty("home_short")]
    public string HomeNameShort;
    [JsonProperty("src_id")]
    public string SourceId;
    public List<Nhl66Stream> Streams;
}

public class Nhl66Stream
{
    public string Seg3;
    public string Url;
    public string MediaId;
    public string Name;
}
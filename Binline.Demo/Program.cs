// This demo project shows Binline in action.
// Newtonsoft.Json is referenced with <Binline>true</Binline>, which means:
// - Its public types are inlined into this assembly as internal types
// - The Newtonsoft.Json.dll file is NOT copied to the output directory
// - This assembly can be distributed without bundling Newtonsoft.Json.dll

using Newtonsoft.Json;

var data = new { Name = "Binline", Version = "0.0.0-dev" };
var json = JsonConvert.SerializeObject(data, Formatting.Indented);

Console.WriteLine("Serialized using inlined Newtonsoft.Json:");
Console.WriteLine(json);

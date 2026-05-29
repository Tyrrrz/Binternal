// This demo project shows Binternal in action.
// Newtonsoft.Json is referenced with Internalize="true", which means:
// - Its public types are merged into this assembly as internal types
// - The Newtonsoft.Json.dll file is NOT copied to the output directory
// - This assembly can be distributed without bundling Newtonsoft.Json.dll

using Newtonsoft.Json;

var data = new { Name = "Binternal", Version = "0.0.0-dev" };
var json = JsonConvert.SerializeObject(data, Formatting.Indented);

Console.WriteLine("Serialized using internalized Newtonsoft.Json:");
Console.WriteLine(json);

using System.Reflection;
using Newtonsoft.Json;

var data = new
{
    Name = "Binternal",
    Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
};

var json = JsonConvert.SerializeObject(data, Formatting.Indented);
Console.WriteLine(json);

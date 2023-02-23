// See https://aka.ms/new-console-template for more information

using System.Text;
using AngleSharp;
using BookParser;
using Newtonsoft.Json;
using Parser;

Console.WriteLine("Hello, World!");

Parser.BookParser bookParser = new Parser.BookParser();

List<Book> parsedBooks = new List<Book>();
List<Url> unloadedPages = new List<Url>();
var resCortages = new List<(List<Book>, List<Url>)>();
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/vysshee_i_sredne_spetsialnoe_obrazovanie/?PAGEN_1=","primbook", 1, 152, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/detskaya/?PAGEN_1=","primbook",  1, 592, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/dom_byt_dosug/?PAGEN_1=", "primbook", 1, 112, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/doshkolno_metodicheskaya/?PAGEN_1=","primbook",  1, 227, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/istoricheskaya/?PAGEN_1=","primbook",  1, 65, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/meditsina//?PAGEN_1=","primbook",  1, 48, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/slovari_razgovorniki/?PAGEN_1=","primbook",  1, 20, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/spetsializirovannaya/?PAGEN_1=","primbook",  1, 210, 10));
resCortages.Add(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/spravochnaya/?PAGEN_1=","primbook",  1, 3, 1));
resCortages.Add( bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/khudozhestvennaya/?PAGEN_1=","primbook",  1, 536, 10));
resCortages.Add( bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/shkolno_metodicheskaya/?PAGEN_1=","primbook",  1, 84, 10));
resCortages.Add( bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/shkolnye_uchebniki/?PAGEN_1=","primbook",  1, 356, 10));
resCortages.Add( bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/entsiklopediya_nauchno_populyarnaya_literatura/?PAGEN_1=","primbook",  1, 67, 10));
foreach (var i in resCortages)
{
    parsedBooks.AddRange(i.Item1);
    unloadedPages.AddRange(i.Item2);
}
WriteToJSON($"Primbook-Books-{DateTime.Today.ToShortDateString()}.json", parsedBooks);
WriteToJSON($"Primbook-UnloadedPages-{DateTime.Today.ToShortDateString()}.json", unloadedPages);
Console.ReadLine();


void WriteToJSON<T>(string path, List<T> collections)
{
    var json = JsonConvert.SerializeObject(collections);
    File.WriteAllText(path, json, Encoding.UTF8);
    Console.WriteLine("Books writed to json, count "+collections.Count);
    File.WriteAllText(path+"Count",Convert.ToString(collections.Count));
}
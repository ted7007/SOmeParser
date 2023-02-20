// See https://aka.ms/new-console-template for more information

using System.Text;
using AngleSharp;
using BookParser;
using Newtonsoft.Json;
using Parser;

Console.WriteLine("Hello, World!");

Parser.BookParser bookParser = new Parser.BookParser();

List<Book> parsedBooks = new List<Book>();
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/vysshee_i_sredne_spetsialnoe_obrazovanie/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/detskaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/dom_byt_dosug/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/doshkolno_metodicheskaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/istoricheskaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/meditsina//?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/slovari_razgovorniki/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/spetsializirovannaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/spravochnaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/khudozhestvennaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/shkolno_metodicheskaya/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/shkolnye_uchebniki/?PAGEN_1=", 1, 1, 1));
parsedBooks.AddRange(bookParser.StartParsingAsyncNew("https://primbook.ru/catalog/entsiklopediya_nauchno_populyarnaya_literatura/?PAGEN_1=", 1, 1, 1));
WriteToJSON($"Primbook-{DateTime.Today.ToShortDateString()}.json", parsedBooks);

Console.ReadLine();


void WriteToJSON(string path, List<Book> books)
{
    var json = JsonConvert.SerializeObject(books);
    File.WriteAllText(path, json, Encoding.UTF8);
    Console.WriteLine("Books writed to json, count "+books.Count);
    File.WriteAllText(path+"Count",Convert.ToString(books.Count));
}
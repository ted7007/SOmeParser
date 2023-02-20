// See https://aka.ms/new-console-template for more information

using AngleSharp;
using Parser;

Console.WriteLine("Hello, World!");

ParserBook parser = new ParserBook();

parser.StartParsingAsyncNew("https://igraslov.store/shop/?products-per-page=all&", 1, 1, 1);

Console.ReadLine();
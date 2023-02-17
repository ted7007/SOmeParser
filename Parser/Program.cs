// See https://aka.ms/new-console-template for more information

using Parser;

Console.WriteLine("Hello, World!");

ParserBook parser = new ParserBook();

parser.StartParsingAsyncNew("https://tochka24.com/catalog/books?limit=500&page=", 1, 2, 10);
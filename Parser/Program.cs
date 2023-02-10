// See https://aka.ms/new-console-template for more information

using Parser;

Console.WriteLine("Hello, World!");

ParserBook parser = new ParserBook();
parser.StartParsingAsync(100);
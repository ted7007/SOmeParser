using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using AngleSharp;
using AngleSharp.Dom;
using BookParser;
using Newtonsoft.Json;

namespace Parser;

public class ParserBook
{
    public static IDocument GetDocument(string url)
    {
        var config = Configuration.Default.WithDefaultLoader();
        var context = BrowsingContext.New(config);
        return context.OpenAsync(url).Result;
    }
    private async Task<List<Book>> ParseBookInfo(string urlWithCollection,int startPage, int endPage)
    {
        Console.WriteLine($"Started thread since {startPage} to {endPage}");
        var books = new List<Book>();
        DateTime nowThread = DateTime.Now;
        for(int i = startPage; i <= endPage; i++)
        {
            try
            {
                DateTime now = DateTime.Now;
                var document2 = GetDocument(urlWithCollection + Convert.ToString(i));


                var textWitHResultSearchElements =
                    document2.GetElementsByClassName("product-card");
                
                if ((textWitHResultSearchElements.Length == 0))
                {
                    Console.WriteLine($"Page - {i}: Книги не найдены");
                    continue;
                }
                Console.WriteLine($"Page - {i} was readed, count = {textWitHResultSearchElements.Length}");
               // Console.WriteLine("Page - "+(i+1));
                foreach (var bookFromList in textWitHResultSearchElements)
                {
                    var book = ParseICollection(bookFromList);
                    books.Add(book);
                    //Console.WriteLine($"Got book -  {book.Name}; Price - {book.Price}; Remainder - {book.Remainder}");
                    //Console.WriteLine();
                }
                DateTime end = DateTime.Now;
                Console.WriteLine($"Page - {i} was parsed, count in thread = {books.Count}; time - {new TimeSpan((end-now).Ticks).TotalMinutes} min");
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now} : ERROR for page- {ex.Message}");
            }
        }
        DateTime endThread = DateTime.Now;
        Console.WriteLine($"Thread parsed all pages.  time - {new TimeSpan((endThread-nowThread).Ticks).TotalMinutes} min");

        return books;
    }

    private Book ParseICollection(IElement element)
    {
        
        string refToBook = element.GetElementsByTagName("a")[0]
            .Attributes["href"].Value;
        //Console.WriteLine("AddToRef is " + refToBook);
        var BookInfo = GetDocument(refToBook);
        string BoookName ="";
        int Remainder =0;
        int Price=0;
        string Description = "";
        String Author = "";
        string Genre = "";
        String Image = "";
        int NumberPages = 0;
        try
        {
            BoookName = BookInfo.GetElementsByTagName("h1")[0].TextContent;
           // Remainder = Int32.Parse(
            //    .TextContent.Split(' ')[0]);
            //var element2 = BookInfo.GetElementsByClassName("stock")[0].Children[0];
            //var element4 = element2.TextContent.Split(' ')[3];
            var div = BookInfo.QuerySelector("div.stock strong");
            if(div != null)
            {
               // string res;
                Remainder = Int32.Parse(div.TextContent.Split(' ')[0]);
            }
            // return null;


            
            //Price = Int32.Parse(BookInfo.QuerySelector("div.price").TextContent.Replace(" ", ""));//GetElementsByClassName("price")[0].TextContent.Split(' ')[0]);
            Price = Int32.Parse(element.GetElementsByClassName("price")[0].TextContent.Split(' ')[0]); //.Replace(" ", "");

            Description = BookInfo.GetElementsByClassName("description")[0].TextContent;
            var properties = BookInfo.GetElementsByClassName("property");
            Author = properties[1].GetElementsByClassName("value")[0].TextContent
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");;
            Genre = BookInfo.GetElementsByClassName("breadcrumb")[0].GetElementsByTagName("li")[^2].TextContent
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");
            //NumberPages = Int32.Parse(BookInfo.GetElementsByClassName("breadcrumb")[0].GetElementsByTagName("li")[3].TextContent.Replace(" ", ""));
            var res = Int32.Parse(BookInfo.GetElementsByClassName("property")[3].GetElementsByClassName("value")[0].TextContent
                .Replace(" ", "")
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", ""));;
            Image = "tochka24.com" + BookInfo.GetElementsByClassName("big-image")[0].GetElementsByTagName("img")[0].Attributes["src"]
                .Value
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");;
        }
        catch (Exception ex)
        {
       //     Console.WriteLine($"{DateTime.Now} : ERROR for book - {ex.Message}");
        }
        Book book = new Book()
        {
            Author = Author,
            Description = Description,
            Genre = Genre,
            Image = Image,
            Name = BoookName,
            Remainder = Remainder,
            Price = Price,
            NumberOfPages = 0
        };
        
        book.SourceName = "https://tochka24.com";
        return book;
    }
    
    public async Task StartParsingAsync()
    {
        Console.WriteLine("ВВЕДИТЕ СТРАНИЦУ С КОТОРОЙ ПАРСИТЬ, БЕЗ ПРОБЕЛОВ И ДРУГИХ ЗНАКОВ");
        //string startStr = Console.ReadLine();
        var finalBooks = new List<Book>();
        List<Book> parsedBooks1 = new List<Book>();
        List<Book> parsedBooks2 = new List<Book>();
        List<Book> parsedBooks3 = new List<Book>();
        List<Book> parsedBooks4 = new List<Book>();
        List<Book> parsedBooks5 = new List<Book>();
        List<Book> parsedBooks6 = new List<Book>();
        List<Book> parsedBooks7 = new List<Book>();
        List<Book> parsedBooks8 = new List<Book>();
        var address = "https://tochka24.com/catalog/books?limit=500&page=";
        var count = 305;
        var countOne = 39;
        int start = 0;
        Task t1 = Task.Run(async () =>
        { 
            parsedBooks1 = await ParseBookInfo(address, start, countOne + start);
            
            WriteToJSON("BooksFromTochka1.json", parsedBooks1);
            parsedBooks1.Clear();
            
        });
        Task t2 = Task.Run(async () =>
        {
             parsedBooks2 = await ParseBookInfo(address, countOne + start + 1, countOne * 2 + start);
            WriteToJSON("BooksFromTochka2.json", parsedBooks2);
            parsedBooks2.Clear();
        });
        Task t3 = Task.Run(async () =>
        {
             parsedBooks3 = await ParseBookInfo(address, countOne * 2 + start + 1, countOne * 3 + start);
             WriteToJSON("BooksFromTochka3.json", parsedBooks3);
             parsedBooks3.Clear();
        });
        Task t4 = Task.Run(async () =>
        {
             parsedBooks4 = await ParseBookInfo(address, countOne * 3 + start + 1, countOne * 4 + start);
             WriteToJSON("BooksFromTochka4.json", parsedBooks4);
             parsedBooks4.Clear();
        });
        Task t5 = Task.Run(async () =>
        {
             parsedBooks5 = await ParseBookInfo(address, start + countOne * 4 + 1, countOne * 5 + start);
             WriteToJSON("BooksFromTochka5.json", parsedBooks5);
             parsedBooks5.Clear();
        });
        Task t6 = Task.Run(async () =>
        {
             parsedBooks6 = await ParseBookInfo(address, start + countOne * 5 + 1, countOne * 6 + start);//196-234
             WriteToJSON("BooksFromTochka6.json", parsedBooks6);
             parsedBooks6.Clear();
        });
        Task t7 = Task.Run(async () =>
        {
             parsedBooks7 = await ParseBookInfo(address, start + countOne * 6 + 1, countOne * 7 + start); //235-273
             WriteToJSON("BooksFromTochka7.json", parsedBooks7);
             parsedBooks7.Clear();
        });
        Task t8 = Task.Run(async () =>
        {
             parsedBooks8 = await ParseBookInfo(address, start + countOne * 7 + 1, countOne * 8 + start);//274-312
             WriteToJSON("BooksFromTochka8.json", parsedBooks8);
             parsedBooks8.Clear();
        });
        Task.WaitAll(t1, t2, t3, t4,t5,t7,t8);
        Console.ReadLine();
    }

    private void WriteToJSON(string path, List<Book> books)
    {
        var json = JsonConvert.SerializeObject(books);
        File.WriteAllText(path, json, Encoding.UTF8);
        Console.WriteLine("Books writed to json, count "+books.Count);
        File.WriteAllText(path+"Count",Convert.ToString(books.Count));
    }
    
    private string GetValue(IHtmlCollection<IElement> collection)
    {
        if (collection.Length > 0)
            return collection[0].TextContent;
        return "";
    }

    private IElement GetElement(IHtmlCollection<IElement> collection)
    {
        if (collection.Length > 0)
            return collection[0];
        return null;
    }
}
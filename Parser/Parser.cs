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
                    document2.GetElementsByClassName("product-thumb");
                
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
        
        
        string BoookName ="";
        int Remainder =0;
        int Price=0;
        string Description = "";
        String Author = "";
        string Genre = "";
        String Image = "";
        int NumberPages = 0;
        string ISBN = "";
        try
        {
            string refToBook = element.Children[0].Children[0].Attributes["href"].Value;
            var BookInfo = GetDocument(refToBook);
            BoookName = BookInfo.GetElementsByTagName("h1")[0].TextContent;
           // Price = Int32.Parse(BookInfo.GetElementsByClassName("list-unstyled")[0].GetElementsByTagName("h2")[0].TextContent.Split('р')[0]); //.Replace(" ", "");
           Price = Int32.Parse(BookInfo.GetElementsByTagName("h2").Where(e => e.GetAttribute("itemprop") == "price").First().TextContent.Split('р')[0]);
            Description = BookInfo.GetElementById("tab-description").Children[0].Children[0].TextContent;
            Author = BookInfo.GetElementsByClassName("col-sm-4")[1].GetElementsByTagName("h2")[0].TextContent
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");;
            Genre = BookInfo.GetElementsByClassName("breadcrumb")[0].GetElementsByTagName("li")[^2].TextContent
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");
            var info = BookInfo.GetElementsByClassName("table table-bordered")[0].Children[0].Children;
            for (int i = 0; i < info.Length; i++)
            {
                var tds = info[i].GetElementsByTagName("td");
                if (tds[0].TextContent == "Страниц")
                    NumberPages = int.Parse(tds[1].TextContent);
                if (tds[0].TextContent == "ISBN")
                    ISBN = tds[1].TextContent;

            }
            Image = BookInfo.GetElementsByClassName("thumbnail")[0].Attributes["href"]
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
        
        book.SourceName = "https://speclit.su";
        return book;
    }
    
    public async Task StartParsingAsync()
    {
        var finalBooks = new List<Book>();
        var address = "https://tochka24.com/catalog/books?limit=500&page=";
        var dokument = GetDocument("https://speclit.su/");
        var groups = dokument.GetElementsByClassName("list-group-item");
        foreach (var i in groups)
        {
            var refToGroup = i.GetAttribute("href") + "?limit=100&page=";
            finalBooks.AddRange(await ParseBookInfo(refToGroup,1,5));
        }
            
            WriteToJSON("BooksFromSpeclit.json", finalBooks);
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
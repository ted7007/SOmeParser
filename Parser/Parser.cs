using System.ComponentModel;
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
        int countBooks = endPage;
        endPage = endPage / 60;
        Console.WriteLine($"Started thread since {startPage} to {endPage}");
        var books = new List<Book>();
        DateTime nowThread = DateTime.Now;
        for(int i = startPage; i <= endPage; i++)
        {
            try
            {
                DateTime now = DateTime.Now;
                var resRef = "http://" + urlWithCollection + "&n9=" + Convert.ToString(i * 60);
                var document2 = GetDocument(resRef);


                var textWitHResultSearchElements =
                    document2.GetElementsByTagName("p").ToList();
                int count = 60;
                if (i == endPage)
                    countBooks = countBooks % 60;
                int curCountInCol = 0;
                var list = new List<IElement>();
                foreach (var j in textWitHResultSearchElements)
                {
                    if (j.ChildElementCount == 6)
                        list.Add(j);

                }
                if ((textWitHResultSearchElements.Count == 0))
                {
                    Console.WriteLine($"Page - {i}: Книги не найдены");
                    continue;
                }
                Console.WriteLine($"Page - {i} was readed, count = {textWitHResultSearchElements.Count}");
                foreach (var bookFromList in list)
                {
                    if(textWitHResultSearchElements.IndexOf(bookFromList) > 59)
                        break;
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
            var NameAndAuthor = element.GetElementsByTagName("b")[0].TextContent.Split(' ');
            for (int i = 0; i < NameAndAuthor.Length; i++)
            {
                if (i < 2)
                    Author += NameAndAuthor[i] + " ";
                else
                    BoookName += NameAndAuthor[i] + " ";
            }
            var info = element.TextContent.Split(' ');
            bool isDesc = false;
            for (int i = 0; i < info.Length; i++)
            {
                if (info[i] == "Цена:" && i < info.Length)
                    Price = Int32.Parse(info[i+1]);
                if (info[i] == "с." && i != 0)
                    NumberPages = int.Parse(info[i - 1]);
                if (info[i] == "Купить")
                    isDesc = true;
                if (info[i] == "Состояние:")
                    isDesc = false;
                if (isDesc)
                    Description += info[i] + " ";
            }
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
            NumberOfPages = NumberPages,
            ISBN = ISBN
        };
        
        book.SourceName = "http://www.alib.ru/bs.php4?bs=veronika_book";
        return book;
    }
    
    public async Task StartParsingAsync()
    {
        var finalBooks = new List<Book>();
        var address = "http://www.alib.ru/bs.php4?bs=veronika_book";
        var file = GetDocument(address);
        var col = file.GetElementsByTagName("table")[7].GetElementsByTagName("tr")[0].Children[1].Children;
        IElement parseElement = null;
        for (int i = 0; i < col.Length; i++)
        {
            if (col[i].TextContent.Contains("По цене"))
                parseElement = col[i + 1];
        }

        foreach (var i in parseElement.Children)
        {
             var refToPage = i.Children[0].GetAttribute("href").Replace("//", "");
             var count = int.Parse(i.TextContent.Split('(')[1].Split(')')[0]);
             finalBooks.AddRange(await ParseBookInfo(refToPage, 0, count));
        }
        //inalBooks.AddRange(await ParseBookInfo(address, 1, 118));
            
            WriteToJSON("BooksFromVeronika_book.json", finalBooks);
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
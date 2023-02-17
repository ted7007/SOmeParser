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
    private List<Book> ParseBookInfo(string urlWithCollection,int startPage, int endPage)
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
        string ISBN = "";
        string PublisherName = "";
        try
        {
            BoookName = BookInfo.GetElementsByTagName("h1")[0].TextContent;
            var div = BookInfo.QuerySelector("div.stock strong");
            if(div != null)
            {
                Remainder = Int32.Parse(div.TextContent.Split(' ')[0]);
            }
            Price = Int32.Parse(element.GetElementsByClassName("price")[0].TextContent.Split(' ')[0]); //.Replace(" ", "");
            Description = BookInfo.GetElementsByClassName("description")[0].TextContent
                .Replace("\t", "")
                .Replace("\n", "");;
            Genre = BookInfo.GetElementsByClassName("breadcrumb")[0].GetElementsByTagName("li")[^2].TextContent
                .Replace("\t", "")
                .Replace("\n", "");
            
            Image = "tochka24.com" + BookInfo.GetElementsByClassName("big-image")[0].GetElementsByTagName("img")[0].Attributes["src"]
                .Value
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\n", "");;
            var properties = BookInfo.GetElementsByClassName("property");
            foreach (var i in properties)
            {
                var label = i.GetElementsByClassName("label")[0].TextContent;
                var value = i.GetElementsByClassName("value")[0].TextContent
                    .Replace(" ", "")
                    .Replace("\t", "")
                    .Replace("\n", "");
                switch (label)
                {
                    case "Автор":
                        Author = value;
                        break;
                    case "ISBN/Артикул":
                        ISBN = value;
                        break;
                    case "Количество страниц":
                        NumberPages = int.Parse(value);
                        break;
                    case "Издательство":
                        PublisherName = value;
                        break;
                    
                }
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
            ISBN = ISBN,
            PublisherName = PublisherName,
            ParsingDate = DateTime.Today.ToUniversalTime()
        };
        
        book.SourceName = refToBook;
        return book;
    }


    public async Task StartParsingAsyncNew(string address, int firstPage, int lastPage, int countTasks)
    {
        if(lastPage-firstPage < countTasks)
           for(int i = 0; i < 10; i++) Console.WriteLine("WARNING!!! Count task more than count parsing pages. CAN ME ERROR");
        DateTime start = DateTime.Now;  // Если счет страниц начинается с 1, то количество страниц = ( последняя страница - 1 )
         // 305 по 500 книг. Если выбрать 0 страницу - покажется 1-ая. Если выбрать 306-ю - не выдаст книг.

         int countPages = lastPage;
        if (firstPage > 0)
            countPages = lastPage - firstPage;
        Task[] tasks = new Task[countTasks+1];
        int countPagesForOneTask = countPages / countTasks;
        int lastParsedPage = 0;
        int startPage = firstPage;
        var finalBooks = new List<Book>();
        for (int i = 0; i < countTasks; i++)
        {
            int endPage = startPage + countPagesForOneTask;
            int numberTask = i;
            int curStartPage = startPage; // чтоб не изменялось после старта потока
            var task = Task.Factory.StartNew(() =>
            {
                var parsedBooks = ParseBookInfo(address, curStartPage, endPage - 1);
                lock (finalBooks)
                {
                    finalBooks.AddRange(parsedBooks);
                }
            });
            lastParsedPage = endPage-1;
            startPage = lastParsedPage + 1;
            tasks[i] = task;
        }

        Task lastTask = null;
        if (lastParsedPage != lastPage)
        {
            lastTask =  Task.Factory.StartNew(() =>
            {
                var parsedBooks = ParseBookInfo(address, lastParsedPage + 1, lastPage);
                lock (finalBooks)
                {
                    finalBooks.AddRange(parsedBooks);
                }
            });
            tasks[countTasks] = lastTask;
        }
        else
        {
            tasks[countTasks] = Task.FromResult(new List<Book>());
        }
        // 1 task - 0,50 min - x1 work
        // 8 tasks - 0,55 min - x8 work
        // 16 tasks - 0,68 min but x16 work
        Task.WaitAll(tasks);
        DateTime end = DateTime.Now;
        Console.WriteLine($"Parsing finished. It got {(end-start).TotalMinutes} minutes; ");
        WriteToJSON($"Tochka-{DateTime.Today.ToShortDateString()}.json", finalBooks);
    }

    private void DoParse(int taskNumber,int startPage, int endPage)
    {
        Console.WriteLine($"[{taskNumber} | {DateTime.Now}]: Started parse since {startPage} to {endPage}");
        Thread.Sleep(2000);
        Console.WriteLine($"[{taskNumber} | {DateTime.Now}]: Finished parse since {startPage} to {endPage}");
        
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
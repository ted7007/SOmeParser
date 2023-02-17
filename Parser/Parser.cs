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
                    document2.GetElementsByClassName("grid-list catalog-list")[0].Children;
                
                if ((textWitHResultSearchElements.Length == 0))
                {
                    Console.WriteLine($"Page - {i}: Книги не найдены");
                    continue;
                }
                Console.WriteLine($"Page - {i} was readed, count = {textWitHResultSearchElements.Length}");
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
            string refToBook = "https://knigivdom.ru"+ element.GetElementsByClassName("img-ratio__inner")[0].Children[0].Attributes["href"].Value;
            var BookInfo = GetDocument(refToBook);
            switch (element.ClassName)
            {
                case "product-preview    is-zero-count-hidden  is-more-variants   ":
                    var info = BookInfo.GetElementsByClassName("option-selector");//[0];//.Children[0]
                       // .TextContent;
                    Author = BookInfo.GetElementsByClassName("name")[0].Children[0].TextContent;
                    BoookName = element.GetElementsByClassName("product__title heading")[0].TextContent;
           
                    Price = Int32.Parse(BookInfo.GetElementsByClassName("mm_value product-detail__price")[0].TextContent.Split(' ')[0]);
                    Description = BookInfo.GetElementsByClassName("mm_product_description")[0].Children[0].TextContent;
                    ISBN = BookInfo.GetElementsByClassName("mm_product_detail__props")[0].Children[1].TextContent.Split(' ')[1];
                    Image ="https://nlobooks.ru"+ BookInfo.GetElementsByClassName("mm_product_detail__image")[0].Children[0].Attributes["src"]
                        .Value
                        .Replace(" ", "")
                        .Replace("\t", "")
                        .Replace("\n", "");; 
                    break;
                case "product-preview    is-zero-count-hidden     ":
                    
                    break;
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
        
        book.SourceName = "https://nlobooks.ru";
        return book;
    }
    
    public async Task StartParsingAsync()
    {
        var finalBooks = new List<Book>();
        var address = "https://knigivdom.ru/collection/all?page=";       // 114
        finalBooks.AddRange(await ParseBookInfo(address, 1, 1));
            
        WriteToJSON("BooksFromNlobooks.json", finalBooks);
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
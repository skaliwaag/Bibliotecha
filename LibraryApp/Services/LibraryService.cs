using LibraryApp.Models;

namespace LibraryApp.Services;

public class LibraryService : ILibraryService
{
    private readonly string _booksPath;
    private readonly string _usersPath;

    private List<Book> _books = new();
    private List<User> _users = new();

    // Mirrors starter: Dictionary<User, List<Book>>
    private Dictionary<User, List<Book>> _borrowedBooks = new();

    public LibraryService(IWebHostEnvironment env)
    {
        _booksPath = Path.Combine(env.ContentRootPath, "Data", "Books.csv");
        _usersPath = Path.Combine(env.ContentRootPath, "Data", "Users.csv");
        ReadBooks();
        ReadUsers();
    }

    // ── Books ──────────────────────────────────────────────────────────────

    private void ReadBooks()
    {
        _books = new List<Book>();
        if (!File.Exists(_booksPath)) return;
        try
        {
            foreach (var line in File.ReadLines(_booksPath))
            {
                // CSV may have quoted fields (e.g. "O Brother, Where Art Thou?")
                var fields = ParseCsvLine(line);
                if (fields.Length >= 4 && int.TryParse(fields[0].Trim(), out int id))
                {
                    _books.Add(new Book
                    {
                        Id     = id,
                        Title  = fields[1].Trim(),
                        Author = fields[2].Trim(),
                        ISBN   = fields[3].Trim()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading books: {ex.Message}");
        }
    }

    private void WriteBooks()
    {
        var lines = _books.Select(b =>
            $"{b.Id},{EscapeCsv(b.Title)},{EscapeCsv(b.Author)},{EscapeCsv(b.ISBN)}");
        File.WriteAllLines(_booksPath, lines);
    }

    public List<Book> GetBooks() => new List<Book>(_books);

    public void AddBook(Book book)
    {
        book.Id = _books.Count > 0 ? _books.Max(b => b.Id) + 1 : 1;
        _books.Add(book);
        WriteBooks();
    }

    public void EditBook(Book updated)
    {
        var idx = _books.FindIndex(b => b.Id == updated.Id);
        if (idx >= 0) { _books[idx] = updated; WriteBooks(); }
    }

    public void DeleteBook(int id)
    {
     #   _books.RemoveAll(b => b.Id == id);
        WriteBooks();
    }

    // ── Users ──────────────────────────────────────────────────────────────

    private void ReadUsers()
    {
        _users = new List<User>();
        if (!File.Exists(_usersPath)) return;
        try
        {
            foreach (var line in File.ReadLines(_usersPath))
            {
                var fields = ParseCsvLine(line);
                if (fields.Length >= 3 && int.TryParse(fields[0].Trim(), out int id))
                {
                    _users.Add(new User
                    {
                        Id    = id,
                        Name  = fields[1].Trim(),
                        Email = fields[2].Trim()
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading users: {ex.Message}");
        }
    }

    private void WriteUsers()
    {
        var lines = _users.Select(u =>
            $"{u.Id},{EscapeCsv(u.Name)},{EscapeCsv(u.Email)}");
        File.WriteAllLines(_usersPath, lines);
    }

    public List<User> GetUsers() => new List<User>(_users);

    public void AddUser(User user)
    {
        user.Id = _users.Count > 0 ? _users.Max(u => u.Id) + 1 : 1;
        _users.Add(user);
        WriteUsers();
    }

    public void EditUser(User updated)
    {
        var idx = _users.FindIndex(u => u.Id == updated.Id);
        if (idx >= 0) { _users[idx] = updated; WriteUsers(); }
    }

    public void DeleteUser(int id)
    {
        _users.RemoveAll(u => u.Id == id);
        WriteUsers();
    }

    // ── Borrowing ──────────────────────────────────────────────────────────
    // Logic directly mirrors starter Program.cs BorrowBook / ReturnBook

    public Dictionary<User, List<Book>> GetBorrowedBooks() => _borrowedBooks;

    public (bool success, string message) BorrowBook(int userId, int bookId)
    {
        var book = _books.FirstOrDefault(b => b.Id == bookId);
        if (book == null) return (false, "Book not found or no available copies.");

        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null) return (false, "User not found.");

        if (!_borrowedBooks.ContainsKey(user))
            _borrowedBooks[user] = new List<Book>();

        _borrowedBooks[user].Add(book);
        _books.Remove(book);   // mirrors starter: removes from list (reduces "copies")
        WriteBooks();

        return (true, $"'{book.Title}' borrowed successfully by {user.Name}.");
    }

    public (bool success, string message) ReturnBook(int userId, int bookIndex)
    {
        var user = _users.FirstOrDefault(u => u.Id == userId);
        if (user == null || !_borrowedBooks.ContainsKey(user) || _borrowedBooks[user].Count == 0)
            return (false, "User not found or has no borrowed books.");

        var list = _borrowedBooks[user];
        if (bookIndex < 0 || bookIndex >= list.Count)
            return (false, "Invalid book selection.");

        var bookToReturn = list[bookIndex];
        list.RemoveAt(bookIndex);
        _books.Add(bookToReturn);   // mirrors starter: returns book to available list
        WriteBooks();

        return (true, $"'{bookToReturn.Title}' returned successfully.");
    }

    // ── CSV helpers ────────────────────────────────────────────────────────

    private static string[] ParseCsvLine(string line)
    {
        // Handles quoted fields that may contain commas
        var result = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static string EscapeCsv(string s) =>
        s.Contains(',') || s.Contains('"') ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
}

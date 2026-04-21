using LibraryApp.Models;

namespace LibraryApp.Services;

public interface ILibraryService
{
    // Books
    List<Book> GetBooks();
    void AddBook(Book book);
    void EditBook(Book book);
    void DeleteBook(int id);

    // Users
    List<User> GetUsers();
    void AddUser(User user);
    void EditUser(User user);
    void DeleteUser(int id);

    // Borrowing — mirrors starter: Dictionary<User, List<Book>>
    Dictionary<User, List<Book>> GetBorrowedBooks();
    (bool success, string message) BorrowBook(int userId, int bookId);
    (bool success, string message) ReturnBook(int userId, int bookIndex);
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using LibraryApp.Models;
using LibraryApp.Services;
using Microsoft.AspNetCore.Hosting;
using Moq;

namespace LibraryApp.Tests;

[TestClass]
public class LibraryServiceTests
{
    private static LibraryService CreateFreshService()
    {
        string tempRoot = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(Path.Combine(tempRoot, "Data"));
        File.WriteAllText(Path.Combine(tempRoot, "Data", "Books.csv"), "");
        File.WriteAllText(Path.Combine(tempRoot, "Data", "Users.csv"), "");

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.ContentRootPath).Returns(tempRoot);

        return new LibraryService(mockEnv.Object);
    }

    // ── AddBook ────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddBook_IncreasesBookCount()
    {
        // Arrange
        var service = CreateFreshService();
        int countBefore = service.GetBooks().Count;

        // Act
        service.AddBook(new Book { Title = "Dune", Author = "Frank Herbert", ISBN = "978-0441013593" });

        // Assert
        Assert.AreEqual(countBefore + 1, service.GetBooks().Count);
    }

    [TestMethod]
    public void AddBook_AssignsPositiveId()
    {
        // Arrange
        var service = CreateFreshService();

        // Act
        service.AddBook(new Book { Title = "1984", Author = "George Orwell", ISBN = "978-0451524935" });

        // Assert
        int assignedId = service.GetBooks().First().Id;
        Assert.IsTrue(assignedId > 0, $"Expected positive Id, got {assignedId}.");
    }

    [TestMethod]
    public void AddBook_SecondBook_GetsHigherId()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "First", Author = "Author A", ISBN = "111" });

        // Act
        service.AddBook(new Book { Title = "Second", Author = "Author B", ISBN = "222" });

        // Assert
        var books = service.GetBooks();
        Assert.IsTrue(books[1].Id > books[0].Id, "Each new book should receive a higher Id than the previous.");
    }

    [TestMethod]
    public void AddBook_WithCommaInTitle_StoredCorrectly()
    {
        // Arrange - edge case: commas in titles must survive the CSV round-trip
        var service = CreateFreshService();

        // Act
        service.AddBook(new Book { Title = "O Brother, Where Art Thou?", Author = "Ethan Coen", ISBN = "444" });

        // Assert
        Assert.AreEqual("O Brother, Where Art Thou?", service.GetBooks().First().Title);
    }

    // ── EditBook ───────────────────────────────────────────────────────────

    [TestMethod]
    public void EditBook_UpdatesTitle()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "Old Title", Author = "Author", ISBN = "555" });
        var book = service.GetBooks().First();

        // Act
        book.Title = "New Title";
        service.EditBook(book);

        // Assert
        Assert.AreEqual("New Title", service.GetBooks().First().Title);
    }

    [TestMethod]
    public void EditBook_NonExistentId_DoesNotThrow()
    {
        // Arrange - edge case: editing a book that doesn't exist should be a silent no-op
        var service = CreateFreshService();

        // Act & Assert - should not throw an exception
        service.EditBook(new Book { Id = 999, Title = "Ghost", Author = "Nobody", ISBN = "000" });
        Assert.AreEqual(0, service.GetBooks().Count);
    }

    // ── DeleteBook ─────────────────────────────────────────────────────────

    [TestMethod]
    public void DeleteBook_RemovesBook()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "To Delete", Author = "Author", ISBN = "666" });
        int bookId = service.GetBooks().First().Id;

        // Act
        service.DeleteBook(bookId);

        // Assert
        Assert.AreEqual(0, service.GetBooks().Count);
    }

    [TestMethod]
    public void DeleteBook_OnlyRemovesMatchingBook()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "Keep Me", Author = "Author A", ISBN = "777" });
        service.AddBook(new Book { Title = "Delete Me", Author = "Author B", ISBN = "888" });
        int deleteId = service.GetBooks().Last().Id;

        // Act
        service.DeleteBook(deleteId);

        // Assert
        var remaining = service.GetBooks();
        Assert.AreEqual(1, remaining.Count);
        Assert.AreEqual("Keep Me", remaining.First().Title);
    }

    [TestMethod]
    public void DeleteBook_NonExistentId_DoesNotAffectExistingBooks()
    {
        // Arrange - edge case: deleting an id that doesn't exist should be a no-op
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "Survivor", Author = "Author", ISBN = "999" });

        // Act
        service.DeleteBook(999);

        // Assert
        Assert.AreEqual(1, service.GetBooks().Count);
    }

    // ── GetBooks ───────────────────────────────────────────────────────────

    [TestMethod]
    public void GetBooks_ReturnsDefensiveCopy()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "Book A", Author = "Author", ISBN = "A1" });

        // Act
        var list = service.GetBooks();
        list.Clear();

        // Assert
        Assert.AreEqual(1, service.GetBooks().Count);
    }

    // ── Users ──────────────────────────────────────────────────────────────

    [TestMethod]
    public void AddUser_IncreasesUserCount()
    {
        // Arrange
        var service = CreateFreshService();
        int countBefore = service.GetUsers().Count;

        // Act
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });

        // Assert
        Assert.AreEqual(countBefore + 1, service.GetUsers().Count);
    }

    [TestMethod]
    public void AddUser_AssignsPositiveId()
    {
        // Arrange
        var service = CreateFreshService();

        // Act
        service.AddUser(new User { Name = "Bob", Email = "bob@example.com" });

        // Assert
        Assert.IsTrue(service.GetUsers().First().Id > 0);
    }

    [TestMethod]
    public void EditUser_UpdatesEmail()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Carol", Email = "old@example.com" });
        var user = service.GetUsers().First();

        // Act
        user.Email = "new@example.com";
        service.EditUser(user);

        // Assert
        Assert.AreEqual("new@example.com", service.GetUsers().First().Email);
    }

    [TestMethod]
    public void DeleteUser_RemovesUser()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Dave", Email = "dave@example.com" });
        int userId = service.GetUsers().First().Id;

        // Act
        service.DeleteUser(userId);

        // Assert
        Assert.AreEqual(0, service.GetUsers().Count);
    }

    // ── Borrow / Return ────────────────────────────────────────────────────

    [TestMethod]
    public void BorrowBook_SucceedsWithValidUserAndBook()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        service.AddBook(new Book { Title = "Dune", Author = "Herbert", ISBN = "B1" });
        int userId = service.GetUsers().First().Id;
        int bookId = service.GetBooks().First().Id;

        // Act
        var (success, message) = service.BorrowBook(userId, bookId);

        // Assert
        Assert.IsTrue(success, $"Expected success but got: {message}");
    }

    [TestMethod]
    public void BorrowBook_RemovesBookFromAvailableList()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        service.AddBook(new Book { Title = "Dune", Author = "Herbert", ISBN = "B2" });
        int userId = service.GetUsers().First().Id;
        int bookId = service.GetBooks().First().Id;

        // Act
        service.BorrowBook(userId, bookId);

        // Assert
        Assert.AreEqual(0, service.GetBooks().Count, "Borrowed book should be removed from the available list.");
    }

    [TestMethod]
    public void BorrowBook_AppearsInBorrowedDictionary()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        service.AddBook(new Book { Title = "Dune", Author = "Herbert", ISBN = "B3" });
        var user = service.GetUsers().First();
        int bookId = service.GetBooks().First().Id;

        // Act
        service.BorrowBook(user.Id, bookId);

        // Assert
        var borrowed = service.GetBorrowedBooks();
        Assert.IsTrue(borrowed.ContainsKey(user));
        Assert.AreEqual(1, borrowed[user].Count);
    }

    [TestMethod]
    public void BorrowBook_FailsForInvalidUserId()
    {
        // Arrange - edge case: user does not exist
        var service = CreateFreshService();
        service.AddBook(new Book { Title = "Dune", Author = "Herbert", ISBN = "B4" });
        int bookId = service.GetBooks().First().Id;

        // Act
        var (success, _) = service.BorrowBook(userId: 999, bookId: bookId);

        // Assert
        Assert.IsFalse(success);
    }

    [TestMethod]
    public void BorrowBook_FailsForInvalidBookId()
    {
        // Arrange - edge case: book does not exist
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        int userId = service.GetUsers().First().Id;

        // Act
        var (success, _) = service.BorrowBook(userId: userId, bookId: 999);

        // Assert
        Assert.IsFalse(success);
    }

    [TestMethod]
    public void ReturnBook_SucceedsAfterBorrow()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        service.AddBook(new Book { Title = "1984", Author = "Orwell", ISBN = "C1" });
        int userId = service.GetUsers().First().Id;
        int bookId = service.GetBooks().First().Id;
        service.BorrowBook(userId, bookId);

        // Act
        var (success, message) = service.ReturnBook(userId, bookIndex: 0);

        // Assert
        Assert.IsTrue(success, $"Expected success but got: {message}");
    }

    [TestMethod]
    public void ReturnBook_RestoresBookToAvailableList()
    {
        // Arrange
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        service.AddBook(new Book { Title = "1984", Author = "Orwell", ISBN = "C2" });
        int userId = service.GetUsers().First().Id;
        int bookId = service.GetBooks().First().Id;
        service.BorrowBook(userId, bookId);

        // Act
        service.ReturnBook(userId, bookIndex: 0);

        // Assert
        Assert.AreEqual(1, service.GetBooks().Count, "Returned book should be back in the available list.");
    }

    [TestMethod]
    public void ReturnBook_FailsForOutOfRangeBookIndex()
    {
        // Arrange - edge case: index is beyond the end of the borrow list
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        service.AddBook(new Book { Title = "1984", Author = "Orwell", ISBN = "C3" });
        int userId = service.GetUsers().First().Id;
        int bookId = service.GetBooks().First().Id;
        service.BorrowBook(userId, bookId);

        // Act
        var (success, _) = service.ReturnBook(userId, bookIndex: 99);

        // Assert
        Assert.IsFalse(success);
    }

    [TestMethod]
    public void ReturnBook_FailsForUserWithNoBorrows()
    {
        // Arrange - edge case: user exists but has no borrowed books
        var service = CreateFreshService();
        service.AddUser(new User { Name = "Alice", Email = "alice@example.com" });
        int userId = service.GetUsers().First().Id;

        // Act
        var (success, _) = service.ReturnBook(userId, bookIndex: 0);

        // Assert
        Assert.IsFalse(success);
    }
}

namespace ExpensesBackend.API.Domain.DTOs;

public class UsageDto
{
    public int BooksOwned        { get; set; }
    public int BooksLimit        { get; set; }   // -1 = unlimited
    public int ExpensesThisMonth { get; set; }
    public int ExpensesLimit     { get; set; }   // -1 = unlimited
    public int CategoriesUsed   { get; set; }   // sum of CategoriesUsed across member docs
    public int CategoriesLimit  { get; set; }   // -1 = unlimited (Pro)
}

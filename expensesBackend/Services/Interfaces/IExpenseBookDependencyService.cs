namespace ExpensesBackend.API.Services.Interfaces;

public interface IExpenseBookDependencyService
{
    Task CopyDefaultCategoriesToBookAsync(string expenseBookId);
}

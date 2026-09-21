namespace MyInstantsApi;

public class MyInstantsException : Exception
{
    public MyInstantsException(string message) : base(message) { }
    public MyInstantsException(string message, Exception inner) : base(message, inner) { }
}

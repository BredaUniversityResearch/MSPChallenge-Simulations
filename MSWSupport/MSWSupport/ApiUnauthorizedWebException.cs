using System;
namespace MSWSupport;

public class ApiUnauthorizedWebException : Exception
{
	public ApiUnauthorizedWebException(Exception? innerException) : base(innerException?.Message, innerException)
	{
	}
}

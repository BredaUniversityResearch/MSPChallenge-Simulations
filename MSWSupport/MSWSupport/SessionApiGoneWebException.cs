using System;
namespace MSWSupport;

public class SessionApiGoneWebException : Exception
{
	public SessionApiGoneWebException(Exception? innerException) : base(innerException?.Message, innerException)
	{
	}    
}
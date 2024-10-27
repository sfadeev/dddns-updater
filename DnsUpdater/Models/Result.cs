namespace DnsUpdater.Models
{
	public class Result<TData>
	{
		public bool Success { get; init; }
		
		public string? Error { get; init; }
		
		public TData? Data { get; init; }
	}

	public class Result : Result<Result>
	{
		public static Result<TData> CreateSuccessResult<TData>(TData? data = default)
		{
			return new Result<TData> { Success = true, Data = data };
		}
		
		public static Result<TData> CreateErrorResult<TData>(string? message = null)
		{
			return new Result<TData> { Success = false, Error = message };
		}
	}
}
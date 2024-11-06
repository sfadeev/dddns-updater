using System.Text;

namespace DnsUpdater.Services
{
	public static class ExceptionUtils
	{
		public static string BuildMessage(Exception? ex)
		{
			var result = new StringBuilder();

			while (ex != null)
			{
				if (result.Length > 0) result.Append(" — ");
				
				result.AppendLine(ex.Message);	
				
				ex = ex.InnerException;
			}
			
			return result.ToString();
		}
	}
}
namespace DnsUpdater.Services
{
	public class AsyncLock
	{
		private readonly SemaphoreSlim _semaphore = new(1, 1);
		
		public async Task<IDisposable> LockAsync(CancellationToken cancellationToken)
		{
			await _semaphore.WaitAsync(cancellationToken);

			return new AsyncLockReleaser(_semaphore);
		}

		private class AsyncLockReleaser(SemaphoreSlim semaphore) : IDisposable
		{
			public void Dispose()
			{
				semaphore.Release();
			}
		}
	}
}
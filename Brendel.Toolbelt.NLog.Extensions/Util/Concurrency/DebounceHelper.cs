namespace Brendel.Toolbelt.NLog.Extensions.Util.Concurrency;

public sealed class DebounceHelper(TimeProvider time) : IDisposable {
	private bool _disposed;
	private CancellationTokenSource? _debounceCts;
	private Task? _debounceTask;
	private long _lastDebounceTime;

	/// <summary>
	/// Action to Invoke when debounced
	/// </summary>
	public Action<DateTimeOffset>? Action { get; set; }

	public bool Active => _debounceTask?.IsCompleted != true && _debounceCts?.IsCancellationRequested == false;

	~DebounceHelper() {
		Dispose(false);
	}

	private readonly object _lock = new();

	private void EnqueueDebonce(DateTimeOffset timestamp, TimeSpan delay) {
		var hasLock = false;
		try {
			Monitor.Enter(_lock, ref hasLock);

			if (_disposed) {
				throw new ObjectDisposedException(nameof(DebounceHelper));
			}

			if (_lastDebounceTime == timestamp.UtcTicks) {
				return;
			}

			_debounceCts?.Cancel();
			_debounceCts = new();
			_lastDebounceTime = timestamp.UtcTicks;

			var token = _debounceCts.Token;
			_debounceTask = Task.Run(async () => {
				try {
					await Task.Delay(delay, time, token);
				} catch (TaskCanceledException) {
					return;
				}

				if (!token.IsCancellationRequested) {
					Action?.Invoke(timestamp);
				}
			}, token);
		}
		finally {
			if (hasLock) {
				Monitor.Exit(_lock);
			}
		}
	}

	public void DebounceAt(DateTimeOffset timestamp) {
		var delay = timestamp - time.GetUtcNow();
		if (delay < TimeSpan.Zero) {
			Action?.Invoke(timestamp);
			return;
		}

		EnqueueDebonce(timestamp, delay);
	}

	public void Cancel() {
		_lastDebounceTime = 0;
		_debounceCts?.Cancel();
	}

	private void Dispose(bool disposing) {
		if (_disposed) {
			return;
		}

		if (disposing) {
			Cancel();
		}

		Action = null;

		_disposed = true;
	}

	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}
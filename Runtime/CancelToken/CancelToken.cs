namespace Aoiro
{
    public sealed class CancelToken<T> : ICancelToken where T : struct, ICancelToken
    {
        private T _token;
        private bool _isCancelled;

        public CancelToken(T token)
        {
            _token = token;
        }

        public void Cancel()
        {
            if (_isCancelled)
            {
                return;
            }

            _isCancelled = true;
            try
            {
                _token.Cancel();
            }
            finally
            {
                _token = default;
            }
        }
    }
}

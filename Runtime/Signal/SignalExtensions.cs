namespace Aoiro
{
    public static class SignalExtensions
    {
        public static ICancelToken ToCancelToken(in this SignalToken token)
        {
            return token.Signal != null ? CancelToken.FromTarget(token.Signal, token) : CancelToken.None;
        }
    }
}

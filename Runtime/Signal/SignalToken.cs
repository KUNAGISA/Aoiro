using System;

namespace Aoiro
{
    public readonly struct SignalToken : IEquatable<SignalToken>, ICancelToken
    {
        internal readonly ISignal Signal;
        internal readonly int Index;
        internal readonly int Version;
        
        internal SignalToken(ISignal signal, int index, int version)
        {
            Index = index;
            Version = version;
            Signal = signal;
        }

        public void Cancel()
        {
            Signal?.Cancel(this);
        }
        
        public bool Equals(SignalToken other)
        {
            return Index == other.Index && Version == other.Version && Signal == other.Signal;
        }

        public override bool Equals(object obj)
        {
            return obj is SignalToken other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, Version, Signal);
        }

        public override string ToString()
        {
            return $"SignalToken({Index}, v{Version})";
        }
    }
}

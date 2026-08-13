using System;

namespace Aoiro
{
    public readonly struct SubscriptionToken : IEquatable<SubscriptionToken>, ICancelToken
    {
        internal readonly ISubscriptionSource Source;
        internal readonly int Index;
        internal readonly int Version;

        internal SubscriptionToken(ISubscriptionSource source, int index, int version)
        {
            Source = source;
            Index = index;
            Version = version;
        }

        public void Cancel() => Source?.Cancel(this);

        public bool Equals(SubscriptionToken other) => Index == other.Index && Version == other.Version && ReferenceEquals(Source, other.Source);

        public override bool Equals(object obj) => obj is SubscriptionToken other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Index, Version, Source);

        public override string ToString() => $"SubscriptionToken({Index}, v{Version})";
    }
}

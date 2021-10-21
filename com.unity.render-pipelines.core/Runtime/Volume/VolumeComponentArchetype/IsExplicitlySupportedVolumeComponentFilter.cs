using System;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    public sealed class IsExplicitlySupportedVolumeComponentFilter : IFilter<VolumeComponentType>
    {
        [NotNull]
        Type targetType { get; }

        public IsExplicitlySupportedVolumeComponentFilter([DisallowNull] Type targetType)
        {
            this.targetType = targetType;
        }

        public bool IsAccepted(VolumeComponentType subjectType)
        {
            return IsSupportedOn.IsExplicitlySupportedBy((Type)subjectType, targetType);
        }

        bool Equals(IsExplicitlySupportedVolumeComponentFilter other)
        {
            return targetType == other.targetType;
        }

        public bool Equals(IFilter<VolumeComponentType> other)
        {
            return other is IsExplicitlySupportedVolumeComponentFilter filter && Equals(filter);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((IsExplicitlySupportedVolumeComponentFilter)obj);
        }

        public override int GetHashCode()
        {
            return (targetType != null ? targetType.GetHashCode() : 0);
        }
    }
}

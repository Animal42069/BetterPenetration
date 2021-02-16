using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core_BetterPenetration
{
    static class Tools
    {
        private struct MemberKey
        {
            public readonly Type type;
            public readonly string name;
            private readonly int _hashCode;

            public MemberKey(Type inType, string inName)
            {
                this.type = inType;
                this.name = inName;
                this._hashCode = this.type.GetHashCode() ^ this.name.GetHashCode();
            }

            public override int GetHashCode()
            {
                return this._hashCode;
            }
        }

        private static readonly Dictionary<MemberKey, PropertyInfo> _propertyCache = new Dictionary<MemberKey, PropertyInfo>();

        internal static object GetPrivateProperty(this object self, string name)
        {
            MemberKey key = new MemberKey(self.GetType(), name);
            if (_propertyCache.TryGetValue(key, out PropertyInfo info) == false)
            {
                info = key.type.GetProperty(key.name, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                _propertyCache.Add(key, info);
            }
            return info.GetValue(self, null);
        }
    }
}

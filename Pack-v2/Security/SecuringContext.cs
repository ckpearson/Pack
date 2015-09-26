using System.Collections.Generic;
using System.Linq;

namespace Pack_v2.Security
{
    public class SecuringContext
    {
        public IReadOnlyCollection<byte[]> TargetPublicKeys { get;}

        public SecuringContext(IReadOnlyCollection<byte[]> targetPublicKeys = null)
        {
            TargetPublicKeys = targetPublicKeys ?? Enumerable.Empty<byte[]>().ToList();
        }
    }
}
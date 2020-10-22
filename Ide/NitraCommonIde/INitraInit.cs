using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NitraCommonIde
{
  public interface INitraInit
  {
    Task Init(CancellationToken cancellationToken, Config config);
  }
}

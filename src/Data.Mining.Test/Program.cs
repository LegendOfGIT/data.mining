using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Mining.Test
{
  class Program
  {
    static void Main(string[] args)
    {
      var filter = Data.Mining.MiningCompiler.ComposeFilter("gelbe Kleidung");
    }
  }
}

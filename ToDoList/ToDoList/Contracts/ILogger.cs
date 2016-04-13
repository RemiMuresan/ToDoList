using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public interface ILogger
    {
        void Error(string msg);
        void Info(string msg);
    }
}

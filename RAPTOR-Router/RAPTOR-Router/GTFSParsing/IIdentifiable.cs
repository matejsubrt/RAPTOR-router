using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPTOR_Router.GTFSParsing
{
    /// <summary>
    /// An Interface indicating, that the object has a unique string Id
    /// </summary>
    public interface IIdentifiable
    {
        public string GetId();
    }
}

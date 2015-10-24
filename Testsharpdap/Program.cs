using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using sharpdap;

namespace Testsharpdap
{
    class Program
    {
        static void Main(string[] args)
        {
            sharpdap.Loader test = new Loader();
            string result = test.loadDataset("http://thredds.emodnet-physics.eu:8080/thredds/dodsC/fmrc/COSYNA/COSYNA_best.ncd");            
        }
    }
}

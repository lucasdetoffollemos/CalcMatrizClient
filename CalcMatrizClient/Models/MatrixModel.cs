using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalcMatrizClient.Models
{
    public class MatrixModel
    {
        public double[][] matrixA { get; set; }
        public double[][] matrixB { get; set; }
        public int index;
        public int length;
    }

    public class MatrixReturn
    {
        public double[][] matrixResult { get; set; }
    }
}

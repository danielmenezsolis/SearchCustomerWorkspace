using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SearchCustomerWorkspace
{
    public class Result
    {
        public string Customer { get; set; }
        public string RFC { get; set; }
        public long ID { get; set; }
    }
}



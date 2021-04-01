using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Models
{
    public class FolderModel
    {        
        public string FolderName { get; set; }
        
        public string FolderPath { get; set; }

        public string FolderSize { get; set; }
        
        public string DestinationPath { get; set; }

        public bool IncludeSubfolders { get; set; }

    }
}

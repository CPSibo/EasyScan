using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EasyScan
{
    public static class Settings
    {
        public static string SavePath { get; set; } = @"C:\Users\User\Desktop\Grabber";

        public static string BookName { get; set; } = @"Book";

        public static string ProjectPath => Path.Combine(Settings.SavePath, Settings.BookName);
    }
}

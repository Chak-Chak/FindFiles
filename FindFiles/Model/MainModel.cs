using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FindFiles.Model
{
    class MainModel
    {
        public List<string> Exclude(string path, string excludeMask)
        {
            List<string> sortFiles = new List<string>();
            string temp = "";
            string[] masks = excludeMask.Split(new[] { '*', ' ', ',', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            //int lenght = files.Length;
            foreach (string f in files)
            {
                foreach (string m in masks)
                {
                    if (f.IndexOf(m) != -1) ;
                    else sortFiles.Add(f);
                }
            }
            return sortFiles;
        }
    }
}

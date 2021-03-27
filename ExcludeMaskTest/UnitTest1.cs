using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ExcludeMaskTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ExcludeMaskTest()
        {
            var excludeFile = new ExcludeFile();
            string excludeMask = "*.docx,,*.dll";
            string includeMask = "*.*";
            List<string> files = excludeFile.SearchFiles("D:\\test", includeMask, excludeMask, true);
            files.Count.Equals(4);
        }
    }

    public class ExcludeFile
    {
        public List<string> SearchFiles(string path, string includeMask, string excludeMask, bool includeSubDirectories)
        {
            List<string> sortFiles = new List<string>();
            string[] masks = excludeMask.Split(new[] { '*', ' ', ',', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            string[] files;
            if (includeSubDirectories) { files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, SearchOption.AllDirectories); }
            else { files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, SearchOption.TopDirectoryOnly); }
            int lenght = files.Length; //use for debugging
            bool isChecked = true;
            foreach (string f in files)
            {
                foreach (string m in masks)
                {
                    if (f.IndexOf(m) != -1) isChecked = false;
                }
                if (isChecked) sortFiles.Add(f);
            }
            return sortFiles;
        }
    }
}
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
            string excludeMask = "*.docx, ";
            List<string> files = excludeFile.Exclude("D:\\test", excludeMask);
            files.Count.Equals(4);
        }
    }

    public class ExcludeFile
    {
        public List<string> Exclude(string path, string excludeMask)
        {
            List<string> sortFiles = new List<string>();
            string temp = "";
            string[] masks = excludeMask.Split(new[] {'*', ' ', ',', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            string[] files = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
            int lenght = files.Length; //use for debugging
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
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
            string excludeMask = "*.docx,*.dll";
            string includeMask = "*.*";
            //List<string> files = excludeFile.SearchFiles("D:\\test", includeMask, excludeMask, true);
            //var files = excludeFile.SearchFiles("D:\\test", includeMask, excludeMask, true);
            //files.Count.Equals(4);
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

        public IEnumerable<string> SearchFiles1(string path, string includeMask, string excludeMask, bool includeSubDirectories)
        {
            //List<string> sortFiles = new List<string>();
            //string[] masks = excludeMask.Split(new[] { '*', ' ', ',', ':', '?', '!' }, StringSplitOptions.RemoveEmptyEntries);
            //var files;
            var searchOption = includeSubDirectories == true ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            IEnumerable<string> sortFiles;
            string[] foundFiles = new string[0];
            string[] excludeFiles = new string[0];

            var allIncludeMasks = includeMask.Replace(" ", "").Split(',');
            var allExcludeMasks = excludeMask?.Replace(" ", "").Split(',');

            foreach (var include in allIncludeMasks)
            {
                foundFiles = foundFiles.Concat(Directory.GetFiles(path, include, searchOption)).ToArray();
            }

            if (string.IsNullOrWhiteSpace(excludeMask) == false)
            {
                foreach (var exclude in allExcludeMasks)
                {
                    excludeFiles = excludeFiles.Concat(Directory.GetFiles(path, exclude, searchOption)).ToArray();
                }
            }

            sortFiles = string.IsNullOrWhiteSpace(excludeMask) == false
                ? foundFiles.Except(excludeFiles).AsEnumerable()
                : foundFiles.AsEnumerable();

            /*if (includeSubDirectories) { var files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, searchOption); }
            else { files = Directory.GetFiles(path, string.IsNullOrWhiteSpace(includeMask) ? "*.*" : includeMask, searchOption); }*/

            /*int lenght = files.Length; //use for debugging
            bool isChecked = true;
            foreach (string file in files)
            {
                foreach (string m in masks)
                {
                    if (file.IndexOf(m) != -1) isChecked = false;
                }
                if (isChecked) sortFiles.Add(file);
            }*/
            return sortFiles;
        }
    }
}
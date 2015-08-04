﻿using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NDSB.FileUtils
{
    public static class CSVHelper
    {
        public static string ColumnBind(string[] filePaths)
        {
            List<string[]> enumerators = new List<string[]>();
            for (int i = 0; i < filePaths.Length; i++)
                enumerators.Add(LinesEnumerator.YieldLines(filePaths[i]).ToArray());

            List<string> toWrite = new List<string>();

            for (int i = 0; i < enumerators[0].Length; i++)
            {
                string line = "";
                for (int j = 0; j < enumerators.Count; j++)
                    line += enumerators[j][i] + ";";
                line.Remove(line.Length - 1);
                toWrite.Add(line);
            }
            string outFilePath = Path.GetDirectoryName(filePaths[0]) + "\\Merged.csv";
            File.WriteAllLines(outFilePath, toWrite.ToArray());

            return outFilePath;
        }

        public static string ExtractColumn(string filePath, int columnIndex)
        {
            List<string> col = new List<string>();
            foreach(string line in LinesEnumerator.YieldLines(filePath))
                col.Add(line.Split(';')[columnIndex]);

            string outFilePath = Path.GetDirectoryName(filePath) + "\\" + Path.GetFileNameWithoutExtension(filePath) + "c_" + columnIndex + ".csv";
            File.WriteAllLines(outFilePath, col.ToArray());
            return outFilePath;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NetEti.FileTools.Zip
{
    /// <summary>
    /// Stellt ein paar allgemeine Hilfsroutinen für alle beteiligten Klassen zur Verfügung.
    /// </summary>
    /// <remarks>
    /// 24.02.2023 Erik Nagel: created.
    /// </remarks>
    public class StaticZipHelpers
    {
        #region Hilfsroutinen

        /// <summary>
        /// Liefert ein string-Array mit den FilePathes aus einer ZippedFileInfo-List.
        /// </summary>
        /// <param name="infos">Liste mit ZippedFileInfos.</param>
        /// <returns>String-Array mit den FilePathes aus infos.</returns>
        public static string[] GetZipEntryFilePathes(List<ZippedFileInfo> infos)
        {
            return infos.Select(i => i.FilePath).ToList().ToArray();
        }

        /// <summary>
        /// Konvertiert ein DateTimeOffset nach DateTime.
        /// </summary>
        /// <param name="dateTime">Ein Zeitpunkt als DateTimeOffset.</param>
        /// <returns>Lokale DateTime</returns>
        public static DateTime ConvertFromDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Offset.Equals(TimeSpan.Zero))
                return dateTime.UtcDateTime;
            else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
                return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
            else
                return dateTime.DateTime;
        }

        /// <summary>
        /// Liefert eine Liste aller Dateinamen und Unterverzeichnisnamen
        /// und deren Dateinamen und Unterverzeichnisnamen u.s.w. rekursiv.
        /// benutzt die interne Hilfsroutine GenerateFileListIntern.
        /// </summary>
        /// <param name="rootDir">Das Root-Verzeichnis</param>
        /// <param name="allBytes">out-Parameter: liefert die Gesamt-Byte-Anzahl aller Dateien.</param>
        /// <returns>String-Liste mit Datei- und Verzeichnisnamen</returns>
        public static List<string> GenerateFileList(string rootDir, out long allBytes)
        {
            allBytes = 0;
            return (GenerateFileListIntern(rootDir, ref allBytes));
        }

        /// <summary>
        /// Liefert eine Liste aller Dateinamen und Unterverzeichnisnamen
        /// und deren Dateinamen und Unterverzeichnisnamen u.s.w. rekursiv.
        /// Interne Hilfsroutine von GenerateFileList.
        /// </summary>
        /// <param name="rootDir">Das Root-Verzeichnis für den aktuellen Durchlauf</param>
        /// <param name="allBytes">ref-Parameter: liefert die Gesamt-Byte-Anzahl aller Dateien.</param>
        /// <returns>String-Liste mit Datei- und Verzeichnisnamen</returns>
        public static List<string> GenerateFileListIntern(string rootDir, ref long allBytes)
        {
            List<string> files = new List<string>();
            bool Empty = true;
            if (File.Exists(rootDir))
            {
                allBytes += new FileInfo(rootDir).Length;
                files.Add(rootDir);
                Empty = false;
            }
            else
            {
                foreach (string file in Directory.GetFiles(rootDir))
                {
                    allBytes += new FileInfo(file).Length;
                    files.Add(file);
                    Empty = false;
                }
                if (Empty)
                {
                    if (Directory.GetDirectories(rootDir).Length == 0)
                    {
                        files.Add(rootDir + @"/");
                    }
                }
                foreach (string dirs in Directory.GetDirectories(rootDir)) // rekursiv
                {
                    foreach (string entry in GenerateFileListIntern(dirs, ref allBytes))
                    {
                        files.Add(entry);
                    }
                }
            }
            return files;
        }

        #endregion Hilfsroutinen
    }
}

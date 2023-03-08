using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Compression;
using System.IO;
using System.Linq;

namespace NetEti.FileTools.Zip
{
    /// <summary>
    /// Funktion: Routinen zum Packen und Entpacken von ZIP-Archiven.
    ///           Nutzt die ICSharpCode.SharpZipLib.dll.
    /// </summary>
    /// <remarks>
    /// Die Microsoft eigenen Routinen legen bei Archiven > 4GIG die Ohren an und können keine Passwörter!<br></br>
    /// Autor: Peter Bromberg (http://www.eggheadcafe.com/tutorials/aspnet/9ce6c242-c14c-4969-9251-af95e4cf320f/zip--unzip-folders-and-f.aspx)<br />
    /// Vielen Dank dafür.
    /// zurechtgepfuscht von: Erik Nagel, NetEti<br />
    /// <br></br>
    /// 24.02.2023 Erik Nagel: erstellt<br />
    /// </remarks>
    public class MSCompressionWorker : IZipWorker
    {
        /// <summary>
        /// Ereignis das Eintritt, wenn sich der Fortschritt von ZipAccess ändert.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressChanged;

        /// <summary>
        /// Ereignis das Eintritt, wenn ZipAccess beendet wird.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressFinished;

        #region MSCompression Routinen

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt</returns>
        public bool IsZip(string zipPathAndFile)
        {
            try
            {
                using (var zipFile = ZipFile.OpenRead(zipPathAndFile))
                {
                    var entries = zipFile.Entries;
                }
            }
            catch (InvalidDataException)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als String-Liste.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>String-Liste mit dem Inhaltsverzeichnis des Archivs</returns>
        public List<ZippedFileInfo> GetZipEntryList(string zipPathAndFile, string? password)
        {
            List<ZippedFileInfo> zipList = new List<ZippedFileInfo>();
            using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zipPathAndFile))
            {
                int anzahlGesamt = archive.Entries.Count;
                if (anzahlGesamt > 0)
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        ZippedFileInfo info = new ZippedFileInfo();
                        info.FilePath = Path.Combine(entry.FullName).TrimEnd('/');
                        info.LastWriteTime = StaticZipHelpers.ConvertFromDateTimeOffset(entry.LastWriteTime);
                        info.Size = entry.Length;
                        info.IsDirectory = entry.FullName.EndsWith("/");
                        zipList.Add(info);
                    }
                }
            }
            return zipList;
        }

        /// <summary>
        /// Entpackt alles aus dem Archiv incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs</param>
        /// <param name="outputFolder">Wohin entpackt werden soll</param>
        /// <param name="password">Passwort oder null</param>
        /// <param name="deleteZipFile">ob das Archiv hinterher gelöscht werden soll</param>
        public void UnZipArchive(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile)
        {
            this.UnZipArchiveFiles(zipPathAndFile, outputFolder, password, deleteZipFile, null);
        }

        /// <summary>
        /// Entpackt alles aus dem Archiv incl. Unterverzeichnis-Strukturen in einen gegebenen Ordner.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs.</param>
        /// <param name="outputFolder">Wohin entpackt werden soll.</param>
        /// <param name="password">Passwort oder null.</param>
        /// <param name="deleteZipFile">Bei true wird das Archiv hinterher gelöscht.</param>
        /// <param name="filePathes">Liste der zu entpackenden Dateien (inklusive relative Pfade)</param>
        public void UnZipArchiveFiles(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile, string[]? filePathes = null)
        {
            // Normalizes the path.
            string extractPath = Path.GetFullPath(outputFolder);

            // Ensures that the last character on the extraction path
            // is the directory separator char.
            // Without this, a malicious zip file could try to traverse outside of the expected
            // extraction path.
            if (!extractPath.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                extractPath += Path.DirectorySeparatorChar;

            using (ZipArchive archive = ZipFile.OpenRead(zipPathAndFile))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    string entryName = entry.FullName;
                    if (filePathes == null || (filePathes.Contains(entryName, StringComparer.OrdinalIgnoreCase)))
                    {
                        // Gets the full path to ensure that relative segments are removed.
                        string destinationPath = Path.GetFullPath(Path.Combine(extractPath, entry.FullName));


                        // Ordinal match is safest, case-sensitive volumes can be mounted within volumes that
                        // are case-insensitive.
                        if (destinationPath.StartsWith(extractPath, StringComparison.Ordinal))
                            entry.ExtractToFile(destinationPath);
                    }
                }
            }

            using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zipPathAndFile))
            {
                int anzahlGesamt = archive.Entries.Count;
                int anzahlEntpackt = 0;
                if (anzahlGesamt > 0)
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (filePathes == null || filePathes.Length == 0 || filePathes.Contains(entry.FullName))
                        {
                            string entryFullPath = Path.Combine(outputFolder, entry.FullName).TrimEnd('/');
                            if (!entry.FullName.EndsWith("/", StringComparison.OrdinalIgnoreCase))
                            {
                                entry.ExtractToFile(entryFullPath, true);
                            }
                            else
                            {
                                if (!Directory.Exists(entryFullPath))
                                {
                                    Directory.CreateDirectory(entryFullPath);
                                }
                            }
                            if (this._aborted)
                            {
                                return; // TODO: check how to abort correctly.
                            }
                            this.OnZipProgressChanged(this, new ProgressChangedEventArgs((byte)(++anzahlEntpackt / anzahlGesamt), 1));
                        }
                    }
                }
            }
            this.OnZipProgressFinished(this, new EventArgs());
        }

        /// <summary>
        /// Entpackt alle ZIP-Archive aus dem Verzeichnis zipsFolder in ein
        /// neues Unterverzeichnis mit dem Namen des Archivs im outputFolder.
        /// </summary>
        /// <param name="zipsFolder">Das Verzeichnis mit den zu entpackenden ZIP-Archiven</param>
        /// <param name="outputFolder">Das Ziel-Verzeichnis</param>
        /// <param name="password">Ein optionales gemeinsames Passwort oder null</param>
        /// <param name="moveZipFile">Wenn true, wird das Archiv nach erfolgreichem Entpacken nach 'moveDir' verschoben;<br />
        ///                           Sollte 'moveDir' leer oder ungültig sein, wird das ZIP-Archiv gelöscht.</param>
        /// <param name="moveDir">Ein Verzeichnis, in das die Archive nach dem Entpacken geschoben werden.</param>
        /// <returns>Liste der neuen Verzeichnisse im outputFolder mit den entpackten Dateien</returns>
        public List<string> UnzipAllArchives(string zipsFolder, string outputFolder, string password, bool moveZipFile, string moveDir)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Packt alles aus dem Verzeichnis inputFolderPath in das Archiv
        /// zipPathAndFile incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="inputFolderPath">Das Verzeichnis mit den zu packenden Daten</param>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort</param>
        /// <param name="packRootAsDir">Den Ordner im Zip mit anlegen</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        public List<ZippedFileInfo> ZipDirectory(string inputFolderPath, string zipPathAndFile, string? password, bool packRootAsDir)
        {
            ZipFile.CreateFromDirectory(inputFolderPath, zipPathAndFile);
            return GetZipEntryList(zipPathAndFile, password);
        }

        /// <summary>
        /// Packt die Files aus "filePathes" in ein Archiv "zipPathAndFile".
        /// Optional kann ein Passwort mitgegeben werden.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs.</param>
        /// <param name="commonRootPath">Pfad und Name des Verzeichnisses, unterhalb dem sich alle zu packenden Dateien/Verzeichnisse befinden müssen.</param>
        /// <param name="password">Passwort für das Archiv oder null.</param>
        /// <param name="packRootAsDir">Bei true wird das Root-Verzeichnis mit eingepackt, bei false nur die enthaltenen Dateien/Verzeichnisse.</param>
        /// <param name="filePathes">String-Array mit Pfaden der zu packenden Dateien/Verzeichnissen.</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        public List<ZippedFileInfo> ZipFiles(string zipPathAndFile, string commonRootPath, string password, bool packRootAsDir, string[] filePathes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Muss im externen EventHandler aufgerufen werden werden,
        /// wenn der laufende Vorgang abgebrochen werden soll.
        /// </summary>
        public void Abort()
        {
            this._aborted = true;
        }

        #endregion MSCompression Routinen

        #region helper

        private volatile bool _aborted = false;

        private void OnZipProgressFinished(object sender, EventArgs e)
        {
            if (ZipProgressFinished != null)
            {
                ZipProgressFinished(this, new ProgressChangedEventArgs(100, null));
            }
        }

        /// <summary>
        /// Löst das ZipProgressChanged-Ereignis aus.
        /// </summary>
        private void OnZipProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (ZipProgressChanged != null)
            {
                ZipProgressChanged(this, new ProgressChangedEventArgs((int)(e.ProgressPercentage), null));
            }
        }

        #endregion helper

    }
}

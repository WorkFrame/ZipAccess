using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using SevenZip;

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
    public class SevenZipWorker : IZipWorker
    {
        /// <summary>
        /// Ereignis das Eintritt, wenn sich der Fortschritt von ZipAccess ändert.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressChanged;

        /// <summary>
        /// Ereignis das Eintritt, wenn ZipAccess beendet wird.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressFinished;

        #region SevenZip Routinen

        /// <summary>
        /// Standard-Konstruktor - Setzt den Pfad zur passenden 7Zip-dll (32/64).
        /// </summary>
        public SevenZipWorker()
        {
            System.Reflection.Assembly? entryAssembly = System.Reflection.Assembly.GetEntryAssembly();
            if (entryAssembly == null)
            {
                entryAssembly = System.Reflection.Assembly.GetExecutingAssembly();
            }
            string dllPath = Path.Combine(Path.GetDirectoryName(
              (new System.Uri(entryAssembly.Location)).AbsolutePath) ?? "", @"7z32.dll");
            if (Environment.Is64BitOperatingSystem)
            {
                dllPath = dllPath.Replace(@"7z32.dll", @"7z64.dll");
            }
            SevenZipBase.SetLibraryPath(WebUtility.HtmlEncode(dllPath).Replace("%20", " "));
        }

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt</returns>
        public bool IsZip(string zipPathAndFile)
        {
            return this.IsZip(zipPathAndFile, null);
        }
        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv.</param>
        /// <param name="password">Ein optionales Passwort.</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt.</returns>
        public bool IsZip(string zipPathAndFile, string? password = null)
        {
            SevenZipExtractor? extractor = null;
            try
            {
                if (!File.Exists(zipPathAndFile))
                {
                    return false;
                }
                if (!String.IsNullOrEmpty(password))
                {
                    extractor = new SevenZipExtractor(zipPathAndFile, password);
                }
                else
                {
                    extractor = new SevenZipExtractor(zipPathAndFile);
                }
                ReadOnlyCollection<ArchiveFileInfo> infos = extractor.ArchiveFileData;
                if (infos != null && infos.Count > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
            finally
            {
                if (extractor != null)
                {
                    extractor.Dispose();
                }
            }
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als List&lt;ZippedFileInfo&gt;.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        public List<ZippedFileInfo> GetZipEntryList(string zipPathAndFile, string? password = null)
        {
            SevenZipExtractor extractor;
            if (!String.IsNullOrEmpty(password))
            {
                extractor = new SevenZipExtractor(zipPathAndFile, password);
            }
            else
            {
                extractor = new SevenZipExtractor(zipPathAndFile);
            }
            return this.SevenGetZipEntryInfos(extractor);
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als List&lt;ZippedFileInfo&gt;.
        /// </summary>
        /// <param name="extractor">Ein gültiger SevenZipExtractor.</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        protected List<ZippedFileInfo> SevenGetZipEntryInfos(SevenZipExtractor extractor)
        {
            List<ZippedFileInfo> zipList = new List<ZippedFileInfo>();
            ReadOnlyCollection<ArchiveFileInfo> archiveFileInfos = extractor.ArchiveFileData;
            foreach (ArchiveFileInfo archiveFileInfo in archiveFileInfos)
            {
                ZippedFileInfo info = new ZippedFileInfo();
                info.FilePath = archiveFileInfo.FileName;
                info.LastWriteTime = archiveFileInfo.LastWriteTime;
                info.Size = Convert.ToInt64(archiveFileInfo.Size);
                info.IsDirectory = archiveFileInfo.IsDirectory;
                zipList.Add(info);
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
        public void UnZipArchiveFiles(string zipPathAndFile, string outputFolder, string? password, bool deleteZipFile, string[]? filePathes = null)
        {
            SevenZipExtractor? extractor = null;
            Stream? reader = null;
            try
            {
                if (!String.IsNullOrEmpty(password))
                {
                    reader = new FileStream(zipPathAndFile, FileMode.Open); // You can change Open mode to OpenOrCreate
                    extractor = new SevenZipExtractor((Stream)reader, password);
                }
                else
                {
                    reader = new FileStream(zipPathAndFile, FileMode.Open); // You can change Open mode to OpenOrCreate
                    extractor = new SevenZipExtractor((Stream)reader);
                }
                extractor.Extracting += OnSevenZipProgressChanged;
                extractor.ExtractionFinished += OnZipProgressFinished;
                //extractor.EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous;
                if (filePathes == null || filePathes.Length == 0)
                {
                    extractor.ExtractArchive(outputFolder);
                }
                else
                {
                    string[] newFilePathes = filePathes.Where(p => (!p.EndsWith("/") && !(p.EndsWith("\\")))).ToArray();
                    extractor.ExtractFiles(outputFolder, newFilePathes);
                }
                if (deleteZipFile)
                {
                    File.Delete(zipPathAndFile);
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                    reader.Dispose();
                }
                if (extractor != null)
                {
                    extractor.Dispose();
                }
                GC.Collect();                   // nur so
                GC.WaitForPendingFinalizers();  // geht's!
            }
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
        /// <param name="inputFolderPath">Das Verzeichnis mit den zu packenden Daten.</param>
        /// <param name="zipPath">Das zu erzeugende Zip-Archiv.</param>
        /// <param name="password">Ein optionales Passwort.</param>
        /// <param name="packRootAsDir">Bei true den Ordner im Zip mit anlegen.</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        public List<ZippedFileInfo> ZipDirectory(string inputFolderPath, string zipPath, string? password, bool packRootAsDir)
        {
            string absZipPath = Path.GetFullPath(zipPath);
            long allBytes;
            string[] filePathes = StaticZipHelpers.GenerateFileList(inputFolderPath, out allBytes).Select(x => x.Replace(inputFolderPath, "").TrimStart('\\')).ToArray();
            return this.ZipFiles(absZipPath, inputFolderPath, null, true, filePathes);
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
        public List<ZippedFileInfo> ZipFiles(string zipPathAndFile, string commonRootPath, string? password, bool packRootAsDir, string[] filePathes)
        {
            string actWorkingDirectory = Directory.GetCurrentDirectory();
            if (Directory.Exists(commonRootPath))
            {
                Directory.SetCurrentDirectory(commonRootPath);
            }
            else
            {
                throw new DirectoryNotFoundException(String.Format("Das Verzeichnis '{0}' wurde nicht gefunden.", commonRootPath));
            }
            SevenZipCompressor compressor = new SevenZipCompressor();
            compressor.Compressing += OnSevenZipProgressChanged;
            compressor.CompressionFinished += OnZipProgressFinished;
            compressor.ArchiveFormat = OutArchiveFormat.Zip;
            compressor.FastCompression = false; // bei true werden keine Events gefeuert.
            compressor.CompressionMethod = SevenZip.CompressionMethod.Default;
            //tmp.CompressionMethod = CompressionMethod.Ppmd;
            //tmp.CompressionLevel = CompressionLevel.High;
            compressor.TempFolderPath = System.IO.Path.GetTempPath();
            //compressor.EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous;
            compressor.IncludeEmptyDirectories = true;
            compressor.PreserveDirectoryRoot = packRootAsDir;
            compressor.DirectoryStructure = true;
            compressor.FileCompressionStarted += (s, e) =>
            {
                if (this._aborted)
                {
                    e.Cancel = true;
                }
                else
                {
                    this.OnZipProgressChanged(s, new ProgressChangedEventArgs(e.PercentDone, e.PercentDone));
                    //Console.WriteLine(String.Format("[{0}%] {1}", e.PercentDone, e.FileName));
                }
            };
            compressor.FilesFound += (se, ea) =>
            {
                Console.WriteLine("Number of files: " + ea.Value.ToString());
            };
            try
            {
                if (!string.IsNullOrEmpty(password))
                {
                    compressor.CompressFilesEncrypted(zipPathAndFile, password, filePathes);
                }
                else
                {
                    compressor.CompressFilesEncrypted(zipPathAndFile, password, filePathes);
                }
                return this.GetZipEntryList(zipPathAndFile, password);
            }
            finally
            {
                Directory.SetCurrentDirectory(actWorkingDirectory);
            }
        }

        /// <summary>
        /// Muss im externen EventHandler aufgerufen werden werden,
        /// wenn der laufende Vorgang abgebrochen werden soll.
        /// </summary>
        public void Abort()
        {
            this._aborted = true;
        }

        #region experimental

        /// <summary>
        /// Packt alles aus dem Verzeichnis inputFolderPath in das Archiv
        /// zipPathAndFile incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="inputFolderPath">Das Verzeichnis mit den zu packenden Daten</param>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort</param>
        /// <param name="packRootAsDir">Den Ordner im Zip mit anlegen</param>
        protected void SevenZipDirectoryAtOnce(string inputFolderPath, string zipPathAndFile, string password, bool packRootAsDir)
        {
            SevenZipCompressor compressor = new SevenZipCompressor();
            compressor.Compressing += OnSevenZipProgressChanged;
            compressor.CompressionFinished += OnZipProgressFinished;
            compressor.FileCompressionStarted += (s, e) =>
            {
                if (this._aborted)
                {
                    e.Cancel = true;
                }
                else
                {
                    this.OnZipProgressChanged(s, new ProgressChangedEventArgs(e.PercentDone, e.PercentDone));
                    //Console.WriteLine(String.Format("[{0}%] {1}", e.PercentDone, e.FileName));
                }
            };
            compressor.FilesFound += (se, ea) =>
            {
                //Console.WriteLine("Number of files: " + ea.Value.ToString());
            };
            compressor.ArchiveFormat = OutArchiveFormat.Zip;
            compressor.FastCompression = false; // bei true werden keine Events gefeuert.
                                                //compressor.CompressionMethod = SevenZip.CompressionMethod.Default; // Default feuert keine Events.
            compressor.CompressionMethod = SevenZip.CompressionMethod.Lzma;
            compressor.TempFolderPath = System.IO.Path.GetTempPath();
            compressor.EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous;
            compressor.IncludeEmptyDirectories = true;
            compressor.DirectoryStructure = true;
            compressor.PreserveDirectoryRoot = packRootAsDir;
            Directory.SetCurrentDirectory(inputFolderPath);
            if (!String.IsNullOrEmpty(password))
            {
                compressor.BeginCompressDirectory(inputFolderPath, zipPathAndFile, password);
            }
            else
            {
                compressor.BeginCompressDirectory(inputFolderPath, zipPathAndFile, password);
            }
        }

        /// <summary>
        /// Entpackt alle ZIP-Archive aus dem Verzeichnis zipsFolder in ein
        /// gegebenes Verzeichnis in Unterverzeichnisse mit den Namen
        /// der jeweiligen Archive.
        /// </summary>
        /// <param name="zipsFolder">Das Verzeichnis mit den zu entpackenden ZIP-Archiven</param>
        /// <param name="outputFolder">Das Ziel-Verzeichnis</param>
        /// <param name="password">Ein optionales gemeinsames Passwort oder null</param>
        /// <param name="moveZipFile">Wenn true, wird das Archiv nach erfolgreichem Entpacken nach 'moveDir' verschoben;<br />
        ///                           Sollte 'moveDir' leer oder ungültig sein, wird das ZIP-Archiv gelöscht.</param>
        /// <param name="moveDir">Ein Verzeichnis, in das die Archive nach dem Entpacken geschoben werden.</param>
        /// <returns>Liste der neuen Verzeichnisse im outputFolder mit den entpackten Dateien</returns>
        protected List<string> SevenUnzipAllArchives(string zipsFolder, string outputFolder, string password, bool moveZipFile, string moveDir)
        {
            this._aborted = false;
            List<string> archivDirectoryNameItems = new List<string>();
            if (Directory.Exists(zipsFolder))
            {
                string[] archivItems = Directory.GetFiles(zipsFolder, "*.zip", SearchOption.AllDirectories);
                int countItems = archivItems.Length;
                int processedItems = 0;
                foreach (string archiv in archivItems)
                {
                    FileInfo dateiArchiv = new FileInfo(archiv);
                    // string archivName = Path.Combine(dateiArchiv.DirectoryName, dateiArchiv.Name);
                    string archivDirectoryName = Path.Combine(outputFolder, dateiArchiv.Name);
                    archivDirectoryName = archivDirectoryName.Substring(0, archivDirectoryName.Length - 4);
                    Directory.CreateDirectory(archivDirectoryName);
                    if (ZipProgressChanged != null)
                    {
                        ZipProgressChanged(this, new ProgressChangedEventArgs(++processedItems * 100 / countItems, this)); // Anzahl Archive
                    }
                    this.UnZipArchiveFiles(archiv, archivDirectoryName, null, false, new string[] { });
                    archivDirectoryNameItems.Add(archivDirectoryName);
                    if (moveZipFile)
                    {
                        if (Directory.Exists(moveDir))
                        {
                            File.Copy(archiv, Path.Combine(moveDir, Path.GetFileName(archiv)), true);
                            File.Delete(archiv);
                        }
                        else
                        {
                            File.Delete(archiv);
                        }
                    }
                    if (this._aborted)
                    {
                        break;
                    }
                }
            }
            return archivDirectoryNameItems;
        }

        #endregion experimental

        #endregion SevenZip Routinen

        #region helper

        private volatile bool _aborted = false;

        /// <summary>
        /// Löst das ZipProgressChanged-Ereignis aus.
        /// </summary>
        private void OnSevenZipProgressChanged(object? sender, ProgressEventArgs e)
        {
            if (ZipProgressChanged != null)
            {
                ZipProgressChanged(this, new ProgressChangedEventArgs((int)(e.PercentDone), null));
            }
        }

        /// <summary>
        /// Löst das ZipProgressChanged-Ereignis aus.
        /// </summary>
        private void OnZipProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (ZipProgressChanged != null)
            {
                ZipProgressChanged(this, new ProgressChangedEventArgs((int)(e.ProgressPercentage), null));
            }
        }

        private void OnZipProgressFinished(object? sender, EventArgs e)
        {
            if (ZipProgressFinished != null)
            {
                ZipProgressFinished(this, new ProgressChangedEventArgs(100, null));
            }
        }

        #endregion helper

    }
}

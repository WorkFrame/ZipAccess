using System;
using System.Collections.Generic;
using System.ComponentModel;
using ICSharpCode.SharpZipLib.Zip;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SevenZip;
using System.Collections.ObjectModel;
using System.Threading;
using NetEti.Globals;
using System.Net;

namespace NetEti.FileTools.Zip
{
    /// <summary>
    /// Informationen für eine Datei im Archiv:
    /// Pfad, Größe, Timestamp und Flag IsDirectory.
    /// </summary>
    public class ZippedFileInfo
    {
        /// <summary>
        /// Dateiname und -Pfad der Datei im Archiv.
        /// </summary>
        public string FilePath { get; internal set; }

        /// <summary>
        /// Größe der Datei im Archiv.
        /// </summary>
        public long Size { get; internal set; }

        /// <summary>
        /// Timestamp der Datei im Archiv.
        /// </summary>
        public DateTime LastWriteTime { get; internal set; }

        /// <summary>
        /// Bei true handelt es sich um ein Verzeichnis.
        /// </summary>
        public bool IsDirectory { get; internal set; }

        /// <summary>
        /// Überschriebene ToString-Methode.
        /// </summary>
        /// <returns>ZippedFileInfo als aufbereiteten String.</returns>
        public override string ToString()
        {
            string dirOrFile = IsDirectory ? "DIR" : "FILE";
            string sizeString = IsDirectory ? "" : String.Format(", Größe: {0} K", Size / 1024);
            return String.Format("{0} ({1}{2})", FilePath, dirOrFile, sizeString);
        }
    }

    /// <summary>
    /// Funktion: Routinen zum Packen und Entpacken von ZIP-Archiven.
    ///           Nutzt die SevenZipSharp.dll, System.IO.Compress und SevenZip
    ///           Die mit Ics beginnenden Routinen setzen auf 'ICSharpCode.SharpZipLib.dll' und 7Zip auf.
    /// </summary>
    /// <remarks>
    /// File: ZipAccess.cs<br />
    /// Die Microsoft eigenen Routinen legen bei Archiven > 4GIG die Ohren an und können keine Passwörter!<br></br>
    /// Autor: Peter Bromberg (http://www.eggheadcafe.com/tutorials/aspnet/9ce6c242-c14c-4969-9251-af95e4cf320f/zip--unzip-folders-and-f.aspx)<br />
    /// Vielen Dank dafür.
    /// zurechtgepfuscht von: Erik Nagel, NetEti<br />
    /// <br></br>
    /// 09.03.2012 Erik Nagel: erstellt<br />
    /// 26.03.2012 Erik Nagel: Fehlerkorrektur bei ZIPs mit mehreren Entries.
    /// 30.03.2012 Erik Nagel: Umstellung von statisch auf instantiiert wegen Problemen bei Abbruch und Wiederanlauf;
    ///                        Fortschrittsmeldungen über Events und mit mehr Infos.
    /// 25.04.2013 Erik Nagel: Archive werden jetzt mit SevenZipSharp.SevenZipExtractor entpackt.
    /// 26.04.2013 Erik Nagel: 7zip.dll wird jetzt differenziert für 32bit- oder 64bit-Systeme geladen (7z32.dll/7z64.dll).
    /// 16.02.2014 Erik Nagel: Pfad für 7zip.dll wird jetzt über HTMLEncode vorbereitet.
    /// 16/17.10.2016 Erik Nagel: Überarbeitet und neu strukturiert.
    /// 11.10.2017 Erik Nagel: Exceptions bei 7Zip auf neuere Windows(Deflate?)-Formate abgefangen
    ///                        und auf System.IO.Compression - Routinen umgeleitet.
    /// </remarks>
    public class ZipAccess : IDisposable
    {
        #region IDisposable Member

        /// <summary>
        /// Öffentliche Methode zum Aufräumen.
        /// </summary>
        public void Dispose()
        {
            this.dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Hier kann aufgeräumt werden.
        /// </summary>
        /// <param name="disposing">False, wenn vom eigenen Destruktor aufgerufen.</param>
        protected virtual void dispose(bool disposing)
        {
            if (disposing)
            {
                this._aborted = true;
                Thread.Sleep(100);
            }
        }

        /// <summary>
        /// Finalizer: wird vom GarbageCollector aufgerufen.
        /// </summary>
        ~ZipAccess()
        {
            this.dispose(false);
        }

        #endregion IDisposable Member

        /// <summary>
        /// Ereignis das Eintritt, wenn sich der Fortschritt von ZipAccess ändert.
        /// </summary>
        public event ProgressChangedEventHandler ZipProgressChanged;

        /// <summary>
        /// Ereignis das Eintritt, wenn sich der Fortschritt von ZipAccess ändert.
        /// </summary>
        public event CommonProgressChangedEventHandler ExtendedZipProgressChanged;

        /// <summary>
        /// Ereignis das Eintritt, wenn ZipAccess beendet wird.
        /// </summary>
        public event ProgressChangedEventHandler ZipProgressFinished;

        #region Allgemeingueltige Routinen

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt</returns>
        public bool IsZip(string zipPathAndFile)
        {
            return this.IsZipICSharp(zipPathAndFile);
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als String-Liste.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>String-Liste mit dem Inhaltsverzeichnis des Archivs</returns>
        public List<ZippedFileInfo> GetZipEntryList(string zipPathAndFile, string password)
        {
            try
            {
                return this.GetZipEntryListICSharp(zipPathAndFile, password);
            }
            catch (Exception)
            {
                return this.MSGetZipEntryList(zipPathAndFile, password);
            }
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
            try
            {
                this.SevenUnZipArchive(zipPathAndFile, outputFolder, password, deleteZipFile, new string[] { });
            }
            catch (Exception)
            {
                this.MSZipUnZipArchive(zipPathAndFile, outputFolder, password, deleteZipFile, new string[] { });
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
                    byte percentDone = (byte)(++processedItems * 100 / countItems);
                    this.OnZipProgressChanged(this, new SevenZip.ProgressEventArgs(percentDone, percentDone));
                    this.OnExtendedZipProgressChanged(this, new SevenZip.ProgressEventArgs(percentDone, percentDone), archiv);
                    this.UnZipArchive(archiv, archivDirectoryName, null, false);
                    archivDirectoryNameItems.Add(archivDirectoryName);
                    if (moveZipFile)
                    {
                        if (Directory.Exists(moveDir))
                        {
                            if (archiv != null)
                            {
                                File.Copy(archiv, Path.Combine(moveDir, Path.GetFileName(archiv)), true);
                                File.Delete(archiv);
                            }
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

        /// <summary>
        /// Entpackt alles aus dem Archiv incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs</param>
        /// <param name="outputFolder">Wohin entpackt werden soll</param>
        /// <param name="password">Passwort oder null</param>
        /// <param name="deleteZipFile">ob das Archiv hinterher gelöscht werden soll</param>
        /// <param name="filePathes">Liste der zu entpackenden Dateien (inklusive relative Pfade)</param>
        public void UnZipArchiveFiles(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile, string[] filePathes)
        {
            try
            {
                this.SevenUnZipArchive(zipPathAndFile, outputFolder, password, deleteZipFile, filePathes);
            }
            catch (Exception)
            {
                this.MSZipUnZipArchive(zipPathAndFile, outputFolder, password, deleteZipFile, filePathes);
            }
        }

        /// <summary>
        /// Packt alles aus dem Verzeichnis inputFolderPath in das Archiv
        /// zipPathAndFile incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="inputFolderPath">Das Verzeichnis mit den zu packenden Daten</param>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort</param>
        /// <param name="packRootAsDir">Den Ordner im Zip mit anlegen</param>
        public void ZipDirectory(string inputFolderPath, string zipPathAndFile, string password, bool packRootAsDir)
        {
            this.ZipDirectoryICSharp(inputFolderPath, zipPathAndFile, password, packRootAsDir);
        }

        /// <summary>
        /// Liefert eine Liste aller Dateinamen und Unterverzeichnisnamen
        /// und deren Dateinamen und Unterverzeichnisnamen u.s.w. rekursiv.
        /// benutzt die interne Hilfsroutine GenerateFileListIntern.
        /// </summary>
        /// <param name="rootDir">Das Root-Verzeichnis</param>
        /// <param name="allBytes">out-Parameter: liefert die Gesamt-Byte-Anzahl aller Dateien.</param>
        /// <returns>String-Liste mit Datei- und Verzeichnisnamen</returns>
        public List<string> GenerateFileList(string rootDir, out long allBytes)
        {
            allBytes = 0;
            return (GenerateFileListIntern(rootDir, ref allBytes));
        }

        /// <summary>
        /// Muss im externen EventHandler aufgerufen werden werden,
        /// wenn der laufende Vorgang abgebrochen werden soll.
        /// </summary>
        public void Abort()
        {
            this._aborted = true;
        }

        /// <summary>
        /// Standard-Konstruktor - Setzt den Pfad zur passenden 7Zip-dll (32/64).
        /// </summary>
        public ZipAccess()
        {
            SevenZipAccessorInit();
        }

        #endregion Allgemeingueltige Routinen

        #region ICSharp Routinen

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt</returns>
        public bool IsZipICSharp(string zipPathAndFile)
        {
            bool rtn = false;
            ZipInputStream zipIn = null;
            try
            {
                if (File.Exists(zipPathAndFile))
                {
                    zipIn = new ZipInputStream(File.OpenRead(zipPathAndFile));
                    if (zipIn.GetNextEntry() != null)
                    {
                        rtn = true;
                    }
                }
            }
            catch { }
            finally
            {
                if (zipIn != null)
                {
                    try { zipIn.Dispose(); }
                    catch { }
                }
            }
            return rtn;
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als String-Liste.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>String-Liste mit dem Inhaltsverzeichnis des Archivs</returns>
        public List<ZippedFileInfo> GetZipEntryListICSharp(string zipPathAndFile, string password)
        {
            List<ZippedFileInfo> zipList = new List<ZippedFileInfo>();
            ZipEntry actZipEntry = null;
            ZipInputStream zipIn = null;
            try
            {
                zipIn = new ZipInputStream(File.OpenRead(zipPathAndFile));
                if (password != null && password != String.Empty)
                    zipIn.Password = password;
                while ((actZipEntry = zipIn.GetNextEntry()) != null)
                {
                    ZippedFileInfo info = new ZippedFileInfo();
                    info.FilePath = actZipEntry.Name;
                    info.LastWriteTime = actZipEntry.DateTime;
                    info.Size = actZipEntry.Size;
                    info.IsDirectory = actZipEntry.IsDirectory;
                    zipList.Add(info);
                }
            }
            finally
            {
                zipIn.Dispose();
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
        public void UnZipArchiveICSharp(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile)
        {
            // Unkompatibel zu Decompress (25.04.2013 Nagel)
            ZipInputStream zipIn = null;
            FileStream fileOut = null;
            ZippedFileInfo zipInfo;
            ZippedFileInfo[] zipInfos = null;
            this._aborted = false;
            string fullPath = "";
            bool fileSureClosed = true;
            try
            {
                List<ZippedFileInfo> zipInfoList = GetSpecialZipEntryListICSharp(zipPathAndFile, password);
                ZipEntry actZipEntry;

                // 1.Durchgang: Gesamtzahl Files und Bytes ermitteln
                int countItems = 0;
                long allBytes = 0;
                foreach (ZippedFileInfo actInfo in zipInfoList)
                {
                    countItems++;
                    allBytes += actInfo.Size;
                }
                zipInfos = new ZippedFileInfo[zipInfoList.Count];
                zipInfoList.CopyTo(zipInfos);
                if (allBytes < 1)
                {
                    allBytes = 1;
                }

                // 2.Durchgang: alles auspacken
                if (outputFolder.Trim().Equals(""))
                {
                    outputFolder = Path.GetDirectoryName(zipPathAndFile);
                    if (outputFolder.Trim().Equals(""))
                    {
                        outputFolder = ".";
                    }
                }
                string rootDirectoryName = Path.GetFullPath(outputFolder.Trim());
                if (!Directory.Exists(rootDirectoryName))
                {
                    Directory.CreateDirectory(rootDirectoryName);
                }
                zipIn = new ZipInputStream(File.OpenRead(zipPathAndFile));
                if (password != null && password != String.Empty)
                    zipIn.Password = password;
                int percentDone = 0;
                int lastPercentDone = 0;
                long allBytesRead = 0;
                while ((!this._aborted) && ((actZipEntry = zipIn.GetNextEntry()) != null))
                {
                    string fileName = Path.GetFileName(actZipEntry.Name);
                    string relativePath = Path.GetDirectoryName(actZipEntry.Name);
                    string absolutePath = rootDirectoryName
                                          + Path.DirectorySeparatorChar
                                          + relativePath;
                    // create directory
                    if (!Directory.Exists(absolutePath))
                    {
                        Directory.CreateDirectory(absolutePath);
                    }
                    if (fileName != String.Empty)
                    {
                        zipInfo = new ZippedFileInfo();
                        // if (actZipEntry.Name.IndexOf(".ini") < 0)
                        // {
                        zipInfo.FilePath = Path.GetDirectoryName(zipPathAndFile)
                            + Path.DirectorySeparatorChar + actZipEntry.Name;
                        zipInfo.Size = actZipEntry.Size;
                        zipInfo.LastWriteTime = actZipEntry.DateTime;

                        fullPath = rootDirectoryName + Path.DirectorySeparatorChar + actZipEntry.Name;
                        fileSureClosed = false;
                        fileOut = File.Create(fullPath);
                        int nBytes = 8388608; // 8MB = willkürliche, größere Zweierpotenz
                        if ((long)nBytes > actZipEntry.Size)
                        {
                            nBytes = (int)(actZipEntry.Size);
                        }
                        byte[] data = new byte[nBytes];
                        long entryBytesRead = 0;
                        while ((!this._aborted) && ((nBytes = zipIn.Read(data, 0, data.Length)) > 0))
                        {
                            // liest u.U. über das Ende des aktuellen actZipEntry hinaus,
                            // deshalb zusätzliche Prüfung
                            if (entryBytesRead + nBytes > actZipEntry.Size)
                            {
                                nBytes = (int)(actZipEntry.Size - entryBytesRead);
                            }
                            if (nBytes > 0)
                            {
                                fileOut.Write(data, 0, nBytes);
                                entryBytesRead += nBytes;
                                allBytesRead += nBytes;
                                percentDone = (int)(allBytesRead * 100 / allBytes);
                                if (percentDone > lastPercentDone)
                                {
                                    //OnZipProgressChanged(zipPathAndFile + ": " + actZipEntry.Name, allBytes, allBytesRead, ItemsTypes.itemParts);
                                    lastPercentDone = percentDone;
                                }
                            }
                            else
                            {
                                break;
                            }
                        } // while ((!ZipAccess.aborted) && ((nBytes = zipIn.Read(data, 0, data.Length)) > 0))
                        fileOut.Close();
                        if (!this._aborted)
                        {
                            if (zipInfo.LastWriteTime.CompareTo(DateTime.MinValue) > 0)
                            {
                                File.SetLastWriteTime(fullPath, zipInfo.LastWriteTime);
                            }
                        }
                        else
                        {
                            try { File.Delete(fullPath); }
                            catch (Exception) { }
                        } // if (!ZipAccess.aborted)
                        fileSureClosed = true;
                        // } // if (actZipEntry.Name.IndexOf(".ini") < 0)
                    } // if (fileName != String.Empty)
                      //OnZipProgressChanged(zipPathAndFile + ": " + actZipEntry.Name, countItems, ++processedItems, ItemsTypes.items);
                } // while ((actZipEntry = s.GetNextEntry()) != null)
            }
            catch (Exception ex)
            {
                if (!fileSureClosed)
                {
                    try { fileOut.Close(); }
                    catch (Exception) { };
                    fileSureClosed = true;
                    try { File.Delete(fullPath); }
                    catch (Exception) { };
                }
                throw (new Exception("Fehler in gepackter Datei " + zipPathAndFile, ex));
            }
            finally
            {
                try
                {
                    if (!fileSureClosed)
                    {
                        try { fileOut.Close(); }
                        catch (Exception) { };
                    }
                    if (fileOut != null) fileOut.Dispose();
                }
                catch (Exception) { };
                try
                {
                    try { zipIn.Close(); }
                    catch (Exception) { };
                    if (zipIn != null) zipIn.Dispose();
                }
                catch (Exception) { };
            }
            if (deleteZipFile)
            {
                File.Delete(zipPathAndFile);
            }
            // Unkompatibel zu Decompress (25.04.2013 Nagel)
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als Liste mit ZippedFileInfos.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>Das Inhaltsverzeichnis des Archivs</returns>
        private static List<ZippedFileInfo> GetSpecialZipEntryListICSharp(string zipPathAndFile, string password)
        {
            List<ZippedFileInfo> zipInfoList = new List<ZippedFileInfo>();
            ZipEntry actZipEntry;
            ZippedFileInfo zipInfo;

            ZipInputStream zipIn = new ZipInputStream(File.OpenRead(zipPathAndFile));
            if (password != null && password != String.Empty)
                zipIn.Password = password;
            while ((actZipEntry = zipIn.GetNextEntry()) != null)
            {
                zipInfo = new ZippedFileInfo();
                zipInfo.FilePath = Path.GetDirectoryName(zipPathAndFile) + Path.DirectorySeparatorChar + actZipEntry.Name;
                zipInfo.Size = actZipEntry.Size;
                zipInfo.LastWriteTime = actZipEntry.DateTime;
                zipInfoList.Add(zipInfo);
            }

            zipIn.Close();
            zipIn.Dispose();
            return zipInfoList;
        }

        /// <summary>
        /// Packt alles aus dem Verzeichnis inputFolderPath in das Archiv
        /// zipPathAndFile incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="inputFolderPath">Das Verzeichnis mit den zu packenden Daten</param>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort</param>
        /// <param name="packRootAsDir">Den Ordner im Zip mit anlegen</param>
        public void ZipDirectoryICSharp(string inputFolderPath, string zipPathAndFile, string password, bool packRootAsDir)
        {
            ZipOutputStream zipOut = null;
            FileStream fileIn = null;
            ZippedFileInfo[] zipInfos = null;
            this._aborted = false;
            bool incomplete = false;
            bool fileSureClosed = true;
            string outPath = "";
            long allBytes = 0;
            try
            {
                List<ZippedFileInfo> zipInfoList = new List<ZippedFileInfo>();
                inputFolderPath = Path.GetFullPath(inputFolderPath);
                inputFolderPath = inputFolderPath.TrimEnd(' ', '\\');
                List<string> filePathNames = this.GenerateFileList(inputFolderPath, out allBytes); // generate file list
                int countItems = filePathNames.Count;

                int TrimLength = 0;
                if (packRootAsDir)
                {
                    TrimLength = (Directory.GetParent(inputFolderPath)).ToString().Length;
                }
                else
                {
                    TrimLength = inputFolderPath.Length;
                }
                // find number of chars to remove     // from orginal file path
                TrimLength += 1; //remove '\'
                outPath = zipPathAndFile;
                if (zipPathAndFile.Trim().Equals(""))
                {
                    outPath = inputFolderPath + @"\" + Path.GetFileName(inputFolderPath) + ".zip";
                }
                if (File.Exists(outPath))
                {
                    File.Delete(outPath);
                }
                ZipEntry actZipEntry;
                int processedItems = 0;
                int percentDone = 0;
                int lastPercentDone = 0;
                long allBytesRead = 0;
                foreach (string file in filePathNames) // for each file, generate a zipentry
                {
                    if (!Path.GetFullPath(file).Equals(outPath))
                    {
                        FileInfo fi = new FileInfo(file);
                        string subPath = file.Remove(0, TrimLength);
                        if (!subPath.Equals(""))
                        {
                            if (zipOut == null)
                            {
                                zipOut = new ZipOutputStream(File.Create(outPath)); // create zip stream
                                if (password != null && password != String.Empty)
                                {
                                    zipOut.Password = password;
                                }
                                zipOut.SetLevel(5);
                            }
                            actZipEntry = new ZipEntry(subPath);
                            if (!file.EndsWith(@"/")) // if a file ends with '/' its a directory
                            {
                                actZipEntry.Size = fi.Length;
                            }
                            else
                            {
                                actZipEntry.Size = 0;
                            }
                            actZipEntry.DateTime = fi.LastWriteTime;
                            ZippedFileInfo zipInfo = new ZippedFileInfo();
                            zipInfo.FilePath = Path.GetDirectoryName(outPath) + Path.DirectorySeparatorChar + actZipEntry.Name;
                            zipInfo.Size = actZipEntry.Size;
                            zipInfo.LastWriteTime = actZipEntry.DateTime;
                            zipInfo.IsDirectory = actZipEntry.IsDirectory;
                            zipInfoList.Add(zipInfo);

                            zipOut.PutNextEntry(actZipEntry);

                            if (!file.EndsWith(@"/")) // if a file ends with '/' its a directory
                            {
                                fileSureClosed = false;
                                fileIn = File.OpenRead(file);
                                int nBytes = 8388608; // 8MB = willkürliche, größere Zweierpotenz
                                if ((long)nBytes > fileIn.Length)
                                {
                                    nBytes = (int)(fileIn.Length);
                                }
                                byte[] data = new byte[nBytes];
                                long entryBytesRead = 0;
                                while ((!this._aborted) && ((nBytes = fileIn.Read(data, 0, data.Length)) > 0))
                                {
                                    zipOut.Write(data, 0, nBytes);
                                    entryBytesRead += nBytes;
                                    allBytesRead += nBytes;
                                    percentDone = (int)(allBytesRead * 100 / allBytes);
                                    if (percentDone > lastPercentDone)
                                    {
                                        ZipProgressChanged(this, new ProgressChangedEventArgs((int)(allBytesRead * 100 / allBytes), this)); // Anzahl Archive
                                        lastPercentDone = percentDone;
                                    }
                                } // while ((!ZipAccess.aborted) && ((nBytes = fileIn.Read(data, 0, data.Length)) > 0))
                                fileIn.Close();
                                fileSureClosed = true;
                            } // if (!file.EndsWith(@"/"))
                        } // if (!subPath.Equals(""))
                    } // if (!Path.GetFullPath(file).Equals(outPath)) {
                    if (this._aborted)
                    {
                        incomplete = true;
                        break;
                    }
                    ZipProgressChanged(this, new ProgressChangedEventArgs((int)(++processedItems * 100 / countItems), this)); // Files
                } // foreach (string file in filePathNames)
                zipInfos = new ZippedFileInfo[zipInfoList.Count];
                zipInfoList.CopyTo(zipInfos);
            }
            catch (Exception ex)
            {
                incomplete = true;
                throw (new Exception("Fehler beim Packen der Datei " + zipPathAndFile, ex));
            }
            finally
            {
                try
                {
                    if (!fileSureClosed)
                    {
                        try { fileIn.Close(); }
                        catch (Exception) { };
                    }
                    if (fileIn != null) fileIn.Dispose();
                }
                catch (Exception) { };
                try
                {
                    try { zipOut.Finish(); }
                    catch (Exception) { };
                    try { zipOut.Close(); }
                    catch (Exception) { };
                    if (zipOut != null) zipOut.Dispose();
                }
                catch (Exception) { };
                if (incomplete)
                {
                    if (File.Exists(outPath))
                    {
                        File.Delete(outPath);
                    }
                }
            }
            // return (zipInfos);
        }

        #region experimental

        /// <summary>
        /// Entpackt das erste File aus dem Archiv (keine Unterordner)
        /// und liefert Informationen darüber zurück. Wenn maxBytes > 0 ist,
        /// werden zur Ermittlung der Informationen nur maxBytes entpackt.
        /// Diese Routine dient in erster Linie dazu, schnell den Anfang
        /// von gepackten Dumps zu entpacken, um an Informationen zu kommen,
        /// ohne den ganzen Dump (i.d.R. > 3GB) entpacken zu müssen.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs</param>
        /// <param name="newFilePath">
        /// Pfad und Name der entpackten Datei, wenn null oder ""
        /// wird der Original Pfadname aus dem Archiv verwendet
        /// </param>
        /// <param name="maxBytes">Wenn > 0, nur entsprechend Bytes entpacken</param>
        /// <returns>Pfad, Größe und Timestamp der Datei im Archiv</returns>
        public ZippedFileInfo IcsUnzipFirstDumpToFile(String zipPathAndFile, string newFilePath, int maxBytes)
        {
            string newFilePathParameter = newFilePath;
            if (newFilePathParameter != null)
            {
                newFilePathParameter = newFilePathParameter.Trim();
            }
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            ZipInputStream zipIn = null;
            FileStream fileOut = null;
            ZippedFileInfo zipInfo = new ZippedFileInfo();
            zipInfo.FilePath = "";
            zipInfo.Size = 0;
            zipInfo.LastWriteTime = DateTime.MinValue;
            zipInfo.IsDirectory = false;
            this._aborted = false;
            bool fileSureClosed = true;
            bool found = false;
            try
            {
                zipIn = new ZipInputStream(File.OpenRead(zipPathAndFile));
                ZipEntry actZipEntry = null;
                long allBytes = 0;
                long allBytesRead = 0;
                while ((!this._aborted) && ((actZipEntry = zipIn.GetNextEntry()) != null))
                {
                    zipInfo.FilePath = Path.GetDirectoryName(zipPathAndFile) + Path.DirectorySeparatorChar + actZipEntry.Name;
                    zipInfo.Size = actZipEntry.Size;
                    zipInfo.LastWriteTime = actZipEntry.DateTime;
                    zipInfo.IsDirectory = actZipEntry.IsDirectory;
                    if (String.IsNullOrEmpty(newFilePathParameter))
                    {
                        newFilePath = zipInfo.FilePath;
                    }
                    else
                    {
                        newFilePath = newFilePathParameter;
                    }
                    fileSureClosed = false;
                    fileOut = new FileStream(newFilePath, FileMode.Create);
                    int nBytes = maxBytes > 0 ? maxBytes : 8388608; // 8MB = willkürliche, größere Zweierpotenz
                    if ((long)nBytes > actZipEntry.Size)
                    {
                        nBytes = (int)(actZipEntry.Size);
                    }
                    int percentDone = 0;
                    int lastPercentDone = 0;
                    long entryBytesRead = 0;
                    byte[] data = new byte[nBytes];
                    bool firstBuffer = true;
                    while ((!this._aborted) && ((nBytes = zipIn.Read(data, 0, data.Length)) > 0) && ((maxBytes == 0) || (entryBytesRead < maxBytes)))
                    {
                        if (firstBuffer)
                        {
                            if ((enc.GetString(data) + "    ").Substring(0, 4).Equals("TAPE"))
                            {
                                // SQL-Server-Dumps beginnen mit "TAPE"...
                                found = true;
                            }
                            else
                            {
                                // ... ansonsten hier raus und im Haupt-Loop weiter suchen
                                break;
                            }
                        }
                        firstBuffer = false;
                        fileOut.Write(data, 0, nBytes);
                        entryBytesRead += nBytes;
                        allBytesRead += nBytes;
                        allBytes += nBytes; // es gibt noch keine Größen-Vorabschätzung
                        percentDone = (int)(allBytesRead * 100 / maxBytes);
                        if (percentDone > lastPercentDone)
                        {
                            ZipProgressChanged(this, new ProgressChangedEventArgs((int)(allBytesRead * 100 / maxBytes), this));
                            lastPercentDone = percentDone;
                        }
                    } // while ((!ZipAccess.aborted) && ((nBytes = zipIn.Read(data, 0, data.Length)) > 0) && ((maxBytes == 0) || (allBytesRead < maxBytes)))
                    fileOut.Close();
                    fileSureClosed = true;
                    if (!this._aborted && found)
                    {
                        if (zipInfo.LastWriteTime.CompareTo(DateTime.MinValue) > 0)
                        {
                            File.SetLastWriteTime(newFilePath, zipInfo.LastWriteTime);
                        }
                        ZipProgressChanged(this, new ProgressChangedEventArgs(100, this));
                    }
                    else
                    {
                        try { File.Delete(newFilePath); }
                        catch (Exception) { }
                    }
                    if (found)
                    {
                        break; // Erster Dump gewinnt - und raus!
                    }
                } // while ((actZipEntry = zipIn.GetNextEntry()) != null)
            }
            catch (Exception ex)
            {
                if (!fileSureClosed)
                {
                    try { fileOut.Close(); }
                    catch (Exception) { };
                    fileSureClosed = true;
                    try { File.Delete(newFilePath); }
                    catch (Exception) { };
                }
                throw (new Exception("Fehler in gepackter Datei " + zipPathAndFile, ex));
            }
            finally
            {
                if (fileOut != null)
                {
                    if (!fileSureClosed)
                    {
                        try { fileOut.Close(); }
                        catch (Exception) { };
                    }
                    fileOut.Dispose();
                }
                if (zipIn != null)
                {
                    try { zipIn.Close(); }
                    catch (Exception) { };
                    zipIn.Dispose();
                }
            }
            if (found)
            {
                return (zipInfo);
            }
            else
            {
                throw (new Exception("Kein Dump in gepackter Datei " + zipPathAndFile));
            }
        } // UnzipFirstDumpToFile

        /// <summary>
        /// Ruft UnzipFirstDumpToFile(zipPathAndFile, "", 0, null).
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs</param>
        /// <returns>Pfad, Größe und Timestamp der Datei im Archiv</returns>
        public ZippedFileInfo IcsUnzipFirstDumpToFile(String zipPathAndFile)
        {
            return (IcsUnzipFirstDumpToFile(zipPathAndFile, "", 0));
        }

        #endregion experimental

        #endregion ICSharp Routinen

        #region System.IO.Comporession Routinen

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als String-Liste.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>String-Liste mit dem Inhaltsverzeichnis des Archivs</returns>
        public List<ZippedFileInfo> MSGetZipEntryList(string zipPathAndFile, string password)
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
                        info.LastWriteTime = ConvertFromDateTimeOffset(entry.LastWriteTime);
                        info.Size = entry.Length;
                        info.IsDirectory = entry.FullName.EndsWith("/");
                        zipList.Add(info);
                    }
                }
            }
            return zipList;
        }

        /// <summary>
        /// Entpackt alles aus dem Archiv incl. Unterverzeichnis-Strukturen in einen gegebenen Ordner.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs.</param>
        /// <param name="outputFolder">Wohin entpackt werden soll.</param>
        /// <param name="password">Passwort oder null.</param>
        /// <param name="deleteZipFile">Bei true wird das Archiv hinterher gelöscht.</param>
        /// <param name="filePathes">Liste der zu entpackenden Dateien (inklusive relative Pfade)</param>
        public void MSZipUnZipArchive(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile, string[] filePathes)
        {
            using (ZipArchive archive = System.IO.Compression.ZipFile.OpenRead(zipPathAndFile))
            {
                int anzahlGesamt = archive.Entries.Count;
                int anzahlEntpackt = 0;
                if (anzahlGesamt > 0)
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (filePathes.Length == 0 || filePathes.Contains(entry.FullName))
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
                            this.OnZipProgressChanged(this, new ProgressEventArgs((byte)(++anzahlEntpackt / anzahlGesamt), 1));
                        }
                    }
                }
            }
            this.OnZipProgressFinished(this, new EventArgs());
        }

        #endregion System.IO.Comporession Routinen

        #region SevenZip Routinen

        /// <summary>
        /// Standard-Konstruktor - Setzt den Pfad zur passenden 7Zip-dll (32/64).
        /// </summary>
        public void SevenZipAccessorInit()
        {
            string dllPath = Path.Combine(Path.GetDirectoryName(
              (new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().Location)).AbsolutePath), @"7z32.dll");
            if (Environment.Is64BitOperatingSystem)
            {
                dllPath = dllPath.Replace(@"7z32.dll", @"7z64.dll");
            }
            SevenZipBase.SetLibraryPath(WebUtility.HtmlEncode(dllPath).Replace("%20", " "));
        }

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv.</param>
        /// <param name="password">Ein optionales Passwort.</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt.</returns>
        public bool SevenIsZip(string zipPathAndFile, string password = null)
        {
            SevenZipExtractor extractor = null;
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
        public List<ZippedFileInfo> SevenGetZipEntryInfos(string zipPathAndFile, string password = null)
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
        /// Entpackt alles aus dem Archiv incl. Unterverzeichnis-Strukturen in einen gegebenen Ordner.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs.</param>
        /// <param name="outputFolder">Wohin entpackt werden soll.</param>
        /// <param name="password">Passwort oder null.</param>
        /// <param name="deleteZipFile">Bei true wird das Archiv hinterher gelöscht.</param>
        /// <param name="filePathes">Liste der zu entpackenden Dateien (inklusive relative Pfade)</param>
        public void SevenUnZipArchive(string zipPathAndFile, string outputFolder, string password, bool deleteZipFile, string[] filePathes)
        {
            SevenZipExtractor extractor = null;
            Stream reader = null;
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
                extractor.Extracting += OnZipProgressChanged;
                extractor.ExtractionFinished += OnZipProgressFinished;
                //extractor.EventSynchronization = EventSynchronizationStrategy.AlwaysAsynchronous;
                if (filePathes.Length == 0)
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
        /// Packt alles aus dem Verzeichnis inputFolderPath in das Archiv
        /// zipPathAndFile incl. Unterverzeichnis-Strukturen.
        /// </summary>
        /// <param name="inputFolderPath">Das Verzeichnis mit den zu packenden Daten.</param>
        /// <param name="zipPath">Das zu erzeugende Zip-Archiv.</param>
        /// <param name="password">Ein optionales Passwort.</param>
        /// <param name="packRootAsDir">Bei true den Ordner im Zip mit anlegen.</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        public List<ZippedFileInfo> SevenZipDirectory(string inputFolderPath, string zipPath, string password, bool packRootAsDir)
        {
            string absZipPath = Path.GetFullPath(zipPath);
            long allBytes;
            string[] filePathes = this.GenerateFileList(inputFolderPath, out allBytes).Select(x => x.Replace(inputFolderPath, "").TrimStart('\\')).ToArray();
            return this.SevenZipFiles(absZipPath, inputFolderPath, null, true, filePathes);
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
        /// Packt die Files aus "filePathes" in ein Archiv "zipPathAndFile".
        /// Optional kann ein Passwort mitgegeben werden.
        /// </summary>
        /// <param name="zipPathAndFile">Pfad und Name des Archivs.</param>
        /// <param name="commonRootPath">Pfad und Name des Verzeichnisses, unterhalb dem sich alle zu packenden Dateien/Verzeichnisse befinden müssen.</param>
        /// <param name="password">Passwort für das Archiv oder null.</param>
        /// <param name="packRootAsDir">Bei true wird das Root-Verzeichnis mit eingepackt, bei false nur die enthaltenen Dateien/Verzeichnisse.</param>
        /// <param name="filePathes">String-Array mit Pfaden der zu packenden Dateien/Verzeichnissen.</param>
        /// <returns>List&lt;ZippedFileInfo&gt; = Infos über alle Archiv_Einträge.</returns>
        protected List<ZippedFileInfo> SevenZipFiles(string zipPathAndFile, string commonRootPath, string password, bool packRootAsDir, string[] filePathes)
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
            compressor.Compressing += OnZipProgressChanged;
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
                    this.OnZipProgressChanged(s, new SevenZip.ProgressEventArgs(e.PercentDone, e.PercentDone));
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
                return this.SevenGetZipEntryInfos(zipPathAndFile, password);
            }
            finally
            {
                Directory.SetCurrentDirectory(actWorkingDirectory);
            }
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
            compressor.Compressing += OnZipProgressChanged;
            compressor.CompressionFinished += OnZipProgressFinished;
            compressor.FileCompressionStarted += (s, e) =>
            {
                if (this._aborted)
                {
                    e.Cancel = true;
                }
                else
                {
                    this.OnZipProgressChanged(s, new SevenZip.ProgressEventArgs(e.PercentDone, e.PercentDone));
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
        public List<string> SevenUnzipAllArchives(string zipsFolder, string outputFolder, string password, bool moveZipFile, string moveDir)
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
                    ZipProgressChanged(this, new ProgressChangedEventArgs(++processedItems * 100 / countItems, this)); // Anzahl Archive
                    this.SevenUnZipArchive(archiv, archivDirectoryName, null, false, new string[] { });
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
        static DateTime ConvertFromDateTimeOffset(DateTimeOffset dateTime)
        {
            if (dateTime.Offset.Equals(TimeSpan.Zero))
                return dateTime.UtcDateTime;
            else if (dateTime.Offset.Equals(TimeZoneInfo.Local.GetUtcOffset(dateTime.DateTime)))
                return DateTime.SpecifyKind(dateTime.DateTime, DateTimeKind.Local);
            else
                return dateTime.DateTime;
        }

        #endregion experimental

        #endregion SevenZip Routinen

        #region Hilfsroutinen

        private bool _aborted = false;

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
        private void OnZipProgressChanged(object sender, ProgressEventArgs e)
        {
            if (ZipProgressChanged != null)
            {
                ZipProgressChanged(this, new ProgressChangedEventArgs((int)(e.PercentDone), null));
            }
        }

        /// <summary>
        /// Löst das ZipProgressChanged-Ereignis aus.
        /// </summary>
        private void OnExtendedZipProgressChanged(object sender, ProgressEventArgs e, string archiv)
        {
            if (ExtendedZipProgressChanged != null)
            {
                ExtendedZipProgressChanged(this, new CommonProgressChangedEventArgs(archiv, 100, (int)(e.PercentDone), ItemsTypes.itemGroups, archiv));
            }
        }

        /* 12.01.2018 Nagel+
        // Prüft, ob die Date existiert. Wenn nicht, wird eine FileNotFoundException geworfen.
        private bool checkFileExists(string file)
        {
          string bla = Path.GetFullPath(file);
          if (!File.Exists(file))
          {
            throw new FileNotFoundException(String.Format("Die Datei '{0}' wurde nicht gefunden.", file));
          }
          return true;
        }
        12.01.2018 Nagel- */

        /// <summary>
        /// Liefert eine Liste aller Dateinamen und Unterverzeichnisnamen
        /// und deren Dateinamen und Unterverzeichnisnamen u.s.w. rekursiv.
        /// Interne Hilfsroutine von GenerateFileList.
        /// </summary>
        /// <param name="rootDir">Das Root-Verzeichnis für den aktuellen Durchlauf</param>
        /// <param name="allBytes">ref-Parameter: liefert die Gesamt-Byte-Anzahl aller Dateien.</param>
        /// <returns>String-Liste mit Datei- und Verzeichnisnamen</returns>
        private List<string> GenerateFileListIntern(string rootDir, ref long allBytes)
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

        /// <summary>
        /// Liefert ein string-Array mit den FilePathes aus einer ZippedFileInfo-List.
        /// </summary>
        /// <param name="infos">Liste mit ZippedFileInfos.</param>
        /// <returns>String-Array mit den FilePathes aus infos.</returns>
        public string[] GetZipEntryFilePathes(List<ZippedFileInfo> infos)
        {
            return infos.Select(i => i.FilePath).ToList().ToArray();
        }

        #endregion Hilfsroutinen
    }
}

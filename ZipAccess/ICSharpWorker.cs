using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public class ICSharpWorker : IZipWorker
    {
        /// <summary>
        /// Ereignis das Eintritt, wenn sich der Fortschritt von ZipAccess ändert.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressChanged;

        /// <summary>
        /// Ereignis das Eintritt, wenn ZipAccess beendet wird.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressFinished;

        #region ICSharp Routinen

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv mit mindestens einem Eintrag ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt</returns>
        public bool IsZip(string zipPathAndFile)
        {
            using (ZipInputStream zipIn = new ZipInputStream(File.OpenRead(zipPathAndFile)))
            {
                try
                {
                    if (File.Exists(zipPathAndFile))
                    {
                        if (zipIn.GetNextEntry() != null)
                        {
                            return true;
                        }
                    }
                }
                catch { }
            }
            return false;
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
            ZipEntry? actZipEntry = null;
            ZipInputStream? zipIn = null;
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
                zipIn?.Dispose();
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
        public void UnZipArchive(string zipPathAndFile, string outputFolder, string? password, bool deleteZipFile)
        {
            // Unkompatibel zu Decompress (25.04.2013 Nagel)
            ZipInputStream? zipIn = null;
            FileStream? fileOut = null;
            ZippedFileInfo zipInfo;
            ZippedFileInfo[]? zipInfos = null;
            this._aborted = false;
            string fullPath = "";
            bool fileSureClosed = true;
            try
            {
                List<ZippedFileInfo> zipInfoList = GetZipEntryList(zipPathAndFile, password);
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
                    outputFolder = Path.GetDirectoryName(zipPathAndFile) ?? "";
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
                    string relativePath = Path.GetDirectoryName(actZipEntry.Name) ?? "";
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
                                    int progressPercentage = (int)(allBytesRead * 100 / allBytes + .5);
                                    OnZipProgressChanged(this, new ProgressChangedEventArgs(progressPercentage, absolutePath));
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
                    try { fileOut?.Close(); }
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
                        try { fileOut?.Close(); }
                        catch (Exception) { };
                    }
                    if (fileOut != null) fileOut.Dispose();
                }
                catch (Exception) { };
                try
                {
                    try { zipIn?.Close(); }
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
                    this.OnZipProgressChanged(this, new ProgressChangedEventArgs(percentDone, percentDone));
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
        public void UnZipArchiveFiles(string zipPathAndFile, string outputFolder, string? password, bool deleteZipFile, string[] filePathes)
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
            ZipOutputStream? zipOut = null;
            FileStream? fileIn = null;
            ZippedFileInfo[]? zipInfos = null;
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
                List<string> filePathNames = StaticZipHelpers.GenerateFileList(inputFolderPath, out allBytes); // generate file list
                int countItems = filePathNames.Count;

                int TrimLength = 0;
                if (packRootAsDir)
                {
                    DirectoryInfo? directoryInfo = Directory.GetParent(inputFolderPath);
                    if (directoryInfo != null)
                    {
                        TrimLength = directoryInfo.ToString().Length;
                    }
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
                                        if (ZipProgressChanged != null)
                                        {
                                            ZipProgressChanged(this, new ProgressChangedEventArgs((int)(allBytesRead * 100 / allBytes), this)); // Anzahl Archive
                                        }
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
                    if (ZipProgressChanged != null)
                    {
                        ZipProgressChanged(this, new ProgressChangedEventArgs((int)(++processedItems * 100 / countItems), this));
                    }
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
                        try { fileIn?.Close(); }
                        catch (Exception) { };
                    }
                    if (fileIn != null) fileIn.Dispose();
                }
                catch (Exception) { };
                try
                {
                    try { zipOut?.Finish(); }
                    catch (Exception) { };
                    try { zipOut?.Close(); }
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
            return zipInfos.ToList<ZippedFileInfo>();
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

        #endregion ICSharp Routinen

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
            ZipInputStream? zipIn = null;
            FileStream? fileOut = null;
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
                ZipEntry? actZipEntry = null;
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
                            if (ZipProgressChanged != null)
                            {
                                ZipProgressChanged(this, new ProgressChangedEventArgs((int)(allBytesRead * 100 / maxBytes), this));
                            }
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
                        if (ZipProgressChanged != null)
                        {
                            ZipProgressChanged(this, new ProgressChangedEventArgs(100, this));
                        }
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
                    try { fileOut?.Close(); }
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

        #endregion helper

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace NetEti.FileTools.Zip
{
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
    public class ZipAccess : IZipWorker, IDisposable
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
                this.Abort();
                if (this._zipWorker != null)
                {
                    this._zipWorker.ZipProgressFinished -= ZipProgressFinished;
                    this._zipWorker.ZipProgressChanged -= ZipProgressChanged;
                }
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
        public event ProgressChangedEventHandler? ZipProgressChanged;

        /// <summary>
        /// Ereignis das Eintritt, wenn ZipAccess beendet wird.
        /// </summary>
        public event ProgressChangedEventHandler? ZipProgressFinished;

        #region Allgemeingueltige Routinen

        /// <summary>
        /// Prüft, ob eine Datei ein Zip-Archiv ist.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <returns>True, wenn es sich um ein Zip-Archiv handelt</returns>
        public bool IsZip(string zipPathAndFile)
        {
            return this._zipWorker.IsZip(zipPathAndFile);
        }

        /// <summary>
        /// Liefert das Inhaltsverzeichnis des Zip-Archivs als String-Liste.
        /// </summary>
        /// <param name="zipPathAndFile">Das Zip-Archiv</param>
        /// <param name="password">Ein optionales Passwort für das Archiv</param>
        /// <returns>String-Liste mit dem Inhaltsverzeichnis des Archivs</returns>
        public List<ZippedFileInfo> GetZipEntryList(string zipPathAndFile, string? password)
        {
            return this._zipWorker.GetZipEntryList(zipPathAndFile, password);
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
            this._zipWorker.UnZipArchive(zipPathAndFile, outputFolder, password, deleteZipFile);
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
            this._zipWorker.UnZipArchiveFiles(zipPathAndFile, outputFolder, password, deleteZipFile, filePathes);
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
            return this._zipWorker.UnzipAllArchives(zipsFolder, outputFolder, password, moveZipFile, moveDir);
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
            return this._zipWorker.ZipDirectory(inputFolderPath, zipPathAndFile, password, packRootAsDir);
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
            return this._zipWorker.ZipFiles(zipPathAndFile, commonRootPath, password, packRootAsDir, filePathes);
        }

        /// <summary>
        /// Muss im externen EventHandler aufgerufen werden werden,
        /// wenn der laufende Vorgang abgebrochen werden soll.
        /// </summary>
        public void Abort()
        {
            this._zipWorker?.Abort();
            Thread.Sleep(100);
        }

        /// <summary>
        /// Standard-Konstruktor - Setzt den Pfad zur passenden 7Zip-dll (32/64).
        /// </summary>
        public ZipAccess()
        {
            // geht nicht: this._zipWorker = new SevenZipWorker();
            // geht, aber keine ProgressBar bei großen Zip-Entries: this._zipWorker = new MSCompressionWorker();
            this._zipWorker = new ICSharpWorker();
            this._zipWorker.ZipProgressChanged += _zipWorker_ZipProgressChanged;
            this._zipWorker.ZipProgressFinished += _zipWorker_ZipProgressFinished;
        }

        private readonly IZipWorker _zipWorker;

        #endregion Allgemeingueltige Routinen

        #region Hilfsroutinen

        private void _zipWorker_ZipProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            if (ZipProgressChanged != null)
            {
                ZipProgressChanged(this, new ProgressChangedEventArgs((int)(e.ProgressPercentage), null));
            }
        }

        private void _zipWorker_ZipProgressFinished(object? sender, ProgressChangedEventArgs e)
        {
            if (ZipProgressFinished != null)
            {
                ZipProgressFinished(this, new ProgressChangedEventArgs(100, null));
            }
        }

       #endregion Hilfsroutinen
    }
}

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

        /// <summary>
        /// Standard-Konstruktor.
        /// </summary>
        public ZippedFileInfo()
        {
            this.FilePath = "";
        }
    }
}

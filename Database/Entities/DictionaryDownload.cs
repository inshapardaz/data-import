﻿namespace Inshapardaz.DataImport.Database.Entities
{
    public class DictionaryDownload
    {
        public int Id { get; set; }

        public int DictionaryId { get; set; }

        public int FileId { get; set; }

        public string MimeType { get; set; }

        public virtual Dictionary Dictionary { get; set; }

        public virtual File File { get; set; }
    }
}
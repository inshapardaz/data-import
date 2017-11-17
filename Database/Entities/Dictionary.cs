﻿using System;
using System.Collections.Generic;

namespace Inshapardaz.DataImport.Database.Entities
{
    public class Dictionary
    {
        public Dictionary()
        {
            Word = new HashSet<Word>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsPublic { get; set; }
        public Languages Language { get; set; }
        public Guid UserId { get; set; }

        public virtual ICollection<Word> Word { get; set; }

        public virtual ICollection<DictionaryDownload> Downloads { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using Inshapardaz.DataImport.Model;

namespace Inshapardaz.DataImport.Model
{
    public class WordDetails
    {
        public long Id { get; set; }

        public long WordInstanceId { get; set; }

        public Word WordInstance { get; set; }

        public Languages Language { get; set; }
        public GrammaticalType Attributes { get; set; }


        public virtual ICollection<Meaning> Meanings { get; set; }

        public virtual ICollection<Translation> Translations { get; set; }
    }
}

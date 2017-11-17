using System.Collections.Generic;

namespace Inshapardaz.DataImport.Model
{
    public class Word
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string TitleWithMovements { get; set; }

        public string Description { get; set; }

        public string Transiliteral { get; set; }
        
        public virtual ICollection<WordRelation> WordRelations { get; set; }
        public virtual ICollection<WordDetails> WordDetails { get; set; }
        
        public int DictionaryId { get; set; }
        
    }
}

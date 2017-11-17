namespace Inshapardaz.DataImport.Model
{
    public class Meaning
    {
        public int Id { get; set; }
        public string Context { get; set; }
        public string Value { get; set; }

        public string Example { get; internal set; }

        public int WordId { get; set; }

        public virtual Word Word { get; set; }
    }
}

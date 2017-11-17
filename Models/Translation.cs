namespace Inshapardaz.DataImport.Model
{
    public class Translation
    {
        public int Id { get; set; }

        public Languages Language { get; set; }

        public string Value { get; set; }

        public int WordId { get; set; }

        public virtual Word Word { get; set; }
    }
}

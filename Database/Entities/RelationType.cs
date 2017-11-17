using System.ComponentModel;

namespace Inshapardaz.DataImport.Database.Entities
{
    public enum RelationType
    {
        [Description("مترادف")]
        Synonym,
        [Description("متضاد")]
        Acronym,
        [Description("مرکب")]
        Compund,
        [Description("تغیرات")]
        Variation,
        [Description("واحد")]
        Singular,
        [Description("جمع")]
        Plural,
        [Description("جمع غیر ندائی")]
        JamaGhairNadai,
        [Description("واحد غیر ندائی")]
        WahidGhairNadai,
        [Description("جمع اثتثنائی")]
        JamaIstasnai,
        [Description("جنس مخالف")]
        OppositeGender,
        [Description("حالت‌‌‌ فعل")]
        FormOfFeal,
        [Description("جالت")]
        Halat,
        [Description("حالت مفعولی")]
        HalatMafooli,
        [Description("حالت اضافی")]
        HalatIzafi,
        [Description("حالت تفصیلی")]
        HalatTafseeli,
        [Description("جمع ندائی")]
        JamaNadai,
        [Description("حالت فائلی")]
        HalatFaili,
        [Description("حالت تخصیص")]
        HalatTakhsis,
        [Description("واحد ندائی")]
        WahidNadai,
        [Description("تقابلی")]
        Takabuli,
        [Description("استعمال")]
        Usage,
        Unknown
    }
}
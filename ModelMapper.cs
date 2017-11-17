using AutoMapper;
using Inshapardaz.DataImport.Database.Entities;

namespace Inshapardaz.DataImport{
    public class MappingProfile : Profile{
        public MappingProfile()
        {
            CreateMap<Model.Word, Word>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Title))
                .ForMember(d => d.TitleWithMovements, o => o.MapFrom(s => s.TitleWithMovements))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.Description))
                .ForMember(d => d.Pronunciation, o => o.MapFrom(s => s.Transiliteral))
                .ForMember(d => d.Language, o => o.Ignore())
                .ForMember(d => d.Attributes, o => o.Ignore())
                .ForMember(d => d.Dictionary, o => o.Ignore())
                .ForMember(d => d.DictionaryId, o => o.Ignore())
                .ForMember(d => d.WordRelationRelatedWord, o => o.Ignore())
                .ForMember(d => d.WordRelationSourceWord, o => o.Ignore())
                .ForMember(d => d.Meaning, o => o.Ignore())
                .ForMember(d => d.Translation, o => o.Ignore());

            CreateMap<Model.WordDetails, Word>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.WordInstance.Title))
                .ForMember(d => d.TitleWithMovements, o => o.MapFrom(s => s.WordInstance.TitleWithMovements))
                .ForMember(d => d.Description, o => o.MapFrom(s => s.WordInstance.Description))
                .ForMember(d => d.Pronunciation, o => o.MapFrom(s => s.WordInstance.Transiliteral))
                .ForMember(d => d.Language, o => o.MapFrom(s => s.Language))
                .ForMember(d => d.Attributes, o => o.MapFrom(s => s.Attributes))
                .ForMember(d => d.Dictionary, o => o.Ignore())
                .ForMember(d => d.DictionaryId, o => o.Ignore())
                .ForMember(d => d.WordRelationRelatedWord, o => o.Ignore())
                .ForMember(d => d.WordRelationSourceWord, o => o.Ignore())
                .ForMember(d => d.Meaning, o => o.MapFrom(s => s.Meanings))
                .ForMember(d => d.Translation, o => o.MapFrom(s => s.Translations));

            CreateMap<Model.WordRelation, WordRelation>()
                .ForMember(d => d.Id, o => o.MapFrom(s => s.Id))
                .ForMember(d => d.SourceWord, o => o.Ignore())
                .ForMember(d => d.SourceWordId, o => o.Ignore())
                .ForMember(d => d.RelationType, o => o.MapFrom(s => s.RelationType))
                .ForMember(d => d.RelatedWordId, o => o.MapFrom(s => s.RelatedWord.Id))
                .ForMember(d => d.RelatedWord, o => o.MapFrom(s => s.RelatedWord));
            CreateMap<Model.Translation, Translation>();
            CreateMap<Model.Meaning, Meaning>();
        }
    }
}
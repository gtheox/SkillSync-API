namespace SkillSync.API.Services;

public interface IMLService
{
    Task<decimal?> PreverCategoriaAsync(string titulo, string descricao);
    Task TreinarModeloAsync();
}


using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Trainers;
using SkillSync.API.Data;
using SkillSync.API.Models;

namespace SkillSync.API.Services;

public class MLService : IMLService
{
    private readonly SkillSyncDbContext _context;
    private readonly ILogger<MLService> _logger;
    private readonly string _modelPath;
    private MLContext? _mlContext;
    private ITransformer? _model;

    public MLService(
        SkillSyncDbContext context,
        ILogger<MLService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _modelPath = Path.Combine(environment.ContentRootPath, "ML", "projeto_categoria_model.zip");

        // Inicializar MLContext
        _mlContext = new MLContext(seed: 0);

        // Carregar modelo se existir
        if (File.Exists(_modelPath))
        {
            try
            {
                _model = _mlContext.Model.Load(_modelPath, out var modelInputSchema);
                _logger.LogInformation("Modelo ML.NET carregado com sucesso");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erro ao carregar modelo ML.NET existente");
            }
        }
    }

    public Task<decimal?> PreverCategoriaAsync(string titulo, string descricao)
    {
        try
        {
            if (_model == null || _mlContext == null)
            {
                _logger.LogWarning("Modelo ML.NET não está disponível. Retornando null.");
                return Task.FromResult<decimal?>(null);
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<ProjetoInput, ProjetoPrediction>(_model);

            var input = new ProjetoInput
            {
                Titulo = titulo,
                Descricao = descricao
            };

            var prediction = predictionEngine.Predict(input);

            _logger.LogInformation("Predição ML.NET: Categoria {Categoria} para projeto '{Titulo}'", 
                prediction.CategoriaId, titulo);

            return Task.FromResult<decimal?>((decimal)prediction.CategoriaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer predição ML.NET");
            return Task.FromResult<decimal?>(null);
        }
    }

    public Task TreinarModeloAsync()
    {
        try
        {
            _logger.LogInformation("Iniciando treinamento do modelo ML.NET...");

            // Buscar dados de treinamento do banco
            var projetos = _context.TGsProjetosContratantes
                .Where(p => p.IdCategoria != null)
                .Select(p => new ProjetoInput
                {
                    Titulo = p.DsTitulo,
                    Descricao = p.DsDescricao,
                    CategoriaId = (float)(p.IdCategoria ?? 0)
                })
                .ToList();

            if (projetos.Count < 10)
            {
                _logger.LogWarning("Dados insuficientes para treinar o modelo: {Count} projetos", projetos.Count);
                return Task.CompletedTask;
            }

            // Converter para IDataView
            var dataView = _mlContext!.Data.LoadFromEnumerable(projetos);

            // Pipeline de pré-processamento
            var pipeline = _mlContext.Transforms.Text.FeaturizeText("TituloFeatures", "Titulo")
                .Append(_mlContext.Transforms.Text.FeaturizeText("DescricaoFeatures", "Descricao"))
                .Append(_mlContext.Transforms.Concatenate("Features", "TituloFeatures", "DescricaoFeatures"))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("Label", "CategoriaId"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy())
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            // Treinar modelo
            _model = pipeline.Fit(dataView);

            // Salvar modelo
            var modelDirectory = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(modelDirectory) && !Directory.Exists(modelDirectory))
            {
                Directory.CreateDirectory(modelDirectory);
            }

            _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
            _logger.LogInformation("Modelo ML.NET treinado e salvo com sucesso em {Path}", _modelPath);
            
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao treinar modelo ML.NET");
            throw;
        }
    }
}

// Classe de input para ML.NET
public class ProjetoInput
{
    [LoadColumn(0)]
    public string Titulo { get; set; } = null!;

    [LoadColumn(1)]
    public string Descricao { get; set; } = null!;

    [LoadColumn(2)]
    public float CategoriaId { get; set; }
}

// Classe de predição para ML.NET
public class ProjetoPrediction
{
    [ColumnName("PredictedLabel")]
    public float CategoriaId { get; set; }

    [ColumnName("Score")]
    public float[]? Scores { get; set; }
}


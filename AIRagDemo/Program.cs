using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.Configuration;

Console.WriteLine("\nConsultando o Manual do Python (RAG)\n");

//Configuração dos Modelos : LLM  e Embedding
var config = new OllamaConfig
{
    Endpoint = "http://localhost:11434",
    TextModel = new OllamaModelConfig("deepseek-r1:latest", 131072),
    EmbeddingModel = new OllamaModelConfig("nomic-embed-text", 768)
};

// Cria o serviço de memória (Servidor local)
// Usar KernelMemoryBuilder com configuração explícita
var memoryBuilder = new KernelMemoryBuilder()
    .WithOllamaTextGeneration(config)
    .WithOllamaTextEmbeddingGeneration(config)
    .WithCustomTextPartitioningOptions(new TextPartitioningOptions
    {
        MaxTokensPerParagraph = 256,
        OverlappingTokens = 30
    });

// Tentar BuildMemory ou alternativa
var memory = memoryBuilder.Build();

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Iniciando Ingestão de Documentos...");

// --- 2. INGESTÃO DE DADOS (O Coração do RAG) ---
// O arquivo do manual do python é lido e dividido em chunks,
// e transformado em embeddings
// que são armazenados localmente pelo Kernel Memory.
try
{

    // O .pfd deve estar na pasta bin/Debug/netX.X
    await memory.ImportDocumentAsync(
        filePath: @"Guia_Completo_Python.pdf",
        documentId: "MANUAL_PYTHON");

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("Documento 'Guia_Completo_Python.pdf' ingerido com sucesso!");
    Console.ResetColor();
}
catch (Exception ex)
{
    // Se ocorrer o erro 404 significa que o Ollama não encontrou
    // o modelo de Embedding!
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"ERRO durante a ingestão. Verifique se o modelo está ativo no Ollama.");
    Console.WriteLine(ex.Message);
    Console.ResetColor();
    return;
}

Console.WriteLine("Modelo pronto para perguntas.\n");
Console.ResetColor();

// --- 3. LOOP DE PERGUNTAS E RESPOSTAS (RAG) ---
while (true)
{
    Console.Write("Pergunte ou (Digite 'sair'), ATENÇÃO:: PELO PODER COMPUTACIONAL PODE SER QUE DEMORE UM POUCO PARA RESPONDER (rsrs): ");
    var entradaUsuario = Console.ReadLine();

    if (entradaUsuario?.ToLower() == "sair")
        break;

    if (string.IsNullOrWhiteSpace(entradaUsuario))
        continue;

    var promptSeguro =
    "Responda somente com base nos documentos fornecidos.'\n\n " +
    $"Pergunta: {entradaUsuario}";

    // A chamada AskAsync faz a parte de RAG:
    // 1. Gera embedding da pergunta.
    // 2. Busca os chunks mais similares no armazenamento.
    // 3. Envia os chunks + pergunta para o LLM.
    var resposta = await memory.AskAsync(promptSeguro);

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine($"\nAssistente: {resposta.Result}");
    Console.ResetColor();

    // --- 1. Ajuste para Cor de Fundo ---
    Console.BackgroundColor = ConsoleColor.Cyan;
    Console.ForegroundColor = ConsoleColor.Black; // Define o texto como Preto para contraste

    // Exibe as fontes de onde a IA tirou a resposta
    Console.WriteLine("\n--- Fontes Encontradas ---");

    if (resposta.RelevantSources.Count == 0)
    {
        Console.WriteLine("Nenhuma fonte relevante encontrada.");
    }
    else
    {
        foreach (var source in resposta.RelevantSources)
        {
           
            Console.WriteLine($"- Arquivo: {source.SourceName}, Trecho: {source.Partitions.FirstOrDefault()?.Text}");
        }
    }
    Console.ResetColor();
    Console.WriteLine("--------------------------\n");
}

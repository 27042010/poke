using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

var botClient = new TelegramBotClient("6946088104:AAH_pTLDaVY61XsibOqENnb3uDlVLVQ2lX8");
var cancellationTokenSource = new CancellationTokenSource();
var httpClient = new HttpClient();

Console.WriteLine("Iniciando");

botClient.StartReceiving(
    updateHandler: UpdateHandlerAsync,
    pollingErrorHandler: PollingErrorHandlerAsync,
    receiverOptions: new ReceiverOptions
    {
        AllowedUpdates = Array.Empty<UpdateType>()
    },
    cancellationToken: cancellationTokenSource.Token
);

Console.WriteLine("Bot está funcionando");

async Task UpdateHandlerAsync(ITelegramBotClient botClient, Update update, CancellationToken cts)
{
    try
    {
        if (update.Message is not { } message || message.Text is not { } messageText)
        {
            Console.WriteLine("Mensagem inválida ou não encontrada");
            return;
        }

        var chatId = message.Chat.Id;
        Console.WriteLine($"Mensagem recebida: {messageText} de Chat ID: {chatId}");

        if (messageText.StartsWith("/info", StringComparison.OrdinalIgnoreCase))
        {
            var pokemonName = messageText.Substring(6).Trim().ToLower();

            if (string.IsNullOrEmpty(pokemonName))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Forneça o nome do Pokémon após o comando /info",
                    cancellationToken: cts
                );
                return;
            }

            var pokemonInfo = await GetPokemonInfoAsync(pokemonName);

            if (!string.IsNullOrEmpty(pokemonInfo))
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: pokemonInfo,
                    cancellationToken: cts
                );
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Pokemon não encontrado",
                    cancellationToken: cts
                );
            }
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Olá digite o comando /info com o nome do pokemon na frente para obter informações",
                cancellationToken: cts
            );
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro ao processar a mensagem: {ex.Message}");
    }
}

async Task<string> GetPokemonInfoAsync(string pokemonName)
{
    try
    {
        Console.WriteLine($"Buscando informações para: {pokemonName}");
        var response = await httpClient.GetStringAsync($"https://pokeapi.co/api/v2/pokemon/{pokemonName}");
        var json = JObject.Parse(response);

        var name = json["name"]?.ToString() ?? "Nome não encontrado";
        var types = string.Join(", ", json["types"]?.Select(t => t["type"]["name"]?.ToString()) ?? new[] { "Tipos não encontrados" });
        var abilities = string.Join(", ", json["abilities"]?.Select(a => a["ability"]["name"]?.ToString()) ?? new[] { "Habilidades não encontradas" });

        // Extraindo estatísticas e traduzindo
        var stats = json["stats"]?.Select(stat =>
        {
            var statName = stat["stat"]["name"]?.ToString();
            var baseStat = stat["base_stat"]?.ToString();

            // Tradução das estatísticas
            var statNamePt = statName switch
            {
                "hp" => "HP",
                "attack" => "Ataque",
                "defense" => "Defesa",
                "special-attack" => "Ataque Especial",
                "special-defense" => "Defesa Especial",
                "speed" => "Velocidade",
                _ => statName
            };

            return $"{Capitalize(statNamePt)}: {baseStat}";
        });

        var statsText = stats != null ? string.Join("\n", stats) : "Estatísticas não encontradas";

        return $"Nome: {Capitalize(name)}\n" +
               $"Tipos: {types}\n" +
               $"Habilidades: {abilities}\n" +
               $"Estatísticas:\n{statsText}";
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"Erro na requisição HTTP: {ex.Message}");
        return "Erro ao buscar informações do Pokemon";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erro geral: {ex.Message}");
        return "Erro ao obter informações do Pokemon";
    }
}

string Capitalize(string text)
{
    if (string.IsNullOrEmpty(text))
        return text;
    return char.ToUpper(text[0]) + text.Substring(1);
}

Task PollingErrorHandlerAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cts)
{
    Console.WriteLine($"Erro no polling: {exception.Message}");
    return Task.CompletedTask;
}

var me = await botClient.GetMeAsync(cancellationTokenSource.Token);
Console.WriteLine($"Bot {me.Username} iniciado");

Console.ReadLine();
cancellationTokenSource.Cancel();

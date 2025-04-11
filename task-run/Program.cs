using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

public interface IScopedService
{
    Task<string> GetResponseAsync(int requestNumber, string additionalText);
}

public class ScopedService : IScopedService
{
    public async Task<string> GetResponseAsync(int requestNumber, string additionalText)
    {
        // Simulação de uma resposta de sucesso com texto adicional
        await Task.Delay(5000); // Aguarda 5 segundos
        return $"Simulated API Response {requestNumber}" + additionalText;
    }
}

class Program
{
    private static readonly object lockObject = new object();
    private static List<string> sentTexts = new List<string>();
    private static int messageLimit = 5; // Limite de mensagens recebidas
    private static int messagesReceived = 0;

    static async Task Main(string[] args)
    {
        var serviceProvider = new ServiceCollection()
            .AddScoped<IScopedService, ScopedService>()
            .BuildServiceProvider();

        var textTask = Task.Run(() =>
        {
            return " - Additional Text";
        });

        var initialText = await textTask;
        var concatenatedText = initialText;

        var apiTasks = new Task<string>[5];
        var responses = new ConcurrentBag<string>();

        for (int i = 0; i < apiTasks.Length; i++)
        {
            int requestNumber = i + 1;
            apiTasks[i] = Task.Run(async () =>
            {
                await Task.Delay(10000); // Aguarda 10 segundos
                using (var scope = serviceProvider.CreateScope())
                {
                    var scopedService = scope.ServiceProvider.GetRequiredService<IScopedService>();
                    string currentText;
                    lock (lockObject)
                    {
                        if (messagesReceived >= messageLimit)
                        {
                            return $"Message limit reached. Skipping request {requestNumber}.";
                        }
                        currentText = concatenatedText;
                        concatenatedText += $" - Additional Text {requestNumber + 1}";
                    }
                    var response = await scopedService.GetResponseAsync(requestNumber, currentText);
                    Console.WriteLine($"Message received: {response}");
                    responses.Add(response);
                    lock (lockObject)
                    {
                        messagesReceived++;
                        // Remove o texto que já foi enviado
                        sentTexts.Add(currentText);
                        concatenatedText = concatenatedText.Replace(currentText, "").Trim();
                    }
                    return response;
                }
            });
        }

        await Task.WhenAll(apiTasks);

        foreach (var response in responses)
        {
            Console.WriteLine(response);
        }
    }
}

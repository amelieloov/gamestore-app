using Azure.Identity;
using Azure.Storage.Blobs;
using GameStore.Models;
using System.Text.Json;

namespace GameStore.Services
{
    public class GameService
    {
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "games.json");

        public List<Game> GetAllGames()
        {
            var jsonData = File.ReadAllText(_filePath);
            var gameList = JsonSerializer.Deserialize<List<Game>>(jsonData) ?? new List<Game>();
            UploadImages();
            return gameList;
        }
        public async void UploadImages()
        {
            string storageAccountUrl = "https://gamestorestrg.blob.core.windows.net/";
            string containerName = "images";

            var jsonData = File.ReadAllText(_filePath);
            var gameList = JsonSerializer.Deserialize<List<Game>>(jsonData) ?? new List<Game>();

            // Use DefaultAzureCredential (works with Managed Identity in Azure)
            var credential = new DefaultAzureCredential();
            var blobServiceClient = new BlobServiceClient(new Uri(storageAccountUrl), credential);
            var containerClient = blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();

            using (HttpClient httpClient = new HttpClient())
            {
                foreach (var game in gameList)
                {
                    try
                    {
                        // Download image from URL
                        HttpResponseMessage response = await httpClient.GetAsync(game.Cover);
                        response.EnsureSuccessStatusCode();
                        Stream imageStream = await response.Content.ReadAsStreamAsync();

                        // Create a blob name
                        string fileName = $"{game.Id}_{Path.GetFileName(new Uri(game.Cover).AbsolutePath)}";

                        // Upload to blob
                        var blobClient = containerClient.GetBlobClient(fileName);
                        await blobClient.UploadAsync(imageStream, overwrite: true);

                        Console.WriteLine($"Uploaded: {fileName}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {game.Name}: {ex.Message}");
                    }
                }
            }
        }
    }
}

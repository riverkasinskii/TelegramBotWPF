using System;
using System.Collections.ObjectModel;
using System.IO;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot.Types.InputFiles;
using System.Text.Json;
using System.Linq;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Exceptions;
using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace HomeWork10
{
    class TelegramMessageClient
    {                
        MainWindow window;

        ITelegramBotClient bot;
        public ObservableCollection<MessageLog> BotMessageLog { get; set; }

        HashSet<string> diskFiles = new HashSet<string>();

        public TelegramMessageClient(MainWindow W)
        {
            BotMessageLog = new ObservableCollection<MessageLog>();

            window = W;                      

            bot = new TelegramBotClient("5427432748:AAEbHJtzqD5R9wIBf58z4kzpHpw0GNyGldI");

            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = { },
            };
            bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, 
            Update update, CancellationToken cancellationToken)
        {     
            var messageText = update.Message!.Text;                        

            window.Dispatcher.Invoke(() =>
            {
                BotMessageLog.Add(
                new MessageLog(
                    DateTime.Now.ToLongTimeString(), 
                    messageText!, 
                    update.Message.Chat.FirstName!, 
                    update.Message.Chat.Id));                             
            });
                        
            var options = new JsonSerializerOptions 
            { 
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
            };

            string json = JsonSerializer.Serialize(BotMessageLog, options);

            System.IO.File.WriteAllText("messagelog.json", json);

            if (update?.Type == UpdateType.Message && update?.Message?.Text != null)
            {
                await HandleMessage(botClient, update.Message);
            }
            if (update?.Message?.Type == MessageType.Document)
            {
                await DownLoadAsync(update.Message.Document!.FileId, 
                                    update.Message.Document.FileName!);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                      text: $"Вы загрузили документ: {update.Message.Document?.FileName}");
                Console.WriteLine($"{update.Message.Document?.FileName} загружен на диск" +
                      $" {DateTime.Now} пользователем {update.Message.Chat.FirstName}");
            }
            if (update?.Message?.Type == MessageType.Photo)
            {
                var photo = update.Message.Photo;
                string fileId = photo!.Last().FileId;
                string fileName = photo!.Last().FileUniqueId + ".jpg";
                await DownLoadAsync(fileId, fileName);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                      text: $"Вы загрузили фото: {fileName}");
                Console.WriteLine($"{fileName} загружен на диск" +
                      $" {DateTime.Now} пользователем {update.Message.Chat.FirstName}");
            }
            if (update?.Message?.Type == MessageType.Video)
            {
                string path = update.Message.Video?.FileUniqueId + ".mp4";
                await DownLoadAsync(update.Message.Video?.FileId!, path);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                      text: $"Вы загрузили видео: {path}");
                Console.WriteLine($"{path} загружен на диск" +
                      $" {DateTime.Now} пользователем {update.Message.Chat.FirstName}");
            }
            if (update?.Message?.Type == MessageType.Audio)
            {
                await DownLoadAsync(update.Message.Audio?.FileId!, 
                                    update.Message.Audio?.FileName!);
                await botClient.SendTextMessageAsync(update.Message.Chat.Id,
                      text: $"Вы загрузили аудиофайл: {update.Message.Audio?.FileName}");
                Console.WriteLine($"{update.Message.Audio?.FileName} загружен на диск" +
                      $" {DateTime.Now} пользователем {update.Message.Chat.FirstName}");
            }
            async Task HandleMessage(ITelegramBotClient botClient, Message message)
            {
                if (message.Text == "/start")
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id,
                        text: $"Приветствую тебя {update.Message?.Chat.FirstName}." +
                        $"\nЗагрузите файл:\n" +
                        "/listFiles - список файлов\n" +
                        "После загрузки файла, он доступен для скачивания.\n" +
                        "Для скачивания введите имя файла\n" +
                        "например picture.jpg");
                }
                else if (message.Text == "/listFiles")
                {
                    if (Directory.Exists("listFiles"))
                    {
                        string output = "На диске: ";
                        int counter = 1;
                        foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory() + "\\listFiles"))
                        {
                            diskFiles.Add(file.Split('\\')[file.Split('\\').Length - 1]);
                            output += "\n" + counter + ". " + file.Split('\\')[file.Split('\\').Length - 1];
                            counter++;
                        }
                        await botClient.SendTextMessageAsync(message.Chat, output);
                    }
                    else
                    {
                        await botClient.SendTextMessageAsync(message.Chat, "Загруженных файлов нет");
                    }

                }
                else if (diskFiles.Contains(message.Text!))
                {
                    await botClient.SendTextMessageAsync(message.Chat, "Высылаю файл");
                    using (var stream = System.IO.File.OpenRead("listFiles\\" + message.Text))
                    {
                        InputOnlineFile file = new InputOnlineFile(stream, message.Text);
                        await botClient.SendDocumentAsync(message.Chat, file);
                    }
                }
                
            }
            async Task DownLoadAsync(string fileId, string path)
            {
                if (!Directory.Exists("listFiles"))
                {
                    Directory.CreateDirectory("listFiles");
                }
                var file = await bot.GetFileAsync(fileId);
                FileStream fs = new FileStream("listFiles\\" + path, FileMode.Create);
                await bot.DownloadFileAsync(file.FilePath!, fs);
                fs.Close();
                fs.Dispose();
            }            
        }
        public void SendMessage(string Text, string Id)
        {
            long id = Convert.ToInt64(Id);
            bot.SendTextMessageAsync(id, Text);
        }                
        Task HandleErrorAsync(ITelegramBotClient botClient, 
            Exception exception, CancellationToken cancellationToken)
        {
            string ErrorMessage = exception switch
            {
                ApiRequestException apiRequestException
                => $"Ошибка телеграм АПИ:\n{apiRequestException.ErrorCode}\n{apiRequestException.Message}",
                _ => exception.ToString()
            };
            Console.WriteLine(ErrorMessage);
            return Task.CompletedTask;
        }
    }
}

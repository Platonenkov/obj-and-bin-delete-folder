using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Konsole;
using Microsoft.Extensions.Configuration;

namespace Clean_Directory_Din_Obj_CORE
{
    //dotnet publish -r win-x64 -c Release --self-contained -o release /p:PublishSingleFile=true /p:PublishTrimmed=true
    class Program
    {
        private static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Environment.CurrentDirectory);

        private static bool Auto { get; set; }
        private static bool LocalClean { get; set; }
        private static IEnumerable<string> Directories { get; set; }

        static void Main(string[] args)
        {
            GetConfiguration(args);
            var settings_row = args.Length>0 ? args.Aggregate((current, s) => current + s):string.Empty;
            var settings = settings_row.Split('-').Select(r => r.Trim()).ToArray();

            var ask_args = args.Contains(a => a == "-a");
            var local_args = args.Contains(a => a == "-l");
            var directory_args = args.Contains(a => a=="-d");
            string[] directories_from_args = settings.Where(s => s.StartsWith('d')).SelectMany(s =>s.TrimStart('d').Split(';')).ToArray();
            var currDir = CurrentDirectory;
            var directories = Directories.ToArray();
            Console.WriteLine("Check settings");
            if (local_args || directory_args || ask_args) // работа по аргументам аргументов
            {
                Console.WriteLine("Work in auto by arguments");
                if (ask_args)
                {
                    Console.WriteLine("Input directory to clean");
                    var dir = Console.ReadLine();
                    CleanDirectories(new [] { dir });
                    return;
                }
                if (local_args && directory_args)
                {
                    if (directories_from_args.Length == 0)
                    {
                        Console.WriteLine("Not found address in arguments");
                    }
                    else
                        CleanDirectories(directories_from_args);

                    CleanDirectories(new []{currDir.FullName});
                }
                else if (local_args)
                {
                    CleanDirectories(new[] { currDir.FullName });
                }
                else
                {
                    if (directories_from_args.Length == 0)
                    {
                        Console.WriteLine("Not found address in arguments");
                        return;
                    }
                    CleanDirectories(directories_from_args);

                }
            }
            else if (!Auto && !LocalClean)
            {
                Console.WriteLine("Settings file was found");
                Console.WriteLine("If you want to clean directories from file - press 1\n"
                                  + "If you want clean current directory - press 2\n"
                                  + "To close - press 3");
                var answer = 0;
                while (answer == 0)
                {
                    Console.WriteLine();
                    Console.WriteLine("Press number from 1 to 3");
                    var flag = int.TryParse(Console.ReadKey().KeyChar.ToString(), out var press);
                    if (flag) answer = press > 0 && press < 4 ? press : 0;
                }

                Console.WriteLine();
                switch (answer)
                {
                    case 3: return;
                    case 1 when directories.Length > 0:
                        CleanDirectories(directories);
                        break;
                    case 1:
                        Console.WriteLine("Not found address in settings file or arguments");
                        DirectoriesSectionIsEmpty();
                        return;
                    case 2:
                        CleanDirectories(new string[] { currDir.FullName });
                        break;
                }
            }
            else if (Auto && !LocalClean ) //авто удаление из директорий по списку
            {
                Console.WriteLine("Work in auto");

                if (directories.Length == 0)
                {
                    Console.WriteLine("Not found address in settings file");
                    DirectoriesSectionIsEmpty();
                    return;
                }
                CleanDirectories(directories);
            }
            else if (LocalClean) //авто удаление из текущей директории
            {
                Console.WriteLine("Work in auto");
                CleanDirectories(new string[] { currDir.FullName });
            }

            Console.WriteLine($"---------------FINISH---------------");
            Console.ReadLine();
        }

        /// <summary>
        /// читает файл конфигурации и возвращает настройки
        /// </summary>
        /// <param name="args">аргументы командной строки</param>
        /// <returns>конфигурация приложения</returns>
        private static IConfiguration GetConfiguration(string[] args)
        {
            try
            {
                var config =  new ConfigurationBuilder()
                   .AddJsonFile("Clean.setting", optional: true, reloadOnChange: true)
                   .AddEnvironmentVariables()
                   .AddCommandLine(args)
                   .Build();
                bool.TryParse(config.GetSection("AutoClean").Value, out var auto);
                Auto = auto;
                bool.TryParse(config.GetSection("CleanLocal").Value, out var local);
                LocalClean = local;
                Directories = GetDirectoriPathFromConfig(config);
                return config;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error to read configuration file, check sections\n{e}");
                Console.ReadLine();
                throw;
            }
        }
        /// <summary>
        /// чтение директорий для чистки
        /// </summary>
        /// <param name="config">конфигурация</param>
        /// <returns>список директорий</returns>
        private static IEnumerable<string> GetDirectoriPathFromConfig(IConfiguration config)
        {
            try
            {
                var dir_serction = config.GetSection("Directories");
                return dir_serction.Value == null ? dir_serction.GetChildren().Select(c => c.Value.Trim()).ToArray() : new[] {dir_serction.Value};
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error to read directories configuration\n{e.Message}");
            }

            return Array.Empty<string>();
        }
        /// <summary>
        /// Запускает процесс очистки каталогов
        /// </summary>
        /// <param name="directories">список каталогов</param>
        private static void CleanDirectories(IEnumerable<string> directories)
        {
            var tasks = new List<Task>();

            foreach (var addres_directory in directories)
            {
                Console.WriteLine($"Try clean {addres_directory}...");

                var directory = new DirectoryInfo(addres_directory);
                if (!directory.Exists)
                {
                    Console.WriteLine($"Directory\n{directory.FullName}\n NOT EXISTS");
                }
                else
                {
                    tasks.Add(new Task(() => CleanDirectories(addres_directory)));
                }
            }

            foreach (var task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks.ToArray());

        }
        /// <summary>
        /// печатает текст что в настройках нет директорий для очистки
        /// </summary>
        private static void DirectoriesSectionIsEmpty()
        {
            Console.WriteLine("just enter in the Clean.setting rows with full address to directories section\n\n"
                              + "Example:\n\t"
                              + "\"Directories\": 	{\n\t\t"
                              + "\"1\": \"D:\\Test 1\",\n\t\t"
                              + "\"2\": \"D:\\Test 2\n\t"
                              + "}");
            Console.WriteLine("\n\nPress any key to close application");
            Console.ReadKey();
        }
        /// <summary>
        /// Чистит директорию и вложенные в неё
        /// </summary>
        /// <param name="parent_directory">основная директория</param>
        private static void CleanDirectories(string parent_directory)
        {
            DirectoryInfo parentDirectory = new DirectoryInfo(parent_directory);
            DirectoryInfo[] directories_bin = parentDirectory.GetDirectories("bin", SearchOption.AllDirectories);
            DirectoryInfo[] directories_obj = parentDirectory.GetDirectories("obj", SearchOption.AllDirectories);

            var tasks = new List<Task>();
            var bars = new List<ProgressBar>();

            var bin_count = directories_bin.Length > 0 ? directories_bin.Length : 1;
            var obj_count = directories_obj.Length > 0 ? directories_obj.Length : 1;

            var pb_bin = new ProgressBar(bin_count);
            var pb_obj = new ProgressBar(obj_count);
            bars.Add(pb_bin);
            bars.Add(pb_obj);


            pb_bin.Refresh(0, $"Delete BIN catalogs");
            tasks.Add(new Task(() => DeleteDirectory(directories_bin, pb_bin, "bin")));


            pb_obj.Refresh(0, $"Delete OBJ catalogs");
            tasks.Add(new Task(() => DeleteDirectory(directories_obj, pb_obj, "obj")));

            foreach (var task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks.ToArray());

        }
        /// <summary>
        /// Удаляет каталоги
        /// </summary>
        /// <param name="directories">список для очистки</param>
        /// <param name="pb">прогресс</param>
        /// <param name="dir_name">коревая директория</param>
        private static void DeleteDirectory(DirectoryInfo[] directories, ProgressBar pb, string dir_name)
        {
            if (directories.Length != 0)
            {
                int count = 1;
                foreach (var dir in directories)
                {
                    pb.Refresh(count, $"Delete catalog {count} из {directories.Length}");
                    if (Directory.Exists(dir.FullName)) dir.Delete(true);
                    count++;
                }
                pb.Refresh(directories.Length, $"{dir_name} directories successfully deleted");
            }
            else
            {
                pb.Refresh(1, $"No {dir_name} folders here");
            }
        }
    }
}

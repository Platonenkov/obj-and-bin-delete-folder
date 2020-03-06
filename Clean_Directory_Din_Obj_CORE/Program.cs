using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konsole;

namespace Clean_Directory_Din_Obj_CORE
{
    class Program
    {
        private static readonly string FileName = "Setting.txt";
        private static readonly DirectoryInfo CurrentDirectory = new DirectoryInfo(Environment.CurrentDirectory);
        private static readonly FileInfo SettingFile = new FileInfo(FileName);

        private static void CreateSettingFile()
        {
            if (!CurrentDirectory.ContainsFile(FileName))
            {
                using (File.Create(SettingFile.FullName)) ;
            }

        }

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
                    tasks.Add(new Task(() => GetDirectories(addres_directory)));
                }


            }

            foreach (var task in tasks)
            {
                task.Start();
            }

            Task.WaitAll(tasks.ToArray());

        }
        static void Main(string[] args)
        {

            var file_name = FileName;
            var currDir = CurrentDirectory;
            var setting_file = SettingFile;

            Console.WriteLine("Search of the file of settings");

            //Если файл настроек не найден
            if (!currDir.ContainsFile(file_name))
            {
                Console.WriteLine("File was not found");
                Console.WriteLine("If you want to create file -  press 1\n"
                                  + "If you want clean current directory - press 2\n"
                                  + "To close - press 3");
                var answer = 0;
                while (answer == 0)
                {
                    Console.WriteLine("Press number from 1 to 3");
                    var flag = int.TryParse(Console.Read().ToString(), out var press);
                    if (flag) answer = press > 0 && press < 4 ? press : 0;
                }


                if (answer == 3) return;
                else if (answer == 1)
                {
                    CreateSettingFile();
                    NotFindSettingsInFile();
                    return;
                }
                else if (answer == 2)
                {
                    CleanDirectories(new string[]{currDir.FullName});
                }
            }
            else
            {
                Console.WriteLine("Settings file was found");
                Console.WriteLine("If you want to clean directories from file - press 1\n"
                                  + "If you want clean current directory - press 2\n"
                                  + "To close - press 3");
                var answer = 0;
                while (answer == 0)
                {
                    Console.WriteLine("Press number from 1 to 3");
                    var flag = int.TryParse(Console.Read().ToString(), out var press);
                    if (flag) answer = press > 0 && press < 4 ? press : 0;
                }


                if (answer == 3) return;
                else if (answer == 1)
                {
                    var directories = ReadSettingsFile(setting_file.FullName).ToArray();
                    if (directories.Length > 0 ) CleanDirectories(directories);
                    else
                    {
                        Console.WriteLine("Not found address in settings file");
                        NotFindSettingsInFile();
                        return;
                    }
                }
                else if (answer == 2)
                {
                    CleanDirectories(new string[] { currDir.FullName });
                }

            }

            Console.WriteLine($"---------------FINISH---------------");
            Console.ReadLine();
        }

        private static void NotFindSettingsInFile()
        {
            Console.WriteLine("just enter in the Setting.txt rows with full address to directories\n"
                              + "Example: C:\\Temp\n"
                              + "If you want enter more that one, split it by Enter");
            Console.WriteLine("Press any key to close application");
            Console.ReadKey();
        }
        private static IEnumerable<string> ReadSettingsFile(string filePath)
        {
            var directories = new List<string>();
            try
            {
                var info_dir = File.ReadAllText(filePath, Encoding.UTF8);

                var dirs = info_dir.Split('\n');
                foreach (var dir in dirs)
                {
                    var row = dir.Replace("\r", "");
                    if (row.Length > 3) directories.Add(row);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Error to read file");
                Console.WriteLine();
                Console.WriteLine(e);
                throw;
            }

            return directories;

        }

        private static void GetDirectories(string parent_directory)
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

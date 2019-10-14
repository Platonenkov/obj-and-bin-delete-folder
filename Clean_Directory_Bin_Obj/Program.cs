using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Konsole;
using MathCore;
using Microsoft.VisualBasic.FileIO;
using SearchOption = System.IO.SearchOption;

namespace Clean_Directory_Bin_Obj
{
    class Program
    {

        static void Main(string[] args)
        {

            var addres_directories = new List<string>();

            var file = "Directory-to-clean.txt";
            var currDir = new DirectoryInfo(Environment.CurrentDirectory);
            var fileInfo = new FileInfo(file);


            if (!currDir.ContainsFile(file))
            {
                using (File.Create(fileInfo.FullName)) ;
                using (new StreamWriter(fileInfo.FullName, false, Encoding.UTF8)) ;
            }

            try
            {
                var info_dir = File.ReadAllText(fileInfo.FullName, Encoding.UTF8);
                
                var dirs = info_dir.Split('\n');
                foreach (var dir in dirs)
                {
                    var row = dir.Replace("\r", "");
                    if (row.Length>3) addres_directories.Add(row);
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            if (addres_directories.Count == 0)
            {
                Console.WriteLine($"File with directory info {file} haven't line with directories");
                Console.ReadLine();
                return;
            }
            var tasks = new List<Task>();

            foreach (var addres_directory in addres_directories)
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

            Console.WriteLine($"---------------FINISH---------------");
            Console.ReadLine();
        }

        private static void GetDirectories(string parent_directory)
        {
            DirectoryInfo parentDirectory = new DirectoryInfo(parent_directory);
            DirectoryInfo[] directories_bin = parentDirectory.GetDirectories("bin",SearchOption.AllDirectories);
            DirectoryInfo[] directories_obj = parentDirectory.GetDirectories("obj",SearchOption.AllDirectories);

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
                    if(Directory.Exists(dir.FullName))dir.Delete(true);
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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using MathCore.Annotations;

namespace Clean_Directory_Bin_Obj
{
    namespace System.IO
    {
        public static class DirectoryInfoExtensions
        {
            [NotNull]
            public static FileSystemWatcher GetWatcher([NotNull] this DirectoryInfo directory, [CanBeNull] Action<FileSystemWatcher> initializer = null) => directory.GetWatcher(null, initializer);

            public static bool ContainsFile([NotNull] this DirectoryInfo directory, [NotNull] string file) => File.Exists(Path.Combine(directory.FullName, file));

            public static bool ContainsFileMask([NotNull] this DirectoryInfo directory, [NotNull] string mask) => directory.EnumerateFiles(mask).Any();

            [NotNull, ItemNotNull] public static IEnumerable<FileInfo> FindFiles([NotNull] this DirectoryInfo dir, [NotNull] string mask) => dir.EnumerateDirectories().SelectMany(d => d.FindFiles(mask)).InsertBefore(dir.EnumerateFiles(mask));

            [NotNull] private static readonly WindowsIdentity sf_CurrentSystemUser = WindowsIdentity.GetCurrent();

            public static bool CanAccessToDirectoryListItems([NotNull] this DirectoryInfo dir) => dir.CanAccessToDirectory(FileSystemRights.ListDirectory);

            public static bool CanAccessToDirectory([NotNull] this DirectoryInfo dir, FileSystemRights AccessRight = FileSystemRights.Modify)
                => dir.CanAccessToDirectory(sf_CurrentSystemUser, AccessRight);

            public static bool CanAccessToDirectory([NotNull] this DirectoryInfo dir, [NotNull] WindowsIdentity user, FileSystemRights AccessRight = FileSystemRights.Modify)
            {
                if (dir is null) throw new ArgumentNullException(nameof(dir));
                if (!dir.Exists) throw new InvalidOperationException($"Директория {dir.FullName} не существует");
                if (user is null) throw new ArgumentNullException(nameof(user));
                if (user.Groups is null) throw new ArgumentException("В идетнификаторе пользователя отсутствует ссылка на группы", nameof(user));

                try
                {
                    AuthorizationRuleCollection rules;
                    try
                    {
                        rules = dir.GetAccessControl(AccessControlSections.Access).GetAccessRules(true, true, typeof(SecurityIdentifier));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        //Debug.WriteLine($"CanAccessToDirectory: Отсутствует разрешение на просмотр разрешений каталога {dir.FullName}");
                        return false;
                    }
                    catch (InvalidOperationException e)
                    {
                        //Debug.WriteLine($"CanAccessToDirectory: Ошибка чтения каталога {dir.FullName}\r\n{e}");
                        return false;
                    }

                    var allow = false;
                    var deny = false;
                    var access_rules = rules.OfType<FileSystemAccessRule>().Where(rule => user.Groups.Contains(rule.IdentityReference) && (rule.FileSystemRights & AccessRight) == AccessRight).ToArray();
                    if (access_rules.Length == 0)
                    {
                        //Trace.WriteLine($"CanAccessToDirectory: В списке прав доступа к {dir.FullName} не найдено записей");
                        return false;
                    }
                    foreach (var rule in access_rules)
                        switch (rule.AccessControlType)
                        {
                            case AccessControlType.Allow:
                                allow = true;
                                break;
                            case AccessControlType.Deny:
                                deny = true;
                                break;
                        }
                    return allow && !deny;
                }
                catch (IOException)
                {
                    //Debug.WriteLine($"CanAccessToDirectory: Ошибка чтения каталога {dir.FullName}\r\n{e}");
                }

                //Trace.WriteLine($"CanAccessToDirectory: Доступ к дирректории {dir.FullName} отсутствует (проверка прав: {AccessRight})");
                return false;
            }

            [NotNull]
            public static Process ShowInFileExplorer([NotNull] this FileSystemInfo dir) => Process.Start("explorer", $"/select,\"{dir.FullName}\"") ?? throw new InvalidOperationException();

            [NotNull]
            public static Process OpenInFileExplorer([NotNull] this DirectoryInfo dir) => Process.Start("explorer", dir.FullName) ?? throw new InvalidOperationException();

            [CanBeNull]
            public static string GetRelativePosition([NotNull] this DirectoryInfo current, [NotNull] DirectoryInfo other)
            {
                if (current is null) throw new ArgumentNullException(nameof(current));
                if (other is null) throw new ArgumentNullException(nameof(other));
                return GetRelativePosition(current.FullName, other.FullName);
            }

            [CanBeNull]
            public static string GetRelativePosition([NotNull] string current, [NotNull] string other)
            {
                if (current is null) throw new ArgumentNullException(nameof(current));
                if (other is null) throw new ArgumentNullException(nameof(other));

                const StringComparison str_cmp = StringComparison.InvariantCultureIgnoreCase;
                return !string.Equals(Path.GetPathRoot(current), Path.GetPathRoot(other), str_cmp)
                    ? null
                    : current.StartsWith(other, str_cmp)
                        ? current.Remove(0, other.Length)
                        : other.StartsWith(current, str_cmp)
                            ? other.Remove(0, current.Length)
                            : null;
            }

            public static bool IsSubDirectoryOf([CanBeNull] this DirectoryInfo target, [CanBeNull] DirectoryInfo parent) => !(target is null || parent is null) && target.FullName.StartsWith(parent.FullName, StringComparison.InvariantCultureIgnoreCase);

            public static void MoveTo(this DirectoryInfo Directory, DirectoryInfo Destination) => Directory.MoveTo(Destination.FullName);

            /// <summary>Получение поддиректории по заданному пути. Если поддиректория отсутствует, то создать новую</summary>
            /// <param name="ParentDirectory">Родительская директория</param>
            /// <param name="SubDirectoryPath">Относительный путь к поддиректории</param>
            /// <returns>Поддиректория</returns>
            [NotNull]
            public static DirectoryInfo SubDirectoryOrCreate([NotNull] this DirectoryInfo ParentDirectory, [NotNull] string SubDirectoryPath)
            {
                if (ParentDirectory is null) throw new ArgumentNullException(nameof(ParentDirectory));
                if (SubDirectoryPath is null) throw new ArgumentNullException(nameof(SubDirectoryPath));
                if (string.IsNullOrWhiteSpace(SubDirectoryPath)) throw new ArgumentException("Не указан путь дочернего каталога", nameof(SubDirectoryPath));

                var sub_dir_path = Path.Combine(ParentDirectory.FullName, SubDirectoryPath);
                var sub_dir = new DirectoryInfo(sub_dir_path);
                if (sub_dir.Exists) return sub_dir;
                sub_dir.Create();
                sub_dir.Refresh();
                return sub_dir;
            }

            /// <summary>Формирование информации о поддиректории, заданной своим именем, либо относительным путём</summary>
            /// <param name="Directory">Корнневая директория</param><param name="SubDirectoryPath">Путь к поддиректории</param>
            /// <exception cref="ArgumentNullException">Если указана пустая ссылка на <paramref name="Directory"/></exception>
            /// <exception cref="ArgumentNullException">Если указана пустая ссылка на <paramref name="SubDirectoryPath"/></exception>
            /// <returns>Информация о поддиректории</returns>
            [NotNull]
            public static DirectoryInfo SubDirectory([NotNull] this DirectoryInfo Directory, [NotNull] string SubDirectoryPath)
            {
                if (Directory is null) throw new ArgumentNullException(nameof(Directory));
                if (SubDirectoryPath is null) throw new ArgumentNullException(nameof(SubDirectoryPath));
                return string.IsNullOrEmpty(SubDirectoryPath) ? Directory : new DirectoryInfo(Path.Combine(Directory.FullName, SubDirectoryPath));
            }

            public static FileInfo GetFile([NotNull] this DirectoryInfo Directory, string FileName) => new FileInfo(Path.Combine(Directory.FullName, FileName));
        }
    }
}

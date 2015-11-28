using System;

using PCLStorage;

namespace MineLib.PacketBuilder.WrapperInstances
{
    public class FileSystemWrapperInstance : Aragas.Core.Wrappers.IFileSystem
    {
        public IFolder AssemblyFolder { get; private set; }
        public IFolder ContentFolder { get; private set; }
        public IFolder SettingsFolder { get; private set; }
        public IFolder LogFolder { get; private set; }
        public IFolder CrashLogFolder { get; private set; }
        public IFolder UsersFolder { get; private set; }
        public IFolder LuaFolder { get; private set; }
        public IFolder DatabaseFolder { get; private set; }

        public FileSystemWrapperInstance()
        {
            var baseDirectory = FileSystem.Current.GetFolderFromPathAsync(AppDomain.CurrentDomain.BaseDirectory).Result;

            AssemblyFolder = baseDirectory.CreateFolderAsync("Protocols", CreationCollisionOption.OpenIfExists).Result;
            ContentFolder   = baseDirectory.CreateFolderAsync("Content", CreationCollisionOption.OpenIfExists).Result;
            SettingsFolder  = baseDirectory.CreateFolderAsync("Settings", CreationCollisionOption.OpenIfExists).Result;
            LogFolder       = baseDirectory.CreateFolderAsync("Logs", CreationCollisionOption.OpenIfExists).Result;

            UsersFolder = baseDirectory.CreateFolderAsync("Users", CreationCollisionOption.OpenIfExists).Result;
            LuaFolder = baseDirectory.CreateFolderAsync("Lua", CreationCollisionOption.OpenIfExists).Result;
            DatabaseFolder = baseDirectory.CreateFolderAsync("Database", CreationCollisionOption.OpenIfExists).Result;
        }
    }
}

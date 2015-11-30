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
        public IFolder OutputFolder { get; private set; }

        public FileSystemWrapperInstance()
        {
            var baseDirectory = FileSystem.Current.GetFolderFromPathAsync(AppDomain.CurrentDomain.BaseDirectory).Result;

            OutputFolder = baseDirectory.CreateFolderAsync("Output", CreationCollisionOption.OpenIfExists).Result;
        }
    }
}

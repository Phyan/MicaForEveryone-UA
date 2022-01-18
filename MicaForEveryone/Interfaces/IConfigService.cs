﻿using System.Threading.Tasks;

namespace MicaForEveryone.Interfaces
{
    public interface IConfigService
    {
        IConfigSource ConfigSource { get; }
        IRule[] Rules { get; }
        Task LoadAsync();
        Task SaveAsync();
    }
}